// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.AccessControl;
namespace IdleReminder;
// Ordinary-permission ROT reader. Only a bounded count crosses the local pipe.
internal static class SlideShowHelper {
 const string Flag="--ppt-state-helper";
 static readonly SemaphoreSlim gate=new(1);
 static readonly CancellationTokenSource lifetime=new();
 static string? endpoint;static long retryAt;static bool warned;
 internal static void Stop()=>lifetime.Cancel();
 internal static async Task<int?> QueryAsync(CancellationToken token){
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token,lifetime.Token);timeout.CancelAfter(1500);
  bool held=false;string stage="gate";
  try{
   await gate.WaitAsync(timeout.Token);held=true;
   if(endpoint==null){
    if(Environment.TickCount64<retryAt)return null;
    endpoint="IdleReminderPpt-"+Guid.NewGuid().ToString("N");
    var name=endpoint;retryAt=Environment.TickCount64+30000;
    // Explorer, not this elevated process, creates the ordinary reader.
    _=Task.Run(()=>Launch(name));
   }
   using var pipe=new NamedPipeClientStream(".",endpoint,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Anonymous);
   stage="connect";await pipe.ConnectAsync(timeout.Token);stage="identity";
   if(!GetNamedPipeServerProcessId(pipe.SafePipeHandle,out uint pid)||(!SameUser((int)pid)||!string.Equals(SuppressionDetector.ImagePath((int)pid),Environment.ProcessPath,StringComparison.OrdinalIgnoreCase)))throw new IOException("Unexpected reader identity");
   stage="request";await pipe.WriteAsync(new byte[]{1},timeout.Token);stage="read";
   var data=new byte[8];int size=0;
   while(size<data.Length){int n=await pipe.ReadAsync(data.AsMemory(size,1),timeout.Token);if(n==0)break;if(data[size++]=='\n')break;}
   if(size==0||data[size-1]!='\n')throw new IOException("Invalid reader response");
   // Closing this client is the read-complete acknowledgement; never discard a valid result due to an extra write.
   var wire=System.Text.Encoding.ASCII.GetString(data,0,size-1);SlideShowDetector.TraceProbe?.Invoke("wire="+SlideHelperProtocol.Encode(SlideHelperProtocol.Decode(wire)));
   return SlideHelperProtocol.Decode(wire);
  }catch(OperationCanceledException){SlideShowDetector.TraceProbe?.Invoke("ipc-timeout-"+stage);if(held&&!lifetime.IsCancellationRequested&&Environment.TickCount64>=retryAt)endpoint=null;return null;}
  catch(Exception ex){SlideShowDetector.TraceProbe?.Invoke("ipc-failed-"+stage+"-"+ex.GetType().Name);if(!warned){warned=true;Store.Log("ppt-helper-unavailable-"+stage,ex);}if(Environment.TickCount64>=retryAt)endpoint=null;return null;}
  finally{if(held)gate.Release();}
 }
 static void Launch(string name){
  object? windows=null,browser=null,view=null,shell=null;
  try{
   if(lifetime.IsCancellationRequested)return;
   var exe=Environment.ProcessPath;if(exe==null||Path.GetFileNameWithoutExtension(exe).Equals("dotnet",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Executable unavailable");
   windows=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"),true)!);
   object[] args=[0,0,8,0,1];
   browser=windows!.GetType().InvokeMember("FindWindowSW",System.Reflection.BindingFlags.InvokeMethod,null,windows,args);
   view=browser!.GetType().InvokeMember("Document",System.Reflection.BindingFlags.GetProperty,null,browser,null);
   shell=view!.GetType().InvokeMember("Application",System.Reflection.BindingFlags.GetProperty,null,view,null);
   using var own=Process.GetCurrentProcess();
   string parameters=$"{Flag} {name} {Environment.ProcessId} {own.StartTime.ToUniversalTime().Ticks}";
   shell!.GetType().InvokeMember("ShellExecute",System.Reflection.BindingFlags.InvokeMethod,null,shell,[exe,parameters,Path.GetDirectoryName(exe)!,"open",0]);
  }catch(Exception ex){Store.Log("ppt-helper-launch-failed",ex);}
  finally{foreach(var value in new[]{shell,view,browser,windows})if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
 }
 static NamedPipeServerStream CreatePipe(string name){
  using var identity=WindowsIdentity.GetCurrent();var sid=identity.User??throw new UnauthorizedAccessException();
  var security=new PipeSecurity();security.SetAccessRuleProtection(true,false);security.SetOwner(sid);
  security.AddAccessRule(new PipeAccessRule(sid,PipeAccessRights.FullControl,AccessControlType.Allow));
  return NamedPipeServerStreamAcl.Create(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.FirstPipeInstance,128,128,security);
 }
 static bool SameUser(int pid){
  IntPtr process=OpenProcess(0x1000,false,pid),token=IntPtr.Zero;
  if(process==IntPtr.Zero)return false;
  try{if(!OpenProcessToken(process,8,out token))return false;using var peer=new WindowsIdentity(token);using var own=WindowsIdentity.GetCurrent();return peer.User!=null&&peer.User==own.User;}
  finally{if(token!=IntPtr.Zero)CloseHandle(token);CloseHandle(process);}
 }
 [DllImport("kernel32.dll")]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("advapi32.dll")]static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetNamedPipeClientProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe,out uint processId);
 internal static bool IsHelper(string[] args)=>args.Contains(Flag);
 internal static void Run(string[] args){
  int index=Array.IndexOf(args,Flag);
  if(index<0||args.Length!=index+4||!SlideHelperProtocol.ValidName(args[index+1])||!int.TryParse(args[index+2],out int owner)||!long.TryParse(args[index+3],out long started)||StartupElevation.IsAdministrator())return;
  bool OwnerAlive(){try{using var process=Process.GetProcessById(owner);return !process.HasExited&&process.StartTime.ToUniversalTime().Ticks==started&&string.Equals(SuppressionDetector.ImagePath(owner),Environment.ProcessPath,StringComparison.OrdinalIgnoreCase);}catch(Exception){return false;}}
  // No config load, tray, audio, input receiver, UAC or Office creation in this mode.
  long lastRequest=Environment.TickCount64;
  using var pipe=CreatePipe(args[index+1]); // Reuse one server instance; do not race old client handles.
  while(OwnerAlive()&&Environment.TickCount64-lastRequest<15000){

   using var deadline=new CancellationTokenSource(3000);
   try{
    pipe.WaitForConnectionAsync(deadline.Token).GetAwaiter().GetResult();
    if(!GetNamedPipeClientProcessId(pipe.SafePipeHandle,out uint peer)||peer!=(uint)owner)continue;
    var request=new byte[1];if(pipe.ReadAsync(request,deadline.Token).AsTask().GetAwaiter().GetResult()!=1||request[0]!=1)continue;
    lastRequest=Environment.TickCount64;
    var count=SlideShowDetector.ProbePowerPointAsync(deadline.Token).GetAwaiter().GetResult();
    var response=System.Text.Encoding.ASCII.GetBytes(SlideHelperProtocol.Encode(count)+"\n");
    pipe.WriteAsync(response,deadline.Token).AsTask().GetAwaiter().GetResult();
    var end=new byte[1];pipe.ReadAsync(end,deadline.Token).AsTask().GetAwaiter().GetResult(); // Wait for client EOF before disconnecting.
   }catch(OperationCanceledException){}
   catch(IOException){}
   finally{try{pipe.Disconnect();}catch(IOException){}catch(InvalidOperationException){}} // Broken pipes also need Disconnect before reuse.
  }
 }
 internal static async Task VerifySeriesAsync(){
  var lines=new List<string>();int? previous=null;int failures=0,changes=0;
  try{
   for(int i=0;i<15;i++){
    using var deadline=new CancellationTokenSource(2500);
    var count=await QueryAsync(deadline.Token);
    if(!count.HasValue)failures++;
    if(previous.HasValue&&count.HasValue&&previous!=count)changes++;
    if(count.HasValue)previous=count;
    lines.Add($"sample={i};count={SlideHelperProtocol.Encode(count)}");
    await Task.Delay(2000);
   }
  }finally{Stop();}
  lines.Add($"unknown={failures};stateChanges={changes}");
  Directory.CreateDirectory(Path.Combine(Store.Root,"audit"));
  File.WriteAllLines(Path.Combine(Store.Root,"audit","ppt-helper-series.txt"),lines);
 }
 internal static async Task VerifyFullSeriesAsync(){
  var lines=new List<string>();SlideShowDetector.TraceProbe=code=>lines.Add(code);
  try{for(int i=0;i<25;i++){
   using var timeout=new CancellationTokenSource(2000);string result;
   try{var state=await SlideShowDetector.EvaluateAsync(new(){Wps=false},timeout.Token);result=state.Active?"show":"inactive";}
   catch(OperationCanceledException){result="timeout";}
   lines.Add($"sample={i};state={result}");await Task.Delay(2000);
  }}finally{SlideShowDetector.TraceProbe=null;Stop();}
  Directory.CreateDirectory(Path.Combine(Store.Root,"audit"));File.WriteAllLines(Path.Combine(Store.Root,"audit","ppt-full-series.txt"),lines);
 }
 internal static async Task VerifyAsync(){
  int? count=null;
  try{using var deadline=new CancellationTokenSource(9000);while(!deadline.IsCancellationRequested){count=await QueryAsync(deadline.Token);if(count.HasValue)break;await Task.Delay(250,deadline.Token);}}
  catch(OperationCanceledException){}
  finally{Stop();}
  Directory.CreateDirectory(Path.Combine(Store.Root,"audit"));
  File.WriteAllText(Path.Combine(Store.Root,"audit","ppt-helper-verification.txt"),$"ordinaryReaderCount={SlideHelperProtocol.Encode(count)}; no presentation content read\n");
 }
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetNamedPipeServerProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe,out uint processId);
}

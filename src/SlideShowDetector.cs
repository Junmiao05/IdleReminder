// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
namespace IdleReminder;
internal sealed record SlideWindow(string ProcessName,string WindowClass,bool Visible){
 public override string ToString()=>$"{ProcessName} — {WindowClass}";
}
internal sealed record SlideShowResult(bool Active,bool WpsUnknown);
internal static class SlideShowDetector {
 delegate bool WindowCallback(IntPtr handle,IntPtr param);
 [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr param);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr handle,StringBuilder name,int size);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr handle);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr handle,out uint id);
 [DllImport("oleaut32.dll",PreserveSig=false)]static extern void GetActiveObject(ref Guid classId,IntPtr reserved,[MarshalAs(UnmanagedType.IUnknown)]out object instance);
 internal static Action<string>? TraceProbe;
 static int comBusy;
 static Dictionary<int,string> Processes(){
  var result=new Dictionary<int,string>();
  foreach(var name in new[]{"POWERPNT","wpp"}){
   var items=Process.GetProcessesByName(name);
   foreach(var p in items){using(p){try{result[p.Id]=name+".exe";}catch(InvalidOperationException){}}}
  }return result;
 }
 internal static List<SlideWindow> Windows(){
  var processes=Processes();var result=new List<SlideWindow>();
  if(processes.Count==0)return result;
  EnumWindows((handle,_)=>{
   GetWindowThreadProcessId(handle,out var id);
   if(processes.TryGetValue((int)id,out var process)){
    var name=new StringBuilder(257);GetClassName(handle,name,name.Capacity);
    if(name.Length>0&&IsWindowVisible(handle))result.Add(new(process,name.ToString(),true));
   }return true;
  },IntPtr.Zero);
  return result.Distinct().ToList();
 }
 static int? RunningShowCount(string[] progIds){
  foreach(var prog in progIds){
   object? app=null;object? windows=null;
   try{
    // Registry read + ROT lookup only. Never create or launch Office/WPS.
    using var key=Registry.ClassesRoot.OpenSubKey(prog+@"\CLSID");
    if(!Guid.TryParse(key?.GetValue(null) as string,out var id))continue;
    GetActiveObject(ref id,IntPtr.Zero,out var instance);app=instance;
    windows=app.GetType().InvokeMember("SlideShowWindows",System.Reflection.BindingFlags.GetProperty,null,app,null);
    if(windows==null)continue;
    var count=windows.GetType().InvokeMember("Count",System.Reflection.BindingFlags.GetProperty,null,windows,null);
    if(count is int n&&n>=0)return n;
   }catch(Exception e) when(e is COMException or System.Reflection.TargetInvocationException or MissingMethodException or UnauthorizedAccessException){}
   finally{if(windows!=null&&Marshal.IsComObject(windows))Marshal.ReleaseComObject(windows);if(app!=null&&Marshal.IsComObject(app))Marshal.ReleaseComObject(app);}
  }return null;
 }
 static async Task<(int? Ppt,int? Wps)> ProbeAsync(bool ppt,bool wps,CancellationToken token){
  if(Interlocked.CompareExchange(ref comBusy,1,0)!=0)return(null,null);
  var completion=new TaskCompletionSource<(int?,int?)>(TaskCreationOptions.RunContinuationsAsynchronously);
  var thread=new Thread(()=>{
   try{completion.TrySetResult((ppt?RunningShowCount(["PowerPoint.Application"]):null,wps?RunningShowCount(["KWPP.Application","WPP.Application"]):null));}
   catch(Exception e){Store.Log("slideshow-probe-failed",e);completion.TrySetResult((null,null));}
   finally{Interlocked.Exchange(ref comBusy,0);}
  }){IsBackground=true,Name="SlideShow read-only probe"};
  thread.SetApartmentState(ApartmentState.STA);thread.Start();
  try{return await completion.Task.WaitAsync(TimeSpan.FromMilliseconds(900),token);}
  catch(TimeoutException){return(null,null);}
 }
 internal static async Task<int?> ProbePowerPointAsync(CancellationToken token)=>(await ProbeAsync(true,false,token)).Ppt;
 internal static async Task<SlideShowResult> EvaluateAsync(SlideShowOptions options,CancellationToken token){
  if(!options.Enabled)return new(false,false);
  var windows=await Task.Run(Windows,token);TraceProbe?.Invoke("visibleWindows="+windows.Count);
  if(windows.Any(w=>SlideShowPolicy.IsShowWindow(w.ProcessName,w.WindowClass,w.Visible,options)))return new(true,false);
  bool ppt=options.PowerPoint&&windows.Any(w=>w.ProcessName.Equals("powerpnt.exe",StringComparison.OrdinalIgnoreCase));
  bool wps=options.Wps&&windows.Any(w=>w.ProcessName.Equals("wpp.exe",StringComparison.OrdinalIgnoreCase));
  if(!ppt&&!wps){TraceProbe?.Invoke("no-enabled-visible-Office");return new(false,false);}
  var counts=await ProbeAsync(ppt,wps,token);TraceProbe?.Invoke("direct="+SlideHelperProtocol.Encode(counts.Ppt));
  if(ppt&&counts.Ppt==null&&StartupElevation.IsAdministrator()){counts.Ppt=await SlideShowHelper.QueryAsync(token);TraceProbe?.Invoke("helper="+SlideHelperProtocol.Encode(counts.Ppt));}
  token.ThrowIfCancellationRequested();
  return new((ppt&&counts.Ppt>0)||(wps&&counts.Wps>0),wps&&counts.Wps==null);
 }
 internal static async Task VerifyAsync(){
  using var timeout=new CancellationTokenSource(3000);
  var result=await EvaluateAsync(new(),timeout.Token);
  File.WriteAllText(Path.Combine(Store.Root,"slideshow-verification.txt"),$"PASS readonly window/ROT probe completed; showDetected={result.Active}; wpsUnknown={result.WpsUnknown}; no presentation content read\n");
 }
}

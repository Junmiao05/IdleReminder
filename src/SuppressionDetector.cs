// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
using Windows.Media.Control;
namespace IdleReminder;
internal sealed record SuppressionResult(bool Active,bool MediaUnavailable=false);
internal static class SuppressionDetector {
 [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)]
 static extern IntPtr OpenProcess(uint access,bool inherit,int id);
 [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]
 static extern bool QueryFullProcessImageName(IntPtr process,uint flags,System.Text.StringBuilder path,ref uint size);
 [System.Runtime.InteropServices.DllImport("kernel32.dll")]
 static extern bool CloseHandle(IntPtr handle);
 internal static string? ImagePath(int id){
  // Query limited metadata only; no process memory/module enumeration.
  var handle=OpenProcess(0x1000,false,id);if(handle==IntPtr.Zero)return null;
  try{var path=new System.Text.StringBuilder(32768);uint size=32768;return QueryFullProcessImageName(handle,0,path,ref size)?path.ToString():null;}
  finally{CloseHandle(handle);}
 }
 internal static async Task VerifyAsync(){
  using var timeout=new CancellationTokenSource(5000);
  var reports=new List<string>();
  var self=new SuppressionRule{Application=Environment.ProcessPath!,Mode=SuppressionMode.WhileRunning};
  var result=await EvaluateAsync(new[]{self},timeout.Token);
  reports.Add(result.Active?"PASS running rule detects own exact executable path using limited metadata":"FAIL own executable detection");
  var absent=new SuppressionRule{Application="IdleReminder-absent-"+Guid.NewGuid().ToString("N")+".exe",Mode=SuppressionMode.WhileRunning};
  result=await EvaluateAsync(new[]{absent},timeout.Token);
  reports.Add(!result.Active?"PASS absent executable does not suppress reminders":"FAIL absent executable");
  File.WriteAllLines(Path.Combine(Store.Root,"suppression-verification.txt"),reports);
 }
 internal static async Task<SuppressionResult> EvaluateAsync(SuppressionRule[] rules,CancellationToken token){
  var running=await Task.Run(()=>{
   foreach(var rule in rules.Where(r=>r.Mode==SuppressionMode.WhileRunning)){
    token.ThrowIfCancellationRequested();
    var name=Path.GetFileNameWithoutExtension(rule.Application);
    if(string.IsNullOrWhiteSpace(name))continue;
    var processes=Process.GetProcessesByName(name);
    try{
     foreach(var process in processes){
      token.ThrowIfCancellationRequested();
      if(!Path.IsPathFullyQualified(rule.Application))return true;
      try{if(string.Equals(ImagePath(process.Id),rule.Application,StringComparison.OrdinalIgnoreCase))return true;}
      catch(Exception e) when(e is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException){/* inaccessible process: do not guess identity */}
     }
    }finally{foreach(var p in processes)p.Dispose();}
   }return false;
  },token);
  if(running)return new(true);
  if(!rules.Any(r=>r.Mode==SuppressionMode.WhilePlaying))return new(false);
  try{
   var manager=await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(token);
   var playing=new List<string>();
   foreach(var session in manager.GetSessions()){
    token.ThrowIfCancellationRequested();
    try{if(session.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)playing.Add(session.SourceAppUserModelId);}
    catch(Exception e){Store.Log("suppression-session-unavailable",e);}
   }
   return new(rules.Any(r=>r.Mode==SuppressionMode.WhilePlaying&&SuppressionPolicy.IsMatch(r,playing,Array.Empty<string>())));
  }catch(OperationCanceledException){throw;}
  catch(Exception){return new(false,true);}
 }
}


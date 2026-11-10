// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
using System.Security.Principal;
namespace IdleReminder;
internal static class StartupElevation {
 internal const string InstanceName=@"Local\IdleReminder-Phase1";
 internal static bool AnotherInstanceRunning(){
  try{if(!Mutex.TryOpenExisting(InstanceName,out var existing))return false;existing.Dispose();return true;}
  catch(UnauthorizedAccessException){return true;}
 }
 internal static bool TryStartElevated(string[] args,Func<bool>? isAdmin=null,Func<string[],bool>? start=null,Func<bool>? exists=null,string? gateName=null){
  try{
   var settings=Store.Load();
   var admin=isAdmin?.Invoke()??IsAdministrator();
   Store.Log(admin?"bootstrap-already-admin":"bootstrap-ordinary");
   if(!ElevationPolicy.NeedsRequest(settings.InputTiming,!Store.WriteBlocked,admin,args.Contains(ElevationPolicy.RequestedFlag)))return false;
   using var gate=new Mutex(false,gateName??@"Local\IdleReminder-ElevationRequest");
   bool owns=false;
   try{
    try{owns=gate.WaitOne(0);}catch(AbandonedMutexException){owns=true;}
    if(!owns){Store.Log("elevation-request-already-pending");return true;}
    if((exists??AnotherInstanceRunning)())return true;
    Store.Log("bootstrap-request-uac");
    return (start??StartChild)(ElevationPolicy.ChildArguments(args));
   }finally{if(owns)gate.ReleaseMutex();}
  }catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode==1223){Store.Log("elevation-request-cancelled");return false;}
  catch(Exception ex){Store.Log("elevation-request-failed",ex);return false;}
 }
 internal const string ParentFlag="--handoff-parent";
 internal static bool TrySwitch(InputTimingMode mode,Func<bool>? isAdmin=null,Func<string[],bool>? launch=null){
  try{
   if(!ElevationPolicy.NeedsSwitchRequest(mode,isAdmin?.Invoke()??IsAdministrator()))return false;
   return (launch??StartChild)(["--background",ElevationPolicy.RequestedFlag,ParentFlag,Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
  }catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode==1223){Store.Log("live-elevation-cancelled");return false;}
  catch(Exception ex){Store.Log("live-elevation-failed",ex);return false;}
 }
 internal static bool WaitForParent(string[] args){
  int index=Array.IndexOf(args,ParentFlag);if(index<0)return true;
  try{
   if(!IsAdministrator()||index+1>=args.Length||!int.TryParse(args[index+1],out int id)||id<=0||id==Environment.ProcessId)return false;
   Process parent;
   try{parent=Process.GetProcessById(id);}catch(ArgumentException){return true;}
   using(parent){
    if(parent.HasExited)return true;
    if(!string.Equals(parent.MainModule?.FileName,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase))return false;
    if(!parent.WaitForExit(30000)){Store.Log("live-elevation-parent-timeout");return false;}
    return true;
   }
  }catch(Exception ex){Store.Log("live-elevation-parent-wait-failed",ex);return false;}
 }
 internal static bool IsAdministrator(){
  using var identity=WindowsIdentity.GetCurrent();
  return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
 }
 static bool StartChild(string[] args){
  var exe=Environment.ProcessPath;
  if(string.IsNullOrEmpty(exe)||!File.Exists(exe)||Path.GetFileNameWithoutExtension(exe).Equals("dotnet",StringComparison.OrdinalIgnoreCase)){Store.Log("elevation-executable-unavailable");return false;}
  var info=new ProcessStartInfo(exe){UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(exe)!,WindowStyle=ProcessWindowStyle.Hidden};
  foreach(var arg in args)info.ArgumentList.Add(arg);
  using var child=Process.Start(info);
  if(child==null)return false;
  // Parent holds only the separate startup gate, never the application mutex.
  // Allow the child to acquire its mutex before releasing concurrent launches.
  try{child.WaitForInputIdle(5000);}catch(Exception ex) when(ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException){Store.Log("elevation-child-idle-unavailable",ex);}
  if(child.HasExited){Store.Log("elevation-child-exited");return false;}
  return true;
 }
}

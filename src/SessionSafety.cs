// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
internal sealed class SessionSafety:NativeWindow,IDisposable {
 IntPtr powerRegistration;bool sessionRegistered,disposed,warned;
 internal bool Locked {get;private set;}=true;
 internal bool Suspended {get;private set;}
 internal bool RegistrationHealthy=>sessionRegistered&&powerRegistration!=IntPtr.Zero;
 internal bool Blocked=>Locked||Suspended||!RegistrationHealthy;
 internal event Action? LockedChanged;
 internal event Action<PowerTransition>? PowerChanged;
 internal SessionSafety(){
  CreateHandle(new CreateParams{Caption="IdleReminder session safety"});
  sessionRegistered=WTSRegisterSessionNotification(Handle,0);
  if(!sessionRegistered)Store.Log("session-notification-register-failed");
  powerRegistration=RegisterSuspendResumeNotification(Handle,0);
  if(powerRegistration==IntPtr.Zero)Store.Log("suspend-notification-register-failed");
  Refresh();
 }
 internal void Refresh(){
  bool unlocked=false;IntPtr data=IntPtr.Zero;
  try{
   using var own=System.Diagnostics.Process.GetCurrentProcess();
   if(WTSQuerySessionInformation(IntPtr.Zero,own.SessionId,25,out data,out uint bytes)&&bytes>=20){
    // Read only the level, connection state and lock flag; never marshal names or session content.
    unlocked=SessionSafetyPolicy.IsUnlocked(Marshal.ReadInt32(data,0),Marshal.ReadInt32(data,12),Marshal.ReadInt32(data,16));
   }else if(!warned){warned=true;Store.Log("session-lock-query-unavailable");}
  }finally{if(data!=IntPtr.Zero)WTSFreeMemory(data);}
  SetLocked(!unlocked);
 }
 void SetLocked(bool value){if(value==Locked)return;Locked=value;Store.Log(value?"session-locked":"session-unlocked");LockedChanged?.Invoke();}
 protected override void WndProc(ref Message message){
  try{
   if(!disposed&&message.Msg==0x02b1){
    int reason=message.WParam.ToInt32();
    if(reason is 7 or 4 or 6)SetLocked(true);else Refresh();
   }
   if(!disposed&&message.Msg==0x0218){
    var transition=SessionSafetyPolicy.PowerEvent(message.WParam.ToInt32());
    if(transition!=PowerTransition.Other){Suspended=transition==PowerTransition.Suspend;Store.Log(Suspended?"native-power-suspend":"native-power-resume");PowerChanged?.Invoke(transition);message.Result=new IntPtr(1);return;}
   }
  }catch(Exception ex){SetLocked(true);Store.Log("session-safety-event-failed",ex);}
  base.WndProc(ref message);
 }
 public void Dispose(){if(disposed)return;disposed=true;if(powerRegistration!=IntPtr.Zero)UnregisterSuspendResumeNotification(powerRegistration);if(sessionRegistered)WTSUnRegisterSessionNotification(Handle);DestroyHandle();}
 [DllImport("wtsapi32.dll",CharSet=CharSet.Unicode,EntryPoint="WTSQuerySessionInformationW")]static extern bool WTSQuerySessionInformation(IntPtr server,int session,int info,out IntPtr data,out uint bytes);
 [DllImport("wtsapi32.dll")]static extern void WTSFreeMemory(IntPtr data);
 [DllImport("wtsapi32.dll")]static extern bool WTSRegisterSessionNotification(IntPtr window,uint flags);
 [DllImport("wtsapi32.dll")]static extern bool WTSUnRegisterSessionNotification(IntPtr window);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr RegisterSuspendResumeNotification(IntPtr window,uint flags);
 [DllImport("user32.dll")]static extern bool UnregisterSuspendResumeNotification(IntPtr registration);
}

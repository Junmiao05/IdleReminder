// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
internal sealed class InputActivitySource:IDisposable {
 HeaderWindow? receiver;InputTimingMode requested;uint generation;bool configured,fallback,requireDevice;readonly Func<InputAccessState> accessProbe;InputAccessState accessState;
 internal InputActivitySource(Func<InputAccessState>? accessProbe=null){this.accessProbe=accessProbe??new ForegroundInputPermission().Read;}
 internal bool AccessRestricted=>receiver!=null&&accessState!=InputAccessState.Allowed;
 internal InputTimingMode ActiveMode=>receiver?.Healthy==true&&accessState==InputAccessState.Allowed?InputTimingMode.DeviceHeaders:InputTimingMode.WindowsLastInput;
 internal string Status=>accessState==InputAccessState.HigherForeground?"前台权限较高，临时回退Windows计时":accessState==InputAccessState.Unavailable?"无法确认前台接收权限，临时回退Windows计时":fallback?"设备事件接收失败，已回退Windows计时":ActiveMode==InputTimingMode.DeviceHeaders?"设备键鼠事件计时（实验）":"Windows输入计时";
 internal void Configure(InputTimingMode mode,bool requireDevice=false){
  if(configured&&requested==mode&&this.requireDevice==requireDevice)return;
  receiver?.Dispose();receiver=null;requested=mode;this.requireDevice=requireDevice;configured=true;fallback=false;accessState=InputAccessState.Allowed;unchecked{generation++;}
  if(mode!=InputTimingMode.DeviceHeaders)return;
  try{receiver=new HeaderWindow(requireDevice);receiver.Open();}
  catch(Exception ex){receiver?.Dispose();receiver=null;fallback=true;Store.Log("device-input-register-failed",ex);}
 }
 internal ActivitySample Read(){
  if(receiver is {Healthy:false}){receiver.Dispose();receiver=null;fallback=true;unchecked{generation++;}}
  var access=receiver!=null?accessProbe():InputAccessState.Allowed;
  if(access!=accessState){accessState=access;unchecked{generation++;}}
  ActivitySample value;
  if(receiver!=null&&accessState==InputAccessState.Allowed)value=receiver.Clock.Read(Environment.TickCount64);
  else{uint input=Native.LastInput();value=new(input,unchecked((uint)Environment.TickCount-input));}
  return InputTimingPolicy.WithGeneration(value,generation);
 }
 public void Dispose(){receiver?.Dispose();receiver=null;}
 [StructLayout(LayoutKind.Sequential)]struct Device {public ushort Page,Usage;public uint Flags;public IntPtr Target;}
 [StructLayout(LayoutKind.Sequential)]struct Header {public uint Type,Size;public IntPtr Device,Parameter;}
 [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterRawInputDevices(Device[] devices,uint count,uint size);
 [DllImport("user32.dll",SetLastError=true)]static extern uint GetRawInputData(IntPtr input,uint command,out Header data,ref uint size,uint headerSize);
 sealed class HeaderWindow:NativeWindow,IDisposable {
  readonly bool requireDevice;internal HeaderWindow(bool requireDevice){this.requireDevice=requireDevice;}
  internal readonly HeaderActivityClock Clock=new(Environment.TickCount64);
  internal bool Healthy=true;bool registered,disposed;
  internal void Open(){
   CreateHandle(new CreateParams{Parent=new IntPtr(-3),Caption="IdleReminder device event timing"});
   var devices=new[]{new Device{Page=1,Usage=2,Flags=0x100,Target=Handle},new Device{Page=1,Usage=6,Flags=0x100,Target=Handle}};
   if(!RegisterRawInputDevices(devices,2,(uint)Marshal.SizeOf<Device>()))throw new System.ComponentModel.Win32Exception();
   registered=true;
  }
  protected override void WndProc(ref Message m){
   try{
    if(m.Msg==0x00ff&&Healthy){
     uint length=(uint)Marshal.SizeOf<Header>();
     uint copied=GetRawInputData(m.LParam,0x10000005,out var header,ref length,(uint)Marshal.SizeOf<Header>()); // RID_HEADER: no key/mouse payload.
     if(copied!=Marshal.SizeOf<Header>()){Healthy=false;Store.Log("device-input-header-failed");}
     else if(DeviceHeaderPolicy.Accept(header.Type,header.Device!=IntPtr.Zero,requireDevice))Clock.Observe(header.Type,Environment.TickCount64);
    }
   }catch(Exception ex){Healthy=false;Store.Log("device-input-receive-failed",ex);}
   finally{base.WndProc(ref m);}
  }
  public void Dispose(){
   if(disposed)return;disposed=true;
   if(registered){
    var remove=new[]{new Device{Page=1,Usage=2,Flags=1},new Device{Page=1,Usage=6,Flags=1}};
    if(!RegisterRawInputDevices(remove,2,(uint)Marshal.SizeOf<Device>()))Store.Log("device-input-unregister-failed",new System.ComponentModel.Win32Exception());
    registered=false;
   }
   if(Handle!=IntPtr.Zero)DestroyHandle();
  }
 }
}



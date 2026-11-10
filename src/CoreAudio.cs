// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
[StructLayout(LayoutKind.Sequential)] internal struct PropertyKey {internal Guid Id;internal uint Index;internal PropertyKey(string id,uint index){Id=new Guid(id);Index=index;}}
[StructLayout(LayoutKind.Explicit,Size=24)] internal struct PropVariant {[FieldOffset(0)]internal ushort Type;[FieldOffset(8)]internal IntPtr Pointer;[FieldOffset(8)]internal uint Number;}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDeviceEnumerator {
 void EnumAudioEndpoints(int flow,uint state,out IDeviceCollection devices);
 void GetDefaultAudioEndpoint(int flow,int role,out IDevice device);
 void GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);
 void RegisterEndpointNotificationCallback(IntPtr callback);void UnregisterEndpointNotificationCallback(IntPtr callback);
}
[ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDeviceCollection {void GetCount(out uint count);void Item(uint index,out IDevice device);}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDevice {
 void Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object instance);
 void OpenPropertyStore(uint access,out IProperties store);
 void GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
 void GetState(out uint state);
}
[ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IProperties {void GetCount(out uint count);void GetAt(uint index,out PropertyKey key);void GetValue(ref PropertyKey key,out PropVariant value);void SetValue(ref PropertyKey key,ref PropVariant value);void Commit();}
[ComVisible(true),Guid("657804FA-D6AD-4496-8A60-352752AF4F89"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IVolumeCallback {[PreserveSig]int OnNotify(IntPtr notification);}
[ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IEndpointVolume {
 void RegisterControlChangeNotify(IVolumeCallback callback);void UnregisterControlChangeNotify(IVolumeCallback callback);
 void GetChannelCount(out uint count);void SetMasterVolumeLevel(float level,ref Guid context);void SetMasterVolumeLevelScalar(float level,ref Guid context);
 void GetMasterVolumeLevel(out float level);void GetMasterVolumeLevelScalar(out float level);
 void SetChannelVolumeLevel(uint channel,float level,ref Guid context);void SetChannelVolumeLevelScalar(uint channel,float level,ref Guid context);
 void GetChannelVolumeLevel(uint channel,out float level);void GetChannelVolumeLevelScalar(uint channel,out float level);
 void SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);void GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
 void GetVolumeStepInfo(out uint step,out uint count);void VolumeStepUp(ref Guid context);void VolumeStepDown(ref Guid context);void QueryHardwareSupport(out uint mask);void GetVolumeRange(out float min,out float max,out float increment);
}
internal sealed record OutputDevice(string Id,string Name,bool Speaker,string Reason) {
 public override string ToString()=>Name+(Speaker?"（扬声器）":"（禁止自动提升）");
}
internal static class CoreAudio {
 [DllImport("ole32.dll")]static extern int PropVariantClear(ref PropVariant value);
 internal static IDeviceEnumerator Enumerator()=>(IDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"))!)!;
 internal static void Release(object? value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
 static object? Property(IProperties store,string guid,uint index){
  var key=new PropertyKey(guid,index);store.GetValue(ref key,out var v);
  try{return v.Type==31?Marshal.PtrToStringUni(v.Pointer):v.Type==19?v.Number:null;}finally{PropVariantClear(ref v);}
 }
 internal static OutputDevice Describe(IDevice device){
  device.GetId(out var id);device.OpenPropertyStore(0,out var props);
  try {
   var name=Property(props,"a45c254e-df1c-4efd-8020-67d146a850e0",14) as string??"未知输出设备";
   var factor=Property(props,"1da5d803-d492-4edd-8c23-e0c0ffee7f0e",0) as uint?;
   string? subtype=null;
   try{subtype=Property(props,"1da5d803-d492-4edd-8c23-e0c0ffee7f0e",8) as string;}catch(COMException){}
   bool speaker=VolumePolicy.IsSpeaker(factor,subtype,name);
   return new(id,name,speaker,speaker?"驱动报告为扬声器；仍需确认实际连接为扬声器":"耳机、混合接口或类型不明确，禁止提升");
  }finally{Release(props);}
 }
 internal static List<OutputDevice> List(){
  var e=Enumerator();IDeviceCollection? list=null;
  try{e.EnumAudioEndpoints(0,1,out list);list.GetCount(out var count);var result=new List<OutputDevice>();
   for(uint i=0;i<count;i++){list.Item(i,out var device);try{result.Add(Describe(device));}catch(Exception ex){Store.Log("device-description-failed",ex);}finally{Release(device);}}
   return result;
  }finally{Release(list);Release(e);}
 }
 internal static string DefaultId(){var e=Enumerator();IDevice? device=null;try{e.GetDefaultAudioEndpoint(0,1,out device);device.GetId(out var id);return id;}finally{Release(device);Release(e);}}
 internal static (IDevice Device,IEndpointVolume Volume) Open(string id){
  var e=Enumerator();IDevice? d=null;try{e.GetDevice(id,out d);var iid=typeof(IEndpointVolume).GUID;d.Activate(ref iid,23,IntPtr.Zero,out var v);return(d,(IEndpointVolume)v);}catch{Release(d);throw;}finally{Release(e);}
 }
}




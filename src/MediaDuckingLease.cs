// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
using System.Text;
namespace IdleReminder;
[ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISimpleAudioVolume {
 void SetMasterVolume(float volume,ref Guid context);void GetMasterVolume(out float volume);
 void SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);void GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
}
[ComVisible(true),Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEvents {
 [PreserveSig]int OnDisplayNameChanged(IntPtr name,IntPtr context);
 [PreserveSig]int OnIconPathChanged(IntPtr path,IntPtr context);
 [PreserveSig]int OnSimpleVolumeChanged(float volume,[MarshalAs(UnmanagedType.Bool)]bool mute,IntPtr context);
 [PreserveSig]int OnChannelVolumeChanged(uint count,IntPtr volumes,uint channel,IntPtr context);
 [PreserveSig]int OnGroupingParamChanged(IntPtr grouping,IntPtr context);
 [PreserveSig]int OnStateChanged(int state);
 [PreserveSig]int OnSessionDisconnected(int reason);
}
[ComVisible(true),ClassInterface(ClassInterfaceType.None)]
internal sealed class DuckVolume:IDuckVolume,IAudioSessionEvents {
 readonly object owner;readonly IAudioSessionControl2 control;readonly ISimpleAudioVolume volume;
 Guid context=Guid.NewGuid();bool registered,disposed;
 public Guid Context=>context;
 public event Action<Guid>? Changed;
 internal DuckVolume(object owner){
  this.owner=owner;control=(IAudioSessionControl2)owner;volume=(ISimpleAudioVolume)owner;
  control.RegisterAudioSessionNotification(this);registered=true;
 }
 public float Volume {get{volume.GetMasterVolume(out float value);return value;}set=>volume.SetMasterVolume(value,ref context);}
 public bool Muted {get{volume.GetMute(out bool value);return value;}}
 public int OnSimpleVolumeChanged(float value,bool mute,IntPtr source){Changed?.Invoke(source==IntPtr.Zero?Guid.Empty:Marshal.PtrToStructure<Guid>(source));return 0;}
 public int OnDisplayNameChanged(IntPtr a,IntPtr b)=>0;
 public int OnIconPathChanged(IntPtr a,IntPtr b)=>0;
 public int OnChannelVolumeChanged(uint a,IntPtr b,uint c,IntPtr d)=>0;
 public int OnGroupingParamChanged(IntPtr a,IntPtr b)=>0;
 public int OnStateChanged(int state)=>0;
 public int OnSessionDisconnected(int reason)=>0;
 public void Dispose(){if(disposed)return;disposed=true;try{if(registered)control.UnregisterAudioSessionNotification(this);}finally{CoreAudio.Release(owner);}}
}
internal sealed class MediaDuckingLease:IDisposable {
 readonly Dictionary<DuckingAdjustment,string> deviceIds=new();
 readonly List<DuckingAdjustment> adjustments=new();readonly DuckedSessionRegistry sessionsById=new();readonly Action<string>? report;
 bool gradualRestore,disposing,finished;
 internal void RequestGradualRestore()=>gradualRestore=true;
 internal bool IsVerified=>sessionsById.IsVerified;
 internal bool Allows(IAudioSessionControl2 session){session.GetSessionInstanceIdentifier(out var id);return sessionsById.Allows(id);}
  internal void PreserveCurrent(){foreach(var adjustment in adjustments)adjustment.PreserveCurrent();}
 internal void StageForMasterBoost(string device){foreach(var pair in deviceIds.Where(x=>x.Value==device))pair.Key.StageForMasterBoost();}
 internal void CompensateMasterGain(string device,float gainDb){foreach(var pair in deviceIds.Where(x=>x.Value==device))pair.Key.CompensateMasterGain(gainDb);report?.Invoke($"已补偿主音量增益 {gainDb:F1} dB，进一步压低同设备音乐");}
 internal void CancelMasterBoost(string device){foreach(var pair in deviceIds.Where(x=>x.Value==device)){try{pair.Key.CancelMasterBoost();}catch(Exception e){Store.Log("duck-compensation-cancel-failed",e);}}}
 MediaDuckingLease(Action<string>? report){this.report=report;}
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,uint process);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder name,ref uint size);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
 static string? Executable(uint process){
  var handle=OpenProcess(0x1000,false,process);if(handle==IntPtr.Zero)return null;
  try{var text=new StringBuilder(32768);uint size=(uint)text.Capacity;return QueryFullProcessImageName(handle,0,text,ref size)?text.ToString():null;}
  finally{CloseHandle(handle);}
 }
 internal static MediaDuckingLease? Begin(Settings settings,Action<string>? report)=>Begin(settings,report,out _);
 internal static MediaDuckingLease? Begin(Settings settings,Action<string>? report,out bool failed){
  failed=false;
  if(!settings.DuckMediaOnReminder)return null;
  var lease=new MediaDuckingLease(report);var enumerator=CoreAudio.Enumerator();IDeviceCollection? devices=null;
  try{
   // Ducking has its own explicit executable list, independent from pause exclusions.
   var entries=(settings.DuckApplications??settings.MediaApplications).Where(x=>x.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)).ToArray();
   if(entries.Length==0){report?.Invoke("未压低媒体：请在压低音量名单中添加播放器 .exe");return null;}
   enumerator.EnumAudioEndpoints(0,1,out devices);devices.GetCount(out uint count);
   for(uint i=0;i<count;i++){
    devices.Item(i,out var device);device.GetId(out var endpointId);object? manager=null;IAudioSessionEnumerator? sessions=null;
    try{
     var iid=typeof(IAudioSessionManager2).GUID;device.Activate(ref iid,23,IntPtr.Zero,out manager);
     ((IAudioSessionManager2)manager).GetSessionEnumerator(out sessions);sessions.GetCount(out int sessionCount);
     for(int j=0;j<sessionCount;j++){
      sessions.GetSession(j,out var value);bool owned=false;
      try{
       var control=(IAudioSessionControl2)value;control.GetState(out int state);
       if(state!=1||control.GetProcessId(out uint process)!=0||process==0||process==(uint)Environment.ProcessId)continue;
       var path=Executable(process);if(path==null||!DuckingPolicy.Matches(path,entries))continue;

       control.GetSessionInstanceIdentifier(out var instance);
       if(string.IsNullOrWhiteSpace(instance)||lease.sessionsById.Contains(instance))continue;
       var volume=new DuckVolume(value);owned=true;
       var adjustment=new DuckingAdjustment(volume,settings.DuckRemainingPercent);
       lease.adjustments.Add(adjustment);lease.deviceIds.Add(adjustment,endpointId);lease.sessionsById.Add(instance,adjustment);
       if(adjustment.Reduced)report?.Invoke($"指定播放器应用音量 {adjustment.Original*100:F1}% → {adjustment.Applied*100:F1}%（临时）");
      }catch(Exception e){failed=true;Store.Log("media-duck-session-failed",e);}
      finally{if(!owned)CoreAudio.Release(value);}
     }
    }catch(Exception e){failed=true;Store.Log("media-duck-device-failed",e);}
    finally{CoreAudio.Release(sessions);CoreAudio.Release(manager);CoreAudio.Release(device);}
   }
   int reduced=lease.adjustments.Count(x=>x.Reduced);
   report?.Invoke(reduced>0?$"已临时压低 {reduced} 个指定播放器音频会话，保留原应用音量的 {settings.DuckRemainingPercent}%":"无需压低音乐：名单内没有需要衰减的活动音频；主音量按设备授权判断");
   if(reduced==0){lease.Dispose();return null;}
   return lease;
  }catch{lease.Dispose();throw;}
  finally{CoreAudio.Release(devices);CoreAudio.Release(enumerator);}
 }
 public void Dispose(){
  if(disposing)return;disposing=true;
  if(gradualRestore&&adjustments.Any(x=>x.Reduced)){
   report?.Invoke("主音量保留手动设置，音乐将在约2秒内渐渐恢复；最终仍按当前主音量播放");
   MusicRestoreScheduler.Begin(progress=>{foreach(var adjustment in adjustments){try{adjustment.RestoreGradually(progress);}catch(Exception e){adjustment.PreserveCurrent();Store.Log("music-restore-step-failed",e);}}},FinishDispose);
  }else FinishDispose();
 }
 void FinishDispose(){
  if(finished)return;finished=true;
  int restored=0,preserved=0;
  foreach(var adjustment in adjustments){try{adjustment.Dispose();if(adjustment.Restored)restored++;if(adjustment.Preserved)preserved++;}catch(Exception e){Store.Log("media-duck-restore-failed",e);report?.Invoke("部分播放器音量恢复失败，请检查 Windows 音量混合器");}}
  adjustments.Clear();sessionsById.Clear();deviceIds.Clear();
  if(restored>0||preserved>0)report?.Invoke($"播放器音量处理结束：已恢复 {restored} 个会话，保留手动调整 {preserved} 个会话");
 }
}

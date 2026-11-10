// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
[ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionManager2 {
 void GetAudioSessionControl(ref Guid id,uint flags,out IntPtr control);
 void GetSimpleAudioVolume(ref Guid id,uint flags,out IntPtr volume);
 void GetSessionEnumerator(out IAudioSessionEnumerator sessions);
}
[ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEnumerator {
 void GetCount(out int count);
 void GetSession(int index,[MarshalAs(UnmanagedType.IUnknown)]out object session);
}
[ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl2 {
 void GetState(out int state);
 void GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
 void SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,ref Guid context);
 void GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
 void SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,ref Guid context);
 void GetGroupingParam(out Guid grouping);void SetGroupingParam(ref Guid grouping,ref Guid context);
 void RegisterAudioSessionNotification(IAudioSessionEvents notification);void UnregisterAudioSessionNotification(IAudioSessionEvents notification);
 void GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
 void GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetProcessId(out uint process);
}
internal readonly record struct AudioSafetyResult(bool Known,bool OtherActive) {
 internal bool AllowsBoost=>AudioSessionPolicy.CanBoost(Known,OtherActive);
 internal string Reason=>Known?"仍有其他程序的活动音频，保持原主音量":"无法确认其他程序的音频状态，保持原主音量";
}
internal static class AudioSessionSafety {
 // Read only session state and ownership, never media titles, paths or content.
 internal static AudioSafetyResult Inspect(IDevice device,MediaDuckingLease? ducking=null,Action<string>? diagnostic=null,bool allowOtherAudioAfterDucking=false,bool explicitVolumeConsent=false){
  object? activated=null;IAudioSessionEnumerator? sessions=null;
  try{
   if(ducking!=null&&!ducking.IsVerified)return new(false,true);
   var iid=typeof(IAudioSessionManager2).GUID;
   device.Activate(ref iid,23,IntPtr.Zero,out activated);
   ((IAudioSessionManager2)activated).GetSessionEnumerator(out sessions);
   sessions.GetCount(out int count);bool other=false;
   for(int i=0;i<count;i++){
    sessions.GetSession(i,out var value);
    try{
     var control=(IAudioSessionControl2)value;control.GetState(out int state);
     if(state!=1)continue;
     int result=control.GetProcessId(out uint process);
     if(result<0)Marshal.ThrowExceptionForHR(result);
     bool isOther=AudioSessionPolicy.HasOtherActive(new[]{new AudioSessionState(state,process,result)},(uint)Environment.ProcessId);
     bool allowed=result==0&&ducking?.Allows(control)==true;
     if(diagnostic!=null){var simple=(ISimpleAudioVolume)value;simple.GetMasterVolume(out var level);simple.GetMute(out var mute);diagnostic($"Active session: pid={process}; singleProcess={result==0}; own={!isOther}; verifiedDuck={allowed}; volume={level}; mute={mute}");}
     if(isOther)other|=!allowed&&!explicitVolumeConsent&&!DuckingPolicy.MayIgnoreOtherAudio(allowOtherAudioAfterDucking,ducking?.IsVerified==true,result>=0);
    }finally{CoreAudio.Release(value);}
   }
   return new(true,other);
  }catch(Exception e){Store.Log("audio-session-safety-unavailable",e);return new(false,true);}
  finally{CoreAudio.Release(sessions);CoreAudio.Release(activated);}
 }
}

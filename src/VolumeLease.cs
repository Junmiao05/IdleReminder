// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
[ComVisible(true),ClassInterface(ClassInterfaceType.None)]
internal sealed class VolumeLease : IVolumeCallback,IDisposable {
 readonly IDevice device;readonly IEndpointVolume volume;readonly string id;
 readonly System.Windows.Forms.Timer guard=new(){Interval=250};
 readonly MediaDuckingLease? ducking;readonly bool allowOtherAudio;readonly bool explicitVolumeConsent;
 readonly Action stop;readonly Action<string>? report;Guid context=Guid.NewGuid();float original,applied;
 int externalChange;bool changed,registered,disposed;
 internal static bool HasDeviceConsent(Settings settings)=>settings.DeviceVolumes.TryGetValue(CoreAudio.DefaultId(),out var config)&&DuckingPolicy.HasDeviceAuthorization(VolumePolicy.HasConfirmedConsent(config),settings.DuckMediaOnReminder,settings.AllowBoostWithOtherAudioAfterDucking);
 internal static VolumeLease? Begin(Settings settings,Action stop,Action<string>? report=null,MediaDuckingLease? ducking=null){
  var id=CoreAudio.DefaultId();
  if(!settings.DeviceVolumes.TryGetValue(id,out var config)){report?.Invoke(UiText.Get("VolumeLease_001"));return null;}
  return Create(id,config,stop,report,ducking,settings.AllowBoostWithOtherAudioAfterDucking,explicitVolumeConsent:DuckingPolicy.HasDeviceAuthorization(VolumePolicy.HasConfirmedConsent(config),settings.DuckMediaOnReminder,settings.AllowBoostWithOtherAudioAfterDucking));
 }
 internal static VolumeLease? Create(string id,DeviceVolumeSetting config,Action stop,Action<string>? report=null,MediaDuckingLease? ducking=null,bool allowOtherAudioAfterDucking=false,bool explicitVolumeConsent=false){
  if(CoreAudio.DefaultId()!=id){report?.Invoke(UiText.Get("VolumeLease_002"));return null;}
  var opened=CoreAudio.Open(id);
  var lease=new VolumeLease(id,opened.Device,opened.Volume,stop,report,ducking,allowOtherAudioAfterDucking,explicitVolumeConsent);bool staged=false;
  try{
   var description=CoreAudio.Describe(opened.Device);
   opened.Volume.GetMasterVolumeLevelScalar(out lease.original);opened.Volume.GetMute(out var mute);
   if(!VolumePolicy.CanRaise(description.Speaker,mute,lease.original,config)){report?.Invoke(UiText.Get("VolumeLease_003")+VolumePolicy.BlockReason(description.Speaker,mute,lease.original,config));lease.Dispose();return null;}
   var safety=AudioSessionSafety.Inspect(opened.Device,ducking,allowOtherAudioAfterDucking:allowOtherAudioAfterDucking,explicitVolumeConsent:explicitVolumeConsent);
   if(!safety.AllowsBoost){report?.Invoke(UiText.Get("VolumeLease_003")+safety.Reason);lease.Dispose();return null;}
   opened.Volume.RegisterControlChangeNotify(lease);lease.registered=true;
   // Recheck device and volume after registration.
   if(CoreAudio.DefaultId()!=id){lease.Dispose();return null;}
   opened.Volume.GetMasterVolumeLevelScalar(out lease.original);
   if(!VolumePolicy.CanRaise(description.Speaker,mute,lease.original,config)||Volatile.Read(ref lease.externalChange)!=0){lease.Dispose();return null;}
   safety=AudioSessionSafety.Inspect(opened.Device,ducking,allowOtherAudioAfterDucking:allowOtherAudioAfterDucking,explicitVolumeConsent:explicitVolumeConsent);
   if(!safety.AllowsBoost){report?.Invoke(UiText.Get("VolumeLease_003")+safety.Reason);lease.Dispose();return null;}
   opened.Volume.GetMasterVolumeLevel(out float originalDb);
   if(!float.IsFinite(originalDb))throw new InvalidOperationException("Invalid endpoint dB level");
   if(ducking!=null){staged=true;ducking.StageForMasterBoost(id);}
   lease.applied=config.TargetPercent/100f;lease.changed=true;
   opened.Volume.SetMasterVolumeLevelScalar(lease.applied,ref lease.context);
   opened.Volume.GetMasterVolumeLevelScalar(out lease.applied);
   opened.Volume.GetMasterVolumeLevel(out float appliedDb);
   if(ducking!=null)ducking.CompensateMasterGain(id,appliedDb-originalDb);
   lease.guard.Tick+=(_,_)=>lease.CheckRoute();
   lease.guard.Start();report?.Invoke(UiText.Format("VolumeLease_004",lease.applied*100));return lease;
  }catch{lease.Dispose();if(staged)ducking?.CancelMasterBoost(id);throw;}
 }
 VolumeLease(string id,IDevice device,IEndpointVolume volume,Action stop,Action<string>? report,MediaDuckingLease? ducking,bool allowOtherAudio,bool explicitVolumeConsent){this.explicitVolumeConsent=explicitVolumeConsent;this.allowOtherAudio=allowOtherAudio;this.ducking=ducking;this.id=id;this.device=device;this.volume=volume;this.stop=stop;this.report=report;}
 public int OnNotify(IntPtr data){
  // Only our unique event context belongs to this lease. Any other writer wins.
  if(Marshal.PtrToStructure<Guid>(data)!=context)Interlocked.Exchange(ref externalChange,1);
  return 0;
 }
 void CheckRoute(){try{if(CoreAudio.DefaultId()!=id){stop();return;}device.GetState(out var state);if(state!=1||!CoreAudio.Describe(device).Speaker){stop();return;}var safety=AudioSessionSafety.Inspect(device,ducking,allowOtherAudioAfterDucking:allowOtherAudio,explicitVolumeConsent:explicitVolumeConsent);if(!safety.AllowsBoost){stop();report?.Invoke(UiText.Get("VolumeLease_005")+safety.Reason);}}catch(Exception ex){Store.Log("volume-route-failed",ex);stop();}}
 public void Dispose(){
  if(disposed)return;disposed=true;guard.Stop();guard.Dispose();
  try{
   if(changed){volume.GetMasterVolumeLevelScalar(out var current);
    if(VolumePolicy.CanRestore(Volatile.Read(ref externalChange)!=0,current,applied))
     {volume.SetMasterVolumeLevelScalar(original,ref context);report?.Invoke(UiText.Format("VolumeLease_006",original*100));}
    else {if(current>original+.002f)ducking?.RequestGradualRestore();report?.Invoke(UiText.Get("VolumeLease_007"));}
   }
  }catch(Exception ex){Store.Log("volume-restore-failed",ex);ducking?.PreserveCurrent();report?.Invoke(UiText.Get("VolumeLease_008"));}
  finally{
   if(registered){try{volume.UnregisterControlChangeNotify(this);}catch(Exception ex){Store.Log("volume-unregister-failed",ex);}}
   CoreAudio.Release(volume);CoreAudio.Release(device);
  }
 }
}

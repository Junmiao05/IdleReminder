// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public sealed class DeviceVolumeSetting {
 public string Name {get;set;}="";
 public int TargetPercent {get;set;}=35;
 public bool Enabled {get;set;}
 public int? ConfirmedTarget {get;set;}
 public DeviceVolumeSetting Copy()=>new(){Name=Name,TargetPercent=TargetPercent,Enabled=Enabled,ConfirmedTarget=ConfirmedTarget};
}
public static class VolumePolicy {
 public static bool IsSpeaker(uint? form,string? subtype,string name){
  bool ambiguous=new[]{"headphone","headset","耳机","耳麦","/","line out","线路","hands-free"}.Any(x=>name.Contains(x,StringComparison.OrdinalIgnoreCase));
  return form==1&&!ambiguous&&Guid.TryParse(subtype,out var id)&&id==new Guid("DFF21CE1-F70F-11D0-B917-00A0C9223196");
 }
 public static bool SkipBoostForMedia(bool pauseRequested,bool resultKnown,int protectedPlaying=0,int failed=0,int unsupportedPlaying=0)
  => pauseRequested&&(!resultKnown||protectedPlaying>0||failed>0||unsupportedPlaying>0);
 public static string? BlockReason(bool speaker,bool muted,float current,DeviceVolumeSetting? config){
  if(!speaker)return UiText.Get("VolumePolicy_001");
  if(config==null)return UiText.Get("VolumePolicy_002");
  if(!config.Enabled)return UiText.Get("VolumePolicy_003");
  if(config.ConfirmedTarget!=config.TargetPercent)return UiText.Get("VolumePolicy_004");
  if(muted)return UiText.Get("VolumePolicy_005");
  if(current+0.001f>=config.TargetPercent/100f)return UiText.Format("VolumePolicy_006",current*100,config.TargetPercent);
  return null;
 }
 public static bool CanRaise(bool speaker,bool muted,float current,DeviceVolumeSetting? config)
  => speaker&&!muted&&config is {Enabled:true}&&config.TargetPercent>=1&&config.TargetPercent<=100&&config.ConfirmedTarget==config.TargetPercent&&current+0.001f<config.TargetPercent/100f;
 public static bool HasConfirmedConsent(DeviceVolumeSetting? config)=>config is {Enabled:true}&&config.TargetPercent>=1&&config.TargetPercent<=100&&config.ConfirmedTarget==config.TargetPercent;
 public static bool CanConfirmPreview(bool completed,bool verified,bool sameDevice,bool sameTarget)=>completed&&verified&&sameDevice&&sameTarget;
 public static bool CanRestore(bool externalChange,float current,float applied)=>!externalChange&&Math.Abs(current-applied)<0.002f;
}

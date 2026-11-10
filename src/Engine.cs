// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public sealed class Settings {
 public int ConfigurationVersion {get;set;}=1;
 // Null is an older existing configuration; false is a fresh configuration awaiting setup.
 public bool? TimingSetupAcknowledged {get;set;}
 [System.Text.Json.Serialization.JsonExtensionData]
 public Dictionary<string,System.Text.Json.JsonElement>? AdditionalFields {get;set;}
 public Settings Copy()=>System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(this))!;
 public void CopyFrom(Settings source){var copy=source.Copy();foreach(var property in typeof(Settings).GetProperties().Where(x=>x.CanRead&&x.CanWrite))property.SetValue(this,property.GetValue(copy));}
 public bool RequireIdentifiedDeviceInput {get;set;}
 public InputTimingMode InputTiming {get;set;}=InputTimingMode.WindowsLastInput;
 public InputModeNoticeOptions InputModeNotices {get;set;}=new(){Applications=new()};
 public InputModeNoticeOptions PenInputModeNotices {get;set;}=new(){Applications=new()};
 public PhoneOptions Phone {get;set;}=new();
 public string SkinPreset {get;set;}="";
 public CheckBoxPalette CheckBoxColors {get;set;}=new();
 public Dictionary<string,DeviceVolumeSetting> DeviceVolumes {get;set;}=new();
 public SlideShowOptions SlideShow {get;set;}=new();
 public bool AppSuppressionEnabled {get;set;}=true;
 public List<SuppressionRule> SuppressionRules {get;set;}=new();
 public MediaPauseScope MediaScope {get;set;}=MediaPauseScope.All;
 public List<string> MediaApplications {get;set;}=new();
 // Null means an older config; an explicit empty list must stay empty.
 public List<string>? DuckApplications {get;set;}
 public bool AllowBoostWithOtherAudioAfterDucking {get;set;}
 public bool DuckMediaOnReminder {get;set;}
 public int DuckRemainingPercent {get;set;}=20;
 public bool PauseMediaOnReminder {get;set;}=true;
 public int IdleMinutes {get;set;}=20;
 public bool Repeat {get;set;}=true;
 public int RepeatMinutes {get;set;}=10;
 public int Volume {get;set;}=60;
 public int Preset {get;set;}
 public string? Tone {get;set;}
 public string? Voice {get;set;}
 public int VoicePreset {get;set;}=-1;
 public int Mode {get;set;}
 public bool Enabled {get;set;}=true;
 public DateTimeOffset? PauseUntil {get;set;}
 public bool AskAgain {get;set;}=true;
 public int AskMinutes {get;set;}=30;
 public DateTimeOffset? AskAt {get;set;}
 // Launch starts a new reminder session; pauses apply only to the current run.
 public void PrepareForLaunch() {
  Enabled=true;
  PauseUntil=null;
  AskAt=null;
 }
 public bool HasVoice() => Voice!=null || (VoicePreset>=0&&VoicePreset<Presets.VoiceFileNames.Length);
 public IEnumerable<string> AudioFiles(string baseDirectory,int mode){
  if(mode!=1)yield return Tone??System.IO.Path.Combine(baseDirectory,"assets",Presets.FileNames[Preset]);
  if(mode!=0){
   if(Voice!=null)yield return Voice;
   else if(VoicePreset>=0&&VoicePreset<Presets.VoiceFileNames.Length)yield return System.IO.Path.Combine(baseDirectory,"assets",Presets.VoiceFileNames[VoicePreset]);
  }
 }
 public void Validate() {
 if(ConfigurationVersion>1||ConfigurationVersion<0)throw new InvalidDataException("不支持的配置格式版本");ConfigurationVersion=1;
 if(MediaApplications?.Any(x=>x==null)==true||DuckApplications?.Any(x=>x==null)==true||DeviceVolumes?.Values.Any(x=>x==null)==true)throw new InvalidDataException("配置名单或设备中含null项");
 if(!Enum.IsDefined(InputTiming))InputTiming=InputTimingMode.WindowsLastInput;
 Phone ??= new();Phone.Validate();
 InputModeNotices ??= new(){Applications=new()};InputModeNotices.Validate();
 PenInputModeNotices ??= new(){Applications=new()};PenInputModeNotices.Validate();
 CheckBoxColors ??= new();CheckBoxColors.Validate();
 SlideShow ??= new();SlideShow.WindowRules ??= new();
 SuppressionRules ??= new();
 if(!Enum.IsDefined(MediaScope))MediaScope=MediaPauseScope.All;
 MediaApplications ??= new();
 DuckApplications ??= MediaApplications.Where(x=>x.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)).ToList();
 DuckRemainingPercent=Math.Clamp(DuckRemainingPercent,1,100);
 DeviceVolumes ??= new();
 foreach(var config in DeviceVolumes.Values){config.TargetPercent=Math.Clamp(config.TargetPercent,1,100);if(config.ConfirmedTarget!=config.TargetPercent)config.Enabled=false;}
 // Migrate the six voices mistakenly stored as tone presets in the previous release.
 if(Preset>=5&&Preset<=10){
  if(Voice==null && VoicePreset<0)VoicePreset=Preset-5;
  Preset=0;
  if(Mode==0)Mode=1;
 }
 bool removedVoice=VoicePreset>=0;
 VoicePreset=Math.Clamp(VoicePreset,-1,Presets.VoiceNames.Length-1);
 if(removedVoice&&VoicePreset<0&&Voice==null&&Mode!=0)Mode=0; // Retired bundled voice: keep reminders audible with the selected tone.
 IdleMinutes=Math.Clamp(IdleMinutes,1,1440); RepeatMinutes=Math.Clamp(RepeatMinutes,1,1440);
 Volume=Math.Clamp(Volume,0,100); Preset=Math.Clamp(Preset,0,Presets.Count-1); Mode=Math.Clamp(Mode,0,2); AskMinutes=Math.Clamp(AskMinutes,1,1440);
 }
}
public enum PowerTransition { Other, Suspend, Resume }
public sealed class ReminderEngine {
 public bool ApplyPowerTransition(PowerTransition transition,long now){
  if(transition==PowerTransition.Suspend){Suspend(now);return true;}
  if(transition==PowerTransition.Resume){Resume(now);return true;}
  // Display/battery/AC changes do not cancel or restart idle reminders.
  return false;
 }
 public bool Suspended {get;private set;}
 public void Suspend(long now){Suspended=true;Reset(now,true);}
 public void Resume(long now){Suspended=false;Reset(now,true);}
 public long Baseline {get;private set;}
 public long? LastReminder {get;private set;}
 public bool WaitingInput {get;private set;}
 public void Reset(long now,bool wait=false) {Baseline=now;LastReminder=null;WaitingInput=wait;}
 public void CancelScheduledReminder(long scheduledAt,long? previous){if(LastReminder==scheduledAt)LastReminder=previous;}
 public bool Due(long now,long idle,bool changed,Settings s,bool suppressed=false) {
 if(Suspended)return false;
 if(changed){Reset(now);return false;}
 if(WaitingInput || !s.Enabled || suppressed)return false;
 if(Math.Min(idle,now-Baseline)<s.IdleMinutes*60000L)return false;
 if(LastReminder is long last && (!s.Repeat || now-last<s.RepeatMinutes*60000L))return false;
 LastReminder=now;return true;
 }
}





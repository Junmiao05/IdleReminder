// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Windows.Media;
namespace IdleReminder;
internal sealed class Audio : IDisposable {
 readonly MediaPlayer player=new();
 readonly Queue<string> queue=new();
 readonly System.Windows.Forms.Timer fade=new(){Interval=25};
 double targetVolume;long fadeStarted;bool fadePending;bool reminderNotified;
 internal double CurrentPlaybackGain=>player.Volume;
 internal double? FirstPlaybackGain {get;private set;}
 readonly System.Windows.Forms.Timer timeout=new(){Interval=120000};
 internal bool Playing {get;private set;}
 internal ulong StartInput {get;private set;}
 internal AudioPurpose Purpose {get;private set;}
 internal bool IsPreview => Playing && Purpose == AudioPurpose.Preview;
 internal void HandleInput(ulong input){if(PlaybackPolicy.ShouldStopForInput(Playing,Purpose,StartInput,input))Stop();}
 internal event Action? ReminderStarted;
 internal event Action<string>? Failed;
 internal event Action<bool>? Finished;
 internal Func<ulong> ReadInput {get;set;}=()=>Native.LastInput();
 internal Func<bool>? CanPlay {get;set;}
 internal event Action<string>? VolumeStatusChanged;
 internal string LastVolumeStatus {get;private set;}="尚未进行自动音量调整";
 internal void ReportVolume(string text){LastVolumeStatus=text;VolumeStatusChanged?.Invoke(text);}
 internal Settings? VolumeSettings {get;set;}
 IDisposable? volumeLease;MediaDuckingLease? duckLease;
 internal Audio(){
    player.MediaOpened+=(_,_)=>{
   if(!Playing)return;
   if(CanPlay?.Invoke()==false){Stop();return;}
   try{
    if(fadePending){fadePending=false;player.Volume=0;fadeStarted=Environment.TickCount64;fade.Start();}
    else player.Volume=fade.Enabled?FadeInPolicy.Gain(targetVolume,Environment.TickCount64-fadeStarted):targetVolume;
    FirstPlaybackGain??=player.Volume;
    // Apply gain after Open has initialized the player, before any playback.
    player.Play();
    if(Purpose==AudioPurpose.Reminder&&!reminderNotified){reminderNotified=true;ReminderStarted?.Invoke();}
   }catch(Exception e){Store.Log("audio-start-failed",e);Stop();Failed?.Invoke("音频无法开始播放。");}
  };
  fade.Tick+=(_,_)=>{try{var elapsed=Environment.TickCount64-fadeStarted;player.Volume=FadeInPolicy.Gain(targetVolume,elapsed);if(elapsed>=FadeInPolicy.DurationMilliseconds)fade.Stop();}catch(Exception e){Store.Log("audio-fade-failed",e);Stop();Failed?.Invoke("播报渐入失败，已停止并恢复音量。");}};
  player.MediaEnded+=(_,_)=>Next();player.MediaFailed+=(_,e)=>{Store.Log("audio-failed",e.ErrorException);Stop();Failed?.Invoke("音频无法播放，请更换文件。");};timeout.Tick+=(_,_)=>Stop();}
 internal void Play(IEnumerable<string> files,int volume,AudioPurpose purpose=AudioPurpose.Reminder,Func<Action,IDisposable?>? previewBoost=null,bool suppressAutoBoost=false){
  Stop();if(CanPlay?.Invoke()==false)return;
  Purpose=purpose;reminderNotified=false;FirstPlaybackGain=null;StartInput=ReadInput();targetVolume=Math.Clamp(volume/100.0,0,1);
  fadePending=purpose==AudioPurpose.Reminder;player.Volume=fadePending?0:targetVolume;
  foreach(var f in files)queue.Enqueue(f);Playing=true;bool duckFailed=false;
  try{
   if(purpose==AudioPurpose.Reminder&&VolumeSettings!=null)
    duckLease=MediaDuckingLease.Begin(VolumeSettings,ReportVolume,out duckFailed);
   // A verified duck permits a fresh session-by-session safety check; it never
   // exempts an entire process or blindly overrides the main-volume safeguards.
   bool deviceConsent=purpose==AudioPurpose.Reminder&&VolumeSettings!=null&&VolumeLease.HasDeviceConsent(VolumeSettings);
   bool mayBoost=DuckingPolicy.PermitAfterDucking(deviceConsent,DuckingPolicy.MayAttemptBoost(suppressAutoBoost,duckLease!=null,duckLease?.IsVerified==true),duckFailed);
   if(previewBoost!=null)volumeLease=previewBoost(()=>Stop());
   else if(purpose==AudioPurpose.Reminder&&mayBoost&&VolumeSettings!=null)
    volumeLease=VolumeLease.Begin(VolumeSettings,()=>Stop(),ReportVolume,duckLease);
   else if(purpose==AudioPurpose.Reminder)
    ReportVolume(duckFailed?"未提高音量：媒体压低或会话检查失败":"未提高音量：媒体暂停或压低状态尚未确认");
  }catch(Exception ex){
   Store.Log("volume-adjustment-failed",ex);ReportVolume("音量调整失败，详情见诊断日志");
   if(previewBoost!=null){Stop();Failed?.Invoke("自动音量试听未能执行：请检查静音状态、默认设备和设备类型。");return;}
  }
  Next();
 }
 void Next(){if(CanPlay?.Invoke()==false){Stop();return;}player.Close();if(queue.Count==0){Stop(true);return;}var p=queue.Dequeue();try{player.Open(new Uri(p));timeout.Stop();timeout.Start();}catch(Exception e){Store.Log("audio-open-failed",e);Stop();Failed?.Invoke("音频打开失败，请检查文件。");}}
 internal void Stop(bool completed=false){var wasPlaying=Playing;fade.Stop();fadePending=false;timeout.Stop();queue.Clear();Playing=false;try{player.Close();}finally{try{volumeLease?.Dispose();}finally{volumeLease=null;try{duckLease?.Dispose();}finally{duckLease=null;if(wasPlaying)Finished?.Invoke(completed);}}}}
 public void Dispose(){Stop();fade.Dispose();timeout.Dispose();}
}



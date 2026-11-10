// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using Microsoft.Win32;
namespace IdleReminder;
// Explicit diagnostic only: use saved settings and the real Audio reminder path.
internal static class ReminderVerification {
 internal static void Run(){
  var lines=new List<string>{"Explicit audible reminder test; uses saved settings without enabling or replacing options."};
  var settings=Store.Load();using var context=new ApplicationContext();using var activity=new InputActivitySource();activity.Configure(settings.InputTiming,settings.RequireIdentifiedDeviceInput);using var audio=new Audio{VolumeSettings=settings,ReadInput=()=>activity.Read().Stamp};
  using var dispatch=new Control();_=dispatch.Handle;
  using var start=new System.Windows.Forms.Timer{Interval=100};
  using var watch=new System.Windows.Forms.Timer{Interval=250};
  long started=Environment.TickCount64;bool ended=false,suspended=false;float low=1,high=0;int samples=0;
  var id=CoreAudio.DefaultId();
  float Master(){var opened=CoreAudio.Open(id);try{opened.Volume.GetMasterVolumeLevelScalar(out var level);return level;}finally{CoreAudio.Release(opened.Volume);CoreAudio.Release(opened.Device);}}
  var before=Master();lines.Add($"Before master={before*100:F1}%; pauseScope={settings.MediaScope}; duck={settings.DuckMediaOnReminder}; coordination={settings.AllowBoostWithOtherAudioAfterDucking}; duckEntries={settings.DuckApplications?.Count??0}");
  audio.VolumeStatusChanged+=text=>lines.Add(text);
  audio.Failed+=text=>lines.Add("FAIL "+text);
  audio.CanPlay=()=>!suspended&&Native.Accessible();
  audio.Finished+=completed=>{ended=true;lines.Add($"Real Audio.Finished: completed={completed}; playback start application gain={audio.FirstPlaybackGain*100:F1}%; master after={Master()*100:F1}%");MusicRestoreScheduler.WhenIdle(()=>context.ExitThread());};
  PowerModeChangedEventHandler power=(_,e)=>{if(e.Mode!=PowerModes.StatusChange){suspended=true;dispatch.BeginInvoke(()=>{audio.Stop();context.ExitThread();});}};
  SystemEvents.PowerModeChanged+=power;
  start.Tick+=async(_,_)=>{
   start.Stop();
   try{
    var input=activity.Read().Stamp;bool blocked=VolumePolicy.SkipBoostForMedia(settings.PauseMediaOnReminder,false);
    if(settings.PauseMediaOnReminder){
     using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(2));
     try{var pause=await MediaPause.PauseAllAsync(deadline.Token,()=>activity.Read().Stamp==input,settings.MediaScope,settings.MediaApplications);blocked=VolumePolicy.SkipBoostForMedia(true,true,pause.Protected,pause.Failed,pause.UnsupportedPlaying);lines.Add($"Pause results: paused={pause.Paused}; protected={pause.Protected}; failed={pause.Failed}");}
     catch(Exception e){lines.Add("Pause unavailable "+e.GetType().Name);}
    }
    if(suspended||!Native.Accessible()||activity.Read().Stamp!=input){lines.Add("CANCEL input/power/desktop changed before playback");context.ExitThread();return;}
    started=Environment.TickCount64;audio.Play(settings.AudioFiles(AppContext.BaseDirectory,settings.Mode),settings.Volume,suppressAutoBoost:blocked);
    if(!audio.Playing){context.ExitThread();return;}
    lines.Add($"After Audio.Play master={Master()*100:F1}%; reminder application gain={audio.CurrentPlaybackGain*100:F1}%");watch.Start();
   }catch(Exception e){lines.Add("FAIL start "+e.GetType().Name);audio.Stop();context.ExitThread();}
  };
  watch.Tick+=(_,_)=>{
   try{
    var level=Master();low=Math.Min(low,level);high=Math.Max(high,level);samples++;if(samples<=6)lines.Add($"Fade sample at {Environment.TickCount64-started}ms: application gain={audio.CurrentPlaybackGain*100:F1}%");
    audio.HandleInput(activity.Read().Stamp);
    if(Environment.TickCount64-started>30000){lines.Add("Timeout 30 seconds");audio.Stop();context.ExitThread();}
   }catch(Exception e){lines.Add("FAIL watch "+e.GetType().Name);audio.Stop();context.ExitThread();}
  };
  try{start.Start();Application.Run(context);}
  finally{
   start.Stop();watch.Stop();SystemEvents.PowerModeChanged-=power;audio.Stop();
   lines.Add($"Playback master samples={samples}; range={(samples>0?low*100:0):F1}..{(samples>0?high*100:0):F1}%; final master={Master()*100:F1}%; finishedEvent={ended}");
   File.WriteAllLines(Path.Combine(Store.Root,"reminder-volume-verification.txt"),lines);
  }
 }
}



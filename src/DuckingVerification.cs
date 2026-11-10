// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class DuckingVerification {
 internal static void Run(bool coordinated=false){
  var report=new List<string>{coordinated?"Authorized three-second coordinated application/master-volume test; uses existing confirmed speaker target; no playback commands.":"Authorized three-second application-volume test; no master-volume writes, no pause or play commands."};
  using var context=new ApplicationContext();using var timer=new System.Windows.Forms.Timer{Interval=3000};
  MediaDuckingLease? duck=null;VolumeLease? master=null;bool finished=false;
  string? deviceId=null;float? before=null;
  float ReadMaster(){var opened=CoreAudio.Open(deviceId!);try{opened.Volume.GetMasterVolumeLevelScalar(out var v);return v;}finally{CoreAudio.Release(opened.Volume);CoreAudio.Release(opened.Device);}}
  void Finish(){
   if(finished)return;finished=true;timer.Stop();
   // Restore main volume before releasing the music attenuation.
   try{master?.Dispose();master=null;}finally{try{duck?.Dispose();duck=null;}finally{context.ExitThread();}}
  }
  try{
   var settings=Store.Load();settings.DuckMediaOnReminder=true;
   if(coordinated)settings.AllowBoostWithOtherAudioAfterDucking=true;
   if(coordinated){deviceId=CoreAudio.DefaultId();before=ReadMaster();report.Add($"Master before: {before*100:F1}%");}
   duck=MediaDuckingLease.Begin(settings,text=>report.Add(text));
   if(coordinated){
    if(duck==null||!duck.IsVerified){report.Add("SKIP no verified active duck; no master volume change");return;}
    var diagnostic=CoreAudio.Open(deviceId!);try{AudioSessionSafety.Inspect(diagnostic.Device,duck,text=>report.Add(text),settings.AllowBoostWithOtherAudioAfterDucking);}finally{CoreAudio.Release(diagnostic.Volume);CoreAudio.Release(diagnostic.Device);}
    master=VolumeLease.Begin(settings,Finish,text=>report.Add(text),duck);
    report.Add($"Master during: {ReadMaster()*100:F1}%");
   }
   timer.Tick+=(_,_)=>Finish();timer.Start();Application.Run(context);
  }catch(Exception e){report.Add("FAIL "+e.GetType().Name+" "+e.HResult);}
  finally{
   try{Finish();if(coordinated&&deviceId!=null)report.Add($"Master after: {ReadMaster()*100:F1}%; before: {before*100:F1}%");}
   finally{File.WriteAllLines(Path.Combine(Store.Root,coordinated?"coordinated-volume-verification.txt":"ducking-verification.txt"),report);}
  }
 }
}

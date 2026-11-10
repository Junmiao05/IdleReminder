// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
// UI-thread-only jobs keep COM session ownership until the short restore finishes.
internal static class MusicRestoreScheduler {
 sealed class Job {internal readonly Action<double> Step;internal readonly Action Finish;internal long LastTick,Elapsed;internal Job(Action<double> step,Action finish,long started){Step=step;Finish=finish;LastTick=started;}}
 static readonly List<Job> jobs=new();static readonly List<Action> idle=new();
 static readonly System.Windows.Forms.Timer timer=new(){Interval=25};
 internal static bool Pending=>jobs.Count>0;
 static MusicRestoreScheduler(){timer.Tick+=(_,_)=>Tick();}
 internal static void Begin(Action<double> step,Action finish){jobs.Add(new(step,finish,Environment.TickCount64));timer.Start();}
 internal static void WhenIdle(Action action){if(!Pending)action();else idle.Add(action);}
 static void Tick(){
  foreach(var job in jobs.ToArray()){
   long now=Environment.TickCount64;job.Elapsed+=Math.Clamp(now-job.LastTick,0,100);job.LastTick=now;
   double p=Math.Clamp(job.Elapsed/2000.0,0,1);
   try{job.Step(p);}catch(Exception e){Store.Log("music-restore-fade-failed",e);p=1;}
   if(p>=1){jobs.Remove(job);try{job.Finish();}catch(Exception e){Store.Log("music-restore-finish-failed",e);}}
  }
  if(!Pending){timer.Stop();var actions=idle.ToArray();idle.Clear();foreach(var action in actions)action();}
 }
}

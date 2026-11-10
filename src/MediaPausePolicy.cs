// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public interface IPausableMedia {
 string AppId => "";
 bool IsPlaying {get;}
 bool CanPause {get;}
 Task<bool> PauseAsync(CancellationToken cancellationToken);
}
public sealed record MediaPauseResult(int Paused,int Skipped,int Failed,int Protected=0,int UnsupportedPlaying=0);
public static class MediaPausePolicy {
 public static async Task<MediaPauseResult> PauseAsync(IEnumerable<IPausableMedia> sessions,CancellationToken cancellationToken,Func<string,bool>? shouldPause=null){
  var tasks=sessions.Select(async session=>{
   cancellationToken.ThrowIfCancellationRequested();
   try{
    if(!session.IsPlaying)return (0,1,0,0,0);
    if(shouldPause!=null&&!shouldPause(session.AppId))return (0,0,0,1,0);
    if(!session.CanPause)return (0,1,0,0,1);
    return await session.PauseAsync(cancellationToken)?(1,0,0,0,0):(0,0,1,0,0);
   }catch(OperationCanceledException){throw;}
   catch{return (0,0,1,0,0);}
  }).ToArray();
  var outcomes=await Task.WhenAll(tasks);
  return new(outcomes.Sum(x=>x.Item1),outcomes.Sum(x=>x.Item2),outcomes.Sum(x=>x.Item3),outcomes.Sum(x=>x.Item4),outcomes.Sum(x=>x.Item5));
 }
}


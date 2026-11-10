// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using Windows.Media.Control;
namespace IdleReminder;
internal sealed class WindowsMediaSession(GlobalSystemMediaTransportControlsSession session):IPausableMedia {
 public string AppId=>session.SourceAppUserModelId;
 public bool IsPlaying=>session.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
 public bool CanPause=>session.GetPlaybackInfo().Controls.IsPauseEnabled;
 public async Task<bool> PauseAsync(CancellationToken token){
  // Recheck immediately before issuing the targeted pause command.
  token.ThrowIfCancellationRequested();
  if(!IsPlaying||!CanPause)return false;
  if(!await session.TryPauseAsync().AsTask(token))return false;
  // Do not raise device volume solely on an accepted request: confirm playback stopped.
  for(int attempt=0;attempt<4;attempt++){
   token.ThrowIfCancellationRequested();
   if(!IsPlaying)return true;
   await Task.Delay(75,token);
  }
  return !IsPlaying;
 }
}
internal static class MediaPause {
 internal static async Task<MediaPauseResult> PauseAllAsync(CancellationToken token,Func<bool> inputUnchanged,MediaPauseScope scope,IReadOnlyList<string> apps){
  var manager=await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(token);
  token.ThrowIfCancellationRequested();
  if(!inputUnchanged()||!Native.Accessible())throw new OperationCanceledException();
  return await MediaPausePolicy.PauseAsync(manager.GetSessions().Select(s=>(IPausableMedia)new WindowsMediaSession(s)).ToArray(),token,id=>MediaApplicationFilter.ShouldPause(id,scope,apps));
 }
 internal static async Task<string[]> ListApplicationsAsync(){
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));
  var manager=await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(timeout.Token);
  return manager.GetSessions().Select(x=>x.SourceAppUserModelId).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
 }
 internal static async Task VerifyReadOnlyAsync(){
  try{
   using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));
   var manager=await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(timeout.Token);
   var sessions=manager.GetSessions();
   int eligible=sessions.Count(s=>s.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing&&s.GetPlaybackInfo().Controls.IsPauseEnabled);
   File.WriteAllText(Path.Combine(Store.Root,"media-verification.txt"),$"PASS GSMTC access; sessions={sessions.Count}; playingAndPausable={eligible}; no pause commands issued\n");
  }catch(Exception e){Store.Log("media-readonly-check-failed",e);File.WriteAllText(Path.Combine(Store.Root,"media-verification.txt"),$"FAIL GSMTC {e.GetType().Name} {e.HResult}\n");}
 }
}






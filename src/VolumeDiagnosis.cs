// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class VolumeDiagnosis {
 internal static void Run(){
  var report=new List<string>();
  try{
   var settings=Store.Load();var id=CoreAudio.DefaultId();var opened=CoreAudio.Open(id);
   try{
    var d=CoreAudio.Describe(opened.Device);opened.Volume.GetMasterVolumeLevelScalar(out var current);opened.Volume.GetMute(out var muted);
    settings.DeviceVolumes.TryGetValue(id,out var config);
    report.Add($"当前 Windows 音量：{current*100:F0}%；静音：{muted}；严格扬声器识别：{d.Speaker}");
    report.Add($"当前设备有配置：{config!=null}；已启用：{config?.Enabled}；目标：{config?.TargetPercent}%；已确认：{config?.ConfirmedTarget}");
    report.Add("设备提升条件："+(VolumePolicy.BlockReason(d.Speaker,muted,current,config)??"满足；正式提醒仍需检查媒体暂停结果"));
    report.Add($"音乐压低已启用：{settings.DuckMediaOnReminder}；保留原应用音量：{settings.DuckRemainingPercent}%；允许其他声音随主音量提高：{settings.AllowBoostWithOtherAudioAfterDucking}；提醒器内部播放音量：{settings.Volume}%");
    report.Add($"媒体暂停开关：{settings.PauseMediaOnReminder}；范围：{settings.MediaScope}；提升还必须通过独立的活动音频检查");
        var safety=AudioSessionSafety.Inspect(opened.Device);
    report.Add($"Core Audio 检查成功：{safety.Known}；其他活动音频：{safety.OtherActive}；允许提升：{safety.AllowsBoost}");
    report.Add(Task.Run(async()=>{
     using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));
     var manager=await Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(timeout.Token);
     var sessions=manager.GetSessions();int playing=0,eligible=0,listed=0;
     foreach(var session in sessions){var info=session.GetPlaybackInfo();if(info.PlaybackStatus!=Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)continue;playing++;if(info.Controls.IsPauseEnabled)eligible++;if(MediaApplicationFilter.ShouldPause(session.SourceAppUserModelId,settings.MediaScope,settings.MediaApplications))listed++;}
     return $"GSMTC 会话：{sessions.Count}；播放中：{playing}；可暂停：{eligible}；符合当前暂停名单：{listed}";
    }).GetAwaiter().GetResult());
        object? manager=null;IAudioSessionEnumerator? sessions=null;
    try{
     var ownProcesses=System.Diagnostics.Process.GetProcessesByName("IdleReminder");
     var ids=new HashSet<uint>();foreach(var process in ownProcesses){using(process)ids.Add((uint)process.Id);}
     var iid=typeof(IAudioSessionManager2).GUID;opened.Device.Activate(ref iid,23,IntPtr.Zero,out manager);
     ((IAudioSessionManager2)manager).GetSessionEnumerator(out sessions);sessions.GetCount(out int count);int found=0;
     for(int i=0;i<count;i++){
      sessions.GetSession(i,out var value);
      try{
       var control=(IAudioSessionControl2)value;
       if(control.GetProcessId(out var pid)!=0||!ids.Contains(pid)||pid==(uint)Environment.ProcessId)continue;
       control.GetState(out var state);var simple=(ISimpleAudioVolume)value;simple.GetMasterVolume(out var level);simple.GetMute(out var mute);
       report.Add($"正在运行提醒器的 Windows 应用会话：状态={state}（0闲置/1活动/2失效）；音量={level*100:F1}%；静音={mute}");found++;
      }finally{CoreAudio.Release(value);}
     }
     if(found==0)report.Add("当前默认输出未发现运行中提醒器的音频会话；尚不能判断其 Windows 混合器音量。");
    }finally{CoreAudio.Release(sessions);CoreAudio.Release(manager);}
    report.Add("仅只读检查，未修改系统音量、未暂停媒体、未保存设置。");
   }finally{CoreAudio.Release(opened.Volume);CoreAudio.Release(opened.Device);}
  }catch(Exception e){report.Add("FAIL "+e.GetType().Name+" "+e.HResult);}
  File.WriteAllLines(Path.Combine(Store.Root,"volume-diagnosis.txt"),report);
 }
}

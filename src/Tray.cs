// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using Microsoft.Win32;
namespace IdleReminder;
internal sealed class Tray : ApplicationContext {
 readonly Control eventDispatch=new();
 volatile bool powerSuspended;
 readonly Settings s=Store.Load();
 readonly ReminderEngine engine=new();
 readonly Audio audio=new();
 readonly SessionSafety safety=new();
 readonly InputActivitySource activity=new();
 readonly PhoneNotifications phone=new();CancellationTokenSource? phoneSend;string phoneStatus="";
 readonly Icon trayArtwork=AppIcons.LoadTray();
 readonly NotifyIcon icon;
 readonly System.Windows.Forms.Timer timer=new(){Interval=1000};
 readonly System.Windows.Forms.Timer phoneStartupTip=new(){Interval=10000};
 SettingsForm? form;readonly DeferredWindowOpen settingsOpen;
 ulong last;
 long tick;
 DateTimeOffset lastWall;
 bool blocked;bool inputAccessWarningShown;
 readonly InputModeNoticePolicy inputNotices=new(),penInputNotices=new(InputTimingMode.DeviceHeaders);long nextAnyInputNotice;bool inputNoticeQuery,inputNoticeWarned,timingBalloon;long nextInputNoticeCheck;int inputNoticeGeneration;bool openTimingRequested;
 string status="提醒功能已启用";
 string noticeTrace="";int noticeTraceBudget=50;
 void TraceNotice(string code){if(code==noticeTrace||noticeTraceBudget<=0)return;noticeTrace=code;noticeTraceBudget--;Store.Log("input-notice-"+code);}
 bool promptOpen;bool exitRequested;
 bool suppressionKnown,statusRendered;
 CancellationTokenSource? preparingReminder;
 string mediaStatus="";string volumeStatus="";
 readonly CancellationTokenSource lifetime=new();
 bool suppressionActive,suppressionQuery,slideShowActive;bool wpsUnknown;long nextSuppressionCheck;int rulesGeneration;bool suppressionWarned;
 public Tray(bool background){
 _=eventDispatch.Handle;settingsOpen=new DeferredWindowOpen(eventDispatch,Open);
 try{Startup.RepairExisting();}catch(Exception ex){Store.Log("startup-path-repair-failed",ex);}
 activity.Configure(s.InputTiming,s.RequireIdentifiedDeviceInput);audio.ReadInput=()=>activity.Read().Stamp;
 audio.CanPlay=()=>!powerSuspended&&!engine.Suspended&&!safety.Blocked&&Native.Accessible();
 safety.LockedChanged+=()=>{CancelPreparation();audio.Stop();engine.Reset(Environment.TickCount64,true);};
 safety.PowerChanged+=transition=>Power(this,new PowerModeChangedEventArgs(transition==PowerTransition.Suspend?PowerModes.Suspend:PowerModes.Resume));
 audio.ReminderStarted+=StartPhoneNotification;audio.Finished+=_=>phoneSend?.Cancel();
 s.PrepareForLaunch();
 audio.VolumeSettings=s;audio.VolumeStatusChanged+=value=>volumeStatus="；"+value;
 icon=new NotifyIcon{Icon=trayArtwork,Text=$"闲置提醒器 v{AppVersion.Current}",Visible=true};
 var menu=new ContextMenuStrip();
 menu.Items.Add("启用提醒",null,(_,_)=>Enable());
 foreach(var n in new[]{30,60,120}){var minutes=n;menu.Items.Add($"暂停 {n} 分钟",null,(_,_)=>Pause(minutes));}
 menu.Items.Add("自定义暂停…",null,(_,_)=>{menu.Close();using var dialog=new CustomPauseForm();if(dialog.ShowDialog()==DialogResult.OK)Pause(dialog.Minutes);});
 menu.Items.Add("暂停直到手动恢复",null,(_,_)=>Pause(null));
 menu.Items.Add($"版本 {AppVersion.Current}").Enabled=false;
 menu.Items.Add("打开设置",null,(_,_)=>{SettingsWindowPresenter.Log("tray-request");menu.Close();settingsOpen.Request();});
 menu.Items.Add("退出程序",null,(_,_)=>ExitThread());
 icon.ContextMenuStrip=menu;icon.DoubleClick+=(_,_)=>{SettingsWindowPresenter.Log("tray-request");menu.Close();settingsOpen.Request();};
 icon.BalloonTipClicked+=(_,_)=>{if(!timingBalloon)return;TraceNotice("clicked");timingBalloon=false;openTimingRequested=true;settingsOpen.Request();};
 icon.BalloonTipShown+=(_,_)=>{if(timingBalloon)TraceNotice("shell-shown");};
 icon.BalloonTipClosed+=(_,_)=>{if(timingBalloon)TraceNotice("closed-or-expired");timingBalloon=false;};
 audio.Failed+=message=>ShowTip("音频提醒失败",message,ToolTipIcon.Warning);
 last=activity.Read().Stamp;tick=Environment.TickCount64;lastWall=DateTimeOffset.UtcNow;engine.Reset(tick);
 SystemEvents.PowerModeChanged+=Power;SystemEvents.SessionSwitch+=Session;
 timer.Tick+=(_,_)=>{try{Poll();}catch(Exception e){CancelPreparation();audio.Stop();engine.Reset(Environment.TickCount64,true);Store.Log("poll-failed",e);SetStatus("检测异常，等待用户操作后恢复");}};
 if(Store.WriteBlocked){s.Enabled=false;ShowTip("配置读取失败","原配置已保留，禁止自动覆盖。请备份并恢复配置后重新启动。",ToolTipIcon.Warning);}else Save();
 phoneStartupTip.Tick+=async(_,_)=>{
  phoneStartupTip.Stop();
  if(exitRequested||Store.WriteBlocked||!s.Phone.Enabled)return;
  try{
   bool submitted=await KdeStartupNotification.ShowAsync(s.Phone.CliPath,lifetime.Token);
   if(!submitted&&!exitRequested&&s.Phone.Enabled&&!lifetime.IsCancellationRequested)
    ShowTip(KdeStartupNotification.Title,KdeStartupNotification.Text,ToolTipIcon.Info);
  }
  catch(Exception ex){Store.Log("phone-startup-notification-failed",ex);}
 };
 if(s.Phone.Enabled&&!Store.WriteBlocked)phoneStartupTip.Start();
 timer.Start();if(!background)settingsOpen.Request();
 }
 void ShowTip(string title,string text,ToolTipIcon type,bool timing=false){timingBalloon=timing;if(timing)TraceNotice("submit");icon.ShowBalloonTip(10000,title,text,type);}
 void RefreshInputNotices(long now){
  bool pen=s.InputTiming==InputTimingMode.DeviceHeaders;var policy=pen?penInputNotices:inputNotices;var options=pen?s.PenInputModeNotices:s.InputModeNotices;
  if(Store.WriteBlocked){TraceNotice("config-blocked");return;}
  if(!policy.Eligible(options,s.InputTiming)){TraceNotice(pen?"pen-disabled-or-empty":"game-disabled-or-empty");return;}
  if(!policy.CanShow(now)||now<nextAnyInputNotice){TraceNotice("cooldown");return;}
  if(inputNoticeQuery||now<nextInputNoticeCheck)return;
  if(audio.Playing||preparingReminder!=null){TraceNotice("audio-busy");return;}
  var entries=policy.Pending(options);if(entries.Length==0){TraceNotice("once-limit");return;}
  inputNoticeQuery=true;nextInputNoticeCheck=now+10000;_ = CheckInputNoticeAsync(entries,inputNoticeGeneration,pen);
 }
 async Task CheckInputNoticeAsync(string[] entries,int generation,bool pen){
  try{
   using var timeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);timeout.CancelAfter(2000);
   var match=await Task.Run(()=>InputModeApplicationProbe.FindRunning(entries,timeout.Token),timeout.Token);
   var policy=pen?penInputNotices:inputNotices;var options=pen?s.PenInputModeNotices:s.InputModeNotices;
   TraceNotice(match==null?"no-match":pen?"pen-match":"game-match");
   if(exitRequested||generation!=inputNoticeGeneration||match==null||powerSuspended||engine.Suspended||!s.Enabled||audio.Playing||preparingReminder!=null||!Native.Accessible()||!policy.Eligible(options,s.InputTiming)||Environment.TickCount64<nextAnyInputNotice)return;
   if(!policy.TryMark(match,Environment.TickCount64)){TraceNotice("mark-rejected");return;}nextAnyInputNotice=Environment.TickCount64+60000;
   ShowTip("是否调整闲置检测方式？",pen?"名单内数位板软件正在运行。若数位笔无法停止播报，可考虑Windows输入计时或关闭无设备标识过滤。点击打开高级设置；不会自动切换。":"名单内游戏软件正在运行。若Windows输入计时受影响，可手动选择设备键鼠事件计时（实验）。点击打开高级设置；不会自动切换。",ToolTipIcon.Info,true);
  }catch(OperationCanceledException){TraceNotice("cancelled-or-timeout");}
  catch(Exception ex){if(!inputNoticeWarned){inputNoticeWarned=true;Store.Log("input-mode-notice-query-failed",ex);}}
  finally{if(generation==inputNoticeGeneration)inputNoticeQuery=false;}
 }
 void OnUi(Action action){
  if(eventDispatch.IsDisposed)return;
  try{if(eventDispatch.InvokeRequired)eventDispatch.BeginInvoke(action);else action();}
  catch(InvalidOperationException ex){if(!eventDispatch.IsDisposed)Store.Log("power-dispatch-failed",ex);}
 }
 void Power(object? o,PowerModeChangedEventArgs e){
  var transition=e.Mode switch{PowerModes.Suspend=>PowerTransition.Suspend,PowerModes.Resume=>PowerTransition.Resume,_=>PowerTransition.Other};
  // StatusChange is not sleep: keep playback and idle counting unchanged.
  if(transition==PowerTransition.Other)return;
  if(transition==PowerTransition.Suspend)powerSuspended=true;
  OnUi(()=>{
   engine.ApplyPowerTransition(transition,Environment.TickCount64);
   if(transition==PowerTransition.Resume)powerSuspended=false;
   CancelPreparation();audio.Stop();
  });
 }
 void Session(object o,SessionSwitchEventArgs e)=>OnUi(()=>{CancelPreparation();audio.Stop();engine.Reset(Environment.TickCount64,true);});
 void Save(){try{Store.Save(s);}catch(Exception e){Store.Log("save-failed",e);MessageBox.Show("设置保存失败，请检查项目 data 文件夹权限。");}}
 void SetStatus(string text){if(statusRendered&&status==text)return;statusRendered=true;status=text;icon.Text=("闲置提醒器："+text)[..Math.Min(63,("闲置提醒器："+text).Length)];form?.UpdateStatus(text);}
 internal void Enable(){if(Store.WriteBlocked){MessageBox.Show("配置读取失败，已禁止启用默认提醒。请恢复配置后重新启动。");return;}CancelPreparation();s.Enabled=true;s.PauseUntil=null;s.AskAt=null;engine.Reset(Environment.TickCount64);Save();SetStatus("提醒功能已启用");}
 internal void Pause(int? minutes){CancelPreparation();s.Enabled=false;s.PauseUntil=minutes.HasValue?DateTimeOffset.UtcNow.AddMinutes(minutes.Value):null;s.AskAt=DateTimeOffset.UtcNow.AddMinutes(s.AskMinutes);audio.Stop();Save();SetStatus("当前已暂停");}
 void Poll(){
 safety.Refresh();
 if(safety.Blocked){CancelPreparation();audio.Stop();engine.Reset(Environment.TickCount64,true);SetStatus(safety.Suspended?"系统睡眠中，不播放提醒":"锁屏或会话状态未知，暂停提醒");return;}
 if(powerSuspended||engine.Suspended){CancelPreparation();audio.Stop();SetStatus("系统睡眠中，不播放提醒");return;}
 var now=Environment.TickCount64;var wall=DateTimeOffset.UtcNow;
 var sample=activity.Read();var input=sample.Stamp;var changed=input!=last;
 if(activity.AccessRestricted&&!inputAccessWarningShown){
  inputAccessWarningShown=true;
  ShowTip("设备输入接收权限受限","前台程序权限较高或无法确认接收权限，已临时使用Windows计时。若游戏会刷新该计时，提醒可能无法触发。设备模式启动时会申请UAC；取消后仍保留回退，请核对高级设置说明。",ToolTipIcon.Warning);
 }
 var gap=now-tick>5000||(wall-lastWall).TotalSeconds>5;
 tick=now;lastWall=wall;last=input;
 if(gap){CancelPreparation();audio.Stop();engine.Reset(now,true);}
 if(!Native.Accessible()){blocked=true;CancelPreparation();audio.Stop();engine.Reset(now,true);SetStatus("锁屏或安全桌面，暂停提醒");return;}
 if(blocked){blocked=false;engine.Reset(now,true);}
 if(changed)CancelPreparation();
 audio.HandleInput(input);
 if(!s.Enabled&&s.PauseUntil is {} until&&wall>=until){Enable();}
 if(!s.Enabled){
 engine.Reset(now);
 SetStatus(s.PauseUntil.HasValue?"当前定时暂停":"当前已关闭，等待用户恢复操作");
 if(s.AskAgain&&s.PauseUntil==null&&s.AskAt is {} ask&&wall>=ask&&changed&&!Native.Fullscreen()&&!promptOpen){
 promptOpen=true;
 // Modeless, non-activating prompt; never interrupts the foreground application.
 var p=new ResumeForm(()=>{Enable();},()=>{s.AskAt=DateTimeOffset.UtcNow.AddMinutes(s.AskMinutes);Save();});
 p.FormClosed+=(_,_)=>{promptOpen=false;if(!s.Enabled){s.AskAt=DateTimeOffset.UtcNow.AddMinutes(s.AskMinutes);Save();}};p.Show();
 }return;
 }
 RefreshInputNotices(now);
 var idle=sample.IdleMilliseconds;
 if(audio.IsPreview)engine.Reset(now);
 RefreshSuppression(now);
 bool suppressed=suppressionActive||suppressionQuery;
 if(suppressed){
  engine.Due(now,idle,changed,s,true);
  if(suppressionActive){CancelPreparation();if(audio.Playing&&!audio.IsPreview)audio.Stop();}
  SetStatus(CurrentRuleStatus());
  return;
 }
 var previousReminder=engine.LastReminder;
 if(preparingReminder==null&&!audio.IsPreview&&engine.Due(now,idle,changed,s)){
 last=activity.Read().Stamp; // Capture fresh baseline before starting playback.
 _=PrepareReminderAsync(previousReminder,now);
 }
 SetStatus(preparingReminder!=null?"正在暂停可暂停媒体":CurrentRuleStatus());
 }
 string CurrentRuleStatus()=>RuleStatusText.Get(suppressionActive,suppressionQuery,suppressionKnown,slideShowActive,engine.WaitingInput,
  "提醒功能已启用；"+activity.Status+(wpsUnknown?"；WPS放映未能可靠识别，可配置例外":"")+mediaStatus+volumeStatus+(s.Phone.Enabled?phoneStatus:""));
 void RefreshSuppression(long now){
  var plan=SuppressionPolicy.Capture(s);
  if(!plan.NeedsCheck){suppressionKnown=true;suppressionActive=false;slideShowActive=false;wpsUnknown=false;return;}
  if(suppressionQuery||now<nextSuppressionCheck)return;
  suppressionQuery=true;

  _=CheckSuppressionAsync(plan.Rules,plan.Slides,rulesGeneration);
 }
 async Task CheckSuppressionAsync(SuppressionRule[] rules,SlideShowOptions slides,int generation){
  try{
   using var timeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);timeout.CancelAfter(2000);
   var slide=await SlideShowDetector.EvaluateAsync(slides,timeout.Token);
   var result=slide.Active?new SuppressionResult(true):await SuppressionDetector.EvaluateAsync(rules,timeout.Token);
   if(lifetime.IsCancellationRequested||generation!=rulesGeneration)return;
   slideShowActive=slide.Active;wpsUnknown=slide.WpsUnknown;suppressionActive=result.Active;
   if(result.MediaUnavailable&&!suppressionWarned){Store.Log("suppression-media-unavailable");suppressionWarned=true;}
   if(result.Active){CancelPreparation();if(audio.Playing&&!audio.IsPreview)audio.Stop();}
  }catch(OperationCanceledException){if(!lifetime.IsCancellationRequested&&generation==rulesGeneration){suppressionActive=false;slideShowActive=false;}}
  catch(Exception ex){if(generation==rulesGeneration)suppressionActive=false;Store.Log("suppression-query-failed",ex);}
  finally{if(generation==rulesGeneration){suppressionKnown=true;suppressionQuery=false;nextSuppressionCheck=Environment.TickCount64+2000;}}
 }
 void CancelPreparation(){preparingReminder?.Cancel();}
 async Task PrepareReminderAsync(long? previousReminder,long scheduledAt){
  bool played=false;
  using var pending=new CancellationTokenSource();
  preparingReminder=pending;
  bool suppressBoost=VolumePolicy.SkipBoostForMedia(s.PauseMediaOnReminder,false);
  try{
   if(safety.Blocked)return;
   var baseline=activity.Read().Stamp;
   if(s.PauseMediaOnReminder){
    using var deadline=CancellationTokenSource.CreateLinkedTokenSource(pending.Token);
    deadline.CancelAfter(TimeSpan.FromSeconds(2));
    try{
     var result=await MediaPause.PauseAllAsync(deadline.Token,()=>activity.Read().Stamp==baseline,s.MediaScope,s.MediaApplications.ToArray());
     suppressBoost=VolumePolicy.SkipBoostForMedia(true,true,result.Protected,result.Failed,result.UnsupportedPlaying);
     mediaStatus=result.Failed>0?$"；部分媒体未能暂停（{result.Failed}）":result.Paused>0?$"；已确认暂停 {result.Paused} 个媒体":result.Protected>0?"；播放中的媒体不在暂停范围内":result.UnsupportedPlaying>0?"；播放中的媒体不支持暂停":"；Windows 未提供可暂停的播放会话";
     if(result.Failed>0)Store.Log("media-pause-partial-failure");
    }catch(OperationCanceledException) when(!pending.IsCancellationRequested){mediaStatus="；媒体暂停超时，仍播放提醒";Store.Log("media-pause-timeout");}
    catch(Exception ex) when(ex is not OperationCanceledException){mediaStatus="；媒体暂停不可用，仍播放提醒";Store.Log("media-pause-failed",ex);}
   }else mediaStatus="";
   pending.Token.ThrowIfCancellationRequested();
   if(safety.Blocked||powerSuspended||engine.Suspended||engine.WaitingInput||suppressionActive||suppressionQuery||!s.Enabled||audio.IsPreview||activity.Read().Stamp!=baseline||!Native.Accessible())return;
   audio.Play(Files(s.Mode),s.Volume,suppressAutoBoost:suppressBoost);played=true;
  }catch(OperationCanceledException){}
  catch(Exception ex){Store.Log("reminder-preparation-failed",ex);}
  finally{if(!played)engine.CancelScheduledReminder(scheduledAt,previousReminder);if(ReferenceEquals(preparingReminder,pending))preparingReminder=null;}
 }
 void StartPhoneNotification(){
  if(!s.Phone.Enabled||lifetime.IsCancellationRequested)return;
  phoneSend?.Cancel();phoneSend?.Dispose();phoneSend=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
  var options=s.Phone.Copy();var token=phoneSend.Token;phoneStatus="；正在发送手机通知";
  _=SendPhoneNotificationAsync(options,token);
 }
 async Task SendPhoneNotificationAsync(PhoneOptions options,CancellationToken token){
  try{var result=await Task.Run(()=>phone.SendAsync(options,false,token),token);if(!token.IsCancellationRequested)OnUi(()=>phoneStatus="；"+result);}
  catch(OperationCanceledException){if(!lifetime.IsCancellationRequested)OnUi(()=>phoneStatus="；手机通知已取消或超时");}
  catch(Exception ex){Store.Log("phone-send-failed",ex);if(!lifetime.IsCancellationRequested)OnUi(()=>phoneStatus="；手机通知失败，电脑播报不受影响");}
 }
 internal IEnumerable<string> Files(int mode)=>s.AudioFiles(AppContext.BaseDirectory,mode);
 void Open(){SettingsWindowPresenter.Log("open-enter");if(form is null||form.IsDisposed){SettingsWindowPresenter.Log("construct-begin");form=new SettingsForm(s,this,audio);SettingsWindowPresenter.Log("construct-end");form.FormClosed+=(_,_)=>form=null;}form.UpdateStatus(status);SettingsWindowPresenter.Show(form);if(openTimingRequested){openTimingRequested=false;form.ShowInputTiming();}}
 bool liveElevationPending;
 internal void RequestInputElevation(){
  if(exitRequested||liveElevationPending||Store.WriteBlocked||s.InputTiming!=InputTimingMode.DeviceHeaders)return;
  liveElevationPending=true;
  try{if(StartupElevation.TrySwitch(s.InputTiming)){Store.Log("live-elevation-child-started");ExitThread();}}
  finally{liveElevationPending=false;}
 }
 internal string? Commit(Settings draft,bool startup){
  var accepted=draft.Copy();accepted.Enabled=s.Enabled;accepted.PauseUntil=s.PauseUntil;accepted.AskAt=s.AskAt;accepted.Validate();
  Store.Save(accepted);
  inputNoticeGeneration++;inputNoticeQuery=false;nextInputNoticeCheck=0;timingBalloon=false;
  rulesGeneration++;suppressionKnown=false;suppressionActive=false;suppressionQuery=false;slideShowActive=false;wpsUnknown=false;nextSuppressionCheck=0;
  CancelPreparation();audio.Stop();
  s.CopyFrom(accepted);audio.VolumeSettings=s;activity.Configure(s.InputTiming,s.RequireIdentifiedDeviceInput);last=activity.Read().Stamp;
  engine.Reset(Environment.TickCount64);
  try{Startup.Set(startup);return null;}catch(Exception e){Store.Log("startup-save-failed",e);return "设置已保存，但自启动更新失败，请重试。";}
 }
 protected override void ExitThreadCore(){if(exitRequested)return;exitRequested=true;settingsOpen.Dispose();SlideShowHelper.Stop();lifetime.Cancel();phoneSend?.Cancel();CancelPreparation();timer.Stop();phoneStartupTip.Dispose();audio.Dispose();safety.Dispose();activity.Dispose();phoneSend?.Dispose();SystemEvents.PowerModeChanged-=Power;SystemEvents.SessionSwitch-=Session;icon.Visible=false;icon.Dispose();trayArtwork.Dispose();timer.Dispose();form?.Dispose();eventDispatch.Dispose();MusicRestoreScheduler.WhenIdle(()=>base.ExitThreadCore());}
}
internal sealed class ResumeForm:Form {
 protected override bool ShowWithoutActivation=>true;
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
 internal ResumeForm(Action enable,Action later){Text="闲置提醒";Size=new Size(410,170);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=true;TopMost=false;var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18)};panel.Controls.Add(new Label{Text="闲置提醒目前已关闭，是否重新启用？",AutoSize=true,Margin=new Padding(3,3,3,20)});var yes=new Button{Text="重新启用",AutoSize=true};var no=new Button{Text="稍后再次提醒",AutoSize=true};yes.Click+=(_,_)=>{enable();Close();};no.Click+=(_,_)=>{later();Close();};panel.Controls.Add(yes);panel.Controls.Add(no);Controls.Add(panel);}
}












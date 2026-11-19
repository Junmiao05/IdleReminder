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
 string status=UiText.Get("Tray_021");
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
 audio.VolumeSettings=s;audio.VolumeStatusChanged+=value=>volumeStatus=UiText.Format("Tray_StatusSuffix",value);
 icon=new NotifyIcon{Icon=trayArtwork,Text=UiText.Format("Tray_022",AppVersion.Current),Visible=true};
 var menu=TrayMenu.Create(Enable,Pause,()=>{SettingsWindowPresenter.Log("tray-request");settingsOpen.Request();},ExitThread);
 icon.ContextMenuStrip=menu;icon.DoubleClick+=(_,_)=>{SettingsWindowPresenter.Log("tray-request");menu.Close();settingsOpen.Request();};
 icon.BalloonTipClicked+=(_,_)=>{if(!timingBalloon)return;TraceNotice("clicked");timingBalloon=false;openTimingRequested=true;settingsOpen.Request();};
 icon.BalloonTipShown+=(_,_)=>{if(timingBalloon)TraceNotice("shell-shown");};
 icon.BalloonTipClosed+=(_,_)=>{if(timingBalloon)TraceNotice("closed-or-expired");timingBalloon=false;};
 audio.Failed+=message=>ShowTip(UiText.Get("Tray_006"),message,ToolTipIcon.Warning);
 last=activity.Read().Stamp;tick=Environment.TickCount64;lastWall=DateTimeOffset.UtcNow;engine.Reset(tick);
 SystemEvents.PowerModeChanged+=Power;SystemEvents.SessionSwitch+=Session;
 timer.Tick+=(_,_)=>{try{Poll();}catch(Exception e){CancelPreparation();audio.Stop();engine.Reset(Environment.TickCount64,true);Store.Log("poll-failed",e);SetStatus(UiText.Get("Tray_007"));}};
 if(Store.WriteBlocked){s.Enabled=false;ShowTip(UiText.Get("Tray_008"),UiText.Get("Tray_025"),ToolTipIcon.Warning);}else Save();
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
   ShowTip(UiText.Get("Tray_009"),pen?UiText.Get("Tray_026"):UiText.Get("Tray_027"),ToolTipIcon.Info,true);
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
 void Save(){try{Store.Save(s);}catch(Exception e){Store.Log("save-failed",e);MessageBox.Show(UiText.Get("Tray_010"));}}
 void SetStatus(string text){if(statusRendered&&status==text)return;statusRendered=true;status=text;var tooltip=UiText.Format("Tray_StatusTooltip",text);icon.Text=tooltip[..Math.Min(63,tooltip.Length)];form?.UpdateStatus(text);}
 internal void Enable(){if(Store.WriteBlocked){MessageBox.Show(UiText.Get("Tray_011"));return;}CancelPreparation();s.Enabled=true;s.PauseUntil=null;s.AskAt=null;engine.Reset(Environment.TickCount64);Save();SetStatus(UiText.Get("Tray_012"));}
 internal void Pause(int? minutes){CancelPreparation();s.Enabled=false;s.PauseUntil=minutes.HasValue?DateTimeOffset.UtcNow.AddMinutes(minutes.Value):null;s.AskAt=DateTimeOffset.UtcNow.AddMinutes(s.AskMinutes);audio.Stop();Save();SetStatus(UiText.Get("Tray_013"));}
 void Poll(){
 safety.Refresh();
 if(safety.Blocked){CancelPreparation();audio.Stop();engine.Reset(Environment.TickCount64,true);SetStatus(safety.Suspended?UiText.Get("Tray_028"):UiText.Get("Tray_029"));return;}
 if(powerSuspended||engine.Suspended){CancelPreparation();audio.Stop();SetStatus(UiText.Get("Tray_014"));return;}
 var now=Environment.TickCount64;var wall=DateTimeOffset.UtcNow;
 var sample=activity.Read();var input=sample.Stamp;var changed=input!=last;
 if(activity.AccessRestricted&&!inputAccessWarningShown){
  inputAccessWarningShown=true;
  ShowTip(UiText.Get("Tray_015"),UiText.Get("Tray_030"),ToolTipIcon.Warning);
 }
 var gap=now-tick>5000||(wall-lastWall).TotalSeconds>5;
 tick=now;lastWall=wall;last=input;
 if(gap){CancelPreparation();audio.Stop();engine.Reset(now,true);}
 if(!Native.Accessible()){blocked=true;CancelPreparation();audio.Stop();engine.Reset(now,true);SetStatus(UiText.Get("Tray_016"));return;}
 if(blocked){blocked=false;engine.Reset(now,true);}
 if(changed)CancelPreparation();
 audio.HandleInput(input);
 if(!s.Enabled&&s.PauseUntil is {} until&&wall>=until){Enable();}
 if(!s.Enabled){
 engine.Reset(now);
 SetStatus(s.PauseUntil.HasValue?UiText.Get("Tray_031"):UiText.Get("Tray_032"));
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
 SetStatus(preparingReminder!=null?UiText.Get("Tray_033"):CurrentRuleStatus());
 }
 string CurrentRuleStatus()=>RuleStatusText.Get(suppressionActive,suppressionQuery,suppressionKnown,slideShowActive,engine.WaitingInput,
  UiText.Get("Tray_034")+activity.Status+(wpsUnknown?UiText.Get("Tray_035"):"")+mediaStatus+volumeStatus+(s.Phone.Enabled?phoneStatus:""));
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
     mediaStatus=result.Failed>0?UiText.Format("Tray_036",result.Failed):result.Paused>0?UiText.Format("Tray_037",result.Paused):result.Protected>0?UiText.Get("Tray_038"):result.UnsupportedPlaying>0?UiText.Get("Tray_039"):UiText.Get("Tray_040");
     if(result.Failed>0)Store.Log("media-pause-partial-failure");
    }catch(OperationCanceledException) when(!pending.IsCancellationRequested){mediaStatus=UiText.Get("Tray_041");Store.Log("media-pause-timeout");}
    catch(Exception ex) when(ex is not OperationCanceledException){mediaStatus=UiText.Get("Tray_042");Store.Log("media-pause-failed",ex);}
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
  var options=s.Phone.Copy();var token=phoneSend.Token;phoneStatus=UiText.Get("Tray_043");
  _=SendPhoneNotificationAsync(options,token);
 }
 async Task SendPhoneNotificationAsync(PhoneOptions options,CancellationToken token){
  try{var result=await Task.Run(()=>phone.SendAsync(options,false,token),token);if(!token.IsCancellationRequested)OnUi(()=>phoneStatus=UiText.Format("Tray_StatusSuffix",result));}
  catch(OperationCanceledException){if(!lifetime.IsCancellationRequested)OnUi(()=>phoneStatus=UiText.Get("Tray_044"));}
  catch(Exception ex){Store.Log("phone-send-failed",ex);if(!lifetime.IsCancellationRequested)OnUi(()=>phoneStatus=UiText.Get("Tray_045"));}
 }
 internal IEnumerable<string> Files(int mode)=>s.AudioFiles(AppContext.BaseDirectory,mode);
 SettingsForm CreateSettingsForm(){
  var created=new SettingsForm(s,this,audio);
  created.FormClosed+=(_,_)=>{if(ReferenceEquals(form,created))form=null;};
  created.LanguageRefreshRequested+=source=>eventDispatch.BeginInvoke((Action)(()=>RefreshSettingsLanguage(source)));
  return created;
 }
 void Open(){SettingsWindowPresenter.Log("open-enter");if(form is null||form.IsDisposed){SettingsWindowPresenter.Log("construct-begin");form=CreateSettingsForm();SettingsWindowPresenter.Log("construct-end");}form.UpdateStatus(status);SettingsWindowPresenter.Show(form);if(openTimingRequested){openTimingRequested=false;form.ShowInputTiming();}}
 void RefreshSettingsLanguage(SettingsForm source){
  if(exitRequested||source.IsDisposed||!ReferenceEquals(form,source))return;
  try{
   var view=SettingsViewState.Capture(source);
   var replacement=CreateSettingsForm();
   source.Close();
   form=replacement;
   replacement.UpdateStatus(status);
   view.Show(replacement);
  }catch(Exception ex){Store.Log("language-window-refresh-failed",ex);}
 }
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
  if(UiText.CultureOverride?.Name!=s.Language){
   ApplicationLanguage.Apply(s.Language);
   TrayMenu.Refresh(icon.ContextMenuStrip!);
   mediaStatus="";volumeStatus="";phoneStatus="";
   statusRendered=false;
   SetStatus(!s.Enabled?UiText.Get(s.PauseUntil.HasValue?"Tray_031":"Tray_032"):CurrentRuleStatus());
   foreach(var prompt in Application.OpenForms.OfType<ResumeForm>().ToArray())prompt.RefreshLanguage();
  }
  try{Startup.Set(startup);return null;}catch(Exception e){Store.Log("startup-save-failed",e);return UiText.Get("Tray_046");}
 }
 protected override void ExitThreadCore(){if(exitRequested)return;exitRequested=true;settingsOpen.Dispose();SlideShowHelper.Stop();lifetime.Cancel();phoneSend?.Cancel();CancelPreparation();timer.Stop();phoneStartupTip.Dispose();audio.Dispose();safety.Dispose();activity.Dispose();phoneSend?.Dispose();SystemEvents.PowerModeChanged-=Power;SystemEvents.SessionSwitch-=Session;icon.Visible=false;icon.Dispose();trayArtwork.Dispose();timer.Dispose();form?.Dispose();eventDispatch.Dispose();MusicRestoreScheduler.WhenIdle(()=>base.ExitThreadCore());}
}
internal sealed class ResumeForm:Form {
 protected override bool ShowWithoutActivation=>true;
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
 internal void RefreshLanguage(){
  Text=UiText.Get("Tray_017");
  foreach(Control control in Controls[0].Controls)if(control.Tag is string key)control.Text=UiText.Get(key);
 }
 internal ResumeForm(Action enable,Action later){Text=UiText.Get("Tray_017");Size=new Size(410,200);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=true;TopMost=false;var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18)};panel.Controls.Add(new Label{Tag="Tray_018",Text=UiText.Get("Tray_018"),AutoSize=true,MaximumSize=new Size(340,0),Margin=new Padding(3,3,3,20)});var yes=new Button{Tag="Tray_019",Text=UiText.Get("Tray_019"),AutoSize=true};var no=new Button{Tag="Tray_020",Text=UiText.Get("Tray_020"),AutoSize=true};yes.Click+=(_,_)=>{enable();Close();};no.Click+=(_,_)=>{later();Close();};panel.Controls.Add(yes);panel.Controls.Add(no);Controls.Add(panel);}
}












using IdleReminder;
int count=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
var s=new Settings();var e=new ReminderEngine();e.Reset(0);
Check(!e.Due(1199999,1199999,false,s),"threshold before");
Check(e.Due(1200000,1200000,false,s),"threshold reached");
Check(!e.Due(1201000,1201000,false,s),"no burst");
Check(e.Due(1800000,1800000,false,s),"repeat interval");
Check(!e.Due(1801000,0,true,s),"input resets");
Check(!e.Due(1802000,1000,false,s),"new idle cycle");
e.Reset(0);s.Repeat=false;Check(e.Due(1200000,1200000,false,s),"single first");
Check(!e.Due(9999999,9999999,false,s),"single no repeat");
e.Reset(0,true);Check(!e.Due(9999999,9999999,false,s),"resume awaits input");
Check(!e.Due(10000000,0,true,s),"resume input reset");
Check(e.Due(11200000,1200000,false,s),"resume full threshold");
s.Enabled=false;Check(!e.Due(99999999,99999999,false,s),"disabled");
s.IdleMinutes=-1;s.Volume=200;s.Validate();Check(s.IdleMinutes==1&&s.Volume==100,"validation");

var pause=new Settings{Enabled=false,PauseUntil=DateTimeOffset.UtcNow.AddHours(1),AskAt=DateTimeOffset.UtcNow.AddMinutes(30),Voice="local-test.wav"};
var restored=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(pause))!;
Check(!restored.Enabled&&restored.PauseUntil==pause.PauseUntil&&restored.AskAt==pause.AskAt&&restored.Voice==pause.Voice,"pause and audio settings roundtrip");

Check(!PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Preview,100,101),"preview continues on input");
Check(!PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Preview,uint.MaxValue,0),"preview continues across tick wrap");
Check(!PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Reminder,100,100),"reminder ignores original input");
Check(PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Reminder,100,101),"reminder stops on new input");
Check(!PlaybackPolicy.ShouldStopForInput(false,AudioPurpose.Reminder,100,101),"stopped playback remains stopped");
foreach(var until in new DateTimeOffset?[]{null,DateTimeOffset.UtcNow.AddHours(1),DateTimeOffset.UtcNow.AddHours(-1)}){
 var stored=new Settings{Enabled=false,PauseUntil=until,AskAt=DateTimeOffset.UtcNow.AddMinutes(30),IdleMinutes=45,Volume=37,Preset=3,Voice="voice.wav",Repeat=false};
 var launched=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(stored))!;
 launched.PrepareForLaunch();
 Check(launched.Enabled&&launched.PauseUntil==null&&launched.AskAt==null,"launch clears pause and enables reminders");
 Check(launched.IdleMinutes==45&&launched.Volume==37&&launched.Preset==3&&launched.Voice=="voice.wav"&&!launched.Repeat,"launch preserves user preferences");
 var restartedEngine=new ReminderEngine();restartedEngine.Reset(9000000);
 Check(!restartedEngine.Due(9000000,9000000,false,launched),"launch does not immediately alert on old idle time");
}
Check(Presets.Names.Length==Presets.FileNames.Length&&Presets.Count==5,"preset names match files");
for(int index=0;index<Presets.Count;index++){
 var choice=new Settings{Preset=index};choice.Validate();
 var roundtrip=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(choice))!;
 roundtrip.Validate();Check(roundtrip.Preset==index,"preset selection roundtrip "+index);
}
Check(Presets.VoiceNames.Length==0&&Presets.VoiceFileNames.Length==0,"no bundled copyrighted voice presets");
for(int i=0;i<6;i++){
 var oldVoice=new Settings{VoicePreset=i,Mode=2,Preset=2};oldVoice.Validate();
 Check(oldVoice.VoicePreset==-1&&oldVoice.Mode==0&&oldVoice.Preset==2,"retired preset falls back to selected tone "+i);
 var oldOnly=new Settings{VoicePreset=i,Mode=1};oldOnly.Validate();
 Check(oldOnly.Mode==0&&oldOnly.AudioFiles("root",oldOnly.Mode).Single().EndsWith("tone0.wav"),"voice only remains audible after retirement "+i);
 var legacy=new Settings{Preset=i+5};legacy.Validate();
 Check(legacy.Preset==0&&legacy.VoicePreset==-1&&legacy.Mode==0,"earliest voice-as-tone migration safe "+i);
 var retained=new Settings{VoicePreset=i,Voice="my-recording.wav",Mode=2};retained.Validate();
 Check(retained.Voice=="my-recording.wav"&&retained.Mode==2&&retained.AudioFiles("root",2).Last()=="my-recording.wav","custom recording never removed "+i);
}
var custom=new Settings{Voice="custom.wav",VoicePreset=1};
Check(custom.AudioFiles("root",1).Single()=="custom.wav","custom recording precedence");
var empty=new Settings();
Check(!empty.HasVoice()&&!empty.AudioFiles("root",1).Any(),"no voice until selected");
var config=new DeviceVolumeSetting{Enabled=true,TargetPercent=40,ConfirmedTarget=40};
Check(VolumePolicy.CanRaise(true,false,.1f,config),"raise approved speaker below target");
Check(!VolumePolicy.CanRaise(false,false,.1f,config),"never raise headphones or unknown");
Check(!VolumePolicy.CanRaise(true,true,.1f,config),"never unmute");
Check(!VolumePolicy.CanRaise(true,false,.4f,config),"never lower equal volume");
Check(!VolumePolicy.CanRaise(true,false,.8f,config),"never lower louder volume");
Check(!VolumePolicy.CanRaise(true,false,.1f,new()),"disabled by default");
Check(!VolumePolicy.CanRaise(true,false,.1f,new(){Enabled=true,TargetPercent=40,ConfirmedTarget=35}),"edited target requires confirmation");
Check(!VolumePolicy.CanRaise(true,false,.1f,null),"unconfigured device denied");
Check(VolumePolicy.CanRestore(false,.4f,.4f),"restore own unchanged boost");
Check(!VolumePolicy.CanRestore(true,.4f,.4f),"respect change even if returned to target");
Check(!VolumePolicy.CanRestore(false,.5f,.4f),"respect volume mismatch");
var perDevice=new Settings();
perDevice.DeviceVolumes["laptop"]=config;
perDevice.DeviceVolumes["external"]=new(){TargetPercent=20,ConfirmedTarget=20,Enabled=true};
var loaded=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(perDevice))!;
loaded.Validate();
Check(loaded.DeviceVolumes["laptop"].TargetPercent==40&&loaded.DeviceVolumes["external"].TargetPercent==20,"independent device persistence");
Check(new DeviceVolumeSetting().TargetPercent==35&&!new DeviceVolumeSetting().Enabled,"35 percent suggestion only");
const string speakerSubtype="DFF21CE1-F70F-11D0-B917-00A0C9223196";
Check(VolumePolicy.IsSpeaker(1,speakerSubtype,"内置扬声器"),"require both speaker indicators");
Check(!VolumePolicy.IsSpeaker(3,speakerSubtype,"Headphones"),"headphone form blocked");
Check(!VolumePolicy.IsSpeaker(1,null,"扬声器"),"missing subtype blocked");
Check(!VolumePolicy.IsSpeaker(null,speakerSubtype,"扬声器"),"missing form blocked");
Check(!VolumePolicy.IsSpeaker(1,"not-a-guid","扬声器"),"unknown subtype blocked");
Check(!VolumePolicy.IsSpeaker(1,speakerSubtype,"Speakers / Headphones"),"mixed speaker headphone output blocked");
Check(!VolumePolicy.IsSpeaker(1,speakerSubtype,"USB 耳机"),"usb headphone name blocked");
Check(!VolumePolicy.IsSpeaker(1,speakerSubtype,"Bluetooth Headset"),"bluetooth headset blocked");
config.Enabled=false;
Check(!VolumePolicy.CanRaise(true,false,.1f,config),"explicit opt out blocked");
var music=new FakeMedia();var video=new FakeMedia();var pausedMedia=new FakeMedia{IsPlaying=false};var cannotPause=new FakeMedia{CanPause=false};
var result=await MediaPausePolicy.PauseAsync(new IPausableMedia[]{music,video,pausedMedia,cannotPause},CancellationToken.None);
Check(result.Paused==2&&result.Skipped==2&&music.Requests==1&&video.Requests==1,"pause every playing controllable session");
Check(pausedMedia.Requests==0&&cannotPause.Requests==0,"do not toggle paused or unsupported media");
var rejects=new FakeMedia{Reply=_=>Task.FromResult(false)};
var throws=new FakeMedia{Reply=_=>throw new InvalidOperationException()};
var valid=new FakeMedia();
var partial=await MediaPausePolicy.PauseAsync(new IPausableMedia[]{rejects,throws,valid},CancellationToken.None);
Check(partial.Failed==2&&partial.Paused==1,"one media failure does not block others");
using(var canceled=new CancellationTokenSource()){
 canceled.Cancel();var untouched=new FakeMedia();bool wasCanceled=false;
 try{await MediaPausePolicy.PauseAsync(new[]{untouched},canceled.Token);}catch(OperationCanceledException){wasCanceled=true;}
 Check(wasCanceled&&untouched.Requests==0,"canceled reminder issues no pause command");
}
using(var timeout=new CancellationTokenSource(20)){
 var slow=new FakeMedia{Reply=async token=>{await Task.Delay(10000,token);return true;}};
 bool timedOut=false;try{await MediaPausePolicy.PauseAsync(new[]{slow},timeout.Token);}catch(OperationCanceledException){timedOut=true;}
 Check(timedOut,"pause operations honor timeout cancellation");
}
Check(new Settings().PauseMediaOnReminder,"media pause enabled by default");
var mediaConfig=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings{PauseMediaOnReminder=false}))!;
Check(!mediaConfig.PauseMediaOnReminder,"media pause option persists");
var sleepEngine=new ReminderEngine();var sleepSettings=new Settings{IdleMinutes=1};
sleepEngine.Reset(0);sleepEngine.Suspend(5000);
Check(!sleepEngine.Due(9999999,9999999,false,sleepSettings),"no reminder throughout suspend");
Check(!sleepEngine.Due(9999999,0,true,sleepSettings)&&sleepEngine.WaitingInput,"input cannot bypass suspend");
sleepEngine.Reset(9999999);Check(!sleepEngine.Due(19999999,19999999,false,sleepSettings),"enable or reset cannot bypass suspend");
sleepEngine.Resume(20000000);
Check(!sleepEngine.Due(20000001,20000001,false,sleepSettings)&&sleepEngine.WaitingInput,"resume waits for fresh input");
Check(!sleepEngine.Due(20001000,0,true,sleepSettings),"first input after resume starts fresh cycle");
Check(!sleepEngine.Due(20060999,59999,false,sleepSettings),"resume requires full idle threshold");
Check(sleepEngine.Due(20061000,60000,false,sleepSettings),"normal reminder resumes after threshold");
var screenEngine=new ReminderEngine();var screenSettings=new Settings{IdleMinutes=1,RepeatMinutes=1};
screenEngine.Reset(0);
Check(!screenEngine.ApplyPowerTransition(PowerTransition.Other,30000)&&screenEngine.Baseline==0&&!screenEngine.WaitingInput,"non-suspend power notification preserves idle counting");
Check(screenEngine.Due(60000,60000,false,screenSettings),"display-only inactivity still reaches reminder threshold");
var lastAlert=screenEngine.LastReminder;
Check(!screenEngine.ApplyPowerTransition(PowerTransition.Other,70000)&&screenEngine.LastReminder==lastAlert,"display or AC notification does not reset reminder history");
Check(screenEngine.Due(120000,120000,false,screenSettings),"repeat reminder remains active with display off");
Check(screenEngine.ApplyPowerTransition(PowerTransition.Suspend,130000),"actual suspend is handled");
screenEngine.ApplyPowerTransition(PowerTransition.Other,140000);
Check(screenEngine.Suspended&&!screenEngine.Due(900000,900000,false,screenSettings),"status notification cannot clear actual suspend");
Check(screenEngine.ApplyPowerTransition(PowerTransition.Resume,910000)&&screenEngine.WaitingInput,"actual resume still awaits input");
string[] listed=["chrome.exe","SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify"];
Check(MediaApplicationFilter.ShouldPause("Chrome.EXE",MediaPauseScope.OnlyListed,listed),"allowlist case insensitive exact match");
Check(!MediaApplicationFilter.ShouldPause("msedge.exe",MediaPauseScope.OnlyListed,listed),"allowlist protects other apps");
Check(!MediaApplicationFilter.ShouldPause("chrome.exe",MediaPauseScope.ExceptListed,listed),"exceptions protect listed apps");
Check(MediaApplicationFilter.ShouldPause("msedge.exe",MediaPauseScope.ExceptListed,listed),"exceptions permit other known apps");
Check(!MediaApplicationFilter.ShouldPause("notchrome.exe",MediaPauseScope.OnlyListed,listed),"no substring matching");
Check(!MediaApplicationFilter.ShouldPause("",MediaPauseScope.ExceptListed,listed),"unknown identity protected in filtered mode");
Check(!MediaApplicationFilter.ShouldPause("chrome.exe",MediaPauseScope.OnlyListed,Array.Empty<string>()),"empty allowlist pauses nothing");
Check(MediaApplicationFilter.ShouldPause("chrome.exe",MediaPauseScope.ExceptListed,Array.Empty<string>()),"empty exceptions allows known apps");
Check(MediaApplicationFilter.ShouldPause("",MediaPauseScope.All,listed),"legacy all scope unchanged");
var cleaned=MediaApplicationFilter.Parse(" chrome.exe\r\nChrome.EXE\nmsedge.exe ");
Check(cleaned.Count==2,"list trims and removes duplicate app identifiers");
bool rejectedUrl=false;try{MediaApplicationFilter.Parse("https://example.com");}catch(ArgumentException){rejectedUrl=true;}
Check(rejectedUrl,"web urls rejected rather than pretending support");
bool rejectedDomain=false;try{MediaApplicationFilter.Parse("example.com");}catch(ArgumentException){rejectedDomain=true;}
Check(rejectedDomain,"bare web domain rejected");
var targetApp=new FakeMedia{AppId="chrome.exe"};var protectedApp=new FakeMedia{AppId="msedge.exe"};
var filtered=await MediaPausePolicy.PauseAsync(new[]{targetApp,protectedApp},CancellationToken.None,id=>MediaApplicationFilter.ShouldPause(id,MediaPauseScope.OnlyListed,listed));
Check(targetApp.Requests==1&&protectedApp.Requests==0&&filtered.Protected==1,"protected playing app receives no pause command");
var scopeConfig=new Settings{MediaScope=MediaPauseScope.OnlyListed,MediaApplications=listed.ToList()};
var savedScope=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(scopeConfig))!;
Check(savedScope.MediaScope==MediaPauseScope.OnlyListed&&savedScope.MediaApplications.SequenceEqual(listed),"scope and list persist");
var browsed=MediaApplicationFilter.ExecutableEntries(@"C:\Program Files\播放器\Player.EXE");
Check(browsed[0]=="Player.EXE"&&browsed[1]==@"C:\Program Files\播放器\Player.EXE","browse preserves unicode full path and executable name");
Check(MediaApplicationFilter.ShouldPause("player.exe",MediaPauseScope.OnlyListed,browsed),"browsed executable matches filename media identity");
Check(MediaApplicationFilter.ShouldPause(@"C:\Program Files\播放器\Player.EXE",MediaPauseScope.OnlyListed,browsed),"browsed executable matches full path media identity");
Check(!MediaApplicationFilter.ShouldPause("Package!Player",MediaPauseScope.OnlyListed,browsed),"browse does not guess unrelated package identifiers");
Check(MediaApplicationFilter.Parse(string.Join("\n",browsed)).Count==2,"browsed entries roundtrip through list parser");
bool rejectsShortcut=false;try{MediaApplicationFilter.ExecutableEntries(@"C:\Apps\Player.lnk");}catch(ArgumentException){rejectsShortcut=true;}
Check(rejectsShortcut,"browse rejects shortcuts");
var rulePlaying=new SuppressionRule{Application="player.exe",Mode=SuppressionMode.WhilePlaying};
var ruleRunning=new SuppressionRule{Application="draw.exe",Mode=SuppressionMode.WhileRunning};
Check(SuppressionPolicy.IsMatch(rulePlaying,new[]{"PLAYER.EXE"},Array.Empty<string>()),"playing rule matches case insensitive media identity");
Check(!SuppressionPolicy.IsMatch(rulePlaying,Array.Empty<string>(),new[]{"player.exe"}),"playing rule ignores mere process presence");
Check(SuppressionPolicy.IsMatch(ruleRunning,Array.Empty<string>(),new[]{"draw.exe"}),"running rule works without media");
Check(!SuppressionPolicy.IsMatch(ruleRunning,new[]{"draw.exe"},Array.Empty<string>()),"running rule requires process evidence");
var suppressEngine=new ReminderEngine();var suppressSettings=new Settings{IdleMinutes=1,RepeatMinutes=1};
suppressEngine.Reset(0);
Check(!suppressEngine.Due(120000,120000,false,suppressSettings,true)&&suppressEngine.LastReminder==null,"suppression does not consume reminder or idle time");
Check(suppressEngine.Due(121000,121000,false,suppressSettings,false),"suppression release uses actual idle time");
Check(!suppressEngine.Due(122000,122000,false,suppressSettings,false),"release does not burst catch up");
Check(suppressEngine.Due(181000,181000,false,suppressSettings,false),"release repeat follows configured interval");
suppressEngine.Due(182000,0,true,suppressSettings,true);
Check(!suppressEngine.Due(200000,18000,false,suppressSettings),"input during suppression resets idle");
var once=new ReminderEngine();var onceSettings=new Settings{IdleMinutes=1,Repeat=false};once.Reset(0);
Check(!once.Due(100000,100000,false,onceSettings,true)&&once.Due(101000,101000,false,onceSettings),"single reminder retained during suppression");
once.CancelScheduledReminder(101000,null);
Check(once.Due(102000,102000,false,onceSettings),"canceled preparation does not consume single reminder");
once.Reset(103000);once.CancelScheduledReminder(102000,50000);
Check(once.LastReminder==null,"old preparation cannot restore history after input reset");
var ruleConfig=new Settings{SuppressionRules=new(){rulePlaying,ruleRunning}};
var rulesReloaded=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(ruleConfig))!;
Check(rulesReloaded.SuppressionRules.Count==2&&rulesReloaded.SuppressionRules[1].Mode==SuppressionMode.WhileRunning,"suppression rules persist independently");
bool invalidRunning=false;try{SuppressionPolicy.Validate(new(){Application="Package!App",Mode=SuppressionMode.WhileRunning});}catch(ArgumentException){invalidRunning=true;}
Check(invalidRunning,"package id cannot masquerade as process name");
var showOptions=new SlideShowOptions();
Check(!SlideShowPolicy.IsShowWindow("POWERPNT.EXE","PPTFrameClass",true,showOptions),"PowerPoint editing window does not suppress");
Check(SlideShowPolicy.IsShowWindow("POWERPNT.EXE","screenClass",true,showOptions),"PowerPoint dedicated show window suppresses independent of size");
Check(!SlideShowPolicy.IsShowWindow("game.exe","screenClass",true,showOptions),"game never matches presentation show class");
Check(!SlideShowPolicy.IsShowWindow("POWERPNT.EXE","screenClass",false,showOptions),"hidden show window is not a native match");
Check(!SlideShowPolicy.IsShowWindow("wpp.exe","UnknownWpsClass",true,showOptions),"unknown WPS window falls back to normal reminders");
Check(!SlideShowPolicy.IsShowWindow("wpp.exe","screenClass",true,showOptions),"PowerPoint class is not guessed for WPS");
showOptions.WindowRules.Add(new(){ProcessName="wpp.exe",WindowClass="UserConfirmedShowWindow"});
Check(SlideShowPolicy.IsShowWindow("WPP.EXE","UserConfirmedShowWindow",true,showOptions),"WPS confirmed exact class exception");
Check(!SlideShowPolicy.IsShowWindow("wpp.exe","UserConfirmedEditorWindow",true,showOptions),"WPS editing does not match specific show exception");
Check(!SlideShowPolicy.IsShowWindow("other.exe","UserConfirmedShowWindow",true,showOptions),"manual exception cannot match other software");
showOptions.Wps=false;
Check(!SlideShowPolicy.IsShowWindow("wpp.exe","UserConfirmedShowWindow",true,showOptions),"per application exception disables WPS recognition");
showOptions.Enabled=false;
Check(!SlideShowPolicy.IsShowWindow("powerpnt.exe","screenClass",true,showOptions),"master slide switch disabled");
bool rejectsGame=false;try{SlideShowPolicy.Validate(new(){ProcessName="game.exe",WindowClass="screenClass"});}catch(ArgumentException){rejectsGame=true;}
Check(rejectsGame,"manual rules restricted to presentation processes");
var showSaved=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings{SlideShow=showOptions}))!;
Check(!showSaved.SlideShow.Enabled&&showSaved.SlideShow.WindowRules.Count==1,"slideshow options and manual rules roundtrip");
var slideTimer=new ReminderEngine();slideTimer.Reset(0);var slideConfig=new Settings{IdleMinutes=1};
Check(!slideTimer.Due(120000,120000,false,slideConfig,true),"show suppresses reminder without consuming history");
Check(slideTimer.Due(121000,121000,false,slideConfig),"leaving show resumes actual idle timing");
Check(!slideTimer.Due(122000,122000,false,slideConfig),"show exit does not burst repeated reminders");
Check(RuleStatusText.Get(false,true,false,false,false,"提醒功能已启用")=="正在检查应用提醒规则","first rule check may show initial check state");
Check(RuleStatusText.Get(false,true,true,false,false,"提醒功能已启用")=="提醒功能已启用","background refresh keeps enabled status");
Check(RuleStatusText.Get(false,false,true,false,false,"提醒功能已启用")=="提醒功能已启用","completed refresh matches in-progress stable status");
Check(RuleStatusText.Get(true,true,true,true,false,"提醒功能已启用")=="幻灯片放映中，暂停闲置提醒","slideshow suppression stays visible while refreshing");
Check(RuleStatusText.Get(true,true,true,false,false,"提醒功能已启用")=="按应用规则暂停闲置提醒","application suppression stays visible while refreshing");
Check(RuleStatusText.Get(false,true,true,false,true,"提醒功能已启用")=="正在等待用户恢复操作","resume waiting state not hidden by routine check");
Check(RuleStatusText.Get(false,true,true,false,false,"提醒功能已启用；诊断信息")=="提醒功能已启用；诊断信息","stable status preserves diagnostic details");
var checkboxColors=new CheckBoxPalette();
Check(checkboxColors.Colors["Checkmark"]=="#FFFFFF"&&checkboxColors.Colors["CheckedFill"]=="#0078D4","default checkbox blue background white mark");
var colorCopy=checkboxColors.Copy();colorCopy.Colors["Checkmark"]="#FFFF00";
Check(checkboxColors.Colors["Checkmark"]=="#FFFFFF","color draft independent from saved settings");
colorCopy.Colors["CheckedFill"]="bad";colorCopy.Validate();
Check(colorCopy.Colors["CheckedFill"]=="#0078D4","invalid color falls back safely");
var coloredSettings=new Settings{CheckBoxColors=colorCopy};
var colorReloaded=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(coloredSettings))!;
colorReloaded.Validate();Check(colorReloaded.CheckBoxColors.Colors["Checkmark"]=="#FFFF00","editable checkbox colors persist");
Check(!VolumePolicy.SkipBoostForMedia(true,true),"allow boost with allowlist when no protected playing media");
Check(VolumePolicy.SkipBoostForMedia(true,true,protectedPlaying:1),"protect unlisted playing media from system volume raise");
Check(VolumePolicy.SkipBoostForMedia(true,true,failed:1),"pause failures block system volume boost");
Check(VolumePolicy.SkipBoostForMedia(true,false),"unknown or timed out media result blocks boost");
Check(VolumePolicy.SkipBoostForMedia(true,true,unsupportedPlaying:1),"playing nonpausable media blocks boost");
Check(!VolumePolicy.SkipBoostForMedia(false,false),"disabled media pause does not silently disable configured volume feature");
var unpausableLive=new FakeMedia{CanPause=false};
var unpausableStopped=new FakeMedia{CanPause=false,IsPlaying=false};
var unpausedResult=await MediaPausePolicy.PauseAsync(new[]{unpausableLive,unpausableStopped},CancellationToken.None);
Check(unpausedResult.UnsupportedPlaying==1,"count only playing unsupported sessions for volume safety");
var readyConfig=new DeviceVolumeSetting{Enabled=true,TargetPercent=40,ConfirmedTarget=40};
Check(VolumePolicy.BlockReason(true,false,.1f,readyConfig)==null&&VolumePolicy.CanRaise(true,false,.1f,readyConfig),"configured 40 percent speaker eligible from 10 percent");
Check(VolumePolicy.BlockReason(true,false,.4f,readyConfig)!.Contains("已达到"),"explain target already reached");
Check(VolumePolicy.BlockReason(true,false,.1f,null)!.Contains("未配置"),"explain missing endpoint configuration");
Check(VolumePolicy.BlockReason(true,true,.1f,readyConfig)!.Contains("静音"),"explain muted device without unmuting");
Check(SkinPresetDocument.IsSafeFilename("茶绿.json"),"unicode preset filename accepted");
Check(!SkinPresetDocument.IsSafeFilename("../outside.json")&&!SkinPresetDocument.IsSafeFilename(@"C:\outside.json"),"preset path escape rejected");
var skinDoc=new SkinPresetDocument();skinDoc.Colors["Text"]="#24302B";skinDoc.Validate();
var skinJson=System.Text.Json.JsonSerializer.Serialize(skinDoc);
var reloadedSkin=System.Text.Json.JsonSerializer.Deserialize<SkinPresetDocument>(skinJson)!;reloadedSkin.Validate();
Check(reloadedSkin.Colors["Text"]=="#24302B"&&reloadedSkin.Colors.Count==SkinPresetDocument.Defaults().Count,"designer compatible theme roundtrip");
Check(!ControlPalette.TabFallbacks.Keys.Any(reloadedSkin.Colors.ContainsKey),"legacy skin leaves native tab colors active");
reloadedSkin.Colors["TabSelectedBackground"]="#274C77";
reloadedSkin.Validate();
Check(reloadedSkin.Colors["TabSelectedBackground"]=="#274C77"&&
      reloadedSkin.Colors["TabSelectedText"]=="#24302B"&&
      reloadedSkin.Colors["TabUnselectedBackground"]==reloadedSkin.Colors["Button"],
      "partial tab palette preserves custom colors and fills missing values");
reloadedSkin.Colors["TabUnselectedText"]="invalid";
bool invalidTabColor=false;
try{reloadedSkin.Validate();}catch(ArgumentException){invalidTabColor=true;}
Check(invalidTabColor,"invalid tab color rejected");
bool invalidPreset=false;try{var bad=new SkinPresetDocument();bad.Colors["Window"]="invalid";bad.Validate();}catch(ArgumentException){invalidPreset=true;}
Check(invalidPreset,"invalid preset cannot be applied");
var selectedSkin=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings{SkinPreset="茶绿.json"}))!;
Check(selectedSkin.SkinPreset=="茶绿.json","selected skin filename persists");
var legacyScheme=new SkinPresetDocument();foreach(var key in ControlPalette.Fallbacks.Keys)legacyScheme.Colors.Remove(key);
legacyScheme.Colors["Accent"]="#365C4C";legacyScheme.Validate();
Check(legacyScheme.Colors["SelectionBackground"]=="#365C4C"&&legacyScheme.Colors["SliderThumb"]=="#365C4C","legacy schemes inherit selection and thumb from existing accent");
legacyScheme.Colors["SelectionBackground"]="#123456";legacyScheme.Colors["SliderThumb"]="#ABCDEF";legacyScheme.Validate();
Check(legacyScheme.Colors["SelectionBackground"]=="#123456"&&legacyScheme.Colors["SliderThumb"]=="#ABCDEF","explicit option and slider overrides preserved");
Check(CheckmarkPath.IsSafe(null)&&CheckmarkPath.IsSafe("assets/checkmark-123.png"),"optional local PNG path accepted");
Check(!CheckmarkPath.IsSafe("../outside.png")&&!CheckmarkPath.IsSafe("assets/../../outside.png"),"image traversal rejected");
Check(!CheckmarkPath.IsSafe("https://example.com/icon.png")&&!CheckmarkPath.IsSafe(@"C:\icon.png"),"remote and absolute image paths rejected");
var imageSkin=new SkinPresetDocument{CheckmarkImage="assets/checkmark-123.png"};
var imageSkinRoundtrip=System.Text.Json.JsonSerializer.Deserialize<SkinPresetDocument>(System.Text.Json.JsonSerializer.Serialize(imageSkin))!;
imageSkinRoundtrip.Validate();Check(imageSkinRoundtrip.CheckmarkImage==imageSkin.CheckmarkImage,"PNG resource reference persists");
Check(AudioSessionPolicy.CanBoost(true,false),"known quiet endpoint permits boost");
Check(!AudioSessionPolicy.CanBoost(false,false),"unknown endpoint cannot boost");
Check(!AudioSessionPolicy.CanBoost(true,true),"other active stream blocks boost regardless GSMTC result");
Check(AudioSessionPolicy.HasOtherActive(new[]{new AudioSessionState(1,42,0)},99),"music absent from GSMTC still blocks through Core Audio");
Check(!AudioSessionPolicy.HasOtherActive(new[]{new AudioSessionState(1,99,0)},99),"own reminder stream does not block itself");
Check(AudioSessionPolicy.HasOtherActive(new[]{new AudioSessionState(1,99,0x0889000D)},99),"multi-process stream never treated as own only");
Check(AudioSessionPolicy.HasOtherActive(new[]{new AudioSessionState(1,0,0)},99),"system audio counts as other stream");
Check(!AudioSessionPolicy.HasOtherActive(new[]{new AudioSessionState(0,42,0),new AudioSessionState(2,42,0)},99),"inactive and expired streams do not block");
Check(DuckingPolicy.Matches(@"D:\Players\music.exe",new[]{"MUSIC.EXE"}),"duck matches exact executable name case insensitively");
Check(!DuckingPolicy.Matches(@"D:\Players\music.exe",new[]{@"C:\Players\music.exe"}),"duck full path does not match other location");
Check(!DuckingPolicy.Matches(@"D:\Players\music.exe",new[]{"music"}),"duck never substring matches a process");
Check(Math.Abs(DuckingPolicy.Target(.5f,20)-.1f)<.0001f,"duck retains relative original volume instead of fixed global value");
var duck=new FakeDuckVolume();var duckLease=new DuckingAdjustment(duck,20);
Check(Math.Abs(duck.Volume-.1f)<.0001f,"duck applies volume");duckLease.Dispose();
Check(Math.Abs(duck.Volume-.5f)<.0001f&&duckLease.Restored,"duck restores original on disposal");
duckLease.Dispose();Check(duck.Disposals==1&&duck.Writes==2,"duck disposal idempotent");
var manualDuck=new FakeDuckVolume();using(var adjust=new DuckingAdjustment(manualDuck,20)){manualDuck.Manual(.3f);}
Check(Math.Abs(manualDuck.Volume-.3f)<.0001f,"duck respects manual volume change");
var backDuck=new FakeDuckVolume();using(var adjust=new DuckingAdjustment(backDuck,20)){backDuck.Manual(.3f);backDuck.Manual(.1f);}
Check(Math.Abs(backDuck.Volume-.1f)<.0001f,"duck respects user changes even when final value equals applied");
var muteDuck=new FakeDuckVolume();using(var adjust=new DuckingAdjustment(muteDuck,20)){muteDuck.Muted=true;muteDuck.Manual(.1f);}
Check(muteDuck.Muted&&Math.Abs(muteDuck.Volume-.1f)<.0001f,"duck respects user mute");
var initiallyMuted=new FakeDuckVolume{Muted=true};using(var adjust=new DuckingAdjustment(initiallyMuted,20)){}
Check(initiallyMuted.Writes==0,"duck never unmutes or modifies muted session");
var noDuck=new FakeDuckVolume();using(var adjust=new DuckingAdjustment(noDuck,100)){}
Check(noDuck.Writes==0,"100 percent never alters volume");
var failedDuck=new FakeDuckVolume{FailNextWrite=true};bool failedApply=false;try{new DuckingAdjustment(failedDuck,20);}catch(InvalidOperationException){failedApply=true;}
Check(failedApply&&failedDuck.Disposals==1&&Math.Abs(failedDuck.Volume-.5f)<.0001f,"failed duck apply releases resource without overwriting original");
var duckSettings=new Settings{DuckMediaOnReminder=true,DuckRemainingPercent=15};var duckRoundtrip=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(duckSettings))!;
Check(duckRoundtrip.DuckMediaOnReminder&&duckRoundtrip.DuckRemainingPercent==15,"duck settings persist");
duckSettings.DuckRemainingPercent=-5;duckSettings.Validate();Check(duckSettings.DuckRemainingPercent==1,"duck percentage validated");
Check(DuckingPolicy.Matches(@"D:\Player\music.exe",new[]{"music.exe"})&&!MediaApplicationFilter.ShouldPause("music.exe",MediaPauseScope.ExceptListed,new[]{"music.exe"}),"pause-exempt music can still be attenuated independently");
Check(!DuckingPolicy.Matches(@"D:\Player\game.exe",new[]{"music.exe"}),"all-pause scope never ducks an unlisted game");
Check(DuckingPolicy.Matches(@"D:\Player\music.exe",new[]{"music.exe"}),"listed music eligible for fallback attenuation");
Check(DuckingPolicy.MayAttemptBoost(false,false,false),"normal safe-device boost remains available without music");
Check(!DuckingPolicy.MayAttemptBoost(true,false,false),"failed pause without duck never enables boost");
Check(!DuckingPolicy.MayAttemptBoost(true,true,false),"unverified duck cannot override failed pause");
Check(DuckingPolicy.MayAttemptBoost(true,true,true),"verified duck allows fresh main volume safety evaluation");
var permits=new DuckedSessionRegistry();Check(!permits.IsVerified,"empty duck registry cannot authorize boost");
var permitVolume=new FakeDuckVolume();using(var reduction=new DuckingAdjustment(permitVolume,20)){
 permits.Add("session-A",reduction);
 Check(permits.IsVerified&&permits.Allows("session-A"),"exact reduced session eligible for boost exemption");
 Check(!permits.Allows("session-B"),"new session from same player gets no exemption");
 permitVolume.Manual(.4f);Check(!permits.IsVerified&&!permits.Allows("session-A"),"manual music volume change revokes boost permit");
 permitVolume.Manual(.1f);Check(!permits.Allows("session-A"),"return to ducked volume never revives revoked permit");
}
Check(!permits.Allows("session-A"),"disposed duck cannot authorize boost");
var ineffectiveVolume=new FakeDuckVolume();using(var unchanged=new DuckingAdjustment(ineffectiveVolume,100)){
 var unverified=new DuckedSessionRegistry();unverified.Add("unchanged",unchanged);
 Check(!unverified.IsVerified&&!unverified.Allows("unchanged"),"unchanged audio session is not exempted");
}
Check(DuckingPolicy.MayIgnoreOtherAudio(true,true,true),"user opted in permits other known sessions after verified duck");
Check(!DuckingPolicy.MayIgnoreOtherAudio(false,true,true),"strict default still blocks other sessions");
Check(!DuckingPolicy.MayIgnoreOtherAudio(true,false,true),"opt-in never bypasses lost duck protection");
Check(!DuckingPolicy.MayIgnoreOtherAudio(true,true,false),"failed session query remains conservative even with opt-in");
var coordinatedSettings=new Settings{AllowBoostWithOtherAudioAfterDucking=true};
Check(System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(coordinatedSettings))!.AllowBoostWithOtherAudioAfterDucking,"coordination opt-in persists");
var legacyDucking=new Settings{MediaScope=MediaPauseScope.ExceptListed,MediaApplications=new(){"music.exe","Store.App"}};legacyDucking.Validate();
Check(legacyDucking.DuckApplications!.SequenceEqual(new[]{"music.exe"}),"legacy executable list migrates once independently of pause scope");
legacyDucking.MediaApplications.Clear();legacyDucking.Validate();Check(legacyDucking.DuckApplications.SequenceEqual(new[]{"music.exe"}),"changing pause list never overwrites migrated duck list");
var explicitlyEmpty=new Settings{DuckApplications=new(),MediaApplications=new(){"music.exe"}};explicitlyEmpty.Validate();Check(explicitlyEmpty.DuckApplications.Count==0,"explicit empty duck list never inherits pause list again");
var independentRoundtrip=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(legacyDucking))!;
Check(independentRoundtrip.DuckApplications!.SequenceEqual(new[]{"music.exe"})&&independentRoundtrip.MediaApplications.Count==0,"both lists persist independently");
Check(DuckingPolicy.MayIgnoreOtherAudio(true,true,0x0889000D>=0),"successful shared-session query does not abort opted-in reminder playback");
Check(!DuckingPolicy.MayIgnoreOtherAudio(false,true,0x0889000D>=0),"strict mode still blocks shared sessions");
Check(Math.Abs(DuckingPolicy.CompensatedTarget(.15f,20)-.015f)<.00001f,"20 dB master gain compensated by tenfold additional attenuation");
Check(Math.Abs(DuckingPolicy.CompensatedTarget(.15f,0)-.15f)<.00001f,"no master gain preserves requested duck ratio");
Check(Math.Abs(DuckingPolicy.CompensatedTarget(.15f,-10)-.15f)<.00001f,"master reduction never causes music amplification");
var compensated=new FakeDuckVolume();using(var adjustment=new DuckingAdjustment(compensated,20)){
 adjustment.StageForMasterBoost();Check(compensated.Volume==0&&adjustment.StillApplied,"stage music at zero while master gain changes");
 adjustment.CompensateMasterGain(20);Check(Math.Abs(compensated.Volume-.01f)<.00001f&&adjustment.StillApplied,"music restored to compensated duck level before reminder starts");
}Check(Math.Abs(compensated.Volume-.5f)<.0001f,"compensated duck still restores original application volume");
var canceledBoost=new FakeDuckVolume();using(var adjustment=new DuckingAdjustment(canceledBoost,20)){adjustment.StageForMasterBoost();adjustment.CancelMasterBoost();Check(Math.Abs(canceledBoost.Volume-.1f)<.0001f,"failed master boost returns to base attenuation after restoring master");}
var manualCompensation=new FakeDuckVolume();using(var adjustment=new DuckingAdjustment(manualCompensation,20)){
 adjustment.StageForMasterBoost();manualCompensation.Manual(.3f);bool refused=false;try{adjustment.CompensateMasterGain(20);}catch(InvalidOperationException){refused=true;}
 Check(refused&&Math.Abs(manualCompensation.Volume-.3f)<.0001f,"compensation never overrides external music volume change");
}
var unsafeRestore=new FakeDuckVolume();using(var adjustment=new DuckingAdjustment(unsafeRestore,20)){adjustment.PreserveCurrent();}
Check(Math.Abs(unsafeRestore.Volume-.1f)<.0001f,"failed master restoration can preserve attenuated music instead of restoring it loudly");
Check(FadeInPolicy.Gain(.8,0)==0,"reminder fade starts silently");
Check(Math.Abs(FadeInPolicy.Gain(.8,500)-.4)<.000001,"reminder fade halfway scales configured application volume");
Check(Math.Abs(FadeInPolicy.Gain(.8,1000)-.8)<.000001,"reminder fade reaches requested volume in one second");
Check(FadeInPolicy.Gain(.8,5000)==.8,"reminder fade never exceeds configured volume");
Check(FadeInPolicy.Gain(0,1000)==0&&FadeInPolicy.Gain(.8,-10)==0,"muted target and pre-start fade stay silent");
Check(StartupCommand.For(@"D:\IdleReminder\publish\current\IdleReminder.exe")==@"""D:\IdleReminder\publish\current\IdleReminder.exe"" --background","startup command quotes complete executable path");
Check(!StartupCommand.Matches("\"D:\\IdleReminder\\publish\\win-x64\\IdleReminder.exe\" --background",@"D:\IdleReminder\publish\current\IdleReminder.exe"),"old startup path detected despite enabled registry value");
Check(!StartupCommand.Matches(null,@"D:\IdleReminder\publish\current\IdleReminder.exe"),"missing startup command does not match enabled path");
Check(StartupCommand.Matches(StartupCommand.For(@"D:\IdleReminder\publish\current\IdleReminder.exe"),@"d:\idlereminder\publish\current\idlereminder.exe"),"startup path comparison ignores Windows case");
Check(VolumePolicy.CanConfirmPreview(true,true,true,true),"complete verified preview permits explicit confirmation");
Check(!VolumePolicy.CanConfirmPreview(false,true,true,true),"stopped or failed preview cannot be confirmed");
Check(!VolumePolicy.CanConfirmPreview(true,false,true,true),"blocked volume adjustment cannot be confirmed");
Check(!VolumePolicy.CanConfirmPreview(true,true,false,true)&&!VolumePolicy.CanConfirmPreview(true,true,true,false),"device or target change invalidates preview confirmation");
Check(VolumePolicy.HasConfirmedConsent(new(){Enabled=true,TargetPercent=35,ConfirmedTarget=35}),"enabled confirmed speaker config authorizes standalone master boost");
Check(!VolumePolicy.HasConfirmedConsent(new(){Enabled=false,TargetPercent=35,ConfirmedTarget=35}),"disabled device never authorizes standalone boost");
Check(!VolumePolicy.HasConfirmedConsent(new(){Enabled=true,TargetPercent=40,ConfirmedTarget=35}),"changed unconfirmed target never authorizes boost");
Check(!VolumePolicy.HasConfirmedConsent(null),"unconfigured device never authorizes boost");
var fixture=Path.Combine(@"D:\IdleReminder\tests\config-fixtures",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fixture);
var file=Path.Combine(fixture,"settings.json");var store=new ConfigFileStore(file);var cfgLoaded=store.Load();cfgLoaded.MediaApplications=new(){"cloudmusic.exe"};store.Save(cfgLoaded);cfgLoaded.Volume=37;store.Save(cfgLoaded);
Check(File.Exists(file+".bak")&&System.Text.Json.JsonSerializer.Deserialize<Settings>(File.ReadAllText(file+".bak"))!.Volume!=37,"last valid configuration backed up before replacement");
var draft=cfgLoaded.Copy();draft.MediaApplications.Clear();draft.Volume=12;Check(cfgLoaded.MediaApplications.Count==1&&cfgLoaded.Volume==37,"settings draft deep copy isolates lists and values");
var sentinel=Path.Combine(fixture,"voice-01234567890123456789012345678901.wav");File.WriteAllText(sentinel,"private audio sentinel");File.WriteAllText(file,"broken-json");var brokenStore=new ConfigFileStore(file);brokenStore.Load();bool cfgRefused=false;try{brokenStore.Save(new());}catch(InvalidOperationException){cfgRefused=true;}
Check(cfgRefused&&File.ReadAllText(file)=="broken-json"&&File.Exists(sentinel),"load failure blocks overwriting and preserves imported audio");
File.WriteAllText(file,"{\"ConfigurationVersion\":999}");var futureStore=new ConfigFileStore(file);futureStore.Load();Check(futureStore.WriteBlocked,"future format blocks destructive downgrade");
File.WriteAllText(file,"{\"ExtraSetting\":{\"value\":42}}");var extraStore=new ConfigFileStore(file);var extra=extraStore.Load();extraStore.Save(extra);using(var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(file)))Check(document.RootElement.GetProperty("ExtraSetting").GetProperty("value").GetInt32()==42,"unknown top-level fields preserved through save");
var externalStore=new ConfigFileStore(file);var external=externalStore.Load();File.WriteAllText(file,"{\"Volume\":9}");cfgRefused=false;try{externalStore.Save(external);}catch(IOException){cfgRefused=true;}Check(cfgRefused&&File.ReadAllText(file)=="{\"Volume\":9}","external write detected rather than silently overwritten");
var lockedStore=new ConfigFileStore(file);var locked=lockedStore.Load();var bytesBefore=File.ReadAllBytes(file);using(var handle=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read)){cfgRefused=false;try{lockedStore.Save(locked);}catch(IOException){cfgRefused=true;}}
Check(cfgRefused&&File.ReadAllBytes(file).SequenceEqual(bytesBefore),"failed replace leaves original settings intact");
File.WriteAllText(file,"{\"DeviceVolumes\":{\"speaker\":null}}");var invalidStore=new ConfigFileStore(file);invalidStore.Load();Check(invalidStore.WriteBlocked,"invalid nested field blocks automatic reset and writeback");
var bomFile=Path.Combine(fixture,"bom.json");File.WriteAllText(bomFile,"{\"MediaApplications\":[\"cloudmusic.exe\"]}",new System.Text.UTF8Encoding(true));var bomStore=new ConfigFileStore(bomFile);var bomConfig=bomStore.Load();Check(!bomStore.WriteBlocked&&bomConfig.MediaApplications.Contains("cloudmusic.exe"),"legacy UTF8 BOM settings load without false corruption detection");
Check(DuckingPolicy.HasDeviceAuthorization(true,true,true),"no active duck session does not revoke enabled coordination consent");
Check(!DuckingPolicy.HasDeviceAuthorization(true,true,false),"manual coordination opt-out still honored when duck enabled");
Check(DuckingPolicy.HasDeviceAuthorization(true,false,false),"standalone confirmed device consent remains valid");
Check(!DuckingPolicy.HasDeviceAuthorization(false,true,true),"unconfirmed or disabled device never authorized");
Check(DuckingPolicy.PermitAfterDucking(true,false,false),"no target music needed still permits confirmed master boost");
Check(!DuckingPolicy.PermitAfterDucking(true,true,true),"known partial duck failure blocks master boost despite consent");
var rampVolume=new FakeDuckVolume();using(var ramp=new DuckingAdjustment(rampVolume,20)){
 ramp.RestoreGradually(0);Check(Math.Abs(rampVolume.Volume-.1f)<.0001f,"music restoration starts at attenuated level");
 ramp.RestoreGradually(.5);Check(Math.Abs(rampVolume.Volume-.3f)<.0001f,"music restoration progresses smoothly through middle");
 ramp.RestoreGradually(1);Check(Math.Abs(rampVolume.Volume-.5f)<.0001f,"music restoration reaches original application volume");
}Check(rampVolume.Disposals==1,"gradual restore still releases session exactly once");
var rampManual=new FakeDuckVolume();using(var ramp=new DuckingAdjustment(rampManual,20)){ramp.RestoreGradually(.2);rampManual.Manual(.22f);ramp.RestoreGradually(1);}Check(Math.Abs(rampManual.Volume-.22f)<.0001f,"manual application change cancels gradual restore and final overwrite");
var rampMute=new FakeDuckVolume();using(var ramp=new DuckingAdjustment(rampMute,20)){rampMute.Muted=true;ramp.RestoreGradually(.5);}Check(rampMute.Muted&&Math.Abs(rampMute.Volume-.1f)<.0001f,"mute cancels gradual restore without amplification");
Check(!new PhoneOptions().Enabled,"phone defaults disabled");
var phonePath=@"D:\IdleReminder\tests\kdeconnect-cli.exe";
Check(PhonePolicy.ValidPath(phonePath),"phone accepts explicit official executable name");
Check(!PhonePolicy.ValidPath(@"\\server\share\kdeconnect-cli.exe"),"phone rejects UNC executable");
Check(!PhonePolicy.ValidPath("kdeconnect-cli.exe"),"phone rejects relative executable");
Check(!PhonePolicy.ValidPath(@"D:\other.exe"),"phone rejects other executable names");
Check(!PhonePolicy.ValidId("--device x")&&!PhonePolicy.ValidId("id\n--ring"),"phone rejects malformed target");
var parsedPhones=PhonePolicy.ParseDevices("chosen_id 手机 一\nchosen_id 重复\nother_id Phone Two\ninvalid/id Bad\n");
Check(parsedPhones.Count==2&&parsedPhones[0].Name=="手机 一","phone preserves names and deduplicates targets");
var phoneSettings=new Settings{Phone=new(){Enabled=true,CliPath=phonePath,DeviceId="chosen_id",DeviceName="手机一"}};
phoneSettings.Validate();var phoneCopy=phoneSettings.Copy();
Check(phoneCopy.Phone.Enabled&&phoneCopy.Phone.DeviceId=="chosen_id","phone settings roundtrip");
phoneCopy.Phone.DeviceId="other_id";Check(phoneSettings.Phone.DeviceId=="chosen_id","phone settings draft independent");
var oldPhoneSettings=System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!;oldPhoneSettings.Validate();
Check(!oldPhoneSettings.Phone.Enabled,"old config migration leaves phone off");
var fakePhone=new FakePhoneRunner();var phoneService=new PhoneNotifications(fakePhone);
var disabledPhone=await phoneService.SendAsync(new(),false,CancellationToken.None);
Check(fakePhone.Calls.Count==0&&disabledPhone.Contains("关闭"),"disabled phone launches nothing");
var requestedPhone=phoneSettings.Phone.Copy();
var sentPhone=await phoneService.SendAsync(requestedPhone,false,CancellationToken.None);
Check(sentPhone.Contains("已提交")&&fakePhone.Calls.Count==2,"phone submits only after availability check");
Check(fakePhone.Calls[0].SequenceEqual(new[]{"--list-available","--id-name-only"}),"phone only queries paired reachable devices");
Check(fakePhone.Calls[1].SequenceEqual(new[]{"--device","chosen_id","--ping-msg",PhonePolicy.Message}),"phone sends fixed private message only to saved target");
fakePhone.Calls.Clear();fakePhone.Handler=(args,token)=>Task.FromResult(new PhoneCommandResult(0,"other_id 其他手机\n"));
var offlinePhone=await phoneService.SendAsync(requestedPhone,false,CancellationToken.None);
Check(offlinePhone.Contains("未连接")&&fakePhone.Calls.Count==1,"offline target skips without selecting another phone");
fakePhone.Calls.Clear();fakePhone.Handler=(args,token)=>Task.FromResult(new PhoneCommandResult(2,""));
try{await phoneService.SendAsync(requestedPhone,false,CancellationToken.None);Check(false,"failed discovery rejected");}catch(IOException){Check(fakePhone.Calls.Count==1,"failed discovery never sends");}
var blockPhone=new TaskCompletionSource<PhoneCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
fakePhone.Handler=(args,token)=>blockPhone.Task.WaitAsync(token);
using(var phoneCancel=new CancellationTokenSource()){
 var pendingPhone=phoneService.SendAsync(requestedPhone,false,phoneCancel.Token);
 var duplicatePhone=await phoneService.SendAsync(requestedPhone,false,CancellationToken.None);
 Check(duplicatePhone.Contains("跳过"),"concurrent phone send cannot duplicate");
 phoneCancel.Cancel();try{await pendingPhone;Check(false,"phone cancellation expected");}catch(OperationCanceledException){Check(true,"phone cancellation releases in-flight operation");}
}
fakePhone.Handler=(args,token)=>Task.FromResult(new PhoneCommandResult(0,"chosen_id 手机一\n"));
requestedPhone.Enabled=false;fakePhone.Calls.Clear();
await phoneService.SendAsync(requestedPhone,true,CancellationToken.None);
Check(fakePhone.Calls.Count==2&&fakePhone.Calls[1][3].Contains("测试"),"explicit phone test allowed while reminders integration disabled");
foreach(bool appSwitch in new[]{false,true})foreach(bool slideSwitch in new[]{false,true}){
 var configured=new Settings{AppSuppressionEnabled=appSwitch,SlideShow=new(){Enabled=slideSwitch,PowerPoint=true,Wps=false},SuppressionRules=new(){new(){Application="game.exe",Mode=SuppressionMode.WhileRunning},new(){Application="player.exe",Mode=SuppressionMode.WhilePlaying}}};
 var plan=SuppressionPolicy.Capture(configured);
 Check(plan.Rules.Length==(appSwitch?2:0),"application switch filters both running and playing rules");
 Check(plan.Slides.Enabled==slideSwitch,"slideshow switch independent of application master");
 bool show=SlideShowPolicy.IsShowWindow("POWERPNT.EXE","screenClass",true,plan.Slides);
 Check(show==slideSwitch,"PowerPoint show remains suppressed when app master off");
 Check(plan.NeedsCheck==(appSwitch||slideSwitch),"no detectors required with both switches off");
 Check(configured.SuppressionRules.Count==2,"master leaves saved rules intact");
 plan.Slides.Enabled=!slideSwitch;Check(configured.SlideShow.Enabled==slideSwitch,"slideshow snapshot independent");
 if(plan.Rules.Length>0){plan.Rules[0].Application="changed.exe";Check(configured.SuppressionRules[0].Application=="game.exe","application snapshot independent");}
}
var masterSettings=new Settings{AppSuppressionEnabled=false,SlideShow=new(){Enabled=true}};
var masterCopy=masterSettings.Copy();Check(!masterCopy.AppSuppressionEnabled&&masterCopy.SlideShow.Enabled,"master off and slideshow on persist together");
masterCopy.PrepareForLaunch();Check(!masterCopy.AppSuppressionEnabled&&masterCopy.SlideShow.Enabled,"startup preserves suppression switches");
var oldMaster=System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!;oldMaster.Validate();Check(oldMaster.AppSuppressionEnabled,"old configs preserve prior application rule behavior");
var offPlan=SuppressionPolicy.Capture(new(){AppSuppressionEnabled=false,SlideShow=new(){Enabled=false},SuppressionRules=new(){new(){Application="game.exe",Mode=SuppressionMode.WhileRunning}}});
var offEngine=new ReminderEngine();offEngine.Reset(0);var offSettings=new Settings{AppSuppressionEnabled=false,SlideShow=new(){Enabled=false}};
Check(offEngine.Due(1200000,1200000,false,offSettings,offPlan.NeedsCheck),"master off allows ordinary idle reminder despite stored game rule");
offSettings.Enabled=false;Check(!offEngine.Due(2400000,2400000,false,offSettings,false),"master off never bypasses manual pause");
offSettings.Enabled=true;offEngine.Suspend(2400000);Check(!offEngine.Due(3600000,3600000,false,offSettings,false),"master off never bypasses sleep");
var masterFile=Path.Combine(fixture,"app-master.json");var masterStore=new ConfigFileStore(masterFile);masterStore.Load();masterStore.Save(masterSettings);
var masterReload=new ConfigFileStore(masterFile).Load();Check(!masterReload.AppSuppressionEnabled&&masterReload.SlideShow.Enabled,"atomic config save preserves independent switches");
var headerClock=new HeaderActivityClock(1000);
Check(headerClock.Read(1100).IdleMilliseconds==100,"header clock starts a fresh idle period");
Check(!headerClock.Observe(2,1200)&&headerClock.Read(1300).Stamp==0,"non keyboard mouse header ignored");
Check(headerClock.Observe(0,1400)&&headerClock.Read(1500).IdleMilliseconds==100,"mouse header resets device clock");
var mouseStamp=headerClock.Read(1500).Stamp;
Check(headerClock.Observe(1,1600)&&headerClock.Read(1600).Stamp!=mouseStamp,"keyboard header increments activity revision");
Check(headerClock.Read(1599).IdleMilliseconds==0,"device clock avoids negative idle");
var firstGeneration=InputTimingPolicy.WithGeneration(new(0,100),1);
var secondGeneration=InputTimingPolicy.WithGeneration(new(0,100),2);
Check(InputTimingPolicy.Changed(firstGeneration,secondGeneration),"source switch invalidates equal low stamps");
Check(PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Reminder,firstGeneration.Stamp,secondGeneration.Stamp),"source change invalidates playback baseline");
var deviceClock=new HeaderActivityClock(0);var deviceEngine=new ReminderEngine();deviceEngine.Reset(0);
var deviceSettings=new Settings{IdleMinutes=2,InputTiming=InputTimingMode.DeviceHeaders,SlideShow=new(){Enabled=false}};
Check(!deviceEngine.Due(50000,deviceClock.Read(50000).IdleMilliseconds,false,deviceSettings),"device reminder before threshold");
Check(!deviceEngine.Due(100000,deviceClock.Read(100000).IdleMilliseconds,false,deviceSettings),"software system resets do not invent raw device activity");
var quietStamp=deviceClock.Read(120000).Stamp;
Check(deviceEngine.Due(120000,deviceClock.Read(120000).IdleMilliseconds,false,deviceSettings),"device idle reaches 2 minutes despite legacy refresh");
Check(!PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Reminder,quietStamp,deviceClock.Read(120100).Stamp),"legacy refresh does not stop device playback");
deviceClock.Observe(0,120200);
Check(PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Reminder,quietStamp,deviceClock.Read(120200).Stamp),"new mouse event stops reminder");
Check(!PlaybackPolicy.ShouldStopForInput(true,AudioPurpose.Preview,quietStamp,deviceClock.Read(120200).Stamp),"device input does not stop ordinary preview");
Check(!deviceEngine.Due(120200,0,true,deviceSettings),"device input restarts idle threshold");
deviceEngine.Resume(200000);
Check(!deviceEngine.Due(400000,200000,false,deviceSettings),"device mode resume waits for user");
deviceClock.Observe(1,400100);Check(!deviceEngine.Due(400100,0,true,deviceSettings),"device mode fresh resume input does not remind immediately");
Check(deviceEngine.Due(520100,120000,false,deviceSettings),"device mode full threshold after resume");
var invalidTiming=new Settings{InputTiming=(InputTimingMode)987};invalidTiming.Validate();
Check(invalidTiming.InputTiming==InputTimingMode.WindowsLastInput,"unknown mode safely defaults to Windows");
var oldTiming=System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!;oldTiming.Validate();
Check(oldTiming.InputTiming==InputTimingMode.WindowsLastInput,"old config never opts into device mode");
var timingFile=Path.Combine(fixture,"timing.json");var timingStore=new ConfigFileStore(timingFile);timingStore.Load();timingStore.Save(deviceSettings);
var timingLoaded=new ConfigFileStore(timingFile).Load();timingLoaded.PrepareForLaunch();
Check(timingLoaded.InputTiming==InputTimingMode.DeviceHeaders,"device mode persists through save and startup");
foreach(uint type in new uint[]{0,1}){
 Check(DeviceHeaderPolicy.Accept(type,false,false),"unfiltered mode retains anonymous keyboard/mouse including touchpad");
 Check(DeviceHeaderPolicy.Accept(type,true,false),"unfiltered mode retains identified device");
 Check(!DeviceHeaderPolicy.Accept(type,false,true),"optional filter rejects only anonymous event");
 Check(DeviceHeaderPolicy.Accept(type,true,true),"optional filter retains real identified keyboard/mouse");
}
Check(!DeviceHeaderPolicy.Accept(2,true,false),"other HID type still not interpreted as keyboard or mouse");
var filteredClock=new HeaderActivityClock(0);
foreach(long now in new long[]{50000,100000})if(DeviceHeaderPolicy.Accept(0,false,true))filteredClock.Observe(0,now);
Check(filteredClock.Read(120000).IdleMilliseconds==120000,"anonymous periodic mouse headers no longer reset filtered idle");
var filteredBaseline=filteredClock.Read(120000).Stamp;
Check(!DeviceHeaderPolicy.Accept(0,false,true)&&filteredClock.Read(120001).Stamp==filteredBaseline,"anonymous event cannot cancel filtered playback");
if(DeviceHeaderPolicy.Accept(0,true,true))filteredClock.Observe(0,120010);
Check(filteredClock.Read(120011).Stamp!=filteredBaseline&&filteredClock.Read(120011).IdleMilliseconds==1,"identified mouse resets idle and invalidates stop baseline");
Check(DeviceHeaderPolicy.Access(8192,12288)==InputAccessState.HigherForeground,"medium receiver declines higher-integrity foreground");
Check(DeviceHeaderPolicy.Access(12288,12288)==InputAccessState.Allowed,"matched high-integrity receiver allowed");
Check(DeviceHeaderPolicy.Access(12288,8192)==InputAccessState.Allowed,"higher receiver allowed for standard foreground");
Check(DeviceHeaderPolicy.Access(8192,8192)==InputAccessState.Allowed,"standard receiver allowed for standard foreground");
Check(DeviceHeaderPolicy.Access(null,8192)==InputAccessState.Unavailable&&DeviceHeaderPolicy.Access(8192,null)==InputAccessState.Unavailable,"unknown permission never silently trusted");
var filteredConfig=new Settings{InputTiming=InputTimingMode.DeviceHeaders,RequireIdentifiedDeviceInput=true};
var filteredCopy=filteredConfig.Copy();filteredCopy.PrepareForLaunch();
Check(filteredCopy.RequireIdentifiedDeviceInput&&filteredCopy.InputTiming==InputTimingMode.DeviceHeaders,"explicit device filter selection survives restart");
var oldFiltered=System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!;oldFiltered.Validate();
Check(!oldFiltered.RequireIdentifiedDeviceInput&&oldFiltered.InputTiming==InputTimingMode.WindowsLastInput,"old configs never opt into risky filtering");
foreach(var mode in new[]{InputTimingMode.WindowsLastInput,InputTimingMode.DeviceHeaders})
foreach(bool readable in new[]{false,true})foreach(bool admin in new[]{false,true})foreach(bool requested in new[]{false,true})
 Check(ElevationPolicy.NeedsRequest(mode,readable,admin,requested)==(mode==InputTimingMode.DeviceHeaders&&readable&&!admin&&!requested),"startup UAC only for readable selected device mode and unelevated initial launch");
Check(!ElevationPolicy.NeedsRequest((InputTimingMode)999,true,false,false),"unknown timing mode never requests admin");
var elevatedArgs=ElevationPolicy.ChildArguments(new[]{"--background","--trace-settings",ElevationPolicy.RequestedFlag});
Check(elevatedArgs.Contains("--background")&&elevatedArgs.Contains("--trace-settings")&&elevatedArgs.Count(a=>a==ElevationPolicy.RequestedFlag)==1,"elevated handoff preserves background and trace and adds one loop guard");
var noticeDefault=new Settings();noticeDefault.Validate();
Check(noticeDefault.InputModeNotices.Enabled&&noticeDefault.InputModeNotices.Applications.Count==0,"old settings receive empty notice lists without advertising a game");
var noticeOld=System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!;noticeOld.Validate();
Check(noticeOld.InputTiming==InputTimingMode.WindowsLastInput&&noticeOld.InputModeNotices.Enabled,"notice migration never enables experimental input mode");
var noticeOptions=new InputModeNoticeOptions{Applications=new(){" game.exe ","GAME.EXE"}};noticeOptions.Validate();
Check(noticeOptions.Applications.Count==1&&noticeOptions.Applications[0]=="game.exe","notice application trim and case insensitive dedup");
foreach(var invalid in new[]{"","game.lnk","https://game.exe","relative/game.exe","game.exe --arg","\"game.exe\"", "bad\napp.exe"}){
 bool rejected=false;try{InputModeNoticeOptions.Normalize(invalid);}catch(ArgumentException){rejected=true;}
 Check(rejected,"notice invalid executable entry rejected");
}
var noticePolicy=new InputModeNoticePolicy();
Check(noticePolicy.Eligible(noticeOptions,InputTimingMode.WindowsLastInput),"notice eligible only selected Windows mode");
Check(!noticePolicy.Eligible(noticeOptions,InputTimingMode.DeviceHeaders),"device mode receives no mode suggestion");
noticeOptions.Enabled=false;Check(!noticePolicy.Eligible(noticeOptions,InputTimingMode.WindowsLastInput),"notice global switch suppresses notification");
noticeOptions.Enabled=true;
Check(noticePolicy.TryMark("game.exe",100),"first detected application can notify");
Check(!noticePolicy.TryMark("game.exe",100000),"same app never repeats this run");
Check(!noticePolicy.TryMark("other.exe",200),"different apps respect global cooldown");
Check(noticePolicy.TryMark("other.exe",60100),"other app can notify after one minute");
Check(noticePolicy.Pending(noticeOptions).Length==0,"already notified name filtered case-insensitively");
noticeDefault.InputModeNotices.Applications.Add("fixture-game.exe");
var noticeCopy=noticeDefault.Copy();noticeCopy.InputModeNotices.Applications.Clear();
Check(noticeDefault.InputModeNotices.Applications.Count==1,"notice draft application edits do not mutate live list");
var emptyNotice=new InputModeNoticeOptions{Enabled=false,Applications=new()};emptyNotice.Validate();
Check(!new InputModeNoticePolicy().Eligible(emptyNotice,InputTimingMode.WindowsLastInput),"explicit empty disabled notice never restores default game");
var noticeStore=new ConfigFileStore(Path.Combine(fixture,"notices.json"));noticeStore.Load();
noticeDefault.InputModeNotices=new(){Enabled=false,Applications=new(){@"D:\SteamLibrary\steamapps\common\Game\game.exe"}};
noticeStore.Save(noticeDefault);var restoredNotice=new ConfigFileStore(Path.Combine(fixture,"notices.json")).Load();
Check(!restoredNotice.InputModeNotices.Enabled&&restoredNotice.InputModeNotices.Applications.SequenceEqual(noticeDefault.InputModeNotices.Applications),"notice paths and switch survive atomic store roundtrip");
Check(restoredNotice.InputTiming==InputTimingMode.WindowsLastInput,"notice save does not change selected timing");
var tooManyNotices=new InputModeNoticeOptions{Applications=Enumerable.Range(0,513).Select(i=>$"game{i}.exe").ToList()};
bool tooManyRejected=false;try{tooManyNotices.Validate();}catch(ArgumentException){tooManyRejected=true;}Check(tooManyRejected,"notice list limit rejects instead of silently truncating");
var steamFixture=Path.Combine(fixture,"steam-import");
var common=Path.Combine(steamFixture,"steamapps","common");
Directory.CreateDirectory(Path.Combine(common,"Game A","bin"));
Directory.CreateDirectory(Path.Combine(common,"Game B"));
File.WriteAllText(Path.Combine(common,"Game A","bin","game.exe"),"fixture only; never executed");
File.WriteAllText(Path.Combine(common,"Game A","helper.exe"),"fixture only");
File.WriteAllText(Path.Combine(common,"Game B","game.exe"),"fixture only");
File.WriteAllText(Path.Combine(common,"Game B","game.dll"),"not an executable candidate");
File.WriteAllText(Path.Combine(common,"root-helper.exe"),"outside game subfolder");
Check(SteamApplicationImport.CommonFolder(steamFixture)==common,"Steam install/library root resolves common");
Check(SteamApplicationImport.CommonFolder(Path.Combine(steamFixture,"steamapps"))==common,"steamapps selection resolves common");
Check(SteamApplicationImport.CommonFolder(common)==common,"common selection resolves itself");
var importedSteam=SteamApplicationImport.Scan(steamFixture,CancellationToken.None);
Check(importedSteam.Candidates.Count==3&&!importedSteam.Limited,"Steam scan finds nested exes and ignores root and non-exe");
Check(importedSteam.Candidates.All(c=>Path.IsPathFullyQualified(c.Executable)),"Steam candidates use full paths");
Check(importedSteam.Candidates.Select(c=>c.Game).Distinct().Count()==2,"Steam candidates retain game folder grouping");
Check(importedSteam.Candidates.Count(c=>Path.GetFileName(c.Executable)=="game.exe")==2,"different games with same exe name remain separate");
Check(SteamApplicationImport.Scan(common,CancellationToken.None,maxCandidates:1).Limited,"bounded Steam scan reports partial candidates");
Check(SteamApplicationImport.Scan(common,CancellationToken.None,maxDirectories:1).Limited,"bounded Steam directory scan reports partial");
Check(SteamApplicationImport.Scan(common,CancellationToken.None,maxDepth:0).Limited,"bounded Steam depth reports partial");
bool steamCancelled=false;using(var cancelledScan=new CancellationTokenSource()){cancelledScan.Cancel();try{SteamApplicationImport.Scan(common,cancelledScan.Token);}catch(OperationCanceledException){steamCancelled=true;}}
Check(steamCancelled,"Steam cancelled scan returns no automatic import");
bool notSteam=false;try{SteamApplicationImport.CommonFolder(fixture);}catch(ArgumentException){notSteam=true;}Check(notSteam,"unrelated directory rejected rather than recursively scanned");
var penPolicy=new InputModeNoticePolicy(InputTimingMode.DeviceHeaders);
var penOptions=new InputModeNoticeOptions{Enabled=true,Applications=new(){"paint.exe"}};
Check(penPolicy.Eligible(penOptions,InputTimingMode.DeviceHeaders),"pen list suggests Windows only when device mode selected");
Check(!penPolicy.Eligible(penOptions,InputTimingMode.WindowsLastInput),"pen list never suggests changing Windows mode");
Check(new Settings().PenInputModeNotices.Applications.Count==0,"pen list defaults empty without guessed drawing applications");
var twoLists=new Settings();twoLists.InputModeNotices.Enabled=false;twoLists.PenInputModeNotices=penOptions;
Check(!new InputModeNoticePolicy().Eligible(twoLists.InputModeNotices,InputTimingMode.WindowsLastInput)&&penPolicy.Eligible(twoLists.PenInputModeNotices,InputTimingMode.DeviceHeaders),"pen switch independent from game switch");
var penCopy=twoLists.Copy();penCopy.PenInputModeNotices.Applications.Clear();
Check(twoLists.PenInputModeNotices.Applications.Count==1,"pen draft edits isolated from live settings");
noticeStore.Save(twoLists);var twoListsLoaded=new ConfigFileStore(Path.Combine(fixture,"notices.json")).Load();
Check(!twoListsLoaded.InputModeNotices.Enabled&&twoListsLoaded.PenInputModeNotices.Enabled&&twoListsLoaded.PenInputModeNotices.Applications.SequenceEqual(new[]{"paint.exe"}),"both lists and switches persist independently");
Check(penPolicy.TryMark("paint.exe",0)&&!penPolicy.TryMark("PAINT.EXE",60000),"pen application also prompts once per run");
Check(new InputModeNoticeOptions().Applications.Count==0,"partial notice options do not invent application entries");
foreach(var timing in new[]{InputTimingMode.WindowsLastInput,InputTimingMode.DeviceHeaders,(InputTimingMode)999})
foreach(bool admin in new[]{false,true})
 Check(ElevationPolicy.NeedsSwitchRequest(timing,admin)==(timing==InputTimingMode.DeviceHeaders&&!admin),"explicit saved device mode requests UAC only while unelevated");
Check(SlideHelperProtocol.ValidName("IdleReminderPpt-"+Guid.NewGuid().ToString("N")),"private helper endpoint accepts nonce");
foreach(var bad in new[]{"","other-"+Guid.NewGuid().ToString("N"),"IdleReminderPpt-../test","IdleReminderPpt-"+new string('z',32)})Check(!SlideHelperProtocol.ValidName(bad),"helper rejects unrelated or malformed endpoint");
foreach(int n in new[]{0,1,10000})Check(SlideHelperProtocol.Decode(SlideHelperProtocol.Encode(n))==n,"bounded helper state roundtrip");
foreach(var bad in new[]{"?","-1","10001","9999999999","abc","1 extra"})Check(SlideHelperProtocol.Decode(bad)==null,"invalid helper state cannot suppress");
Check(SlideHelperProtocol.Encode(-1)=="?"&&SlideHelperProtocol.Encode(null)=="?","unknown never becomes active");
foreach(int level in new[]{0,1,2})foreach(int state in new[]{0,4})foreach(int flags in new[]{-1,0,1})
 Check(SessionSafetyPolicy.IsUnlocked(level,state,flags)==(level==1&&state==0&&flags==1),"only confirmed active unlocked session permits playback");
Check(SessionSafetyPolicy.PowerEvent(4)==PowerTransition.Suspend,"native S0 suspend event blocks playback");
Check(SessionSafetyPolicy.PowerEvent(7)==PowerTransition.Resume&&SessionSafetyPolicy.PowerEvent(18)==PowerTransition.Resume,"both native resume messages recognized");
Check(SessionSafetyPolicy.PowerEvent(0x8013)==PowerTransition.Other,"display power-setting message is not sleep");
Check(new Settings().IdleMinutes==20,"default idle remains twenty minutes");
var freshSetupStore=new ConfigFileStore(Path.Combine(fixture,"setup-"+Guid.NewGuid().ToString("N")+".json"));
var freshSetup=freshSetupStore.Load();
Check(freshSetup.TimingSetupAcknowledged==false&&freshSetup.IdleMinutes==20,"brand new configuration receives setup notice without changing threshold");
freshSetup.PrepareForLaunch();freshSetupStore.Save(freshSetup);
Check(freshSetupStore.Load().TimingSetupAcknowledged==false,"background startup does not dismiss setup notice");
var setupDraft=freshSetup.Copy();setupDraft.TimingSetupAcknowledged=true;
Check(freshSetup.TimingSetupAcknowledged==false,"setup notice draft does not mutate live confirmation");
freshSetupStore.Save(setupDraft);
Check(freshSetupStore.Load().TimingSetupAcknowledged==true,"explicit saved confirmation survives restart");
var oldSetup=System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"IdleMinutes\":4}")!;oldSetup.Validate();
Check(oldSetup.TimingSetupAcknowledged==null&&oldSetup.IdleMinutes==4,"older config retains settings and does not receive new-user notice");
foreach(bool device in new[]{false,true}){
 Check(PermissionStatusText.Get(true,device).Contains("当前已以管理员权限运行"),"admin status never implies missing permission");
 Check(PermissionStatusText.Get(false,device).Contains("当前未以管理员权限运行"),"ordinary status stays visible in both modes");
 Check(PermissionStatusText.Get(null,device).Contains("无法确认"),"unknown permission is not reported as admin");
}
Check(PermissionStatusText.Get(false,true).Contains("保存设置")&&PermissionStatusText.Get(false,true).Contains("回退"),"ordinary device mode gives retry and fallback explanation");
Check(PermissionStatusText.Get(false,false).Contains("无需管理员"),"Windows mode never claims elevation is required");
var originalUiCulture=System.Globalization.CultureInfo.CurrentUICulture;
var originalCulture=System.Globalization.CultureInfo.CurrentCulture;
try{
 System.Globalization.CultureInfo.CurrentUICulture=System.Globalization.CultureInfo.GetCultureInfo("fr-CA");
 System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
 Check(UiText.Get("SettingsForm_036")=="保存设置","missing translation falls back to Simplified Chinese");
 Check(UiText.Format("SteamImportForm_010",7,3)=="找到 7 个候选EXE；跳过 3 个链接或不可访问目录。","localized Steam summary preserves argument order");
 Check(UiText.Format("MediaDuckingLease_001",2.5)=="已补偿主音量增益 2.5 dB，进一步压低同设备音乐","localized volume status preserves numeric format");
}finally{
 System.Globalization.CultureInfo.CurrentCulture=originalCulture;
 System.Globalization.CultureInfo.CurrentUICulture=originalUiCulture;
}
var legacyLanguage=System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"IdleMinutes\":7}")!;
legacyLanguage.Validate();
Check(legacyLanguage.Language=="zh-Hans"&&legacyLanguage.IdleMinutes==7,"legacy configuration retains Chinese and existing idle threshold");
foreach(var invalidLanguage in new string?[]{null,"","unknown","../../en","en-US"}){
 Check(LanguagePolicy.Normalize(invalidLanguage)=="zh-Hans","unsupported language safely selects Chinese");
}
var englishSettings=new Settings{Language="EN",IdleMinutes=7};
englishSettings.Validate();
var englishCopy=englishSettings.Copy();
Check(englishCopy.Language=="en"&&englishCopy.IdleMinutes==7,"English setting survives validation and JSON copy");
englishCopy.Language="zh-Hans";
Check(englishSettings.Language=="en","language changes stay in settings draft");
var savedDefaultUi=System.Globalization.CultureInfo.DefaultThreadCurrentUICulture;
var savedResourceOverride=UiText.CultureOverride;
var savedUi=System.Globalization.CultureInfo.CurrentUICulture;
var savedNumberCulture=System.Globalization.CultureInfo.CurrentCulture;
try{
 ApplicationLanguage.Apply(englishSettings.Language);
 Check(UiText.Get("SettingsForm_036")=="Save Settings","saved English culture resolves English resources");
 Check(UiText.Get("Program_001").StartsWith("Administrator handoff"),"startup handoff message is translated into English");
 Check(UiText.Get("Program_001",System.Globalization.CultureInfo.GetCultureInfo("fr")).Contains("管理员交接"),"unsupported resource culture still falls back to Chinese");
 Check(System.Globalization.CultureInfo.DefaultThreadCurrentUICulture?.Name=="en","background threads inherit chosen UI language");
 Check(System.Globalization.CultureInfo.CurrentCulture.Equals(savedNumberCulture),"UI language preserves numeric culture");
 ApplicationLanguage.Apply("zh-Hans");
 Check(UiText.Get("SettingsForm_036")=="保存设置","Chinese can be restored after English");
 ApplicationLanguage.Apply("en");
 var capturedEnglish=ExecutionContext.Capture()!;
 ApplicationLanguage.Apply("zh-Hans");
 ExecutionContext.Run(capturedEnglish,_=>Check(UiText.Get("SettingsForm_036")=="保存设置","pending English continuation follows current saved Chinese choice"),null);
 var capturedChinese=ExecutionContext.Capture()!;
 ApplicationLanguage.Apply("en");
 ExecutionContext.Run(capturedChinese,_=>Check(UiText.Get("SettingsForm_036")=="Save Settings","pending Chinese continuation follows current saved English choice"),null);
 Check(UiText.Get("Language_Help").Contains("click Save Settings")&&!UiText.Get("Language_Help").Contains("restart"),"English language help describes saving without restart sentence");
 Check(UiText.Format("Tray_023",60)=="Pause for 60 minutes","English tray pause keeps numbered duration");
 ApplicationLanguage.Apply("zh-Hans");
 Check(UiText.Get("Language_Help").Contains("即可切换")&&!UiText.Get("Language_Help").Contains("无需退出"),"Chinese language help omits redundant restart sentence");
 Check(UiText.Get("SettingsForm_036",System.Globalization.CultureInfo.GetCultureInfo("en"))=="Save Settings","target-language caption resolves independently of live language");
 Check(UiText.Get("SettingsForm_036")=="保存设置","target-language preview does not change live resources");
}finally{
 UiText.CultureOverride=savedResourceOverride;
 System.Globalization.CultureInfo.DefaultThreadCurrentUICulture=savedDefaultUi;
 System.Globalization.CultureInfo.CurrentUICulture=savedUi;
}
var guideFixture=Path.Combine(fixture,"guide-paths-"+Guid.NewGuid().ToString("N"));
var guideProgram=Path.Combine(guideFixture,"Program Files");var guideProject=Path.Combine(guideFixture,"项目");
Directory.CreateDirectory(Path.Combine(guideProgram,"docs"));Directory.CreateDirectory(Path.Combine(guideProject,"docs"));
var guideEnglish=Path.Combine(guideProgram,"docs","UserGuide.en.html");
var guideChinese=Path.Combine(guideProgram,"docs","UserGuide.zh-Hans.html");
var guideRootEnglish=Path.Combine(guideProject,"docs","UserGuide.en.html");
Check(GuideLocation.Resolve(true,guideProgram,guideProject)==guideEnglish,"missing guide reports expected packaged HTML path");
File.WriteAllText(guideRootEnglish,"root English");
Check(GuideLocation.Resolve(true,guideProgram,guideProject)==guideRootEnglish,"development checkout HTML is a fallback");
File.WriteAllText(guideEnglish,"packaged English");File.WriteAllText(guideChinese,"packaged Chinese");
Check(GuideLocation.Resolve(true,guideProgram,guideProject)==guideEnglish,"packaged English HTML wins over checkout copy");
Check(GuideLocation.Resolve(false,guideProgram,guideProject)==guideChinese,"Chinese mode selects packaged Chinese HTML");
File.Delete(guideEnglish);File.Delete(guideRootEnglish);
var guideLegacy=Path.Combine(guideProgram,"GUIDE.en.txt");File.WriteAllText(guideLegacy,"legacy English");
Check(GuideLocation.Resolve(true,guideProgram,guideProject)==guideLegacy,"legacy English remains a missing-HTML fallback");
File.Delete(guideLegacy);
Check(GuideLocation.Resolve(true,guideProgram,guideProject)==guideChinese,"missing English falls back to Chinese HTML");
Console.WriteLine($"{count} checks passed");






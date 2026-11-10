// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SettingsForm:Form {
 readonly Icon windowArtwork=AppIcons.LoadWindow();
 readonly Settings s;readonly Settings live;readonly Tray tray;readonly Audio audio;
 readonly TabControl tabs=new(){Dock=DockStyle.Fill};
 readonly ThemedCheckBox identifiedInput=new(){Text="忽略无设备标识事件（实验，默认关闭）",AutoSize=true,MaximumSize=new Size(490,0)};
 readonly ThemedComboBox inputTiming=List("Windows 输入计时（默认）","设备键鼠事件计时（实验）");
 readonly ThemedCheckBox appSuppression=new(){Text="启用按应用不提醒规则（总开关，幻灯片独立控制）",AutoSize=true};
 readonly ThemedCheckBox pauseMedia=new(){Text="正式提醒前暂停正在播放且支持暂停的媒体",AutoSize=true};
 readonly Label status=new(){AutoSize=true};
 readonly NumericUpDown idle=Number(20),repeat=Number(10),ask=Number(30),volume=Number(60,0,100);
 readonly ThemedCheckBox repeating=new(){Text="持续闲置时重复提醒",AutoSize=true},asking=new(){Text="永久关闭后询问是否重新启用",AutoSize=true},startup=new(){Text="登录 Windows 后启动",AutoSize=true};
 readonly ThemedComboBox preset=List(Presets.Names),mode=List("仅提示音","仅人声","提示音 + 人声");
 readonly ThemedComboBox voicePreset=List(["未导入 / 自定义录音",..Presets.VoiceNames]);
 readonly Label toneLabel=new(){AutoSize=true},voiceLabel=new(){AutoSize=true};
 static NumericUpDown Number(int v,int min=1,int max=1440)=>new(){Minimum=min,Maximum=max,Value=v,Width=100};
 static ThemedComboBox List(params string[] items){var c=new ThemedComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=210};c.Items.AddRange(items);return c;}
 internal SettingsForm(Settings settings,Tray host,Audio player){
 Icon=windowArtwork;Disposed+=(_,_)=>windowArtwork.Dispose();
 ThemedCheckBox.ApplyPalette(settings.CheckBoxColors);
 live=settings;s=settings.Copy();tray=host;audio=player;Text=$"Windows 闲置提醒器 v{AppVersion.Current}";Size=new Size(640,670);MinimumSize=new Size(600,550);StartPosition=FormStartPosition.CenterScreen;

 FlowLayoutPanel Page(string title){var page=new TabPage(title);var p=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(18)};page.Controls.Add(p);tabs.TabPages.Add(page);return p;}
 void Row(FlowLayoutPanel p,string label,Control c){var r=new FlowLayoutPanel{AutoSize=true,Width=540};r.Controls.Add(new Label{Text=label,AutoSize=true,Padding=new Padding(0,6,10,0)});r.Controls.Add(c);p.Controls.Add(r);}
 void Button(FlowLayoutPanel p,string label,Action action){var b=new Button{Text=label,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(Exception e){Store.Log("settings-action-failed",e);MessageBox.Show("操作失败："+e.Message,"闲置提醒器");}};p.Controls.Add(b);}
 bool? currentAdmin=null;
 try{currentAdmin=StartupElevation.IsAdministrator();}catch(Exception ex){Store.Log("settings-privilege-read-failed",ex);}
 Label PrivilegeLabel(string name)=>new(){Name=name,Tag="Accent",AutoSize=true,MaximumSize=new Size(490,0)};
 var basicPermission=PrivilegeLabel("PermissionStatusBasic");var advancedPermission=PrivilegeLabel("PermissionStatusAdvanced");
 void RefreshPermissions(){var text=PermissionStatusText.Get(currentAdmin,inputTiming.SelectedIndex==1);basicPermission.Text=text;advancedPermission.Text=text;}
 var basic=Page("基础设置");basic.Controls.Add(status);basic.Controls.Add(basicPermission);
 Label? firstTimingTip=null;
 if(s.TimingSetupAcknowledged==false){
  firstTimingTip=new Label{Name="FirstTimingSetupTip",Tag="Accent",Text="首次设置：默认闲置阈值为20分钟。\n睡眠后能否播报取决于电脑的睡眠方式和系统策略，建议先实际测试。\n如果自动睡眠后仍能正常提醒，无需仅为IR调整电源设置。\n仅当提前睡眠导致漏提醒时，再考虑缩短提醒阈值（例如系统10分钟睡眠、提醒设5分钟），或自行调整Windows睡眠时间。\n仅息屏不等于睡眠；程序不会擅自修改电源策略。核对后点击“保存设置”，以后不再显示本提示。",AutoSize=true,MaximumSize=new Size(490,0)};
  basic.Controls.Add(firstTimingTip);
 }Row(basic,"闲置阈值（分钟）",idle);basic.Controls.Add(repeating);Row(basic,"重复间隔（分钟）",repeat);basic.Controls.Add(startup);basic.Controls.Add(appSuppression);basic.Controls.Add(new Label{Text="关闭后，媒体规则中的“播放中／运行中不提醒”暂不生效，名单仍保留。\n幻灯片放映抑制由其独立页面控制，不受此开关影响。\n程序不自动识别下载或游戏；未配置的软件照常检测闲置。\n锁屏、睡眠、手动暂停及提醒前暂停／压低媒体不受此开关影响。",AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});Button(basic,"启用提醒",()=>tray.Enable());Button(basic,"暂停直到手动恢复",()=>tray.Pause(null));
 var sound=Page("提醒音");Row(sound,"播放模式",mode);Row(sound,"内置提示音",preset);Row(sound,"应用音量（0–100）",volume);sound.Controls.Add(toneLabel);
 Button(sound,"导入 / 替换提示音",()=>Import(false));Button(sound,"恢复内置提示音",()=>{s.Tone=null;Labels();});Row(sound,"人声来源",voicePreset);sound.Controls.Add(voiceLabel);
 Button(sound,"导入 / 替换人声",()=>Import(true));Button(sound,"删除人声",()=>{s.Voice=null;s.VoicePreset=-1;voicePreset.SelectedIndex=0;Labels();});
 Button(sound,"试听提示音",()=>Preview(0));Button(sound,"试听人声",()=>Preview(1));Button(sound,"试听组合",()=>Preview(2));Button(sound,"停止试听",()=>audio.Stop());
 sound.Controls.Add(new Label{Text="支持 WAV、MP3，最大 50 MB。每段最长播放 2 分钟。\n导入文件复制到项目 data 目录；不录音。手机通知启用后使用局域网。",AutoSize=true});
 var pause=Page("暂停与恢复");pause.Controls.Add(asking);Row(pause,"再次询问延迟（分钟）",ask);pause.Controls.Add(new Label{Text="每次启动软件自动启用提醒，上次暂停不延续。\n定时暂停到期自动恢复。\n本次运行内，永久暂停在延迟到期后、有新操作且非全屏时询问。\n询问窗口不主动获取焦点，稍后提醒会重新计时。",AutoSize=true});
 var media=Page("媒体规则");media.Controls.Add(pauseMedia);media.Controls.Add(new Label{Text="正式提醒前暂停正在播放且支持暂停的媒体。\n试听不触发；不自动恢复媒体。仅支持 Windows 暴露的媒体会话。",AutoSize=true});
 var mediaScope=new MediaScopePanel(s);mediaScope.SetPauseEnabled(s.PauseMediaOnReminder);media.Controls.Add(mediaScope);pauseMedia.CheckedChanged+=(_,_)=>mediaScope.SetPauseEnabled(pauseMedia.Checked);
 var suppression=new SuppressionPanel(s);media.Controls.Add(suppression);
 var slides=new SlideShowPanel(s);Page("幻灯片放映").Controls.Add(slides);
 var phone=new PhonePanel(s);Page("手机通知").Controls.Add(phone);
 var deviceVolume=new DeviceVolumePanel(s,audio,tray);Page("设备音量").Controls.Add(deviceVolume);
 var advanced=Page("高级设置");advanced.Controls.Add(advancedPermission);inputTiming.Width=340;Row(advanced,"闲置检测方式",inputTiming);advanced.Controls.Add(identifiedInput);inputTiming.SelectedIndexChanged+=(_,_)=>{identifiedInput.Enabled=inputTiming.SelectedIndex==1;RefreshPermissions();};advanced.Controls.Add(new Label{Text="此过滤项默认关闭，仅在实测无设备标识事件干扰时主动启用。\n可能无法识别部分触控板、远程输入；只用于已验证的外接键鼠。\n非空设备标识也不保证一定来自物理设备。\n本机高权限游戏需匹配权限才能收到真实操作；普通权限遇高权限前台将临时回退并提示，选择设备事件模式并保存后，若当前不是管理员会申请正常UAC；启动时也会检查。取消仍普通运行并保留权限回退。\n开启设备计时后，如当前未以管理员身份运行且未收到权限请求，可尝试再次点击“保存设置”，关闭保存成功提示后留意Windows权限请求。已是管理员时不会重复请求。",AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});advanced.Controls.Add(new Label{Text="设备事件模式用于排查游戏等软件刷新系统输入时间的问题。\n只统计键鼠事件头，不读取按键内容或鼠标坐标，不安装钩子。\n虚拟设备、触控、远程控制及反作弊兼容性仍有限，不能保证等同于物理操作。\n接收失败会回退Windows计时；锁屏、睡眠、幻灯片及暂停规则仍生效。\n保存或切换模式后重新累计闲置时间。",AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});advanced.Controls.Add(new Label{Text="自动提高音量：请在设备音量页为独立扬声器分别设置。\n配置、导入音频和有限大小诊断日志位于项目 data 文件夹。\n检测间隔 1 秒；恢复睡眠或解锁后等待新操作。\n默认获取最后输入时间；实验模式只读取事件头，不读取按键内容。",AutoSize=true});
 var timingNotices=new InputModeNoticePanel(s);advanced.Controls.Add(timingNotices);
 var penTimingNotices=new InputModeNoticePanel(s,true);advanced.Controls.Add(penTimingNotices);
 inputTiming.SelectedIndexChanged+=(_,_)=>{timingNotices.SetInputMode((InputTimingMode)inputTiming.SelectedIndex);penTimingNotices.SetInputMode((InputTimingMode)inputTiming.SelectedIndex);};
 var colors=new SkinSelectionPanel(s,this);Page("皮肤").Controls.Add(colors);
 Page("版本与更新").Controls.Add(new VersionInfoPanel());
 // Append the reminder after each page's content, leaving one empty text line.
 foreach(TabPage page in tabs.TabPages){
  var content=(FlowLayoutPanel)page.Controls[0];
  content.Controls.Add(new Label{
   Text="记得手动点击“保存设置”按钮",
   AutoSize=true,
   ForeColor=SystemColors.GrayText,
   Margin=new Padding(3,Font.Height+6,3,3)
  });
 }
 basic.Controls.Add(new Label{
  Text="如果电脑提前自动睡眠导致漏提醒，可缩短提醒阈值或自行调整Windows睡眠时间；若睡眠后仍能正常提醒，无需仅为IR更改电源设置。\n程序不会擅自修改Windows睡眠策略。锁屏时暂停提醒；睡眠后的播报行为需在本机验证，不能用仅息屏结果代替。",
  AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText,
  Margin=new Padding(3,Font.Height+6,3,3)
 });
 var save=new System.Windows.Forms.Button{Text="保存设置",Dock=DockStyle.Bottom,Height=42};save.Click+=(_,_)=>{try{if(mode.SelectedIndex!=0&&!s.HasVoice()){MessageBox.Show("请先导入本地录音。");return;}timingNotices.Apply(s);penTimingNotices.Apply(s);phone.Apply(s);colors.Apply(s);slides.Apply(s);suppression.Apply(s);mediaScope.Apply(s);Apply();deviceVolume.Apply();var warning=tray.Commit(s,startup.Checked);if(firstTimingTip!=null)firstTimingTip.Visible=false;MessageBox.Show(warning??"设置已保存。");tray.RequestInputElevation();}catch(Exception e){Store.Log("save-settings-failed",e);MessageBox.Show("保存失败："+e.Message);}};
 Controls.Add(tabs);Controls.Add(save);
 identifiedInput.Checked=s.RequireIdentifiedDeviceInput;inputTiming.SelectedIndex=(int)s.InputTiming;appSuppression.Checked=s.AppSuppressionEnabled;pauseMedia.Checked=s.PauseMediaOnReminder;
 idle.Value=s.IdleMinutes;repeat.Value=s.RepeatMinutes;volume.Value=s.Volume;ask.Value=s.AskMinutes;repeating.Checked=s.Repeat;asking.Checked=s.AskAgain;startup.Checked=Startup.Enabled;preset.SelectedIndex=s.Preset;mode.SelectedIndex=s.Mode;voicePreset.SelectedIndex=s.VoicePreset+1;Labels();
 preset.SelectionChangeCommitted+=(_,_)=>{s.Tone=null;Labels();};
 voicePreset.SelectionChangeCommitted+=(_,_)=>{s.VoicePreset=voicePreset.SelectedIndex-1;if(s.VoicePreset>=0)s.Voice=null;Labels();};
 UiStyle.Apply(this);colors.Preview();
 FormClosed+=(_,_)=>{audio.Stop();ThemedCheckBox.ApplyPalette(live.CheckBoxColors);};
 }
 void Labels(){toneLabel.Text=s.Tone==null?"正在使用内置提示音":"已导入自定义提示音";voiceLabel.Text=s.Voice!=null?"已导入自定义人声":s.VoicePreset>=0?"正在使用人声预设："+Presets.VoiceNames[s.VoicePreset]:"未提供内置人声，请自行导入录音";}
 void Apply(){s.TimingSetupAcknowledged=true;s.RequireIdentifiedDeviceInput=identifiedInput.Checked;s.InputTiming=(InputTimingMode)inputTiming.SelectedIndex;s.AppSuppressionEnabled=appSuppression.Checked;s.PauseMediaOnReminder=pauseMedia.Checked;s.IdleMinutes=(int)idle.Value;s.RepeatMinutes=(int)repeat.Value;s.Volume=(int)volume.Value;s.AskMinutes=(int)ask.Value;s.Repeat=repeating.Checked;s.AskAgain=asking.Checked;s.Preset=preset.SelectedIndex;s.VoicePreset=voicePreset.SelectedIndex-1;s.Mode=mode.SelectedIndex;}
 void Import(bool voice){using var d=new OpenFileDialog{Filter="音频文件|*.wav;*.mp3",Title="选择本地音频"};if(d.ShowDialog()!=DialogResult.OK)return;var p=Store.Import(d.FileName,voice?"voice":"tone");if(voice){s.Voice=p;s.VoicePreset=-1;voicePreset.SelectedIndex=0;}else s.Tone=p;Labels();}
 void Preview(int m){Apply();if(m!=0&&!s.HasVoice()){MessageBox.Show("请先导入本地录音。");return;}audio.Play(s.AudioFiles(AppContext.BaseDirectory,m),s.Volume,AudioPurpose.Preview);}
 internal void ShowInputTiming(){tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().Single(x=>x.Text=="高级设置");inputTiming.Focus();}
 internal void UpdateStatus(string value)=>status.Text="状态："+value;
}











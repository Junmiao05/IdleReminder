// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SettingsForm:Form {
 internal event Action<SettingsForm>? LanguageRefreshRequested;
 internal Action<string> SaveFeedback=message=>MessageBox.Show(message);
 internal TabControl ViewTabs=>tabs;
 readonly string displayedLanguage=LanguagePolicy.Normalize(UiText.CultureOverride?.Name??System.Globalization.CultureInfo.CurrentUICulture.Name);
 readonly Icon windowArtwork=AppIcons.LoadWindow();
 readonly Settings s;readonly Settings live;readonly Tray tray;readonly Audio audio;
 readonly ThemedComboBox language=List("简体中文","English");
 readonly ThemedTabControl tabs=new(){Dock=DockStyle.Fill};
 readonly ThemedCheckBox identifiedInput=new(){Text=UiText.Get("SettingsForm_001"),AutoSize=true,MaximumSize=new Size(490,0)};
 readonly ThemedComboBox inputTiming=List(UiText.Get("SettingsForm_041"),UiText.Get("SettingsForm_042"));
 readonly ThemedCheckBox appSuppression=new(){Text=UiText.Get("SettingsForm_002"),AutoSize=true};
 readonly ThemedCheckBox pauseMedia=new(){Text=UiText.Get("SettingsForm_003"),AutoSize=true};
 readonly TabPage advancedPage;
 readonly Label status=new(){AutoSize=true};
 readonly NumericUpDown idle=Number(20),repeat=Number(10),ask=Number(30),volume=Number(60,0,100);
 readonly ThemedCheckBox repeating=new(){Text=UiText.Get("SettingsForm_004"),AutoSize=true},asking=new(){Text=UiText.Get("SettingsForm_005"),AutoSize=true},startup=new(){Text=UiText.Get("SettingsForm_006"),AutoSize=true};
 readonly ThemedComboBox preset=List(Presets.Names),mode=List(UiText.Get("SettingsForm_043"),UiText.Get("SettingsForm_044"),UiText.Get("SettingsForm_045"));
 readonly ThemedComboBox voicePreset=List([UiText.Get("SettingsForm_046"),..Presets.VoiceNames]);
 readonly Label toneLabel=new(){AutoSize=true},voiceLabel=new(){AutoSize=true};
 static NumericUpDown Number(int v,int min=1,int max=1440)=>new(){Minimum=min,Maximum=max,Value=v,Width=100};
 static ThemedComboBox List(params string[] items){var c=new ThemedComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=210};c.Items.AddRange(items);return c;}
 internal SettingsForm(Settings settings,Tray host,Audio player){
 Icon=windowArtwork;Disposed+=(_,_)=>windowArtwork.Dispose();
 ThemedCheckBox.ApplyPalette(settings.CheckBoxColors);
 live=settings;s=settings.Copy();tray=host;audio=player;Text=UiText.Format("SettingsForm_047",AppVersion.Current);Size=new Size(640,670);MinimumSize=new Size(600,550);StartPosition=FormStartPosition.CenterScreen;

 FlowLayoutPanel Page(string title){var page=new TabPage(title);var p=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(18)};page.Controls.Add(p);tabs.TabPages.Add(page);return p;}
 void Row(FlowLayoutPanel p,string label,Control c){var r=new FlowLayoutPanel{AutoSize=true,Width=540};r.Controls.Add(new Label{Text=label,AutoSize=true,Padding=new Padding(0,6,10,0)});r.Controls.Add(c);p.Controls.Add(r);}
 void Button(FlowLayoutPanel p,string label,Action action){var b=new ThemedButton{Text=label,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(Exception e){Store.Log("settings-action-failed",e);MessageBox.Show(UiText.Get("SettingsForm_007")+e.Message,UiText.Get("SettingsForm_048"));}};p.Controls.Add(b);}
 Label Explanation(string key, bool helper=false) => new() {
 Text=UiText.Get(key), AutoSize=true, MaximumSize=new Size(490,0),
 Margin=new Padding(3,3,3,8), ForeColor=helper?SystemColors.GrayText:SystemColors.ControlText
};
void Heading(FlowLayoutPanel page,string key) {
 var label=Explanation(key);
 label.Font=new Font(Font,FontStyle.Bold);
 label.Margin=new Padding(3,12,3,4);
 label.Disposed+=(_,_)=>label.Font.Dispose();
 page.Controls.Add(label);
}
void Details(FlowLayoutPanel page,string name,params string[] keys) => DetailsSection.Add(page,name,490,keys.Select(key=>(key,true)).ToArray());
bool? currentAdmin=null;
 try{currentAdmin=StartupElevation.IsAdministrator();}catch(Exception ex){Store.Log("settings-privilege-read-failed",ex);}
 Label PrivilegeLabel(string name)=>new(){Name=name,Tag="Accent",AutoSize=true,MaximumSize=new Size(490,0)};
 var basicPermission=PrivilegeLabel("PermissionStatusBasic");var advancedPermission=PrivilegeLabel("PermissionStatusAdvanced");
 void RefreshPermissions(){var text=PermissionStatusText.Get(currentAdmin,inputTiming.SelectedIndex==1);basicPermission.Text=UiText.Get(currentAdmin==true?"Layout_Admin":currentAdmin==false?"Layout_Ordinary":"Layout_PermissionUnknown");advancedPermission.Text=text;}
 var basic=Page(UiText.Get("SettingsForm_008"));basic.Controls.Add(status);basic.Controls.Add(basicPermission);
 Label? firstTimingTip=null;
 if(s.TimingSetupAcknowledged==false){
  firstTimingTip=new Label{Name="FirstTimingSetupTip",Tag="Accent",Text=UiText.Get("SettingsForm_009"),AutoSize=true,MaximumSize=new Size(490,0)};
  basic.Controls.Add(firstTimingTip);
 }Row(basic,UiText.Get("SettingsForm_010"),idle);basic.Controls.Add(repeating);Row(basic,UiText.Get("SettingsForm_011"),repeat);basic.Controls.Add(startup);basic.Controls.Add(appSuppression);basic.Controls.Add(Explanation("Layout_AppRulesEffect"));basic.Controls.Add(Explanation("Layout_SlidesIndependent",true));Details(basic,"BasicRuleDetails","SettingsForm_012");Button(basic,UiText.Get("SettingsForm_049"),()=>tray.Enable());Button(basic,UiText.Get("SettingsForm_050"),()=>tray.Pause(null));
 var sound=Page(UiText.Get("SettingsForm_013"));Row(sound,UiText.Get("SettingsForm_014"),mode);Row(sound,UiText.Get("SettingsForm_015"),preset);Row(sound,UiText.Get("SettingsForm_016"),volume);sound.Controls.Add(toneLabel);
 Button(sound,UiText.Get("SettingsForm_051"),()=>Import(false));Button(sound,UiText.Get("SettingsForm_052"),()=>{s.Tone=null;Labels();});Row(sound,UiText.Get("SettingsForm_017"),voicePreset);sound.Controls.Add(voiceLabel);
 Button(sound,UiText.Get("SettingsForm_053"),()=>Import(true));Button(sound,UiText.Get("SettingsForm_054"),()=>{s.Voice=null;s.VoicePreset=-1;voicePreset.SelectedIndex=0;Labels();});
 Button(sound,UiText.Get("SettingsForm_055"),()=>Preview(0));Button(sound,UiText.Get("SettingsForm_056"),()=>Preview(1));Button(sound,UiText.Get("SettingsForm_057"),()=>Preview(2));Button(sound,UiText.Get("SettingsForm_058"),()=>audio.Stop());
 sound.Controls.Add(new Label{Text=UiText.Get("SettingsForm_018"),AutoSize=true});
 var pause=Page(UiText.Get("SettingsForm_019"));pause.Controls.Add(asking);Row(pause,UiText.Get("SettingsForm_020"),ask);pause.Controls.Add(new Label{Text=UiText.Get("SettingsForm_021"),AutoSize=true});
 var media=Page(UiText.Get("SettingsForm_022"));media.Controls.Add(pauseMedia);media.Controls.Add(new Label{Text=UiText.Get("SettingsForm_023"),AutoSize=true});
 var mediaScope=new MediaScopePanel(s);mediaScope.SetPauseEnabled(s.PauseMediaOnReminder);media.Controls.Add(mediaScope);pauseMedia.CheckedChanged+=(_,_)=>mediaScope.SetPauseEnabled(pauseMedia.Checked);
 var suppression=new SuppressionPanel(s);media.Controls.Add(suppression);
 var slides=new SlideShowPanel(s);Page(UiText.Get("SettingsForm_024")).Controls.Add(slides);
 var phone=new PhonePanel(s);Page(UiText.Get("SettingsForm_025")).Controls.Add(phone);
 var deviceVolume=new DeviceVolumePanel(s,audio,tray);Page(UiText.Get("SettingsForm_026")).Controls.Add(deviceVolume);
 var advanced=Page(UiText.Get("SettingsForm_027"));
 advancedPage=(TabPage)advanced.Parent!;
 Heading(advanced,"Layout_InputHeading");
 inputTiming.Width=340;
 Row(advanced,UiText.Get("SettingsForm_028"),inputTiming);
 advanced.Controls.Add(Explanation("Layout_InputSummary",true));
 Heading(advanced,"Layout_PermissionHeading");
 advanced.Controls.Add(advancedPermission);
 Heading(advanced,"Layout_FilterHeading");
 advanced.Controls.Add(identifiedInput);
 advanced.Controls.Add(Explanation("Layout_FilterWarning"));
 inputTiming.SelectedIndexChanged+=(_,_)=>{
  identifiedInput.Enabled=inputTiming.SelectedIndex==1;RefreshPermissions();
 };
 Details(advanced,"AdvancedInputDetails","SettingsForm_029","SettingsForm_030","SettingsForm_031");
 var timingNotices=new InputModeNoticePanel(s);advanced.Controls.Add(timingNotices);
 var penTimingNotices=new InputModeNoticePanel(s,true);advanced.Controls.Add(penTimingNotices);
 inputTiming.SelectedIndexChanged+=(_,_)=>{timingNotices.SetInputMode((InputTimingMode)inputTiming.SelectedIndex);penTimingNotices.SetInputMode((InputTimingMode)inputTiming.SelectedIndex);};
 var colors=new SkinSelectionPanel(s,this);Page(UiText.Get("SettingsForm_032")).Controls.Add(colors);
 var languagePage=Page("语言 / Language");Row(languagePage,UiText.Get("Language_Label"),language);languagePage.Controls.Add(Explanation("Language_Help"));
 Page(UiText.Get("SettingsForm_033")).Controls.Add(new VersionInfoPanel());
 // Append the reminder after each page's content, leaving one empty text line.
 foreach(TabPage page in tabs.TabPages){
  var content=(FlowLayoutPanel)page.Controls[0];
  content.Controls.Add(new Label{
   Text=UiText.Get("SettingsForm_034"),
   AutoSize=true,
   ForeColor=SystemColors.GrayText,
   Margin=new Padding(3,Font.Height+6,3,3)
  });
 }
 basic.Controls.Add(new Label{
  Text=UiText.Get("SettingsForm_035"),
  AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText,
  Margin=new Padding(3,Font.Height+6,3,3)
 });
 var save=new ThemedButton{Text=UiText.Get("SettingsForm_036"),Tag="Primary",Dock=DockStyle.Bottom,Height=42};save.Click+=(_,_)=>{try{if(mode.SelectedIndex!=0&&!s.HasVoice()){SaveFeedback(UiText.Get("SettingsForm_037"));return;}timingNotices.Apply(s);penTimingNotices.Apply(s);phone.Apply(s);colors.Apply(s);slides.Apply(s);suppression.Apply(s);mediaScope.Apply(s);Apply();deviceVolume.Apply();var warning=tray.Commit(s,startup.Checked);if(firstTimingTip!=null)firstTimingTip.Visible=false;SaveFeedback(warning??UiText.Get("SettingsForm_059"));if(displayedLanguage!=s.Language)LanguageRefreshRequested?.Invoke(this);tray.RequestInputElevation();}catch(Exception e){Store.Log("save-settings-failed",e);SaveFeedback(UiText.Get("SettingsForm_038")+e.Message);}};
 Controls.Add(tabs);Controls.Add(save);
 language.SelectedIndexChanged+=(_,_)=>save.Text=UiText.Get("SettingsForm_036",System.Globalization.CultureInfo.GetCultureInfo(language.SelectedIndex==1?LanguagePolicy.English:LanguagePolicy.SimplifiedChinese));
 language.SelectedIndex=s.Language==LanguagePolicy.English?1:0;identifiedInput.Checked=s.RequireIdentifiedDeviceInput;inputTiming.SelectedIndex=(int)s.InputTiming;appSuppression.Checked=s.AppSuppressionEnabled;pauseMedia.Checked=s.PauseMediaOnReminder;
 idle.Value=s.IdleMinutes;repeat.Value=s.RepeatMinutes;volume.Value=s.Volume;ask.Value=s.AskMinutes;repeating.Checked=s.Repeat;asking.Checked=s.AskAgain;startup.Checked=Startup.Enabled;preset.SelectedIndex=s.Preset;mode.SelectedIndex=s.Mode;voicePreset.SelectedIndex=s.VoicePreset+1;Labels();
 preset.SelectionChangeCommitted+=(_,_)=>{s.Tone=null;Labels();};
 voicePreset.SelectionChangeCommitted+=(_,_)=>{s.VoicePreset=voicePreset.SelectedIndex-1;if(s.VoicePreset>=0)s.Voice=null;Labels();};
 // Bound explanatory labels; native checkbox captions stay concise and autosized.
 void WrapText(Control parent){foreach(Control child in parent.Controls){
  if(child is Label {AutoSize:true})
   if(child.MaximumSize.Width==0)child.MaximumSize=new Size(490,0);
  WrapText(child);
 }}
 WrapText(this);UiStyle.Apply(this);colors.Preview();
 FormClosed+=(_,_)=>{audio.Stop();ThemedCheckBox.ApplyPalette(live.CheckBoxColors);};
 }
 void Labels(){toneLabel.Text=s.Tone==null?UiText.Get("SettingsForm_060"):UiText.Get("SettingsForm_061");voiceLabel.Text=s.Voice!=null?UiText.Get("SettingsForm_062"):s.VoicePreset>=0?UiText.Format("SettingsForm_VoicePreset",Presets.VoiceNames[s.VoicePreset]):UiText.Get("SettingsForm_063");}
 void Apply(){s.Language=language.SelectedIndex==1?LanguagePolicy.English:LanguagePolicy.SimplifiedChinese;s.TimingSetupAcknowledged=true;s.RequireIdentifiedDeviceInput=identifiedInput.Checked;s.InputTiming=(InputTimingMode)inputTiming.SelectedIndex;s.AppSuppressionEnabled=appSuppression.Checked;s.PauseMediaOnReminder=pauseMedia.Checked;s.IdleMinutes=(int)idle.Value;s.RepeatMinutes=(int)repeat.Value;s.Volume=(int)volume.Value;s.AskMinutes=(int)ask.Value;s.Repeat=repeating.Checked;s.AskAgain=asking.Checked;s.Preset=preset.SelectedIndex;s.VoicePreset=voicePreset.SelectedIndex-1;s.Mode=mode.SelectedIndex;}
 void Import(bool voice){using var d=new OpenFileDialog{Filter=UiText.Get("SettingsForm_AudioFileFilter")+"|*.wav;*.mp3",Title=UiText.Get("SettingsForm_039")};if(d.ShowDialog()!=DialogResult.OK)return;var p=Store.Import(d.FileName,voice?"voice":"tone");if(voice){s.Voice=p;s.VoicePreset=-1;voicePreset.SelectedIndex=0;}else s.Tone=p;Labels();}
 void Preview(int m){Apply();if(m!=0&&!s.HasVoice()){MessageBox.Show(UiText.Get("SettingsForm_037"));return;}audio.Play(s.AudioFiles(AppContext.BaseDirectory,m),s.Volume,AudioPurpose.Preview);}
 internal void ShowInputTiming(){tabs.SelectedTab=advancedPage;inputTiming.Focus();}
 internal void UpdateStatus(string value)=>status.Text=UiText.Get("SettingsForm_040")+value;
}













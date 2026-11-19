// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class InputModeNoticePanel:FlowLayoutPanel {
 readonly ThemedCheckBox enabled=new(){Text=UiText.Get("InputModeNoticePanel_001"),AutoSize=true,MaximumSize=new Size(475,0)};
 readonly ListBox list=new(){Width=475,Height=120,HorizontalScrollbar=true};
 readonly TextBox entry=new(){Width=475};
 readonly bool pen;InputTimingMode chosenMode;
 readonly Label modeStatus=new(){Name="InputModeNoticeState",AutoSize=true,MaximumSize=new Size(475,0)};
 internal InputModeNoticePanel(Settings settings,bool pen=false){
  this.pen=pen;var options=pen?settings.PenInputModeNotices:settings.InputModeNotices;
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  Controls.Add(new Label{Text=pen?UiText.Get("InputModeNoticePanel_007"):UiText.Get("InputModeNoticePanel_008"),AutoSize=true,Font=new Font(SystemFonts.MessageBoxFont!,FontStyle.Bold)});
  enabled.Text=pen?UiText.Get("InputModeNoticePanel_009"):UiText.Get("InputModeNoticePanel_010");Controls.Add(enabled);enabled.Checked=options.Enabled;Controls.Add(modeStatus);enabled.CheckedChanged+=(_,_)=>RefreshModeStatus();
  Controls.Add(new Label{Text=pen?UiText.Get("InputModeNoticePanel_011"):UiText.Get("InputModeNoticePanel_012"),AutoSize=true,MaximumSize=new Size(475,0)});
  if(!pen)Controls.Add(new Label{Name="GameNoticeHelp",Text=UiText.Get("InputModeNoticePanel_002"),AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText});
  foreach(var value in options.Applications)list.Items.Add(value);
  Controls.Add(list);Controls.Add(entry);
  AddButton(UiText.Get("SuppressionPanel_005"),()=>{using var dialog=new OpenFileDialog{Filter=UiText.Get("InputModeNoticePanel_013")+"|*.exe",CheckFileExists=true,RestoreDirectory=true};if(dialog.ShowDialog(FindForm())==DialogResult.OK)entry.Text=dialog.FileName;});
  AddButton(UiText.Get("InputModeNoticePanel_003"),()=>AddEntries([InputModeNoticeOptions.Normalize(entry.Text)]));
  AddButton(UiText.Get("InputModeNoticePanel_004"),()=>{if(list.SelectedIndex>=0){list.Items.RemoveAt(list.SelectedIndex);RefreshModeStatus();}});
  if(!pen)AddButton(UiText.Get("InputModeNoticePanel_005"),()=>{
   using var folder=new FolderBrowserDialog{Description=UiText.Get("InputModeNoticePanel_014"),UseDescriptionForTitle=true};
   if(folder.ShowDialog(FindForm())!=DialogResult.OK)return;
   using var preview=new SteamImportForm(folder.SelectedPath);if(preview.ShowDialog(FindForm())==DialogResult.OK)AddEntries(preview.SelectedApplications);
  });
  SetInputMode(settings.InputTiming);
  Controls.Add(new Label{Text=UiText.Get("InputModeNoticePanel_006"),AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText});
 }
 internal void AddEntries(IEnumerable<string> entries){
  var merged=list.Items.Cast<string>().Concat(entries).Select(InputModeNoticeOptions.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  if(merged.Length>512)throw new ArgumentException(UiText.Get("InputModeNoticePanel_015"));
  list.BeginUpdate();try{list.Items.Clear();list.Items.AddRange(merged);}finally{list.EndUpdate();}RefreshModeStatus();
 }
 internal void SetInputMode(InputTimingMode mode){chosenMode=mode;RefreshModeStatus();}
 void RefreshModeStatus(){
  var selected=chosenMode==InputTimingMode.DeviceHeaders?UiText.Get("InputModeNoticePanel_016"):UiText.Get("InputModeNoticePanel_017");
  var eligible=chosenMode==(pen?InputTimingMode.DeviceHeaders:InputTimingMode.WindowsLastInput);
  modeStatus.Text=!enabled.Checked?UiText.Get("InputModeNoticePanel_018"):!eligible?UiText.Format("InputModeNoticePanel_019",selected):list.Items.Count==0?UiText.Format("InputModeNoticePanel_020",selected):UiText.Format("InputModeNoticePanel_021",selected);
 }
 void AddButton(string text,Action action){var b=new ThemedButton{Text=text,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(ArgumentException ex){MessageBox.Show(ex.Message,UiText.Get("InputModeNoticePanel_022"));}};Controls.Add(b);}
 internal void Apply(Settings settings){var options=new InputModeNoticeOptions{Enabled=enabled.Checked,Applications=list.Items.Cast<string>().ToList()};options.Validate();if(pen)settings.PenInputModeNotices=options;else settings.InputModeNotices=options;}
}

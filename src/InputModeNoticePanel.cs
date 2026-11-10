// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class InputModeNoticePanel:FlowLayoutPanel {
 readonly ThemedCheckBox enabled=new(){Text="名单内软件运行时，提示手动选择闲置检测方式",AutoSize=true,MaximumSize=new Size(475,0)};
 readonly ListBox list=new(){Width=475,Height=120,HorizontalScrollbar=true};
 readonly TextBox entry=new(){Width=475};
 readonly bool pen;InputTimingMode chosenMode;
 readonly Label modeStatus=new(){Name="InputModeNoticeState",AutoSize=true,MaximumSize=new Size(475,0)};
 internal InputModeNoticePanel(Settings settings,bool pen=false){
  this.pen=pen;var options=pen?settings.PenInputModeNotices:settings.InputModeNotices;
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  Controls.Add(new Label{Text=pen?"数位板软件：提示切换检测方式":"游戏类软件：提示切换检测方式",AutoSize=true,Font=new Font(SystemFonts.MessageBoxFont!,FontStyle.Bold)});
  enabled.Text=pen?"启用数位板软件检测方式提示":"启用游戏类软件检测方式提示";Controls.Add(enabled);enabled.Checked=options.Enabled;Controls.Add(modeStatus);enabled.CheckedChanged+=(_,_)=>RefreshModeStatus();
  Controls.Add(new Label{Text=pen?"当前使用设备键鼠计时且名单内软件运行时，提示考虑Windows计时或关闭无设备标识过滤。\n名单默认留空，请浏览添加自己的绘画等软件。只提示，由你手动选择并保存，不自动切换。":"当前使用Windows输入计时且名单内软件运行时，发送Windows通知；点击通知可打开本页手动选择并保存。\n不自动切换、不自动提权，不影响是否播放闲置提醒。\n名单默认留空，请自行浏览或导入软件；不把全屏软件当作游戏。",AutoSize=true,MaximumSize=new Size(475,0)});
  if(!pen)Controls.Add(new Label{Name="GameNoticeHelp",Text="看不到游戏提示时，请检查：\nWindows“设置 → 系统 → 通知”中的勿扰，以及“玩游戏时／全屏使用应用时”的自动规则。\n名单中的路径是否实为启动器路径；请选择实际运行的游戏主EXE，启动器可能在拉起游戏后退出。",AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText});
  foreach(var value in options.Applications)list.Items.Add(value);
  Controls.Add(list);Controls.Add(entry);
  AddButton("浏览软件…",()=>{using var dialog=new OpenFileDialog{Filter="Windows程序|*.exe",CheckFileExists=true,RestoreDirectory=true};if(dialog.ShowDialog(FindForm())==DialogResult.OK)entry.Text=dialog.FileName;});
  AddButton("添加到提示名单",()=>AddEntries([InputModeNoticeOptions.Normalize(entry.Text)]));
  AddButton("删除所选提示项",()=>{if(list.SelectedIndex>=0){list.Items.RemoveAt(list.SelectedIndex);RefreshModeStatus();}});
  if(!pen)AddButton("从Steam目录批量导入…",()=>{
   using var folder=new FolderBrowserDialog{Description="选择Steam安装目录或游戏库目录（包含steamapps\\common）",UseDescriptionForTitle=true};
   if(folder.ShowDialog(FindForm())!=DialogResult.OK)return;
   using var preview=new SteamImportForm(folder.SelectedPath);if(preview.ShowDialog(FindForm())==DialogResult.OK)AddEntries(preview.SelectedApplications);
  });
  SetInputMode(settings.InputTiming);
  Controls.Add(new Label{Text="两套提示独立启用，各自保留名单；每项每次运行最多提示一次，通知至少间隔1分钟。\n支持EXE文件名或完整路径，最多512项；浏览与Steam导入保存完整路径。\n可触发不代表立即显示：需匹配运行软件，且不在暂停／播报中；同类程序不重复通知。\n仅修改草稿，点击底部“保存设置”后生效。拒绝/关闭通知不更改设置；勿扰模式可能隐藏通知。",AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText});
 }
 internal void AddEntries(IEnumerable<string> entries){
  var merged=list.Items.Cast<string>().Concat(entries).Select(InputModeNoticeOptions.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  if(merged.Length>512)throw new ArgumentException("提示名单最多512项，请减少勾选项或先删除部分名单。");
  list.BeginUpdate();try{list.Items.Clear();list.Items.AddRange(merged);}finally{list.EndUpdate();}RefreshModeStatus();
 }
 internal void SetInputMode(InputTimingMode mode){chosenMode=mode;RefreshModeStatus();}
 void RefreshModeStatus(){
  var selected=chosenMode==InputTimingMode.DeviceHeaders?"设备键鼠计时":"Windows输入计时";
  var eligible=chosenMode==(pen?InputTimingMode.DeviceHeaders:InputTimingMode.WindowsLastInput);
  modeStatus.Text=!enabled.Checked?"本类提示已关闭；修改后需保存。":!eligible?$"所选{selected}：本类提示不触发。":list.Items.Count==0?$"所选{selected}：名单为空，本类提示不触发。":$"所选{selected}：本类提示可触发；修改后需保存。";
 }
 void AddButton(string text,Action action){var b=new Button{Text=text,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(ArgumentException ex){MessageBox.Show(ex.Message,"闲置提醒器");}};Controls.Add(b);}
 internal void Apply(Settings settings){var options=new InputModeNoticeOptions{Enabled=enabled.Checked,Applications=list.Items.Cast<string>().ToList()};options.Validate();if(pen)settings.PenInputModeNotices=options;else settings.InputModeNotices=options;}
}

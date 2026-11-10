// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class MediaScopePanel:FlowLayoutPanel {
 readonly ThemedComboBox scope=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=475};
 readonly ThemedCheckBox duck=new(){Text="播报期间临时压低指定播放器音量",AutoSize=true};
 readonly ThemedCheckBox coordinated=new(){Text="音乐已压低时允许提高主音量（其他声音可能变大）",AutoSize=true};
 readonly NumericUpDown remaining=new(){Minimum=1,Maximum=100,Value=20,Width=100};
 readonly TextBox apps=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Width=475,Height=110};
 readonly TextBox duckApps=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Width=475,Height=85};
 readonly List<Control> pauseControls=new();
 readonly ThemedComboBox current=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=475};
 internal MediaScopePanel(Settings settings){
  FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;Width=490;
  scope.Items.AddRange(new object[]{"全部：暂停所有可暂停媒体","仅暂停名单内的软件，其余保持播放","名单内的软件保持播放，其余可暂停媒体才暂停"});
  scope.SelectedIndex=(int)settings.MediaScope;
  duck.Checked=settings.DuckMediaOnReminder;coordinated.Checked=settings.AllowBoostWithOtherAudioAfterDucking;remaining.Value=settings.DuckRemainingPercent;
  // Subscribe after loading: reopening saved settings must respect manual opt-out.
  duck.CheckedChanged+=(_,_)=>{if(duck.Checked)coordinated.Checked=true;};
  apps.Text=string.Join(Environment.NewLine,settings.MediaApplications);
  duckApps.Text=string.Join(Environment.NewLine,settings.DuckApplications??settings.MediaApplications.Where(x=>x.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)));
  Controls.Add(new Label{Text="暂停范围（仅对上方选项勾选时生效）",AutoSize=true});
  Controls.Add(scope);Controls.Add(new Label{Text="软件名单：每行一个媒体应用标识，精确匹配，不区分大小写",AutoSize=true});Controls.Add(apps);
  var browse=new Button{Text="浏览软件…",AutoSize=true};
  browse.Click+=(_,_)=>{
   using var dialog=new OpenFileDialog{
    Title="选择要加入媒体名单的软件（不会运行它）",
    Filter="Windows 程序（*.exe）|*.exe",
    CheckFileExists=true,CheckPathExists=true,Multiselect=true,RestoreDirectory=true
   };
   if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
   try{
    var list=MediaApplicationFilter.Parse(apps.Text);
    foreach(var path in dialog.FileNames)
     foreach(var id in MediaApplicationFilter.ExecutableEntries(path))
      if(!list.Contains(id,StringComparer.OrdinalIgnoreCase))list.Add(id);
    apps.Text=string.Join(Environment.NewLine,list);
   }catch(Exception e) when(e is ArgumentException or System.IO.IOException or UnauthorizedAccessException){
    Store.Log("browse-media-app-failed",e);MessageBox.Show("无法添加所选程序："+e.Message);
   }
  };
  Controls.Add(browse);
  Controls.Add(new Label{
   Text="可选择一个或多个 .exe；不会运行或复制所选软件。\n将按文件名和完整路径加入名单，同名程序可能一起匹配。\n若媒体应用标识不同（如商店应用），请改用下方读取当前媒体软件。\n添加后请点击保存设置。",
   AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText
  });
  Controls.Add(current);
  var refresh=new Button{Text="读取当前媒体软件",AutoSize=true};
  refresh.Click+=async(_,_)=>{
   refresh.Enabled=false;
   try{var list=await MediaPause.ListApplicationsAsync();if(IsDisposed)return;current.Items.Clear();current.Items.AddRange(list);if(list.Length>0)current.SelectedIndex=0;else MessageBox.Show("当前没有 Windows 可识别的媒体软件。请先打开播放器并开始播放后重试。");}
   catch(Exception e){Store.Log("media-application-list-failed",e);if(!IsDisposed)MessageBox.Show("无法读取媒体软件，请稍后重试。");}
   finally{if(!refresh.IsDisposed)refresh.Enabled=true;}
  };
  Controls.Add(refresh);
  var add=new Button{Text="将所选软件加入名单",AutoSize=true};
  add.Click+=(_,_)=>{try{if(current.SelectedItem is string id){var list=MediaApplicationFilter.Parse(apps.Text);if(!list.Contains(id,StringComparer.OrdinalIgnoreCase))list.Add(id);apps.Text=string.Join(Environment.NewLine,list);}}catch(ArgumentException e){MessageBox.Show(e.Message);}};
  Controls.Add(add);
    pauseControls.AddRange(Controls.Cast<Control>());
  Controls.Add(duck);
  Controls.Add(new Label{Text="压低音量名单（独立于上方暂停名单）：每行一个 .exe 文件名或完整路径",AutoSize=true,MaximumSize=new Size(475,0)});
  Controls.Add(duckApps);
  var browseDuck=new Button{Text="浏览要压低音量的软件…",AutoSize=true};
  browseDuck.Click+=(_,_)=>{
   using var dialog=new OpenFileDialog{Title="选择要临时压低音量的播放器（不会运行它）",Filter="Windows 程序（*.exe）|*.exe",Multiselect=true,CheckFileExists=true,RestoreDirectory=true};
   if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
   try{
    var entries=ParseDuckApps(duckApps.Text);
    foreach(var path in dialog.FileNames)foreach(var entry in MediaApplicationFilter.ExecutableEntries(path))if(!entries.Contains(entry,StringComparer.OrdinalIgnoreCase))entries.Add(entry);
    duckApps.Text=string.Join(Environment.NewLine,entries);
   }catch(ArgumentException e){MessageBox.Show(e.Message);}
  };
  Controls.Add(browseDuck);
  Controls.Add(new Label{Text="保留原应用音量的百分比（例如 20%：原来 50% → 临时 10%）",AutoSize=true,MaximumSize=new Size(475,0)});Controls.Add(remaining);Controls.Add(coordinated);
  Controls.Add(new Label{Text="只压低本名单内仍有活动音频的播放器。\n“不暂停”不等于“不压低”；关闭暂停功能也可独立压低音乐。\n上方暂停范围和例外名单不会阻止这里的音量调整。\n浏览器会影响该软件的音频，不能按网址区分。\n提醒结束恢复；期间手动调整应用音量或静音则保留调整。\n开启压低音乐会同时勾选允许提高主音量，之后可单独取消。\n音乐进度继续；协调提升还需在设备音量页启用并确认目标。\n提高主音量时会进一步衰减同设备音乐，补偿实际分贝增益。\n切换瞬间音乐可能短暂静音，以避免先随主音量放大。\n勾选允许提升后，未列入名单的其他声音也可能随主音量变大。\n结束时先恢复主音量，再恢复播放器音量；普通试听不触发。",AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText});
  Controls.Add(new Label{Text="浏览器按整个软件处理，无法可靠按网址或标签页区分。\n本版不支持网址名单，不读取网页、历史或媒体标题。\n仅暂停名单模式下，空名单表示不暂停任何媒体。\n暂停名单与压低名单分别保存；两者不决定是否发出闲置提醒。\n未启用协调提升时，其他未压低的活动音频会阻止提高主音量。\n设置后请点击保存；普通试听不会暂停媒体。",AutoSize=true,MaximumSize=new Size(475,0)});
 }
 internal void SetPauseEnabled(bool enabled){foreach(var control in pauseControls)control.Enabled=enabled;}
 static List<string> ParseDuckApps(string text){var entries=MediaApplicationFilter.Parse(text);if(entries.Any(x=>!x.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("压低音量名单只支持 .exe 文件名或完整路径，请使用浏览按钮添加。");return entries;}
 internal void Apply(Settings settings){var list=MediaApplicationFilter.Parse(apps.Text);var reductions=ParseDuckApps(duckApps.Text);settings.DuckApplications=reductions;settings.MediaScope=(MediaPauseScope)scope.SelectedIndex;settings.MediaApplications=list;settings.AllowBoostWithOtherAudioAfterDucking=coordinated.Checked;settings.DuckMediaOnReminder=duck.Checked;settings.DuckRemainingPercent=(int)remaining.Value;}
}

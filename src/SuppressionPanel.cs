// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SuppressionPanel:FlowLayoutPanel {
 readonly DataGridView grid=new(){Width=475,Height=160,AutoGenerateColumns=false,AllowUserToAddRows=false,ReadOnly=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false};
 readonly TextBox app=new(){Width=475};
 readonly ThemedComboBox behavior=new(){Width=300,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly ThemedComboBox mediaApps=new(){Width=475,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly List<SuppressionRule> draft;
 internal SuppressionPanel(Settings s){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  draft=s.SuppressionRules.Select(r=>new SuppressionRule{Application=r.Application,Mode=r.Mode}).ToList();
  Controls.Add(new Label{Text="闲置提醒抑制规则（独立于媒体暂停名单）",AutoSize=true,Font=new Font(SystemFonts.MessageBoxFont!,FontStyle.Bold)});
  Controls.Add(new Label{Text="本区域受基础设置“按应用不提醒规则”总开关控制；关闭时保留名单但不执行。\n幻灯片抑制仍由独立页面控制。\n不暂停不等于不提醒。\n暂停名单决定媒体是否停播；下方规则决定闲置提醒是否发声。\n未配置的软件按普通闲置规则处理，多个规则任一符合即不提醒。",AutoSize=true,MaximumSize=new Size(475,0)});
  grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="软件 / 媒体应用标识",Width=285});
  grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="何时不提醒",Width=165});
  Controls.Add(grid);Controls.Add(app);
  behavior.Items.AddRange(new object[]{"播放中不提醒","运行中不提醒"});behavior.SelectedIndex=0;Controls.Add(behavior);
  var browse=AddButton("浏览软件…",()=>{
   using var dialog=new OpenFileDialog{Filter="Windows 程序|*.exe",CheckFileExists=true,RestoreDirectory=true};
   if(dialog.ShowDialog(FindForm())==DialogResult.OK)app.Text=behavior.SelectedIndex==1?dialog.FileName:Path.GetFileName(dialog.FileName);
  });
  browse.Enabled=behavior.SelectedIndex==1;
  behavior.SelectedIndexChanged+=(_,_)=>browse.Enabled=behavior.SelectedIndex==1;
  Controls.Add(mediaApps);
  var refresh=new Button{Text="读取当前媒体软件",AutoSize=true};
  refresh.Click+=async(_,_)=>{
   refresh.Enabled=false;
   try{var ids=await MediaPause.ListApplicationsAsync();if(IsDisposed)return;mediaApps.Items.Clear();mediaApps.Items.AddRange(ids);if(ids.Length>0){mediaApps.SelectedIndex=0;app.Text=ids[0];}else MessageBox.Show("没有可识别的媒体会话，请先开始播放。");}
   catch(Exception ex){Store.Log("suppression-app-list-failed",ex);if(!IsDisposed)MessageBox.Show("媒体接口不可用，请稍后重试。");}
   finally{if(!refresh.IsDisposed)refresh.Enabled=true;}
  };
  mediaApps.SelectedIndexChanged+=(_,_)=>{if(mediaApps.SelectedItem is string id)app.Text=id;};
  Controls.Add(refresh);
  AddButton("添加 / 更新规则",()=>{
   var rule=new SuppressionRule{Application=app.Text.Trim(),Mode=(SuppressionMode)behavior.SelectedIndex};SuppressionPolicy.Validate(rule);
   draft.RemoveAll(r=>r.Application.Equals(rule.Application,StringComparison.OrdinalIgnoreCase));draft.Add(rule);Render();
  });
  AddButton("删除所选规则",()=>{if(grid.CurrentRow is {} row){draft.RemoveAt(row.Index);Render();}});
  Controls.Add(new Label{Text="播放规则请先播放媒体，再用“读取当前媒体软件”添加，EXE名可能与媒体标识不同。\n“浏览软件”仅用于运行中不提醒规则。\n播放规则仅识别 Windows 媒体会话；音乐和视频均适用，不区分网址。\n运行规则检测 EXE 是否存在，最小化/后台也生效；不读取进程内存。\n浏览器整个软件运行或播放都会影响规则，请慎选。\n识别失败回退普通闲置规则；约每 2 秒检查一次。\n规则解除后按实际闲置时间判断，不补发堆积提醒。\n删除规则即可恢复该软件的普通检测。修改后点击保存设置。",AutoSize=true,MaximumSize=new Size(475,0)});
  Render();
 }
 Button AddButton(string text,Action action){var button=new Button{Text=text,AutoSize=true};button.Click+=(_,_)=>{try{action();}catch(ArgumentException e){MessageBox.Show(e.Message);}};Controls.Add(button);return button;}
 void Render(){grid.Rows.Clear();foreach(var r in draft)grid.Rows.Add(r.Application,r.Mode==SuppressionMode.WhilePlaying?"播放中不提醒":"运行中不提醒");}
 internal void Apply(Settings settings){foreach(var r in draft)SuppressionPolicy.Validate(r);settings.SuppressionRules=draft.Select(r=>new SuppressionRule{Application=r.Application,Mode=r.Mode}).ToList();}
}




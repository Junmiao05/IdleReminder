// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class CustomPauseForm:Form {
 readonly NumericUpDown minutes=new(){Minimum=1,Maximum=10080,Value=30,Width=140,ThousandsSeparator=true};
 internal int Minutes=>(int)minutes.Value;
 internal CustomPauseForm(){
  Text="自定义暂停";AutoScaleMode=AutoScaleMode.Dpi;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;
  FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterScreen;
  Icon=AppIcons.LoadTray();
  var panel=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(16)};
  var row=new FlowLayoutPanel{AutoSize=true,WrapContents=false};
  row.Controls.Add(new Label{Text="暂停时长（分钟）",AutoSize=true,Margin=new Padding(3,7,8,3)});row.Controls.Add(minutes);panel.Controls.Add(row);
  panel.Controls.Add(new Label{Text="可设置1～10,080分钟（最长7天）。\n到期自动恢复提醒；重新启动程序会自动启用。\n取消不会改变当前暂停状态。",AutoSize=true,MaximumSize=new Size(380,0),Margin=new Padding(3,10,3,10),ForeColor=SystemColors.GrayText});
  var buttons=new FlowLayoutPanel{AutoSize=true,WrapContents=false};
  var accept=new Button{Text="开始暂停",AutoSize=true,DialogResult=DialogResult.OK};
  var cancel=new Button{Text="取消",AutoSize=true,DialogResult=DialogResult.Cancel};
  buttons.Controls.Add(accept);buttons.Controls.Add(cancel);panel.Controls.Add(buttons);Controls.Add(panel);
  AcceptButton=accept;CancelButton=cancel;UiStyle.Apply(this);
  Shown+=(_,_)=>{minutes.Focus();minutes.Select(0,minutes.Text.Length);};
 }
}
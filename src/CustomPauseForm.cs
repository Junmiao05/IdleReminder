// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class CustomPauseForm:Form {
 readonly NumericUpDown minutes=new(){Minimum=1,Maximum=10080,Value=30,Width=140,ThousandsSeparator=true};
 internal int Minutes=>(int)minutes.Value;
 internal CustomPauseForm(){
  Text=UiText.Get("CustomPauseForm_001");AutoScaleMode=AutoScaleMode.Dpi;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;
  FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterScreen;
  Icon=AppIcons.LoadTray();
  var panel=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(16)};
  var row=new FlowLayoutPanel{AutoSize=true,WrapContents=false};
  row.Controls.Add(new Label{Text=UiText.Get("CustomPauseForm_002"),AutoSize=true,Margin=new Padding(3,7,8,3)});row.Controls.Add(minutes);panel.Controls.Add(row);
  panel.Controls.Add(new Label{Text=UiText.Get("CustomPauseForm_003"),AutoSize=true,MaximumSize=new Size(380,0),Margin=new Padding(3,10,3,10),ForeColor=SystemColors.GrayText});
  var buttons=new FlowLayoutPanel{AutoSize=true,WrapContents=false};
  var accept=new Button{Text=UiText.Get("CustomPauseForm_004"),AutoSize=true,DialogResult=DialogResult.OK};
  var cancel=new Button{Text=UiText.Get("SteamImportForm_007"),AutoSize=true,DialogResult=DialogResult.Cancel};
  buttons.Controls.Add(accept);buttons.Controls.Add(cancel);panel.Controls.Add(buttons);Controls.Add(panel);
  AcceptButton=accept;CancelButton=cancel;UiStyle.Apply(this);
  Shown+=(_,_)=>{minutes.Focus();minutes.Select(0,minutes.Text.Length);};
 }
}
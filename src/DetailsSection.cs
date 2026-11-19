// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class DetailsSection {
 internal static void Add(FlowLayoutPanel page,string name,int textWidth,params (string Key,bool Helper)[] paragraphs){
  var group=new FlowLayoutPanel{Name=name,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(3,4,3,10)};
  var toggle=new DetailsToggle{Text=UiText.Get("Layout_DetailsShow")};
  var content=new FlowLayoutPanel{Name=name+"Content",AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Visible=false};
  foreach(var paragraph in paragraphs)content.Controls.Add(new Label{Text=UiText.Get(paragraph.Key),UseMnemonic=false,AutoSize=true,MaximumSize=new Size(textWidth,0),Margin=new Padding(3,3,3,8),ForeColor=paragraph.Helper?SystemColors.GrayText:SystemColors.ControlText});
  toggle.LinkClicked+=(_,_)=>{
   toggle.Expanded=!toggle.Expanded;content.Visible=toggle.Expanded;
   toggle.Text=UiText.Get(toggle.Expanded?"Layout_DetailsHide":"Layout_DetailsShow");
   page.PerformLayout();
  };
  group.Controls.Add(toggle);group.Controls.Add(content);page.Controls.Add(group);
 }
}


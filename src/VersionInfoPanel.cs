// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
namespace IdleReminder;
internal sealed class VersionInfoPanel:FlowLayoutPanel {
 internal const string RepositoryUrl="https://github.com/Junmiao05/IdleReminder";
 internal static string Executable=>Environment.ProcessPath??Application.ExecutablePath;
 internal static string GuidePath {
  get {
   bool english=(UiText.CultureOverride??System.Globalization.CultureInfo.CurrentUICulture).TwoLetterISOLanguageName=="en";
   return GuideLocation.Resolve(english,AppContext.BaseDirectory,Store.Root);
  }
 }
 internal VersionInfoPanel(){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  AddText(UiText.Format("VersionInfoPanel_006",AppVersion.Current));
  PathField(UiText.Get("VersionInfoPanel_007"),Executable,"ExecutablePath");
  PathField(UiText.Get("VersionInfoPanel_008"),Store.Data,"DataPath");
  PathField(UiText.Get("VersionInfoPanel_009"),GuidePath,"GuidePath");
  var guide=new ThemedButton{Text=UiText.Get("VersionInfoPanel_001"),AutoSize=true};guide.Click+=(_,_)=>{
   try{if(!File.Exists(GuidePath)){MessageBox.Show(UiText.Get("VersionInfoPanel_002"));return;}Process.Start(new ProcessStartInfo(GuidePath){UseShellExecute=true});}
   catch(Exception e){Store.Log("guide-open-failed",e);MessageBox.Show(UiText.Get("VersionInfoPanel_003"));}
  };Controls.Add(guide);
  AddText(UiText.Get("VersionInfoPanel_010"),true);
  AddText(UiText.Get("VersionInfoPanel_011"),false);
  Controls.Add(new Label{Text=UiText.Get("VersionInfoPanel_004"),UseMnemonic=false,AutoSize=true,MaximumSize=new Size(475,0)});
  AddText(UiText.Get("VersionInfoPanel_012"));
  var repository=new LinkLabel{Text="GitHub："+RepositoryUrl,AutoSize=true,MaximumSize=new Size(475,0),UseMnemonic=false,Tag="Accent"};
  repository.LinkArea=new LinkArea("GitHub：".Length,RepositoryUrl.Length);
  repository.LinkClicked+=(_,_)=>{
   try{Process.Start(new ProcessStartInfo(RepositoryUrl){UseShellExecute=true});}
   catch(Exception ex){Store.Log("repository-open-failed",ex);MessageBox.Show(UiText.Get("VersionInfoPanel_005")+RepositoryUrl);}
  };
  Controls.Add(repository);
 }
 void AddText(string text,bool heading=false){var label=new Label{Text=text,AutoSize=true,MaximumSize=new Size(475,0)};if(heading)label.Font=new Font(Font,FontStyle.Bold);Controls.Add(label);}
 void PathField(string title,string path,string tag){AddText(title);Controls.Add(new TextBox{Text=path,ReadOnly=true,Multiline=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,Width=475,Height=58,Tag=tag});}
}





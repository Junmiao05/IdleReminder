// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
namespace IdleReminder;
internal sealed class VersionInfoPanel:FlowLayoutPanel {
 internal const string RepositoryUrl="https://github.com/Junmiao05/IdleReminder";
 internal static string Executable=>Environment.ProcessPath??Application.ExecutablePath;
 internal static string GuidePath=>File.Exists(Path.Combine(Store.Root,"使用指南.txt"))?Path.Combine(Store.Root,"使用指南.txt"):Path.Combine(AppContext.BaseDirectory,"使用指南.txt");
 internal VersionInfoPanel(){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  AddText($"当前运行版本：{AppVersion.Current}");
  PathField("当前程序完整位置（随实际运行位置变化）",Executable,"ExecutablePath");
  PathField("配置与录音目录",Store.Data,"DataPath");
  PathField("使用指南",GuidePath,"GuidePath");
  var guide=new Button{Text="打开使用指南",AutoSize=true};guide.Click+=(_,_)=>{
   try{if(!File.Exists(GuidePath)){MessageBox.Show("使用指南文件缺失，请保留完整发布包中的使用指南.txt。");return;}Process.Start(new ProcessStartInfo(GuidePath){UseShellExecute=true});}
   catch(Exception e){Store.Log("guide-open-failed",e);MessageBox.Show("无法打开使用指南，可手动打开上方显示的文件。");}
  };Controls.Add(guide);
  AddText("更新建议",true);
  AddText("1. 先从托盘退出旧程序，再更新完整发布文件；只关设置窗口不会退出。\n2. 更新前备份上方配置目录和项目 themes，保留私人录音、皮肤与设置。\n3. 尽量使用固定位置；本项目推荐 publish\\current。\n4. 更换位置后手动运行一次新版，检查版本及路径，并保存自启动设置。\n5. 旧实例先启动会阻止新版常驻；下次登录后再次核对托盘版本。\n6. 更新后检查人声、皮肤和设备目标，不覆盖私人 data/themes。\n目前没有自动补丁安装器；本页不会下载更新或修改自启动。\n本页信息只读，不需要保存。",false);
  Controls.Add(new Label{Text="开发：君喵&Codex",UseMnemonic=false,AutoSize=true,MaximumSize=new Size(475,0)});
  AddText("本项目完全免费");
  var repository=new LinkLabel{Text="GitHub："+RepositoryUrl,AutoSize=true,MaximumSize=new Size(475,0),UseMnemonic=false,Tag="Accent"};
  repository.LinkArea=new LinkArea("GitHub：".Length,RepositoryUrl.Length);
  repository.LinkClicked+=(_,_)=>{
   try{Process.Start(new ProcessStartInfo(RepositoryUrl){UseShellExecute=true});}
   catch(Exception ex){Store.Log("repository-open-failed",ex);MessageBox.Show("无法打开浏览器，可手动访问："+RepositoryUrl);}
  };
  Controls.Add(repository);
 }
 void AddText(string text,bool heading=false){var label=new Label{Text=text,AutoSize=true,MaximumSize=new Size(475,0)};if(heading)label.Font=new Font(Font,FontStyle.Bold);Controls.Add(label);}
 void PathField(string title,string path,string tag){AddText(title);Controls.Add(new TextBox{Text=path,ReadOnly=true,Multiline=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,Width=475,Height=58,Tag=tag});}
}





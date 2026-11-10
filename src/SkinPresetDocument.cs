// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Text.Json;
namespace IdleReminder;
public sealed class SkinPresetDocument {
 public string? CheckmarkImage {get;set;}
 internal string? ImageSource;
 public string Name {get;set;}="未命名";
 public Dictionary<string,string> Colors {get;set;}=Defaults();
 public static Dictionary<string,string> Defaults(){
  var c=new Dictionary<string,string>{["Window"]="#F5F6F8",["Surface"]="#FFFFFF",["Control"]="#FFFFFF",["Text"]="#202020",["Helper"]="#606060",["Accent"]="#0078D4",["Button"]="#F3F3F3",["ButtonText"]="#202020",["AccentText"]="#FFFFFF",["Border"]="#D1D5DB",["DisabledControl"]="#E5E7EB",["DisabledText"]="#808080"};
  foreach(var p in CheckBoxPalette.Defaults())c["CheckBox."+p.Key]=p.Value;ControlPalette.FillMissing(c);return c;
 }
 public void Validate(){
  if(!CheckmarkPath.IsSafe(CheckmarkImage))throw new ArgumentException("勾号PNG路径无效");
  if(Colors==null)throw new ArgumentException("方案颜色为空");
  ControlPalette.FillMissing(Colors);
  foreach(var p in Defaults()){
   if(!Colors.TryGetValue(p.Key,out var color)){Colors[p.Key]=p.Value;continue;}
   if(!System.Text.RegularExpressions.Regex.IsMatch(color??"",@"^#[a-fA-F0-9]{6}$"))throw new ArgumentException("方案包含无效色值");
  }
 }
 public static bool IsSafeFilename(string file)=>!string.IsNullOrWhiteSpace(file)&&file.Length<=240&&!file.Contains('/')&&!file.Contains('\\')&&!file.Contains(':')&&file.EndsWith(".json",StringComparison.OrdinalIgnoreCase);
 public static SkinPresetDocument Load(string path){
  if(new FileInfo(path).Length>131072)throw new ArgumentException("方案文件过大");
  var doc=JsonSerializer.Deserialize<SkinPresetDocument>(File.ReadAllText(path))??throw new ArgumentException("无效方案");doc.Validate();if(!string.IsNullOrEmpty(doc.CheckmarkImage)){doc.ImageSource=CheckmarkPath.Resolve(Path.GetDirectoryName(Path.GetFullPath(path))!,doc.CheckmarkImage);}return doc;
 }
}




// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class GuideLocation {
 internal static string Resolve(bool english,string programDirectory,string projectDirectory){
  var chineseHtml=Path.Combine("docs","UserGuide.zh-Hans.html");
  var englishHtml=Path.Combine("docs","UserGuide.en.html");
  var candidates=english?new[]{englishHtml,"GUIDE.en.txt",chineseHtml,"使用指南.txt"}:new[]{chineseHtml,"使用指南.txt"};
  foreach(var file in candidates)
   foreach(var folder in new[]{programDirectory,projectDirectory}){
    var path=Path.Combine(folder,file);if(File.Exists(path))return path;
   }
  return Path.Combine(programDirectory,english?englishHtml:chineseHtml);
 }
}

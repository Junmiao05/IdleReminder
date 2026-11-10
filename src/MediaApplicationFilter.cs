// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public enum MediaPauseScope { All, OnlyListed, ExceptListed }
public static class MediaApplicationFilter {
 public static bool ShouldPause(string appId,MediaPauseScope scope,IEnumerable<string> list){
  if(scope==MediaPauseScope.All)return true;
  bool matched=!string.IsNullOrWhiteSpace(appId)&&list.Any(x=>string.Equals(x.Trim(),appId.Trim(),StringComparison.OrdinalIgnoreCase));
  return scope==MediaPauseScope.OnlyListed?matched:!matched&&!string.IsNullOrWhiteSpace(appId);
 }
 public static string[] ExecutableEntries(string path){
  if(!System.IO.Path.IsPathFullyQualified(path)||!string.Equals(System.IO.Path.GetExtension(path),".exe",StringComparison.OrdinalIgnoreCase))
   throw new ArgumentException("请选择软件的 .exe 文件，不支持快捷方式或其他文件。");
  var full=System.IO.Path.GetFullPath(path);
  return [System.IO.Path.GetFileName(full),full];
 }
 public static List<string> Parse(string text){
  var entries=text.Split(['\r','\n'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  if(entries.Any(x=>x.Contains("://")||x.StartsWith("www.",StringComparison.OrdinalIgnoreCase)||System.Text.RegularExpressions.Regex.IsMatch(x,@"^[\w.-]+\.(com|net|org|cn|tv|io)(/.*)?$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)||x.Length>512))
   throw new ArgumentException("这里只支持软件的媒体应用标识，不支持网址。请从当前媒体列表添加。");
  return entries;
 }
}



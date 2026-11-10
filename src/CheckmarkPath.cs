// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class CheckmarkPath {
 public static string Resolve(string folder,string relative){
  if(string.IsNullOrEmpty(relative)||!IsSafe(relative))throw new ArgumentException("勾号资源路径无效。");
  return System.IO.Path.Combine(folder,relative.Replace('/',System.IO.Path.DirectorySeparatorChar));
 }
 public static bool IsSafe(string? path){
  if(string.IsNullOrEmpty(path))return true;
  return System.Text.RegularExpressions.Regex.IsMatch(path,@"^assets/[A-Za-z0-9_-]{1,100}\.png$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
 }
}


// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public sealed class InputModeNoticeOptions {
 public bool Enabled {get;set;}=true;
 public List<string> Applications {get;set;}=new();
 public void Validate(){
  Applications ??= new();
  if(Applications.Count>512)throw new ArgumentException("检测方式提示名单最多支持512项。");
  Applications=Applications.Select(Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
 }
 public static string Normalize(string value){
  value=value?.Trim()??"";
  if(value.Length==0||value.Length>32767||value.Contains('\n')||value.Contains('\r')||value.Contains('"')||value.Contains("://")||!value.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("请填写EXE文件名或完整路径，不支持网址、快捷方式或启动参数。");
  if(Path.IsPathFullyQualified(value))return Path.GetFullPath(value);
  if(value.Contains('/')||value.Contains('\\')||value.Contains(':')||value.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new ArgumentException("请填写单独的EXE文件名，或通过浏览选择完整路径。");
  return value;
 }
}
public sealed class InputModeNoticePolicy {
 readonly HashSet<string> shown=new(StringComparer.OrdinalIgnoreCase);
 readonly InputTimingMode sourceMode;
 public InputModeNoticePolicy(InputTimingMode sourceMode=InputTimingMode.WindowsLastInput){this.sourceMode=sourceMode;}
 long? lastShown;
 public bool Eligible(InputModeNoticeOptions options,InputTimingMode mode)=>options.Enabled&&mode==sourceMode&&options.Applications.Count>0;
 public string[] Pending(InputModeNoticeOptions options)=>options.Applications.Where(x=>!shown.Contains(x)).ToArray();
 public bool CanShow(long now)=>lastShown==null||now-lastShown.Value>=60000;
 public bool TryMark(string application,long now){if(!CanShow(now)||!shown.Add(application))return false;lastShown=now;return true;}
}

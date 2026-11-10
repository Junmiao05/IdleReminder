// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public sealed class SlideWindowRule {
 public string ProcessName {get;set;}="wpp.exe";
 public string WindowClass {get;set;}="";
}
public sealed class SlideShowOptions {
 public bool Enabled {get;set;}=true;
 public bool PowerPoint {get;set;}=true;
 public bool Wps {get;set;}=true;
 public List<SlideWindowRule> WindowRules {get;set;}=new();
 public SlideShowOptions Copy()=>new(){Enabled=Enabled,PowerPoint=PowerPoint,Wps=Wps,WindowRules=WindowRules.Select(x=>new SlideWindowRule{ProcessName=x.ProcessName,WindowClass=x.WindowClass}).ToList()};
}
public static class SlideShowPolicy {
 public static bool IsPresentationProcess(string process)=>process.Equals("powerpnt.exe",StringComparison.OrdinalIgnoreCase)||process.Equals("wpp.exe",StringComparison.OrdinalIgnoreCase);
 public static bool IsShowWindow(string process,string windowClass,bool visible,SlideShowOptions options){
  if(!options.Enabled||!visible||!IsPresentationProcess(process))return false;
  bool ppt=process.Equals("powerpnt.exe",StringComparison.OrdinalIgnoreCase);
  if(ppt&&!options.PowerPoint||!ppt&&!options.Wps)return false;
  if(ppt&&windowClass.Equals("screenClass",StringComparison.Ordinal))return true;
  return options.WindowRules.Any(r=>r.ProcessName.Equals(process,StringComparison.OrdinalIgnoreCase)&&r.WindowClass.Equals(windowClass,StringComparison.Ordinal));
 }
 public static void Validate(SlideWindowRule rule){
  if(!IsPresentationProcess(rule.ProcessName)||string.IsNullOrWhiteSpace(rule.WindowClass)||rule.WindowClass.Length>256)
   throw new ArgumentException("仅允许 POWERPNT.EXE / wpp.exe 的具体窗口类名，不能使用全屏或通配符规则。");
  if(rule.WindowClass.Contains('*')||rule.WindowClass.Contains('?'))throw new ArgumentException("窗口类名必须精确匹配。");
 }
}

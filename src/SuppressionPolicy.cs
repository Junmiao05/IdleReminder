// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public enum SuppressionMode { WhilePlaying, WhileRunning }
public sealed class SuppressionRule {
 public string Application {get;set;}="";
 public SuppressionMode Mode {get;set;}
}
public sealed record SuppressionPlan(SuppressionRule[] Rules,SlideShowOptions Slides) {
 public bool NeedsCheck=>Rules.Length>0||Slides.Enabled;
}
public static class SuppressionPolicy {
 public static SuppressionPlan Capture(Settings settings)=>new(
  settings.AppSuppressionEnabled?settings.SuppressionRules.Select(r=>new SuppressionRule{Application=r.Application,Mode=r.Mode}).ToArray():Array.Empty<SuppressionRule>(),
  settings.SlideShow.Copy());
 public static bool IsMatch(SuppressionRule rule,IEnumerable<string> playing,IEnumerable<string> running)
  => (rule.Mode==SuppressionMode.WhileRunning?running:playing).Any(id=>string.Equals(id,rule.Application,StringComparison.OrdinalIgnoreCase));
 public static void Validate(SuppressionRule rule){
  if(string.IsNullOrWhiteSpace(rule.Application)||MediaApplicationFilter.Parse(rule.Application).Count!=1)
   throw new ArgumentException(UiText.Get("SuppressionPolicy_001"));
  if(!Enum.IsDefined(rule.Mode))throw new ArgumentException(UiText.Get("SuppressionPolicy_002"));
  if(rule.Mode==SuppressionMode.WhileRunning&&!rule.Application.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))
   throw new ArgumentException(UiText.Get("SuppressionPolicy_003"));
 }
}


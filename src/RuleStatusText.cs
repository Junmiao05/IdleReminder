// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class RuleStatusText {
 public static string Get(bool active,bool querying,bool hasResult,bool slideshow,bool waitingInput,string enabledText){
  if(active)return slideshow?UiText.Get("RuleStatusText_001"):UiText.Get("RuleStatusText_002");
  if(waitingInput)return UiText.Get("RuleStatusText_003");
  if(querying&&!hasResult)return UiText.Get("RuleStatusText_004");
  return enabledText;
 }
}

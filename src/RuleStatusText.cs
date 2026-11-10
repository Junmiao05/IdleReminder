// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class RuleStatusText {
 public static string Get(bool active,bool querying,bool hasResult,bool slideshow,bool waitingInput,string enabledText){
  if(active)return slideshow?"幻灯片放映中，暂停闲置提醒":"按应用规则暂停闲置提醒";
  if(waitingInput)return "正在等待用户恢复操作";
  if(querying&&!hasResult)return "正在检查应用提醒规则";
  return enabledText;
 }
}

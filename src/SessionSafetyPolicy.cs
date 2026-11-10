// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class SessionSafetyPolicy {
 public static bool IsUnlocked(int level,int state,int flags)=>level==1&&state==0&&flags==1;
 public static PowerTransition PowerEvent(int message)=>message switch{4=>PowerTransition.Suspend,7 or 18=>PowerTransition.Resume,_=>PowerTransition.Other};
}

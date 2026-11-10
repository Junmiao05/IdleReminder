// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
// Only an unambiguously single-process session owned by us may be ignored.
public readonly record struct AudioSessionState(int State,uint ProcessId,int ProcessResult);
public static class AudioSessionPolicy {
 public static bool HasOtherActive(IEnumerable<AudioSessionState> sessions,uint ownProcess)
  =>sessions.Any(s=>s.State==1&&(s.ProcessResult!=0||s.ProcessId!=ownProcess));
 public static bool CanBoost(bool querySucceeded,bool otherActive)=>querySucceeded&&!otherActive;
}

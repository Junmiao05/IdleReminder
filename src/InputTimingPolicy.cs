// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public enum InputTimingMode { WindowsLastInput, DeviceHeaders }
public readonly record struct ActivitySample(ulong Stamp,long IdleMilliseconds);
public sealed class HeaderActivityClock {
 long lastActivity;uint revision;
 public HeaderActivityClock(long now){lastActivity=now;}
 public bool Observe(uint deviceType,long now){
  if(deviceType is not (0 or 1))return false;
  lastActivity=now;unchecked{revision++;}return true;
 }
 public ActivitySample Read(long now)=>new(revision,Math.Max(0,now-lastActivity));
}
public static class InputTimingPolicy {
 public static ActivitySample WithGeneration(ActivitySample sample,uint generation)=>new(((ulong)generation<<32)|(uint)sample.Stamp,sample.IdleMilliseconds);
 public static bool Changed(ActivitySample baseline,ActivitySample current)=>baseline.Stamp!=current.Stamp;
}

public enum InputAccessState { Allowed, HigherForeground, Unavailable }
public static class DeviceHeaderPolicy {
 public static bool Accept(uint type,bool hasDevice,bool requireDevice)=>type is 0 or 1&&(!requireDevice||hasDevice);
 public static InputAccessState Access(int? receiver,int? foreground)
  =>receiver==null||foreground==null?InputAccessState.Unavailable:foreground>receiver?InputAccessState.HigherForeground:InputAccessState.Allowed;
}

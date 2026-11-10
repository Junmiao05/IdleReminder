// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class ElevationPolicy {
 public static bool NeedsSwitchRequest(InputTimingMode mode,bool alreadyAdmin)=>mode==InputTimingMode.DeviceHeaders&&!alreadyAdmin;
 public const string RequestedFlag="--elevation-requested";
 public static bool NeedsRequest(InputTimingMode mode,bool readableConfig,bool alreadyAdmin,bool alreadyRequested)
  =>readableConfig&&mode==InputTimingMode.DeviceHeaders&&!alreadyAdmin&&!alreadyRequested;
 public static string[] ChildArguments(IEnumerable<string> args)=>args.Where(a=>a!=RequestedFlag).Append(RequestedFlag).ToArray();
}

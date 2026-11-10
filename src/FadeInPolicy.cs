// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class FadeInPolicy {
 internal const long DurationMilliseconds=1000;
 internal static double Gain(double target,long elapsedMilliseconds)=>Math.Clamp(target,0,1)*Math.Clamp(elapsedMilliseconds/(double)DurationMilliseconds,0,1);
}

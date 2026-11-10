// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class StartupCommand {
 internal static string For(string executable)=>$"\"{executable}\" --background";
 internal static bool Matches(string? stored,string executable)=>string.Equals(stored?.Trim(),For(executable),StringComparison.OrdinalIgnoreCase);
}

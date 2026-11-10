// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class AppVersion {
 internal static string Current=>typeof(AppVersion).Assembly.GetName().Version?.ToString()??"0.0.0.0";
}

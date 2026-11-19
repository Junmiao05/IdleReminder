// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class PermissionStatusText {
 public static string Get(bool? admin,bool device)=>admin switch {
  true=>UiText.Get("PermissionStatusText_001"),
  false=>device?UiText.Get("PermissionStatusText_002"):UiText.Get("PermissionStatusText_003"),
  null=>UiText.Get("PermissionStatusText_004")
 };
}

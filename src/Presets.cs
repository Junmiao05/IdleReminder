// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class Presets {
 public static readonly string[] Names = ["清脆铃声","电子提示音","柔和提示音","短促警报音","经典系统风格"];
 public static readonly string[] FileNames = ["tone0.wav","tone1.wav","tone2.wav","tone3.wav","tone4.wav"];
 public static readonly string[] VoiceNames = [];
 public static readonly string[] VoiceFileNames = [];
 public static int Count => FileNames.Length;
}

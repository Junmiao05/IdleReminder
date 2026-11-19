// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class Presets {
 public static string[] Names => [UiText.Get("Presets_001"),UiText.Get("Presets_002"),UiText.Get("Presets_003"),UiText.Get("Presets_004"),UiText.Get("Presets_005")];
 public static readonly string[] FileNames = ["tone0.wav","tone1.wav","tone2.wav","tone3.wav","tone4.wav"];
 public static readonly string[] VoiceNames = [];
 public static readonly string[] VoiceFileNames = [];
 public static int Count => FileNames.Length;
}

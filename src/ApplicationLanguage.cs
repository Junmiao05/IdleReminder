// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Globalization;

namespace IdleReminder;

public static class LanguagePolicy {
 public const string SimplifiedChinese = "zh-Hans";
 public const string English = "en";

 public static string Normalize(string? language) =>
  string.Equals(language, English, StringComparison.OrdinalIgnoreCase)
   ? English
   : SimplifiedChinese;
}

internal static class ApplicationLanguage {
 internal static void Apply(string? language) {
  var culture = CultureInfo.GetCultureInfo(LanguagePolicy.Normalize(language));
  UiText.CultureOverride = culture;
  CultureInfo.DefaultThreadCurrentUICulture = culture;
  CultureInfo.CurrentUICulture = culture;
 }
}

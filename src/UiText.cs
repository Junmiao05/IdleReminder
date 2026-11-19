// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Globalization;
using System.Resources;
namespace IdleReminder;
internal static class UiText {
 static readonly ResourceManager resources=new("IdleReminder.Resources.Strings",typeof(UiText).Assembly);
 // Apply the choice even to continuations that captured an older UI culture.
 internal static volatile CultureInfo? CultureOverride;
 internal static string Get(string key)=>Get(key,CultureOverride??CultureInfo.CurrentUICulture);
 internal static string Get(string key,CultureInfo culture)=>resources.GetString(key,culture)
  ??throw new MissingManifestResourceException("Missing UI resource: "+key);
 internal static string Format(string key,params object?[] args)
  =>string.Format(CultureInfo.CurrentCulture,Get(key),args);
}

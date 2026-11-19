// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class AppIcons {
 internal static Icon LoadWindow()=>Load("App",SystemInformation.IconSize);
 internal static Icon LoadTray()=>Load("Tray",SystemInformation.SmallIconSize);
 static Icon Load(string kind,Size size){
  using var stream=typeof(AppIcons).Assembly.GetManifestResourceStream("IdleReminder.Icons."+kind+".ico")
   ??throw new InvalidDataException(UiText.Get("AppIcons_001"));
  return new Icon(stream,size);
 }
}

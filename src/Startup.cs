// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using Microsoft.Win32;
namespace IdleReminder;
internal static class Startup {
 const string Key=@"Software\Microsoft\Windows\CurrentVersion\Run";
 static string? Command {get{using var key=Registry.CurrentUser.OpenSubKey(Key);return key?.GetValue("IdleReminder") as string;}}
 internal static bool Enabled=>Command!=null;
 internal static void RepairExisting(){var command=Command;if(command!=null&&!StartupCommand.Matches(command,Environment.ProcessPath!))Set(true);}
 internal static void Set(bool enabled){using var key=Registry.CurrentUser.CreateSubKey(Key);if(enabled)key.SetValue("IdleReminder",StartupCommand.For(Environment.ProcessPath!));else key.DeleteValue("IdleReminder",false);}
}

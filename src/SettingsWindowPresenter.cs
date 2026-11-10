// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
internal static class SettingsWindowPresenter {
 internal static bool Trace;
 internal static void Log(string state){if(Trace)Store.Log("settings-window-"+state);}
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr window,int command);
 [DllImport("user32.dll")]internal static extern bool IsWindowVisible(IntPtr window);
 internal static void Show(Form form){
  if(form.WindowState==FormWindowState.Minimized)form.WindowState=FormWindowState.Normal;
  form.Show();
  // An SW_HIDE startup hint can suppress the first ShowWindow despite Visible=true.
  // This explicit second native show is only used after the user's open request.
  ShowWindow(form.Handle,5);
  form.BringToFront();form.Activate();SetForegroundWindow(form.Handle);
  Log($"present-visible-{IsWindowVisible(form.Handle)}-foreground-{GetForegroundWindow()==form.Handle}");
 }
}

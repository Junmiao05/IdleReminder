// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
namespace IdleReminder;
internal static class InputModeApplicationProbe {
 internal static string? FindRunning(string[] applications,CancellationToken token){
  var names=applications.GroupBy(x=>Path.GetFileNameWithoutExtension(x),StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.OrdinalIgnoreCase);
  var processes=Process.GetProcesses();
  try{
   foreach(var process in processes){
    token.ThrowIfCancellationRequested();
    try{
     if(!names.TryGetValue(process.ProcessName,out var entries))continue;
     foreach(var entry in entries){
      if(!Path.IsPathFullyQualified(entry))return entry;
      if(string.Equals(SuppressionDetector.ImagePath(process.Id),entry,StringComparison.OrdinalIgnoreCase))return entry;
     }
    }catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException){/* Process exited or metadata unavailable: do not guess. */}
   }
   return null;
  }finally{foreach(var process in processes)process.Dispose();}
 }
}

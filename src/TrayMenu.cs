// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class TrayMenu {
 sealed record Caption(string Key,object[] Args);
 internal static ContextMenuStrip Create(Action enable,Action<int?> pause,Action open,Action exit){
  var menu=new ContextMenuStrip();
  void Add(string key,Action action,params object[] args){
   var item=new ToolStripMenuItem{Tag=new Caption(key,args)};
   item.Click+=(_,_)=>{menu.Close();action();};
   menu.Items.Add(item);
  }
  Add("Tray_001",enable);
  foreach(var minutes in new[]{30,60,120})Add("Tray_023",()=>pause(minutes),minutes);
  Add("Tray_002",()=>{using var dialog=new CustomPauseForm();if(dialog.ShowDialog()==DialogResult.OK)pause(dialog.Minutes);});
  Add("Tray_003",()=>pause(null));
  Add("Tray_024",()=>{},AppVersion.Current);menu.Items[^1].Enabled=false;
  Add("Tray_004",open);Add("Tray_005",exit);
  Refresh(menu);return menu;
 }
 internal static void Refresh(ContextMenuStrip menu){
  foreach(ToolStripItem item in menu.Items)
   if(item.Tag is Caption caption)item.Text=UiText.Format(caption.Key,caption.Args);
 }
}

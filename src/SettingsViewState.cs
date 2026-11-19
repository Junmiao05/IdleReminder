// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SettingsViewState {
 readonly Rectangle bounds;
 readonly FormWindowState windowState;
 readonly int tab;
 readonly HashSet<string> expanded=new();
 readonly Point[] scroll;
 static IEnumerable<Control> Descendants(Control parent){
  foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}
 }
 SettingsViewState(SettingsForm form){
  bounds=form.WindowState==FormWindowState.Normal?form.Bounds:form.RestoreBounds;
  windowState=form.WindowState;tab=form.ViewTabs.SelectedIndex;
  scroll=form.ViewTabs.TabPages.Cast<TabPage>().Select(p=>((ScrollableControl)p.Controls[0]).AutoScrollPosition).ToArray();
  foreach(var toggle in Descendants(form).OfType<DetailsToggle>())if(toggle.Expanded)expanded.Add(toggle.Parent!.Name);
 }
 internal static SettingsViewState Capture(SettingsForm form)=>new(form);
 internal void Show(SettingsForm form){
  form.StartPosition=FormStartPosition.Manual;form.Bounds=bounds;
  form.ViewTabs.SelectedIndex=tab;
  foreach(var toggle in Descendants(form).OfType<DetailsToggle>()){
   if(!expanded.Contains(toggle.Parent!.Name))continue;
   toggle.Expanded=true;
   toggle.Text=UiText.Get("Layout_DetailsHide");
   toggle.Parent.Controls[1].Visible=true;
  }
  form.Show();form.WindowState=windowState;
  for(int i=0;i<scroll.Length;i++)((ScrollableControl)form.ViewTabs.TabPages[i].Controls[0]).AutoScrollPosition=new Point(-scroll[i].X,-scroll[i].Y);
 }
}

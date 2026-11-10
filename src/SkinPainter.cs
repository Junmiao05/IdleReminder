// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.CompilerServices;
namespace IdleReminder;
internal static class SkinPainter {
 sealed class ButtonColors {internal Color Back,Fore,DisabledBack,DisabledFore; internal void Apply(Button button){button.BackColor=button.Enabled?Back:DisabledBack;button.ForeColor=button.Enabled?Fore:DisabledFore;}}
 static readonly ConditionalWeakTable<Button,ButtonColors> buttonColors=new();
 sealed class TextRole {internal bool Helper;}
 static readonly ConditionalWeakTable<Label,TextRole> roles=new();
 internal static void Apply(Form form,SkinPresetDocument skin){
  Color C(string key)=>ColorTranslator.FromHtml(skin.Colors[key]);
  var checkbox=new CheckBoxPalette();foreach(var key in checkbox.Colors.Keys.ToArray())checkbox.Colors[key]=skin.Colors["CheckBox."+key];
  ThemedCheckBox.ApplyPalette(checkbox);
  using(var mark=skin.ImageSource==null?null:CheckmarkAsset.LoadPng(skin.ImageSource))ThemedCheckBox.ApplyCheckmark(mark);
  form.BackColor=C("Window");
  void Visit(Control parent){
   foreach(Control c in parent.Controls){
    c.ForeColor=C("Text");
    if(c is TabPage)c.BackColor=C("Window");
    else if(c is FlowLayoutPanel)c.BackColor=c.GetType()==typeof(FlowLayoutPanel)?parent.BackColor:C("Surface");
    else if(c is Label l){
     var role=roles.GetValue(l,label=>new(){Helper=label.ForeColor==Color.FromArgb(96,96,96)||label.ForeColor==SystemColors.GrayText});
     l.BackColor=parent.BackColor;l.ForeColor=C(Equals(l.Tag,"Accent")?"Accent":role.Helper?"Helper":"Text");
     if(l is LinkLabel link){link.LinkColor=C("Accent");link.ActiveLinkColor=C("Text");link.VisitedLinkColor=C("Accent");}
    }else if(c is Button b){
     bool primary=(b.Text=="保存设置"&&b.Dock==DockStyle.Bottom)||Equals(b.Tag,"Primary");
     b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=C("Border");
     var colors=buttonColors.GetValue(b,button=>{var value=new ButtonColors();button.EnabledChanged+=(_,_)=>value.Apply(button);return value;});
     colors.Back=C(primary?"Accent":"Button");colors.Fore=C(primary?"AccentText":"ButtonText");colors.DisabledBack=C("DisabledControl");colors.DisabledFore=C("DisabledText");colors.Apply(b);
    }else if(c is TextBoxBase or ComboBox or NumericUpDown or ListBox or DataGridView){
     c.BackColor=C(c.Enabled?"Control":"DisabledControl");c.ForeColor=C(c.Enabled?"Text":"DisabledText");
     if(c is DataGridView grid){grid.BackgroundColor=C("Control");grid.GridColor=C("Border");grid.DefaultCellStyle.BackColor=C("Control");grid.DefaultCellStyle.ForeColor=C("Text");grid.DefaultCellStyle.SelectionBackColor=C("Accent");grid.DefaultCellStyle.SelectionForeColor=C("AccentText");}
    }else{c.BackColor=parent.BackColor;if(!c.Enabled)c.ForeColor=C("DisabledText");}
    if(c is ThemedComboBox combo)combo.SetColors(skin.Colors);
    if(c is ThemedTrackBar slider)slider.SetColors(skin.Colors);
    Visit(c);
   }
  }
  // Capture helper roles before changing ForeColor.
  foreach(var l in Labels(form))roles.GetValue(l,label=>new(){Helper=label.ForeColor==Color.FromArgb(96,96,96)||label.ForeColor==SystemColors.GrayText});
  Visit(form);form.Invalidate(true);
 }
 static IEnumerable<Label> Labels(Control parent){foreach(Control c in parent.Controls){if(c is Label l)yield return l;foreach(var label in Labels(c))yield return label;}}
}



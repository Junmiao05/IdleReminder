// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public sealed class CheckBoxPalette {
 public Dictionary<string,string> Colors {get;set;}=Defaults();
 public static Dictionary<string,string> Defaults()=>new(){
  ["CheckedFill"]="#0078D4",["CheckedBorder"]="#0078D4",["Checkmark"]="#FFFFFF",
  ["UncheckedFill"]="#FFFFFF",["UncheckedBorder"]="#D1D5DB",
  ["DisabledFill"]="#E5E7EB",["DisabledBorder"]="#9CA3AF",["DisabledCheckmark"]="#FFFFFF"
 };
 public void Validate(){
  Colors??=new();
  foreach(var item in Defaults())
   if(!Colors.TryGetValue(item.Key,out var color)||!System.Text.RegularExpressions.Regex.IsMatch(color??"",@"^#[0-9a-fA-F]{6}$"))Colors[item.Key]=item.Value;
 }
 public CheckBoxPalette Copy()=>new(){Colors=new(Colors)};
}

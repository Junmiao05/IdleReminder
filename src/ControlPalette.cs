// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class ControlPalette {
 public static readonly Dictionary<string,string> Fallbacks=new(){
  ["SelectionBackground"]="Accent",["SelectionText"]="AccentText",["SliderTrack"]="Border",
  ["SliderFill"]="Accent",["SliderThumb"]="Accent",["SliderTicks"]="Helper",["SliderDisabled"]="DisabledControl"
 };
 public static void FillMissing(Dictionary<string,string> colors){
  foreach(var pair in Fallbacks)if(!colors.ContainsKey(pair.Key)&&colors.TryGetValue(pair.Value,out var color))colors[pair.Key]=color;
 }
}

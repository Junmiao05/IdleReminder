// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
// Visual-only styling; no configuration, detection or playback behavior.
internal static class UiStyle {
 static readonly Color TextColor=Color.FromArgb(32,32,32);
 static readonly Color HintColor=Color.FromArgb(96,96,96);
 internal static Color ReadableText(Color background){
  double Channel(byte c){double v=c/255.0;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
  double lum=.2126*Channel(background.R)+.7152*Channel(background.G)+.0722*Channel(background.B);
  return 1.05/(lum+.05)>=(lum+.05)/.05?Color.White:Color.Black;
 }
 internal static void Apply(Form form){
  // Local fonts only: use the existing Windows/WinForms font family.
  form.Font=new Font(form.Font.FontFamily,9F,FontStyle.Regular);
  form.BackColor=Color.FromArgb(245,246,248);
  Visit(form);
 }
 static void Visit(Control parent){
  foreach(Control control in parent.Controls){
   if(control is TabPage){control.BackColor=Color.White;control.ForeColor=TextColor;}
   else if(control is FlowLayoutPanel){control.BackColor=Color.Transparent;}
   else if(control is Label label){
    bool helper=label.ForeColor==SystemColors.GrayText;
    bool heading=label.Font.Bold||label.Text.StartsWith("复选框颜色");
    if(heading)label.Font=new Font(label.Font.FontFamily,10F,FontStyle.Bold);
    label.ForeColor=helper?HintColor:TextColor;
    // Keep padding outside the full font line, including Chinese lower strokes.
    label.MinimumSize=new Size(label.MinimumSize.Width,Math.Max(label.MinimumSize.Height,label.Font.Height+label.Padding.Vertical+4));
    // Fixed-height padded labels clipped Chinese descenders at higher scaling.
    if(!label.AutoSize){
     int width=label.Width;label.AutoSize=true;
     label.MinimumSize=new Size(width,label.Font.Height+label.Padding.Vertical+4);label.MaximumSize=new Size(width,0);
    }
   }else if(control is Button button){
    if(!button.UseVisualStyleBackColor)button.ForeColor=ReadableText(button.BackColor);
    if(button.AutoSize)button.MinimumSize=new Size(0,button.Font.Height+12);
   }else if(control is NumericUpDown number){number.Height=Math.Max(number.Height,number.PreferredHeight);}
   else if(control is DataGridView grid){grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;}
   Visit(control);
  }
 }
}


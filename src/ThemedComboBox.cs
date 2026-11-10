// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal class ThemedComboBox:ComboBox {
 internal Color SelectedBackground=SystemColors.Highlight,SelectedForeground=SystemColors.HighlightText;
 public ThemedComboBox(){DrawMode=DrawMode.OwnerDrawFixed;}
 internal void SetColors(IReadOnlyDictionary<string,string> colors){
  SelectedBackground=ColorTranslator.FromHtml(colors["SelectionBackground"]);SelectedForeground=ColorTranslator.FromHtml(colors["SelectionText"]);Invalidate();
 }
 protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);ItemHeight=Font.Height+4;}
 protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);ItemHeight=Font.Height+4;Invalidate();}
 protected override void OnDrawItem(DrawItemEventArgs e){
  bool selected=(e.State&DrawItemState.Selected)!=0;
  using var background=new SolidBrush(selected?SelectedBackground:BackColor);e.Graphics.FillRectangle(background,e.Bounds);
  string text=e.Index>=0&&e.Index<Items.Count?GetItemText(Items[e.Index])??"":Text;
  TextRenderer.DrawText(e.Graphics,text,e.Font??Font,Rectangle.Inflate(e.Bounds,-2,0),Enabled?(selected?SelectedForeground:ForeColor):SystemColors.GrayText,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
  if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();
  base.OnDrawItem(e);
 }
}


// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Drawing.Drawing2D;
using System.Windows.Forms.VisualStyles;
using ContentAlignment = System.Drawing.ContentAlignment;
namespace IdleReminder;
// Preserve native text/focus drawing, accessibility and input behavior.
// Repaint only the checkbox glyph after the normal WinForms renderer.
internal class ThemedCheckBox:CheckBox {
 static CheckBoxPalette palette=new();
 static Bitmap? customMark;
 internal static void ApplyCheckmark(Image? image){var next=image==null?null:new Bitmap(image);var old=customMark;customMark=next;old?.Dispose();PaletteChanged?.Invoke();}
 static event Action? PaletteChanged;
 bool listening;
 internal static void ApplyPalette(CheckBoxPalette value){palette=value.Copy();palette.Validate();PaletteChanged?.Invoke();}
 internal static CheckBoxPalette Palette=>palette.Copy();
 internal const int LogicalGlyphSide=18;
 public ThemedCheckBox(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Padding=new Padding(5,3,5,3);}
 public override Size GetPreferredSize(Size proposedSize){var size=base.GetPreferredSize(proposedSize);if(Appearance==Appearance.Normal)size.Height=Math.Max(size.Height,(int)Math.Round(LogicalGlyphSide*DeviceDpi/96f)+Padding.Vertical);return size;}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);EnsureGlyphRoom();if(!listening){PaletteChanged+=RefreshPalette;listening=true;}}
 protected override void OnHandleDestroyed(EventArgs e){if(listening){PaletteChanged-=RefreshPalette;listening=false;}base.OnHandleDestroyed(e);}
 void RefreshPalette(){if(!IsDisposed)Invalidate();}
 protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);EnsureGlyphRoom();if(AutoSize)Size=GetPreferredSize(Size.Empty);Invalidate();}
 Color Get(string key)=>ColorTranslator.FromHtml(palette.Colors[key]);
 internal static Rectangle GlyphBounds(Rectangle client,Padding padding,Size size,ContentAlignment align,bool rtl){
  var area=new Rectangle(client.Left+padding.Left,client.Top+padding.Top,Math.Max(0,client.Width-padding.Horizontal),Math.Max(0,client.Height-padding.Vertical));
  bool left=(align & (ContentAlignment.TopLeft|ContentAlignment.MiddleLeft|ContentAlignment.BottomLeft))!=0;
  bool right=(align & (ContentAlignment.TopRight|ContentAlignment.MiddleRight|ContentAlignment.BottomRight))!=0;
  if(rtl)(left,right)=(right,left);
  int x=left?area.Left:right?area.Right-size.Width:area.Left+(area.Width-size.Width)/2;
  bool top=(align&(ContentAlignment.TopLeft|ContentAlignment.TopCenter|ContentAlignment.TopRight))!=0;
  bool bottom=(align&(ContentAlignment.BottomLeft|ContentAlignment.BottomCenter|ContentAlignment.BottomRight))!=0;
  int y=top?area.Top:bottom?area.Bottom-size.Height:area.Top+(area.Height-size.Height)/2;
  return new(x,y,size.Width,size.Height);
 }
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);
  if(Appearance!=Appearance.Normal)return;
  var bounds=EnlargedGlyphBounds(e.Graphics);
  DrawGlyph(e.Graphics,bounds,CheckState,Enabled,palette);
 }
 void EnsureGlyphRoom(){
  using var graphics=CreateGraphics();
  var native=CheckBoxRenderer.GetGlyphSize(graphics,CheckBoxState.UncheckedNormal);
  int side=(int)Math.Round(LogicalGlyphSide*DeviceDpi/96f),extra=Math.Max(0,side-native.Width),vertical=(extra+1)/2;
  var next=new Padding(Math.Max(Padding.Left,extra),Math.Max(Padding.Top,vertical),Math.Max(Padding.Right,extra),Math.Max(Padding.Bottom,vertical));
  if(Padding!=next)Padding=next;
 }
 internal Rectangle EnlargedGlyphBounds(Graphics graphics){
  var nativeSize=CheckBoxRenderer.GetGlyphSize(graphics,CheckBoxState.UncheckedNormal);
  var native=GlyphBounds(ClientRectangle,Padding,nativeSize,CheckAlign,RightToLeft==RightToLeft.Yes);
  int side=Math.Max(nativeSize.Width,(int)Math.Round(LogicalGlyphSide*DeviceDpi/96f));
  bool left=(CheckAlign&(ContentAlignment.TopLeft|ContentAlignment.MiddleLeft|ContentAlignment.BottomLeft))!=0;
  bool right=(CheckAlign&(ContentAlignment.TopRight|ContentAlignment.MiddleRight|ContentAlignment.BottomRight))!=0;
  if(RightToLeft==RightToLeft.Yes)(left,right)=(right,left);
  // Expand into the reserved outer padding, keeping the text-facing edge fixed.
  int x=left?native.Right-side:right?native.Left:native.Left-(side-native.Width)/2;
  return new Rectangle(x,native.Top-(side-native.Height)/2,side,side);
 }
 internal static void DrawGlyph(Graphics g,Rectangle bounds,CheckState state,bool enabled,CheckBoxPalette colors){
  Color C(string key)=>ColorTranslator.FromHtml(colors.Colors[key]);
  bool marked=state!=CheckState.Unchecked;
  var fill=C(enabled?(marked?"CheckedFill":"UncheckedFill"):"DisabledFill");
  var border=C(enabled?(marked?"CheckedBorder":"UncheckedBorder"):"DisabledBorder");
  // Stroke, vector mark and PNG scale with the enlarged DPI-aware glyph.
  float scale=bounds.Width/13f;
  using(var brush=new SolidBrush(fill))g.FillRectangle(brush,bounds);
  using(var pen=new Pen(border,Math.Max(1,scale)))g.DrawRectangle(pen,bounds.X+scale/2,bounds.Y+scale/2,bounds.Width-scale,bounds.Height-scale);
  if(!marked)return;
  if(state==CheckState.Checked&&customMark!=null){
   var image=customMark;float inset=Math.Max(1,scale);float w=bounds.Width-2*inset,h=bounds.Height-2*inset;float ratio=Math.Min(w/image.Width,h/image.Height);
   int iw=Math.Max(1,(int)(image.Width*ratio)),ih=Math.Max(1,(int)(image.Height*ratio));var destination=new Rectangle(bounds.Left+(bounds.Width-iw)/2,bounds.Top+(bounds.Height-ih)/2,iw,ih);
   using var attributes=new System.Drawing.Imaging.ImageAttributes();if(!enabled){var matrix=new System.Drawing.Imaging.ColorMatrix{Matrix33=.45f};attributes.SetColorMatrix(matrix);}
   var oldMode=g.InterpolationMode;try{g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,destination,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);}finally{g.InterpolationMode=oldMode;}return;
  }
  var saved=g.Save();
  try{
   g.SmoothingMode=SmoothingMode.AntiAlias;
   using var mark=new Pen(C(enabled?"Checkmark":"DisabledCheckmark"),Math.Max(1.5f,1.8f*scale)){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
   if(state==CheckState.Indeterminate)g.DrawLine(mark,bounds.Left+3*scale,bounds.Top+6.5f*scale,bounds.Right-3*scale,bounds.Top+6.5f*scale);
   else g.DrawLines(mark,new PointF[]{new(bounds.Left+3*scale,bounds.Top+6.5f*scale),new(bounds.Left+5.5f*scale,bounds.Top+9*scale),new(bounds.Left+10*scale,bounds.Top+3.5f*scale)});
  }finally{g.Restore(saved);}
 }
}

// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Drawing.Imaging;
using System.Windows.Forms.VisualStyles;
namespace IdleReminder;
internal static class CheckBoxVerification {
 sealed class NativeProbe:CheckBox{internal void ClickTest()=>OnClick(EventArgs.Empty);}
 sealed class ThemeProbe:ThemedCheckBox{internal void ClickTest()=>OnClick(EventArgs.Empty);}
 internal static void Run(){
  var report=new List<string>();
  void Check(bool value,string text){report.Add((value?"PASS ":"FAIL ")+text);}
  using var host=new Form{Size=new Size(700,450),ShowInTaskbar=false};
  using var native=new NativeProbe{Text="启用提醒（测试）",AutoSize=true};
  using var custom=new ThemeProbe{Text=native.Text,AutoSize=true};
  host.Controls.Add(native);host.Controls.Add(custom);
  _=host.Handle;_=native.Handle;_=custom.Handle;
  native.Padding=custom.Padding;
  var nativePreferred=native.GetPreferredSize(Size.Empty);var customPreferred=custom.GetPreferredSize(Size.Empty);
  Check(customPreferred.Width==nativePreferred.Width&&customPreferred.Height>=nativePreferred.Height,"native text width retained with additional glyph height");
  native.AutoSize=custom.AutoSize=false;
  int nc=0,cc=0;native.CheckedChanged+=(_,_)=>nc++;custom.CheckedChanged+=(_,_)=>cc++;
  native.ClickTest();custom.ClickTest();
  Check(native.Checked&&custom.Checked&&nc==cc,"native click toggle and checked event preserved");
  native.AutoCheck=false;custom.AutoCheck=false;native.ClickTest();custom.ClickTest();
  Check(native.Checked==custom.Checked&&nc==cc,"AutoCheck false preserved");
  Check(native.TabStop==custom.TabStop&&native.AccessibleRole==custom.AccessibleRole,"native tabstop and accessible role preserved");
  native.AutoCheck=true;custom.AutoCheck=true;
  foreach(var state in new[]{CheckState.Unchecked,CheckState.Checked,CheckState.Indeterminate}){
   native.ThreeState=custom.ThreeState=true;native.CheckState=custom.CheckState=state;
   native.Size=custom.Size=custom.GetPreferredSize(Size.Empty);
   using var a=new Bitmap(native.Width,native.Height);using var b=new Bitmap(custom.Width,custom.Height);
   native.DrawToBitmap(a,new Rectangle(Point.Empty,a.Size));custom.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));
   using var graphics=Graphics.FromImage(b);
   var glyph=custom.EnlargedGlyphBounds(graphics);
   Check(custom.ClientRectangle.Contains(glyph)&&glyph.Width>CheckBoxRenderer.GetGlyphSize(graphics,CheckBoxState.UncheckedNormal).Width,"larger glyph fits control "+state);
   bool textUnchanged=true;
   for(int y=0;y<a.Height;y++)for(int x=Math.Min(a.Width,glyph.Right+2);x<a.Width;x++)
    if(a.GetPixel(x,y)!=b.GetPixel(x,y))textUnchanged=false;
   Check(textUnchanged,"native text pixels preserved "+state);
  }
  var colors=ThemedCheckBox.Palette;
  using var sheet=new Bitmap(760,480);
  using(var g=Graphics.FromImage(sheet)){
   g.Clear(Color.White);
   using var font=new Font("Segoe UI",10);
   int row=0;
   foreach(int dpi in new[]{96,120,144,168,192,240,288}){
    int side=(int)Math.Round(ThemedCheckBox.LogicalGlyphSide*dpi/96f);int y=8+row*64;
    g.DrawString($"{dpi/96.0:P0} ({dpi} DPI)",font,Brushes.Black,5,y+8);
    foreach(var (state,active,index) in new[]{(CheckState.Unchecked,true,0),(CheckState.Checked,true,1),(CheckState.Indeterminate,true,2),(CheckState.Checked,false,3)}){
     var rect=new Rectangle(190+index*125,y,side,side);
     ThemedCheckBox.DrawGlyph(g,rect,state,active,colors);
     Check(rect.Width==side&&rect.Height==side,"DPI glyph render "+dpi+" "+state+" "+active);
    }
    row++;
   }
  }
  sheet.Save(Path.Combine(Store.Root,"checkbox-dpi.png"),ImageFormat.Png);
  File.WriteAllLines(Path.Combine(Store.Root,"checkbox-verification.txt"),report);
 }
}

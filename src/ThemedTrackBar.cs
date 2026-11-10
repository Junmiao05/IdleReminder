// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
// Native trackbar remains responsible for values, hit-testing and keyboard input.
internal sealed class ThemedTrackBar:TrackBar {
 internal Color TrackColor=SystemColors.ControlLight,FillColor=SystemColors.Highlight,ThumbColor=SystemColors.Highlight,TicksColor=SystemColors.GrayText,DisabledColor=SystemColors.ControlDark;
 internal int ChannelDraws,ThumbDraws;
 [StructLayout(LayoutKind.Sequential)]struct Header{public IntPtr Hwnd;public UIntPtr Id;public int Code;}
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;public readonly Rectangle Bounds=>Rectangle.FromLTRB(Left,Top,Right,Bottom);}
 [StructLayout(LayoutKind.Sequential)]struct CustomDraw{public Header Header;public uint Stage;public IntPtr Dc;public Rect Rect;public UIntPtr Item;public uint State;public IntPtr Param;}
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,ref Rect rect);
 internal void SetColors(IReadOnlyDictionary<string,string> colors){
  Color C(string key)=>ColorTranslator.FromHtml(colors[key]);
  TrackColor=C("SliderTrack");FillColor=C("SliderFill");ThumbColor=C("SliderThumb");TicksColor=C("SliderTicks");DisabledColor=C("SliderDisabled");Invalidate();
 }
 protected override void WndProc(ref Message m){
  if(m.Msg==0x204E&&m.LParam!=IntPtr.Zero){
   var draw=Marshal.PtrToStructure<CustomDraw>(m.LParam);
   if(draw.Header.Code==-12){
    if(draw.Stage==1){m.Result=(IntPtr)0x20;return;}
    if(draw.Stage==0x10001){
     using var g=Graphics.FromHdc(draw.Dc);
     int item=(int)draw.Item.ToUInt64();
     if(item==3){ // TBCD_CHANNEL
      ChannelDraws++;var r=draw.Rect.Bounds;
      using var track=new SolidBrush(Enabled?TrackColor:DisabledColor);g.FillRectangle(track,r);
      var thumb=new Rect();SendMessage(Handle,0x400+25,IntPtr.Zero,ref thumb);
      using var filled=new SolidBrush(Enabled?FillColor:DisabledColor);
      if(Orientation==Orientation.Horizontal){
       int x=(thumb.Left+thumb.Right)/2;bool reverse=RightToLeft==RightToLeft.Yes;
       g.FillRectangle(filled,reverse?x:r.Left,r.Top,Math.Max(0,reverse?r.Right-x:x-r.Left),r.Height);
      }else{int y=(thumb.Top+thumb.Bottom)/2;g.FillRectangle(filled,r.Left,y,r.Width,Math.Max(0,r.Bottom-y));}
      m.Result=(IntPtr)4;return;
     }
     if(item==2){ // TBCD_THUMB
      ThumbDraws++;using var brush=new SolidBrush(Enabled?ThumbColor:DisabledColor);
      var thumbBounds=draw.Rect.Bounds;
      if(Orientation==Orientation.Horizontal&&TickStyle==TickStyle.BottomRight){
       int tip=Math.Max(2,(int)Math.Round(4*DeviceDpi/96f));
       g.FillPolygon(brush,new Point[]{new(thumbBounds.Left,thumbBounds.Top),new(thumbBounds.Right,thumbBounds.Top),new(thumbBounds.Right,thumbBounds.Bottom-tip),new(thumbBounds.Left+thumbBounds.Width/2,thumbBounds.Bottom),new(thumbBounds.Left,thumbBounds.Bottom-tip)});
      }else g.FillRectangle(brush,thumbBounds);
      if(Focused)ControlPaint.DrawFocusRectangle(g,draw.Rect.Bounds);
      m.Result=(IntPtr)4;return;
     }
     if(item==1){ // TBCD_TICS
      if(TickStyle!=TickStyle.None){
       var channel=new Rect();SendMessage(Handle,0x400+26,IntPtr.Zero,ref channel);
       float dpi=DeviceDpi/96f;
       using var pen=new Pen(Enabled?TicksColor:DisabledColor,Math.Max(1,dpi));
       int frequency=Math.Max(1,TickFrequency),range=Math.Max(1,Maximum-Minimum);
       for(long value=Minimum;value<=Maximum;value+=frequency){
        float t=(value-Minimum)/(float)range;
        if(Orientation==Orientation.Horizontal){
         float x=channel.Left+t*(channel.Right-channel.Left);
         if(TickStyle is TickStyle.BottomRight or TickStyle.Both)g.DrawLine(pen,x,channel.Bottom+5*dpi,x,channel.Bottom+7*dpi);
         if(TickStyle is TickStyle.TopLeft or TickStyle.Both)g.DrawLine(pen,x,channel.Top-7*dpi,x,channel.Top-5*dpi);
        }else{
         float y=channel.Top+t*(channel.Bottom-channel.Top);
         if(TickStyle is TickStyle.BottomRight or TickStyle.Both)g.DrawLine(pen,channel.Right+5*dpi,y,channel.Right+7*dpi,y);
         if(TickStyle is TickStyle.TopLeft or TickStyle.Both)g.DrawLine(pen,channel.Left-7*dpi,y,channel.Left-5*dpi,y);
        }
       }
      }m.Result=(IntPtr)4;return;
     }
    }
   }
  }
  base.WndProc(ref m);
 }
}


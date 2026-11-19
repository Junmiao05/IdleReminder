// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Drawing.Drawing2D;

namespace IdleReminder;

internal sealed class DetailsToggle : LinkLabel {
 bool expanded;
 [System.ComponentModel.Browsable(false)]
 [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
 internal bool Expanded {
  get=>expanded;
  set {expanded=value;Invalidate();}
 }

 internal DetailsToggle() {
  AutoSize=true;
  TabStop=true;
  LinkBehavior=LinkBehavior.HoverUnderline;
  Tag="DetailsLink";
  Font=new Font(SystemFonts.MessageBoxFont!,FontStyle.Bold);
  Padding=new Padding(18,0,0,0);
  LinkColor=ActiveLinkColor=VisitedLinkColor=SystemColors.ControlText;
 }

 protected override void OnPaint(PaintEventArgs e) {
  base.OnPaint(e);
  float scale=DeviceDpi/96f;
  float centerX=Padding.Left/2f;
  float centerY=ClientSize.Height/2f;
  var points=new[]{new PointF(-2,-4),new PointF(2,0),new PointF(-2,4)};
  for(int i=0;i<points.Length;i++) {
   var point=points[i];
   if(expanded)point=new PointF(-point.Y,point.X);
   points[i]=new PointF(centerX+point.X*scale,centerY+point.Y*scale);
  }
  var previous=e.Graphics.SmoothingMode;
  e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  using var pen=new Pen(Enabled?LinkColor:SystemColors.GrayText,1.5f*scale) {
   StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round
  };
  e.Graphics.DrawLines(pen,points);
  e.Graphics.SmoothingMode=previous;
 }

 protected override void OnMouseUp(MouseEventArgs e) {
  base.OnMouseUp(e);
  if(Enabled&&e.Button==MouseButtons.Left&&e.X>=0&&e.X<Padding.Left&&ClientRectangle.Contains(e.Location))
   OnLinkClicked(new LinkLabelLinkClickedEventArgs(Links[0]));
 }
}

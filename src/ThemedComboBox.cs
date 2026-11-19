// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Globalization;
using System.Runtime.InteropServices;

namespace IdleReminder;
internal class ThemedComboBox:ComboBox {
 internal Color SelectedBackground=SystemColors.Highlight,SelectedForeground=SystemColors.HighlightText;
 readonly ToolTip fullNameTip=new(){InitialDelay=400,ReshowDelay=100,AutoPopDelay=30000,ShowAlways=true};
 readonly ToolTip listTip=new(){ShowAlways=true,AutoPopDelay=30000};
 readonly System.Windows.Forms.Timer hoverTimer=new(){Interval=450};
 readonly ContextMenuStrip nameMenu=new();
 readonly ListObserver observer;
 bool queued,refreshing,tipResourcesDisposed;
 int hovered=-1;
 [StructLayout(LayoutKind.Sequential)]struct ComboInfo {public int Size;public Rectangle Item,Button;public int State;public nint Combo,Edit,List;}
 [StructLayout(LayoutKind.Sequential)]struct NativeRect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct MouseTrack {public int Size,Flags;public nint Window;public int Time;}
 [DllImport("user32.dll")]static extern bool GetComboBoxInfo(nint window,ref ComboInfo info);
 [DllImport("user32.dll")]static extern bool GetClientRect(nint window,out NativeRect rect);
 [DllImport("user32.dll")]static extern bool GetWindowRect(nint window,out NativeRect rect);
 [DllImport("user32.dll")]static extern bool TrackMouseEvent(ref MouseTrack track);
 [DllImport("user32.dll")]static extern nint SendMessage(nint window,int message,nint wParam,nint lParam);

 public ThemedComboBox(){
  DrawMode=DrawMode.OwnerDrawFixed;
  observer=new ListObserver(this);hoverTimer.Tick+=(_,_)=>ShowHoveredName();
  var view=new ToolStripMenuItem(UiText.Get("ComboBox_ViewName"));
  view.Click+=(_,_)=>ShowFullName();nameMenu.Items.Add(view);
  nameMenu.Opening+=(_,_)=>view.Enabled=SelectedName().Length>0;ContextMenuStrip=nameMenu;
 }
 int Scale(int value)=>(int)Math.Round(value*DeviceDpi/96f);
 string SelectedName()=>SelectedItem==null?Text:GetItemText(SelectedItem)??"";
 internal int MeasureName(string text)=>TextRenderer.MeasureText(text,Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix).Width;
 internal void RefreshSizing(){
  if(refreshing||IsDisposed||Disposing)return;
  refreshing=true;
  try{
   // Keep the popup aligned with the authored control width, regardless of item text.
   DropDownWidth=Width;
   RefreshTip();
  }finally{refreshing=false;}
 }
 void QueueSizing(){
  if(!IsHandleCreated||IsDisposed||Disposing||queued||refreshing)return;
  queued=true;
  try{BeginInvoke((Action)(()=>{queued=false;if(!IsDisposed)RefreshSizing();}));}catch(InvalidOperationException){queued=false;}
 }
 void RefreshTip(){
  string name=SelectedName();
  fullNameTip?.SetToolTip(this,MeasureName(name)>Width-Scale(28)?name+Environment.NewLine+UiText.Get("ComboBox_NameHint"):null);
 }
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);QueueSizing();}
 protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);ItemHeight=Font.Height+4;QueueSizing();}
 protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);ItemHeight=Font.Height+4;QueueSizing();Invalidate();}
 protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);QueueSizing();}
 protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);QueueSizing();}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);QueueSizing();}
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);RefreshTip();}
 protected override void OnDropDown(EventArgs e){
  RefreshSizing();fullNameTip.Active=false;
  var info=new ComboInfo{Size=Marshal.SizeOf<ComboInfo>()};
  if(GetComboBoxInfo(Handle,ref info)&&info.List!=0)observer.Attach(info.List);
  base.OnDropDown(e);
 }
 protected override void OnDropDownClosed(EventArgs e){StopHover();observer.Detach();fullNameTip.Active=true;base.OnDropDownClosed(e);}
 protected override void OnHandleDestroyed(EventArgs e){if(!Disposing&&!IsDisposed){StopHover();observer.Detach();fullNameTip.Active=true;}base.OnHandleDestroyed(e);}
 protected override void WndProc(ref Message message){
  int kind=message.Msg;
  if(kind is 0x020A or 0x0115)StopHover();
  base.WndProc(ref message);
  if(kind is 0x0143 or 0x014A or 0x0144 or 0x014B)QueueSizing();
 }

 int PopupTextWidth(){
  if(observer.Handle!=0&&GetClientRect(observer.Handle,out var rect))return Math.Max(1,rect.Right-rect.Left-4);
  return Math.Max(1,DropDownWidth-Scale(28));
 }
 internal string ListTooltipText(int index){
  if(index<0||index>=Items.Count)return "";
  string name=GetItemText(Items[index])??"";
  return MeasureName(name)>PopupTextWidth()?name:"";
 }
 void ListMouseMoved(nint coordinates){
  if(!DroppedDown||observer.Handle==0)return;
  var tracking=new MouseTrack{Size=Marshal.SizeOf<MouseTrack>(),Flags=2,Window=observer.Handle};TrackMouseEvent(ref tracking);
  long hit=(long)SendMessage(observer.Handle,0x01A9,0,coordinates); // LB_ITEMFROMPOINT: no selection changes.
  int index=(hit>>16)!=0?-1:(int)(hit&0xffff);
  if(index==hovered)return;
  StopHover();
  if(ListTooltipText(index).Length>0){hovered=index;hoverTimer.Start();}
 }
 void StopHover(){hoverTimer.Stop();hovered=-1;listTip.Hide(this);}
 void ShowHoveredName(){
  hoverTimer.Stop();
  if(!DroppedDown||observer.Handle==0)return;
  string name=ListTooltipText(hovered);if(name.Length==0)return;
  if(!GetWindowRect(observer.Handle,out var rect))return;
  var work=Screen.FromControl(this).WorkingArea;
  var screenPoint=rect.Right+Scale(350)<work.Right?new Point(rect.Right+Scale(8),rect.Top):new Point(rect.Left+Scale(8),rect.Bottom+Scale(8));
  listTip.Show(WrapTip(name),this,PointToClient(screenPoint),30000);
 }
 string WrapTip(string text){
  int width=Math.Min(Scale(560),Math.Max(100,Screen.FromControl(this).WorkingArea.Width-Scale(32)));
  var output=new List<string>();
  foreach(var paragraph in text.Replace("\r\n","\n").Split('\n')){
   var positions=StringInfo.ParseCombiningCharacters(paragraph).Append(paragraph.Length).ToArray();int start=0;
   if(positions.Length==1){output.Add("");continue;}
   while(start<positions.Length-1){
    int low=start+1,high=positions.Length-1,best=low;
    while(low<=high){int middle=(low+high)/2;int measured=TextRenderer.MeasureText(paragraph[positions[start]..positions[middle]],SystemFonts.MessageBoxFont,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix).Width;if(measured<=width){best=middle;low=middle+1;}else high=middle-1;}
    string piece=paragraph[positions[start]..positions[best]];int space=piece.LastIndexOf(' ');
    if(best<positions.Length-1&&space>piece.Length/2){int boundary=Array.BinarySearch(positions,positions[start]+space+1);if(boundary>start){best=boundary;piece=paragraph[positions[start]..positions[best]];}}
    output.Add(piece);start=best;
   }
  }
  return string.Join(Environment.NewLine,output);
 }
 sealed class ListObserver(ThemedComboBox owner):NativeWindow {
  internal void Attach(nint handle){if(Handle==handle)return;Detach();AssignHandle(handle);}
  internal void Detach(){if(Handle!=0)ReleaseHandle();}
  protected override void WndProc(ref Message message){
   int kind=message.Msg;var point=message.LParam;base.WndProc(ref message);
   if(owner.IsDisposed||owner.Disposing)return;
   if(kind==0x0200)owner.ListMouseMoved(point);
   else if(kind is 0x02A3 or 0x0201 or 0x020A or 0x0115 or 0x0082)owner.StopHover();
  }
 }

 protected override void OnKeyDown(KeyEventArgs e){
  if(DroppedDown)StopHover();
  if(e.KeyCode==Keys.F2&&SelectedName().Length>0){e.Handled=e.SuppressKeyPress=true;DroppedDown=false;ShowFullName();return;}
  base.OnKeyDown(e);
 }
 internal Form CreateNameDialog(){
  var area=Screen.FromControl(this).WorkingArea;
  var dialog=new Form{Text=UiText.Get("ComboBox_NameTitle"),Size=new Size(Math.Min(700,area.Width-40),Math.Min(320,area.Height-40)),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false,ShowIcon=false,Font=Font,KeyPreview=true};
  dialog.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape){e.Handled=e.SuppressKeyPress=true;dialog.Close();}};
  var text=new TextBox{Text=SelectedName(),ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill};
  dialog.Controls.Add(text);dialog.Shown+=(_,_)=>{text.Select(0,0);text.Focus();};return dialog;
 }
 void ShowFullName(){using var dialog=CreateNameDialog();dialog.ShowDialog(FindForm());}
 internal void SetColors(IReadOnlyDictionary<string,string> colors){SelectedBackground=ColorTranslator.FromHtml(colors["SelectionBackground"]);SelectedForeground=ColorTranslator.FromHtml(colors["SelectionText"]);Invalidate();}
 protected override void OnDrawItem(DrawItemEventArgs e){
  bool selected=(e.State&DrawItemState.Selected)!=0;
  using var background=new SolidBrush(selected?SelectedBackground:BackColor);e.Graphics.FillRectangle(background,e.Bounds);
  string text=e.Index>=0&&e.Index<Items.Count?GetItemText(Items[e.Index])??"":Text;
  TextRenderer.DrawText(e.Graphics,text,e.Font??Font,Rectangle.Inflate(e.Bounds,-2,0),Enabled?(selected?SelectedForeground:ForeColor):SystemColors.GrayText,TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
  if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();base.OnDrawItem(e);
 }
 protected override void Dispose(bool disposing){
  if(disposing&&!tipResourcesDisposed){StopHover();observer.Detach();hoverTimer.Dispose();listTip.Dispose();fullNameTip.Dispose();nameMenu.Dispose();tipResourcesDisposed=true;}
  base.Dispose(disposing);
 }
}


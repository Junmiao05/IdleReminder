// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SteamImportForm:Form {
 readonly CheckedListBox list=new(){Dock=DockStyle.Fill,CheckOnClick=true,HorizontalScrollbar=true,IntegralHeight=false};
 readonly Label status=new(){AutoSize=true,MaximumSize=new Size(720,0)};
 readonly Button accept=new(){Text=UiText.Get("SteamImportForm_001"),AutoSize=true,Enabled=false};
 readonly Button selectAll=new(){Text=UiText.Get("SteamImportForm_002"),AutoSize=true,Enabled=false};
 readonly Button selectNone=new(){Text=UiText.Get("SteamImportForm_003"),AutoSize=true,Enabled=false};
 readonly CancellationTokenSource cancel=new();
 readonly string folder;bool scanning=true;
 internal string[] SelectedApplications {get;private set;}=[];
 internal SteamImportForm(string folder){
  this.folder=folder;Text=UiText.Get("SteamImportForm_004");Size=new Size(820,570);MinimumSize=new Size(600,400);StartPosition=FormStartPosition.CenterParent;
  var heading=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown,WrapContents=false};
  heading.Controls.Add(new Label{Text=UiText.Get("SteamImportForm_005"),AutoSize=true,MaximumSize=new Size(720,0)});
  status.Text=UiText.Get("SteamImportForm_006");heading.Controls.Add(status);
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(12)};
  accept.Click+=(_,_)=>{SelectedApplications=list.CheckedItems.Cast<SteamImportCandidate>().Select(x=>x.Executable).ToArray();DialogResult=DialogResult.OK;Close();};
  selectAll.Click+=(_,_)=>SetAllChecked(true);selectNone.Click+=(_,_)=>SetAllChecked(false);
  var close=new Button{Text=UiText.Get("SteamImportForm_007"),AutoSize=true};close.Click+=(_,_)=>Close();buttons.Controls.Add(accept);buttons.Controls.Add(selectAll);buttons.Controls.Add(selectNone);buttons.Controls.Add(close);
  Controls.Add(list);Controls.Add(heading);Controls.Add(buttons);CancelButton=close;
  Shown+=async(_,_)=>await ScanAsync();FormClosing+=(_,e)=>{if(scanning){e.Cancel=true;cancel.Cancel();status.Text=UiText.Get("SteamImportForm_008");}};
  UiStyle.Apply(this);
 }
 void SetAllChecked(bool value){
  list.BeginUpdate();try{for(int i=0;i<list.Items.Count;i++)list.SetItemChecked(i,value);}finally{list.EndUpdate();}
 }
 async Task ScanAsync(){
  try{
   var result=await Task.Run(()=>SteamApplicationImport.Scan(folder,cancel.Token),cancel.Token);
   if(cancel.IsCancellationRequested)return;
   list.BeginUpdate();try{foreach(var candidate in result.Candidates)list.Items.Add(candidate,false);}finally{list.EndUpdate();}
   status.Text=UiText.Format("SteamImportForm_010",result.Candidates.Count,result.SkippedDirectories)+(result.Limited?UiText.Get("SteamImportForm_011"):"");accept.Enabled=selectAll.Enabled=selectNone.Enabled=result.Candidates.Count>0;
  }catch(OperationCanceledException){}
  catch(Exception ex){Store.Log("steam-import-scan-failed",ex);status.Text=UiText.Get("SteamImportForm_009");}
  finally{scanning=false;if(cancel.IsCancellationRequested)Close();cancel.Dispose();}
 }
}

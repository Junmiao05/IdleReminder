// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SteamImportForm:Form {
 readonly CheckedListBox list=new(){Dock=DockStyle.Fill,CheckOnClick=true,HorizontalScrollbar=true,IntegralHeight=false};
 readonly Label status=new(){AutoSize=true,MaximumSize=new Size(720,0)};
 readonly Button accept=new(){Text="导入勾选项",AutoSize=true,Enabled=false};
 readonly Button selectAll=new(){Text="全选",AutoSize=true,Enabled=false};
 readonly Button selectNone=new(){Text="全不选",AutoSize=true,Enabled=false};
 readonly CancellationTokenSource cancel=new();
 readonly string folder;bool scanning=true;
 internal string[] SelectedApplications {get;private set;}=[];
 internal SteamImportForm(string folder){
  this.folder=folder;Text="从Steam目录批量导入";Size=new Size(820,570);MinimumSize=new Size(600,400);StartPosition=FormStartPosition.CenterParent;
  var heading=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown,WrapContents=false};
  heading.Controls.Add(new Label{Text="仅导入需要提示切换计时方式的游戏EXE，不修改Steam或游戏文件。\n同一游戏可能包含多个EXE，请优先选择实际游戏主程序；启动器、反作弊、崩溃报告和卸载程序通常不需要。\n默认均不勾选；全选会包含辅助程序，请确认后导入。其他Steam库需分别选择。",AutoSize=true,MaximumSize=new Size(720,0)});
  status.Text="正在扫描，可随时取消…";heading.Controls.Add(status);
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(12)};
  accept.Click+=(_,_)=>{SelectedApplications=list.CheckedItems.Cast<SteamImportCandidate>().Select(x=>x.Executable).ToArray();DialogResult=DialogResult.OK;Close();};
  selectAll.Click+=(_,_)=>SetAllChecked(true);selectNone.Click+=(_,_)=>SetAllChecked(false);
  var close=new Button{Text="取消",AutoSize=true};close.Click+=(_,_)=>Close();buttons.Controls.Add(accept);buttons.Controls.Add(selectAll);buttons.Controls.Add(selectNone);buttons.Controls.Add(close);
  Controls.Add(list);Controls.Add(heading);Controls.Add(buttons);CancelButton=close;
  Shown+=async(_,_)=>await ScanAsync();FormClosing+=(_,e)=>{if(scanning){e.Cancel=true;cancel.Cancel();status.Text="正在取消扫描…";}};
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
   status.Text=$"找到 {result.Candidates.Count} 个候选EXE；跳过 {result.SkippedDirectories} 个链接或不可访问目录。"+(result.Limited?"扫描达到数量/深度/时间限制，结果不完整；可手动浏览补充。":"");accept.Enabled=selectAll.Enabled=selectNone.Enabled=result.Candidates.Count>0;
  }catch(OperationCanceledException){}
  catch(Exception ex){Store.Log("steam-import-scan-failed",ex);status.Text="扫描失败，请核对Steam库目录与访问权限。";}
  finally{scanning=false;if(cancel.IsCancellationRequested)Close();cancel.Dispose();}
 }
}

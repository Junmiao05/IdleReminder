// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class SuppressionPanel:FlowLayoutPanel {
 readonly DataGridView grid=new(){Width=475,Height=160,AutoGenerateColumns=false,AllowUserToAddRows=false,ReadOnly=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false};
 readonly TextBox app=new(){Width=475};
 readonly ThemedComboBox behavior=new(){Width=300,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly ThemedComboBox mediaApps=new(){Width=475,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly List<SuppressionRule> draft;
 internal SuppressionPanel(Settings s){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  draft=s.SuppressionRules.Select(r=>new SuppressionRule{Application=r.Application,Mode=r.Mode}).ToList();
  Controls.Add(new Label{Text=UiText.Get("SuppressionPanel_001"),AutoSize=true,Font=new Font(SystemFonts.MessageBoxFont!,FontStyle.Bold)});
  Controls.Add(new Label{Text=UiText.Get("SuppressionPanel_002"),AutoSize=true,MaximumSize=new Size(475,0)});
  grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=UiText.Get("SuppressionPanel_003"),Width=285});
  grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=UiText.Get("SuppressionPanel_004"),Width=165});
  Controls.Add(grid);Controls.Add(app);
  behavior.Items.AddRange(new object[]{UiText.Get("SuppressionPanel_012"),UiText.Get("SuppressionPanel_013")});behavior.SelectedIndex=0;Controls.Add(behavior);
  var browse=AddButton(UiText.Get("SuppressionPanel_005"),()=>{
   using var dialog=new OpenFileDialog{Filter=UiText.Get("SuppressionPanel_014")+"|*.exe",CheckFileExists=true,RestoreDirectory=true};
   if(dialog.ShowDialog(FindForm())==DialogResult.OK)app.Text=behavior.SelectedIndex==1?dialog.FileName:Path.GetFileName(dialog.FileName);
  });
  browse.Enabled=behavior.SelectedIndex==1;
  behavior.SelectedIndexChanged+=(_,_)=>browse.Enabled=behavior.SelectedIndex==1;
  Controls.Add(mediaApps);
  var refresh=new ThemedButton{Text=UiText.Get("SuppressionPanel_006"),AutoSize=true};
  refresh.Click+=async(_,_)=>{
   refresh.Enabled=false;
   try{var ids=await MediaPause.ListApplicationsAsync();if(IsDisposed)return;mediaApps.Items.Clear();mediaApps.Items.AddRange(ids);if(ids.Length>0){mediaApps.SelectedIndex=0;app.Text=ids[0];}else MessageBox.Show(UiText.Get("SuppressionPanel_007"));}
   catch(Exception ex){Store.Log("suppression-app-list-failed",ex);if(!IsDisposed)MessageBox.Show(UiText.Get("SuppressionPanel_008"));}
   finally{if(!refresh.IsDisposed)refresh.Enabled=true;}
  };
  mediaApps.SelectedIndexChanged+=(_,_)=>{if(mediaApps.SelectedItem is string id)app.Text=id;};
  Controls.Add(refresh);
  AddButton(UiText.Get("SuppressionPanel_009"),()=>{
   var rule=new SuppressionRule{Application=app.Text.Trim(),Mode=(SuppressionMode)behavior.SelectedIndex};SuppressionPolicy.Validate(rule);
   draft.RemoveAll(r=>r.Application.Equals(rule.Application,StringComparison.OrdinalIgnoreCase));draft.Add(rule);Render();
  });
  AddButton(UiText.Get("SuppressionPanel_010"),()=>{if(grid.CurrentRow is {} row){draft.RemoveAt(row.Index);Render();}});
  Controls.Add(new Label{Text=UiText.Get("SuppressionPanel_011"),AutoSize=true,MaximumSize=new Size(475,0)});
  DetailsSection.Add(this,"SuppressionRuleDetails",475,("SuppressionPanel_011Details",false));
  Controls.Add(new Label{Text=UiText.Get("SuppressionPanel_011Tail"),AutoSize=true,MaximumSize=new Size(475,0)});
  Render();
 }
 Button AddButton(string text,Action action){var button=new ThemedButton{Text=text,AutoSize=true};button.Click+=(_,_)=>{try{action();}catch(ArgumentException e){MessageBox.Show(e.Message);}};Controls.Add(button);return button;}
 void Render(){grid.Rows.Clear();foreach(var r in draft)grid.Rows.Add(r.Application,r.Mode==SuppressionMode.WhilePlaying?UiText.Get("SuppressionPanel_012"):UiText.Get("SuppressionPanel_013"));}
 internal void Apply(Settings settings){foreach(var r in draft)SuppressionPolicy.Validate(r);settings.SuppressionRules=draft.Select(r=>new SuppressionRule{Application=r.Application,Mode=r.Mode}).ToList();}
}





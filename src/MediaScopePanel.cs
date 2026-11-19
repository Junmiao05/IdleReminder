// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class MediaScopePanel:FlowLayoutPanel {
 readonly ThemedComboBox scope=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=475};
 readonly ThemedCheckBox duck=new(){Text=UiText.Get("MediaScopePanel_001"),AutoSize=true};
 readonly ThemedCheckBox coordinated=new(){Text=UiText.Get("MediaScopePanel_002"),AutoSize=true};
 readonly NumericUpDown remaining=new(){Minimum=1,Maximum=100,Value=20,Width=100};
 readonly TextBox apps=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Width=475,Height=110};
 readonly TextBox duckApps=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Width=475,Height=85};
 readonly List<Control> pauseControls=new();
 readonly ThemedComboBox current=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=475};
 internal MediaScopePanel(Settings settings){
  FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;Width=490;
  scope.Items.AddRange(new object[]{UiText.Get("MediaScopePanel_017"),UiText.Get("MediaScopePanel_018"),UiText.Get("MediaScopePanel_019")});
  scope.SelectedIndex=(int)settings.MediaScope;
  duck.Checked=settings.DuckMediaOnReminder;coordinated.Checked=settings.AllowBoostWithOtherAudioAfterDucking;remaining.Value=settings.DuckRemainingPercent;
  // Subscribe after loading: reopening saved settings must respect manual opt-out.
  duck.CheckedChanged+=(_,_)=>{if(duck.Checked)coordinated.Checked=true;};
  apps.Text=string.Join(Environment.NewLine,settings.MediaApplications);
  duckApps.Text=string.Join(Environment.NewLine,settings.DuckApplications??settings.MediaApplications.Where(x=>x.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)));
  Controls.Add(new Label{Text=UiText.Get("MediaScopePanel_003"),AutoSize=true});
  Controls.Add(scope);Controls.Add(new Label{Text=UiText.Get("MediaScopePanel_004"),AutoSize=true});Controls.Add(apps);
  var browse=new ThemedButton{Text=UiText.Get("SuppressionPanel_005"),AutoSize=true};
  browse.Click+=(_,_)=>{
   using var dialog=new OpenFileDialog{
    Title=UiText.Get("MediaScopePanel_005"),
    Filter=UiText.Get("MediaScopePanel_020")+"|*.exe",
    CheckFileExists=true,CheckPathExists=true,Multiselect=true,RestoreDirectory=true
   };
   if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
   try{
    var list=MediaApplicationFilter.Parse(apps.Text);
    foreach(var path in dialog.FileNames)
     foreach(var id in MediaApplicationFilter.ExecutableEntries(path))
      if(!list.Contains(id,StringComparer.OrdinalIgnoreCase))list.Add(id);
    apps.Text=string.Join(Environment.NewLine,list);
   }catch(Exception e) when(e is ArgumentException or System.IO.IOException or UnauthorizedAccessException){
    Store.Log("browse-media-app-failed",e);MessageBox.Show(UiText.Get("MediaScopePanel_006")+e.Message);
   }
  };
  Controls.Add(browse);
  Controls.Add(new Label{
   Text=UiText.Get("MediaScopePanel_007"),
   AutoSize=true,MaximumSize=new Size(475,0),ForeColor=SystemColors.GrayText
  });
  Controls.Add(current);
  var refresh=new ThemedButton{Text=UiText.Get("SuppressionPanel_006"),AutoSize=true};
  refresh.Click+=async(_,_)=>{
   refresh.Enabled=false;
   try{var list=await MediaPause.ListApplicationsAsync();if(IsDisposed)return;current.Items.Clear();current.Items.AddRange(list);if(list.Length>0)current.SelectedIndex=0;else MessageBox.Show(UiText.Get("MediaScopePanel_008"));}
   catch(Exception e){Store.Log("media-application-list-failed",e);if(!IsDisposed)MessageBox.Show(UiText.Get("MediaScopePanel_009"));}
   finally{if(!refresh.IsDisposed)refresh.Enabled=true;}
  };
  Controls.Add(refresh);
  var add=new ThemedButton{Text=UiText.Get("MediaScopePanel_010"),AutoSize=true};
  add.Click+=(_,_)=>{try{if(current.SelectedItem is string id){var list=MediaApplicationFilter.Parse(apps.Text);if(!list.Contains(id,StringComparer.OrdinalIgnoreCase))list.Add(id);apps.Text=string.Join(Environment.NewLine,list);}}catch(ArgumentException e){MessageBox.Show(e.Message);}};
  Controls.Add(add);
    pauseControls.AddRange(Controls.Cast<Control>());
  Controls.Add(duck);
  Controls.Add(new Label{Text=UiText.Get("MediaScopePanel_011"),AutoSize=true,MaximumSize=new Size(475,0)});
  Controls.Add(duckApps);
  var browseDuck=new ThemedButton{Text=UiText.Get("MediaScopePanel_012"),AutoSize=true};
  browseDuck.Click+=(_,_)=>{
   using var dialog=new OpenFileDialog{Title=UiText.Get("MediaScopePanel_013"),Filter=UiText.Get("MediaScopePanel_020")+"|*.exe",Multiselect=true,CheckFileExists=true,RestoreDirectory=true};
   if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
   try{
    var entries=ParseDuckApps(duckApps.Text);
    foreach(var path in dialog.FileNames)foreach(var entry in MediaApplicationFilter.ExecutableEntries(path))if(!entries.Contains(entry,StringComparer.OrdinalIgnoreCase))entries.Add(entry);
    duckApps.Text=string.Join(Environment.NewLine,entries);
   }catch(ArgumentException e){MessageBox.Show(e.Message);}
  };
  Controls.Add(browseDuck);
  Controls.Add(new Label{Text=UiText.Get("MediaScopePanel_014"),AutoSize=true,MaximumSize=new Size(475,0)});Controls.Add(remaining);Controls.Add(coordinated);
  DetailsSection.Add(this,"MediaVolumeDetails",475,("MediaScopePanel_015",true),("MediaScopePanel_016Details",false));
  Controls.Add(new Label{Text=UiText.Get("MediaScopePanel_016"),AutoSize=true,MaximumSize=new Size(475,0)});
 }
 internal void SetPauseEnabled(bool enabled){foreach(var control in pauseControls)control.Enabled=enabled;}
 static List<string> ParseDuckApps(string text){var entries=MediaApplicationFilter.Parse(text);if(entries.Any(x=>!x.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)))throw new ArgumentException(UiText.Get("MediaScopePanel_021"));return entries;}
 internal void Apply(Settings settings){var list=MediaApplicationFilter.Parse(apps.Text);var reductions=ParseDuckApps(duckApps.Text);settings.DuckApplications=reductions;settings.MediaScope=(MediaPauseScope)scope.SelectedIndex;settings.MediaApplications=list;settings.AllowBoostWithOtherAudioAfterDucking=coordinated.Checked;settings.DuckMediaOnReminder=duck.Checked;settings.DuckRemainingPercent=(int)remaining.Value;}
}


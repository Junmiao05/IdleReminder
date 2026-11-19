// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class DeviceVolumePanel : FlowLayoutPanel {
 readonly Settings settings;readonly Audio audio;readonly Tray tray;
 readonly ThemedComboBox devices=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=490};
 readonly ThemedTrackBar target=new(){Minimum=1,Maximum=100,Value=35,TickFrequency=10,Width=360,AutoSize=true};
 readonly Label percent=new(){AutoSize=true},info=new(){AutoSize=true,MaximumSize=new Size(490,0)};
 readonly ThemedCheckBox physical=new(){Text=UiText.Get("DeviceVolumePanel_001"),AutoSize=true};
 readonly ThemedCheckBox enabled=new(){Text=UiText.Get("DeviceVolumePanel_002"),AutoSize=true};
 readonly Dictionary<string,DeviceVolumeSetting> drafts;
 readonly ThemedButton confirm=new(){Text=UiText.Get("DeviceVolumePanel_003"),Tag="Primary",AutoSize=true,Enabled=false};
 int? testedTarget;string? testedDevice;
 void ResetConfirmation(){testedTarget=null;testedDevice=null;confirm.Enabled=false;}
 bool loading,testing;
 OutputDevice? Selected=>devices.SelectedItem as OutputDevice;
 DeviceVolumeSetting? Current=>Selected is {} d?drafts.GetValueOrDefault(d.Id):null;
 internal DeviceVolumePanel(Settings settings,Audio audio,Tray tray){
  this.settings=settings;this.audio=audio;this.tray=tray;
  drafts=settings.DeviceVolumes.ToDictionary(x=>x.Key,x=>x.Value.Copy());
  FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;Width=510;
  Controls.Add(new Label{Text=UiText.Get("DeviceVolumePanel_004"),AutoSize=true});
  Controls.Add(new Label{Text=UiText.Get("DeviceVolumePanel_005"),AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});
  var diagnostic=new Label{Text=audio.LastVolumeStatus,AutoSize=true,MaximumSize=new Size(490,0)};
  Action<string> updateVolumeStatus=text=>diagnostic.Text=text;audio.VolumeStatusChanged+=updateVolumeStatus;Disposed+=(_,_)=>audio.VolumeStatusChanged-=updateVolumeStatus;Controls.Add(diagnostic);
  Controls.Add(devices);AddButton(UiText.Get("DeviceVolumePanel_006"),RefreshDevices);Controls.Add(info);
  Controls.Add(new Label{Text=UiText.Get("DeviceVolumePanel_007"),AutoSize=true});
  Controls.Add(target);Controls.Add(percent);Controls.Add(physical);Controls.Add(new Label{Text=UiText.Get("DeviceVolumePanel_008"),AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});Controls.Add(enabled);
  AddButton(UiText.Get("DeviceVolumePanel_009"),Test);
  AddButton(UiText.Get("DeviceVolumePanel_010"),()=>audio.Stop());
  Controls.Add(confirm);confirm.Click+=(_,_)=>{if(Current is not {} current||testedTarget!=target.Value||testedDevice!=Selected?.Id||!physical.Checked||Selected?.Speaker!=true||CoreAudio.DefaultId()!=testedDevice){ResetConfirmation();return;}current.ConfirmedTarget=target.Value;info.Text=UiText.Get("DeviceVolumePanel_011");ResetConfirmation();};
  Controls.Add(new Label{Text=UiText.Get("DeviceVolumePanel_012"),AutoSize=true,MaximumSize=new Size(490,0)});
  DetailsSection.Add(this,"DeviceVolumeDetails",490,("DeviceVolumePanel_012Details",false));
  Controls.Add(new Label{Text=UiText.Get("DeviceVolumePanel_012Tail"),AutoSize=true,MaximumSize=new Size(490,0)});
  devices.SelectedIndexChanged+=(_,_)=>LoadSelected();
  target.ValueChanged+=(_,_)=>{percent.Text=$"{target.Value}%";if(!loading&&Current is {} c){ResetConfirmation();c.TargetPercent=target.Value;c.ConfirmedTarget=null;c.Enabled=false;enabled.Checked=false;}};
  physical.CheckedChanged+=(_,_)=>{if(!loading&&!physical.Checked&&Current is {} c){ResetConfirmation();c.Enabled=false;c.ConfirmedTarget=null;enabled.Checked=false;}};
  enabled.CheckedChanged+=(_,_)=>{
   if(loading||Current is not {} c)return;
   if(enabled.Checked&&(Selected?.Speaker!=true||!physical.Checked||c.ConfirmedTarget!=target.Value)){enabled.Checked=false;MessageBox.Show(UiText.Get("DeviceVolumePanel_013"));return;}
   c.Enabled=enabled.Checked;
  };
  RefreshDevices();
 }
 void AddButton(string text,Action action){var b=new ThemedButton{Text=text,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(Exception e){Store.Log("device-volume-ui-failed",e);MessageBox.Show(UiText.Get("DeviceVolumePanel_014"));}};Controls.Add(b);}
 void RefreshDevices(){
  if(testing){audio.Stop();}
  var id=Selected?.Id;devices.Items.Clear();
  try{
   var list=CoreAudio.List();devices.Items.AddRange(list.Cast<object>().ToArray());
   var preferred=id??CoreAudio.DefaultId();
   devices.SelectedIndex=list.FindIndex(d=>d.Id==preferred);
   if(devices.SelectedIndex<0&&list.Count>0)devices.SelectedIndex=0;
   if(list.Count==0)info.Text=UiText.Get("DeviceVolumePanel_015");
  }catch(Exception e){Store.Log("device-enumeration-failed",e);info.Text=UiText.Get("DeviceVolumePanel_016");}
 }
 void LoadSelected(){
  ResetConfirmation();
  loading=true;
  try{
   if(Selected is not {} d){target.Enabled=physical.Enabled=enabled.Enabled=false;return;}
   if(!drafts.TryGetValue(d.Id,out var c)){c=new(){Name=d.Name};drafts[d.Id]=c;}
   c.Name=d.Name;
   target.Value=Math.Clamp(c.TargetPercent,1,100);percent.Text=$"{target.Value}%";
   target.Enabled=physical.Enabled=enabled.Enabled=d.Speaker;
   physical.Checked=c.ConfirmedTarget==c.TargetPercent;
   enabled.Checked=c.Enabled&&d.Speaker;
   info.Text=d.Reason+UiText.Get("DeviceVolumePanel_024");
  }finally{loading=false;}
 }
 void Test(){
  if(testing){MessageBox.Show(UiText.Get("DeviceVolumePanel_017"));return;}
  if(Selected is not {} d||!d.Speaker||Current is not {} c||!physical.Checked){MessageBox.Show(UiText.Get("DeviceVolumePanel_018"));return;}
  if(CoreAudio.DefaultId()!=d.Id){MessageBox.Show(UiText.Get("DeviceVolumePanel_019"));return;}
  if(settings.Mode!=0&&!settings.HasVoice()){MessageBox.Show(UiText.Get("DeviceVolumePanel_020"));return;}
  if(MessageBox.Show(UiText.Format("DeviceVolumePanel_025",d.Name,target.Value),UiText.Get("DeviceVolumePanel_026"),MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
  audio.Stop();ResetConfirmation();
  int requested=target.Value;bool validated=false;
  var trial=new DeviceVolumeSetting{TargetPercent=requested,Enabled=true,ConfirmedTarget=requested};
  testing=true;
  Action<bool>? done=null;
  done=completed=>{
   audio.Finished-=done;testing=false;
   if(!VolumePolicy.CanConfirmPreview(completed,validated,Selected?.Id==d.Id,target.Value==requested)){info.Text=UiText.Get("DeviceVolumePanel_021");return;}
   try{
    if(CoreAudio.DefaultId()!=d.Id||Selected?.Id!=d.Id||target.Value!=requested)return;
    testedTarget=requested;testedDevice=d.Id;confirm.Enabled=true;info.Text=UiText.Get("DeviceVolumePanel_022");
   }catch(Exception e){Store.Log("volume-preview-confirm-failed",e);}
  };
  audio.Finished+=done;
  audio.Play(settings.AudioFiles(AppContext.BaseDirectory,settings.Mode),settings.Volume,AudioPurpose.Preview,stop=>{
   var opened=CoreAudio.Open(d.Id);
   try{
    if(!CoreAudio.Describe(opened.Device).Speaker||CoreAudio.DefaultId()!=d.Id)throw new InvalidOperationException();
    opened.Volume.GetMute(out var mute);if(mute)throw new InvalidOperationException();
   }finally{CoreAudio.Release(opened.Volume);CoreAudio.Release(opened.Device);}
   var lease=VolumeLease.Create(d.Id,trial,stop,audio.ReportVolume,explicitVolumeConsent:true);
   if(lease==null){var check=CoreAudio.Open(d.Id);try{check.Volume.GetMasterVolumeLevelScalar(out var current);validated=current+0.001f>=requested/100f;}finally{CoreAudio.Release(check.Volume);CoreAudio.Release(check.Device);}}
   else validated=true;
   if(!validated)throw new InvalidOperationException("Target volume preview was blocked");return lease;
  });
  if(testing&&!audio.Playing){audio.Finished-=done;testing=false;ResetConfirmation();info.Text=UiText.Get("DeviceVolumePanel_023");}
 }
 internal void Apply(){
  if(testing)audio.Stop();
  settings.DeviceVolumes=drafts.ToDictionary(x=>x.Key,x=>x.Value.Copy());
 }
}



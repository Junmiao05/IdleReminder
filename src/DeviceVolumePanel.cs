// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class DeviceVolumePanel : FlowLayoutPanel {
 readonly Settings settings;readonly Audio audio;readonly Tray tray;
 readonly ThemedComboBox devices=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=490};
 readonly ThemedTrackBar target=new(){Minimum=1,Maximum=100,Value=35,TickFrequency=10,Width=360,AutoSize=true};
 readonly Label percent=new(){AutoSize=true},info=new(){AutoSize=true,MaximumSize=new Size(490,0)};
 readonly ThemedCheckBox physical=new(){Text="确认实际连接的是独立扬声器，不是耳机或混合插孔",AutoSize=true};
 readonly ThemedCheckBox enabled=new(){Text="对此设备启用自动提高音量（试听确认后可选）",AutoSize=true};
 readonly Dictionary<string,DeviceVolumeSetting> drafts;
 readonly Button confirm=new(){Text="确认本次试听音量合适",Tag="Primary",AutoSize=true,Enabled=false};
 int? testedTarget;string? testedDevice;
 void ResetConfirmation(){testedTarget=null;testedDevice=null;confirm.Enabled=false;}
 bool loading,testing;
 OutputDevice? Selected=>devices.SelectedItem as OutputDevice;
 DeviceVolumeSetting? Current=>Selected is {} d?drafts.GetValueOrDefault(d.Id):null;
 internal DeviceVolumePanel(Settings settings,Audio audio,Tray tray){
  this.settings=settings;this.audio=audio;this.tray=tray;
  drafts=settings.DeviceVolumes.ToDictionary(x=>x.Key,x=>x.Value.Copy());
  FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;Width=510;
  Controls.Add(new Label{Text="每个输出设备独立保存 Windows 主音量目标。默认关闭。",AutoSize=true});
  Controls.Add(new Label{Text="试听不触发媒体自动暂停；普通试听不自动提高音量。\n本页“带音量调整试听并确认”会在同意后临时调整主音量。",AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});
  var diagnostic=new Label{Text=audio.LastVolumeStatus,AutoSize=true,MaximumSize=new Size(490,0)};
  Action<string> updateVolumeStatus=text=>diagnostic.Text=text;audio.VolumeStatusChanged+=updateVolumeStatus;Disposed+=(_,_)=>audio.VolumeStatusChanged-=updateVolumeStatus;Controls.Add(diagnostic);
  Controls.Add(devices);AddButton("刷新输出设备",RefreshDevices);Controls.Add(info);
  Controls.Add(new Label{Text="目标 Windows 音量百分比（35% 仅为初始建议）",AutoSize=true});
  Controls.Add(target);Controls.Add(percent);Controls.Add(physical);Controls.Add(new Label{Text="建议设置好需要的软件音量压低后再启用（不强制）。\n未开启压低时，自动提高主音量会同时放大其他程序的声音。",AutoSize=true,MaximumSize=new Size(490,0),ForeColor=SystemColors.GrayText});Controls.Add(enabled);
  AddButton("带音量调整试听并确认",Test);
  AddButton("停止试听 / 恢复音量",()=>audio.Stop());
  Controls.Add(confirm);confirm.Click+=(_,_)=>{if(Current is not {} current||testedTarget!=target.Value||testedDevice!=Selected?.Id||!physical.Checked||Selected?.Speaker!=true||CoreAudio.DefaultId()!=testedDevice){ResetConfirmation();return;}current.ConfirmedTarget=target.Value;info.Text="试听已确认。请勾选对此设备启用自动提高音量，再点击保存设置。";ResetConfirmation();};
  Controls.Add(new Label{Text="注意：临时提高设备主音量，会同时放大该设备上其他程序的声音。\n设备已试听确认并启用后，未开启音乐压低也可提高主音量。\n媒体规则可允许压低音乐后协调提升，其他声音也可能变大。\n状态检查失败时仍不提高主音量。\n仅提高低于目标的音量，不降低现有音量，不解除静音。\n播放结束恢复；期间手动改音量或静音后不覆盖您的操作。\n播报期间调整音量可能会导致被压低的媒体声音突然变响。\n设备名或驱动报告不能保证识别真实接线；混合插孔禁止使用。\n普通试听按钮不会自动调整主音量。确认后还需勾选启用并保存设置。",AutoSize=true,MaximumSize=new Size(490,0)});
  devices.SelectedIndexChanged+=(_,_)=>LoadSelected();
  target.ValueChanged+=(_,_)=>{percent.Text=$"{target.Value}%";if(!loading&&Current is {} c){ResetConfirmation();c.TargetPercent=target.Value;c.ConfirmedTarget=null;c.Enabled=false;enabled.Checked=false;}};
  physical.CheckedChanged+=(_,_)=>{if(!loading&&!physical.Checked&&Current is {} c){ResetConfirmation();c.Enabled=false;c.ConfirmedTarget=null;enabled.Checked=false;}};
  enabled.CheckedChanged+=(_,_)=>{
   if(loading||Current is not {} c)return;
   if(enabled.Checked&&(Selected?.Speaker!=true||!physical.Checked||c.ConfirmedTarget!=target.Value)){enabled.Checked=false;MessageBox.Show("请先确认独立扬声器，并完成当前目标音量的试听确认。");return;}
   c.Enabled=enabled.Checked;
  };
  RefreshDevices();
 }
 void AddButton(string text,Action action){var b=new Button{Text=text,AutoSize=true};b.Click+=(_,_)=>{try{action();}catch(Exception e){Store.Log("device-volume-ui-failed",e);MessageBox.Show("设备音量操作失败，请刷新设备后重试。");}};Controls.Add(b);}
 void RefreshDevices(){
  if(testing){audio.Stop();}
  var id=Selected?.Id;devices.Items.Clear();
  try{
   var list=CoreAudio.List();devices.Items.AddRange(list.Cast<object>().ToArray());
   var preferred=id??CoreAudio.DefaultId();
   devices.SelectedIndex=list.FindIndex(d=>d.Id==preferred);
   if(devices.SelectedIndex<0&&list.Count>0)devices.SelectedIndex=0;
   if(list.Count==0)info.Text="没有可用输出设备，自动提升不可用。";
  }catch(Exception e){Store.Log("device-enumeration-failed",e);info.Text="设备识别不可用：自动提升已阻止。";}
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
   info.Text=d.Reason+"\n不同设备按 Windows 端点 ID 区分；驱动重装后可能需要重新设置。";
  }finally{loading=false;}
 }
 void Test(){
  if(testing){MessageBox.Show("正在试听，请先停止或等待结束。");return;}
  if(Selected is not {} d||!d.Speaker||Current is not {} c||!physical.Checked){MessageBox.Show("仅允许已确认实际连接为独立扬声器的设备试听调整。");return;}
  if(CoreAudio.DefaultId()!=d.Id){MessageBox.Show("请先在 Windows 中把所选设备设为默认输出，然后刷新。");return;}
  if(settings.Mode!=0&&!settings.HasVoice()){MessageBox.Show("请先选择人声，或切换为仅提示音并保存设置。");return;}
  if(MessageBox.Show($"将试听当前提醒，必要时把「{d.Name}」的 Windows 主音量提高到 {target.Value}%。\n这是明确授权的音量测试，会放大其他程序声音；不会自动暂停或压低媒体。请先暂停其他播放，并从较低目标开始。\n播放结束后恢复；不会解除静音。确定开始？","音量试听警告",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
  audio.Stop();ResetConfirmation();
  int requested=target.Value;bool validated=false;
  var trial=new DeviceVolumeSetting{TargetPercent=requested,Enabled=true,ConfirmedTarget=requested};
  testing=true;
  Action<bool>? done=null;
  done=completed=>{
   audio.Finished-=done;testing=false;
   if(!VolumePolicy.CanConfirmPreview(completed,validated,Selected?.Id==d.Id,target.Value==requested)){info.Text="试听未完成或目标音量未验证，无法确认。请查看上方音量状态后重试。";return;}
   try{
    if(CoreAudio.DefaultId()!=d.Id||Selected?.Id!=d.Id||target.Value!=requested)return;
    testedTarget=requested;testedDevice=d.Id;confirm.Enabled=true;info.Text="试听已完成。若音量合适，请点击下方确认按钮；确认后还需启用并保存。";
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
  if(testing&&!audio.Playing){audio.Finished-=done;testing=false;ResetConfirmation();info.Text="试听未能开始，请检查上方状态后重试。";}
 }
 internal void Apply(){
  if(testing)audio.Stop();
  settings.DeviceVolumes=drafts.ToDictionary(x=>x.Key,x=>x.Value.Copy());
 }
}


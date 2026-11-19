// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class PhonePanel:FlowLayoutPanel {
 readonly ThemedCheckBox enabled=new(){Text=UiText.Get("PhonePanel_001"),AutoSize=true};
 readonly TextBox path=new(){Width=475};
 readonly ThemedComboBox devices=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=475};
 readonly Label state=new(){Text=UiText.Get("PhonePanel_002"),AutoSize=true,MaximumSize=new Size(475,0)};
 readonly PhoneNotifications service=new();readonly CancellationTokenSource lifetime=new();
 readonly List<Button> actions=new();bool busy;string selectedId,selectedName;
 internal PhonePanel(Settings settings){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  Controls.Add(new Label{Text=UiText.Get("PhonePanel_003"),Tag="Accent",AutoSize=true,MaximumSize=new Size(475,0),UseMnemonic=false});
  selectedId=settings.Phone.DeviceId;selectedName=settings.Phone.DeviceName;enabled.Checked=settings.Phone.Enabled;path.Text=settings.Phone.CliPath;
  if(string.IsNullOrWhiteSpace(path.Text))path.Text=FindRunningCli();
  TextLine(UiText.Get("PhonePanel_004"));
  Controls.Add(enabled);TextLine(UiText.Get("PhonePanel_005"));Controls.Add(path);
  AddButton(UiText.Get("PhonePanel_006"),()=>{using var dialog=new OpenFileDialog{Filter=UiText.Get("PhonePanel_017")+"|kdeconnect-cli.exe",Title=UiText.Get("PhonePanel_007")};if(dialog.ShowDialog()==DialogResult.OK)path.Text=dialog.FileName;return Task.CompletedTask;});
  AddButton(UiText.Get("PhonePanel_008"),RefreshAsync);Controls.Add(devices);Controls.Add(state);
  AddButton(UiText.Get("PhonePanel_009"),TestAsync);
  TextLine(UiText.Get("PhonePanel_010"));
  DetailsSection.Add(this,"PhoneDeliveryDetails",475,("PhonePanel_010Details",false),("PhonePanel_011",false));
  if(!string.IsNullOrEmpty(selectedId))state.Text=UiText.Get("PhonePanel_012")+selectedName+UiText.Get("PhonePanel_018");
  devices.SelectedIndexChanged+=(_,_)=>{if(devices.SelectedItem is PhoneDevice d){selectedId=d.Id;selectedName=d.Name;state.Text=UiText.Get("PhonePanel_013")+d.Name+UiText.Get("PhonePanel_019");}};
  path.TextChanged+=(_,_)=>{devices.Items.Clear();selectedId=selectedName="";state.Text=UiText.Get("PhonePanel_014");};
  Disposed+=(_,_)=>{lifetime.Cancel();lifetime.Dispose();};
 }
 static string FindRunningCli(){
  foreach(var process in System.Diagnostics.Process.GetProcessesByName("kdeconnectd")){
   using(process){try{var exe=process.MainModule?.FileName;if(exe!=null){var cli=Path.Combine(Path.GetDirectoryName(exe)!,"kdeconnect-cli.exe");if(File.Exists(cli))return cli;}}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}}
  }
  return "";
 }
 void TextLine(string text)=>Controls.Add(new Label{Text=text,AutoSize=true,MaximumSize=new Size(475,0),UseMnemonic=false});
 void AddButton(string text,Func<Task> action){var button=new ThemedButton{Text=text,AutoSize=true};actions.Add(button);Controls.Add(button);button.Click+=async(_,_)=>{
  if(busy)return;busy=true;foreach(var b in actions)b.Enabled=false;path.Enabled=devices.Enabled=false;
  try{await action();}catch(OperationCanceledException){if(!IsDisposed)state.Text=UiText.Get("PhonePanel_015");}
  catch(Exception ex){Store.Log("phone-ui-failed",ex);if(!IsDisposed)state.Text=UiText.Get("PhonePanel_016");}
  finally{if(!IsDisposed){busy=false;foreach(var b in actions)b.Enabled=true;path.Enabled=devices.Enabled=true;}}
 };}
 async Task RefreshAsync(){
  var cliPath=path.Text;var token=lifetime.Token;var result=await Task.Run(()=>service.AvailableAsync(cliPath,token));if(IsDisposed)return;
  devices.Items.Clear();devices.Items.AddRange(result.Cast<object>().ToArray());
  var index=result.ToList().FindIndex(d=>d.Id==selectedId);devices.SelectedIndex=index;
  state.Text=result.Count==0?UiText.Get("PhonePanel_020"):index>=0?UiText.Get("PhonePanel_021"):UiText.Get("PhonePanel_022");
 }
 async Task TestAsync(){
  var options=Draft();var token=lifetime.Token;var text=await Task.Run(()=>service.SendAsync(options,true,token));if(!IsDisposed)state.Text=text;
 }
 PhoneOptions Draft()=>new(){Enabled=enabled.Checked,CliPath=path.Text.Trim(),DeviceId=selectedId,DeviceName=selectedName};
 internal void Apply(Settings settings){
  if(busy)throw new InvalidOperationException(UiText.Get("PhonePanel_023"));var draft=Draft();
  if(draft.Enabled&&(!PhonePolicy.ValidId(draft.DeviceId)||!PhonePolicy.ValidPath(draft.CliPath)||!File.Exists(draft.CliPath)))throw new InvalidOperationException(UiText.Get("PhonePanel_024"));
  settings.Phone=draft;
 }
}







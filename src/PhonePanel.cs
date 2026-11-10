// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal sealed class PhonePanel:FlowLayoutPanel {
 readonly ThemedCheckBox enabled=new(){Text="启用手机通知（正式播报时发送）",AutoSize=true};
 readonly TextBox path=new(){Width=475};
 readonly ThemedComboBox devices=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=475};
 readonly Label state=new(){Text="手机连接尚未检查：请刷新已配对且在线的手机。",AutoSize=true,MaximumSize=new Size(475,0)};
 readonly PhoneNotifications service=new();readonly CancellationTokenSource lifetime=new();
 readonly List<Button> actions=new();bool busy;string selectedId,selectedName;
 internal PhonePanel(Settings settings){
  AutoSize=true;Width=490;FlowDirection=FlowDirection.TopDown;WrapContents=false;
  Controls.Add(new Label{Text="实验性功能，效果不稳定",Tag="Accent",AutoSize=true,MaximumSize=new Size(475,0),UseMnemonic=false});
  selectedId=settings.Phone.DeviceId;selectedName=settings.Phone.DeviceName;enabled.Checked=settings.Phone.Enabled;path.Text=settings.Phone.CliPath;
  if(string.IsNullOrWhiteSpace(path.Text))path.Text=FindRunningCli();
  TextLine("通过 KDE Connect 向指定、已配对的 Android 手机发送本地提醒。默认关闭。电脑和手机需安装 KDE Connect，并在可信局域网中手动配对。");
  Controls.Add(enabled);TextLine("官方命令行程序 kdeconnect-cli.exe 的完整位置");Controls.Add(path);
  AddButton("浏览 KDE Connect 命令行程序",()=>{using var dialog=new OpenFileDialog{Filter="KDE Connect 命令行|kdeconnect-cli.exe",Title="选择官方安装目录中的 kdeconnect-cli.exe"};if(dialog.ShowDialog()==DialogResult.OK)path.Text=dialog.FileName;return Task.CompletedTask;});
  AddButton("刷新已配对且在线的手机",RefreshAsync);Controls.Add(devices);Controls.Add(state);
  AddButton("向所选手机发送测试通知",TestAsync);
  TextLine("测试会立即向当前所选手机发送一条固定文字，无需先保存；普通音频试听不发送。设置完成后请点击底部保存设置。\n正式播报实际开始后异步发送；手机离线或失败不影响电脑播报，重连后不补发过期通知。");
  TextLine("请先在 KDE Connect 中完成配对，允许手机显示通知；如锁屏收不到，检查手机后台省电限制。关闭文件共享、剪贴板、远程输入、通知镜像等无关插件。\n本程序不自动安装软件、配对或修改防火墙，只发送固定提醒，不发送文件名或应用内容。\n提交成功不等于手机已收到；通知声音、勿扰和锁屏表现由手机系统决定。");
  if(!string.IsNullOrEmpty(selectedId))state.Text="已保存目标："+selectedName+"；尚未检查连接。离线刷新不会清除已保存目标。";
  devices.SelectedIndexChanged+=(_,_)=>{if(devices.SelectedItem is PhoneDevice d){selectedId=d.Id;selectedName=d.Name;state.Text="已选择："+d.Name+"。请测试并保存。";}};
  path.TextChanged+=(_,_)=>{devices.Items.Clear();selectedId=selectedName="";state.Text="命令行位置已修改，请刷新并重新选择手机。";};
  Disposed+=(_,_)=>{lifetime.Cancel();lifetime.Dispose();};
 }
 static string FindRunningCli(){
  foreach(var process in System.Diagnostics.Process.GetProcessesByName("kdeconnectd")){
   using(process){try{var exe=process.MainModule?.FileName;if(exe!=null){var cli=Path.Combine(Path.GetDirectoryName(exe)!,"kdeconnect-cli.exe");if(File.Exists(cli))return cli;}}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}}
  }
  return "";
 }
 void TextLine(string text)=>Controls.Add(new Label{Text=text,AutoSize=true,MaximumSize=new Size(475,0),UseMnemonic=false});
 void AddButton(string text,Func<Task> action){var button=new Button{Text=text,AutoSize=true};actions.Add(button);Controls.Add(button);button.Click+=async(_,_)=>{
  if(busy)return;busy=true;foreach(var b in actions)b.Enabled=false;path.Enabled=devices.Enabled=false;
  try{await action();}catch(OperationCanceledException){if(!IsDisposed)state.Text="手机通知操作已取消或超时，电脑播报不受影响。";}
  catch(Exception ex){Store.Log("phone-ui-failed",ex);if(!IsDisposed)state.Text="手机通知操作失败，请检查官方CLI路径、KDE Connect是否运行及设备配对。";}
  finally{if(!IsDisposed){busy=false;foreach(var b in actions)b.Enabled=true;path.Enabled=devices.Enabled=true;}}
 };}
 async Task RefreshAsync(){
  var cliPath=path.Text;var token=lifetime.Token;var result=await Task.Run(()=>service.AvailableAsync(cliPath,token));if(IsDisposed)return;
  devices.Items.Clear();devices.Items.AddRange(result.Cast<object>().ToArray());
  var index=result.ToList().FindIndex(d=>d.Id==selectedId);devices.SelectedIndex=index;
  state.Text=result.Count==0?"未找到已配对且在线的手机；保留原目标，不会自动改发其他设备。":index>=0?"目标手机在线。可发送测试通知。":"请选择一台已配对且在线的手机，不会自动选择其他设备。";
 }
 async Task TestAsync(){
  var options=Draft();var token=lifetime.Token;var text=await Task.Run(()=>service.SendAsync(options,true,token));if(!IsDisposed)state.Text=text;
 }
 PhoneOptions Draft()=>new(){Enabled=enabled.Checked,CliPath=path.Text.Trim(),DeviceId=selectedId,DeviceName=selectedName};
 internal void Apply(Settings settings){
  if(busy)throw new InvalidOperationException("请等待手机通知操作结束后保存。");var draft=Draft();
  if(draft.Enabled&&(!PhonePolicy.ValidId(draft.DeviceId)||!PhonePolicy.ValidPath(draft.CliPath)||!File.Exists(draft.CliPath)))throw new InvalidOperationException("启用手机通知前，请选择官方命令行程序和已配对手机。");
  settings.Phone=draft;
 }
}






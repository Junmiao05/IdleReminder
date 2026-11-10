// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
using System.Text;
namespace IdleReminder;

public sealed class PhoneOptions {
 public bool Enabled {get;set;}
 public string CliPath {get;set;}="";
 public string DeviceId {get;set;}="";
 public string DeviceName {get;set;}="";
 public PhoneOptions Copy()=>new(){Enabled=Enabled,CliPath=CliPath,DeviceId=DeviceId,DeviceName=DeviceName};
 public void Validate(){CliPath??="";DeviceId??="";DeviceName??="";if(!PhonePolicy.ValidId(DeviceId)||!PhonePolicy.ValidPath(CliPath))Enabled=false;}
}
public static class PhonePolicy {
 public const string Message="闲置提醒：你已离开电脑一段时间，记得回来看看。";
 public static bool ValidId(string? id)=>!string.IsNullOrEmpty(id)&&id.Length<=128&&char.IsAsciiLetterOrDigit(id[0])&&id.All(c=>char.IsAsciiLetterOrDigit(c)||c is '-' or '_');
 public static bool ValidPath(string? path)=>!string.IsNullOrWhiteSpace(path)&&Path.IsPathFullyQualified(path)&&!path.StartsWith(@"\\")&&string.Equals(Path.GetFileName(path),"kdeconnect-cli.exe",StringComparison.OrdinalIgnoreCase);
 public static IReadOnlyList<PhoneDevice> ParseDevices(string text){
  var devices=new Dictionary<string,PhoneDevice>(StringComparer.Ordinal);
  foreach(var raw in text.Split('\n')){
   var line=raw.Trim();int split=line.IndexOf(' ');var id=split<0?line:line[..split];
   if(ValidId(id))devices.TryAdd(id,new(id,split<0?id:line[(split+1)..].Trim()));
  }
  return devices.Values.ToArray();
 }
}
public sealed record PhoneDevice(string Id,string Name){public override string ToString()=>Name+" ["+Id+"]";}
public sealed record PhoneCommandResult(int ExitCode,string Output);
public interface IPhoneCommandRunner {Task<PhoneCommandResult> RunAsync(string path,IReadOnlyList<string> arguments,CancellationToken token);}
public sealed class PhoneCommandRunner:IPhoneCommandRunner {
 public async Task<PhoneCommandResult> RunAsync(string path,IReadOnlyList<string> arguments,CancellationToken token){
  if(!PhonePolicy.ValidPath(path)||!File.Exists(path))throw new FileNotFoundException("请选择官方 kdeconnect-cli.exe。");
  using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(4));
  var start=new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=Path.GetDirectoryName(path)!};
  foreach(var argument in arguments)start.ArgumentList.Add(argument);
  using var process=new Process{StartInfo=start};deadline.Token.ThrowIfCancellationRequested();
  if(!process.Start())throw new IOException("KDE Connect命令未启动。");
  var output=ReadBoundedAsync(process.StandardOutput,deadline.Token);var error=ReadBoundedAsync(process.StandardError,deadline.Token);
  try{await Task.WhenAll(process.WaitForExitAsync(deadline.Token),output,error).ConfigureAwait(false);return new(process.ExitCode,await output.ConfigureAwait(false));}
  catch{try{if(!process.HasExited)process.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}throw;}
 }
 [System.Runtime.InteropServices.DllImport("kernel32.dll")]static extern uint GetACP();
 static async Task<string> ReadBoundedAsync(StreamReader reader,CancellationToken token){
  var buffer=new byte[1024];using var output=new MemoryStream();int length;
  while((length=await reader.BaseStream.ReadAsync(buffer.AsMemory(),token).ConfigureAwait(false))>0){
   if(output.Length+length>32768)throw new InvalidDataException("KDE Connect输出超出限制。");output.Write(buffer,0,length);
  }
  var bytes=output.ToArray();try{return new UTF8Encoding(false,true).GetString(bytes);}
  catch(DecoderFallbackException) when(OperatingSystem.IsWindows()){Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);return Encoding.GetEncoding((int)GetACP()).GetString(bytes);}
 }
}
public sealed class PhoneNotifications {
 readonly IPhoneCommandRunner runner;int sending;
 public PhoneNotifications(IPhoneCommandRunner? runner=null){this.runner=runner??new PhoneCommandRunner();}
 public async Task<IReadOnlyList<PhoneDevice>> AvailableAsync(string path,CancellationToken token){
  var result=await runner.RunAsync(path,new[]{"--list-available","--id-name-only"},token).ConfigureAwait(false);
  if(result.ExitCode!=0)throw new IOException("KDE Connect设备检查失败。");
  return PhonePolicy.ParseDevices(result.Output);
 }
 public async Task<string> SendAsync(PhoneOptions options,bool test,CancellationToken token){
  if(!test&&!options.Enabled)return "手机通知已关闭";
  if(!PhonePolicy.ValidId(options.DeviceId)||!PhonePolicy.ValidPath(options.CliPath))return "手机通知未配置";
  if(Interlocked.CompareExchange(ref sending,1,0)!=0)return "手机通知发送中，本次跳过";
  try{
   using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(6));
   var devices=await AvailableAsync(options.CliPath,deadline.Token).ConfigureAwait(false);
   if(!devices.Any(d=>d.Id==options.DeviceId))return "手机未连接，本次跳过（不补发）";
   var result=await runner.RunAsync(options.CliPath,new[]{"--device",options.DeviceId,"--ping-msg",test?"闲置提醒器：这是一条手机通知测试。":PhonePolicy.Message},deadline.Token).ConfigureAwait(false);
   return result.ExitCode==0?"通知命令已提交，请在手机确认是否收到":"手机通知发送失败，电脑提醒不受影响";
  }finally{Volatile.Write(ref sending,0);}
 }
}



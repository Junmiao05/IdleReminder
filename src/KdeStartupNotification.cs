// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
internal static class KdeStartupNotification {
 internal const string Title="手机通知已启用";
 internal const string Text="请打开手机上的 KDE Connect，并确认电脑与手机已连接。手机端被彻底关闭后，可能无法收到通知。";
 internal static string? FindIconExecutable(string cliPath){
  if(!PhonePolicy.ValidPath(cliPath))return null;
  var folder=Path.GetDirectoryName(cliPath)!;
  var app=Path.Combine(folder,"kdeconnect-app.exe");
  return File.Exists(app)?app:File.Exists(cliPath)?cliPath:null;
 }
 internal static Task<bool> ShowAsync(string cliPath,CancellationToken token){
  var completion=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
  var thread=new Thread(()=>{
   bool submitted=false;object? instance=null;
   try{
    if(token.IsCancellationRequested)return;
    var exe=FindIconExecutable(cliPath);if(exe==null)return;
    using var picture=Icon.ExtractAssociatedIcon(exe);if(picture==null)return;
    instance=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("0010890e-8789-413c-adbc-48f5b511b3af"),true)!);
    var notification=(IUserNotification)instance!;
    notification.SetIconInfo(picture.Handle,"闲置提醒器：手机连接提示");
    notification.SetBalloonInfo(Title,Text,0x4|0x20|0x80); // USER, LARGE_ICON, RESPECT_QUIET_TIME
    notification.SetBalloonRetry(10000,0,0);
    token.ThrowIfCancellationRequested();
    Store.Log("kde-startup-custom-icon-submitted");
    submitted=true;
    var query=new NotificationContinue(token);
    int result=notification.Show(query,250);
    Store.Log("kde-startup-show-result-"+result.ToString("X8",System.Globalization.CultureInfo.InvariantCulture));
    if(result<0&&result!=unchecked((int)0x800704C7)&&result!=unchecked((int)0x800705B4)&&!token.IsCancellationRequested)Store.Log("kde-startup-notification-ended",Marshal.GetExceptionForHR(result));
    GC.KeepAlive(query);
   }catch(OperationCanceledException){}
   catch(Exception ex){Store.Log("kde-startup-custom-icon-failed",ex);}
   finally{
    if(instance!=null)try{Marshal.ReleaseComObject(instance);}catch(Exception ex){Store.Log("kde-startup-icon-release-failed",ex);}
    completion.TrySetResult(submitted);
   }
  }){IsBackground=true,Name="KDE startup notification"};
  thread.SetApartmentState(ApartmentState.STA);thread.Start();return completion.Task;
 }
 [ComImport,Guid("ba9711ba-5893-4787-a7e1-41277151550b"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IUserNotification {
  void SetBalloonInfo([MarshalAs(UnmanagedType.LPWStr)]string title,[MarshalAs(UnmanagedType.LPWStr)]string text,uint flags);
  void SetBalloonRetry(uint showTime,uint interval,uint retries);
  void SetIconInfo(IntPtr icon,[MarshalAs(UnmanagedType.LPWStr)]string tooltip);
  [PreserveSig]int Show([MarshalAs(UnmanagedType.Interface)]INotificationContinue query,uint pollInterval);
  void PlaySound([MarshalAs(UnmanagedType.LPWStr)]string sound);
 }
}
[ComVisible(true),Guid("7307055c-b24a-486b-9f25-163e597a28a9"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INotificationContinue {[PreserveSig]int QueryContinue();}
[ComVisible(true),ClassInterface(ClassInterfaceType.None)]
public sealed class NotificationContinue:INotificationContinue {
 readonly CancellationToken token;readonly long deadline=Environment.TickCount64+20000;
 public NotificationContinue(CancellationToken token){this.token=token;}
 public int QueryContinue()=>token.IsCancellationRequested||Environment.TickCount64>=deadline?1:0;
}


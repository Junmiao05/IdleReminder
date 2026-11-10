// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class Program {
 [STAThread] static void Main(string[] args) {
 if(SlideShowHelper.IsHelper(args)){try{SlideShowHelper.Run(args);}catch(Exception ex){Store.Log("ppt-helper-exit",ex);}return;}
 if(args.Contains("--verify-ppt-full-series")){SlideShowHelper.VerifyFullSeriesAsync().GetAwaiter().GetResult();return;}
 if(args.Contains("--verify-ppt-helper-series")){SlideShowHelper.VerifySeriesAsync().GetAwaiter().GetResult();return;}
 if(args.Contains("--verify-ppt-helper")){SlideShowHelper.VerifyAsync().GetAwaiter().GetResult();return;}
 SettingsWindowPresenter.Trace=args.Contains("--trace-settings");
 if(args.Contains("--test-reminder-volume")){ApplicationConfiguration.Initialize();ReminderVerification.Run();return;}
 if(args.Contains("--test-coordinated-volume")){ApplicationConfiguration.Initialize();DuckingVerification.Run(true);return;}
 if(args.Contains("--test-ducking")){ApplicationConfiguration.Initialize();DuckingVerification.Run();return;}
 if(args.Contains("--diagnose-volume")){ApplicationConfiguration.Initialize();VolumeDiagnosis.Run();return;}
 if(args.Contains("--verify-checkboxes")){ApplicationConfiguration.Initialize();CheckBoxVerification.Run();return;}
 if(args.Contains("--verify-slideshow")){SlideShowDetector.VerifyAsync().GetAwaiter().GetResult();return;}
 if(args.Contains("--verify-suppression")){SuppressionDetector.VerifyAsync().GetAwaiter().GetResult();return;}
 if(args.Contains("--verify-media")){MediaPause.VerifyReadOnlyAsync().GetAwaiter().GetResult();return;}
 if(args.Contains("--verify-devices")){ApplicationConfiguration.Initialize();DeviceVerification.Run();return;}
 if(args.Contains("--verify-audio")){ApplicationConfiguration.Initialize();Verification.Run();return;}
 if(!StartupElevation.WaitForParent(args)){MessageBox.Show("管理员交接未完成，原实例保留；请从托盘退出后重试。");return;}
 if(StartupElevation.AnotherInstanceRunning()){MessageBox.Show("闲置提醒器已在运行，请从系统托盘打开设置。");return;}
 if(StartupElevation.TryStartElevated(args))return;
 using var mutex=new Mutex(true,StartupElevation.InstanceName,out var first);
 if(!first){MessageBox.Show("闲置提醒器已在运行，请从系统托盘打开设置。");return;}
 ApplicationConfiguration.Initialize();
 Application.ThreadException+=(_,e)=>{Store.Log("ui-error",e.Exception);MessageBox.Show("发生错误，详情见项目 data 诊断日志。");};
 AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Store.Log("fatal-error",e.ExceptionObject as Exception);
 try{using var app=new Tray(args.Contains("--background"));Application.Run(app);}catch(Exception e){Store.Log("startup-failed",e);MessageBox.Show("程序无法启动，请检查项目 data 诊断日志。");}
 }
}


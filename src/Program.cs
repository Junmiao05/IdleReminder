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
 try{ApplicationLanguage.Apply(Store.Load().Language);}catch(Exception ex){Store.Log("language-load-failed",ex);ApplicationLanguage.Apply(LanguagePolicy.SimplifiedChinese);}
 if(!StartupElevation.WaitForParent(args)){MessageBox.Show(UiText.Get("Program_001"));return;}
 if(StartupElevation.AnotherInstanceRunning()){MessageBox.Show(UiText.Get("Program_002"));return;}
 if(StartupElevation.TryStartElevated(args))return;
 using var mutex=new Mutex(true,StartupElevation.InstanceName,out var first);
 if(!first){MessageBox.Show(UiText.Get("Program_002"));return;}
 ApplicationConfiguration.Initialize();
 Application.ThreadException+=(_,e)=>{Store.Log("ui-error",e.Exception);MessageBox.Show(UiText.Get("Program_003"));};
 AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Store.Log("fatal-error",e.ExceptionObject as Exception);
 try{using var app=new Tray(args.Contains("--background"));Application.Run(app);}catch(Exception e){Store.Log("startup-failed",e);MessageBox.Show(UiText.Get("Program_004"));}
 }
}


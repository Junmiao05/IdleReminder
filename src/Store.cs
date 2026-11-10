// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Text.Json;
namespace IdleReminder;
internal static class Store {
 internal static readonly string Root=FindRoot();
 internal static string Data=>Path.Combine(Root,"data");
 static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d!=null){if(File.Exists(Path.Combine(d.FullName,".idlereminder-root")))return d.FullName;d=d.Parent;}return AppContext.BaseDirectory;}
 internal static void Log(string code,Exception? ex=null){try{Directory.CreateDirectory(Data);var p=Path.Combine(Data,"diagnostic.log");if(File.Exists(p)&&new FileInfo(p).Length>262144)File.Move(p,p+".old",true);File.AppendAllText(p,$"{DateTimeOffset.Now:O} {code} {ex?.GetType().Name} HResult={ex?.HResult}\n");}catch(System.IO.IOException){}catch(UnauthorizedAccessException){}}
 static readonly ConfigFileStore config=new(Path.Combine(Data,"settings.json"));
 internal static bool WriteBlocked=>config.WriteBlocked;
 internal static Settings Load(){var value=config.Load();if(config.WriteBlocked)Log("config-load-blocked");return value;}
 internal static void Save(Settings settings)=>config.Save(settings);
 internal static string Import(string path,string kind){var ext=Path.GetExtension(path).ToLowerInvariant();if(ext!=".wav"&&ext!=".mp3")throw new InvalidOperationException("仅支持 WAV 或 MP3");if(new FileInfo(path).Length>50*1024*1024)throw new InvalidOperationException("音频不能超过 50 MB");var p=Path.Combine(Data,kind+"-"+Guid.NewGuid().ToString("N")+ext);File.Copy(path,p);return p;}
}

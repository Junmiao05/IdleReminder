// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace IdleReminder;
public sealed class ConfigFileStore {
 readonly string path;readonly object gate=new();string? fingerprint;bool loaded;
 public bool WriteBlocked {get;private set;}
 public string? LoadError {get;private set;}
 public ConfigFileStore(string path){this.path=Path.GetFullPath(path);}
 static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
 public Settings Load(){lock(gate){
  loaded=true;WriteBlocked=false;LoadError=null;
  try{
   if(!File.Exists(path)){if(File.Exists(path+".bak"))throw new IOException("主配置缺失，但存在备份；请先恢复备份。");fingerprint=null;return new(){TimingSetupAcknowledged=false};}
   var bytes=File.ReadAllBytes(path);fingerprint=Hash(bytes);
   var value=JsonSerializer.Deserialize<Settings>(bytes.AsSpan(bytes.Length>=3&&bytes[0]==0xEF&&bytes[1]==0xBB&&bytes[2]==0xBF?3:0))??throw new InvalidDataException("配置不能为null");value.Validate();return value;
  }catch(Exception e){WriteBlocked=true;LoadError=e.GetType().Name;return new();}
 }}
 public void Save(Settings settings){lock(gate){
  if(!loaded||WriteBlocked)throw new InvalidOperationException("配置读取失败或尚未读取，已禁止覆盖。请保留settings.json并从备份恢复后重新启动。");
  var candidate=settings.Copy();candidate.Validate();
  string? current=File.Exists(path)?Hash(File.ReadAllBytes(path)):null;
  if(current!=fingerprint)throw new IOException("配置被其他实例或外部程序修改，已停止保存。请重新打开程序核对配置。");
  Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(candidate,new JsonSerializerOptions{WriteIndented=true}));
  string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){stream.Write(bytes);stream.Flush(true);}
   if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
   fingerprint=Hash(bytes);
  }finally{if(File.Exists(temp)){try{File.Delete(temp);}catch(IOException){}}}
 }}
}

// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Diagnostics;
namespace IdleReminder;
public sealed record SteamImportCandidate(string Game,string Executable){public override string ToString()=>Game+" — "+Executable;}
public sealed record SteamImportResult(IReadOnlyList<SteamImportCandidate> Candidates,int SkippedDirectories,bool Limited);
public static class SteamApplicationImport {
 public static string CommonFolder(string selected){
  var folder=Path.GetFullPath(selected);
  string[] candidates=[Path.Combine(folder,"steamapps","common"),Path.Combine(folder,"common"),folder];
  foreach(var candidate in candidates){
   if(!Directory.Exists(candidate))continue;
   var info=new DirectoryInfo(candidate);
   if(info.Name.Equals("common",StringComparison.OrdinalIgnoreCase)&&info.Parent?.Name.Equals("steamapps",StringComparison.OrdinalIgnoreCase)==true)return info.FullName;
  }
  throw new ArgumentException("请选择Steam安装目录、Steam库目录、steamapps或steamapps\\common目录。其他库需分别导入。");
 }
 public static SteamImportResult Scan(string selected,CancellationToken token,int maxCandidates=3000,int maxDirectories=20000,int maxDepth=12,TimeSpan? timeLimit=null){
  token.ThrowIfCancellationRequested();
  var root=CommonFolder(selected);var results=new List<SteamImportCandidate>();var pending=new Stack<(string Path,int Depth)>();
  // Never follow directory links/junctions, including ancestors of the chosen common folder.
  for(DirectoryInfo? p=new(root);p!=null;p=p.Parent)if((p.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("为避免扫描链接指向的其他目录，请选择不经过目录链接的Steam库。");
  pending.Push((root,0));var clock=Stopwatch.StartNew();int visited=0,skipped=0;bool limited=false;
  while(pending.Count>0){
   token.ThrowIfCancellationRequested();
   if(visited++>=maxDirectories||clock.Elapsed>(timeLimit??TimeSpan.FromSeconds(20))){limited=true;break;}
   var (folder,depth)=pending.Pop();
   try{
    if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0){skipped++;continue;}
    // The root itself is not a game directory.
    if(depth>0)foreach(var file in Directory.EnumerateFiles(folder,"*.exe",SearchOption.TopDirectoryOnly)){
     token.ThrowIfCancellationRequested();
     if(clock.Elapsed>(timeLimit??TimeSpan.FromSeconds(20))){limited=true;break;}
     if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)continue;
     if(results.Count>=maxCandidates){limited=true;break;}
     var relative=Path.GetRelativePath(root,file);var game=relative.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)[0];
     results.Add(new(game,Path.GetFullPath(file)));
    }
    if(limited)break;
    foreach(var child in Directory.EnumerateDirectories(folder)){
     token.ThrowIfCancellationRequested();
     if(depth>=maxDepth){limited=true;continue;}
     if(pending.Count+visited>=maxDirectories){limited=true;break;}
     pending.Push((child,depth+1));
    }
   }catch(Exception ex) when(ex is UnauthorizedAccessException or IOException){skipped++;}
  }
  return new(results.OrderBy(x=>x.Game,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.Executable,StringComparer.OrdinalIgnoreCase).ToArray(),skipped,limited);
 }
}

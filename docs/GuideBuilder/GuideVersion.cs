// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Text.RegularExpressions;
using System.Xml.Linq;

internal static class GuideVersion {
 internal static string Resolve(string folder,string? buildVersion=null){
  var value=buildVersion??XDocument.Load(Path.Combine(folder,"..","src","IdleReminder.csproj")).Descendants("Version").FirstOrDefault()?.Value;
  if(!Version.TryParse(value,out var version)||version.Build<0||version.Revision<0)throw new InvalidDataException("A four-part IR version is required to generate guides.");
  return version.ToString(4);
 }
 internal static string Annotate(string markdown,string version,bool english){
  var title=Regex.Match(markdown,@"(?m)^# [^\r\n]+\r?$");
  if(!title.Success)throw new InvalidDataException("Missing guide title");
  string line=(english?"Applies to IdleReminder ":"适用版本：IdleReminder ")+"v"+version;
  return markdown.Insert(title.Index+title.Length,"\n\n"+line);
 }
}

// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

if(args.Contains("--self-test")){GuidePlainTextChecks.Run();return;}
var folder=Path.GetFullPath(args.Length>0?args[0]:Path.Combine(AppContext.BaseDirectory,"..","..","..",".."));
var version=GuideVersion.Resolve(folder,args.Length>1?args[1]:null);
var css=File.ReadAllText(Path.Combine(folder,"guide.css"));
foreach(var language in new[]{"zh-Hans","en"}){
 var source=Path.Combine(folder,$"UserGuide.{language}.md");
 var markdown=GuideVersion.Annotate(File.ReadAllText(source),version,language=="en");
 var (body,headings,title)=GuideMarkdown.Render(markdown);
 bool english=language=="en";
 string toc=string.Join("\n",headings.Select(x=>$"<li><a href=\"#{x.Id}\">{GuideMarkdown.Inline(x.Title)}</a></li>"));
 string currentZh=english?"":" aria-current=\"page\"";
 string currentEn=english?" aria-current=\"page\"":"";
 string html=$$"""
<!doctype html>
<html lang="{{language}}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="light dark">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'">
<title>{{WebUtility.HtmlEncode(title)}} — v{{version}}</title>
<style>{{css}}</style>
</head>
<body>
<a class="skip" href="#content">{{(english?"Skip to guide":"跳到正文")}}</a>
<header class="site-header">
 <a class="brand" href="#content">IdleReminder <span>{{(english?"User guide":"使用指南")}}</span></a>
 <nav class="languages" aria-label="{{(english?"Language":"语言")}}"><a href="UserGuide.zh-Hans.html" lang="zh-Hans"{{currentZh}}>简体中文</a><a href="UserGuide.en.html" lang="en"{{currentEn}}>English</a></nav>
</header>
<div class="layout">
 <nav class="toc" aria-label="{{(english?"Contents":"目录")}}"><h2>{{(english?"On this page":"本页目录")}}</h2><ol>{{toc}}</ol></nav>
 <details class="mobile-toc"><summary>{{(english?"Contents — jump to a section":"目录 — 跳转到章节")}}</summary><nav aria-label="{{(english?"Contents":"目录")}}"><ol>{{toc}}</ol></nav></details>
 <main id="content">{{body}}<p class="back-top"><a href="#content">{{(english?"Back to top":"返回顶部")}} ↑</a></p></main>
</div>
<footer>{{(english?"Read this guide offline. External links need an internet connection.":"本指南可离线阅读；访问外部链接时需要网络连接。")}}</footer>
</body>
</html>
""";
 if(headings.Count<5)throw new InvalidDataException("Guide has too few sections: "+source);
 File.WriteAllText(Path.Combine(folder,$"UserGuide.{language}.html"),html,new UTF8Encoding(false));
 File.WriteAllText(Path.Combine(folder,$"UserGuide.{language}.txt"),GuideMarkdown.PlainText(markdown),new UTF8Encoding(true));
 Console.WriteLine($"Built {language}: HTML + TXT, v{version}, {headings.Count} sections");
}

// A deliberately small Markdown subset for the two maintained guides. Unknown
// text is escaped rather than treated as HTML. No package or runtime web access.
internal static class GuideMarkdown {
 internal record Heading(string Id,string Title);
 static readonly Regex tokens=new(@"`([^`]+)`|\*\*([^*]+)\*\*|\[([^\]]+)\]\(([^\s)]+)\)",RegexOptions.Compiled);
 static string Encode(string text)=>WebUtility.HtmlEncode(text);
 internal static string PlainInline(string text){
  var output=new StringBuilder();int position=0;
  foreach(Match match in tokens.Matches(text)){
   output.Append(text[position..match.Index]);
   if(match.Groups[1].Success)output.Append(match.Groups[1].Value);
   else if(match.Groups[2].Success)output.Append(PlainInline(match.Groups[2].Value));
   else{
    string label=PlainInline(match.Groups[3].Value),url=match.Groups[4].Value;
    output.Append(label);
    if(!url.StartsWith('#')&&label!=url)output.Append(" (" ).Append(url).Append(')');
   }
   position=match.Index+match.Length;
  }
  return output.Append(text[position..]).ToString();
 }
 internal static string PlainText(string markdown){
  var lines=new List<string>();var paragraph=new List<string>();int section=0;bool quote=false;
  void Flush(){if(paragraph.Count>0){lines.Add(PlainInline(string.Join(" ",paragraph)));paragraph.Clear();}}
  void Gap(){if(lines.Count>0&&lines[^1].Length>0)lines.Add("");}
  foreach(var raw in markdown.Replace("\r\n","\n").Split('\n')){
   string line=raw.Trim();
   if(line.Length==0){Flush();Gap();quote=false;continue;}
   if(line.StartsWith('#')){
    Flush();Gap();int level=line.TakeWhile(x=>x=='#').Count();
    if(level is <1 or >3||line.Length<=level||line[level]!=' ')throw new InvalidDataException("Unsupported heading: "+line);
    lines.Add((level==2?$"{++section}. ":"")+PlainInline(line[(level+1)..]));Gap();quote=false;continue;
   }
   if(line.StartsWith("> ")){
    if(!quote){Flush();Gap();}paragraph.Add(line[2..]);quote=true;continue;
   }
   if(quote){Flush();Gap();quote=false;}
   if(line.StartsWith("- ")||Regex.IsMatch(line,@"^\d+\.\s+")){Flush();lines.Add(PlainInline(line));continue;}
   paragraph.Add(line);
  }
  Flush();while(lines.Count>0&&lines[^1].Length==0)lines.RemoveAt(lines.Count-1);
  return string.Join("\r\n",lines)+"\r\n";
 }
 internal static string Inline(string text){
  var output=new StringBuilder();int position=0;
  foreach(Match match in tokens.Matches(text)){
   output.Append(Encode(text[position..match.Index]));
   if(match.Groups[1].Success)output.Append("<code>").Append(Encode(match.Groups[1].Value)).Append("</code>");
   else if(match.Groups[2].Success)output.Append("<strong>").Append(Encode(match.Groups[2].Value)).Append("</strong>");
   else{
    string url=match.Groups[4].Value;
    if(!url.StartsWith('#')&&!(Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.Scheme is "https" or "http"))throw new InvalidDataException("Unsupported guide link: "+url);
    output.Append("<a href=\"").Append(Encode(url)).Append("\">").Append(Encode(match.Groups[3].Value)).Append("</a>");
   }
   position=match.Index+match.Length;
  }
  return output.Append(Encode(text[position..])).ToString();
 }
 internal static (string Body,List<Heading> Headings,string Title) Render(string markdown){
  var html=new StringBuilder();var paragraph=new List<string>();var quote=new List<string>();var headings=new List<Heading>();string title="";string? list=null;bool section=false;
  void FlushParagraph(){if(paragraph.Count==0)return;html.Append("<p>").Append(Inline(string.Join(" ",paragraph))).AppendLine("</p>");paragraph.Clear();}
  void CloseList(){if(list==null)return;html.Append("</").Append(list).AppendLine(">");list=null;}
  void FlushQuote(){if(quote.Count==0)return;html.Append("<aside class=\"notice\" role=\"note\"><p>").Append(Inline(string.Join(" ",quote))).AppendLine("</p></aside>");quote.Clear();}
  foreach(var raw in markdown.Replace("\r\n","\n").Split('\n')){
   var line=raw.Trim();
   if(line.Length==0){FlushParagraph();CloseList();FlushQuote();continue;}
   if(line.StartsWith("> ")){FlushParagraph();CloseList();quote.Add(line[2..]);continue;}
   FlushQuote();
   if(line.StartsWith('#')){
    FlushParagraph();CloseList();
    int level=line.TakeWhile(x=>x=='#').Count();
    if(level is <1 or >3||line.Length<=level||line[level]!=' ')throw new InvalidDataException("Unsupported heading: "+line);
    string text=line[(level+1)..];
    if(level==1){if(title.Length>0)throw new InvalidDataException("Duplicate guide title");title=text;html.Append("<h1>").Append(Inline(text)).AppendLine("</h1>");}
    else if(level==2){if(section)html.AppendLine("</section>");section=true;string id=$"section-{headings.Count+1:D2}";headings.Add(new(id,text));html.Append("<section aria-labelledby=\"").Append(id).Append("\"><h2 id=\"").Append(id).Append("\">").Append(Inline(text)).AppendLine("</h2>");}
    else html.Append("<h3>").Append(Inline(text)).AppendLine("</h3>");
    continue;
   }
   bool unordered=line.StartsWith("- ");var ordered=Regex.Match(line,@"^\d+\.\s+(.+)$");
   if(unordered||ordered.Success){
    FlushParagraph();string kind=unordered?"ul":"ol";if(list!=kind){CloseList();list=kind;html.Append('<').Append(kind).AppendLine(">");}
    html.Append("<li>").Append(Inline(unordered?line[2..]:ordered.Groups[1].Value)).AppendLine("</li>");continue;
   }
   CloseList();paragraph.Add(line);
  }
  FlushParagraph();CloseList();FlushQuote();if(section)html.AppendLine("</section>");
  if(title.Length==0)throw new InvalidDataException("Missing guide title");
  return(html.ToString(),headings,title);
 }
}

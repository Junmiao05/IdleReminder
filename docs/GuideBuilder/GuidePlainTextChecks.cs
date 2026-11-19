// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
internal static class GuidePlainTextChecks {
 internal static void Run(){
  int count=0;
  void Check(bool ok,string label){if(!ok)throw new InvalidDataException(label);Console.WriteLine("PASS "+label);count++;}
  Check(GuideMarkdown.PlainInline("Click **Save** and run `IdleReminder.exe`.")=="Click Save and run IdleReminder.exe.","formatting becomes readable text");
  Check(GuideMarkdown.PlainInline("[KDE Connect](https://kdeconnect.kde.org/)")=="KDE Connect (https://kdeconnect.kde.org/)","external link retains name and complete URL");
  Check(GuideMarkdown.PlainInline("[https://example.com/](https://example.com/)")=="https://example.com/","URL label is not duplicated");
  Check(GuideMarkdown.PlainInline("[Quick start](#section-01)")=="Quick start","local HTML anchor is omitted");
  Check(GuideMarkdown.PlainInline("`D:\\Music\\a_b*.mp3` & **`file.exe`**")=="D:\\Music\\a_b*.mp3 & file.exe","literal path characters and nested inline code survive");
  string sample="# Guide\n\nIntro wrapped\nacross source lines.\n\n> **Important:** Save first.\n> Keep the files.\n\n## Start\n\n### Run\n\n1. Run `app.exe`.\n2. Click **Save**.\n\n- Keep data.\n- Keep themes.\n\n## Help\n\nRead [KDE](https://kdeconnect.kde.org/).\n";
  string expected="Guide\r\n\r\nIntro wrapped across source lines.\r\n\r\nImportant: Save first. Keep the files.\r\n\r\n1. Start\r\n\r\nRun\r\n\r\n1. Run app.exe.\r\n2. Click Save.\r\n\r\n- Keep data.\r\n- Keep themes.\r\n\r\n2. Help\r\n\r\nRead KDE (https://kdeconnect.kde.org/).\r\n";
  Check(GuideMarkdown.PlainText(sample)==expected,"headings paragraphs notices and lists retain readable structure with Windows line endings");
  Check(GuideMarkdown.PlainText(sample.Replace("\n","\r\n"))==expected,"input line endings do not affect output");
  Check(GuideMarkdown.PlainText("# 中文指南\n\n**保存设置**后继续。\n")=="中文指南\r\n\r\n保存设置后继续。\r\n","Chinese text is preserved");
  Check(GuideMarkdown.PlainText("# Guide\n\n"+new string('x',400)).Split("\r\n").Contains(new string('x',400)),"long paragraphs are not hard-wrapped");
  Check(GuideVersion.Resolve("unused","0.0.2.0")=="0.0.2.0","build version is preserved including zero revision");
  var annotated=GuideVersion.Annotate("# 中文指南\r\n\r\n用户修改的内容。","0.0.2.0",false);
  Check(GuideMarkdown.PlainText(annotated).Contains("适用版本：IdleReminder v0.0.2.0\r\n"),"Chinese TXT has version below title");
  Check(GuideMarkdown.Render(annotated).Body.Contains("<p>适用版本：IdleReminder v0.0.2.0</p>"),"Chinese HTML has visible version");
  Check(annotated.EndsWith("用户修改的内容。"),"version insertion leaves existing guide content intact");
  Check(GuideVersion.Annotate("# Guide\n\nBody.","0.0.2.0",true).Contains("Applies to IdleReminder v0.0.2.0"),"English version label is localized");
  try{GuideVersion.Resolve("unused","bad-version");Check(false,"invalid version rejected");}catch(InvalidDataException){Check(true,"invalid version rejected");}
  Console.WriteLine($"{count} plain-text checks passed");
 }
}

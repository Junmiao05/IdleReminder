// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class Verification {
 static double Duration(string path){
  using var reader=new BinaryReader(File.OpenRead(path));
  reader.BaseStream.Position=12;int rate=0,size=0;
  while(reader.BaseStream.Position+8<=reader.BaseStream.Length){
   var id=new string(reader.ReadChars(4));int n=reader.ReadInt32();
   var end=reader.BaseStream.Position+n+(n%2);
   if(id=="fmt "){reader.BaseStream.Position+=8;rate=reader.ReadInt32();}
   if(id=="data")size=n;
   reader.BaseStream.Position=end;
  }
  if(rate<=0||size<=0)throw new InvalidDataException("Invalid WAV");
  return size/(double)rate;
 }
 internal static void Run(){
 var names=Presets.Names.Concat(Presets.VoiceNames).ToArray();
 var files=Presets.FileNames.Concat(Presets.VoiceFileNames).ToArray();
 var report=new List<string>();
 using var context=new ApplicationContext();
 using var audio=new Audio();
 using var timer=new System.Windows.Forms.Timer{Interval=100};
 int index=0;long started=0;bool failed=false;double expected=0;
 audio.Failed+=_=>failed=true;
 void Next(){
  var path=Path.Combine(AppContext.BaseDirectory,"assets",files[index]);
  expected=Duration(path);started=Environment.TickCount64;
  audio.Play(new[]{path},0,AudioPurpose.Preview);
 }
 Next();
 timer.Tick+=(_,_)=>{
 audio.HandleInput(unchecked(audio.StartInput+1));
 double elapsed=(Environment.TickCount64-started)/1000.0;
 if(failed||elapsed>expected+8){report.Add($"FAIL {names[index]}");audio.Stop();}
 else if(audio.Playing)return;
 else if(elapsed<expected-0.5)report.Add($"FAIL {names[index]} preview ended prematurely");
 else report.Add($"PASS {names[index]} full {expected:F3}s preview with changing input timestamps (muted)");
 index++;failed=false;
 if(index==files.Length){File.WriteAllLines(Path.Combine(Store.Root,"audio-verification.txt"),report);context.ExitThread();}else Next();
 };
 timer.Start();Application.Run(context);
 }
}


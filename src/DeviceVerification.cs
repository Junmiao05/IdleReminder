// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal static class DeviceVerification {
 internal static void Run(){
  var report=new List<string>();
  try{
   var devices=CoreAudio.List();
   report.Add($"PASS enumerate active render endpoints: {devices.Count}");
   foreach(var d in devices){
    var opened=CoreAudio.Open(d.Id);
    try{
     opened.Volume.GetMasterVolumeLevelScalar(out var volume);opened.Volume.GetMute(out var mute);
     if(volume<0||volume>1)throw new InvalidOperationException();
     // Register/unregister callbacks without changing endpoint volume.
     using var listener=new ReadOnlyCallback(opened.Volume);
     report.Add($"PASS endpoint read + callback registration; speakerEligible={d.Speaker}; muted={mute}");
    }finally{CoreAudio.Release(opened.Volume);CoreAudio.Release(opened.Device);}
   }
   if(devices.Count>0)report.Add(devices.Any(d=>d.Id==CoreAudio.DefaultId())?"PASS default endpoint matched":"FAIL default endpoint mismatch");
  }catch(Exception ex){report.Add("FAIL "+ex.GetType().Name+" "+ex.HResult);}
  File.WriteAllLines(Path.Combine(Store.Root,"device-verification.txt"),report);
 }
 [System.Runtime.InteropServices.ComVisible(true),System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.None)]
 sealed class ReadOnlyCallback : IVolumeCallback,IDisposable{
  readonly IEndpointVolume endpoint;
  internal ReadOnlyCallback(IEndpointVolume endpoint){this.endpoint=endpoint;endpoint.RegisterControlChangeNotify(this);}
  public int OnNotify(IntPtr value)=>0;
  public void Dispose()=>endpoint.UnregisterControlChangeNotify(this);
 }
}



// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class DuckingPolicy {
 public static bool Matches(string path,IEnumerable<string> entries)=>entries.Any(entry=>
  string.Equals(entry.Trim(),path,StringComparison.OrdinalIgnoreCase)||
  string.Equals(entry.Trim(),System.IO.Path.GetFileName(path),StringComparison.OrdinalIgnoreCase));
 public static bool MayAttemptBoost(bool pauseBlocked,bool hasDucking,bool duckingVerified)=>!pauseBlocked||(hasDucking&&duckingVerified);
 public static bool MayIgnoreOtherAudio(bool optedIn,bool verifiedDucking,bool querySucceeded)=>optedIn&&verifiedDucking&&querySucceeded;
 public static float CompensatedTarget(float baseTarget,float gainDb){if(!float.IsFinite(gainDb))throw new ArgumentOutOfRangeException(nameof(gainDb));return baseTarget*(float)Math.Pow(10,-Math.Max(0,gainDb)/20.0);}
 public static bool HasDeviceAuthorization(bool confirmed,bool duckEnabled,bool coordination)=>confirmed&&(!duckEnabled||coordination);
 public static bool PermitAfterDucking(bool deviceConsent,bool ordinaryConsent,bool failed)=>!failed&&(deviceConsent||ordinaryConsent);
 public static float Target(float original,int percent)=>original*Math.Clamp(percent,1,100)/100f;
}
public interface IDuckVolume:IDisposable {
 float Volume {get;set;}
 bool Muted {get;}
 Guid Context {get;}
 event Action<Guid>? Changed;
}
public sealed class DuckingAdjustment:IDisposable {
 readonly IDuckVolume session;readonly float original,baseApplied;float applied;float? restoreStart;int external;bool changed,disposed;
 public float Original=>original;
 public float Applied=>applied;
 public bool Reduced=>changed;
 public bool StillApplied=>!disposed&&changed&&Volatile.Read(ref external)==0&&!session.Muted&&Math.Abs(session.Volume-applied)<.0001f;
 public bool Restored {get;private set;}
 public bool Preserved {get;private set;}
 public DuckingAdjustment(IDuckVolume session,int percent){
  this.session=session;
  try{
   original=session.Volume;applied=baseApplied=DuckingPolicy.Target(original,percent);
   session.Changed+=OnChanged;
   if(session.Muted||original<=0||applied>=original)return;
   // Recheck after registering; do not override a concurrent manual adjustment.
   if(Volatile.Read(ref external)!=0||Math.Abs(session.Volume-original)>.0001f)return;
   changed=true;session.Volume=applied;
  }catch{Dispose();throw;}
 }
  public void RestoreGradually(double progress){
  if(!StillApplied){PreserveCurrent();return;}
  restoreStart??=applied;float p=(float)Math.Clamp(progress,0,1);p=p*p*(3-2*p);
  SetApplied(restoreStart.Value+(original-restoreStart.Value)*p);
 }
 public void PreserveCurrent()=>Interlocked.Exchange(ref external,1);
 public void StageForMasterBoost(){if(changed)SetApplied(0);}
 public void CompensateMasterGain(float gainDb){if(changed)SetApplied(DuckingPolicy.CompensatedTarget(baseApplied,gainDb));}
 public void CancelMasterBoost(){if(changed&&StillApplied)SetApplied(baseApplied);}
 void SetApplied(float target){
  if(!StillApplied)throw new InvalidOperationException("Ducked session changed externally");
  float previous=applied;applied=target;
  try{session.Volume=target;if(!StillApplied)throw new InvalidOperationException("Ducking write could not be verified");}
  catch{applied=previous;throw;}
 }
 void OnChanged(Guid context){if(context!=session.Context)Interlocked.Exchange(ref external,1);}
 public void Dispose(){
  if(disposed)return;disposed=true;
  try{if(changed){if(Volatile.Read(ref external)==0&&!session.Muted&&Math.Abs(session.Volume-applied)<.0001f){session.Volume=original;Restored=Math.Abs(session.Volume-original)<.0001f;if(!Restored)throw new InvalidOperationException("Application volume restore did not match original");}else Preserved=true;}}
  finally{session.Changed-=OnChanged;session.Dispose();}
 }
}


// Per-instance permits: a new stream from the same application gets no exemption.
public sealed class DuckedSessionRegistry {
 readonly Dictionary<string,DuckingAdjustment> sessions=new(StringComparer.Ordinal);
 public bool Contains(string id)=>sessions.ContainsKey(id);
 public void Add(string id,DuckingAdjustment adjustment)=>sessions.Add(id,adjustment);
 public bool IsVerified=>sessions.Values.Any(x=>x.Reduced)&&sessions.Values.Where(x=>x.Reduced).All(x=>x.StillApplied);
 public bool Allows(string id)=>sessions.TryGetValue(id,out var adjustment)&&adjustment.StillApplied;
 public void Clear()=>sessions.Clear();
}

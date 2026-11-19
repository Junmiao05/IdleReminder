using IdleReminder;
internal sealed class FakeDuckVolume:IDuckVolume {
 float volume=.5f;
 public Guid Context {get;}=Guid.NewGuid();
 public event Action<Guid>? Changed;
 public bool Muted {get;set;}
 public int Writes,Disposals;
 public bool FailNextWrite;
 public float Volume {get=>volume;set{if(FailNextWrite){FailNextWrite=false;throw new InvalidOperationException();}Writes++;volume=value;Changed?.Invoke(Context);}}
 public void Manual(float value){volume=value;Changed?.Invoke(Guid.NewGuid());}
 public void Dispose()=>Disposals++;
}

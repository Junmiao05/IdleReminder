using IdleReminder;
internal sealed class FakeMedia : IPausableMedia {
 public string AppId {get;set;}="test.exe";
 public bool IsPlaying {get;set;}=true;
 public bool CanPause {get;set;}=true;
 public int Requests {get;private set;}
 public Func<CancellationToken,Task<bool>> Reply {get;set;}=_=>Task.FromResult(true);
 public Task<bool> PauseAsync(CancellationToken token){Requests++;return Reply(token);}
}


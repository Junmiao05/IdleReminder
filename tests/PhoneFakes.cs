using IdleReminder;
internal sealed class FakePhoneRunner: IPhoneCommandRunner {
 internal readonly List<string[]> Calls=new();
 internal Func<IReadOnlyList<string>,CancellationToken,Task<PhoneCommandResult>> Handler=(args,token)=>Task.FromResult(new PhoneCommandResult(0,"chosen_id 手机一\nother_id 手机二\n"));
 public Task<PhoneCommandResult> RunAsync(string path,IReadOnlyList<string> arguments,CancellationToken token){Calls.Add(arguments.ToArray());return Handler(arguments,token);}
}

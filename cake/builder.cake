//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

public Builder Build => CommandLineOptions.Usage
    ? new Builder(() => Information(HelpMessages.Usage))
    : new Builder(() => RunTarget(CommandLineOptions.Target.Value));

public class Builder
{
    private Action _action;

    public Builder(Action action)
    {
        _action = action;
    }

    public void Run()
    {
        _action();
    }
}

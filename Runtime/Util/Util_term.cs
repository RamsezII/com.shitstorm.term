using _TERM_;

public static partial class Util_term
{
    public static CmdExecution TryCommand(this CmdNamespace.IUser user, in CmdContext context)
    {
        context.reader.TryRead(out string arg0);
        return user.OnTryCommand(arg0, context);
    }
}
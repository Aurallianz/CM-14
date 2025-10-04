using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[Serializable] [NetSerializable]
public enum RMCNukeUIKey
{
    Key,
}

[Serializable] [NetSerializable]
public sealed class RMCNukeWindowSafetyBuiMsg(bool state) : BoundUserInterfaceMessage
{
    public readonly bool State = state;
}

[Serializable] [NetSerializable]
public sealed class RMCNukeWindowCommandBuiMsg(bool state) : BoundUserInterfaceMessage
{
    public readonly bool State = state;
}

[Serializable] [NetSerializable]
public sealed class RMCNukeWindowAnchorBuiMsg(bool state) : BoundUserInterfaceMessage
{
    public readonly bool State = state;
}

[Serializable] [NetSerializable]
public sealed class RMCNukeWindowDecryptionBuiMsg(bool state) : BoundUserInterfaceMessage
{
    public readonly bool State = state;
}

[Serializable] [NetSerializable]
public sealed class RMCNukeWindowNukeBuiMsg() : BoundUserInterfaceMessage;

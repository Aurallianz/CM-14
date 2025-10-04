using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[Serializable, NetSerializable]
public sealed partial class RMCNukeSafetyEvent : SimpleDoAfterEvent
{
    public bool Set;

    public RMCNukeSafetyEvent(bool set)
    {
        Set = set;
    }
}

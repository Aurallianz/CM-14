using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[Serializable, NetSerializable]
public sealed partial class RMCNukeAnchorEvent : SimpleDoAfterEvent
{
    public bool Set;

    public RMCNukeAnchorEvent(bool set)
    {
        Set = set;
    }
}

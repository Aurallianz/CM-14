using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[Serializable, NetSerializable]
public sealed partial class RMCNukeDecryptionEvent : SimpleDoAfterEvent
{
    public bool Set;

    public RMCNukeDecryptionEvent(bool set)
    {
        Set = set;
    }
}

using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[Serializable, NetSerializable]

public enum RMCNukeVisuals
{
    Nuke,
    Deployed,
    Unsafe,
    Timing,
    Activation,
}

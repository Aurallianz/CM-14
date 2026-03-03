using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Synth.RepairStation;

[Serializable, NetSerializable]
public sealed partial class SynthRepairStationEnterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SynthRepairStationRepairEvent : SimpleDoAfterEvent;

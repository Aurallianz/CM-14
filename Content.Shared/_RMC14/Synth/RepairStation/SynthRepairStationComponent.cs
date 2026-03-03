using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth.RepairStation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SynthRepairStationComponent : Component
{
    public string SynthContainer = "RepairStation-SynthContainer";

    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterTime =  new TimeSpan(0, 0, 4);
}

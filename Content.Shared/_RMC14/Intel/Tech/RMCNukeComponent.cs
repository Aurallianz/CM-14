using Content.Shared._RMC14.Communications;
using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Intel.Tech;

[RegisterComponent] [NetworkedComponent] [AutoGenerateComponentState(true)]
public sealed partial class RMCNukeComponent : Component
{

    [DataField, AutoNetworkedField]
    public TimeSpan MaxDecryptionDelay = TimeSpan.FromSeconds(600);

    [DataField, AutoNetworkedField]
    public TimeSpan CurrentDecryptionTime = TimeSpan.FromSeconds(600);

    [DataField, AutoNetworkedField]
    public TimeSpan DecryptionTimeIncrease = TimeSpan.FromSeconds(120);

    [DataField, AutoNetworkedField]
    public TimeSpan ExplosionTime = TimeSpan.FromSeconds(180);

    [DataField, AutoNetworkedField]
    public TimeSpan DecryptAt;

    [DataField, AutoNetworkedField]
    public TimeSpan NukeAt;

    [DataField, AutoNetworkedField]
    public TimeSpan DecryptAnnounceHalfTime = TimeSpan.FromSeconds(300);

    [DataField, AutoNetworkedField]
    public bool DecryptAnnounceHalf = false;

    [DataField, AutoNetworkedField]
    public TimeSpan DecryptAnnounceMinuteTime = TimeSpan.FromSeconds(60);

    [DataField, AutoNetworkedField]
    public bool DecryptAnnounceMinute = false;

    [DataField, AutoNetworkedField]
    public TimeSpan NukeAnnounceHalfTime = TimeSpan.FromSeconds(90);

    [DataField, AutoNetworkedField]
    public bool NukeAnnounceHalf = false;

    [DataField, AutoNetworkedField]
    public TimeSpan NukeAnnounceMinuteTime = TimeSpan.FromSeconds(60);

    [DataField, AutoNetworkedField]
    public bool NukeAnnounceMinute = false;

    [DataField, AutoNetworkedField]
    public TimeSpan NukeAnnounceFifteenTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public bool NukeAnnounceFifteen = false;

    [DataField, AutoNetworkedField]
    public TimeSpan NukeAnnounceTenTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool NukeAnnounceTen = false;

    [DataField, AutoNetworkedField]
    public bool Decrypted;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> TrackedTowers = new();

    [DataField, AutoNetworkedField]
    public int RequiredNumberOfTowers = 2;

    [DataField, AutoNetworkedField]
    public ProtoId<AccessLevelPrototype> NukeAccess = "RMCAccessCommand";

    [DataField, AutoNetworkedField]
    public bool CommandLockout = true;

    [DataField, AutoNetworkedField]
    public bool Safety = true;

    [DataField, AutoNetworkedField]
    public bool Decrypting = false;

    [DataField, AutoNetworkedField]
    public bool Nuking = false;

    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan QueenDoAfter = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public bool UseAble = true;
}

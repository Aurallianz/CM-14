using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared.Access.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Intel.Tech;

public sealed class RMCNukeSystem : EntitySystem
{
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedExplosionSystem _explosions = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;


    private static readonly ProtoId<DamageTypePrototype> DamageTypeBlunt = "Blunt";

    public override void Initialize()
    {
        Subs.BuiEvents<RMCNukeComponent>(RMCNukeUIKey.Key,
            subs =>
            {
                subs.Event<RMCNukeWindowSafetyBuiMsg>(OnNukeSafetyBuiMsg);
                subs.Event<RMCNukeWindowCommandBuiMsg>(OnNukeCommandBuiMsg);
                subs.Event<RMCNukeWindowAnchorBuiMsg>(OnNukeAnchorBuiMsg);
                subs.Event<RMCNukeWindowDecryptionBuiMsg>(OnNukeDecryptionBuiMsg);
                subs.Event<RMCNukeWindowNukeBuiMsg>(OnNukeNukeBuiMsg);
            });

        SubscribeLocalEvent<CommunicationsTowerComponent, CommunicationsTowerStateChangedEvent>(OnCommunicationsTowerStateChanged);
        SubscribeLocalEvent<RMCNukeComponent, ActivateInWorldEvent>(OnNukeActivateInWorld);
        // SubscribeLocalEvent<RMCNukeComponent, MapInitEvent>(OnNukeMapInit);
        // SubscribeLocalEvent<CommunicationsTowerComponent, ComponentRemove>(TowerClear);
        // SubscribeLocalEvent<CommunicationsTowerComponent, EntityTerminatingEvent>(TowerClear);
        SubscribeLocalEvent<RMCNukedComponent, UpdateMobStateEvent>(OnHasNukedUpdateMobState, after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<RMCNukeComponent, RMCNukeQueenEvent>(QueenDoAfter);
        SubscribeLocalEvent<RMCNukeComponent, RMCNukeSafetyEvent>(SafetyDoAfter);
        SubscribeLocalEvent<RMCNukeComponent, RMCNukeAnchorEvent>(AnchorDoAfter);
        SubscribeLocalEvent<RMCNukeComponent, RMCNukeDecryptionEvent>(DecryptionDoAfter);
        SubscribeLocalEvent<RMCNukeComponent, RMCNukeNukeEvent>(NukeDoAfter);
    }

    private void QueenDoAfter(Entity<RMCNukeComponent> ent, ref RMCNukeQueenEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Nuking)
        {
            ent.Comp.Nuking = false;
            // Gods Favorite Error.
            _marineAnnounce.AnnounceHighCommand($"ERROR. \n \nNUCLEAR EXPLOSIVE FAILURE, ERROR: 418, Solution: Reactivate Nuclear Ordnance.",  "ARES V3.2 Ordnance Manager");
            _xenoAnnounce.AnnounceQueenMother("The hive killer has been deactivated, you must stop them from reactivating it!");
        }

        if (ent.Comp.Decrypting || ent.Comp.CurrentDecryptionTime != ent.Comp.MaxDecryptionDelay)
        {
            ent.Comp.Decrypting = false;
            ent.Comp.CurrentDecryptionTime = ent.Comp.MaxDecryptionDelay;
            _marineAnnounce.AnnounceHighCommand($"ERROR. \n \nNUCLEAR EXPLOSIVE FAILURE, ERROR: 420, Solution: Reactivate Nuclear Decryption.",  "ARES V3.2 Ordnance Manager");
            _xenoAnnounce.AnnounceQueenMother("The hive killer has been deactivated, you must stop them from reactivating it!");
        }
    }

    private void OnNukeActivateInWorld(Entity<RMCNukeComponent> ent, ref ActivateInWorldEvent args)
    {
        //ToDO RMC14 Add human defuse?
        if (!ent.Comp.UseAble)
        {
            _popup.PopupClient($"You probably shouldn't touch that!", args.User, PopupType.MediumCaution);
            return;
        }
        if (!HasComp<XenoComponent>(args.User))
            return;

        if (!HasComp<DropshipHijackerComponent>(args.User))
        {
            _popup.PopupClient($"You stare cluelessly at the {Name(ent.Owner)}, maybe the queen would know better?", ent, args.User, PopupType.LargeCaution);
            return;
        }

        if (ent.Comp is { Nuking: false, Decrypting: false } && ent.Comp.CurrentDecryptionTime == ent.Comp.MaxDecryptionDelay)
        {
            _popup.PopupClient($"We have done everything we can with this, we better move on.", args.User, PopupType.MediumCaution);
            return;
        }

        var ev = new RMCNukeQueenEvent();
        var doAfterArg = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.QueenDoAfter,
            ev,
            ent,
            ent,
            ent
        )
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfterArg);
    }

    private bool AuthenticationCheck(RMCNukeComponent nuke, EntityUid user)
    {
        return (_idCard.TryFindIdCard(user, out var idCard) && _access.TryGetTags(idCard) is { } tags &&
            tags.Contains(nuke.NukeAccess));
        // nuke.CommandLockout
    }

    private bool AnchorCheck(EntityUid ent)
    {
        return TryComp(ent, out TransformComponent? transform) && transform.Anchored;
    }

    private void OnNukeSafetyBuiMsg(Entity<RMCNukeComponent> ent, ref RMCNukeWindowSafetyBuiMsg args)
    {
        if (!AuthenticationCheck(ent, args.Actor) && ent.Comp.CommandLockout)
            return;
        if (!NukeSafetyCheck(ent, args.Actor))
            return;

        var ev = new RMCNukeSafetyEvent(args.State);
        var doAfterArg = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.QueenDoAfter,
            ev,
            ent,
            ent,
            ent
        )
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfterArg);
    }

    private bool NukeSafetyCheck(Entity<RMCNukeComponent> ent, EntityUid user)
    {
        if (!ent.Comp.UseAble)
        {
            _popup.PopupClient($"You probably shouldn't touch that!", user, PopupType.MediumCaution);
            return false;
        }
        if (!AnchorCheck(ent))
        {
            _popup.PopupClient($"You cannot change the safety while the nuke is unanchored!", user, PopupType.MediumCaution);
            return false;
        }
        if (ent.Comp.Decrypting)
        {
            _popup.PopupClient($"You cannot disable safety during decryption!", user, PopupType.MediumCaution);
            return false;
        }
        if (ent.Comp.Nuking)
        {
            _popup.PopupClient($"You cannot disable safety while the nuke is armed...", user, PopupType.MediumCaution);
            return false;
        }
        return true;
    }

    private void SafetyDoAfter(Entity<RMCNukeComponent> ent, ref RMCNukeSafetyEvent args)
    {
        if (args.Cancelled)
            return;
        if (!NukeSafetyCheck(ent, args.User))
            return;

        ent.Comp.Safety = !args.Set;
        Dirty(ent);
    }

    private void OnNukeCommandBuiMsg(Entity<RMCNukeComponent> ent, ref RMCNukeWindowCommandBuiMsg args)
    {
        if (!AuthenticationCheck(ent, args.Actor))
            return;
        if (!ent.Comp.UseAble)
        {
            _popup.PopupClient($"You probably shouldn't touch that!", args.Actor, PopupType.MediumCaution);
            return;
        }
        ent.Comp.CommandLockout = !args.State;
        Dirty(ent);
    }

    private void OnNukeAnchorBuiMsg(Entity<RMCNukeComponent> ent, ref RMCNukeWindowAnchorBuiMsg args)
    {
        if (!AuthenticationCheck(ent, args.Actor) && ent.Comp.CommandLockout)
            return;
        if(!NukeAnchorCheck(ent, args.Actor))
            return;

        var ev = new RMCNukeAnchorEvent(args.State);
        var doAfterArg = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.QueenDoAfter,
            ev,
            ent,
            ent,
            ent
        )
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfterArg);
    }

    private bool NukeAnchorCheck(Entity<RMCNukeComponent> ent,  EntityUid user)
    {
        if (!ent.Comp.UseAble)
        {
            _popup.PopupClient($"You probably shouldn't touch that!", user, PopupType.MediumCaution);
            return false;
        }
        if (!_rmcPlanet.IsOnPlanet(ent.Owner.ToCoordinates()) || _dropship.IsOnDropship(ent))
        {
            _popup.PopupClient($"You cannot anchor this here!", user, PopupType.MediumCaution);
            return false;
        }
        if (ent.Comp.Nuking)
        {
            _popup.PopupClient($"You cannot unanchor while the nuke is armed...", user, PopupType.MediumCaution);
            return false;
        }
        if (ent.Comp.Decrypting)
        {
            _popup.PopupClient($"You cannot unanchor during decryption!", user, PopupType.MediumCaution);
            return false;
        }
        if (!ent.Comp.Safety)
        {
            _popup.PopupClient($"You cannot unanchor while the safety is disabled", user, PopupType.MediumCaution);
            return false;
        }
        return true;
    }

    private void AnchorDoAfter(Entity<RMCNukeComponent> ent, ref RMCNukeAnchorEvent args)
    {
        if(!NukeAnchorCheck(ent, args.User))
            return;
        switch (args.Set)
        {
            case true:
                _transform.Unanchor(ent);
                break;
            case false:
                _transform.AnchorEntity(ent);
                break;
        }
        Dirty(ent);
    }

    private void OnNukeDecryptionBuiMsg(Entity<RMCNukeComponent> ent, ref RMCNukeWindowDecryptionBuiMsg args)
    {
        if (!AuthenticationCheck(ent, args.Actor) && ent.Comp.CommandLockout)
            return;
        if (!NukeDecryptionCheck(ent, args.Actor, args.State))
            return;

        var ev = new RMCNukeDecryptionEvent(args.State);
        var doAfterArg = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.QueenDoAfter,
            ev,
            ent,
            ent,
            ent
        )
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfterArg);
    }

    private bool NukeDecryptionCheck(Entity<RMCNukeComponent> ent, EntityUid user, bool set)
    {
        if (!ent.Comp.UseAble)
        {
            _popup.PopupClient($"You probably shouldn't touch that!", user, PopupType.MediumCaution);
            return false;
        }
        if (ent.Comp.Decrypted || ent.Comp.Decrypting != set)
            return false;
        if (ent.Comp.Safety)
        {
            _popup.PopupClient($"You cannot decrypt while the safety is enabled", user, PopupType.MediumCaution);
            return false;
        }
        if (!AnchorCheck(ent))
        {
            _popup.PopupClient($"You cannot decrypt while the nuke is unanchored!", user, PopupType.MediumCaution);
            return false;
        }

        var towerCounter = 0;
        // foreach (var tower in ent.Comp.TrackedTowers)
        // {
        //     if (!TryComp(tower, out CommunicationsTowerComponent? towerComp))
        //         continue;
        //     if (towerComp.State == CommunicationsTowerState.On)
        //         towerCounter++;
        // }
        var query = EntityQueryEnumerator<CommunicationsTowerComponent>();
        while (query.MoveNext(out var uid, out var towerComp))
        {
            if(!_rmcPlanet.IsOnPlanet(uid.ToCoordinates()))
                continue;
            if (towerComp.State == CommunicationsTowerState.On)
                towerCounter++;
        }
        if (!(towerCounter >= ent.Comp.RequiredNumberOfTowers))
        {
            _popup.PopupClient($"You require {ent.Comp.RequiredNumberOfTowers} communications towers to decrypt!", user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private void DecryptionDoAfter(Entity<RMCNukeComponent> ent, ref RMCNukeDecryptionEvent args)
    {
        if (!NukeDecryptionCheck(ent, args.User, args.Set))
            return;
        switch (args.Set)
        {
            case false:
                DecryptionEnable(ent, args);
                break;
            case true:
                DecryptionDisable(ent, args);
                break;
        }
        Dirty(ent);
    }

    private void DecryptionEnable(Entity<RMCNukeComponent> ent, RMCNukeDecryptionEvent args)
    {
        ent.Comp.DecryptAt = _timing.CurTime + ent.Comp.CurrentDecryptionTime;
        ent.Comp.Decrypting = true;
        _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE DECRYPTION STARTED.",  "ARES V3.2 Ordnance Manager");
        var areaName = _area.TryGetArea(ent, out var area, out _) ? Name(area.Value) : "Unknown";
        _xenoAnnounce.AnnounceQueenMother($"A hive killer's initial phase has started at {areaName}, disable one of their communication towers to stop it!");
    }

    private void DecryptionDisable(Entity<RMCNukeComponent> ent, RMCNukeDecryptionEvent args)
    {
        StopDecryption(ent.Owner, ent.Comp);
        _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE DECRYPTION HALTED.",  "ARES V3.2 Ordnance Manager");
        _xenoAnnounce.AnnounceQueenMother("The hive killer's initial phase has been stopped!");
    }

    private void OnNukeNukeBuiMsg(Entity<RMCNukeComponent> ent, ref RMCNukeWindowNukeBuiMsg args)
    {
        if (!AuthenticationCheck(ent, args.Actor) && ent.Comp.CommandLockout)
            return;
        if (!NukeNukeCheck(ent, args.Actor))
            return;

        var ev = new RMCNukeNukeEvent();
        var doAfterArg = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.QueenDoAfter,
            ev,
            ent,
            ent,
            ent
        )
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfterArg);
    }

    private bool NukeNukeCheck(Entity<RMCNukeComponent> ent, EntityUid user)
    {
        if (!ent.Comp.UseAble)
        {
            _popup.PopupClient($"You probably shouldn't touch that!", user, PopupType.MediumCaution);
            return false;
        }
        if (!ent.Comp.Decrypted)
        {
            _popup.PopupClient($"You cannot nuke until its been decrypted", user, PopupType.MediumCaution);
            return false;
        }

        if (ent.Comp.Nuking)
        {
            _popup.PopupClient($"You cannot nuke it's already doing that... Run?", user, PopupType.MediumCaution);
            return false;
        }
        if (!AnchorCheck(ent))
        {
            _popup.PopupClient($"You cannot nuke while the nuke is unanchored!", user, PopupType.MediumCaution);
            return false;
        }
        return true;
    }
    private void NukeDoAfter(Entity<RMCNukeComponent> ent, ref RMCNukeNukeEvent args)
    {
        if (!NukeNukeCheck(ent, args.User))
            return;

        ent.Comp.Nuking = true;
        _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE DETONATION HAS COMMENCED.",  "ARES V3.2 Ordnance Manager");
        var areaName = _area.TryGetArea(ent, out var area, out _) ? Name(area.Value) : "Unknown";
        _xenoAnnounce.AnnounceQueenMother($"The hive killer's final phase has started at {areaName}!");
        ent.Comp.NukeAt = _timing.CurTime + ent.Comp.ExplosionTime;
        Dirty(ent);
    }

    private void OnCommunicationsTowerStateChanged(Entity<CommunicationsTowerComponent> ent, ref CommunicationsTowerStateChangedEvent args)
    {
        if(!_rmcPlanet.IsOnPlanet(ent.Owner.ToCoordinates()))
            return;
        var query = EntityQueryEnumerator<RMCNukeComponent>();

        while (query.MoveNext(out var uid, out var nukeComp))
        {
            if (!_rmcPlanet.IsOnPlanet(uid.ToCoordinates()) || !nukeComp.Decrypted || !nukeComp.UseAble)
                continue;

            // nukeComp.TrackedTowers.Add(ent);

            switch (ent.Comp.State)
            {
                case CommunicationsTowerState.Broken:
                    if (nukeComp.Decrypting)
                        StopDecryption(uid, nukeComp);

                    if (nukeComp.CurrentDecryptionTime != nukeComp.MaxDecryptionDelay)
                    {
                        _marineAnnounce.AnnounceHighCommand(
                            $"ALERT. \n \nNUCLEAR EXPLOSIVE DECRYPTION HALTED. \n \nUnexpected decryption shutdown has led to data loss.",
                            "ARES V3.2 Ordnance Manager");
                        _xenoAnnounce.AnnounceQueenMother("The hive killer's initial phase has been halted! Rejoice!");
                        nukeComp.CurrentDecryptionTime += nukeComp.DecryptionTimeIncrease;
                        if (nukeComp.CurrentDecryptionTime > nukeComp.MaxDecryptionDelay)
                            nukeComp.CurrentDecryptionTime = nukeComp.MaxDecryptionDelay;
                    }
                    break;
                case CommunicationsTowerState.Off:
                    if(!nukeComp.Decrypting)
                        return;
                    StopDecryption(uid, nukeComp);
                    _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE DECRYPTION HALTED.",  "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother("The hive killer's initial phase has been stopped!");
                    break;
                case CommunicationsTowerState.On:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Dirty(uid, nukeComp);
        }
    }

    private void StopDecryption(EntityUid ent, RMCNukeComponent nukeComp)
    {
        nukeComp.CurrentDecryptionTime = (nukeComp.DecryptAt - _timing.CurTime);
        nukeComp.Decrypting = false;
    }

    // private void OnNukeMapInit(Entity<RMCNukeComponent> ent, ref MapInitEvent args)
    // {
    //     var query = EntityQueryEnumerator<CommunicationsTowerComponent>();
    //
    //     while (query.MoveNext(out var uid, out var towerComp))
    //     {
    //         if(!_rmcPlanet.IsOnPlanet(uid.ToCoordinates()))
    //             continue;
    //
    //         ent.Comp.TrackedTowers.Add(uid);
    //     }
    // }

    // private void TowerClear<T>(Entity<CommunicationsTowerComponent> ent, ref T args)
    // {
    //     var query = EntityQueryEnumerator<RMCNukeComponent>();
    //
    //     while (query.MoveNext(out var uid, out var nukeComp))
    //     {
    //         nukeComp.TrackedTowers.Remove(ent);
    //         Dirty(uid, nukeComp);
    //     }
    // }

    private void Nuked(EntityUid uid, RMCNukeComponent nukeComp)
    {
        var queryMob = EntityQueryEnumerator<MobStateComponent>();
        while (queryMob.MoveNext(out var mobUid, out var mobStateComp))
        {
            if(!_rmcPlanet.IsOnPlanet(mobUid.ToCoordinates()))
                continue;
            var damage = new DamageSpecifier()
            {
                DamageDict =
                {
                    [DamageTypeBlunt] = 200000,
                },
            };

            _damageable.TryChangeDamage(mobUid, damage);
            _mobState.ChangeMobState(mobUid, MobState.Dead);
            _unrevivable.MakeUnrevivable(mobUid);
            EnsureComp(mobUid, out RMCNukedComponent component);
        }

        var queryPylon = EntityQueryEnumerator<HivePylonComponent>();
        while (queryPylon.MoveNext(out var pylonUid, out var pylonComp))
        {
            if(!_rmcPlanet.IsOnPlanet(pylonUid.ToCoordinates()))
                continue;
            QueueDel(pylonUid);
        }

        var queryHive = EntityQueryEnumerator<HiveCoreComponent>();
        while (queryHive.MoveNext(out var coreUid, out var coreComp))
        {
            if(!_rmcPlanet.IsOnPlanet(coreUid.ToCoordinates()))
                continue;
            QueueDel(coreUid);
        }

        var queryEgg = EntityQueryEnumerator<XenoEggComponent>();
        while (queryEgg.MoveNext(out var eggUid, out var eggComp))
        {
            if(!_rmcPlanet.IsOnPlanet(eggUid.ToCoordinates()))
                continue;
            QueueDel(eggUid);
        }

        _rmcExplosion.TriggerExplosive(uid, true, radius: 160);

        nukeComp.Nuking = false;
        QueueDel(uid);
    }

    private void OnHasNukedUpdateMobState(Entity<RMCNukedComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<RMCNukeComponent>();

        while (query.MoveNext(out var uid, out var nukeComp))
        {
            if (nukeComp.Decrypting)
            {
                if (!nukeComp.DecryptAnnounceHalf && nukeComp.DecryptAt-time <= nukeComp.DecryptAnnounceHalfTime)
                {
                    nukeComp.DecryptAnnounceHalf = true;
                    _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE HALFWAY TO DECRYPTION COMPLETE.",  "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother($"A hive killer's initial phase is halfway to completion.");
                    Dirty(uid, nukeComp);
                }
                else if(!nukeComp.DecryptAnnounceMinute && nukeComp.DecryptAt-time <= nukeComp.DecryptAnnounceMinuteTime)
                {
                    nukeComp.DecryptAnnounceMinute = true;
                    _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE ONE MINUTE TO DECRYPTION COMPLETE.",  "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother($"A hive killer's initial phase is one minute to completion.");
                    Dirty(uid, nukeComp);
                }
                else if (time >= nukeComp.DecryptAt)
                {
                    nukeComp.Decrypted = true;
                    nukeComp.Decrypting = false;
                    _marineAnnounce.AnnounceHighCommand($"ALERT. \n \nNUCLEAR EXPLOSIVE DECRYPTION COMPLETE.",  "ARES V3.2 Ordnance Manager");
                    var areaName = _area.TryGetArea(uid, out var area, out _) ? Name(area.Value) : "Unknown";
                    _xenoAnnounce.AnnounceQueenMother($"A hive killer's initial phase has completed at {areaName}! You must stop it at all costs!");
                    Dirty(uid, nukeComp);
                }
            }
            //Add announcemount timers

            if (nukeComp.Nuking)
            {
                if (!nukeComp.NukeAnnounceHalf && nukeComp.NukeAt-time <= nukeComp.NukeAnnounceHalfTime)
                {
                    _marineAnnounce.AnnounceHighCommand($"WARNING. \n \nNUCLEAR EXPLOSIVE HALFWAY TO DETONATION.",  "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother($"The end is approaching... the hive killer is halfway complete.");
                    nukeComp.NukeAnnounceHalf = true;
                    Dirty(uid, nukeComp);
                }
                else if (!nukeComp.NukeAnnounceMinute && nukeComp.NukeAt-time <= nukeComp.NukeAnnounceMinuteTime)
                {
                    _marineAnnounce.AnnounceHighCommand($"WARNING. \n \nNUCLEAR EXPLOSIVE ONE MINUTE TO DETONATION.",  "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother($"The Hive killer is about to trigger, END THIS!");
                    nukeComp.NukeAnnounceMinute = true;
                    Dirty(uid, nukeComp);
                }
                else if (!nukeComp.NukeAnnounceFifteen && nukeComp.NukeAt-time <= nukeComp.NukeAnnounceFifteenTime)
                {
                    RaiseLocalEvent(new RMCNukeAudioEvent());
                    nukeComp.NukeAnnounceFifteen = true;
                    Dirty(uid, nukeComp);
                }
                else if (!nukeComp.NukeAnnounceTen && nukeComp.NukeAt-time <= nukeComp.NukeAnnounceTenTime)
                {
                    _marineAnnounce.AnnounceHighCommand($"WARNING. \n \nNUCLEAR EXPLOSIVE TEN SECONDS TO DETONATION.",  "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother($"DISABLE IT! NOW!");
                    nukeComp.NukeAnnounceTen = true;
                    Dirty(uid, nukeComp);
                }
                else if (time >= nukeComp.NukeAt)
                {
                    _marineAnnounce.AnnounceHighCommand($"WARNING. \n \nNUCLEAR DETONATION HAS OCCURED.",
                        "ARES V3.2 Ordnance Manager");
                    _xenoAnnounce.AnnounceQueenMother($"Goodbye my young hive.");
                    Nuked(uid, nukeComp);
                }
            }
        }
    }
}

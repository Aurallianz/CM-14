using Content.Shared._RMC14.Power;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Synth.RepairStation;

public sealed class SynthRepairStationSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly SharedSynthSystem _synth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthRepairStationComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<SynthRepairStationComponent, SynthRepairStationEnterEvent>(OnDoAfterEnter);
        SubscribeLocalEvent<SynthRepairStationComponent, SynthRepairStationRepairEvent>(OnDoAfterRepair);
    }

    private void OnDoAfterRepair(Entity<SynthRepairStationComponent> ent, ref SynthRepairStationRepairEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            args.Cancelled ||
            args.Target is not { } target||
            !TryComp(user, out SynthComponent? synthComponent) ||
            !TryComp(args.Target, out SynthRepairStationComponent? repComp))
        {
            return;
        }

        if (synthComponent.WelderDamageToRepair != null)
            _damageable.TryChangeDamage(user, synthComponent.WelderDamageToRepair, true, false, origin: user);

        if (synthComponent.CableCoilDamageToRepair != null)
            _damageable.TryChangeDamage(user, synthComponent.CableCoilDamageToRepair, true, false, origin: user);

        if (!_power.IsPowered(ent))
        {
            var container = _container.GetContainer(args.Target.Value, repComp.SynthContainer);
            _container.EmptyContainer(container);
            return;
        }

        if (_synth.HasAnyDamage((user, synthComponent)))
            args.Repeat = true;
        else
        {
            var container = _container.GetContainer(args.Target.Value, repComp.SynthContainer);
            _container.EmptyContainer(container);
        }
    }

    private void OnDoAfterEnter(Entity<SynthRepairStationComponent> ent, ref SynthRepairStationEnterEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            args.Cancelled ||
            args.Target is not { } target||
            !TryComp(user, out SynthComponent? synthComponent) ||
            !TryComp(args.Target, out SynthRepairStationComponent? repComp))
        {
            return;
        }

        args.Handled = true;

        if (!_power.IsPowered(ent))
            return;

        if (!_synth.HasAnyDamage((user, synthComponent)))
            return;

        var container = _container.GetContainer(args.Target.Value, repComp.SynthContainer);
        if(!_container.Insert(user, container))
            return;

        var ev = new SynthRepairStationRepairEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterTime, ev, ent, ent)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            MovementThreshold = 0.5f,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnInteract(Entity<SynthRepairStationComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp(args.User, out SynthComponent? synthComponent))
            return;

        if (!_power.IsPowered(ent))
            return;

        if (!_synth.HasAnyDamage((args.User, synthComponent)))
            return;

        var ev = new SynthRepairStationEnterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterTime, ev, ent, ent)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            MovementThreshold = 0.5f,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }
}

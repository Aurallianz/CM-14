using Content.Shared._RMC14.Intel.Tech;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Timing;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Marines.Nuke;

public sealed class NukeUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCNukeComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<RMCNukeComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCNukeComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnState(Entity<RMCNukeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_timing.CurTick != _timing.LastRealTick)
            return;

        RefreshUIs(ent);
    }

    private void OnInserted(Entity<RMCNukeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshUIs(ent);
    }

    private void OnRemoved(Entity<RMCNukeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshUIs(ent);
    }

    private void RefreshUIs(Entity<RMCNukeComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCNukeBui>(ent.Owner);
    }
}

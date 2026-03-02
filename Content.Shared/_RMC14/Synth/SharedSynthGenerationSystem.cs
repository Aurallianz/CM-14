using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

public sealed class SharedSynthGenerationSystem : EntitySystem
{
    private static readonly EntProtoId<SynthGenerationComponent> DefaultGen= "RMCSynthGenTwo";

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;

    private readonly ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthGenerationComponent, GenerationSelectActionEvent>(OnGenerationSelectAction);
        SubscribeLocalEvent<SynthGenerationComponent, GenerationSelectedActionEvent>(OnGenerationSelectedAction);
    }

    public void SynthStartup(Entity<SynthComponent> ent)
    {
        EnsureComp(ent, out SynthGenerationComponent comp);

        if (comp.Generation != null)
            return;
        _actions.AddAction(ent, ref comp.SelectGenerationActionEntity, comp.GenerationAction);

        Dirty(ent.Owner, comp);
        GenerationPopup((ent.Owner, comp));
    }

    private void OnGenerationSelectAction(Entity<SynthGenerationComponent> ent, ref GenerationSelectActionEvent args)
    {
        GenerationPopup(ent);
    }

    private void GenerationPopup(Entity<SynthGenerationComponent> ent)
    {
        if (_net.IsClient)
            return;

        var options = new List<DialogOption>();
        HashSet<EntProtoId<SynthGenerationComponent>> synthTypes = [];

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.HasComponent<SynthGenerationComponent>())
            {
                synthTypes.Add(proto.ID);
                continue;
            }
        }

        foreach (var synth in synthTypes)
        {
            if(!_prototype.TryIndex(synth, out var proto))
                continue;
            options.Add(new DialogOption($"{proto.Name}", new GenerationSelectedActionEvent(synth)));
        }

        _dialog.OpenOptions(ent.Owner, "Select a Generation", options, "Available Generations");
    }

    private void OnGenerationSelectedAction(Entity<SynthGenerationComponent> ent, ref GenerationSelectedActionEvent args)
    {
        if (ent.Comp.Generation != null)
            return;

        if (!_prototype.TryIndex(args.Generation, out var proto))
        {
            _sawmill.Log(LogLevel.Warning, "attempting to index Entity prototype failed");
            return;
        }

        EntityManager.AddComponents(ent, proto);
        _actions.RemoveAction(ent.Owner, ent.Comp.SelectGenerationActionEntity);
    }
}

using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

public sealed class SharedSynthGenerationSystem : EntitySystem
{
    private static readonly EntProtoId<SynthGenerationComponent> DefaultGen= "RMCSynthGenOne";

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private readonly ISawmill _sawmill = default!;


    public override void Initialize()
    {
        base.Initialize();

    }

    public void OnGenerationUpdate(EntityUid ent)
    {
        if (!TryComp(ent, out SynthComponent? synthComp))
            return;

        synthComp.Generation ??= DefaultGen;

        if (!_prototype.TryIndex(synthComp.Generation, out var generation))
        {
            _sawmill.Log(LogLevel.Warning, "attempting to Index Synthetic generation failed");
            return;
        }

        EntityManager.AddComponents(ent, generation.Components);
    }
}

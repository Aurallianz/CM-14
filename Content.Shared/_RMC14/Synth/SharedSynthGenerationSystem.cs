using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

public sealed class SharedSynthGenerationSystem : EntitySystem
{
    private static readonly EntProtoId<SynthGenerationComponent> DefaultGen= "RMCSynthGenTwo";

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private readonly ISawmill _sawmill = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthGenerationComponent, MapInitEvent>(OnGenerationUpdate);
    }

    private void OnGenerationUpdate(Entity<SynthGenerationComponent> ent, ref MapInitEvent args)
    {
    }
}

using Content.Server.Administration;
using Content.Shared._RMC14.Synth;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin.Utility;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
internal sealed class TestCommand : ToolshedCommand
{
    private SharedSynthGenerationSystem? _synth;

    [CommandImplementation]
    public void MoveSpeed([PipedArgument] IEnumerable<EntityUid> input)
    {
        _synth ??= Sys<SharedSynthGenerationSystem>();

        foreach (var entity in input)
        {
            _synth.OnGenerationUpdate(entity);
        }
    }
}

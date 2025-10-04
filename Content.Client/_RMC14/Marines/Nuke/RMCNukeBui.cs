using System.Linq;
using Content.Client.Access;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Marines.Nuke;

public sealed class RMCNukeBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    private readonly IdCardSystem _idCard;
    private readonly AccessSystem _access;

    public RMCNukeBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _idCard = _entities.System<IdCardSystem>();
        _access = _entities.System<AccessSystem>();
    }

    private RMCNukeWindow? _window;


    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCNukeWindow>();

        if (!EntMan.TryGetComponent(Owner, out RMCNukeComponent? nuke))
            return;

        Refresh();

        _window.Safety.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCNukeWindowSafetyBuiMsg(_window.Safety.Status));
            Refresh();
        };

        _window.Command.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCNukeWindowCommandBuiMsg(_window.Command.Status));
            Refresh();
        };

        _window.Anchor.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCNukeWindowAnchorBuiMsg(_window.Anchor.Status));
            Refresh();
        };

        _window.Decryption.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCNukeWindowDecryptionBuiMsg(_window.Decryption.Status));
            Refresh();
        };

        _window.Nuke.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCNukeWindowNukeBuiMsg());
            Refresh();
        };
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCNukeComponent? nuke) || !EntMan.TryGetComponent(Owner, out TransformComponent? transform))
            return;

        if (!AuthenticationCheck(nuke))
        {
            _window.ButtonBox.Visible = false;
            _window.NukeTimer.Visible = false;
            _window.NoAccess.Visible = true;
            return;
        }

        UpdateTimer(nuke);
        UpdateSafety(nuke);
        UpdateCommand(nuke);
        UpdateAnchor(transform);
        UpdateDecrypt(nuke);
        UpdateNuke(nuke);
    }

    private bool AuthenticationCheck(RMCNukeComponent nuke)
    {
        return !nuke.CommandLockout || (_player.LocalSession is { AttachedEntity: not null } &&
               _idCard.TryFindIdCard(_player.LocalSession.AttachedEntity.Value, out var card) &&
               _access.TryGetTags(card) is { } tags && tags.Contains(
                   nuke.NukeAccess));
    }

        private bool AuthenticationCheckIgnoreCommandLockout(RMCNukeComponent nuke)
    {
        return (_player.LocalSession is { AttachedEntity: not null } &&
               _idCard.TryFindIdCard(_player.LocalSession.AttachedEntity.Value, out var card) &&
               _access.TryGetTags(card) is { } tags && tags.Contains(
                   nuke.NukeAccess));
    }

    private void UpdateTimer(RMCNukeComponent nuke)
    {
        if (_window is not { IsOpen: true })
            return;

        if (nuke.Nuking)
        {
            _window.Time = nuke.NukeAt;
            _window.NukingTime = true;
            _window.Ticking = true;
        }
        else if (nuke.Decrypted)
        {
            _window.Time = nuke.ExplosionTime;
            _window.NukingTime = true;
            _window.Ticking = false;
        }
        else if (nuke.Decrypting)
        {
            _window.Time = nuke.DecryptAt;
            _window.NukingTime = false;
            _window.Ticking = true;
        }
        else
        {
            _window.Time = nuke.CurrentDecryptionTime;
            _window.NukingTime = false;
            _window.Ticking = false;
        }

        TimerMessageChange();
    }

    private void TimerMessageChange()
    {
        if (_window is not { IsOpen: true })
            return;

        var displayTime = _window.Time;

        if (_window.Ticking)
            displayTime = _window.Time - _timing.CurTime;

        _window.NukeTimer.Text = _window.NukingTime ? $"[bold]Detonation Timer: {displayTime:mm\\:ss} Minutes" : $"[bold]Decryption Timer: {displayTime:mm\\:ss} Minutes";
    }

    private void UpdateSafety(RMCNukeComponent nuke)
    {
        if (_window is not { IsOpen: true })
            return;

        switch (nuke.Safety)
        {
            case true:
                _window.Safety.Button.Text = "Safety Lockout (Enabled)";
                _window.Safety.Status = true;
                break;
            case false:
                _window.Safety.Button.Text = "Safety Lockout (Disabled)";
                _window.Safety.Status = false;
                break;
        }
    }
    private void UpdateCommand(RMCNukeComponent nuke)
    {
        if (_window is not { IsOpen: true })
            return;

        switch (nuke.CommandLockout)
        {
            case true:
                _window.Command.Button.Text = "Command Lockout (Enabled)";
                _window.Command.Status = true;
                break;
            case false:
                _window.Command.Button.Text = "Command Lockout (Disabled)";
                _window.Command.Status = false;
                break;
        }

        if (!AuthenticationCheckIgnoreCommandLockout(nuke))
            _window.Command.Button.Disabled = true;

    }

    private void UpdateAnchor(TransformComponent transform)
    {
        if (_window is not { IsOpen: true })
            return;

        switch (transform.Anchored)
        {
            case true:
                _window.Anchor.Button.Text = "Anchor (Enabled)";
                _window.Anchor.Status = true;
                break;
            case false:
                _window.Anchor.Button.Text = "Anchor (Disabled)";
                _window.Anchor.Status = false;
                break;
        }
    }
    private void UpdateDecrypt(RMCNukeComponent nuke)
    {
        if (_window is not { IsOpen: true })
            return;

        switch (nuke.Decrypting)
        {
            case true:
                _window.Decryption.Button.Text = "Decryption (Enabled)";
                _window.Decryption.Button.Modulate = Color.Maroon;
                _window.Decryption.Status = true;
                break;
            case false:
                _window.Decryption.Button.Text = "Decryption (Disabled)";
                _window.Decryption.Button.Modulate = Color.LimeGreen;
                _window.Decryption.Status = false;
                break;
        }

        if (!nuke.Decrypted)
            return;
        _window.Decryption.Button.Text = "Decryption (Enabled)";
        _window.Decryption.Button.Modulate = Color.Maroon;
        _window.Decryption.Button.Disabled = true;
        _window.Decryption.Status = false;
        _window.DecryptionState.Text = "Encryption Status: [color=green]Decrypted[/color]";
    }
    private void UpdateNuke(RMCNukeComponent nuke)
    {
        if (_window is not { IsOpen: true })
            return;

        if (nuke is { Nuking: false, Decrypted: false })
        {
            _window.Nuke.Button.Text = "Activate Nuclear Ordnance";
            _window.Nuke.Button.Modulate = Color.Maroon;
            _window.Nuke.Button.Disabled = true;
            _window.Nuke.Status = false;
            return;
        }

        if (nuke.Nuking)
        {
            _window.Nuke.Button.Text = "Activate Nuclear Ordnance";
            _window.Nuke.Button.Modulate = Color.LimeGreen;
            _window.Nuke.Button.Disabled = true;
            return;
        }

        if (nuke.Decrypted)
        {
            _window.Nuke.Button.Text = "Activate Nuclear Ordnance";
            _window.Nuke.Button.Modulate = Color.LimeGreen;
            _window.Nuke.Button.Disabled = false;
            return;
        }
    }
}


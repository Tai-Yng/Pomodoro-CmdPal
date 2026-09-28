// Copyright (c) Tai-Yng. MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Pomodoro.CmdPal.Commands;
using Pomodoro.Core;

namespace Pomodoro.CmdPal;

/// <summary>
/// The single top-level page. Actions depend on the persisted state; while a phase runs
/// the list ticks every second (safe — the page is only visible while the host keeps us alive).
/// Correctness never depends on this process: timing is wall-clock + OS scheduled toast.
/// </summary>
internal sealed partial class PomodoroPage : DynamicListPage
{
    private const int TickMs = 1000;

    private readonly PomodoroStore _store;
    private readonly SettingsManager _settings;
    private System.Threading.Timer? _ticker;

    public PomodoroPage(PomodoroStore store, SettingsManager settings)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Pomodoro";
        Name = "Open";

        _store = store;
        _settings = settings;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) =>
        RaiseItemsChanged(0);

    public override IListItem[] GetItems()
    {
        var now = DateTimeOffset.UtcNow;
        var state = _store.Load();
        ManageTicker(state);

        var items = new List<IListItem>();
        Func<PomodoroAction, PomodoroActionCommand> action = kind =>
            new PomodoroActionCommand(_store, _settings, kind, () => RaiseItemsChanged(0));

        if (state.Phase == PomodoroPhase.Idle)
        {
            items.Add(new ListItem(action(PomodoroAction.StartFocus))
            {
                Title = "Start focus",
                Subtitle = $"{TimeSpan.FromMilliseconds(_settings.FocusMs).TotalMinutes:0} minutes — no background process needed, the system reminds you when time is up",
            });
            items.Add(new ListItem(action(PomodoroAction.StartBreak))
            {
                Title = "Start break",
                Subtitle = $"{TimeSpan.FromMilliseconds(_settings.BreakMs).TotalMinutes:0} minutes",
            });
            items.Add(new ListItem(new CommandItem(_settings.Settings.SettingsPage))
            {
                Title = "Settings",
                Subtitle = "focus / break lengths",
            });
            return items.ToArray();
        }

        var remaining = PomodoroLogic.RemainingMs(state, now);
        var remainingText = PomodoroLogic.FormatRemaining(remaining);
        var expired = PomodoroLogic.IsExpired(state, now);
        var phaseText = (state.Phase == PomodoroPhase.Focus ? "Focus" : "Break")
            + (state.Paused ? " (paused)" : expired ? " — finished" : "");

        items.Add(new ListItem(new NoOpCommand())
        {
            Title = $"⏳ {phaseText} — {remainingText}",
            Subtitle = $"cycle {state.Cycle} · {state.DurationMs / 60000:0} min sessions",
        });

        if (state.Paused)
        {
            items.Add(new ListItem(action(PomodoroAction.Resume))
            {
                Title = "Resume",
                Subtitle = $"{remainingText} left",
            });
        }
        else if (!expired)
        {
            items.Add(new ListItem(action(PomodoroAction.Pause))
            {
                Title = "Pause",
                Subtitle = "cancels the scheduled system notification",
            });
        }

        items.Add(new ListItem(action(PomodoroAction.StartFocus))
        {
            Title = "Start focus",
            Subtitle = state.Phase == PomodoroPhase.Focus ? "restarts the session" : $"cycle {state.Cycle + 1}",
        });
        items.Add(new ListItem(action(PomodoroAction.StartBreak))
        {
            Title = "Start break",
            Subtitle = $"{TimeSpan.FromMilliseconds(_settings.BreakMs).TotalMinutes:0} minutes",
        });
        items.Add(new ListItem(action(PomodoroAction.Cancel))
        {
            Title = "Cancel",
            Subtitle = "clears state and scheduled notifications",
        });

        return items.ToArray();
    }

    private void ManageTicker(PomodoroState state)
    {
        var shouldTick = state.Phase != PomodoroPhase.Idle && !state.Paused;
        if (shouldTick && _ticker is null)
        {
            _ticker = new System.Threading.Timer(_ => RaiseItemsChanged(0), null, TickMs, TickMs);
        }
        else if (!shouldTick && _ticker is not null)
        {
            _ticker.Dispose();
            _ticker = null;
        }
    }
}

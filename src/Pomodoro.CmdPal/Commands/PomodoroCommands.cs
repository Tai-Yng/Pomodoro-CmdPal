// Copyright (c) Tai-Yng. MIT license.

using System;
using System.Globalization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Pomodoro.Core;

namespace Pomodoro.CmdPal.Commands;

public enum PomodoroAction
{
    StartFocus,
    StartBreak,
    Pause,
    Resume,
    Cancel,
}

/// <summary>Applies a state transition, syncs the OS scheduled toast, reports via toast.</summary>
public sealed partial class PomodoroActionCommand : InvokableCommand
{
    private readonly PomodoroStore _store;
    private readonly SettingsManager _settings;
    private readonly PomodoroAction _action;
    private readonly Action _onChanged;

    public PomodoroActionCommand(PomodoroStore store, SettingsManager settings, PomodoroAction action, Action onChanged)
    {
        _store = store;
        _settings = settings;
        _action = action;
        _onChanged = onChanged;

        (Name, Icon) = action switch
        {
            PomodoroAction.StartFocus => ("Start focus", new IconInfo("\uE768")),   // Play
            PomodoroAction.StartBreak => ("Start break", new IconInfo("\uE76E")),   // Coffee-ish
            PomodoroAction.Pause => ("Pause", new IconInfo("\uE769")),              // Pause
            PomodoroAction.Resume => ("Resume", new IconInfo("\uE768")),
            PomodoroAction.Cancel => ("Cancel", new IconInfo("\uE711")),            // Cancel
            _ => ("Pomodoro", new IconInfo("\uE823")),
        };
    }

    public override CommandResult Invoke()
    {
        var now = DateTimeOffset.UtcNow;
        var current = _store.Load();

        switch (_action)
        {
            case PomodoroAction.StartFocus:
            {
                var started = PomodoroLogic.Start(PomodoroPhase.Focus, now, _settings.FocusMs, current.Cycle);
                _store.Save(started);
                ToastScheduler.ScheduleForPhase(PomodoroPhase.Focus, now.AddMilliseconds(_settings.FocusMs));
                break;
            }
            case PomodoroAction.StartBreak:
            {
                var started = PomodoroLogic.Start(PomodoroPhase.Break, now, _settings.BreakMs, current.Cycle);
                _store.Save(started);
                ToastScheduler.ScheduleForPhase(PomodoroPhase.Break, now.AddMilliseconds(_settings.BreakMs));
                break;
            }
            case PomodoroAction.Pause:
            {
                var paused = PomodoroLogic.Pause(current, now);
                _store.Save(paused);
                ToastScheduler.Clear();
                break;
            }
            case PomodoroAction.Resume:
            {
                var resumed = PomodoroLogic.Resume(current, now);
                _store.Save(resumed);
                var remaining = PomodoroLogic.RemainingMs(resumed, now);
                ToastScheduler.ScheduleForPhase(resumed.Phase, now.AddMilliseconds(remaining));
                break;
            }
            case PomodoroAction.Cancel:
            {
                _store.Save(PomodoroLogic.Cancel(current));
                ToastScheduler.Clear();
                break;
            }
        }

        var after = _store.Load();
        var message = _action switch
        {
            PomodoroAction.StartFocus => $"Focus started ({FormatMs(_settings.FocusMs)})",
            PomodoroAction.StartBreak => $"Break started ({FormatMs(_settings.BreakMs)})",
            PomodoroAction.Pause => $"Paused ({PomodoroLogic.FormatRemaining(PomodoroLogic.RemainingMs(after, now))} left)",
            PomodoroAction.Resume => $"Resumed ({PomodoroLogic.FormatRemaining(PomodoroLogic.RemainingMs(after, now))} left)",
            PomodoroAction.Cancel => "Pomodoro cancelled",
            _ => "Done",
        };

        _onChanged();
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = message,
            Result = CommandResult.Dismiss(),
        });
    }

    private static string FormatMs(long ms) =>
        TimeSpan.FromMilliseconds(ms).TotalMinutes.ToString("0", CultureInfo.InvariantCulture) + ":00";
}

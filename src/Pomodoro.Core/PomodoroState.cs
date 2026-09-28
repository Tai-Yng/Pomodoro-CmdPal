// Copyright (c) Tai-Yng. MIT license.

namespace Pomodoro.Core;

public enum PomodoroPhase
{
    Idle,
    Focus,
    Break,
}

/// <summary>
/// Persisted pomodoro state. All timing is wall-clock based (startedAt + duration) so the
/// extension process may die at any moment without losing the session.
/// </summary>
public sealed record PomodoroState(
    PomodoroPhase Phase,
    long StartedAtUnixMs,
    long DurationMs,
    bool Paused,
    long RemainingOnPauseMs,
    int Cycle)
{
    public static PomodoroState Idle { get; } = new(PomodoroPhase.Idle, 0, 0, false, 0, 0);
}

/// <summary>Pure state transitions over PomodoroState; no I/O, fully testable.</summary>
public static class PomodoroLogic
{
    public static PomodoroState Start(PomodoroPhase phase, DateTimeOffset now, long durationMs, int previousCycle)
    {
        if (phase is not (PomodoroPhase.Focus or PomodoroPhase.Break))
        {
            throw new ArgumentOutOfRangeException(nameof(phase));
        }

        var cycle = phase == PomodoroPhase.Focus ? previousCycle + 1 : previousCycle;
        return new PomodoroState(phase, now.ToUnixTimeMilliseconds(), durationMs, false, 0, cycle);
    }

    /// <summary>Remaining time; clamped at zero when the phase has already elapsed.</summary>
    public static long RemainingMs(PomodoroState state, DateTimeOffset now)
    {
        if (state.Phase == PomodoroPhase.Idle)
        {
            return 0;
        }

        if (state.Paused)
        {
            return state.RemainingOnPauseMs;
        }

        var elapsed = now.ToUnixTimeMilliseconds() - state.StartedAtUnixMs;
        return Math.Max(0, state.DurationMs - elapsed);
    }

    public static bool IsExpired(PomodoroState state, DateTimeOffset now) =>
        state.Phase != PomodoroPhase.Idle && !state.Paused && RemainingMs(state, now) == 0;

    public static PomodoroState Pause(PomodoroState state, DateTimeOffset now)
    {
        if (state.Phase == PomodoroPhase.Idle || state.Paused)
        {
            return state;
        }

        return state with
        {
            Paused = true,
            RemainingOnPauseMs = RemainingMs(state, now),
        };
    }

    public static PomodoroState Resume(PomodoroState state, DateTimeOffset now)
    {
        if (state.Phase == PomodoroPhase.Idle || !state.Paused)
        {
            return state;
        }

        // Shift the start backwards so that remaining (duration - elapsed) equals the
        // remaining time frozen at pause: started = now - (duration - remaining).
        return state with
        {
            Paused = false,
            StartedAtUnixMs = now.ToUnixTimeMilliseconds() - (state.DurationMs - state.RemainingOnPauseMs),
            RemainingOnPauseMs = 0,
        };
    }

    public static PomodoroState Cancel(PomodoroState state) => PomodoroState.Idle;

    /// <summary>"mm:ss" for the status list item.</summary>
    public static string FormatRemaining(long remainingMs)
    {
        var total = (int)Math.Ceiling(remainingMs / 1000.0);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}

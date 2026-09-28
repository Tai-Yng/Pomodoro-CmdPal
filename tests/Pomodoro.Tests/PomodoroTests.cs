// Copyright (c) Tai-Yng. MIT license.

using Pomodoro.Core;

namespace Pomodoro.Tests;

public sealed class PomodoroTests : IDisposable
{
    private readonly string _dir;

    public PomodoroTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "pomodoro-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_Focus_IncrementsCycle()
    {
        var state = PomodoroLogic.Start(PomodoroPhase.Focus, T0, 25 * 60_000, previousCycle: 2);

        Assert.Equal(PomodoroPhase.Focus, state.Phase);
        Assert.Equal(3, state.Cycle);
        Assert.False(state.Paused);
        Assert.Equal(25 * 60_000, PomodoroLogic.RemainingMs(state, T0));
        Assert.Equal(15 * 60_000, PomodoroLogic.RemainingMs(state, T0.AddMinutes(10)));
        Assert.Equal(0, PomodoroLogic.RemainingMs(state, T0.AddMinutes(26)));
    }

    [Fact]
    public void Start_Break_KeepsCycle()
    {
        var state = PomodoroLogic.Start(PomodoroPhase.Break, T0, 5 * 60_000, previousCycle: 3);

        Assert.Equal(PomodoroPhase.Break, state.Phase);
        Assert.Equal(3, state.Cycle);
    }

    [Fact]
    public void PauseResume_PreservesRemaining()
    {
        var started = PomodoroLogic.Start(PomodoroPhase.Focus, T0, 25 * 60_000, 0);
        var atPause = T0.AddMinutes(10);
        var paused = PomodoroLogic.Pause(started, atPause);

        Assert.True(paused.Paused);
        Assert.Equal(15 * 60_000, paused.RemainingOnPauseMs);
        // while paused, remaining does not shrink with the wall clock
        Assert.Equal(15 * 60_000, PomodoroLogic.RemainingMs(paused, atPause.AddMinutes(30)));

        var resumed = PomodoroLogic.Resume(paused, atPause.AddMinutes(40));
        Assert.False(resumed.Paused);
        Assert.Equal(15 * 60_000, PomodoroLogic.RemainingMs(resumed, atPause.AddMinutes(40)));
        Assert.Equal(0, PomodoroLogic.RemainingMs(resumed, atPause.AddMinutes(56)));
    }

    [Fact]
    public void PauseResume_IdempotentOnWrongState()
    {
        var idle = PomodoroState.Idle;
        Assert.Equal(idle, PomodoroLogic.Pause(idle, T0));
        Assert.Equal(idle, PomodoroLogic.Resume(idle, T0));

        var started = PomodoroLogic.Start(PomodoroPhase.Focus, T0, 25 * 60_000, 0);
        Assert.Equal(started, PomodoroLogic.Resume(started, T0));
    }

    [Fact]
    public void IsExpired_DetectsElapsedPhase_ButNotPaused()
    {
        var started = PomodoroLogic.Start(PomodoroPhase.Focus, T0, 25 * 60_000, 0);
        Assert.False(PomodoroLogic.IsExpired(started, T0.AddMinutes(24)));
        Assert.True(PomodoroLogic.IsExpired(started, T0.AddMinutes(25)));

        var paused = PomodoroLogic.Pause(started, T0.AddMinutes(1));
        Assert.False(PomodoroLogic.IsExpired(paused, T0.AddHours(5)));
    }

    [Fact]
    public void FormatRemaining_RoundsUpSeconds()
    {
        Assert.Equal("25:00", PomodoroLogic.FormatRemaining(25 * 60_000));
        Assert.Equal("00:01", PomodoroLogic.FormatRemaining(1));
        Assert.Equal("00:00", PomodoroLogic.FormatRemaining(0));
    }

    // ---- store ----

    [Fact]
    public void Store_RoundTripsState()
    {
        var store = new PomodoroStore(_dir);
        var state = PomodoroLogic.Start(PomodoroPhase.Focus, T0, 25 * 60_000, 1);
        store.Save(state);

        var loaded = store.Load();

        Assert.Equal(state, loaded);
    }

    [Fact]
    public void Store_CorruptFile_DegradesToIdle()
    {
        var store = new PomodoroStore(_dir);
        File.WriteAllText(store.FilePath, "{ not json !!!");

        Assert.Equal(PomodoroState.Idle, store.Load());
    }

    [Fact]
    public void Store_MissingFile_Idle()
    {
        var store = new PomodoroStore(_dir);

        Assert.Equal(PomodoroState.Idle, store.Load());
    }
}

public sealed class AlarmWavTests
{
    [Fact]
    public void Generate_ProducesValidRiffWav_ScaledByChimeCount()
    {
        var wav = AlarmWav.Generate(3);

        Assert.True(wav.Length > 44);
        Assert.Equal((byte)'R', wav[0]);
        Assert.Equal((byte)'I', wav[1]);
        Assert.Equal((byte)'F', wav[2]);
        Assert.Equal((byte)'F', wav[3]);
        var dataSize = BitConverter.ToUInt32(wav, 40);
        Assert.Equal((long)(wav.Length - 44), (long)dataSize);

        var one = AlarmWav.Generate(1);
        var five = AlarmWav.Generate(5);
        Assert.True(one.Length < wav.Length && wav.Length < five.Length);
    }

    [Fact]
    public void Generate_ClampsOutOfRangeChimes()
    {
        var clamped = AlarmWav.Generate(99);
        var atMax = AlarmWav.Generate(5);
        Assert.Equal(atMax.Length, clamped.Length);
    }
}

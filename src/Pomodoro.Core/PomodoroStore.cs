// Copyright (c) Tai-Yng. MIT license.

using System.IO;
using System.Text.Json;

namespace Pomodoro.Core;

/// <summary>
/// Persists pomodoro state to a single JSON file. Corrupt or missing files degrade to
/// Idle — the store must never throw into the host.
/// </summary>
public sealed class PomodoroStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private readonly string _filePath;

    public PomodoroStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "state.json");
    }

    public string FilePath => _filePath;

    public PomodoroState Load()
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            var state = JsonSerializer.Deserialize<PomodoroState>(json, Options);
            if (state is not null && Enum.IsDefined(state.Phase) && state.DurationMs >= 0)
            {
                return state;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // corrupt → idle
        }
        return PomodoroState.Idle;
    }

    public void Save(PomodoroState state)
    {
        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(_filePath, json);
    }
}

// Based on the official CmdPal ExtensionTemplate (microsoft/PowerToys, MIT).
// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Pomodoro.CmdPal;

public sealed partial class SettingsManager : JsonSettingsManager
{
    private static readonly string _namespace = "Pomodoro.CmdPal";

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    private readonly TextSetting _focusMinutes = new(
        Namespaced("FocusMinutes"),
        "Focus length (minutes)",
        "Duration of one focus session (1-180)",
        "25");

    private readonly TextSetting _breakMinutes = new(
        Namespaced("BreakMinutes"),
        "Break length (minutes)",
        "Duration of one break (1-180)",
        "5");

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("Pomodoro.CmdPal");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_focusMinutes);
        Settings.Add(_breakMinutes);

        // Load settings from file upon initialization
        LoadSettings();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    public long FocusMs => ParseMinutes(_focusMinutes.Value, 25);

    public long BreakMs => ParseMinutes(_breakMinutes.Value, 5);

    private static long ParseMinutes(string? value, int fallback) =>
        long.TryParse(value, out var minutes) && minutes is > 0 and <= 180
            ? minutes * 60_000
            : fallback * 60_000;
}

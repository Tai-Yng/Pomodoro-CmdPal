// Based on the official CmdPal ExtensionTemplate (microsoft/PowerToys, MIT).
// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Pomodoro.Core;

namespace Pomodoro.CmdPal;

public partial class PomodoroCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();

    public PomodoroCommandsProvider()
    {
        DisplayName = "Pomodoro";
        Icon = IconHelpers.FromRelativePath("Assets\\icon.png");

        var store = new PomodoroStore(Utilities.BaseSettingsPath("Pomodoro.CmdPal"));
        _commands =
        [
            new CommandItem(new PomodoroPage(store, _settingsManager))
            {
                Title = DisplayName,
                MoreCommands = [new CommandContextItem(_settingsManager.Settings.SettingsPage)],
            },
        ];

        Settings = _settingsManager.Settings;
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}

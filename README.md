# Pomodoro for Command Palette

Pomodoro timer extension for PowerToys **Command Palette (CmdPal)** — start focus sessions from the launcher, get reminded by the OS even when the extension process is gone.

- **No background process**: timing is wall-clock + a persisted state file; completion reminders are OS **scheduled toasts** (they fire on time even if the extension process died)
- Commands: **Start focus** / **Start break** / **Pause** / **Resume** / **Cancel**, plus a live status row (cycle counter, mm:ss)
- Configurable focus/break lengths (default 25/5 minutes) in the extension settings
- Fully offline, single local state file: `%LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\Pomodoro\state.json`

## Install

Same trust flow as any self-published CmdPal extension:

1. Download `Pomodoro.CmdPal_<version>_x64.msix` + `pomodoro-sign.cer` from Releases
2. Install the `.cer` into **Local Machine → Trusted People**
3. Install the `.msix` (or `Add-AppxPackage -Path <msix>`)
4. `Win+Alt+Space` → type **Pomodoro**

## Usage

- Open the Pomodoro command → **Start focus** — when the time is up the system pops "Focus session complete"
- While running: the status row shows remaining + end time (the list only re-renders on your input, so your selection never jumps); **Pause** freezes the remaining time (and cancels the scheduled notification), **Resume** re-schedules it
- **Start break** keeps the cycle counter; **Cancel** clears everything
- Settings (context menu on the Pomodoro entry): focus length, break length (1–180 minutes)

## Troubleshooting

- **Duplicate entry / broken icon after upgrading the extension**: the running CmdPal host kept a stale registration. Restart PowerToys (or run the "Reload" command inside Command Palette) — it collapses to a single entry with a proper icon.

## Development

```powershell
dotnet build src/Pomodoro.CmdPal/Pomodoro.CmdPal.csproj -p:Platform=x64
dotnet test tests/Pomodoro.Tests/Pomodoro.Tests.csproj
```

Layout: `src/Pomodoro.Core` (pure state machine + store, host-free) · `src/Pomodoro.CmdPal` (CmdPal host adapter + toast scheduling) · `tests`.

## License

MIT

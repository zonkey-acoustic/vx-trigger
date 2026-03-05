# ShotTrigger

A lightweight Windows system tray application that monitors ProTee United shot data and fires triggers to notify swing recording software when a shot is detected.

## How It Works

ProTee United writes a timestamped subdirectory to `%APPDATA%\ProTeeUnited\Shots` for each shot. ShotTrigger watches that directory using a `FileSystemWatcher` and fires a configured trigger the moment a new shot folder appears — no network packet capture, no admin privileges required.

## Trigger Types

**Audio Trigger** — Plays a synthetic golf impact sound through a selected audio output device. The sound envelope (frequency, noise/tone decay, mix, duration) is configurable to match your launch monitor's timing.

**Network Trigger (UDP)** — Sends a UDP packet to a configured host and port. Compatible with Kinovea, Swing Catalyst, and any other recording software that supports UDP trigger input.

## Requirements

- Windows 10/11
- [ProTee United](https://www.proteegolf.com/) launch monitor software

## Installation

Download `ShotTrigger.exe` from the [releases page](../../releases) and run it. No installer, no .NET runtime required — it's a self-contained executable.

The app will open the configuration window on first launch if no trigger has been configured yet.

## Usage

1. Run `ShotTrigger.exe` — a colored circle appears in the system tray
2. Double-click the tray icon (or right-click → **Configure...**) to open settings
3. Set the **Shots directory** (default: `%APPDATA%\ProTeeUnited\Shots`)
4. Choose a trigger type and configure it, then click **Save**
5. The tray icon turns green when monitoring is active

### Tray Icon Colors

| Color | Meaning |
|-------|---------|
| Green | Actively monitoring |
| Yellow | Configured but stopped |
| Gray | Not configured |

### Settings

Settings are saved to `Documents\ShotTrigger\settings.json`. If you previously used SimLogger, your audio and network trigger settings are imported automatically on first launch.

## Building

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
.\build.ps1
```

Output: `dist\ShotTrigger.exe`

## Project Structure

```
ShotTrigger/
├── Services/
│   ├── ShotFolderWatcher.cs      # FileSystemWatcher on ProTee Shots directory
│   ├── TriggerOrchestrator.cs    # Wires detection → triggers with debounce
│   ├── AudioTriggerService.cs    # Golf impact sound via NAudio
│   ├── NetworkTriggerService.cs  # UDP trigger packet sender
│   └── TriggerSettings.cs        # Settings model + JSON persistence
└── Views/
    ├── ConfigWindow.xaml         # Configuration UI
    └── MessageDialog.xaml        # Info/error dialogs
```

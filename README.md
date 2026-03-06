# VXTrigger

A lightweight Windows system tray application that monitors ProTee Labs shot data and fires triggers to notify swing recording software when a shot is detected.

## How It Works

ProTee Labs writes a timestamped subdirectory to `%APPDATA%\Roaming\ProTeeUnited\Shots` for each shot. VXTrigger watches that directory using a `FileSystemWatcher` and fires a configured trigger the moment a new shot folder appears.

## Trigger Types

**Audio Trigger** — Plays a synthetic golf impact sound through a selected audio output device. The sound envelope (frequency, noise/tone decay, mix, duration) is configurable. Have tested with SwingCatalyst.

Download and install: [VB-CABLE](https://vb-audio.com/Cable/index.htm)

1. Select CABLE Input (VB-Audio Virtual Cable) as the output source in VXTrigger.
2. In your recording software, set the trigger input to CABLE Output (VB-Audio Virtual Cable). Adjust the threshold so the test sound triggers a swing recording.

**Network Trigger (UDP)** — Sends a UDP packet to a configured host and port. Compatible with [Kinovea 2025.1](https://www.kinovea.org/) and any other recording software that supports UDP trigger input.

**Swing Video Capture** — For users recording with ProTee Labs' built-in high-speed swing cameras instead of external recording software. ProTee writes `.mp4` files to `%APPDATA%\ProTeeUnited\SwingVideos` with random filenames, overwriting them after each shot. This trigger copies the videos to a user-defined destination folder, organized by date with sequential shot numbering:

```
Destination/
  2026-03-06/
    Shot_001_1.mp4
    Shot_001_2.mp4
    Shot_002_1.mp4
    Shot_002_2.mp4
```

## Requirements

- Windows 10/11
- [ProTee Labs](https://proteelaunchmonitors.com/) launch monitor software

## Installation

Download `VXTrigger.exe` from the [releases page](../../releases) and run it. No installer, no .NET runtime required — it's a self-contained executable.

The app will open the configuration window on first launch if no trigger has been configured yet.

## Usage

1. Run `VXTrigger.exe` — a colored circle appears in the system tray
2. Double-click the tray icon (or right-click → **Configure...**) to open settings
3. Set the **Shots directory** (default: `%APPDATA%\Roaming\ProTeeUnited\Shots`)
4. Choose a trigger type and configure it, then click **Save**
5. The tray icon turns green when monitoring is active

### Tray Icon Colors

| Color | Meaning |
|-------|---------|
| Green | Actively monitoring |
| Yellow | Configured but stopped |
| Gray | Not configured |

### Settings

Settings are saved to `Documents\VXTrigger\settings.json`

## Building

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
.\build.ps1
```

Output: `dist\VXTrigger.exe`

## Project Structure

```
VXTrigger/
├── Services/
│   ├── ShotFolderWatcher.cs      # FileSystemWatcher on ProTee Shots directory
│   ├── TriggerOrchestrator.cs    # Wires detection → triggers with debounce
│   ├── AudioTriggerService.cs    # Golf impact sound via NAudio
│   ├── NetworkTriggerService.cs  # UDP trigger packet sender
│   ├── SwingVideoService.cs      # Copies ProTee swing videos to organized folders
│   └── TriggerSettings.cs        # Settings model + JSON persistence
└── Views/
    ├── ConfigWindow.xaml         # Configuration UI
    └── MessageDialog.xaml        # Info/error dialogs
```

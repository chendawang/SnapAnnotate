# SnapAnnotate

SnapAnnotate is an open-source screenshot and annotation tool for Windows. It supports smart window detection, region capture, on-screen annotation, clipboard output, local PNG history, global hotkeys, a system tray, and a movable floating toolbar.

> Current version: `0.1.0`. This is an early public test release.

[中文说明](README.md) · [Releases](https://github.com/chendawang/SnapAnnotate/releases) · [Issues](https://github.com/chendawang/SnapAnnotate/issues)

## Highlights

- Press `Ctrl+Shift+A` to freeze the desktop and start a region capture.
- Select detected windows or controls, or drag a custom region.
- Annotate within the selected region before exporting.
- Save a PNG and copy it to the clipboard in one action.
- Browse local history by date, with multi-select copy and delete.
- Configure the selection border, output folder, naming pattern, and startup behavior.
- Check GitHub Releases manually from Settings.

Screenshots, settings, and history stay on your computer by default. SnapAnnotate does not contain AI upload functionality and does not automatically upload captures.

## Install

Download the latest `ScreenshotAssistant-Setup-*-win-x64.exe` from [GitHub Releases](https://github.com/chendawang/SnapAnnotate/releases). Windows 10/11 x64 is supported; the installer is self-contained.

## Build

Install Git and .NET SDK 10.0.300 or a compatible feature band, then run:

```powershell
./eng/sync-sharex.ps1
dotnet restore ScreenshotAssistant.slnx
dotnet build ScreenshotAssistant.slnx --no-restore
dotnet test ScreenshotAssistant.slnx --no-build
dotnet run --project src/ScreenshotAssistant.App/ScreenshotAssistant.App.csproj
```

Inno Setup 6 is required to create the Windows installer:

```powershell
./eng/package-windows.ps1 -Version 0.1.0
```

See [CONTRIBUTING.md](CONTRIBUTING.md), [release instructions](docs/RELEASING.md), and the [privacy statement](docs/PRIVACY.md) for details.

## License

SnapAnnotate directly links a pinned ShareX source revision and applies documented patches. The combined project is distributed under **GNU GPL v3 or later**. See [LICENSE](LICENSE) and [open-source compliance](docs/OPEN_SOURCE_COMPLIANCE.md).

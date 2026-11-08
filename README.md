# Installed Software Inventory

A small, read-only Windows desktop utility that inventories the software installed
on the local machine and presents it in a clean, filterable table. Built with
**C# / WPF (.NET 8)** using the **MVVM** pattern.

It reads only the non-sensitive metadata that Windows already exposes in
*Add / Remove Programs* — nothing is launched, changed, or uploaded anywhere.

![screenshot placeholder](docs/screenshot.png)

## What it does

- Enumerates installed desktop programs from the standard Windows **uninstall
  registry keys** (`HKLM` 64-bit, `HKLM` 32-bit / `WOW6432Node`, and `HKCU`).
- Shows **name, version, publisher, install date, source hive**, and a
  **"running" flag** (via the Process API, by matching the install directory of
  live processes).
- **Live filtering** by name or publisher.
- **CSV export** (RFC-4180 quoting, UTF-8 BOM so Excel opens it cleanly).
- Background scanning on a worker thread, so the UI never freezes.

## Why it's built this way

- **MVVM** keeps the registry/process logic (`Services/`) fully separated from
  the UI (`MainWindow.xaml`) and the bindable state (`ViewModels/`). The services
  have no WPF dependency and are unit-testable on their own.
- **Read-only by design.** The services only ever call `OpenSubKey` and read
  values; there is no write path. This is the right default for an inventory tool
  and makes the app safe to run without elevation.
- **Defensive parsing.** Registry data is inconsistent in the wild (missing
  `DisplayName`, update entries, odd date formats), so each field is validated
  and system components are filtered out the same way Windows hides them.

## Project layout

```
src/
├─ Models/InstalledApp.cs            # immutable record for one program
├─ Services/
│  ├─ RegistryInventoryService.cs    # reads the uninstall keys + running state
│  └─ CsvExportService.cs            # RFC-4180 CSV writer
├─ ViewModels/
│  ├─ MainViewModel.cs               # commands, filtering, async scan
│  └─ RelayCommand.cs                # minimal ICommand, no external deps
├─ MainWindow.xaml(.cs)              # the view
└─ App.xaml(.cs)                     # entry point + resources
```

## Build and run

Requires the .NET 8 SDK on Windows.

```powershell
cd src
dotnet run
```

## Publish as a single self-contained .exe

No .NET install required on the target machine:

```powershell
dotnet publish src/InstalledSoftwareInventory.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The result is one `InstalledSoftwareInventory.exe` under
`bin/Release/net8.0-windows/win-x64/publish/`.

## Notes

This repository is a portfolio sample demonstrating WPF + MVVM, Windows Registry
and Process APIs, CSV export, and single-file publishing. It is intentionally
general-purpose; the same architecture adapts cleanly to narrower inventory
targets by swapping the detection logic in `Services/`.

## License

MIT

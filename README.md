# EntropyPasswordForge_CS

EntropyPasswordForge_CS is a Windows 11 x64 password generator built with C# / .NET 8 / Windows Forms.

It generates passwords by combining the Windows cryptographic random number generator with mouse-input entropy and short CPU-jitter samples, then mixing the collected material with SHA-512 / HMAC-SHA512.

> This tool is not a strict physical TRNG. The foundation is the operating system CSPRNG. Mouse movement and CPU jitter are additional mixing material.

## Current version

- Version: **v1.0.8**
- Executable: `EntropyPasswordForge_CS_v1.0.8.exe`
- Target framework: `.NET 8` / `net8.0-windows`
- Runtime target: `win-x64`
- UI language: Japanese

## Safety and privacy design

This tool is designed to avoid suspicious or unnecessary behavior.

- Uses `System.Security.Cryptography.RandomNumberGenerator` as the cryptographic base.
- Does not use `System.Random`.
- Does not use `Guid.NewGuid()` as a random source.
- Does not use `DateTime.Now.Ticks` as a standalone random source.
- Does not collect keyboard input, key events, key timing, modifier keys, or typed text as entropy.
- Does not perform external network communication.
- Does not include telemetry.
- Does not save generated passwords to files.
- Does not write generated passwords to logs or Windows Event Log.

## Main features

- OS CSPRNG + mouse-input entropy + CPU-jitter mixing.
- SHA-512 / HMAC-SHA512 based mixing and DRBG flow.
- Rejection sampling for unbiased character selection.
- Fisher-Yates shuffle after required character groups are selected.
- Full-screen mouse entropy collection mode.
- Clipboard auto-clear after 30 seconds.
- Generated-result auto-clear after 60 seconds.
- Optional auto-copy mode. When enabled, generation count is fixed to 1.
- Ambiguous-character exclusion option.
- Minimum-use sliders for uppercase letters, digits, and symbols.
- Custom symbol mode: include or exclude.

## UI layout

The UI is adjusted around Full HD 1920x1080 usage.

The password settings area has two tabs:

- **基本**: length, generation count, character-category checkboxes, auto-copy, auto-clear settings.
- **記号・配分**: minimum-use sliders and symbol settings.

Only the result/log split area is intended to be draggable. Fixed borders are not displayed as draggable controls.

## Custom symbols

The symbol set can be set to either standard symbols or custom symbols.

In **含む** mode, every valid custom symbol entered by the user is included at least once when symbols are enabled.

In **除外** mode, the entered symbols are removed from the standard symbol set, and generated passwords are checked so excluded symbols do not appear.

Duplicate custom symbols, spaces, tabs, and line breaks are normalized away. The full custom-symbol text is not written to logs.

## Full-screen mouse entropy collection

Click the normal entropy collection area to start full-screen mouse collection mode. Move the mouse freely, then left-click again to exit. Esc also exits the mode, but Esc is only an exit operation and is not used as entropy material.

Very short duplicate mouse events and same-coordinate mouse moves are suppressed to reduce double counting.

## Clipboard and display clearing

When clipboard auto-clear is enabled, the clipboard is cleared after 30 seconds only if its content still matches the generated password.

When result auto-clear is enabled, displayed generated results are removed after 60 seconds. The password body is not written to logs.

## Memory limitation

Generated passwords are .NET managed strings while they are displayed or copied. They may temporarily remain in process memory. Avoid screenshots, screen sharing, clipboard monitoring environments, and unnecessary reuse of generated passwords.

## Build

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet restore .\EntropyPasswordForge_CS_v1.0.8.csproj
dotnet build .\EntropyPasswordForge_CS_v1.0.8.csproj -c Release
dotnet publish .\EntropyPasswordForge_CS_v1.0.8.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The published executable is expected under:

```text
bin\Release\net8.0-windows\win-x64\publish\EntropyPasswordForge_CS_v1.0.8.exe
```

## Release hash

SHA-256 for the v1.0.8 published executable:

```text
D0802520867151A48C57B54F9E4108EED78519E1D9BF454767A66A68CB23BA5A
```

## Repository policy

Build artifacts such as `bin/`, `obj/`, `publish/`, `*.exe`, `*.dll`, `*.pdb`, `*.deps.json`, and `*.runtimeconfig.json` should not be committed to the repository.

Executable files should be distributed through GitHub Releases, not from the repository root.

## Disclaimer

Use this tool at your own risk. Password storage, rotation, and compatibility with destination systems are the user's responsibility.

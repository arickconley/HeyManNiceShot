# HeyManNiceShot

A modern, keyboard-driven screenshot tool for Windows — inspired by
[CleanShot X](https://cleanshot.com) and [Shottr](https://shottr.cc) on macOS.

Press a hotkey, drag to snip a region, watch the dimensions tick up next to
the cursor, then pick **Copy / Save / Edit** from a floating toolbar that
appears under the selection. Open the result in an editor with arrows,
shapes, freehand, text, and blur — plus document-level rounded corners,
padding, drop shadow, and a solid background.

> **Status: early scaffold (Avalonia rewrite).** All source files are in
> place but only the pure-logic parts (Editing, Geometry, Settings) are
> covered by automated tests. The Avalonia overlay/editor windows, the
> Direct3D 11 + Windows.Graphics.Capture pipeline, and the Win32 clipboard
> dual-format helper need real Windows hardware to verify.

---

## Why this looks the way it does

This is the second pass at the project. The first pass used WinUI 3, hit a
fundamental limitation — WinUI 3 windows can't composite over the live
desktop — and ended up with a freeze-frame overlay that broke on multi-
monitor setups and stale during selection. Avalonia 11 with
`TransparencyLevelHint="Transparent"` does what WinUI 3 wouldn't, gives us
Fluent design + Mica out of the box, and lets the renderer talk straight
to the same Skia surface Avalonia draws on.

The cross-platform `Core` library from the first pass is gone — there are
already excellent screenshot tools on macOS and Linux, and the abstraction
was paying for a future that wasn't going to happen. One Windows-only
project, no `ICaptureSource` interface, one TFM.

---

## Prerequisites

- **Windows 10 20H1 (build 19041) or later**
- **.NET 10 SDK** — `winget install Microsoft.DotNet.SDK.10`
- **Visual Studio 2022 17.12+**, **JetBrains Rider 2024.3+**, or just `dotnet` CLI

The project is Windows-only. `dotnet restore` from a non-Windows machine
won't resolve `Vortice.Direct3D11` or the WinRT Graphics Capture
projection; that's expected.

---

## Build, run, test

```powershell
git clone https://github.com/arickconley/HeyManNiceShot.git
cd HeyManNiceShot
dotnet restore
dotnet build
dotnet test tests/HeyManNiceShot.Tests
dotnet run --project src/HeyManNiceShot
```

The app lives in the system tray. Right-click for **Capture / Settings / Quit**,
or press the hotkey (default `Ctrl+Shift+4`).

---

## Project layout

```
src/HeyManNiceShot/
  Program.cs                    Avalonia AppBuilder entry point
  App.axaml(.cs)                Application, FluentTheme, lifecycle, crash handlers
  app.manifest                  PerMonitorV2 DPI awareness, longPathAware
  Theme/
    Tokens.axaml                Brushes, radii, spacing
    Styles.axaml                Buttons, toolbars, badges
  Capture/
    CaptureService.cs           Virtual-screen enum, multi-monitor region capture
    Direct3D11Helper.cs         D3D11 device + texture → SKBitmap (Vortice)
    MonitorCapture.cs           Windows.Graphics.Capture wrapper + interop
  Geometry/
    PhysicalRect / LogicalRect / MonitorInfo / VirtualScreen
  Editing/
    Document.cs                 Immutable layered document
    Renderer.cs                 Flatten-to-bitmap pipeline (Skia)
    UndoStack.cs                Generic snapshot stack
    Layer.cs                    All layer record types
    Tool.cs                     Abstract Tool + 7 concrete tools
  Hotkeys/
    HotkeyService.cs            RegisterHotKey + chord conversion
    MessageWindow.cs            Hidden message-only Win32 window
  Tray/
    TrayHost.cs                 Avalonia TrayIcon + NativeMenu
  Settings/
    AppSettings.cs              Settings record
    HotkeyChord.cs              Modifiers + Key with parse/format
    SettingsStore.cs            JSON persistence in %LOCALAPPDATA%
    SettingsWindow.axaml(.cs)   Functional settings UI
  Overlay/
    OverlayWindow.axaml(.cs)    Transparent virtual-screen-spanning window
    SelectionLayer.cs           Skia ICustomDrawOperation host
    OverlayResult.cs            Result record + action enum
  Editor/
    EditorWindow.axaml(.cs)     Three-pane editor
    DocumentSurface.cs          Skia ICustomDrawOperation host for documents
  Export/
    Exporter.cs                 PNG/JPEG encode + clipboard entry points
    Win32ClipboardImage.cs      OleSetClipboard PNG + DIB dual-format helper
  CaptureOrchestrator.cs        Hotkey → overlay → capture → action dispatch
  CrashLog.cs                   Rolling crash log in %LOCALAPPDATA%

tests/HeyManNiceShot.Tests/
  RendererTests / UndoStackTests / ToolTests / VirtualScreenTests / SettingsTests
```

---

## What works on day 1 vs what needs Windows verification

### Built and reviewed
- Project structure, Avalonia bootstrap, theme tokens
- Geometry types and virtual-screen math (covered by tests)
- Editing engine: immutable Document, Renderer, UndoStack, Tools (covered by tests)
- HotkeyChord parsing and SettingsStore round-trip (covered by tests)

### Built but unverified — needs a Windows box
- `TransparencyLevelHint="Transparent"` on the overlay window (the strategic claim)
- `IGraphicsCaptureItemInterop::CreateForMonitor` interop
- `Direct3D11CaptureFramePool` + staging-texture readback
- Multi-monitor virtual-screen sizing across mixed DPIs
- `Win32ClipboardImage` PNG + DIB dual format across Paint, Chrome, Office, Slack
- Tray icon visibility + native context menu
- Hotkey registration
- Mica backdrop on Win11 / fallback on Win10

The verification checklist is in
[`docs/verification.md`](docs/verification.md) — work through it on a real
Windows machine before declaring v1 done.

---

## Roadmap (Phase 2+)

Deliberately out of scope for this build:

- Post-capture floating thumbnail with quick actions ("CleanShot Overlay" pattern)
- Pin-to-screen floating window
- Magnifier loupe in overlay for pixel-precise selection
- Settings visual redesign (sidebar nav)
- Number/step tool, highlighter, gradient annotation
- Drag-out export
- Light theme
- Editor zoom (Ctrl+wheel)
- MSIX packaging / Microsoft Store
- Single-instance enforcement
- Cloud upload, scrolling capture, video/GIF, OCR

---

## License

TBD. Treat as All Rights Reserved until a `LICENSE` file is added.

# State Manifest
## Metadata: [g-suite-contained] Session Identity
- Timestamp: 2026-10-23T22:23:30Z
- Source Instance: d67e20f4-655a-4f40-bc3b-67881b84f04c
- Target Client: ide

## Anchors: [g-suite-contained] Permanent Context
- Project: g-suite-contained (`GSuiteContainer`)
- Core Objectives:
  - [x] Reverse-engineer and evaluate 6 Microsoft Store Google web app launcher packages for efficiency, capabilities, security, and resource footprint.
  - [x] Identify standout capabilities across all 6 packages (tabs, profile isolation, deep sub-links, ad-pruning, command palette, AI switcher).
  - [x] Consolidate the strongest architectural features into a single clean-room, zero-paywall, zero-telemetry desktop application (`GSuiteContainer.exe`).
  - [x] Publish the project source, build pipeline, and forensic evaluation to GitHub (`MonocIe/g-suite-contained`).
- Constraints:
  - Clean-room implementation using standard `.NET 8.0` WPF and Microsoft WebView2 APIs (no in-place binary patching of commercial Store apps).
  - Dark-mode glassmorphism (`#171717` canvas, `#202124` elevated surfaces, Windows 11 DWM Mica) and `Inter` / `Segoe UI Variable Display` typography.
  - Minimal disk footprint (`1.21 MB` total deployed binaries vs. `363.8 MB` in Store clones) by binding to the shared system `Microsoft.WindowsDesktop.App 8.0.x` runtime.

## Horizon: [g-suite-contained] Active Telemetry
- Last Completed Action: Created public GitHub repository `MonocIe/g-suite-contained`, pushed `README.md`, `.gitignore`, `GSuiteContainer.runtimeconfig.json`, `build.ps1`, `docs/FORENSIC_EVALUATION.md`, and `src/GSuiteContainer.cs` to `origin/main` (commit `c647e9350d010482e255af3b2f1ca85244668c64`), and synchronized local `.git` repository tracking `origin/main`.
- Next Steps (priority order):
  1. Optional: Add unpacked Chromium extension loader (`CoreWebView2Profile.AddBrowserExtensionAsync`) to load `uBlock Origin Lite (uBOL)` alongside the built-in 4-layer YouTube ad-pruner.
  2. Optional: Add a system tray icon (`NotifyIcon` / Win32 `Shell_NotifyIcon`) with minimize-to-tray and background badge counter support.
  3. Optional: Create a Windows Start Menu and Desktop `.lnk` shortcut installer script (`Install-Shortcut.ps1`) with a custom `.ico` asset.
  4. Optional: Publish a pre-compiled `.zip` release artifact to GitHub Releases (`v1.0.0`) on `MonocIe/g-suite-contained`.
- Active Blockers:
  - [None recorded]
- Volatile State:
  - `GitHubRepoUrl`: `https://github.com/MonocIe/g-suite-contained`
  - `GitHeadCommit`: `c647e9350d010482e255af3b2f1ca85244668c64`
  - `LocalWorkspace`: `c:\Users\georg\.gemini\config\projects\g-suite-contained`
  - `RuntimeDataDir`: `%LOCALAPPDATA%\GSuiteContainer`
  - `TargetFramework`: `net8.0-windows` (`C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.32`)
  - `WebView2Version`: `1.0.2903.40`

## Ledger: [g-suite-contained] Historical Decisions
- Decisions:
  - [2026-10-23T18:15:00Z] Classified the 6 Store packages into 4 codebase families (`Family A` SVGDesign/longwindows, `Family B` MakoApps Rust/Tauri, `Family C` Semivio .NET 4.7.2, `Family D` raghuapps/jananapps WinUI 3 clones) | Rationale: Static metadata inspection proved Apps 5 and 6 are 98% identical white-label clones (`Samuno.Billing`) and Apps 1 and 4 share the same base WPF classes.
  - [2026-10-23T18:25:00Z] Ranked App 2 (`9971MakoApps`) #1 for engineering/security (`10.63 MB`, Grade `A+`) and Apps 5/6 #1 for multi-tab workspace features (`363.8 MB`, Grade `B-`), while rejecting App 3 (`9601Semivio`, Grade `D`) | Rationale: App 3 contains obfuscated symbols, encrypted resources, and corrupted PE CLI headers unsuitable for a Google credential container.
  - [2026-10-23T18:45:00Z] Targeted shared `.NET 8.0` (`Microsoft.WindowsDesktop.App 8.0.32`) compiled via PowerShell 7's built-in Roslyn `CSharpCompilation` and a patched native `.NET 8.0` `apphost.exe` (`GSuiteContainer.exe`) | Rationale: Host machine has `.NET 8/9/10` runtimes installed without the `.NET SDK` CLI; Roslyn + native `apphost.exe` produces a `1.21 MB` native GUI executable with zero console flash and `0.53s` cold start.
  - [2026-10-23T18:53:00Z] Enforced UTF-8 with BOM parsing in `build.ps1` and Trusted-Types-safe DOM creation (`document.createElement` only, zero `innerHTML`) in `InjectedScripts.DocumentStartInitScript` | Rationale: Prevents Unicode mojibake in WPF window titles/buttons and avoids Google Workspace CSP `require-trusted-types-for 'script'` violations.
  - [2026-10-23T22:17:00Z] Published repository to `https://github.com/MonocIe/g-suite-contained` via GitHub MCP and bound local `.git` repo to `origin/main` | Rationale: Local `gh` CLI was unauthenticated, whereas the `github` MCP server had active write credentials for `MonocIe`.
- Dead Ends (do not reattempt):
  - `dotnet build` CLI invocation | Failure: Host has `.NET 8.0.32`, `9.0.21`, and `10.0.13` shared runtimes installed, but no `.NET SDK` (`dotnet --list-sdks` is empty); always compile via `pwsh -NoProfile -File .\build.ps1` using Roslyn `Microsoft.CodeAnalysis.CSharp`.
  - Child-process `LauncherStub.exe` spawning `dotnet.exe` with `CreateNoWindow = true` | Failure: Windows passes `STARTUPINFO.wShowWindow = SW_HIDE` to the child process, hiding the initial WPF window; use the native `.NET 8.0` `apphost.exe` binary slot at byte offset `141184` instead.
  - Calling GitHub MCP `push_files` on an uninitialized empty repository | Failure: GitHub Git Data API returns `409 Conflict: Git Repository is empty` until at least one file (`README.md`) is committed via `create_or_update_file`.
- Asset Registry:
  - `c:\Users\georg\.gemini\config\projects\g-suite-contained\src\GSuiteContainer.cs`: Complete C# source for `GSuiteContainer` (38 Google services + deep `SubItems`, multi-profile isolation, SHA-256 PIN lock, multi-tab strip, split view, mini-player, 4-layer YouTube ad-pruner, and AI companion drawer).
  - `c:\Users\georg\.gemini\config\projects\g-suite-contained\build.ps1`: Roslyn `.NET 8.0` build script that compiles `GSuiteContainer.dll` and patches `GSuiteContainer.exe`.
  - `c:\Users\georg\.gemini\config\projects\g-suite-contained\GSuiteContainer.exe`: Native `.NET 8.0` Windows GUI executable (`188.5 KB`).
  - `c:\Users\georg\.gemini\config\projects\g-suite-contained\GSuiteContainer.dll`: Compiled managed application assembly (`124.5 KB`).
  - `c:\Users\georg\.gemini\config\projects\g-suite-contained\docs\FORENSIC_EVALUATION.md`: Full comparative forensic report on the 6 Microsoft Store applications.
  - `C:\Users\georg\.gemini\antigravity\brain\d67e20f4-655a-4f40-bc3b-67881b84f04c\g_suite_container_evaluation.md`: Conversation brain artifact copy of the forensic evaluation.
  - `https://github.com/MonocIe/g-suite-contained`: Remote GitHub repository (`main` branch).

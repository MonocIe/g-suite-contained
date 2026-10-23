# G-Suite Contained (`GSuiteContainer`)

A lightweight (**1.21 MB**), zero-telemetry, 100% unlocked native `.NET 8.0` + `WebView2` desktop workspace container for Google Workspace and 38+ Google services.

Built via clean-room architectural consolidation of 6 Microsoft Store Google web app launcher packages (`MakoApps`, `raghuapps`, `jananapps`, `SVGDesignSolutions`, `27822longwindows`, and `SemivioTechnologies`).

---

## Core Capabilities

1. **Multi-Workspace Session Isolation + SHA-256 PIN Lock**:
   - Dedicated `CoreWebView2Environment` user-data folders per workspace (`%LOCALAPPDATA%\GSuiteContainer\Profiles\<WorkspaceId>`).
   - Built-in `Personal`, `Work / Corp`, and `Executive Vault` profiles plus unlimited custom profiles (`+ Profile`).
   - Automatic Google Account email detection badge and optional per-workspace SHA-256 PIN lock.
2. **True In-Window Multi-Tab Workspace + Dual-Pane Split View + Mini-Player**:
   - Multi-tab strip inside a single dark-mode glassmorphic window (`#171717` / `#202124` + Windows 11 DWM Mica).
   - **Dual-Pane Split View (`Alt+S`)**: Run any two Google services side-by-side with a draggable splitter.
   - **Always-on-Top PiP Mini-Player (`Alt+P`)**: Pop out YouTube, Google Meet, or YouTube Music into a floating desktop mini-player.
   - **Dedicated Popout Window (`Ctrl+Shift+N`)**: Tear off the current tab into a standalone window.
   - **One-Click Tab Hibernation (`zzz Hibernate Bg`)**: Puts background tabs to sleep (`about:blank`) to reclaim Chromium RAM while preserving state for instant wake-on-click.
3. **38 Built-In Google Services with Deep `SubItems` + Unlimited Custom Web Apps**:
   - Organized across 6 categories (`Core Productivity`, `Docs & Creation`, `Communication`, `Cloud, AI & Dev`, `Media, Maps & Research`, `Business & Analytics`) plus `Favorites` and `Custom Apps`.
   - Right-click or click the `▾` badge on any card or left-dock icon to jump directly into deep sub-routes (e.g., Gmail `Compose / Inbox / Starred / Sent / Drafts / Spam`, Drive `My Drive / Shared with me / Recent / Quota`, Cloud Console `IAM / APIs / Cloud Run / BigQuery / Billing`).
4. **Spotlight Command Palette (`Alt+Space` / `Ctrl+Shift+G` / `Ctrl+K`) & 12 Scoped Search Engines**:
   - System-wide Windows hotkey summons the container command palette from anywhere.
   - Prefix-driven omnibox and search pill selector supporting `Google Web (g)`, `YouTube (yt)`, `Gmail (m)`, `Google Drive (d)`, `Google Docs (doc)`, `Calendar (cal)`, `Google Maps (map)`, `Google Scholar (sch)`, `Gemini AI (ai)`, `Perplexity (px)`, `DuckDuckGo (ddg)`, and `GitHub (gh)`.
   - Footer Quick-Create bar for instant `+ New Doc`, `+ New Sheet`, `+ New Slide`, `+ New Form`, `+ New Event`, `+ New Meet`, and `+ New Note`.
5. **Trusted-Types-Safe Overlay & 4-Layer YouTube Ad/Tracker Shield**:
   - Zero-`innerHTML` DOM injection (`document.createElement` only) that never violates Google's `require-trusted-types-for 'script'` CSP on `accounts.google.com`.
   - Context-aware `Ctrl+[` / `Ctrl+]` Back/Forward navigation that ignores `INPUT`, `TEXTAREA`, and `isContentEditable` fields.
   - Network-layer blocking (`WebResourceRequested`) of `doubleclick.net`, `googlesyndication.com`, `googleadservices.com`, `youtube.com/api/stats/ads`, and `youtube.com/pagead/*`, paired with `ytInitialPlayerResponse` and `/youtubei/v1/player` JSON ad-slot pruning.
6. **Free Official AI Companion Drawer (`Alt+A`)**:
   - Collapsible right-hand drawer switching between official **Gemini**, **NotebookLM**, **Google AI Studio**, **ChatGPT**, **Claude**, and **Perplexity** web sessions using your own browser login.

---

## Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Alt + Space` or `Ctrl + Shift + G` | Global Windows Hotkey: Summon window & open Spotlight Command Palette |
| `Ctrl + K` | Open Spotlight Command Palette |
| `Ctrl + T` | Show 38-Service Launcher Hub |
| `Ctrl + W` | Close active tab |
| `Ctrl + L` | Focus Scoped Omnibox |
| `Alt + S` | Toggle Dual-Pane Split View |
| `Alt + A` | Toggle Free Official AI Companion Drawer |
| `Alt + P` | Pop active tab into Always-on-Top PiP Mini-Player |
| `Ctrl + Shift + N` | Open active tab in Dedicated Popout Window |
| `Ctrl + [` / `Ctrl + ]` | Navigate Back / Forward (input-safe) |

---

## Building from Source

Requires Windows 10/11 with `.NET 8.0 Desktop Runtime` (`Microsoft.WindowsDesktop.App 8.0.x`) and `PowerShell 7+` (`pwsh` with built-in Roslyn `Microsoft.CodeAnalysis.CSharp`).

```powershell
pwsh -NoProfile -File .\build.ps1
.\GSuiteContainer.exe
```

---

## Forensic Evaluation Summary of the 6 Store Apps

| Rank | Package | Stack | Disk Size | Security | Verdict |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | `9971MakoApps` (`g-app-launcher.exe`) | Rust + Tauri `2.11.5` + React 19 | `10.63 MB` | **A+** | Best engineering & ad-pruner; lacks tabs; `\$24.99` paywall |
| **2** | `32249raghuapps` (`gservices.exe`) & `27501jananapps` (`launcher.exe`) | `.NET 8.0` + WinUI 3 (`Samuno.Billing` clones) | `363.8 MB` | **B-** | Best multi-tab & workspace features; 98% identical white-label clones |
| **3** | `SVGDesignSolutions` (`GAppLauncher.exe`) | `.NET 8.0` WPF (Family A Clean) | `192.4 MB` | **A-** | Good profile isolation & free AI switcher; background WebView2 RAM overhead |
| **4** | `27822longwindows` (`GAppsServer.exe`) | `.NET 8.0` WPF (Family A Bloated) | `235.3 MB` | **C-** | Great 37-app `SubItems` catalog, but bundles `WH_MOUSE_LL` hook, PC cleaner & paid AI credits |
| **5** | `9601SemivioTechnologies` (`GLauncher.exe`) | `.NET Framework 4.7.2` x86 | `7.10 MB` | **D** | Obfuscated & corrupted PE metadata tables; avoid |

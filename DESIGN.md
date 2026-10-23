---
name: Google Workspace Material Design 3
colors:
  light-surface: '#F8FAFD'
  light-surface-container: '#FFFFFF'
  light-surface-variant: '#EFF3F8'
  light-primary: '#0B57D0'
  light-primary-container: '#D3E3FD'
  light-on-primary-container: '#041E49'
  light-on-surface: '#1F1F1F'
  light-on-surface-variant: '#444746'
  light-outline: '#E0E2E0'
  dark-surface: '#131314'
  dark-surface-container: '#1E1F20'
  dark-surface-variant: '#282A2C'
  dark-primary: '#A8C7FA'
  dark-primary-container: '#0842A0'
  dark-on-primary-container: '#D3E3FD'
  dark-on-surface: '#E3E3E3'
  dark-on-surface-variant: '#C4C7C5'
  dark-outline: '#333537'
typography:
  headline-md:
    fontFamily: Inter, Google Sans, Segoe UI Variable Display
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 28px
  title-md:
    fontFamily: Inter, Google Sans, Segoe UI Variable Display
    fontSize: 15px
    fontWeight: '500'
    lineHeight: 22px
  body-md:
    fontFamily: Inter, Roboto, Segoe UI
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: Inter, Roboto, Segoe UI
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
rounded:
  sm: 6px
  DEFAULT: 8px
  md: 12px
  lg: 16px
  full: 9999px
---

# G-Suite Container V2 | Stitch Material Design 3 Specification

A clean, minimal, native desktop Google Workspace container synchronized dynamically with Windows OS `prefers-color-scheme` (`AppsUseLightTheme`).

## 1. DESIGN SYSTEM (REQUIRED)
- **Platform**: Desktop (Windows 11 WPF + WebView2, Desktop-first)
- **Theme**: Dynamic OS Sync (`prefers-color-scheme: light` and `dark`)
- **Light Mode Tokens**:
  - Base Canvas Surface: Google Workspace Light (`#F8FAFD`)
  - Elevated / Card Surface: Crisp White (`#FFFFFF`)
  - Tonal Hover Surface: Soft Ice (`#EFF3F8`)
  - Active Pill / Tab Surface: Tonal Blue (`#D3E3FD`)
  - Primary Accent: Google Workspace Blue (`#0B57D0`)
  - Text Primary: Near Black (`#1F1F1F`)
  - Text Secondary: Muted Slate (`#444746`)
  - Border / Divider: Hairline Neutral (`#E0E2E0`)
- **Dark Mode Tokens**:
  - Base Canvas Surface: Workspace Obsidian (`#131314`)
  - Elevated / Card Surface: Elevated Charcoal (`#1E1F20`)
  - Tonal Hover Surface: Tonal Graphite (`#282A2C`)
  - Active Pill / Tab Surface: Deep Tonal Blue (`#0842A0`)
  - Primary Accent: Luminous Sky Blue (`#A8C7FA`)
  - Text Primary: Crisp Light (`#E3E3E3`)
  - Text Secondary: Muted Silver (`#C4C7C5`)
  - Border / Divider: Subtle Outline (`#333537`)

## 2. Page & Shell Structure
1. **Themed Window Chrome (`REQ-10`)**: DWM caption and text colors synced dynamically to `#F8FAFD` (Light) or `#131314` (Dark), eliminating the default Windows teal title bar. Main window icon bound to the official Google Apps for Work icon (`assets/app-icon.ico`).
2. **Top Header & Flex Tab Bar (`REQ-03`, `REQ-08`)**:
   - Left: Consolidated `"Profiles"` dropdown button displaying active account (`george.christosp@gmail.com`) and isolated WebView2 profile selector.
   - Center: Custom `FlexTabPanel` strip that shrinks open tabs proportionally (`42px` to `180px`) with official 16x16 product icons and zero horizontal scrollbar.
   - Right: Quick session actions (`Hibernate Bg`, `Lock / PIN`).
3. **Navigation & Omnibox Bar (`REQ-04`, `REQ-05`, `REQ-06`, `REQ-07`)**:
   - Left: Smooth collapsible left sidebar toggle (`☰`), `Apps Grid` toggle (`Ctrl + T`), Back (`←`), Forward (`→`), Reload (`↻`).
   - Center: Pill-shaped Material Design 3 Omnibox with scoped search engine selector (`g`, `yt`, `m`, `d`, `doc`, `cal`, `map`, `sch`, `ai`, `px`, `ddg`, `gh`) and Command Palette trigger (`⌘K / Alt+Space`).
   - Right: In-container tools (`Split View`, `PiP Mini`, `Browser` in-container tab, `AI Drawer`). `Shield: ON` UI and network ad-pruner hooks are completely stripped.
4. **Collapsible Left Favorites Dock & Container Hub (`REQ-01`, `REQ-02`, `REQ-10`)**:
   - Left Dock: 64px vertical icon rail displaying official full-color product icons from `assets/icons/` (smoothly collapsible to `0px`).
   - Container Hub: Clean horizontal product cards featuring official Google product SVG/PNG iconography on the left and clean service title on the right (matching the `Keep` reference card), with all 11 category filter pills and 3-letter text acronyms stripped.
   - Bottom Edge: Flush web viewport with the bottom RAM/telemetry footer completely removed.

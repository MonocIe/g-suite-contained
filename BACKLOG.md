# G-Suite Container | Parked QOL Backlog (@Jules Routing)

This backlog tracks non-blocking Quality-of-Life (QOL) enhancements parked outside the V2 Material Design 3 & `REQ-01` through `REQ-10` sprint scope.

## Parked Items (Post-V2 Sprint)

| ID | Feature | Scope & Technical Notes | Status |
| :--- | :--- | :--- | :--- |
| **`QOL-01`** | **Custom WebView2 Context Menus** | Intercept `CoreWebView2.ContextMenuRequested` to append custom actions (`Open Link in Split View`, `Send Selection to AI Drawer`, `Copy Clean URL`) alongside native Chromium context menu items. | Parked |
| **`QOL-02`** | **Native Windows Toast Notifications** | Route `window.Notification` bridge payloads from `CoreWebView2.WebMessageReceived` into Windows 10/11 Action Center (`Windows.UI.Notifications.ToastNotificationManager`) with click-to-activate tab routing. | Parked |
| **`QOL-03`** | **System Tray Integration** | Add Win32/Forms `NotifyIcon` support with minimize-to-tray option, quick-launch context menu, and unread badge indicator for Gmail/Chat. | Parked |
| **`QOL-04`** | **Custom WebView2 Runtime Flags UI** | Expose an advanced settings modal allowing power users to toggle custom Chromium flags (`--enable-features=WebUIDarkMode`, hardware acceleration, custom user-agent overrides) persisted in `config.json`. | Parked |

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace GSuiteContainer
{
    // =========================================================================
    // DATA MODELS & CONFIGURATION PERSISTENCE
    // =========================================================================
    public class SubItemDef
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class GoogleAppDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "Workspace";
        public string Url { get; set; } = "";
        public string IconFile { get; set; } = "google-apps-for-work.png";
        public string AccentHex { get; set; } = "#0B57D0";
        public bool IsFavorite { get; set; }
        public bool IsCustom { get; set; }
        public List<SubItemDef> SubItems { get; set; } = new List<SubItemDef>();
    }

    public class SearchEngineDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Prefix { get; set; } = "";
        public string UrlTemplate { get; set; } = "";
        public string IconFile { get; set; } = "google-apps-for-work.png";
    }

    public class WorkspaceProfile
    {
        public string Id { get; set; } = "personal";
        public string Name { get; set; } = "Personal";
        public string PinSha256 { get; set; } = "";
        public string DetectedAccount { get; set; } = "george.christosp@gmail.com";
        public List<string> OpenTabUrls { get; set; } = new List<string>();
    }

    public class AppConfig
    {
        public string ActiveWorkspaceId { get; set; } = "personal";
        public bool SidebarCollapsed { get; set; } = false;
        public bool AiDrawerOpen { get; set; } = false;
        public string ActiveAiProviderUrl { get; set; } = "https://gemini.google.com/app";
        public string DefaultSearchEngineId { get; set; } = "google";
        public List<string> FavoriteAppIds { get; set; } = new List<string>
        {
            "gmail", "calendar", "drive", "docs", "sheets", "keep", "meet", "youtube", "gemini", "notebooklm"
        };
        public List<GoogleAppDef> CustomApps { get; set; } = new List<GoogleAppDef>();
        public List<WorkspaceProfile> Workspaces { get; set; } = new List<WorkspaceProfile>
        {
            new WorkspaceProfile { Id = "personal", Name = "Personal", DetectedAccount = "george.christosp@gmail.com" },
            new WorkspaceProfile { Id = "work", Name = "Work / Corp", DetectedAccount = "george@panag.io" },
            new WorkspaceProfile { Id = "executive", Name = "Executive Vault", DetectedAccount = "george.christosp@gmail.com" }
        };
    }

    public class BrowserTabEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "New Tab";
        public string Url { get; set; } = "about:blank";
        public string IconFile { get; set; } = "google-apps-for-work.png";
        public bool IsHibernated { get; set; }
        public WebView2? WebView { get; set; }
        public Border? TabChipBorder { get; set; }
        public TextBlock? TabTitleText { get; set; }
        public TextBlock? TabStateBadge { get; set; }
    }

    // =========================================================================
    // CUSTOM FLEX TAB PANEL (REQ-03: CSS FLEX-LIKE TAB SHRINKING, ZERO SCROLLBAR)
    // =========================================================================
    public class FlexTabPanel : Panel
    {
        public double MinTabWidth { get; set; } = 44.0;
        public double MaxTabWidth { get; set; } = 190.0;

        protected override Size MeasureOverride(Size availableSize)
        {
            int count = InternalChildren.Count;
            if (count == 0) return new Size(0, 0);

            double availW = double.IsInfinity(availableSize.Width) ? 1200.0 : availableSize.Width;
            double fixedWidth = 0;
            int flexCount = 0;
            double maxHeight = 34.0;

            for (int i = 0; i < count; i++)
            {
                var child = InternalChildren[i];
                bool isFixed = (i == 0 || i == count - 1);
                if (isFixed)
                {
                    child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                    fixedWidth += child.DesiredSize.Width;
                    if (child.DesiredSize.Height > maxHeight) maxHeight = child.DesiredSize.Height;
                }
                else
                {
                    flexCount++;
                }
            }

            if (flexCount > 0)
            {
                double remaining = Math.Max(0, availW - fixedWidth);
                double eachW = Math.Clamp(remaining / flexCount, MinTabWidth, MaxTabWidth);
                for (int i = 1; i < count - 1; i++)
                {
                    var child = InternalChildren[i];
                    child.Measure(new Size(eachW, availableSize.Height));
                    if (child.DesiredSize.Height > maxHeight) maxHeight = child.DesiredSize.Height;
                }
            }

            return new Size(Math.Min(availW, fixedWidth + flexCount * MaxTabWidth), maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int count = InternalChildren.Count;
            if (count == 0) return finalSize;

            double fixedWidth = 0;
            int flexCount = 0;

            for (int i = 0; i < count; i++)
            {
                bool isFixed = (i == 0 || i == count - 1);
                if (isFixed)
                    fixedWidth += InternalChildren[i].DesiredSize.Width;
                else
                    flexCount++;
            }

            double remaining = Math.Max(0, finalSize.Width - fixedWidth);
            double eachW = flexCount > 0 ? Math.Clamp(remaining / flexCount, MinTabWidth, MaxTabWidth) : 0;

            double x = 0;
            for (int i = 0; i < count; i++)
            {
                var child = InternalChildren[i];
                bool isFixed = (i == 0 || i == count - 1);
                double w = isFixed ? child.DesiredSize.Width : eachW;
                double h = child.DesiredSize.Height > 0 ? child.DesiredSize.Height : finalSize.Height;
                double y = Math.Max(0, (finalSize.Height - h) / 2.0);
                child.Arrange(new Rect(x, y, w, h));
                x += w;
            }

            return finalSize;
        }
    }

    // =========================================================================
    // STITCH MATERIAL DESIGN 3 DYNAMIC OS THEME TOKENS
    // =========================================================================
    public static class Md3Theme
    {
        public static bool IsDark { get; private set; } = DetectWindowsDarkMode();

        // Surface & Primary Tokens synced to OS default
        public static SolidColorBrush Surface { get; private set; } = null!;
        public static SolidColorBrush SurfaceContainer { get; private set; } = null!;
        public static SolidColorBrush SurfaceVariant { get; private set; } = null!;
        public static SolidColorBrush Primary { get; private set; } = null!;
        public static SolidColorBrush PrimaryContainer { get; private set; } = null!;
        public static SolidColorBrush OnPrimaryContainer { get; private set; } = null!;
        public static SolidColorBrush TextPrimary { get; private set; } = null!;
        public static SolidColorBrush TextSecondary { get; private set; } = null!;
        public static SolidColorBrush Outline { get; private set; } = null!;

        static Md3Theme()
        {
            RefreshPalette();
        }

        public static bool DetectWindowsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    object? val = key.GetValue("AppsUseLightTheme");
                    if (val is int i) return i == 0;
                }
            }
            catch { }
            return false;
        }

        public static bool SyncWithOsTheme()
        {
            bool nowDark = DetectWindowsDarkMode();
            if (nowDark != IsDark || Surface == null)
            {
                IsDark = nowDark;
                RefreshPalette();
                return true;
            }
            return false;
        }

        public static void RefreshPalette()
        {
            if (IsDark)
            {
                // Official Google Workspace Material Design 3 Dark Tokens
                Surface            = BrushFromHex("#131314");
                SurfaceContainer   = BrushFromHex("#1E1F20");
                SurfaceVariant     = BrushFromHex("#282A2C");
                Primary            = BrushFromHex("#A8C7FA");
                PrimaryContainer   = BrushFromHex("#0842A0");
                OnPrimaryContainer = BrushFromHex("#D3E3FD");
                TextPrimary        = BrushFromHex("#E3E3E3");
                TextSecondary      = BrushFromHex("#C4C7C5");
                Outline            = BrushFromHex("#333537");
            }
            else
            {
                // Official Google Workspace Material Design 3 Light Tokens
                Surface            = BrushFromHex("#F8FAFD");
                SurfaceContainer   = BrushFromHex("#FFFFFF");
                SurfaceVariant     = BrushFromHex("#EFF3F8");
                Primary            = BrushFromHex("#0B57D0");
                PrimaryContainer   = BrushFromHex("#D3E3FD");
                OnPrimaryContainer = BrushFromHex("#041E49");
                TextPrimary        = BrushFromHex("#1F1F1F");
                TextSecondary      = BrushFromHex("#444746");
                Outline            = BrushFromHex("#E0E2E0");
            }
        }

        public static SolidColorBrush BrushFromHex(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }

    // =========================================================================
    // CATALOG & ENGINE REGISTRY (REQ-01 & REQ-10: OFFICIAL DRIVE ICONOGRAPHY)
    // =========================================================================
    public static class CatalogRegistry
    {
        public static List<SearchEngineDef> GetSearchEngines() => new List<SearchEngineDef>
        {
            new SearchEngineDef { Id = "google", Name = "Google Web", Prefix = "g", UrlTemplate = "https://www.google.com/search?q={0}", IconFile = "google-apps-for-work.png" },
            new SearchEngineDef { Id = "youtube", Name = "YouTube", Prefix = "yt", UrlTemplate = "https://www.youtube.com/results?search_query={0}", IconFile = "youtube.png" },
            new SearchEngineDef { Id = "gmail", Name = "Gmail", Prefix = "m", UrlTemplate = "https://mail.google.com/mail/u/0/#search/{0}", IconFile = "gmail.png" },
            new SearchEngineDef { Id = "drive", Name = "Drive", Prefix = "d", UrlTemplate = "https://drive.google.com/drive/search?q={0}", IconFile = "google-drive.png" },
            new SearchEngineDef { Id = "docs", Name = "Docs", Prefix = "doc", UrlTemplate = "https://docs.google.com/document/u/0/?q={0}", IconFile = "google-docs.png" },
            new SearchEngineDef { Id = "calendar", Name = "Calendar", Prefix = "cal", UrlTemplate = "https://calendar.google.com/calendar/u/0/r/search?q={0}", IconFile = "google-calendar.png" },
            new SearchEngineDef { Id = "maps", Name = "Maps", Prefix = "map", UrlTemplate = "https://www.google.com/maps/search/{0}", IconFile = "google-maps.png" },
            new SearchEngineDef { Id = "scholar", Name = "Scholar", Prefix = "sch", UrlTemplate = "https://scholar.google.com/scholar?q={0}", IconFile = "google-scholar.png" },
            new SearchEngineDef { Id = "gemini", Name = "Gemini", Prefix = "ai", UrlTemplate = "https://gemini.google.com/app?q={0}", IconFile = "google-gemini.png" }
        };

        public static List<(string Name, string Url, string IconFile)> GetAiProviders() => new List<(string, string, string)>
        {
            ("Gemini", "https://gemini.google.com/app", "google-gemini.png"),
            ("NotebookLM", "https://notebooklm.google.com/", "gemini-notebook.png"),
            ("AI Studio", "https://aistudio.google.com/", "google-ai-studio.png"),
            ("ChatGPT", "https://chatgpt.com/", "google-apps-for-work.png"),
            ("Claude", "https://claude.ai/", "google-apps-for-work.png"),
            ("Perplexity", "https://www.perplexity.ai/", "google-apps-for-work.png")
        };

        public static List<GoogleAppDef> GetBuiltInCatalog() => new List<GoogleAppDef>
        {
            new GoogleAppDef {
                Id = "gmail", Name = "Gmail", IconFile = "gmail.png",
                Url = "https://mail.google.com/mail/u/0/#inbox",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Compose Email", Url = "https://mail.google.com/mail/u/0/#inbox?compose=new" },
                    new SubItemDef { Name = "Inbox", Url = "https://mail.google.com/mail/u/0/#inbox" },
                    new SubItemDef { Name = "Starred", Url = "https://mail.google.com/mail/u/0/#starred" },
                    new SubItemDef { Name = "Sent Mail", Url = "https://mail.google.com/mail/u/0/#sent" },
                    new SubItemDef { Name = "Drafts", Url = "https://mail.google.com/mail/u/0/#drafts" },
                    new SubItemDef { Name = "All Mail", Url = "https://mail.google.com/mail/u/0/#all" }
                }
            },
            new GoogleAppDef {
                Id = "calendar", Name = "Calendar", IconFile = "google-calendar.png",
                Url = "https://calendar.google.com/calendar/u/0/r",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ New Event", Url = "https://calendar.google.com/calendar/u/0/r/eventedit" },
                    new SubItemDef { Name = "Day View", Url = "https://calendar.google.com/calendar/u/0/r/day" },
                    new SubItemDef { Name = "Week View", Url = "https://calendar.google.com/calendar/u/0/r/week" },
                    new SubItemDef { Name = "Month View", Url = "https://calendar.google.com/calendar/u/0/r/month" }
                }
            },
            new GoogleAppDef {
                Id = "drive", Name = "Drive", IconFile = "google-drive.png",
                Url = "https://drive.google.com/drive/my-drive",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "My Drive", Url = "https://drive.google.com/drive/my-drive" },
                    new SubItemDef { Name = "Shared with me", Url = "https://drive.google.com/drive/shared-with-me" },
                    new SubItemDef { Name = "Recent Files", Url = "https://drive.google.com/drive/recent" },
                    new SubItemDef { Name = "Starred", Url = "https://drive.google.com/drive/starred" }
                }
            },
            new GoogleAppDef {
                Id = "docs", Name = "Docs", IconFile = "google-docs.png",
                Url = "https://docs.google.com/document/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Doc", Url = "https://docs.google.com/document/create" },
                    new SubItemDef { Name = "All Documents", Url = "https://docs.google.com/document/u/0/" }
                }
            },
            new GoogleAppDef {
                Id = "sheets", Name = "Sheets", IconFile = "google-sheets.png",
                Url = "https://docs.google.com/spreadsheets/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Sheet", Url = "https://docs.google.com/spreadsheets/create" },
                    new SubItemDef { Name = "All Spreadsheets", Url = "https://docs.google.com/spreadsheets/u/0/" }
                }
            },
            new GoogleAppDef {
                Id = "slides", Name = "Slides", IconFile = "google-slides.png",
                Url = "https://docs.google.com/presentation/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Presentation", Url = "https://docs.google.com/presentation/create" },
                    new SubItemDef { Name = "All Presentations", Url = "https://docs.google.com/presentation/u/0/" }
                }
            },
            new GoogleAppDef {
                Id = "keep", Name = "Keep", IconFile = "google-keep.png",
                Url = "https://keep.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Notes", Url = "https://keep.google.com/#home" },
                    new SubItemDef { Name = "Reminders", Url = "https://keep.google.com/#reminders" },
                    new SubItemDef { Name = "Archive", Url = "https://keep.google.com/#archive" }
                }
            },
            new GoogleAppDef {
                Id = "tasks", Name = "Tasks", IconFile = "google-tasks.png",
                Url = "https://tasks.google.com/embed/?origin=https://calendar.google.com&fullWidth=1"
            },
            new GoogleAppDef {
                Id = "meet", Name = "Meet", IconFile = "google-meet.png",
                Url = "https://meet.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Start Instant Meeting", Url = "https://meet.google.com/new" },
                    new SubItemDef { Name = "Meet Home", Url = "https://meet.google.com/" }
                }
            },
            new GoogleAppDef {
                Id = "chat", Name = "Chat", IconFile = "google-chat.png",
                Url = "https://mail.google.com/chat/u/0/#chat/home"
            },
            new GoogleAppDef {
                Id = "gemini", Name = "Gemini", IconFile = "google-gemini.png",
                Url = "https://gemini.google.com/app",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Gemini Chat", Url = "https://gemini.google.com/app" },
                    new SubItemDef { Name = "Gems Manager", Url = "https://gemini.google.com/gems/view" }
                }
            },
            new GoogleAppDef {
                Id = "notebooklm", Name = "NotebookLM", IconFile = "gemini-notebook.png",
                Url = "https://notebooklm.google.com/"
            },
            new GoogleAppDef {
                Id = "aistudio", Name = "AI Studio", IconFile = "google-ai-studio.png",
                Url = "https://aistudio.google.com/prompts/new_chat",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Prompt Playground", Url = "https://aistudio.google.com/prompts/new_chat" },
                    new SubItemDef { Name = "API Keys", Url = "https://aistudio.google.com/app/apikey" }
                }
            },
            new GoogleAppDef {
                Id = "forms", Name = "Forms", IconFile = "google-forms.png",
                Url = "https://docs.google.com/forms/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Form", Url = "https://docs.google.com/forms/create" },
                    new SubItemDef { Name = "All Forms", Url = "https://docs.google.com/forms/u/0/" }
                }
            },
            new GoogleAppDef {
                Id = "sites", Name = "Sites", IconFile = "google-sites.png",
                Url = "https://sites.google.com/new"
            },
            new GoogleAppDef {
                Id = "drawings", Name = "Drawings", IconFile = "google-drawings.png",
                Url = "https://docs.google.com/drawings/create"
            },
            new GoogleAppDef {
                Id = "contacts", Name = "Contacts", IconFile = "google-contacts.png",
                Url = "https://contacts.google.com/"
            },
            new GoogleAppDef {
                Id = "account", Name = "Account", IconFile = "google-account.png",
                Url = "https://myaccount.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Security & Sign-in", Url = "https://myaccount.google.com/security" },
                    new SubItemDef { Name = "Data & Privacy", Url = "https://myaccount.google.com/data-and-privacy" },
                    new SubItemDef { Name = "Password Manager", Url = "https://passwords.google.com/" }
                }
            },
            new GoogleAppDef {
                Id = "voice", Name = "Voice", IconFile = "google-voice.png",
                Url = "https://voice.google.com/u/0/calls",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Calls", Url = "https://voice.google.com/u/0/calls" },
                    new SubItemDef { Name = "Messages", Url = "https://voice.google.com/u/0/messages" },
                    new SubItemDef { Name = "Voicemail", Url = "https://voice.google.com/u/0/voicemail" }
                }
            },
            new GoogleAppDef {
                Id = "messages", Name = "Messages", IconFile = "google-messages.png",
                Url = "https://messages.google.com/web/conversations"
            },
            new GoogleAppDef {
                Id = "groups", Name = "Groups", IconFile = "google-groups.png",
                Url = "https://groups.google.com/my-groups"
            },
            new GoogleAppDef {
                Id = "cloud", Name = "Cloud Console", IconFile = "google-cloud-console.png",
                Url = "https://console.cloud.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Dashboard", Url = "https://console.cloud.google.com/home/dashboard" },
                    new SubItemDef { Name = "IAM & Admin", Url = "https://console.cloud.google.com/iam-admin" },
                    new SubItemDef { Name = "APIs & Services", Url = "https://console.cloud.google.com/apis/dashboard" },
                    new SubItemDef { Name = "Cloud Run", Url = "https://console.cloud.google.com/run" },
                    new SubItemDef { Name = "BigQuery", Url = "https://console.cloud.google.com/bigquery" }
                }
            },
            new GoogleAppDef {
                Id = "colab", Name = "Colab", IconFile = "google-colab.png",
                Url = "https://colab.research.google.com/"
            },
            new GoogleAppDef {
                Id = "firebase", Name = "Firebase", IconFile = "firebase-console.png",
                Url = "https://console.firebase.google.com/"
            },
            new GoogleAppDef {
                Id = "appsscript", Name = "Apps Script", IconFile = "apps-script.png",
                Url = "https://script.google.com/home"
            },
            new GoogleAppDef {
                Id = "developers", Name = "Developers", IconFile = "google-developers.png",
                Url = "https://developers.google.com/"
            },
            new GoogleAppDef {
                Id = "youtube", Name = "YouTube", IconFile = "youtube.png",
                Url = "https://www.youtube.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Home Feed", Url = "https://www.youtube.com/" },
                    new SubItemDef { Name = "Subscriptions", Url = "https://www.youtube.com/feed/subscriptions" },
                    new SubItemDef { Name = "Watch Later", Url = "https://www.youtube.com/playlist?list=WL" },
                    new SubItemDef { Name = "History", Url = "https://www.youtube.com/feed/history" }
                }
            },
            new GoogleAppDef {
                Id = "ytmusic", Name = "YouTube Music", IconFile = "youtube-music.png",
                Url = "https://music.youtube.com/"
            },
            new GoogleAppDef {
                Id = "photos", Name = "Photos", IconFile = "google-photos.png",
                Url = "https://photos.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Photos Stream", Url = "https://photos.google.com/" },
                    new SubItemDef { Name = "Albums", Url = "https://photos.google.com/albums" },
                    new SubItemDef { Name = "Favorites", Url = "https://photos.google.com/favorites" }
                }
            },
            new GoogleAppDef {
                Id = "maps", Name = "Maps", IconFile = "google-maps.png",
                Url = "https://www.google.com/maps"
            },
            new GoogleAppDef {
                Id = "earth", Name = "Earth", IconFile = "google-earth.png",
                Url = "https://earth.google.com/web/"
            },
            new GoogleAppDef {
                Id = "translate", Name = "Translate", IconFile = "google-translate.png",
                Url = "https://translate.google.com/"
            },
            new GoogleAppDef {
                Id = "scholar", Name = "Scholar", IconFile = "google-scholar.png",
                Url = "https://scholar.google.com/"
            },
            new GoogleAppDef {
                Id = "news", Name = "News", IconFile = "google-news.png",
                Url = "https://news.google.com/"
            },
            new GoogleAppDef {
                Id = "travel", Name = "Travel", IconFile = "flights-and-travel.png",
                Url = "https://www.google.com/travel/flights",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Flights", Url = "https://www.google.com/travel/flights" },
                    new SubItemDef { Name = "Hotels", Url = "https://www.google.com/travel/hotels" },
                    new SubItemDef { Name = "Trips", Url = "https://www.google.com/travel/" }
                }
            },
            new GoogleAppDef {
                Id = "fonts", Name = "Fonts", IconFile = "google-fonts.png",
                Url = "https://fonts.google.com/"
            },
            new GoogleAppDef {
                Id = "admin", Name = "Admin", IconFile = "workspace-admin.png",
                Url = "https://admin.google.com/"
            },
            new GoogleAppDef {
                Id = "finance", Name = "Finance", IconFile = "google-finance.png",
                Url = "https://www.google.com/finance/"
            },
            new GoogleAppDef {
                Id = "analytics", Name = "Analytics", IconFile = "google-analytics.png",
                Url = "https://analytics.google.com/"
            },
            new GoogleAppDef {
                Id = "trends", Name = "Trends", IconFile = "google-trends.png",
                Url = "https://trends.google.com/trends/"
            },
            new GoogleAppDef {
                Id = "searchconsole", Name = "Search Console", IconFile = "search-console.png",
                Url = "https://search.google.com/search-console"
            }
        };
    }

    // =========================================================================
    // TRUSTED-TYPES-SAFE INJECTED JS (REQ-05: STRIPPED 4-LAYER YOUTUBE AD-PRUNER)
    // =========================================================================
    public static class InjectedScripts
    {
        public const string DocumentStartInitScript = @"
(function() {
    if (window.__gsuiteContainerInit) return;
    window.__gsuiteContainerInit = true;

    // 1. Keyboard Navigation Bridge (Ctrl+[ Back, Ctrl+] Forward, Ctrl+T Hub, Ctrl+K / Alt+Space Palette)
    window.addEventListener('keydown', function(e) {
        var el = document.activeElement;
        var isEditable = el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.isContentEditable);
        if (!isEditable && e.ctrlKey && !e.shiftKey && !e.altKey) {
            if (e.key === '[') { e.preventDefault(); history.back(); }
            else if (e.key === ']') { e.preventDefault(); history.forward(); }
        }
        if (e.ctrlKey && !e.shiftKey && !e.altKey && (e.key === 't' || e.key === 'T')) {
            e.preventDefault();
            try { window.chrome.webview.postMessage('cmd:hub'); } catch (_) {}
        }
        if ((e.ctrlKey && (e.key === 'k' || e.key === 'K')) || (e.altKey && e.code === 'Space')) {
            e.preventDefault();
            try { window.chrome.webview.postMessage('cmd:palette'); } catch (_) {}
        }
    }, true);

    // 2. Account Observation (Detects signed-in Google email without DOM innerHTML)
    function detectGoogleAccount() {
        try {
            var nodes = document.querySelectorAll('[aria-label*=""@""], [data-email]');
            for (var i = 0; i < nodes.length; i++) {
                var email = nodes[i].getAttribute('data-email') || nodes[i].getAttribute('aria-label') || '';
                var m = email.match(/[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/);
                if (m && m[0]) {
                    window.chrome.webview.postMessage('account:' + m[0]);
                    return;
                }
            }
        } catch (_) {}
    }
    window.addEventListener('DOMContentLoaded', function() {
        setTimeout(detectGoogleAccount, 1800);
    });
})();
";
    }

    // =========================================================================
    // MAIN WINDOW | GOOGLE WORKSPACE MATERIAL DESIGN 3 CONTAINER
    // =========================================================================
    public class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        private const int HOTKEY_ID_ALT_SPACE = 9001;
        private const int HOTKEY_ID_CTRL_SHIFT_G = 9002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_SPACE = 0x20;
        private const uint VK_G = 0x47;

        private static readonly FontFamily UiFont = new FontFamily("Inter, Segoe UI Variable Display, Segoe UI");

        // State & Storage
        private readonly string _appRoot;
        private readonly string _iconsDir;
        private readonly string _dataRoot;
        private readonly string _configPath;
        private AppConfig _config = new AppConfig();
        private readonly List<GoogleAppDef> _allApps = new List<GoogleAppDef>();
        private readonly List<SearchEngineDef> _searchEngines = CatalogRegistry.GetSearchEngines();
        private SearchEngineDef _activeSearchEngine;
        private readonly Dictionary<string, CoreWebView2Environment> _envCache = new Dictionary<string, CoreWebView2Environment>();
        private readonly Dictionary<string, BitmapImage> _iconCache = new Dictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

        // Tabs & View State
        private readonly List<BrowserTabEntry> _tabs = new List<BrowserTabEntry>();
        private BrowserTabEntry? _activeTab;
        private bool _splitViewActive;
        private WebView2? _splitWebView;
        private WebView2? _aiWebView;
        private bool _workspaceLocked;

        // UI References for Dynamic Theme & Layout Updates
        private Grid _rootGrid = null!;
        private Border _topBarBorder = null!;
        private Button _profilesDropdownBtn = null!;
        private TextBlock _profilesDropdownLabel = null!;
        private FlexTabPanel _tabStripPanel = null!;
        private Border _navBarBorder = null!;
        private Button _sidebarToggleBtn = null!;
        private Button _appsGridBtn = null!;
        private Border _omniBorder = null!;
        private Button _searchEnginePillBtn = null!;
        private TextBox _omniboxInput = null!;
        private Border _sidebarBorder = null!;
        private StackPanel _sidebarFavoritesPanel = null!;
        private Grid _contentHostGrid = null!;
        private Grid _homeDashboardOverlay = null!;
        private TextBlock _hubTitleText = null!;
        private TextBlock _hubSubtitleText = null!;
        private Border _hubSearchBorder = null!;
        private TextBox _homeFilterBox = null!;
        private WrapPanel _homeAppsWrapPanel = null!;
        private Grid _splitPaneContainer = null!;
        private GridSplitter _splitSplitter = null!;
        private Border _splitRightBorder = null!;
        private TextBox _splitUrlBox = null!;
        private Border _aiDrawerBorder = null!;
        private StackPanel _aiProviderTabsPanel = null!;
        private Grid _aiWebViewHost = null!;
        private Grid _commandPaletteOverlay = null!;
        private Border _commandPaletteModal = null!;
        private TextBox _paletteSearchBox = null!;
        private StackPanel _paletteResultsPanel = null!;
        private Grid _pinLockOverlay = null!;
        private Border _pinLockCard = null!;
        private PasswordBox _pinInputBox = null!;
        private TextBlock _pinPromptLabel = null!;
        private Border _toastBanner = null!;
        private TextBlock _toastText = null!;

        public MainWindow()
        {
            _appRoot = AppDomain.CurrentDomain.BaseDirectory;
            _iconsDir = System.IO.Path.Combine(_appRoot, "assets", "icons");

            _dataRoot = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GSuiteContainer");
            Directory.CreateDirectory(_dataRoot);
            _configPath = System.IO.Path.Combine(_dataRoot, "config.json");

            LoadConfig();
            _activeSearchEngine = _searchEngines.FirstOrDefault(s => s.Id == _config.DefaultSearchEngineId) ?? _searchEngines[0];
            RebuildAppCatalog();

            Title = "Google Workspace";
            Width = 1480;
            Height = 930;
            MinWidth = 920;
            MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Md3Theme.Surface;
            Foreground = Md3Theme.TextPrimary;
            FontFamily = UiFont;

            // REQ-10: Set official Google Apps for Work icon on Main Window
            LoadMainWindowIcon();

            BuildUserInterface();
            RegisterKeyboardShortcuts();

            Loaded += OnMainWindowLoaded;
            Closing += (s, e) => SaveConfig();
        }

        private void LoadMainWindowIcon()
        {
            try
            {
                string icoPath = System.IO.Path.Combine(_appRoot, "assets", "app-icon.ico");
                string pngPath = System.IO.Path.Combine(_iconsDir, "google-apps-for-work.png");
                if (File.Exists(pngPath))
                {
                    Icon = LoadIconBitmap("google-apps-for-work.png");
                }
                else if (File.Exists(icoPath))
                {
                    Icon = BitmapFrame.Create(new Uri(icoPath, UriKind.Absolute));
                }
            }
            catch { }
        }

        private BitmapImage? LoadIconBitmap(string iconFileName)
        {
            if (string.IsNullOrWhiteSpace(iconFileName)) iconFileName = "google-apps-for-work.png";
            if (_iconCache.TryGetValue(iconFileName, out var cached)) return cached;

            try
            {
                string fullPath = System.IO.Path.Combine(_iconsDir, iconFileName);
                if (!File.Exists(fullPath))
                {
                    fullPath = System.IO.Path.Combine(_iconsDir, "google-apps-for-work.png");
                }
                if (File.Exists(fullPath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    _iconCache[iconFileName] = bmp;
                    return bmp;
                }
            }
            catch { }
            return null;
        }

        private Image CreateCrispIconImage(string iconFileName, double size, Thickness margin = default)
        {
            var img = new Image
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Source = LoadIconBitmap(iconFileName)
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            return img;
        }

        // =====================================================================
        // CONFIG & CATALOG HYDRATION
        // =====================================================================
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                    if (loaded != null) _config = loaded;
                }
            }
            catch { _config = new AppConfig(); }

            foreach (var ws in _config.Workspaces)
            {
                if (string.IsNullOrWhiteSpace(ws.DetectedAccount))
                {
                    ws.DetectedAccount = "george.christosp@gmail.com";
                }
            }
        }

        private void SaveConfig()
        {
            try
            {
                var ws = GetActiveWorkspace();
                ws.OpenTabUrls = _tabs.Where(t => !string.IsNullOrWhiteSpace(t.Url) && t.Url != "about:blank")
                                      .Select(t => t.Url).ToList();
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private WorkspaceProfile GetActiveWorkspace()
        {
            return _config.Workspaces.FirstOrDefault(w => w.Id == _config.ActiveWorkspaceId)
                ?? _config.Workspaces[0];
        }

        private void RebuildAppCatalog()
        {
            _allApps.Clear();
            foreach (var app in CatalogRegistry.GetBuiltInCatalog())
            {
                app.IsFavorite = _config.FavoriteAppIds.Contains(app.Id);
                _allApps.Add(app);
            }
            foreach (var custom in _config.CustomApps)
            {
                custom.IsCustom = true;
                if (string.IsNullOrWhiteSpace(custom.IconFile))
                    custom.IconFile = "google-apps-for-work.png";
                custom.IsFavorite = _config.FavoriteAppIds.Contains(custom.Id);
                _allApps.Add(custom);
            }
        }

        // =====================================================================
        // UI CONSTRUCTION (GOOGLE WORKSPACE MATERIAL DESIGN 3)
        // =====================================================================
        private void BuildUserInterface()
        {
            _rootGrid = new Grid { Background = Md3Theme.Surface };
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // Row 0: Profiles Dropdown + Flex Tab Bar
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) }); // Row 1: Sidebar Toggle, Navigation, MD3 Omnibox & Tools
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 2: Main Body (REQ-02: No Footer Row)

            // -----------------------------------------------------------------
            // ROW 0: CONSOLIDATED PROFILES DROPDOWN + FLEX TAB STRIP
            // -----------------------------------------------------------------
            _topBarBorder = new Border
            {
                Background = Md3Theme.Surface,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 0, 10, 0)
            };
            var topBarGrid = new Grid();
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // REQ-08: Profiles Dropdown
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // REQ-03: Flex Tabs
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Session Controls

            // REQ-08: Single Consolidated "Profiles" Dropdown Button
            _profilesDropdownBtn = new Button
            {
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Switch or manage isolated Google Workspace profiles"
            };
            var profStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            profStack.Children.Add(CreateCrispIconImage("google-account.png", 16, new Thickness(0, 0, 7, 0)));
            _profilesDropdownLabel = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            profStack.Children.Add(_profilesDropdownLabel);
            _profilesDropdownBtn.Content = profStack;
            _profilesDropdownBtn.Click += OnProfilesDropdownClick;
            Grid.SetColumn(_profilesDropdownBtn, 0);
            topBarGrid.Children.Add(_profilesDropdownBtn);

            // REQ-03: Custom FlexTabPanel (Zero ScrollViewer, Zero Horizontal Scrollbar)
            _tabStripPanel = new FlexTabPanel
            {
                Margin = new Thickness(4, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_tabStripPanel, 1);
            topBarGrid.Children.Add(_tabStripPanel);

            var rightTopControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var lockBtn = CreateToolbarButton("Lock", "Lock or set SHA-256 PIN for current profile");
            lockBtn.Click += (s, e) => PromptOrLockWorkspace();
            rightTopControls.Children.Add(lockBtn);

            Grid.SetColumn(rightTopControls, 2);
            topBarGrid.Children.Add(rightTopControls);
            _topBarBorder.Child = topBarGrid;
            Grid.SetRow(_topBarBorder, 0);
            _rootGrid.Children.Add(_topBarBorder);

            // -----------------------------------------------------------------
            // ROW 1: SIDEBAR TOGGLE + APPS GRID + NAV + MD3 OMNIBOX + TOOLS
            // -----------------------------------------------------------------
            _navBarBorder = new Border
            {
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 4, 10, 4)
            };
            var navGrid = new Grid();
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Sidebar Toggle + Apps Grid + Back/Fwd/Reload
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Pill Omnibox
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Tools (No Shield: ON)

            var navLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // REQ-07: Smooth Collapsible Left Sidebar Toggle
            _sidebarToggleBtn = CreateToolbarButton("☰", "Toggle Left Favorites Sidebar");
            _sidebarToggleBtn.FontSize = 14;
            _sidebarToggleBtn.Click += (s, e) => ToggleSidebarCollapse();
            navLeft.Children.Add(_sidebarToggleBtn);

            // REQ-04: Rebound Apps Grid Button (toggles Container Hub cleanly over WebView2)
            _appsGridBtn = new Button
            {
                Background = Md3Theme.PrimaryContainer,
                Foreground = Md3Theme.OnPrimaryContainer,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Toggle Google Workspace Container Hub (Ctrl+T)"
            };
            var appsGridContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            appsGridContent.Children.Add(CreateCrispIconImage("google-apps-for-work.png", 16, new Thickness(0, 0, 6, 0)));
            appsGridContent.Children.Add(new TextBlock
            {
                Text = "Apps Grid",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            _appsGridBtn.Content = appsGridContent;
            _appsGridBtn.Click += (s, e) => ToggleLauncherDashboard();
            navLeft.Children.Add(_appsGridBtn);

            var btnBack = CreateToolbarButton("←", "Back (Ctrl+[)");
            btnBack.Click += (s, e) => { if (_activeTab?.WebView?.CoreWebView2?.CanGoBack == true) _activeTab.WebView.CoreWebView2.GoBack(); };
            var btnFwd = CreateToolbarButton("→", "Forward (Ctrl+])");
            btnFwd.Click += (s, e) => { if (_activeTab?.WebView?.CoreWebView2?.CanGoForward == true) _activeTab.WebView.CoreWebView2.GoForward(); };
            var btnReload = CreateToolbarButton("↻", "Reload (Ctrl+R)");
            btnReload.Click += (s, e) => { _activeTab?.WebView?.CoreWebView2?.Reload(); };

            navLeft.Children.Add(btnBack);
            navLeft.Children.Add(btnFwd);
            navLeft.Children.Add(btnReload);
            Grid.SetColumn(navLeft, 0);
            navGrid.Children.Add(navLeft);

            // Pill-Shaped Material Design 3 Omnibox
            _omniBorder = new Border
            {
                Background = Md3Theme.SurfaceVariant,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Margin = new Thickness(10, 0, 10, 0),
                Padding = new Thickness(10, 2, 10, 2)
            };
            var omniGrid = new Grid();
            omniGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            omniGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            omniGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _searchEnginePillBtn = new Button
            {
                Content = $"{_activeSearchEngine.Name} ▾",
                Background = Brushes.Transparent,
                Foreground = Md3Theme.Primary,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 2, 8, 2),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            _searchEnginePillBtn.Click += OnSearchEngineSelectorClick;
            Grid.SetColumn(_searchEnginePillBtn, 0);
            omniGrid.Children.Add(_searchEnginePillBtn);

            _omniboxInput = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = Md3Theme.TextPrimary,
                CaretBrush = Md3Theme.Primary,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Text = "Search Google Workspace, enter URL, or type prefix (g / yt / m / d / doc / cal / ai)..."
            };
            _omniboxInput.GotFocus += (s, e) =>
            {
                if (_omniboxInput.Text.StartsWith("Search Google")) _omniboxInput.Text = "";
                _omniboxInput.SelectAll();
            };
            _omniboxInput.KeyDown += OnOmniboxKeyDown;
            Grid.SetColumn(_omniboxInput, 1);
            omniGrid.Children.Add(_omniboxInput);

            var paletteTriggerBtn = CreateToolbarButton("⌘K", "Spotlight Command Palette (Ctrl+K / Alt+Space)");
            paletteTriggerBtn.BorderThickness = new Thickness(0);
            paletteTriggerBtn.Background = Brushes.Transparent;
            paletteTriggerBtn.Click += (s, e) => ToggleCommandPalette();
            Grid.SetColumn(paletteTriggerBtn, 2);
            omniGrid.Children.Add(paletteTriggerBtn);

            _omniBorder.Child = omniGrid;
            Grid.SetColumn(_omniBorder, 1);
            navGrid.Children.Add(_omniBorder);

            // Right Tools (REQ-05: Shield: ON stripped; REQ-06: Browser stays in-container)
            var navRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var splitBtn = CreateToolbarButton("Split View", "Toggle Dual-Pane Side-by-Side Workspace (Alt+S)");
            splitBtn.Click += (s, e) => ToggleSplitView();
            navRight.Children.Add(splitBtn);

            var miniPlayerBtn = CreateToolbarButton("PiP Mini", "Pop current tab into Always-on-Top Mini-Player (Alt+P)");
            miniPlayerBtn.Click += (s, e) => OpenMiniPlayerForActiveTab();
            navRight.Children.Add(miniPlayerBtn);

            var browserBtn = CreateToolbarButton("Browser", "Open in-container Google Web Browser tab");
            browserBtn.Click += async (s, e) => await OpenInContainerBrowserTabAsync();
            navRight.Children.Add(browserBtn);

            var aiBtn = CreateToolbarButton("✦ AI Drawer", "Toggle Official AI Companion Drawer (Alt+A)");
            aiBtn.Foreground = Md3Theme.Primary;
            aiBtn.FontWeight = FontWeights.SemiBold;
            aiBtn.Click += (s, e) => ToggleAiDrawer();
            navRight.Children.Add(aiBtn);

            Grid.SetColumn(navRight, 2);
            navGrid.Children.Add(navRight);
            _navBarBorder.Child = navGrid;
            Grid.SetRow(_navBarBorder, 1);
            _rootGrid.Children.Add(_navBarBorder);

            // -----------------------------------------------------------------
            // ROW 2: MAIN BODY (COLLAPSIBLE LEFT DOCK + WEBVIEW/HUB + AI DRAWER)
            // -----------------------------------------------------------------
            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Collapsible Left Dock
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Center Content
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right AI Drawer

            // REQ-07: Smooth Collapsible Left Dock with Official Product Icons
            _sidebarBorder = new Border
            {
                Width = _config.SidebarCollapsed ? 0 : 64,
                Background = Md3Theme.Surface,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(0, 10, 0, 10),
                ClipToBounds = true
            };
            var sidebarScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            _sidebarFavoritesPanel = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };
            sidebarScroll.Content = _sidebarFavoritesPanel;
            _sidebarBorder.Child = sidebarScroll;
            Grid.SetColumn(_sidebarBorder, 0);
            bodyGrid.Children.Add(_sidebarBorder);

            // Center Area
            var centerWrapper = new Grid();

            _splitPaneContainer = new Grid();
            _splitPaneContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _splitPaneContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _splitPaneContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });

            _contentHostGrid = new Grid { Background = Md3Theme.Surface };
            Grid.SetColumn(_contentHostGrid, 0);
            _splitPaneContainer.Children.Add(_contentHostGrid);

            _splitSplitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Md3Theme.Outline,
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(_splitSplitter, 1);
            _splitPaneContainer.Children.Add(_splitSplitter);

            _splitRightBorder = BuildSplitRightPane();
            Grid.SetColumn(_splitRightBorder, 2);
            _splitPaneContainer.Children.Add(_splitRightBorder);

            centerWrapper.Children.Add(_splitPaneContainer);

            // Container Hub Overlay (REQ-01 & REQ-10: Clean Icon + Title Cards, Zero Category Pills)
            _homeDashboardOverlay = BuildHomeDashboardOverlay();
            centerWrapper.Children.Add(_homeDashboardOverlay);

            // Spotlight Command Palette Overlay
            _commandPaletteOverlay = BuildCommandPaletteOverlay();
            centerWrapper.Children.Add(_commandPaletteOverlay);

            // Workspace PIN Lock Overlay
            _pinLockOverlay = BuildPinLockOverlay();
            centerWrapper.Children.Add(_pinLockOverlay);

            // Toast Banner
            _toastBanner = new Border
            {
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Primary,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 14, 18, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };
            _toastText = new TextBlock { Foreground = Md3Theme.TextPrimary, FontSize = 12.5, FontWeight = FontWeights.Medium };
            _toastBanner.Child = _toastText;
            centerWrapper.Children.Add(_toastBanner);

            Grid.SetColumn(centerWrapper, 1);
            bodyGrid.Children.Add(centerWrapper);

            // Right AI Companion Drawer
            _aiDrawerBorder = BuildAiCompanionDrawer();
            Grid.SetColumn(_aiDrawerBorder, 2);
            bodyGrid.Children.Add(_aiDrawerBorder);

            Grid.SetRow(bodyGrid, 2);
            _rootGrid.Children.Add(bodyGrid);

            Content = _rootGrid;
        }

        // =====================================================================
        // CONTAINER HUB (REQ-01 & REQ-10: OFFICIAL ICONS, NO CATEGORY PILLS)
        // =====================================================================
        private Grid BuildHomeDashboardOverlay()
        {
            var overlay = new Grid { Background = Md3Theme.Surface, Visibility = Visibility.Visible };
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(40, 28, 40, 36)
            };
            var mainStack = new StackPanel { MaxWidth = 1220, HorizontalAlignment = HorizontalAlignment.Stretch };

            // Header + Filter + Add Custom App
            var heroGrid = new Grid { Margin = new Thickness(0, 0, 0, 24) };
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleRow.Children.Add(CreateCrispIconImage("google-apps-for-work.png", 36, new Thickness(0, 0, 14, 0)));

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _hubTitleText = new TextBlock
            {
                Text = "Google Workspace Hub",
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = Md3Theme.TextPrimary
            };
            titleStack.Children.Add(_hubTitleText);
            _hubSubtitleText = new TextBlock
            {
                Text = "Click any app to launch in a tab, or right-click for quick actions and deep links.",
                FontSize = 12.5,
                Foreground = Md3Theme.TextSecondary,
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(_hubSubtitleText);
            titleRow.Children.Add(titleStack);
            Grid.SetColumn(titleRow, 0);
            heroGrid.Children.Add(titleRow);

            var rightHeroActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _hubSearchBorder = new Border
            {
                Width = 260,
                Height = 36,
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(12, 0, 12, 0)
            };
            var hubSearchGrid = new Grid();
            hubSearchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hubSearchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var searchHintIcon = new TextBlock
            {
                Text = "⌕",
                Foreground = Md3Theme.TextSecondary,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 1)
            };
            Grid.SetColumn(searchHintIcon, 0);
            hubSearchGrid.Children.Add(searchHintIcon);

            _homeFilterBox = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = Md3Theme.TextPrimary,
                CaretBrush = Md3Theme.Primary,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Text = "",
                ToolTip = "Filter Google Workspace apps"
            };
            _homeFilterBox.TextChanged += (s, e) => RenderDashboardCards();
            Grid.SetColumn(_homeFilterBox, 1);
            hubSearchGrid.Children.Add(_homeFilterBox);
            _hubSearchBorder.Child = hubSearchGrid;
            rightHeroActions.Children.Add(_hubSearchBorder);

            var addCustomBtn = CreateToolbarButton("+ Add App", "Add any custom web app to your Workspace Hub");
            addCustomBtn.Height = 36;
            addCustomBtn.Margin = new Thickness(10, 0, 0, 0);
            addCustomBtn.Click += (s, e) => ShowAddCustomAppDialog();
            rightHeroActions.Children.Add(addCustomBtn);

            Grid.SetColumn(rightHeroActions, 1);
            heroGrid.Children.Add(rightHeroActions);
            mainStack.Children.Add(heroGrid);

            // REQ-01: Category Filter Pills completely stripped. Clean Grid of Official Icon Cards:
            _homeAppsWrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            mainStack.Children.Add(_homeAppsWrapPanel);

            scroll.Content = mainStack;
            overlay.Children.Add(scroll);
            return overlay;
        }

        private void RenderDashboardCards()
        {
            if (_homeAppsWrapPanel == null) return;
            _homeAppsWrapPanel.Children.Clear();
            string query = (_homeFilterBox?.Text ?? "").Trim();

            IEnumerable<GoogleAppDef> filtered = _allApps;
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(a =>
                    FuzzyMatch(a.Name, query) ||
                    a.SubItems.Any(sub => FuzzyMatch(sub.Name, query)));
            }

            foreach (var app in filtered)
            {
                _homeAppsWrapPanel.Children.Add(CreateAppCard(app));
            }
        }

        // Matches Uploaded Reference Image 3 (Keep card: Official Full-Color Icon Left + Clean Product Name Right)
        private Border CreateAppCard(GoogleAppDef app)
        {
            var card = new Border
            {
                Width = 212,
                Height = 68,
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 14, 14),
                Padding = new Thickness(16, 12, 16, 12),
                Cursor = Cursors.Hand,
                ToolTip = app.SubItems.Count > 0
                    ? $"{app.Name} (Right-click for {app.SubItems.Count} shortcuts & options)"
                    : $"{app.Name} (Right-click for options)"
            };

            card.MouseEnter += (s, e) =>
            {
                card.Background = Md3Theme.SurfaceVariant;
                card.BorderBrush = Md3Theme.Primary;
            };
            card.MouseLeave += (s, e) =>
            {
                card.Background = Md3Theme.SurfaceContainer;
                card.BorderBrush = Md3Theme.Outline;
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconImg = CreateCrispIconImage(app.IconFile, 32, new Thickness(0, 0, 14, 0));
            Grid.SetColumn(iconImg, 0);
            row.Children.Add(iconImg);

            var nameBlock = new TextBlock
            {
                Text = app.Name,
                Foreground = Md3Theme.TextPrimary,
                FontWeight = FontWeights.Medium,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameBlock, 1);
            row.Children.Add(nameBlock);

            card.Child = row;
            card.MouseLeftButtonUp += async (s, e) =>
            {
                await OpenAppInTabAsync(app.Name, app.Url, app.IconFile, switchToTab: true);
            };
            card.MouseRightButtonUp += (s, e) =>
            {
                OpenSubItemsMenu(app, card);
            };

            return card;
        }

        private void OpenSubItemsMenu(GoogleAppDef app, UIElement placementTarget)
        {
            var menu = new ContextMenu
            {
                PlacementTarget = placementTarget,
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                BorderBrush = Md3Theme.Outline
            };

            var openTabItem = new MenuItem
            {
                Header = $"Open {app.Name} in New Tab",
                Foreground = Md3Theme.Primary,
                Icon = CreateCrispIconImage(app.IconFile, 16)
            };
            openTabItem.Click += async (s, e) => await OpenAppInTabAsync(app.Name, app.Url, app.IconFile, switchToTab: true);
            menu.Items.Add(openTabItem);

            var pinItem = new MenuItem
            {
                Header = app.IsFavorite ? "Unpin from Left Sidebar" : "Pin to Left Sidebar",
                Foreground = Md3Theme.TextPrimary
            };
            pinItem.Click += (s, e) => ToggleFavorite(app);
            menu.Items.Add(pinItem);

            var openSplitItem = new MenuItem
            {
                Header = $"Open {app.Name} in Split View",
                Foreground = Md3Theme.TextPrimary
            };
            openSplitItem.Click += async (s, e) => await OpenUrlInSplitPaneAsync(app.Url);
            menu.Items.Add(openSplitItem);

            var openPopoutItem = new MenuItem
            {
                Header = $"Open {app.Name} in Popout Window",
                Foreground = Md3Theme.TextPrimary
            };
            openPopoutItem.Click += (s, e) => OpenDedicatedPopoutWindow(app.Name, app.Url);
            menu.Items.Add(openPopoutItem);

            if (app.SubItems.Count > 0)
            {
                menu.Items.Add(new Separator());
                foreach (var sub in app.SubItems)
                {
                    var subItem = new MenuItem { Header = sub.Name, Foreground = Md3Theme.TextPrimary };
                    string u = sub.Url;
                    string n = $"{app.Name}: {sub.Name}";
                    subItem.Click += async (s, e) => await OpenAppInTabAsync(n, u, app.IconFile, switchToTab: true);
                    menu.Items.Add(subItem);
                }
            }

            menu.IsOpen = true;
        }

        // =====================================================================
        // REQ-07 & REQ-08: PROFILES DROPDOWN, COLLAPSIBLE SIDEBAR & FLEX TABS
        // =====================================================================
        private void UpdateProfilesDropdownButton()
        {
            var ws = GetActiveWorkspace();
            string acct = string.IsNullOrWhiteSpace(ws.DetectedAccount) ? "george.christosp@gmail.com" : ws.DetectedAccount;
            _profilesDropdownLabel.Text = $"Profiles: {acct} ▾";
            _profilesDropdownLabel.Foreground = Md3Theme.TextPrimary;
        }

        private void OnProfilesDropdownClick(object sender, RoutedEventArgs e)
        {
            var ws = GetActiveWorkspace();
            var menu = new ContextMenu
            {
                PlacementTarget = _profilesDropdownBtn,
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                BorderBrush = Md3Theme.Outline
            };

            var acctHeader = new MenuItem
            {
                Header = $"Active Account: {ws.DetectedAccount}",
                IsEnabled = false,
                FontWeight = FontWeights.SemiBold
            };
            menu.Items.Add(acctHeader);
            menu.Items.Add(new Separator());

            foreach (var profile in _config.Workspaces)
            {
                bool isCurrent = profile.Id == _config.ActiveWorkspaceId;
                string label = isCurrent
                    ? $"✓ {profile.Name} ({profile.DetectedAccount})"
                    : $"   {profile.Name} ({profile.DetectedAccount})";
                var item = new MenuItem
                {
                    Header = label,
                    FontWeight = isCurrent ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isCurrent ? Md3Theme.Primary : Md3Theme.TextPrimary
                };
                string targetId = profile.Id;
                item.Click += async (s, args) => await SwitchWorkspaceAsync(targetId);
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            var addProfileItem = new MenuItem { Header = "+ Create New Profile...", Foreground = Md3Theme.Primary };
            addProfileItem.Click += (s, args) => CreateNewWorkspaceDialog();
            menu.Items.Add(addProfileItem);

            var hibernateItem = new MenuItem { Header = "Hibernate Background Tabs", Foreground = Md3Theme.TextPrimary };
            hibernateItem.Click += (s, args) => HibernateBackgroundTabs();
            menu.Items.Add(hibernateItem);

            var lockItem = new MenuItem { Header = "Lock / Set Profile PIN...", Foreground = Md3Theme.TextPrimary };
            lockItem.Click += (s, args) => PromptOrLockWorkspace();
            menu.Items.Add(lockItem);

            menu.IsOpen = true;
        }

        private void ToggleSidebarCollapse()
        {
            _config.SidebarCollapsed = !_config.SidebarCollapsed;
            double fromW = _sidebarBorder.ActualWidth;
            double toW = _config.SidebarCollapsed ? 0.0 : 64.0;

            var anim = new DoubleAnimation(fromW, toW, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            _sidebarBorder.BeginAnimation(FrameworkElement.WidthProperty, anim);
            SaveConfig();
        }

        private void RenderSidebarFavorites()
        {
            if (_sidebarFavoritesPanel == null) return;
            _sidebarFavoritesPanel.Children.Clear();
            foreach (var app in _allApps.Where(a => a.IsFavorite))
            {
                var btn = new Button
                {
                    Width = 44,
                    Height = 44,
                    Margin = new Thickness(0, 0, 0, 8),
                    Background = Md3Theme.SurfaceContainer,
                    BorderBrush = Md3Theme.Outline,
                    BorderThickness = new Thickness(1),
                    Content = CreateCrispIconImage(app.IconFile, 24),
                    Cursor = Cursors.Hand,
                    ToolTip = $"{app.Name} (Right-click for options)"
                };
                var capturedApp = app;
                btn.Click += async (s, e) => await OpenAppInTabAsync(capturedApp.Name, capturedApp.Url, capturedApp.IconFile, switchToTab: true);
                btn.MouseRightButtonUp += (s, e) => OpenSubItemsMenu(capturedApp, btn);
                _sidebarFavoritesPanel.Children.Add(btn);
            }
        }

        private async Task<CoreWebView2Environment> GetOrCreateWorkspaceEnvironmentAsync(string workspaceId)
        {
            if (_envCache.TryGetValue(workspaceId, out var existing))
                return existing;

            string profileDir = System.IO.Path.Combine(_dataRoot, "Profiles", workspaceId);
            Directory.CreateDirectory(profileDir);

            var opts = new CoreWebView2EnvironmentOptions("--enable-smooth-scrolling");
            var env = await CoreWebView2Environment.CreateAsync(null, profileDir, opts);
            _envCache[workspaceId] = env;
            return env;
        }

        private string ResolveIconForUrl(string url, string fallbackIcon)
        {
            if (!string.IsNullOrWhiteSpace(fallbackIcon) && fallbackIcon != "google-apps-for-work.png")
                return fallbackIcon;

            if (string.IsNullOrWhiteSpace(url)) return "google-apps-for-work.png";
            string u = url.ToLowerInvariant();
            if (u.Contains("mail.google.com/chat")) return "google-chat.png";
            if (u.Contains("mail.google.com")) return "gmail.png";
            if (u.Contains("calendar.google.com")) return "google-calendar.png";
            if (u.Contains("drive.google.com")) return "google-drive.png";
            if (u.Contains("docs.google.com/document")) return "google-docs.png";
            if (u.Contains("docs.google.com/spreadsheets")) return "google-sheets.png";
            if (u.Contains("docs.google.com/presentation")) return "google-slides.png";
            if (u.Contains("docs.google.com/forms")) return "google-forms.png";
            if (u.Contains("docs.google.com/drawings")) return "google-drawings.png";
            if (u.Contains("keep.google.com")) return "google-keep.png";
            if (u.Contains("tasks.google.com")) return "google-tasks.png";
            if (u.Contains("meet.google.com")) return "google-meet.png";
            if (u.Contains("gemini.google.com")) return "google-gemini.png";
            if (u.Contains("notebooklm.google.com")) return "gemini-notebook.png";
            if (u.Contains("aistudio.google.com")) return "google-ai-studio.png";
            if (u.Contains("music.youtube.com")) return "youtube-music.png";
            if (u.Contains("youtube.com")) return "youtube.png";
            if (u.Contains("photos.google.com")) return "google-photos.png";
            if (u.Contains("google.com/maps")) return "google-maps.png";
            if (u.Contains("contacts.google.com")) return "google-contacts.png";
            if (u.Contains("myaccount.google.com")) return "google-account.png";
            if (u.Contains("console.cloud.google.com")) return "google-cloud-console.png";
            if (u.Contains("translate.google.com")) return "google-translate.png";
            if (u.Contains("scholar.google.com")) return "google-scholar.png";
            if (u.Contains("news.google.com")) return "google-news.png";
            return "google-apps-for-work.png";
        }

        public async Task OpenAppInTabAsync(string title, string url, string iconFile, bool switchToTab)
        {
            var tab = new BrowserTabEntry
            {
                Title = title,
                Url = url,
                IconFile = ResolveIconForUrl(url, iconFile)
            };

            var bgColor = Md3Theme.IsDark
                ? System.Drawing.Color.FromArgb(255, 19, 19, 20)
                : System.Drawing.Color.FromArgb(255, 248, 250, 253);

            var webView = new WebView2
            {
                DefaultBackgroundColor = bgColor,
                Visibility = Visibility.Collapsed
            };
            webView.PreviewKeyDown += (s, e) =>
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    if (e.Key == Key.T) { e.Handled = true; ShowLauncherDashboard(); }
                    else if (e.Key == Key.W && _activeTab != null) { e.Handled = true; CloseTab(_activeTab); }
                    else if (e.Key == Key.K) { e.Handled = true; ToggleCommandPalette(); }
                    else if (e.Key == Key.L) { e.Handled = true; _omniboxInput.Focus(); }
                    else if (e.Key == Key.B) { e.Handled = true; ToggleSidebarCollapse(); }
                }
            };
            tab.WebView = webView;
            _contentHostGrid.Children.Add(webView);
            _tabs.Add(tab);

            RenderTabStrip();
            if (switchToTab)
            {
                ActivateTab(tab);
            }

            await InitializeTabWebViewAsync(tab, url);
        }

        private async Task InitializeTabWebViewAsync(BrowserTabEntry tab, string targetUrl)
        {
            if (tab.WebView == null) return;
            var ws = GetActiveWorkspace();
            var env = await GetOrCreateWorkspaceEnvironmentAsync(ws.Id);
            await tab.WebView.EnsureCoreWebView2Async(env);

            var core = tab.WebView.CoreWebView2;
            core.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreDevToolsEnabled = true;

            // REQ-05: Only Keyboard + Account detection injected; 4-layer YouTube ad-pruner & network hooks removed
            await core.AddScriptToExecuteOnDocumentCreatedAsync(InjectedScripts.DocumentStartInitScript);

            // REQ-06: Prevent NewWindowRequested from spawning external Microsoft Edge windows
            core.NewWindowRequested += async (s, e) =>
            {
                e.Handled = true;
                string cleanUri = e.Uri.Replace("#gab-ext", "");
                await OpenAppInTabAsync("Workspace Tab", cleanUri, ResolveIconForUrl(cleanUri, ""), switchToTab: true);
            };

            core.DocumentTitleChanged += (s, e) =>
            {
                tab.Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? tab.Title : core.DocumentTitle;
                if (tab.TabTitleText != null)
                {
                    tab.TabTitleText.Text = tab.Title;
                }
            };

            core.SourceChanged += (s, e) =>
            {
                if (core.Source != "about:blank")
                {
                    tab.Url = core.Source;
                    if (_activeTab == tab && _homeDashboardOverlay.Visibility != Visibility.Visible)
                        _omniboxInput.Text = tab.Url;
                }
            };

            core.WebMessageReceived += (s, e) =>
            {
                string msg = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(msg)) return;
                if (msg == "cmd:palette") ToggleCommandPalette();
                else if (msg == "cmd:hub") ShowLauncherDashboard();
                else if (msg.StartsWith("account:"))
                {
                    string email = msg.Substring("account:".Length);
                    ws.DetectedAccount = email;
                    UpdateProfilesDropdownButton();
                }
            };

            core.Navigate(targetUrl);
        }

        private void RenderTabStrip()
        {
            if (_tabStripPanel == null) return;
            _tabStripPanel.Children.Clear();

            bool hubVisible = _homeDashboardOverlay?.Visibility == Visibility.Visible;

            // Hub Chip (Fixed First Element in FlexTabPanel)
            var homeChip = new Button
            {
                Background = hubVisible ? Md3Theme.PrimaryContainer : Md3Theme.SurfaceContainer,
                Foreground = hubVisible ? Md3Theme.OnPrimaryContainer : Md3Theme.TextPrimary,
                BorderBrush = hubVisible ? Md3Theme.Primary : Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Open Google Workspace Hub (Ctrl+T)"
            };
            var hubContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            hubContent.Children.Add(CreateCrispIconImage("google-apps-for-work.png", 15, new Thickness(0, 0, 6, 0)));
            hubContent.Children.Add(new TextBlock
            {
                Text = "Hub",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            homeChip.Content = hubContent;
            homeChip.Click += (s, e) => ShowLauncherDashboard();
            _tabStripPanel.Children.Add(homeChip);

            // Flex Browser Tabs (Shrink proportionally via FlexTabPanel, zero 3-letter text badges)
            foreach (var tab in _tabs)
            {
                bool isActive = (_activeTab == tab && !hubVisible);
                var chipBorder = new Border
                {
                    Background = isActive ? Md3Theme.PrimaryContainer : Md3Theme.SurfaceContainer,
                    BorderBrush = isActive ? Md3Theme.Primary : Md3Theme.Outline,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 4, 6, 4),
                    Margin = new Thickness(0, 0, 4, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = tab.Title
                };

                var chipGrid = new Grid();
                chipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Official 16x16 Icon
                chipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Flex Title
                chipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // State Badge
                chipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Close Button

                var iconImg = CreateCrispIconImage(tab.IconFile, 15, new Thickness(0, 0, 6, 0));
                Grid.SetColumn(iconImg, 0);
                chipGrid.Children.Add(iconImg);

                var titleBlock = new TextBlock
                {
                    Text = tab.Title,
                    Foreground = isActive ? Md3Theme.OnPrimaryContainer : Md3Theme.TextPrimary,
                    FontWeight = isActive ? FontWeights.Medium : FontWeights.Normal,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                tab.TabTitleText = titleBlock;
                Grid.SetColumn(titleBlock, 1);
                chipGrid.Children.Add(titleBlock);

                var stateBadge = new TextBlock
                {
                    Text = tab.IsHibernated ? " [zzz]" : "",
                    Foreground = Md3Theme.TextSecondary,
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center
                };
                tab.TabStateBadge = stateBadge;
                Grid.SetColumn(stateBadge, 2);
                chipGrid.Children.Add(stateBadge);

                var closeBtn = new Button
                {
                    Content = "×",
                    Background = Brushes.Transparent,
                    Foreground = isActive ? Md3Theme.OnPrimaryContainer : Md3Theme.TextSecondary,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(4, 0, 0, 0),
                    Padding = new Thickness(3, 0, 3, 0),
                    FontSize = 13,
                    Cursor = Cursors.Hand,
                    ToolTip = "Close Tab (Ctrl+W)"
                };
                var capturedTab = tab;
                closeBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    CloseTab(capturedTab);
                };
                Grid.SetColumn(closeBtn, 3);
                chipGrid.Children.Add(closeBtn);

                chipBorder.Child = chipGrid;
                chipBorder.MouseLeftButtonUp += async (s, e) => await WakeAndActivateTabAsync(capturedTab);
                tab.TabChipBorder = chipBorder;
                _tabStripPanel.Children.Add(chipBorder);
            }

            // Fixed Last Element (+) in FlexTabPanel
            var newTabBtn = CreateToolbarButton("+", "Open Google Workspace Hub (Ctrl+T)");
            newTabBtn.FontWeight = FontWeights.Bold;
            newTabBtn.Click += (s, e) => ShowLauncherDashboard();
            _tabStripPanel.Children.Add(newTabBtn);
        }

        private async Task WakeAndActivateTabAsync(BrowserTabEntry tab)
        {
            if (tab.IsHibernated && tab.WebView?.CoreWebView2 != null)
            {
                tab.IsHibernated = false;
                if (tab.TabStateBadge != null) tab.TabStateBadge.Text = "";
                tab.WebView.CoreWebView2.Navigate(tab.Url);
            }
            ActivateTab(tab);
            await Task.CompletedTask;
        }

        private void ActivateTab(BrowserTabEntry tab)
        {
            _activeTab = tab;
            _homeDashboardOverlay.Visibility = Visibility.Collapsed;
            _commandPaletteOverlay.Visibility = Visibility.Collapsed;
            foreach (var t in _tabs)
            {
                if (t.WebView != null)
                    t.WebView.Visibility = (t == tab) ? Visibility.Visible : Visibility.Collapsed;
            }
            _omniboxInput.Text = tab.Url;
            RenderTabStrip();
        }

        // REQ-04: Toggle / Show Launcher Dashboard while hiding WebView2 HWNDs (fixes WPF Airspace issue)
        private void ToggleLauncherDashboard()
        {
            if (_homeDashboardOverlay.Visibility == Visibility.Visible && _activeTab != null)
            {
                ActivateTab(_activeTab);
            }
            else
            {
                ShowLauncherDashboard();
            }
        }

        private void ShowLauncherDashboard()
        {
            // Hide active WebView2 HWNDs so the WPF Container Hub overlay is never occluded
            foreach (var t in _tabs)
            {
                if (t.WebView != null)
                    t.WebView.Visibility = Visibility.Collapsed;
            }
            _homeDashboardOverlay.Visibility = Visibility.Visible;
            RenderTabStrip();
            RenderDashboardCards();
            _homeFilterBox?.Focus();
        }

        private void CloseTab(BrowserTabEntry tab)
        {
            int idx = _tabs.IndexOf(tab);
            if (tab.WebView != null)
            {
                _contentHostGrid.Children.Remove(tab.WebView);
                tab.WebView.Dispose();
            }
            _tabs.Remove(tab);

            if (_tabs.Count == 0)
            {
                _activeTab = null;
                ShowLauncherDashboard();
            }
            else if (_activeTab == tab)
            {
                var next = _tabs[Math.Max(0, idx - 1)];
                ActivateTab(next);
            }
            else
            {
                RenderTabStrip();
            }
        }

        private void HibernateBackgroundTabs()
        {
            int count = 0;
            foreach (var t in _tabs)
            {
                if (t != _activeTab && !t.IsHibernated && t.WebView?.CoreWebView2 != null)
                {
                    t.IsHibernated = true;
                    t.WebView.CoreWebView2.Navigate("about:blank");
                    if (t.TabStateBadge != null) t.TabStateBadge.Text = " [zzz]";
                    count++;
                }
            }
            ShowToast(count > 0
                ? $"Hibernated {count} background tab(s)."
                : "No background tabs to hibernate.");
        }

        // =====================================================================
        // DUAL-PANE SPLIT VIEW, FLOATING MINI-PLAYER & AI COMPANION DRAWER
        // =====================================================================
        private Border BuildSplitRightPane()
        {
            var border = new Border
            {
                Background = Md3Theme.Surface,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new Grid { Background = Md3Theme.SurfaceContainer };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            header.Children.Add(new TextBlock
            {
                Text = "Split Pane",
                Foreground = Md3Theme.Primary,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 8, 0)
            });

            _splitUrlBox = new TextBox
            {
                Background = Md3Theme.SurfaceVariant,
                Foreground = Md3Theme.TextPrimary,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 11.5,
                Padding = new Thickness(6, 2, 6, 2),
                Text = "https://calendar.google.com/calendar/u/0/r"
            };
            _splitUrlBox.KeyDown += async (s, e) =>
            {
                if (e.Key == Key.Enter) await OpenUrlInSplitPaneAsync(_splitUrlBox.Text);
            };
            Grid.SetColumn(_splitUrlBox, 1);
            header.Children.Add(_splitUrlBox);

            var closeSplitBtn = CreateToolbarButton("×", "Close right split pane");
            closeSplitBtn.Click += (s, e) => ToggleSplitView();
            Grid.SetColumn(closeSplitBtn, 2);
            header.Children.Add(closeSplitBtn);

            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            _splitWebView = new WebView2();
            Grid.SetRow(_splitWebView, 1);
            grid.Children.Add(_splitWebView);

            border.Child = grid;
            return border;
        }

        private async void ToggleSplitView()
        {
            _splitViewActive = !_splitViewActive;
            if (_splitViewActive)
            {
                _splitPaneContainer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                _splitPaneContainer.ColumnDefinitions[1].Width = new GridLength(5);
                _splitPaneContainer.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                _splitSplitter.Visibility = Visibility.Visible;
                _splitRightBorder.Visibility = Visibility.Visible;
                await OpenUrlInSplitPaneAsync(_splitUrlBox.Text);
            }
            else
            {
                _splitPaneContainer.ColumnDefinitions[1].Width = new GridLength(0);
                _splitPaneContainer.ColumnDefinitions[2].Width = new GridLength(0);
                _splitSplitter.Visibility = Visibility.Collapsed;
                _splitRightBorder.Visibility = Visibility.Collapsed;
            }
        }

        private async Task OpenUrlInSplitPaneAsync(string url)
        {
            if (!_splitViewActive)
            {
                _splitViewActive = true;
                _splitPaneContainer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                _splitPaneContainer.ColumnDefinitions[1].Width = new GridLength(5);
                _splitPaneContainer.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                _splitSplitter.Visibility = Visibility.Visible;
                _splitRightBorder.Visibility = Visibility.Visible;
            }
            _splitUrlBox.Text = url;
            if (_splitWebView != null)
            {
                var env = await GetOrCreateWorkspaceEnvironmentAsync(GetActiveWorkspace().Id);
                await _splitWebView.EnsureCoreWebView2Async(env);
                _splitWebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
                _splitWebView.CoreWebView2.Navigate(NormalizeUrlOrSearch(url));
            }
        }

        private Border BuildAiCompanionDrawer()
        {
            var drawer = new Border
            {
                Width = 420,
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Visibility = _config.AiDrawerOpen ? Visibility.Visible : Visibility.Collapsed
            };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _aiProviderTabsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0)
            };
            foreach (var p in CatalogRegistry.GetAiProviders())
            {
                var btn = CreateToolbarButton(p.Name, $"Switch AI Drawer to {p.Name}");
                btn.Padding = new Thickness(7, 3, 7, 3);
                btn.Margin = new Thickness(0, 0, 4, 0);
                btn.FontSize = 11;
                string targetUrl = p.Url;
                btn.Click += async (s, e) => await NavigateAiDrawerAsync(targetUrl);
                _aiProviderTabsPanel.Children.Add(btn);
            }
            Grid.SetRow(_aiProviderTabsPanel, 0);
            grid.Children.Add(_aiProviderTabsPanel);

            _aiWebViewHost = new Grid { Background = Md3Theme.Surface };
            _aiWebView = new WebView2();
            _aiWebViewHost.Children.Add(_aiWebView);
            Grid.SetRow(_aiWebViewHost, 1);
            grid.Children.Add(_aiWebViewHost);

            drawer.Child = grid;
            return drawer;
        }

        private async void ToggleAiDrawer()
        {
            _config.AiDrawerOpen = !_config.AiDrawerOpen;
            _aiDrawerBorder.Visibility = _config.AiDrawerOpen ? Visibility.Visible : Visibility.Collapsed;
            if (_config.AiDrawerOpen)
            {
                await NavigateAiDrawerAsync(_config.ActiveAiProviderUrl);
            }
        }

        private async Task NavigateAiDrawerAsync(string url)
        {
            _config.ActiveAiProviderUrl = url;
            if (_aiWebView == null) return;
            var env = await GetOrCreateWorkspaceEnvironmentAsync(GetActiveWorkspace().Id);
            await _aiWebView.EnsureCoreWebView2Async(env);
            _aiWebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            if (_aiWebView.CoreWebView2.Source != url)
            {
                _aiWebView.CoreWebView2.Navigate(url);
            }
        }

        private async void OpenMiniPlayerForActiveTab()
        {
            string url = _activeTab?.Url ?? "https://www.youtube.com/";
            string title = _activeTab?.Title ?? "Always-on-Top Mini-Player";

            var miniWin = new Window
            {
                Title = $"Mini-Player | {title}",
                Width = 480,
                Height = 310,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = SystemParameters.WorkArea.Width - 500,
                Top = SystemParameters.WorkArea.Height - 340,
                Background = Md3Theme.Surface,
                Foreground = Md3Theme.TextPrimary,
                FontFamily = UiFont,
                Icon = Icon
            };
            var wv = new WebView2();
            miniWin.Content = wv;
            miniWin.Show();

            var env = await GetOrCreateWorkspaceEnvironmentAsync(GetActiveWorkspace().Id);
            await wv.EnsureCoreWebView2Async(env);
            wv.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(InjectedScripts.DocumentStartInitScript);
            wv.CoreWebView2.Navigate(url);
        }

        private async void OpenDedicatedPopoutWindow(string title, string url)
        {
            var popWin = new Window
            {
                Title = $"{title} | Google Workspace",
                Width = 1180,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Md3Theme.Surface,
                Foreground = Md3Theme.TextPrimary,
                Icon = Icon
            };
            var wv = new WebView2();
            popWin.Content = wv;
            popWin.Show();

            var env = await GetOrCreateWorkspaceEnvironmentAsync(GetActiveWorkspace().Id);
            await wv.EnsureCoreWebView2Async(env);
            wv.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(InjectedScripts.DocumentStartInitScript);
            wv.CoreWebView2.Navigate(url);
        }

        // REQ-06: In-Container Browser Action (Never spawns external Microsoft Edge windows)
        private async Task OpenInContainerBrowserTabAsync()
        {
            string url = (_activeTab != null && !string.IsNullOrWhiteSpace(_activeTab.Url) && _activeTab.Url != "about:blank")
                ? _activeTab.Url
                : "https://www.google.com/";
            await OpenAppInTabAsync("Google Web", url, "google-apps-for-work.png", switchToTab: true);
        }

        // =====================================================================
        // SPOTLIGHT COMMAND PALETTE (Alt+Space / Ctrl+K)
        // =====================================================================
        private Grid BuildCommandPaletteOverlay()
        {
            var overlay = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
                Visibility = Visibility.Collapsed
            };
            overlay.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource == overlay) ToggleCommandPalette();
            };

            _commandPaletteModal = new Border
            {
                Width = 620,
                MaxHeight = 480,
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Primary,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 64, 0, 0),
                Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.35 }
            };

            var stack = new StackPanel();
            _paletteSearchBox = new TextBox
            {
                Height = 38,
                Background = Md3Theme.SurfaceVariant,
                Foreground = Md3Theme.TextPrimary,
                CaretBrush = Md3Theme.Primary,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 7, 12, 7),
                FontSize = 14
            };
            _paletteSearchBox.TextChanged += (s, e) => RenderPaletteResults();
            _paletteSearchBox.KeyDown += OnPaletteSearchKeyDown;
            stack.Children.Add(_paletteSearchBox);

            var scroll = new ScrollViewer
            {
                MaxHeight = 380,
                Margin = new Thickness(0, 10, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _paletteResultsPanel = new StackPanel();
            scroll.Content = _paletteResultsPanel;
            stack.Children.Add(scroll);

            _commandPaletteModal.Child = stack;
            overlay.Children.Add(_commandPaletteModal);
            return overlay;
        }

        private void ToggleCommandPalette()
        {
            if (_commandPaletteOverlay.Visibility == Visibility.Visible)
            {
                _commandPaletteOverlay.Visibility = Visibility.Collapsed;
                if (_homeDashboardOverlay.Visibility != Visibility.Visible && _activeTab?.WebView != null)
                {
                    _activeTab.WebView.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Temporarily collapse active WebView2 so WPF Command Palette modal renders on top
                foreach (var t in _tabs)
                {
                    if (t.WebView != null) t.WebView.Visibility = Visibility.Collapsed;
                }
                _commandPaletteOverlay.Visibility = Visibility.Visible;
                _paletteSearchBox.Text = "";
                RenderPaletteResults();
                _paletteSearchBox.Focus();
            }
        }

        private void RenderPaletteResults()
        {
            _paletteResultsPanel.Children.Clear();
            string q = (_paletteSearchBox.Text ?? "").Trim();

            var matches = _allApps
                .Where(a => string.IsNullOrEmpty(q) || FuzzyMatch(a.Name, q))
                .Take(10)
                .ToList();

            foreach (var app in matches)
            {
                var rowBtn = new Button
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Background = Md3Theme.Surface,
                    Foreground = Md3Theme.TextPrimary,
                    BorderBrush = Md3Theme.Outline,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };
                var rowStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                rowStack.Children.Add(CreateCrispIconImage(app.IconFile, 20, new Thickness(0, 0, 10, 0)));
                rowStack.Children.Add(new TextBlock
                {
                    Text = app.Name,
                    FontWeight = FontWeights.Medium,
                    FontSize = 13,
                    Foreground = Md3Theme.TextPrimary,
                    VerticalAlignment = VerticalAlignment.Center
                });
                rowBtn.Content = rowStack;

                var captured = app;
                rowBtn.Click += async (s, e) =>
                {
                    _commandPaletteOverlay.Visibility = Visibility.Collapsed;
                    await OpenAppInTabAsync(captured.Name, captured.Url, captured.IconFile, switchToTab: true);
                };
                _paletteResultsPanel.Children.Add(rowBtn);
            }

            if (!string.IsNullOrEmpty(q))
            {
                foreach (var se in _searchEngines.Take(4))
                {
                    var searchRow = new Button
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Background = Md3Theme.PrimaryContainer,
                        Foreground = Md3Theme.OnPrimaryContainer,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(12, 7, 12, 7),
                        Margin = new Thickness(0, 0, 0, 4),
                        Cursor = Cursors.Hand,
                        Content = $"Search {se.Name} for \"{q}\""
                    };
                    string url = string.Format(se.UrlTemplate, Uri.EscapeDataString(q));
                    string name = se.Name;
                    string icon = se.IconFile;
                    searchRow.Click += async (s, e) =>
                    {
                        _commandPaletteOverlay.Visibility = Visibility.Collapsed;
                        await OpenAppInTabAsync($"{name}: {q}", url, icon, switchToTab: true);
                    };
                    _paletteResultsPanel.Children.Add(searchRow);
                }
            }
        }

        private async void OnPaletteSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ToggleCommandPalette();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                string q = (_paletteSearchBox.Text ?? "").Trim();
                _commandPaletteOverlay.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(q))
                {
                    var topApp = _allApps.FirstOrDefault(a => FuzzyMatch(a.Name, q));
                    if (topApp != null)
                        await OpenAppInTabAsync(topApp.Name, topApp.Url, topApp.IconFile, switchToTab: true);
                    else
                        await ExecuteOmniboxQueryAsync(q);
                }
            }
        }

        // =====================================================================
        // WORKSPACE SWITCHING & SHA-256 PIN SECURITY OVERLAY
        // =====================================================================
        private Grid BuildPinLockOverlay()
        {
            var overlay = new Grid
            {
                Background = Md3Theme.Surface,
                Visibility = Visibility.Collapsed
            };
            _pinLockCard = new Border
            {
                Width = 380,
                Background = Md3Theme.SurfaceContainer,
                BorderBrush = Md3Theme.Primary,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var stack = new StackPanel();
            _pinPromptLabel = new TextBlock
            {
                Text = "Profile Locked | Enter PIN",
                Foreground = Md3Theme.TextPrimary,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(_pinPromptLabel);

            _pinInputBox = new PasswordBox
            {
                Height = 36,
                Background = Md3Theme.SurfaceVariant,
                Foreground = Md3Theme.TextPrimary,
                BorderBrush = Md3Theme.Outline,
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 14
            };
            _pinInputBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) VerifyOrSetPin(); };
            stack.Children.Add(_pinInputBox);

            var unlockBtn = CreateToolbarButton("Unlock / Confirm PIN", "Verify SHA-256 PIN");
            unlockBtn.Height = 36;
            unlockBtn.Margin = new Thickness(0, 12, 0, 0);
            unlockBtn.Click += (s, e) => VerifyOrSetPin();
            stack.Children.Add(unlockBtn);

            _pinLockCard.Child = stack;
            overlay.Children.Add(_pinLockCard);
            return overlay;
        }

        private void PromptOrLockWorkspace()
        {
            var ws = GetActiveWorkspace();
            foreach (var t in _tabs)
            {
                if (t.WebView != null) t.WebView.Visibility = Visibility.Collapsed;
            }
            _pinInputBox.Password = "";
            if (string.IsNullOrEmpty(ws.PinSha256))
            {
                _pinPromptLabel.Text = $"Set New PIN for '{ws.Name}' Profile:";
            }
            else
            {
                _pinPromptLabel.Text = $"'{ws.Name}' Locked | Enter PIN:";
                _workspaceLocked = true;
            }
            _pinLockOverlay.Visibility = Visibility.Visible;
            _pinInputBox.Focus();
        }

        private void VerifyOrSetPin()
        {
            var ws = GetActiveWorkspace();
            string raw = _pinInputBox.Password.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            string hash = ComputeSha256(raw);
            if (string.IsNullOrEmpty(ws.PinSha256))
            {
                ws.PinSha256 = hash;
                SaveConfig();
                _pinLockOverlay.Visibility = Visibility.Collapsed;
                if (_homeDashboardOverlay.Visibility != Visibility.Visible && _activeTab != null)
                    ActivateTab(_activeTab);
                ShowToast($"PIN protection enabled for '{ws.Name}'.");
            }
            else if (ws.PinSha256 == hash)
            {
                _workspaceLocked = false;
                _pinLockOverlay.Visibility = Visibility.Collapsed;
                if (_homeDashboardOverlay.Visibility != Visibility.Visible && _activeTab != null)
                    ActivateTab(_activeTab);
                ShowToast($"Profile '{ws.Name}' unlocked.");
            }
            else
            {
                _pinPromptLabel.Text = "Incorrect PIN | Try Again:";
                _pinInputBox.Password = "";
            }
        }

        private async Task SwitchWorkspaceAsync(string workspaceId)
        {
            if (_config.ActiveWorkspaceId == workspaceId) return;
            SaveConfig();

            foreach (var t in _tabs.ToList())
            {
                if (t.WebView != null)
                {
                    _contentHostGrid.Children.Remove(t.WebView);
                    t.WebView.Dispose();
                }
            }
            _tabs.Clear();
            _activeTab = null;

            _config.ActiveWorkspaceId = workspaceId;
            var ws = GetActiveWorkspace();
            UpdateProfilesDropdownButton();
            ShowLauncherDashboard();

            if (!string.IsNullOrEmpty(ws.PinSha256))
            {
                PromptOrLockWorkspace();
            }

            foreach (var savedUrl in ws.OpenTabUrls.Take(5))
            {
                await OpenAppInTabAsync("Restored Tab", savedUrl, ResolveIconForUrl(savedUrl, ""), switchToTab: false);
            }
        }

        // =====================================================================
        // OMNIBOX, SCOPED SEARCH & DIALOG HELPERS
        // =====================================================================
        private void OnSearchEngineSelectorClick(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu
            {
                PlacementTarget = _searchEnginePillBtn,
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                BorderBrush = Md3Theme.Outline
            };
            foreach (var se in _searchEngines)
            {
                var item = new MenuItem
                {
                    Header = $"{se.Name}   (prefix: '{se.Prefix} ')",
                    Foreground = Md3Theme.TextPrimary,
                    Icon = CreateCrispIconImage(se.IconFile, 16)
                };
                var captured = se;
                item.Click += (s, args) =>
                {
                    _activeSearchEngine = captured;
                    _config.DefaultSearchEngineId = captured.Id;
                    _searchEnginePillBtn.Content = $"{captured.Name} ▾";
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private async void OnOmniboxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string input = (_omniboxInput.Text ?? "").Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    await ExecuteOmniboxQueryAsync(input);
                }
            }
        }

        private async Task ExecuteOmniboxQueryAsync(string rawInput)
        {
            var parts = rawInput.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var prefixEngine = _searchEngines.FirstOrDefault(s =>
                    string.Equals(s.Prefix, parts[0], StringComparison.OrdinalIgnoreCase));
                if (prefixEngine != null)
                {
                    string qUrl = string.Format(prefixEngine.UrlTemplate, Uri.EscapeDataString(parts[1]));
                    await NavigateOrOpenTabAsync($"{prefixEngine.Name}: {parts[1]}", qUrl, prefixEngine.IconFile);
                    return;
                }
            }

            string targetUrl = NormalizeUrlOrSearch(rawInput);
            await NavigateOrOpenTabAsync("Search", targetUrl, _activeSearchEngine.IconFile);
        }

        private async Task NavigateOrOpenTabAsync(string title, string url, string iconFile)
        {
            if (_activeTab != null && _homeDashboardOverlay.Visibility != Visibility.Visible && _activeTab.WebView?.CoreWebView2 != null)
            {
                _activeTab.WebView.CoreWebView2.Navigate(url);
            }
            else
            {
                await OpenAppInTabAsync(title, url, iconFile, switchToTab: true);
            }
        }

        private string NormalizeUrlOrSearch(string input)
        {
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return input;

            if (!input.Contains(" ") && input.Contains("."))
                return "https://" + input;

            return string.Format(_activeSearchEngine.UrlTemplate, Uri.EscapeDataString(input));
        }

        private void ToggleFavorite(GoogleAppDef app)
        {
            app.IsFavorite = !app.IsFavorite;
            if (app.IsFavorite && !_config.FavoriteAppIds.Contains(app.Id))
                _config.FavoriteAppIds.Add(app.Id);
            else if (!app.IsFavorite)
                _config.FavoriteAppIds.Remove(app.Id);

            SaveConfig();
            RenderSidebarFavorites();
            RenderDashboardCards();
        }

        private void ShowAddCustomAppDialog()
        {
            var dlg = new Window
            {
                Title = "Add Custom Web App",
                Width = 420,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                FontFamily = UiFont,
                Icon = Icon
            };
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "App Name:", Foreground = Md3Theme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) });
            var nameBox = new TextBox { Height = 32, Background = Md3Theme.SurfaceVariant, Foreground = Md3Theme.TextPrimary, BorderBrush = Md3Theme.Outline, Padding = new Thickness(8, 5, 8, 5) };
            stack.Children.Add(nameBox);

            stack.Children.Add(new TextBlock { Text = "HTTPS URL:", Foreground = Md3Theme.TextSecondary, Margin = new Thickness(0, 12, 0, 4) });
            var urlBox = new TextBox { Height = 32, Background = Md3Theme.SurfaceVariant, Foreground = Md3Theme.TextPrimary, BorderBrush = Md3Theme.Outline, Padding = new Thickness(8, 5, 8, 5), Text = "https://" };
            stack.Children.Add(urlBox);

            var saveBtn = CreateToolbarButton("Save Custom App", "Add to Workspace Hub");
            saveBtn.Height = 34;
            saveBtn.Margin = new Thickness(0, 18, 0, 0);
            saveBtn.Click += (s, e) =>
            {
                string n = nameBox.Text.Trim();
                string u = urlBox.Text.Trim();
                if (!string.IsNullOrEmpty(n) && !string.IsNullOrEmpty(u))
                {
                    string finalUrl = u.StartsWith("http") ? u : "https://" + u;
                    var custom = new GoogleAppDef
                    {
                        Id = "custom_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                        Name = n,
                        Url = finalUrl,
                        IconFile = ResolveIconForUrl(finalUrl, "google-apps-for-work.png"),
                        IsCustom = true,
                        IsFavorite = true
                    };
                    _config.CustomApps.Add(custom);
                    _config.FavoriteAppIds.Add(custom.Id);
                    SaveConfig();
                    RebuildAppCatalog();
                    RenderSidebarFavorites();
                    RenderDashboardCards();
                    dlg.Close();
                }
            };
            stack.Children.Add(saveBtn);
            dlg.Content = stack;
            dlg.ShowDialog();
        }

        private void CreateNewWorkspaceDialog()
        {
            var dlg = new Window
            {
                Title = "Create Isolated Profile",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                FontFamily = UiFont,
                Icon = Icon
            };
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Profile Label (e.g., Personal, Work, Client):", Foreground = Md3Theme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) });
            var nameBox = new TextBox { Height = 32, Background = Md3Theme.SurfaceVariant, Foreground = Md3Theme.TextPrimary, BorderBrush = Md3Theme.Outline, Padding = new Thickness(8, 5, 8, 5) };
            stack.Children.Add(nameBox);

            stack.Children.Add(new TextBlock { Text = "Account Email:", Foreground = Md3Theme.TextSecondary, Margin = new Thickness(0, 12, 0, 4) });
            var emailBox = new TextBox { Height = 32, Background = Md3Theme.SurfaceVariant, Foreground = Md3Theme.TextPrimary, BorderBrush = Md3Theme.Outline, Padding = new Thickness(8, 5, 8, 5), Text = "george.christosp@gmail.com" };
            stack.Children.Add(emailBox);

            var createBtn = CreateToolbarButton("Create & Switch Profile", "Creates isolated WebView2 profile folder");
            createBtn.Height = 34;
            createBtn.Margin = new Thickness(0, 16, 0, 0);
            createBtn.Click += async (s, e) =>
            {
                string n = nameBox.Text.Trim();
                string em = emailBox.Text.Trim();
                if (!string.IsNullOrEmpty(n))
                {
                    string id = new string(n.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                    if (string.IsNullOrEmpty(id)) id = "ws_" + Guid.NewGuid().ToString("N").Substring(0, 5);
                    if (_config.Workspaces.All(w => w.Id != id))
                    {
                        _config.Workspaces.Add(new WorkspaceProfile
                        {
                            Id = id,
                            Name = n,
                            DetectedAccount = string.IsNullOrEmpty(em) ? "george.christosp@gmail.com" : em
                        });
                        SaveConfig();
                    }
                    dlg.Close();
                    await SwitchWorkspaceAsync(id);
                }
            };
            stack.Children.Add(createBtn);
            dlg.Content = stack;
            dlg.ShowDialog();
        }

        // =====================================================================
        // LIFECYCLE, HOTKEYS & DYNAMIC WINDOWS OS THEME SYNC (DWM CAPTION COLOR)
        // =====================================================================
        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyWindows11ThemeChrome();
            RegisterGlobalHotkeys();

            UpdateProfilesDropdownButton();
            RenderSidebarFavorites();
            RenderDashboardCards();
            RenderTabStrip();

            if (_config.AiDrawerOpen)
            {
                await NavigateAiDrawerAsync(_config.ActiveAiProviderUrl);
            }

            var cmdArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < cmdArgs.Length - 1; i++)
            {
                if (cmdArgs[i] == "--verify-screenshot")
                {
                    string outPath = cmdArgs[i + 1];
                    if (cmdArgs.Contains("--demo-tabs"))
                    {
                        _tabs.Add(new BrowserTabEntry { Title = "Inbox (3) - george.christosp@gmail.com", Url = "https://mail.google.com/mail/u/0/#inbox", IconFile = "gmail.png" });
                        _tabs.Add(new BrowserTabEntry { Title = "Google Calendar - Week", Url = "https://calendar.google.com/calendar/u/0/r", IconFile = "google-calendar.png" });
                        _tabs.Add(new BrowserTabEntry { Title = "Google Keep", Url = "https://keep.google.com/", IconFile = "google-keep.png" });
                        _tabs.Add(new BrowserTabEntry { Title = "Google Docs", Url = "https://docs.google.com/document/u/0/", IconFile = "google-docs.png" });
                        _tabs.Add(new BrowserTabEntry { Title = "Google Drive - My Drive", Url = "https://drive.google.com/drive/my-drive", IconFile = "google-drive.png" });
                        RenderTabStrip();
                    }
                    await Task.Delay(400);
                    CaptureWindowRenderToPng(outPath);
                    Close();
                    break;
                }
            }
        }

        private void CaptureWindowRenderToPng(string outPath)
        {
            try
            {
                UpdateLayout();
                int w = (int)Math.Max(1200, ActualWidth);
                int h = (int)Math.Max(800, ActualHeight);
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(_rootGrid);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using var fs = File.Create(outPath);
                enc.Save(fs);
            }
            catch { }
        }

        // Eliminates the Windows teal title bar by painting DWM Caption & Text to match MD3 Surface (#F8FAFD Light / #131314 Dark)
        private void ApplyWindows11ThemeChrome()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int dark = Md3Theme.IsDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE

                // COLORREF format is 0x00BBGGRR
                int captionColorRef = Md3Theme.IsDark ? 0x00141313 : 0x00FDFAF8; // #131314 Dark / #F8FAFD Light
                DwmSetWindowAttribute(hwnd, 35, ref captionColorRef, sizeof(int)); // DWMWA_CAPTION_COLOR

                int textColorRef = Md3Theme.IsDark ? 0x00E3E3E3 : 0x001F1F1F; // #E3E3E3 Dark / #1F1F1F Light
                DwmSetWindowAttribute(hwnd, 36, ref textColorRef, sizeof(int)); // DWMWA_TEXT_COLOR

                int borderColorRef = Md3Theme.IsDark ? 0x00373533 : 0x00E0E2E0;
                DwmSetWindowAttribute(hwnd, 34, ref borderColorRef, sizeof(int)); // DWMWA_BORDER_COLOR
            }
            catch { }
        }

        private void ApplyLiveThemeUpdate()
        {
            ApplyWindows11ThemeChrome();

            Background = Md3Theme.Surface;
            Foreground = Md3Theme.TextPrimary;
            _rootGrid.Background = Md3Theme.Surface;
            _topBarBorder.Background = Md3Theme.Surface;
            _topBarBorder.BorderBrush = Md3Theme.Outline;
            _profilesDropdownBtn.Background = Md3Theme.SurfaceContainer;
            _profilesDropdownBtn.BorderBrush = Md3Theme.Outline;
            _profilesDropdownLabel.Foreground = Md3Theme.TextPrimary;

            _navBarBorder.Background = Md3Theme.SurfaceContainer;
            _navBarBorder.BorderBrush = Md3Theme.Outline;
            _appsGridBtn.Background = Md3Theme.PrimaryContainer;
            _appsGridBtn.Foreground = Md3Theme.OnPrimaryContainer;

            _omniBorder.Background = Md3Theme.SurfaceVariant;
            _omniBorder.BorderBrush = Md3Theme.Outline;
            _searchEnginePillBtn.Foreground = Md3Theme.Primary;
            _omniboxInput.Foreground = Md3Theme.TextPrimary;
            _omniboxInput.CaretBrush = Md3Theme.Primary;

            _sidebarBorder.Background = Md3Theme.Surface;
            _sidebarBorder.BorderBrush = Md3Theme.Outline;
            _contentHostGrid.Background = Md3Theme.Surface;

            _homeDashboardOverlay.Background = Md3Theme.Surface;
            _hubTitleText.Foreground = Md3Theme.TextPrimary;
            _hubSubtitleText.Foreground = Md3Theme.TextSecondary;
            _hubSearchBorder.Background = Md3Theme.SurfaceContainer;
            _hubSearchBorder.BorderBrush = Md3Theme.Outline;
            _homeFilterBox.Foreground = Md3Theme.TextPrimary;

            RenderSidebarFavorites();
            RenderDashboardCards();
            RenderTabStrip();
        }

        private void RegisterGlobalHotkeys()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                HwndSource source = HwndSource.FromHwnd(hwnd);
                source.AddHook(HwndHook);
                RegisterHotKey(hwnd, HOTKEY_ID_ALT_SPACE, MOD_ALT, VK_SPACE);
                RegisterHotKey(hwnd, HOTKEY_ID_CTRL_SHIFT_G, MOD_CONTROL | MOD_SHIFT, VK_G);
            }
            catch { }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_SETTINGCHANGE = 0x001A;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_ALT_SPACE || id == HOTKEY_ID_CTRL_SHIFT_G)
                {
                    if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                    Activate();
                    ToggleCommandPalette();
                    handled = true;
                }
            }
            else if (msg == WM_SETTINGCHANGE)
            {
                if (Md3Theme.SyncWithOsTheme())
                {
                    Dispatcher.BeginInvoke(new Action(ApplyLiveThemeUpdate));
                }
            }
            return IntPtr.Zero;
        }

        private void RegisterKeyboardShortcuts()
        {
            PreviewKeyDown += (s, e) =>
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    if (e.Key == Key.K) { e.Handled = true; ToggleCommandPalette(); }
                    else if (e.Key == Key.T) { e.Handled = true; ShowLauncherDashboard(); }
                    else if (e.Key == Key.W && _activeTab != null) { e.Handled = true; CloseTab(_activeTab); }
                    else if (e.Key == Key.L) { e.Handled = true; _omniboxInput.Focus(); }
                    else if (e.Key == Key.B) { e.Handled = true; ToggleSidebarCollapse(); }
                }
                else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                {
                    if (e.Key == Key.S) { e.Handled = true; ToggleSplitView(); }
                    else if (e.Key == Key.A) { e.Handled = true; ToggleAiDrawer(); }
                    else if (e.Key == Key.P) { e.Handled = true; OpenMiniPlayerForActiveTab(); }
                }
                else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.N)
                {
                    e.Handled = true;
                    OpenDedicatedPopoutWindow(_activeTab?.Title ?? "Google Workspace", _activeTab?.Url ?? "https://www.google.com");
                }
            };
        }

        private void ShowToast(string message)
        {
            _toastText.Text = message;
            _toastBanner.Visibility = Visibility.Visible;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.2) };
            timer.Tick += (s, e) =>
            {
                _toastBanner.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        // =====================================================================
        // UTILITY HELPERS
        // =====================================================================
        private static Button CreateToolbarButton(string text, string tooltip)
        {
            return new Button
            {
                Content = text,
                ToolTip = tooltip,
                Background = Md3Theme.SurfaceContainer,
                Foreground = Md3Theme.TextPrimary,
                BorderBrush = Md3Theme.Outline,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 11.5,
                Cursor = Cursors.Hand
            };
        }

        private static bool FuzzyMatch(string text, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            int qi = 0;
            for (int ti = 0; ti < text.Length && qi < query.Length; ti++)
            {
                if (char.ToLowerInvariant(text[ti]) == char.ToLowerInvariant(query[qi]))
                    qi++;
            }
            return qi == query.Length;
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string logDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GSuiteContainer");
            Directory.CreateDirectory(logDir);
            string logFile = System.IO.Path.Combine(logDir, "startup.log");
            try
            {
                File.WriteAllText(logFile, $"[{DateTime.Now:O}] Initializing GSuiteContainer V2 (Material Design 3)...\r\n");
                var app = new Application();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    File.AppendAllText(logFile, $"[DISPATCHER_ERR] {e.Exception}\r\n");
                    e.Handled = true;
                };
                var win = new MainWindow();
                win.Loaded += (s, e) => File.AppendAllText(logFile, $"[{DateTime.Now:O}] MainWindow Loaded OK! Title={win.Title} | IsDark={Md3Theme.IsDark}\r\n");
                app.Run(win);
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"[FATAL] {ex}\r\n");
            }
        }
    }
}

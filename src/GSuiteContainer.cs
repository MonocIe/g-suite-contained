using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
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
        public string Category { get; set; } = "Core Productivity";
        public string Url { get; set; } = "";
        public string Badge { get; set; } = "G";
        public string AccentHex { get; set; } = "#8AB4F8";
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
        public string AccentHex { get; set; } = "#8AB4F8";
    }

    public class WorkspaceProfile
    {
        public string Id { get; set; } = "personal";
        public string Name { get; set; } = "Personal";
        public string AccentHex { get; set; } = "#8AB4F8";
        public string PinSha256 { get; set; } = "";
        public string DetectedAccount { get; set; } = "";
        public List<string> OpenTabUrls { get; set; } = new List<string>();
    }

    public class AppConfig
    {
        public string ActiveWorkspaceId { get; set; } = "personal";
        public bool AdBlockEnabled { get; set; } = true;
        public bool AutoHideSidebar { get; set; } = false;
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
            new WorkspaceProfile { Id = "personal", Name = "Personal", AccentHex = "#8AB4F8" },
            new WorkspaceProfile { Id = "work", Name = "Work / Corp", AccentHex = "#81C995" },
            new WorkspaceProfile { Id = "executive", Name = "Executive Vault", AccentHex = "#FDD663" }
        };
    }

    public class BrowserTabEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "New Tab";
        public string Url { get; set; } = "about:blank";
        public string Badge { get; set; } = "G";
        public string AccentHex { get; set; } = "#8AB4F8";
        public bool IsHibernated { get; set; }
        public bool IsHomeTab { get; set; }
        public WebView2? WebView { get; set; }
        public Border? TabChipBorder { get; set; }
        public TextBlock? TabTitleText { get; set; }
        public TextBlock? TabStateBadge { get; set; }
    }

    // =========================================================================
    // CATALOG & ENGINE REGISTRY (CONSOLIDATED FROM ALL 6 APPS)
    // =========================================================================
    public static class CatalogRegistry
    {
        public static List<SearchEngineDef> GetSearchEngines() => new List<SearchEngineDef>
        {
            new SearchEngineDef { Id = "google", Name = "Google Web", Prefix = "g", UrlTemplate = "https://www.google.com/search?q={0}", AccentHex = "#8AB4F8" },
            new SearchEngineDef { Id = "youtube", Name = "YouTube", Prefix = "yt", UrlTemplate = "https://www.youtube.com/results?search_query={0}", AccentHex = "#F28B82" },
            new SearchEngineDef { Id = "gmail", Name = "Gmail Mail", Prefix = "m", UrlTemplate = "https://mail.google.com/mail/u/0/#search/{0}", AccentHex = "#F28B82" },
            new SearchEngineDef { Id = "drive", Name = "Google Drive", Prefix = "d", UrlTemplate = "https://drive.google.com/drive/search?q={0}", AccentHex = "#FDD663" },
            new SearchEngineDef { Id = "docs", Name = "Google Docs", Prefix = "doc", UrlTemplate = "https://docs.google.com/document/u/0/?q={0}", AccentHex = "#8AB4F8" },
            new SearchEngineDef { Id = "calendar", Name = "Calendar", Prefix = "cal", UrlTemplate = "https://calendar.google.com/calendar/u/0/r/search?q={0}", AccentHex = "#8AB4F8" },
            new SearchEngineDef { Id = "maps", Name = "Google Maps", Prefix = "map", UrlTemplate = "https://www.google.com/maps/search/{0}", AccentHex = "#81C995" },
            new SearchEngineDef { Id = "scholar", Name = "Scholar", Prefix = "sch", UrlTemplate = "https://scholar.google.com/scholar?q={0}", AccentHex = "#8AB4F8" },
            new SearchEngineDef { Id = "gemini", Name = "Gemini AI", Prefix = "ai", UrlTemplate = "https://gemini.google.com/app?q={0}", AccentHex = "#C58AF9" },
            new SearchEngineDef { Id = "perplexity", Name = "Perplexity", Prefix = "px", UrlTemplate = "https://www.perplexity.ai/search?q={0}", AccentHex = "#78D9EC" },
            new SearchEngineDef { Id = "duckduckgo", Name = "DuckDuckGo", Prefix = "ddg", UrlTemplate = "https://duckduckgo.com/?q={0}", AccentHex = "#F6AEA9" },
            new SearchEngineDef { Id = "github", Name = "GitHub", Prefix = "gh", UrlTemplate = "https://github.com/search?q={0}", AccentHex = "#E8EAED" }
        };

        public static List<(string Label, string Badge, string Url, string Hex)> GetQuickCreateActions() => new List<(string, string, string, string)>
        {
            ("+ New Doc", "Doc", "https://docs.google.com/document/create", "#8AB4F8"),
            ("+ New Sheet", "Xls", "https://docs.google.com/spreadsheets/create", "#81C995"),
            ("+ New Slide", "Ppt", "https://docs.google.com/presentation/create", "#FDD663"),
            ("+ New Form", "Frm", "https://docs.google.com/forms/create", "#C58AF9"),
            ("+ New Event", "Cal", "https://calendar.google.com/calendar/u/0/r/eventedit", "#8AB4F8"),
            ("+ New Meet", "Vid", "https://meet.google.com/new", "#81C995"),
            ("+ New Note", "Kep", "https://keep.google.com/#NOTE", "#FDD663")
        };

        public static List<(string Name, string Badge, string Url, string Hex)> GetAiProviders() => new List<(string, string, string, string)>
        {
            ("Gemini", "Gem", "https://gemini.google.com/app", "#C58AF9"),
            ("NotebookLM", "NLM", "https://notebooklm.google.com/", "#81C995"),
            ("AI Studio", "AIS", "https://aistudio.google.com/", "#8AB4F8"),
            ("ChatGPT", "GPT", "https://chatgpt.com/", "#78D9EC"),
            ("Claude", "Cld", "https://claude.ai/", "#F6AEA9"),
            ("Perplexity", "Ppx", "https://www.perplexity.ai/", "#8AB4F8")
        };

        public static List<GoogleAppDef> GetBuiltInCatalog() => new List<GoogleAppDef>
        {
            // 1. Core Productivity
            new GoogleAppDef {
                Id = "gmail", Name = "Gmail", Category = "Core Productivity", Badge = "M", AccentHex = "#F28B82",
                Url = "https://mail.google.com/mail/u/0/#inbox",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Compose Email", Url = "https://mail.google.com/mail/u/0/#inbox?compose=new" },
                    new SubItemDef { Name = "Inbox", Url = "https://mail.google.com/mail/u/0/#inbox" },
                    new SubItemDef { Name = "Starred", Url = "https://mail.google.com/mail/u/0/#starred" },
                    new SubItemDef { Name = "Sent Mail", Url = "https://mail.google.com/mail/u/0/#sent" },
                    new SubItemDef { Name = "Drafts", Url = "https://mail.google.com/mail/u/0/#drafts" },
                    new SubItemDef { Name = "All Mail", Url = "https://mail.google.com/mail/u/0/#all" },
                    new SubItemDef { Name = "Spam", Url = "https://mail.google.com/mail/u/0/#spam" },
                    new SubItemDef { Name = "Trash", Url = "https://mail.google.com/mail/u/0/#trash" }
                }
            },
            new GoogleAppDef {
                Id = "calendar", Name = "Google Calendar", Category = "Core Productivity", Badge = "31", AccentHex = "#8AB4F8",
                Url = "https://calendar.google.com/calendar/u/0/r",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ New Event", Url = "https://calendar.google.com/calendar/u/0/r/eventedit" },
                    new SubItemDef { Name = "Day View", Url = "https://calendar.google.com/calendar/u/0/r/day" },
                    new SubItemDef { Name = "Week View", Url = "https://calendar.google.com/calendar/u/0/r/week" },
                    new SubItemDef { Name = "Month View", Url = "https://calendar.google.com/calendar/u/0/r/month" },
                    new SubItemDef { Name = "Schedule / Agenda", Url = "https://calendar.google.com/calendar/u/0/r/agenda" }
                }
            },
            new GoogleAppDef {
                Id = "drive", Name = "Google Drive", Category = "Core Productivity", Badge = "Drv", AccentHex = "#FDD663",
                Url = "https://drive.google.com/drive/my-drive",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "My Drive", Url = "https://drive.google.com/drive/my-drive" },
                    new SubItemDef { Name = "Shared with me", Url = "https://drive.google.com/drive/shared-with-me" },
                    new SubItemDef { Name = "Recent Files", Url = "https://drive.google.com/drive/recent" },
                    new SubItemDef { Name = "Starred", Url = "https://drive.google.com/drive/starred" },
                    new SubItemDef { Name = "Storage Quota", Url = "https://drive.google.com/drive/quota" },
                    new SubItemDef { Name = "Trash", Url = "https://drive.google.com/drive/trash" }
                }
            },
            new GoogleAppDef {
                Id = "keep", Name = "Google Keep", Category = "Core Productivity", Badge = "Kep", AccentHex = "#FDD663",
                Url = "https://keep.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Notes", Url = "https://keep.google.com/#home" },
                    new SubItemDef { Name = "Reminders", Url = "https://keep.google.com/#reminders" },
                    new SubItemDef { Name = "Archive", Url = "https://keep.google.com/#archive" },
                    new SubItemDef { Name = "Trash", Url = "https://keep.google.com/#trash" }
                }
            },
            new GoogleAppDef {
                Id = "tasks", Name = "Google Tasks", Category = "Core Productivity", Badge = "Tsk", AccentHex = "#8AB4F8",
                Url = "https://tasks.google.com/embed/?origin=https://calendar.google.com&fullWidth=1"
            },
            new GoogleAppDef {
                Id = "contacts", Name = "Google Contacts", Category = "Core Productivity", Badge = "Cnt", AccentHex = "#8AB4F8",
                Url = "https://contacts.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "All Contacts", Url = "https://contacts.google.com/" },
                    new SubItemDef { Name = "Frequently Contacted", Url = "https://contacts.google.com/frequent" },
                    new SubItemDef { Name = "Merge & Fix", Url = "https://contacts.google.com/suggestions" }
                }
            },
            new GoogleAppDef {
                Id = "account", Name = "Google Account", Category = "Core Productivity", Badge = "Acc", AccentHex = "#8AB4F8",
                Url = "https://myaccount.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Security & Sign-in", Url = "https://myaccount.google.com/security" },
                    new SubItemDef { Name = "Data & Privacy", Url = "https://myaccount.google.com/data-and-privacy" },
                    new SubItemDef { Name = "Payments & Subscriptions", Url = "https://myaccount.google.com/payments-and-subscriptions" },
                    new SubItemDef { Name = "Password Manager", Url = "https://passwords.google.com/" }
                }
            },

            // 2. Docs & Creation
            new GoogleAppDef {
                Id = "docs", Name = "Google Docs", Category = "Docs & Creation", Badge = "Doc", AccentHex = "#8AB4F8",
                Url = "https://docs.google.com/document/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Doc", Url = "https://docs.google.com/document/create" },
                    new SubItemDef { Name = "All Documents", Url = "https://docs.google.com/document/u/0/" },
                    new SubItemDef { Name = "Template Gallery", Url = "https://docs.google.com/document/u/0/?ftv=1" }
                }
            },
            new GoogleAppDef {
                Id = "sheets", Name = "Google Sheets", Category = "Docs & Creation", Badge = "Xls", AccentHex = "#81C995",
                Url = "https://docs.google.com/spreadsheets/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Sheet", Url = "https://docs.google.com/spreadsheets/create" },
                    new SubItemDef { Name = "All Spreadsheets", Url = "https://docs.google.com/spreadsheets/u/0/" },
                    new SubItemDef { Name = "Template Gallery", Url = "https://docs.google.com/spreadsheets/u/0/?ftv=1" }
                }
            },
            new GoogleAppDef {
                Id = "slides", Name = "Google Slides", Category = "Docs & Creation", Badge = "Ppt", AccentHex = "#FDD663",
                Url = "https://docs.google.com/presentation/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Presentation", Url = "https://docs.google.com/presentation/create" },
                    new SubItemDef { Name = "All Presentations", Url = "https://docs.google.com/presentation/u/0/" },
                    new SubItemDef { Name = "Template Gallery", Url = "https://docs.google.com/presentation/u/0/?ftv=1" }
                }
            },
            new GoogleAppDef {
                Id = "forms", Name = "Google Forms", Category = "Docs & Creation", Badge = "Frm", AccentHex = "#C58AF9",
                Url = "https://docs.google.com/forms/u/0/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Create Blank Form", Url = "https://docs.google.com/forms/create" },
                    new SubItemDef { Name = "All Forms", Url = "https://docs.google.com/forms/u/0/" }
                }
            },
            new GoogleAppDef {
                Id = "sites", Name = "Google Sites", Category = "Docs & Creation", Badge = "Sit", AccentHex = "#8AB4F8",
                Url = "https://sites.google.com/new"
            },
            new GoogleAppDef {
                Id = "drawings", Name = "Google Drawings", Category = "Docs & Creation", Badge = "Drw", AccentHex = "#F28B82",
                Url = "https://docs.google.com/drawings/create"
            },
            new GoogleAppDef {
                Id = "fonts", Name = "Google Fonts", Category = "Docs & Creation", Badge = "Aa", AccentHex = "#E8EAED",
                Url = "https://fonts.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Font Catalog", Url = "https://fonts.google.com/" },
                    new SubItemDef { Name = "Material Symbols & Icons", Url = "https://fonts.google.com/icons" }
                }
            },

            // 3. Communication
            new GoogleAppDef {
                Id = "meet", Name = "Google Meet", Category = "Communication", Badge = "Met", AccentHex = "#81C995",
                Url = "https://meet.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "+ Start Instant Meeting", Url = "https://meet.google.com/new" },
                    new SubItemDef { Name = "Meet Home / Schedule", Url = "https://meet.google.com/" }
                }
            },
            new GoogleAppDef {
                Id = "chat", Name = "Google Chat", Category = "Communication", Badge = "Cht", AccentHex = "#81C995",
                Url = "https://mail.google.com/chat/u/0/#chat/home"
            },
            new GoogleAppDef {
                Id = "voice", Name = "Google Voice", Category = "Communication", Badge = "Vox", AccentHex = "#81C995",
                Url = "https://voice.google.com/u/0/calls",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Calls", Url = "https://voice.google.com/u/0/calls" },
                    new SubItemDef { Name = "Messages", Url = "https://voice.google.com/u/0/messages" },
                    new SubItemDef { Name = "Voicemail", Url = "https://voice.google.com/u/0/voicemail" }
                }
            },
            new GoogleAppDef {
                Id = "messages", Name = "Google Messages", Category = "Communication", Badge = "SMS", AccentHex = "#8AB4F8",
                Url = "https://messages.google.com/web/conversations"
            },
            new GoogleAppDef {
                Id = "groups", Name = "Google Groups", Category = "Communication", Badge = "Grp", AccentHex = "#8AB4F8",
                Url = "https://groups.google.com/my-groups"
            },

            // 4. Cloud, AI & Dev
            new GoogleAppDef {
                Id = "gemini", Name = "Google Gemini", Category = "Cloud, AI & Dev", Badge = "Gem", AccentHex = "#C58AF9",
                Url = "https://gemini.google.com/app",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Gemini Chat", Url = "https://gemini.google.com/app" },
                    new SubItemDef { Name = "Gems Manager", Url = "https://gemini.google.com/gems/view" }
                }
            },
            new GoogleAppDef {
                Id = "notebooklm", Name = "NotebookLM", Category = "Cloud, AI & Dev", Badge = "NLM", AccentHex = "#81C995",
                Url = "https://notebooklm.google.com/"
            },
            new GoogleAppDef {
                Id = "aistudio", Name = "Google AI Studio", Category = "Cloud, AI & Dev", Badge = "AIS", AccentHex = "#8AB4F8",
                Url = "https://aistudio.google.com/prompts/new_chat",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Prompt Playground", Url = "https://aistudio.google.com/prompts/new_chat" },
                    new SubItemDef { Name = "API Keys", Url = "https://aistudio.google.com/app/apikey" }
                }
            },
            new GoogleAppDef {
                Id = "cloud", Name = "Google Cloud Console", Category = "Cloud, AI & Dev", Badge = "GCP", AccentHex = "#8AB4F8",
                Url = "https://console.cloud.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Dashboard", Url = "https://console.cloud.google.com/home/dashboard" },
                    new SubItemDef { Name = "IAM & Admin", Url = "https://console.cloud.google.com/iam-admin" },
                    new SubItemDef { Name = "APIs & Services", Url = "https://console.cloud.google.com/apis/dashboard" },
                    new SubItemDef { Name = "Cloud Run", Url = "https://console.cloud.google.com/run" },
                    new SubItemDef { Name = "BigQuery", Url = "https://console.cloud.google.com/bigquery" },
                    new SubItemDef { Name = "Billing", Url = "https://console.cloud.google.com/billing" }
                }
            },
            new GoogleAppDef {
                Id = "colab", Name = "Google Colab", Category = "Cloud, AI & Dev", Badge = "Clb", AccentHex = "#FDD663",
                Url = "https://colab.research.google.com/"
            },
            new GoogleAppDef {
                Id = "firebase", Name = "Firebase Console", Category = "Cloud, AI & Dev", Badge = "Fbs", AccentHex = "#FDD663",
                Url = "https://console.firebase.google.com/"
            },
            new GoogleAppDef {
                Id = "appsscript", Name = "Apps Script", Category = "Cloud, AI & Dev", Badge = "Scr", AccentHex = "#8AB4F8",
                Url = "https://script.google.com/home"
            },

            // 5. Media, Maps & Research
            new GoogleAppDef {
                Id = "youtube", Name = "YouTube (Ad-Pruned)", Category = "Media, Maps & Research", Badge = "YT", AccentHex = "#F28B82",
                Url = "https://www.youtube.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Home Feed", Url = "https://www.youtube.com/" },
                    new SubItemDef { Name = "Subscriptions", Url = "https://www.youtube.com/feed/subscriptions" },
                    new SubItemDef { Name = "Watch Later", Url = "https://www.youtube.com/playlist?list=WL" },
                    new SubItemDef { Name = "History", Url = "https://www.youtube.com/feed/history" },
                    new SubItemDef { Name = "YouTube Studio", Url = "https://studio.youtube.com/" }
                }
            },
            new GoogleAppDef {
                Id = "ytmusic", Name = "YouTube Music", Category = "Media, Maps & Research", Badge = "YTM", AccentHex = "#F28B82",
                Url = "https://music.youtube.com/"
            },
            new GoogleAppDef {
                Id = "photos", Name = "Google Photos", Category = "Media, Maps & Research", Badge = "Pho", AccentHex = "#FDD663",
                Url = "https://photos.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Photos Stream", Url = "https://photos.google.com/" },
                    new SubItemDef { Name = "Albums", Url = "https://photos.google.com/albums" },
                    new SubItemDef { Name = "Favorites", Url = "https://photos.google.com/favorites" },
                    new SubItemDef { Name = "Trash", Url = "https://photos.google.com/trash" }
                }
            },
            new GoogleAppDef {
                Id = "maps", Name = "Google Maps", Category = "Media, Maps & Research", Badge = "Map", AccentHex = "#81C995",
                Url = "https://www.google.com/maps",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Explore Map", Url = "https://www.google.com/maps" },
                    new SubItemDef { Name = "Directions", Url = "https://www.google.com/maps/dir/" },
                    new SubItemDef { Name = "Saved Places", Url = "https://www.google.com/maps/contrib/" }
                }
            },
            new GoogleAppDef {
                Id = "earth", Name = "Google Earth", Category = "Media, Maps & Research", Badge = "Ert", AccentHex = "#8AB4F8",
                Url = "https://earth.google.com/web/"
            },
            new GoogleAppDef {
                Id = "translate", Name = "Google Translate", Category = "Media, Maps & Research", Badge = "Trn", AccentHex = "#8AB4F8",
                Url = "https://translate.google.com/"
            },
            new GoogleAppDef {
                Id = "scholar", Name = "Google Scholar", Category = "Media, Maps & Research", Badge = "Sch", AccentHex = "#8AB4F8",
                Url = "https://scholar.google.com/"
            },
            new GoogleAppDef {
                Id = "news", Name = "Google News", Category = "Media, Maps & Research", Badge = "Nws", AccentHex = "#8AB4F8",
                Url = "https://news.google.com/"
            },
            new GoogleAppDef {
                Id = "travel", Name = "Flights & Travel", Category = "Media, Maps & Research", Badge = "Flt", AccentHex = "#8AB4F8",
                Url = "https://www.google.com/travel/flights",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Google Flights", Url = "https://www.google.com/travel/flights" },
                    new SubItemDef { Name = "Hotels", Url = "https://www.google.com/travel/hotels" },
                    new SubItemDef { Name = "Trips", Url = "https://www.google.com/travel/" }
                }
            },

            // 6. Business & Analytics
            new GoogleAppDef {
                Id = "admin", Name = "Workspace Admin", Category = "Business & Analytics", Badge = "Adm", AccentHex = "#8AB4F8",
                Url = "https://admin.google.com/",
                SubItems = new List<SubItemDef> {
                    new SubItemDef { Name = "Admin Console", Url = "https://admin.google.com/" },
                    new SubItemDef { Name = "Users Directory", Url = "https://admin.google.com/ac/users" },
                    new SubItemDef { Name = "Groups", Url = "https://admin.google.com/ac/groups" },
                    new SubItemDef { Name = "Billing", Url = "https://admin.google.com/ac/billing" }
                }
            },
            new GoogleAppDef {
                Id = "finance", Name = "Google Finance", Category = "Business & Analytics", Badge = "Fin", AccentHex = "#81C995",
                Url = "https://www.google.com/finance/"
            },
            new GoogleAppDef {
                Id = "analytics", Name = "Google Analytics", Category = "Business & Analytics", Badge = "GA", AccentHex = "#FDD663",
                Url = "https://analytics.google.com/"
            },
            new GoogleAppDef {
                Id = "trends", Name = "Google Trends", Category = "Business & Analytics", Badge = "Trd", AccentHex = "#8AB4F8",
                Url = "https://trends.google.com/trends/"
            },
            new GoogleAppDef {
                Id = "searchconsole", Name = "Search Console", Category = "Business & Analytics", Badge = "GSC", AccentHex = "#8AB4F8",
                Url = "https://search.google.com/search-console"
            }
        };
    }

    // =========================================================================
    // TRUSTED-TYPES-SAFE INJECTED JS & 4-LAYER YOUTUBE / AD-BLOCK SCRIPT
    // (Consolidated from MakoApps Tauri v2 + SVGDesign Notification Bridge)
    // =========================================================================
    public static class InjectedScripts
    {
        public static readonly string[] BlockedUrlPatterns = new[]
        {
            "*://*.doubleclick.net/*",
            "*://*.googlesyndication.com/*",
            "*://*.googleadservices.com/*",
            "*://*.adservice.google.com/*",
            "*://*.google-analytics.com/*",
            "*://*.googletagmanager.com/*",
            "*://*.zedo.com/*",
            "*://*.scorecardresearch.com/*",
            "*://www.youtube.com/api/stats/ads*",
            "*://www.youtube.com/pagead/*",
            "*://www.youtube.com/ptracking*",
            "*://www.youtube.com/get_midroll_*"
        };

        public const string DocumentStartInitScript = @"
(function() {
    if (window.__gsuiteContainerInit) return;
    window.__gsuiteContainerInit = true;

    // 1. Keyboard Navigation Bridge (Ctrl+[ Back, Ctrl+] Forward, Alt+Space Command Palette)
    // Ignores editable inputs so Gmail/Docs indent shortcuts work normally
    window.addEventListener('keydown', function(e) {
        var el = document.activeElement;
        var isEditable = el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.isContentEditable);
        if (!isEditable && e.ctrlKey && !e.shiftKey && !e.altKey) {
            if (e.key === '[') { e.preventDefault(); history.back(); }
            else if (e.key === ']') { e.preventDefault(); history.forward(); }
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

    // 3. Web Notification Bridge -> Container Banner
    try {
        var OrigNotif = window.Notification;
        if (OrigNotif) {
            var WrappedNotif = function(title, opts) {
                try {
                    var body = (opts && opts.body) ? opts.body : '';
                    window.chrome.webview.postMessage('notify:' + title + ' | ' + body);
                } catch (_) {}
                return new OrigNotif(title, opts);
            };
            WrappedNotif.requestPermission = OrigNotif.requestPermission ? OrigNotif.requestPermission.bind(OrigNotif) : function() { return Promise.resolve('granted'); };
            Object.defineProperty(WrappedNotif, 'permission', { get: function() { return 'granted'; } });
            window.Notification = WrappedNotif;
        }
    } catch (_) {}

    // 4. YouTube 4-Layer Ad-Pruner (Active on youtube.com / music.youtube.com)
    if (location.hostname.indexOf('youtube.com') !== -1) {
        var AD_KEYS = ['adPlacements', 'playerAds', 'adSlots', 'adBreakHeartbeatParams'];
        function pruneYtPayload(obj) {
            if (!obj || typeof obj !== 'object') return obj;
            for (var i = 0; i < AD_KEYS.length; i++) {
                if (AD_KEYS[i] in obj) delete obj[AD_KEYS[i]];
            }
            if (obj.playerResponse && typeof obj.playerResponse === 'object') {
                for (var j = 0; j < AD_KEYS.length; j++) {
                    if (AD_KEYS[j] in obj.playerResponse) delete obj.playerResponse[AD_KEYS[j]];
                }
            }
            return obj;
        }

        // Trap ytInitialPlayerResponse setter
        var _ytResp = window.ytInitialPlayerResponse;
        try {
            Object.defineProperty(window, 'ytInitialPlayerResponse', {
                configurable: true,
                get: function() { return _ytResp; },
                set: function(v) { _ytResp = pruneYtPayload(v); }
            });
        } catch (_) {}

        // Wrap JSON.parse for youtubei player endpoints
        var origFetch = window.fetch;
        if (origFetch) {
            window.fetch = function(input, init) {
                var url = (typeof input === 'string') ? input : (input && input.url ? input.url : '');
                if (url.indexOf('/youtubei/v1/player') !== -1 || url.indexOf('/youtubei/v1/next') !== -1) {
                    return origFetch.apply(this, arguments).then(function(resp) {
                        return resp.clone().json().then(function(data) {
                            pruneYtPayload(data);
                            return new Response(JSON.stringify(data), {
                                status: resp.status,
                                statusText: resp.statusText,
                                headers: resp.headers
                            });
                        }).catch(function() { return resp; });
                    });
                }
                return origFetch.apply(this, arguments);
            };
        }

        // Cosmetic CSS & 300ms Ad-Skip Watcher (Trusted-Types-safe textContent)
        window.addEventListener('DOMContentLoaded', function() {
            try {
                var st = document.createElement('style');
                st.textContent = 'ytd-ad-slot-renderer, ytd-banner-promo-renderer, ytd-player-legacy-desktop-watch-ads-renderer, .ytp-ad-overlay-container, #masthead-ad, ytd-in-feed-ad-layout-renderer, ytd-promoted-sparkles-web-renderer { display: none !important; }';
                (document.head || document.documentElement).appendChild(st);
            } catch (_) {}

            setInterval(function() {
                try {
                    var skipBtn = document.querySelector('.ytp-ad-skip-button, .ytp-ad-skip-button-modern, .ytp-skip-ad-button');
                    if (skipBtn) { skipBtn.click(); return; }
                    var player = document.querySelector('.html5-video-player.ad-showing');
                    if (player) {
                        var v = player.querySelector('video');
                        if (v && isFinite(v.duration) && v.duration > 0) {
                            v.muted = true;
                            v.currentTime = v.duration;
                        }
                    }
                } catch (_) {}
            }, 300);
        });
    }
})();
";
    }

    // =========================================================================
    // MAIN WINDOW | ULTIMATE G-SUITE CONTAINER
    // =========================================================================
    public class MainWindow : Window
    {
        // Win32 P/Invoke for DWM Dark Titlebar, Mica & Global Hotkey
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_ALT_SPACE = 9001;
        private const int HOTKEY_ID_CTRL_SHIFT_G = 9002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_SPACE = 0x20;
        private const uint VK_G = 0x47;

        // Design Palette (#171717 / #202124 Minimal Dark Glassmorphism)
        private static readonly SolidColorBrush BgCanvas = BrushFromHex("#171717");
        private static readonly SolidColorBrush BgElevated = BrushFromHex("#202124");
        private static readonly SolidColorBrush BgCard = BrushFromHex("#24262A");
        private static readonly SolidColorBrush BgHover = BrushFromHex("#2E3136");
        private static readonly SolidColorBrush BorderSubtle = BrushFromHex("#33363C");
        private static readonly SolidColorBrush TextPrimary = BrushFromHex("#E8EAED");
        private static readonly SolidColorBrush TextSecondary = BrushFromHex("#9AA0A6");
        private static readonly SolidColorBrush AccentBlue = BrushFromHex("#8AB4F8");
        private static readonly SolidColorBrush AccentGreen = BrushFromHex("#81C995");
        private static readonly SolidColorBrush AccentGold = BrushFromHex("#FDD663");
        private static readonly SolidColorBrush AccentRed = BrushFromHex("#F28B82");
        private static readonly SolidColorBrush AccentPurple = BrushFromHex("#C58AF9");
        private static readonly FontFamily UiFont = new FontFamily("Inter, Segoe UI Variable Display, Segoe UI");

        // State & Storage
        private readonly string _dataRoot;
        private readonly string _configPath;
        private AppConfig _config = new AppConfig();
        private readonly List<GoogleAppDef> _allApps = new List<GoogleAppDef>();
        private readonly List<SearchEngineDef> _searchEngines = CatalogRegistry.GetSearchEngines();
        private SearchEngineDef _activeSearchEngine;
        private readonly Dictionary<string, CoreWebView2Environment> _envCache = new Dictionary<string, CoreWebView2Environment>();

        // Tabs & Split View State
        private readonly List<BrowserTabEntry> _tabs = new List<BrowserTabEntry>();
        private BrowserTabEntry? _activeTab;
        private bool _splitViewActive;
        private WebView2? _splitWebView;
        private WebView2? _aiWebView;
        private string _selectedCategory = "All Services";
        private bool _workspaceLocked;

        // Core UI Controls
        private StackPanel _workspaceSelectorPanel = null!;
        private TextBlock _accountBadgeText = null!;
        private StackPanel _tabStripPanel = null!;
        private TextBox _omniboxInput = null!;
        private Button _searchEnginePillBtn = null!;
        private StackPanel _sidebarFavoritesPanel = null!;
        private Grid _contentHostGrid = null!;
        private Grid _homeDashboardOverlay = null!;
        private WrapPanel _homeAppsWrapPanel = null!;
        private StackPanel _categoryFilterPanel = null!;
        private TextBox _homeFilterBox = null!;
        private Grid _splitPaneContainer = null!;
        private GridSplitter _splitSplitter = null!;
        private Border _splitRightBorder = null!;
        private TextBox _splitUrlBox = null!;
        private Border _aiDrawerBorder = null!;
        private StackPanel _aiProviderTabsPanel = null!;
        private Grid _aiWebViewHost = null!;
        private Grid _commandPaletteOverlay = null!;
        private TextBox _paletteSearchBox = null!;
        private StackPanel _paletteResultsPanel = null!;
        private Grid _pinLockOverlay = null!;
        private PasswordBox _pinInputBox = null!;
        private TextBlock _pinPromptLabel = null!;
        private TextBlock _statusFooterText = null!;
        private TextBlock _memoryFooterText = null!;
        private Border _toastBanner = null!;
        private TextBlock _toastText = null!;
        private DispatcherTimer _telemetryTimer = null!;

        public MainWindow()
        {
            _dataRoot = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GSuiteContainer");
            Directory.CreateDirectory(_dataRoot);
            _configPath = System.IO.Path.Combine(_dataRoot, "config.json");

            LoadConfig();
            _activeSearchEngine = _searchEngines.FirstOrDefault(s => s.Id == _config.DefaultSearchEngineId) ?? _searchEngines[0];
            RebuildAppCatalog();

            Title = "G-Suite Container | Unified Workspace & App Hub";
            Width = 1480;
            Height = 930;
            MinWidth = 960;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = BgCanvas;
            Foreground = TextPrimary;
            FontFamily = UiFont;

            BuildUserInterface();
            RegisterKeyboardShortcuts();

            Loaded += OnMainWindowLoaded;
            Closing += (s, e) => SaveConfig();
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
        }

        private void SaveConfig()
        {
            try
            {
                var ws = GetActiveWorkspace();
                ws.OpenTabUrls = _tabs.Where(t => !t.IsHomeTab && !string.IsNullOrWhiteSpace(t.Url) && t.Url != "about:blank")
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
                custom.Category = "Custom Apps";
                custom.IsFavorite = _config.FavoriteAppIds.Contains(custom.Id);
                _allApps.Add(custom);
            }
        }

        // =====================================================================
        // UI CONSTRUCTION (#171717 / #202124 DARK GLASSMORPHIC WORKSPACE)
        // =====================================================================
        private void BuildUserInterface()
        {
            var rootGrid = new Grid { Background = BgCanvas };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) }); // Row 0: Workspace & Tab Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) }); // Row 1: Navigation, Omnibox & Tool Actions
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 2: Main Body
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) }); // Row 3: Telemetry & Quick-Create Status Bar

            // -----------------------------------------------------------------
            // ROW 0: WORKSPACE ISOLATION PILLS + MULTI-TAB STRIP + QUICK ACTIONS
            // -----------------------------------------------------------------
            var topBarBorder = new Border
            {
                Background = BgElevated,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 0, 10, 0)
            };
            var topBarGrid = new Grid();
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Workspaces
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Tabs
            topBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right Controls

            _workspaceSelectorPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_workspaceSelectorPanel, 0);
            topBarGrid.Children.Add(_workspaceSelectorPanel);

            var tabScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _tabStripPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            tabScroll.Content = _tabStripPanel;
            Grid.SetColumn(tabScroll, 1);
            topBarGrid.Children.Add(tabScroll);

            var rightTopControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _accountBadgeText = new TextBlock
            {
                Text = "Isolated Session",
                Foreground = AccentGreen,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            rightTopControls.Children.Add(_accountBadgeText);

            var hibernateBtn = CreateToolbarButton("zzz Hibernate Bg", "Free RAM from background tabs", AccentGold);
            hibernateBtn.Click += (s, e) => HibernateBackgroundTabs();
            rightTopControls.Children.Add(hibernateBtn);

            var lockBtn = CreateToolbarButton("Lock / PIN", "Lock or set SHA-256 PIN for current workspace", TextSecondary);
            lockBtn.Click += (s, e) => PromptOrLockWorkspace();
            rightTopControls.Children.Add(lockBtn);

            Grid.SetColumn(rightTopControls, 2);
            topBarGrid.Children.Add(rightTopControls);
            topBarBorder.Child = topBarGrid;
            Grid.SetRow(topBarBorder, 0);
            rootGrid.Children.Add(topBarBorder);

            // -----------------------------------------------------------------
            // ROW 1: NAVIGATION + SCOPED OMNIBOX + SPLIT VIEW / MINI-PLAYER / AI
            // -----------------------------------------------------------------
            var navBarBorder = new Border
            {
                Background = BgCanvas,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 10, 5)
            };
            var navGrid = new Grid();
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Back/Fwd/Reload/Home
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Scoped Omnibox
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Power tools

            var navLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var btnHome = CreateToolbarButton("Apps Grid", "Show 38-Service Launcher Dashboard (Ctrl+T)", AccentBlue);
            btnHome.Click += (s, e) => ShowLauncherDashboard();
            var btnBack = CreateToolbarButton("←", "Back (Ctrl+[)", TextPrimary);
            btnBack.Click += (s, e) => { if (_activeTab?.WebView?.CoreWebView2?.CanGoBack == true) _activeTab.WebView.CoreWebView2.GoBack(); };
            var btnFwd = CreateToolbarButton("→", "Forward (Ctrl+])", TextPrimary);
            btnFwd.Click += (s, e) => { if (_activeTab?.WebView?.CoreWebView2?.CanGoForward == true) _activeTab.WebView.CoreWebView2.GoForward(); };
            var btnReload = CreateToolbarButton("↻", "Reload (Ctrl+R)", TextPrimary);
            btnReload.Click += (s, e) => { _activeTab?.WebView?.CoreWebView2?.Reload(); };

            navLeft.Children.Add(btnHome);
            navLeft.Children.Add(btnBack);
            navLeft.Children.Add(btnFwd);
            navLeft.Children.Add(btnReload);
            Grid.SetColumn(navLeft, 0);
            navGrid.Children.Add(navLeft);

            // Omnibox with Scoped Search Engine Selector
            var omniBorder = new Border
            {
                Background = BgElevated,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Margin = new Thickness(10, 0, 10, 0),
                Padding = new Thickness(6, 2, 6, 2)
            };
            var omniGrid = new Grid();
            omniGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            omniGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            omniGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _searchEnginePillBtn = new Button
            {
                Content = $"Search: {_activeSearchEngine.Name} ▾",
                Background = BrushFromHex("#2C3038"),
                Foreground = BrushFromHex(_activeSearchEngine.AccentHex),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold
            };
            _searchEnginePillBtn.Click += OnSearchEngineSelectorClick;
            Grid.SetColumn(_searchEnginePillBtn, 0);
            omniGrid.Children.Add(_searchEnginePillBtn);

            _omniboxInput = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = TextPrimary,
                CaretBrush = AccentBlue,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Text = "Search Google, enter URL, or type prefix (yt / m / d / doc / ai / gh)..."
            };
            _omniboxInput.GotFocus += (s, e) =>
            {
                if (_omniboxInput.Text.StartsWith("Search Google,")) _omniboxInput.Text = "";
                _omniboxInput.SelectAll();
            };
            _omniboxInput.KeyDown += OnOmniboxKeyDown;
            Grid.SetColumn(_omniboxInput, 1);
            omniGrid.Children.Add(_omniboxInput);

            var paletteTriggerBtn = CreateToolbarButton("⌘K / Alt+Space", "Open Spotlight Command Palette", TextSecondary);
            paletteTriggerBtn.Click += (s, e) => ToggleCommandPalette();
            Grid.SetColumn(paletteTriggerBtn, 2);
            omniGrid.Children.Add(paletteTriggerBtn);

            omniBorder.Child = omniGrid;
            Grid.SetColumn(omniBorder, 1);
            navGrid.Children.Add(omniBorder);

            // Right Power Tools: AdBlock Toggle, Split View, Mini-Player, Popout, External Browser, AI Companion
            var navRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var adBlockBtn = CreateToolbarButton(_config.AdBlockEnabled ? "Shield: ON" : "Shield: OFF",
                "4-Layer YouTube Ad-Pruner & Network Tracker Shield", _config.AdBlockEnabled ? AccentGreen : TextSecondary);
            adBlockBtn.Click += (s, e) =>
            {
                _config.AdBlockEnabled = !_config.AdBlockEnabled;
                adBlockBtn.Content = _config.AdBlockEnabled ? "Shield: ON" : "Shield: OFF";
                adBlockBtn.Foreground = _config.AdBlockEnabled ? AccentGreen : TextSecondary;
                ShowToast(_config.AdBlockEnabled ? "4-Layer Ad & Tracker Shield enabled." : "Ad & Tracker Shield paused.");
            };
            navRight.Children.Add(adBlockBtn);

            var splitBtn = CreateToolbarButton("Split View (Alt+S)", "Toggle Dual-Pane Side-by-Side Workspace", AccentBlue);
            splitBtn.Click += (s, e) => ToggleSplitView();
            navRight.Children.Add(splitBtn);

            var miniPlayerBtn = CreateToolbarButton("PiP Mini (Alt+P)", "Pop current tab into Always-on-Top Mini-Player", AccentGold);
            miniPlayerBtn.Click += (s, e) => OpenMiniPlayerForActiveTab();
            navRight.Children.Add(miniPlayerBtn);

            var extBtn = CreateToolbarButton("↗ Browser", "Open current URL in system default browser", TextSecondary);
            extBtn.Click += (s, e) => OpenActiveUrlInSystemBrowser();
            navRight.Children.Add(extBtn);

            var aiBtn = CreateToolbarButton("✦ AI Drawer (Alt+A)", "Toggle Free Official AI Companion (Gemini, NotebookLM, Claude, ChatGPT)", AccentPurple);
            aiBtn.Click += (s, e) => ToggleAiDrawer();
            navRight.Children.Add(aiBtn);

            Grid.SetColumn(navRight, 2);
            navGrid.Children.Add(navRight);
            navBarBorder.Child = navGrid;
            Grid.SetRow(navBarBorder, 1);
            rootGrid.Children.Add(navBarBorder);

            // -----------------------------------------------------------------
            // ROW 2: MAIN BODY (LEFT DOCK + WEBVIEW/SPLIT HOST + RIGHT AI DRAWER)
            // -----------------------------------------------------------------
            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) }); // Left Dock
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Center Content
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right AI Drawer

            // Left Dock (Pinned Favorites + Custom App Adder)
            var sidebarBorder = new Border
            {
                Background = BgElevated,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(4, 8, 4, 8)
            };
            var sidebarScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            _sidebarFavoritesPanel = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };
            sidebarScroll.Content = _sidebarFavoritesPanel;
            sidebarBorder.Child = sidebarScroll;
            Grid.SetColumn(sidebarBorder, 0);
            bodyGrid.Children.Add(sidebarBorder);

            // Center Area: Split-Pane Container + Home Dashboard Overlay + Command Palette Overlay + Toast Banner
            var centerWrapper = new Grid();

            _splitPaneContainer = new Grid();
            _splitPaneContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _splitPaneContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _splitPaneContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });

            _contentHostGrid = new Grid { Background = BgCanvas };
            Grid.SetColumn(_contentHostGrid, 0);
            _splitPaneContainer.Children.Add(_contentHostGrid);

            _splitSplitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = BorderSubtle,
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(_splitSplitter, 1);
            _splitPaneContainer.Children.Add(_splitSplitter);

            _splitRightBorder = BuildSplitRightPane();
            Grid.SetColumn(_splitRightBorder, 2);
            _splitPaneContainer.Children.Add(_splitRightBorder);

            centerWrapper.Children.Add(_splitPaneContainer);

            // Home Launcher Dashboard (38 Google Apps + Deep SubItems + Quick-Create Bar)
            _homeDashboardOverlay = BuildHomeDashboardOverlay();
            centerWrapper.Children.Add(_homeDashboardOverlay);

            // Spotlight Command Palette Overlay (Alt+Space / Ctrl+K)
            _commandPaletteOverlay = BuildCommandPaletteOverlay();
            centerWrapper.Children.Add(_commandPaletteOverlay);

            // Workspace PIN Lock Overlay
            _pinLockOverlay = BuildPinLockOverlay();
            centerWrapper.Children.Add(_pinLockOverlay);

            // Notification / Status Toast Banner
            _toastBanner = new Border
            {
                Background = BrushFromHex("#282C34"),
                BorderBrush = AccentBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 14, 18, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };
            _toastText = new TextBlock { Foreground = TextPrimary, FontSize = 12.5, FontWeight = FontWeights.Medium };
            _toastBanner.Child = _toastText;
            centerWrapper.Children.Add(_toastBanner);

            Grid.SetColumn(centerWrapper, 1);
            bodyGrid.Children.Add(centerWrapper);

            // Right AI Companion Drawer (Gemini, NotebookLM, AI Studio, ChatGPT, Claude, Perplexity)
            _aiDrawerBorder = BuildAiCompanionDrawer();
            Grid.SetColumn(_aiDrawerBorder, 2);
            bodyGrid.Children.Add(_aiDrawerBorder);

            Grid.SetRow(bodyGrid, 2);
            rootGrid.Children.Add(bodyGrid);

            // -----------------------------------------------------------------
            // ROW 3: STATUS & QUICK-CREATE FOOTER BAR
            // -----------------------------------------------------------------
            var footerBorder = new Border
            {
                Background = BgElevated,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 0, 12, 0)
            };
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var quickCreateBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var qc in CatalogRegistry.GetQuickCreateActions())
            {
                var b = new Button
                {
                    Content = qc.Label,
                    Background = Brushes.Transparent,
                    Foreground = BrushFromHex(qc.Hex),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(7, 2, 7, 2),
                    Margin = new Thickness(0, 0, 4, 0),
                    FontSize = 11,
                    Cursor = Cursors.Hand
                };
                string targetUrl = qc.Url;
                string title = qc.Label;
                b.Click += async (s, e) => await OpenAppInTabAsync(title, targetUrl, qc.Badge, qc.Hex, switchToTab: true);
                quickCreateBar.Children.Add(b);
            }
            Grid.SetColumn(quickCreateBar, 0);
            footerGrid.Children.Add(quickCreateBar);

            _statusFooterText = new TextBlock
            {
                Text = "Ready | 100% Unlocked Local Container | Zero Telemetry | Trusted-Types CSP Safe",
                Foreground = TextSecondary,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(_statusFooterText, 1);
            footerGrid.Children.Add(_statusFooterText);

            _memoryFooterText = new TextBlock
            {
                Text = "Container RAM: -- MB | Tabs: 0",
                Foreground = AccentGreen,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_memoryFooterText, 2);
            footerGrid.Children.Add(_memoryFooterText);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 3);
            rootGrid.Children.Add(footerBorder);

            Content = rootGrid;
        }

        // =====================================================================
        // HOME LAUNCHER DASHBOARD (38 APPS + SUBITEMS + CATEGORIES + CUSTOM APP)
        // =====================================================================
        private Grid BuildHomeDashboardOverlay()
        {
            var overlay = new Grid { Background = BgCanvas, Visibility = Visibility.Visible };
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(32, 24, 32, 32)
            };
            var mainStack = new StackPanel { MaxWidth = 1240, HorizontalAlignment = HorizontalAlignment.Stretch };

            // Hero Header + Instant Filter
            var heroGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            titleStack.Children.Add(new TextBlock
            {
                Text = "G-Suite Container Hub",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimary
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "38 Google Services with Deep Sub-Links • Multi-Account Isolation • 4-Layer Ad-Pruner • Split View & Free AI Drawer",
                FontSize = 12.5,
                Foreground = TextSecondary,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(titleStack, 0);
            heroGrid.Children.Add(titleStack);

            var rightHeroActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _homeFilterBox = new TextBox
            {
                Width = 260,
                Height = 34,
                Background = BgElevated,
                Foreground = TextPrimary,
                CaretBrush = AccentBlue,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 12.5,
                Text = ""
            };
            _homeFilterBox.TextChanged += (s, e) => RenderDashboardCards();
            rightHeroActions.Children.Add(_homeFilterBox);

            var addCustomBtn = CreateToolbarButton("+ Add Custom Web App", "Add any internal or custom web app (Unlimited, Zero Paywall)", AccentGreen);
            addCustomBtn.Height = 34;
            addCustomBtn.Margin = new Thickness(10, 0, 0, 0);
            addCustomBtn.Click += (s, e) => ShowAddCustomAppDialog();
            rightHeroActions.Children.Add(addCustomBtn);

            Grid.SetColumn(rightHeroActions, 1);
            heroGrid.Children.Add(rightHeroActions);
            mainStack.Children.Add(heroGrid);

            // Category Pills Bar
            _categoryFilterPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
            mainStack.Children.Add(_categoryFilterPanel);

            // App Cards Grid
            _homeAppsWrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            mainStack.Children.Add(_homeAppsWrapPanel);

            scroll.Content = mainStack;
            overlay.Children.Add(scroll);
            return overlay;
        }

        private void RenderCategoryPills()
        {
            _categoryFilterPanel.Children.Clear();
            var categories = new List<string> { "All Services", "Favorites" };
            categories.AddRange(_allApps.Select(a => a.Category).Distinct());

            foreach (var cat in categories)
            {
                bool active = string.Equals(cat, _selectedCategory, StringComparison.OrdinalIgnoreCase);
                var btn = new Button
                {
                    Content = cat,
                    Background = active ? BrushFromHex("#2B3548") : BgElevated,
                    Foreground = active ? AccentBlue : TextSecondary,
                    BorderBrush = active ? AccentBlue : BorderSubtle,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 5, 12, 5),
                    Margin = new Thickness(0, 0, 8, 0),
                    FontSize = 12,
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                    Cursor = Cursors.Hand
                };
                string c = cat;
                btn.Click += (s, e) =>
                {
                    _selectedCategory = c;
                    RenderCategoryPills();
                    RenderDashboardCards();
                };
                _categoryFilterPanel.Children.Add(btn);
            }
        }

        private void RenderDashboardCards()
        {
            _homeAppsWrapPanel.Children.Clear();
            string query = (_homeFilterBox?.Text ?? "").Trim();

            IEnumerable<GoogleAppDef> filtered = _allApps;
            if (_selectedCategory == "Favorites")
                filtered = filtered.Where(a => a.IsFavorite);
            else if (_selectedCategory != "All Services")
                filtered = filtered.Where(a => string.Equals(a.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(a =>
                    FuzzyMatch(a.Name, query) ||
                    FuzzyMatch(a.Category, query) ||
                    a.SubItems.Any(sub => FuzzyMatch(sub.Name, query)));
            }

            foreach (var app in filtered)
            {
                _homeAppsWrapPanel.Children.Add(CreateAppCard(app));
            }
        }

        private Border CreateAppCard(GoogleAppDef app)
        {
            var card = new Border
            {
                Width = 225,
                Height = 112,
                Background = BgCard,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 14, 14),
                Padding = new Thickness(14, 12, 14, 12),
                Cursor = Cursors.Hand
            };

            card.MouseEnter += (s, e) => { card.Background = BgHover; card.BorderBrush = BrushFromHex(app.AccentHex); };
            card.MouseLeave += (s, e) => { card.Background = BgCard; card.BorderBrush = BorderSubtle; };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top row: Service Badge + Star Button + Deep SubItems Menu Button
            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBadge = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(8),
                Background = BrushFromHex("#1A1C20"),
                BorderBrush = BrushFromHex(app.AccentHex),
                BorderThickness = new Thickness(1.5)
            };
            iconBadge.Child = new TextBlock
            {
                Text = app.Badge,
                Foreground = BrushFromHex(app.AccentHex),
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBadge, 0);
            topRow.Children.Add(iconBadge);

            var actionsStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var starBtn = new Button
            {
                Content = app.IsFavorite ? "★" : "☆",
                Foreground = app.IsFavorite ? AccentGold : TextSecondary,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Cursor = Cursors.Hand,
                ToolTip = "Pin / Unpin from Left Dock"
            };
            starBtn.Click += (s, e) =>
            {
                e.Handled = true;
                ToggleFavorite(app);
            };
            actionsStack.Children.Add(starBtn);

            if (app.SubItems.Count > 0)
            {
                var subBtn = new Button
                {
                    Content = $"▾ {app.SubItems.Count}",
                    Foreground = BrushFromHex(app.AccentHex),
                    Background = BrushFromHex("#1B1D22"),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(4, 0, 0, 0),
                    FontSize = 11,
                    Cursor = Cursors.Hand,
                    ToolTip = "Open Deep Links & Shortcuts"
                };
                subBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    OpenSubItemsMenu(app, subBtn);
                };
                actionsStack.Children.Add(subBtn);
            }

            Grid.SetColumn(actionsStack, 2);
            topRow.Children.Add(actionsStack);
            Grid.SetRow(topRow, 0);
            grid.Children.Add(topRow);

            // Middle: App Name
            var nameBlock = new TextBlock
            {
                Text = app.Name,
                Foreground = TextPrimary,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(nameBlock, 1);
            grid.Children.Add(nameBlock);

            // Bottom: Category + Split-View quick button
            var bottomRow = new Grid();
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            bottomRow.Children.Add(new TextBlock
            {
                Text = app.Category,
                Foreground = TextSecondary,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });

            var splitOpenBtn = new Button
            {
                Content = "◫ Split",
                Foreground = TextSecondary,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 10.5,
                Cursor = Cursors.Hand,
                ToolTip = "Open in Right Split-View Pane"
            };
            splitOpenBtn.Click += async (s, e) =>
            {
                e.Handled = true;
                await OpenUrlInSplitPaneAsync(app.Url);
            };
            Grid.SetColumn(splitOpenBtn, 1);
            bottomRow.Children.Add(splitOpenBtn);

            Grid.SetRow(bottomRow, 2);
            grid.Children.Add(bottomRow);

            card.Child = grid;
            card.MouseLeftButtonUp += async (s, e) =>
            {
                await OpenAppInTabAsync(app.Name, app.Url, app.Badge, app.AccentHex, switchToTab: true);
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
                Background = BgElevated,
                Foreground = TextPrimary,
                BorderBrush = BorderSubtle
            };

            var openTabItem = new MenuItem { Header = $"Open {app.Name} in New Tab", Foreground = AccentBlue };
            openTabItem.Click += async (s, e) => await OpenAppInTabAsync(app.Name, app.Url, app.Badge, app.AccentHex, switchToTab: true);
            menu.Items.Add(openTabItem);

            var openSplitItem = new MenuItem { Header = $"Open {app.Name} in Split View (Right Pane)", Foreground = AccentGreen };
            openSplitItem.Click += async (s, e) => await OpenUrlInSplitPaneAsync(app.Url);
            menu.Items.Add(openSplitItem);

            var openPopoutItem = new MenuItem { Header = $"Open {app.Name} in Dedicated Popout Window", Foreground = TextPrimary };
            openPopoutItem.Click += (s, e) => OpenDedicatedPopoutWindow(app.Name, app.Url);
            menu.Items.Add(openPopoutItem);

            if (app.SubItems.Count > 0)
            {
                menu.Items.Add(new Separator());
                foreach (var sub in app.SubItems)
                {
                    var subItem = new MenuItem { Header = $"↳ {sub.Name}", Foreground = TextPrimary };
                    string u = sub.Url;
                    string n = $"{app.Name}: {sub.Name}";
                    subItem.Click += async (s, e) => await OpenAppInTabAsync(n, u, app.Badge, app.AccentHex, switchToTab: true);
                    menu.Items.Add(subItem);
                }
            }

            menu.IsOpen = true;
        }

        // =====================================================================
        // WORKSPACE ISOLATION, SIDEBAR FAVORITES & TAB MANAGEMENT
        // =====================================================================
        private void RenderWorkspacePills()
        {
            _workspaceSelectorPanel.Children.Clear();
            foreach (var ws in _config.Workspaces)
            {
                bool isCurrent = ws.Id == _config.ActiveWorkspaceId;
                var btn = new Button
                {
                    Content = isCurrent ? $"● {ws.Name}" : ws.Name,
                    Background = isCurrent ? BrushFromHex("#283142") : Brushes.Transparent,
                    Foreground = isCurrent ? BrushFromHex(ws.AccentHex) : TextSecondary,
                    BorderBrush = isCurrent ? BrushFromHex(ws.AccentHex) : BorderSubtle,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(9, 3, 9, 3),
                    Margin = new Thickness(0, 0, 5, 0),
                    FontSize = 11.5,
                    FontWeight = isCurrent ? FontWeights.SemiBold : FontWeights.Normal,
                    Cursor = Cursors.Hand,
                    ToolTip = $"Isolated WebView2 Profile: Profiles\\{ws.Id}"
                };
                string targetId = ws.Id;
                btn.Click += async (s, e) => await SwitchWorkspaceAsync(targetId);
                _workspaceSelectorPanel.Children.Add(btn);
            }

            var addWsBtn = CreateToolbarButton("+ Profile", "Create a new isolated Google account workspace", TextSecondary);
            addWsBtn.Click += (s, e) => CreateNewWorkspaceDialog();
            _workspaceSelectorPanel.Children.Add(addWsBtn);
        }

        private void RenderSidebarFavorites()
        {
            _sidebarFavoritesPanel.Children.Clear();
            foreach (var app in _allApps.Where(a => a.IsFavorite))
            {
                var btn = new Button
                {
                    Width = 46,
                    Height = 46,
                    Margin = new Thickness(0, 0, 0, 8),
                    Background = BgCard,
                    Foreground = BrushFromHex(app.AccentHex),
                    BorderBrush = BorderSubtle,
                    BorderThickness = new Thickness(1),
                    Content = app.Badge,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Cursor = Cursors.Hand,
                    ToolTip = $"{app.Name} (Right-click for Deep Sub-Links)"
                };
                var capturedApp = app;
                btn.Click += async (s, e) => await OpenAppInTabAsync(capturedApp.Name, capturedApp.Url, capturedApp.Badge, capturedApp.AccentHex, switchToTab: true);
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

            var opts = new CoreWebView2EnvironmentOptions(
                "--disable-features=msSmartScreenProtection --enable-smooth-scrolling");
            var env = await CoreWebView2Environment.CreateAsync(null, profileDir, opts);
            _envCache[workspaceId] = env;
            return env;
        }

        public async Task OpenAppInTabAsync(string title, string url, string badge, string accentHex, bool switchToTab)
        {
            // Check if an existing tab in this workspace already points to the same service host/path
            var tab = new BrowserTabEntry
            {
                Title = title,
                Url = url,
                Badge = badge,
                AccentHex = accentHex
            };

            var webView = new WebView2
            {
                DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 23, 23, 23),
                Visibility = Visibility.Collapsed
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
            UpdateMemoryStatus();
        }

        private async Task InitializeTabWebViewAsync(BrowserTabEntry tab, string targetUrl)
        {
            if (tab.WebView == null) return;
            var ws = GetActiveWorkspace();
            var env = await GetOrCreateWorkspaceEnvironmentAsync(ws.Id);
            await tab.WebView.EnsureCoreWebView2Async(env);

            var core = tab.WebView.CoreWebView2;
            core.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreDevToolsEnabled = true;

            // Inject DocumentStart Trusted-Types-Safe Keyboard, Account & YouTube 4-Layer Ad-Pruner
            await core.AddScriptToExecuteOnDocumentCreatedAsync(InjectedScripts.DocumentStartInitScript);

            // Register Network-Layer Ad & Tracker Filter
            foreach (var pattern in InjectedScripts.BlockedUrlPatterns)
            {
                core.AddWebResourceRequestedFilter(pattern, CoreWebView2WebResourceContext.All);
            }
            core.WebResourceRequested += (s, e) =>
            {
                if (_config.AdBlockEnabled)
                {
                    e.Response = core.Environment.CreateWebResourceResponse(
                        Stream.Null, 204, "No Content", "Content-Type: text/plain");
                }
            };

            // Handle NewWindowRequested (route Google links into container tabs; external links on Shift)
            core.NewWindowRequested += async (s, e) =>
            {
                e.Handled = true;
                if (e.Uri.Contains("#gab-ext"))
                {
                    Process.Start(new ProcessStartInfo(e.Uri.Replace("#gab-ext", "")) { UseShellExecute = true });
                    return;
                }
                await OpenAppInTabAsync("Tab", e.Uri, tab.Badge, tab.AccentHex, switchToTab: true);
            };

            core.DocumentTitleChanged += (s, e) =>
            {
                tab.Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? tab.Title : core.DocumentTitle;
                if (tab.TabTitleText != null)
                {
                    tab.TabTitleText.Text = Truncate(tab.Title, 22);
                }
            };

            core.SourceChanged += (s, e) =>
            {
                if (core.Source != "about:blank")
                {
                    tab.Url = core.Source;
                    if (_activeTab == tab) _omniboxInput.Text = tab.Url;
                }
            };

            core.WebMessageReceived += (s, e) =>
            {
                string msg = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(msg)) return;
                if (msg == "cmd:palette") ToggleCommandPalette();
                else if (msg.StartsWith("account:"))
                {
                    string email = msg.Substring("account:".Length);
                    ws.DetectedAccount = email;
                    _accountBadgeText.Text = $"{ws.Name}: {email}";
                }
                else if (msg.StartsWith("notify:"))
                {
                    ShowToast(msg.Substring("notify:".Length));
                }
            };

            core.Navigate(targetUrl);
        }

        private void RenderTabStrip()
        {
            _tabStripPanel.Children.Clear();

            // Apps Launcher Home Chip
            var homeChip = new Button
            {
                Content = "⊞ Hub",
                Background = _homeDashboardOverlay.Visibility == Visibility.Visible ? BrushFromHex("#283142") : BgCard,
                Foreground = AccentBlue,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            homeChip.Click += (s, e) => ShowLauncherDashboard();
            _tabStripPanel.Children.Add(homeChip);

            foreach (var tab in _tabs)
            {
                bool isActive = (_activeTab == tab && _homeDashboardOverlay.Visibility != Visibility.Visible);
                var chipBorder = new Border
                {
                    Background = isActive ? BrushFromHex("#292D36") : BgCard,
                    BorderBrush = isActive ? BrushFromHex(tab.AccentHex) : BorderSubtle,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 3, 6, 3),
                    Margin = new Thickness(0, 0, 5, 0),
                    Cursor = Cursors.Hand
                };
                var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                stack.Children.Add(new TextBlock
                {
                    Text = tab.Badge,
                    Foreground = BrushFromHex(tab.AccentHex),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var titleBlock = new TextBlock
                {
                    Text = Truncate(tab.Title, 22),
                    Foreground = isActive ? TextPrimary : TextSecondary,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                tab.TabTitleText = titleBlock;
                stack.Children.Add(titleBlock);

                var stateBadge = new TextBlock
                {
                    Text = tab.IsHibernated ? " [zzz]" : "",
                    Foreground = AccentGold,
                    FontSize = 10.5,
                    VerticalAlignment = VerticalAlignment.Center
                };
                tab.TabStateBadge = stateBadge;
                stack.Children.Add(stateBadge);

                var closeBtn = new Button
                {
                    Content = "×",
                    Background = Brushes.Transparent,
                    Foreground = TextSecondary,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(6, 0, 0, 0),
                    Padding = new Thickness(3, 0, 3, 0),
                    FontSize = 13,
                    Cursor = Cursors.Hand
                };
                var capturedTab = tab;
                closeBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    CloseTab(capturedTab);
                };
                stack.Children.Add(closeBtn);

                chipBorder.Child = stack;
                chipBorder.MouseLeftButtonUp += async (s, e) => await WakeAndActivateTabAsync(capturedTab);
                tab.TabChipBorder = chipBorder;
                _tabStripPanel.Children.Add(chipBorder);
            }

            var newTabBtn = CreateToolbarButton("+", "Open Launcher Dashboard (Ctrl+T)", AccentBlue);
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
            foreach (var t in _tabs)
            {
                if (t.WebView != null)
                    t.WebView.Visibility = (t == tab) ? Visibility.Visible : Visibility.Collapsed;
            }
            _omniboxInput.Text = tab.Url;
            RenderTabStrip();
        }

        private void ShowLauncherDashboard()
        {
            _homeDashboardOverlay.Visibility = Visibility.Visible;
            RenderTabStrip();
            RenderCategoryPills();
            RenderDashboardCards();
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
            UpdateMemoryStatus();
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
                ? $"Hibernated {count} background tab(s) to reclaim WebView2 memory."
                : "No active background tabs to hibernate.");
            UpdateMemoryStatus();
        }

        // =====================================================================
        // DUAL-PANE SPLIT VIEW, FLOATING MINI-PLAYER & AI COMPANION DRAWER
        // =====================================================================
        private Border BuildSplitRightPane()
        {
            var border = new Border
            {
                Background = BgCanvas,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new Grid { Background = BgElevated };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            header.Children.Add(new TextBlock
            {
                Text = "◫ Split Pane",
                Foreground = AccentGreen,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 8, 0)
            });

            _splitUrlBox = new TextBox
            {
                Background = BgCard,
                Foreground = TextPrimary,
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

            var closeSplitBtn = CreateToolbarButton("× Close Split", "Close right split pane", TextSecondary);
            closeSplitBtn.Click += (s, e) => ToggleSplitView();
            Grid.SetColumn(closeSplitBtn, 2);
            header.Children.Add(closeSplitBtn);

            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            _splitWebView = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 23, 23, 23) };
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
                _splitWebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
                _splitWebView.CoreWebView2.Navigate(NormalizeUrlOrSearch(url));
            }
        }

        private Border BuildAiCompanionDrawer()
        {
            var drawer = new Border
            {
                Width = 420,
                Background = BgElevated,
                BorderBrush = BorderSubtle,
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
                var btn = new Button
                {
                    Content = p.Name,
                    Background = BgCard,
                    Foreground = BrushFromHex(p.Hex),
                    BorderBrush = BorderSubtle,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(0, 0, 4, 0),
                    FontSize = 11,
                    Cursor = Cursors.Hand
                };
                string targetUrl = p.Url;
                btn.Click += async (s, e) => await NavigateAiDrawerAsync(targetUrl);
                _aiProviderTabsPanel.Children.Add(btn);
            }
            Grid.SetRow(_aiProviderTabsPanel, 0);
            grid.Children.Add(_aiProviderTabsPanel);

            _aiWebViewHost = new Grid { Background = BgCanvas };
            _aiWebView = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 23, 23, 23) };
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
            _aiWebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
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
                Title = $"📌 Mini-Player | {title}",
                Width = 480,
                Height = 310,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = SystemParameters.WorkArea.Width - 500,
                Top = SystemParameters.WorkArea.Height - 340,
                Background = BgCanvas,
                Foreground = TextPrimary,
                FontFamily = UiFont
            };
            var wv = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 23, 23, 23) };
            miniWin.Content = wv;
            miniWin.Show();

            var env = await GetOrCreateWorkspaceEnvironmentAsync(GetActiveWorkspace().Id);
            await wv.EnsureCoreWebView2Async(env);
            await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(InjectedScripts.DocumentStartInitScript);
            wv.CoreWebView2.Navigate(url);
        }

        private async void OpenDedicatedPopoutWindow(string title, string url)
        {
            var popWin = new Window
            {
                Title = $"{title} | G-Suite Container",
                Width = 1180,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = BgCanvas,
                Foreground = TextPrimary
            };
            var wv = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 23, 23, 23) };
            popWin.Content = wv;
            popWin.Show();

            var env = await GetOrCreateWorkspaceEnvironmentAsync(GetActiveWorkspace().Id);
            await wv.EnsureCoreWebView2Async(env);
            await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(InjectedScripts.DocumentStartInitScript);
            wv.CoreWebView2.Navigate(url);
        }

        // =====================================================================
        // SPOTLIGHT COMMAND PALETTE (Alt+Space / Ctrl+K)
        // =====================================================================
        private Grid BuildCommandPaletteOverlay()
        {
            var overlay = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(185, 12, 13, 16)),
                Visibility = Visibility.Collapsed
            };
            overlay.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource == overlay) ToggleCommandPalette();
            };

            var modal = new Border
            {
                Width = 640,
                MaxHeight = 500,
                Background = BgElevated,
                BorderBrush = AccentBlue,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 70, 0, 0),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.65 }
            };

            var stack = new StackPanel();
            _paletteSearchBox = new TextBox
            {
                Height = 38,
                Background = BgCard,
                Foreground = TextPrimary,
                CaretBrush = AccentBlue,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 7, 12, 7),
                FontSize = 14
            };
            _paletteSearchBox.TextChanged += (s, e) => RenderPaletteResults();
            _paletteSearchBox.KeyDown += OnPaletteSearchKeyDown;
            stack.Children.Add(_paletteSearchBox);

            var scroll = new ScrollViewer
            {
                MaxHeight = 400,
                Margin = new Thickness(0, 10, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _paletteResultsPanel = new StackPanel();
            scroll.Content = _paletteResultsPanel;
            stack.Children.Add(scroll);

            modal.Child = stack;
            overlay.Children.Add(modal);
            return overlay;
        }

        private void ToggleCommandPalette()
        {
            if (_commandPaletteOverlay.Visibility == Visibility.Visible)
            {
                _commandPaletteOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
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
                .Where(a => string.IsNullOrEmpty(q) || FuzzyMatch(a.Name, q) || FuzzyMatch(a.Category, q))
                .Take(12)
                .ToList();

            foreach (var app in matches)
            {
                var row = new Button
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Background = BgCard,
                    Foreground = TextPrimary,
                    BorderBrush = BorderSubtle,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand,
                    Content = $"[{app.Badge}]  {app.Name}   |   {app.Category} ({app.Url})"
                };
                var captured = app;
                row.Click += async (s, e) =>
                {
                    _commandPaletteOverlay.Visibility = Visibility.Collapsed;
                    await OpenAppInTabAsync(captured.Name, captured.Url, captured.Badge, captured.AccentHex, switchToTab: true);
                };
                _paletteResultsPanel.Children.Add(row);
            }

            if (!string.IsNullOrEmpty(q))
            {
                foreach (var se in _searchEngines.Take(4))
                {
                    var searchRow = new Button
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Background = BrushFromHex("#1E2430"),
                        Foreground = BrushFromHex(se.AccentHex),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(12, 7, 12, 7),
                        Margin = new Thickness(0, 0, 0, 4),
                        Cursor = Cursors.Hand,
                        Content = $"🔎 Search {se.Name} for \"{q}\""
                    };
                    string url = string.Format(se.UrlTemplate, Uri.EscapeDataString(q));
                    string name = se.Name;
                    string hex = se.AccentHex;
                    searchRow.Click += async (s, e) =>
                    {
                        _commandPaletteOverlay.Visibility = Visibility.Collapsed;
                        await OpenAppInTabAsync($"{name}: {q}", url, "🔎", hex, switchToTab: true);
                    };
                    _paletteResultsPanel.Children.Add(searchRow);
                }
            }
        }

        private async void OnPaletteSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _commandPaletteOverlay.Visibility = Visibility.Collapsed;
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
                        await OpenAppInTabAsync(topApp.Name, topApp.Url, topApp.Badge, topApp.AccentHex, switchToTab: true);
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
                Background = BgCanvas,
                Visibility = Visibility.Collapsed
            };
            var card = new Border
            {
                Width = 380,
                Background = BgElevated,
                BorderBrush = AccentGold,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var stack = new StackPanel();
            _pinPromptLabel = new TextBlock
            {
                Text = "Workspace Locked | Enter PIN",
                Foreground = TextPrimary,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(_pinPromptLabel);

            _pinInputBox = new PasswordBox
            {
                Height = 36,
                Background = BgCard,
                Foreground = TextPrimary,
                BorderBrush = BorderSubtle,
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 14
            };
            _pinInputBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) VerifyOrSetPin(); };
            stack.Children.Add(_pinInputBox);

            var unlockBtn = CreateToolbarButton("Unlock / Confirm PIN", "Verify SHA-256 PIN", AccentGold);
            unlockBtn.Height = 36;
            unlockBtn.Margin = new Thickness(0, 12, 0, 0);
            unlockBtn.Click += (s, e) => VerifyOrSetPin();
            stack.Children.Add(unlockBtn);

            card.Child = stack;
            overlay.Children.Add(card);
            return overlay;
        }

        private void PromptOrLockWorkspace()
        {
            var ws = GetActiveWorkspace();
            _pinInputBox.Password = "";
            if (string.IsNullOrEmpty(ws.PinSha256))
            {
                _pinPromptLabel.Text = $"Set New PIN for '{ws.Name}' Workspace:";
            }
            else
            {
                _pinPromptLabel.Text = $"🔒 '{ws.Name}' Locked | Enter PIN:";
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
                ShowToast($"PIN protection enabled for workspace '{ws.Name}'.");
            }
            else if (ws.PinSha256 == hash)
            {
                _workspaceLocked = false;
                _pinLockOverlay.Visibility = Visibility.Collapsed;
                ShowToast($"Workspace '{ws.Name}' unlocked.");
            }
            else
            {
                _pinPromptLabel.Text = "❌ Incorrect PIN | Try Again:";
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
            _accountBadgeText.Text = string.IsNullOrEmpty(ws.DetectedAccount)
                ? $"Isolated: {ws.Name}"
                : $"{ws.Name}: {ws.DetectedAccount}";

            RenderWorkspacePills();
            ShowLauncherDashboard();

            if (!string.IsNullOrEmpty(ws.PinSha256))
            {
                PromptOrLockWorkspace();
            }

            foreach (var savedUrl in ws.OpenTabUrls.Take(5))
            {
                await OpenAppInTabAsync("Restored Tab", savedUrl, "G", ws.AccentHex, switchToTab: false);
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
                Background = BgElevated,
                Foreground = TextPrimary,
                BorderBrush = BorderSubtle
            };
            foreach (var se in _searchEngines)
            {
                var item = new MenuItem
                {
                    Header = $"{se.Name}   (prefix: '{se.Prefix} ')",
                    Foreground = BrushFromHex(se.AccentHex)
                };
                var captured = se;
                item.Click += (s, args) =>
                {
                    _activeSearchEngine = captured;
                    _config.DefaultSearchEngineId = captured.Id;
                    _searchEnginePillBtn.Content = $"Search: {captured.Name} ▾";
                    _searchEnginePillBtn.Foreground = BrushFromHex(captured.AccentHex);
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
            // Check prefix shortcuts: e.g., "yt rust tutorial", "m invoice", "d budget"
            var parts = rawInput.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var prefixEngine = _searchEngines.FirstOrDefault(s =>
                    string.Equals(s.Prefix, parts[0], StringComparison.OrdinalIgnoreCase));
                if (prefixEngine != null)
                {
                    string qUrl = string.Format(prefixEngine.UrlTemplate, Uri.EscapeDataString(parts[1]));
                    await NavigateOrOpenTabAsync($"{prefixEngine.Name}: {parts[1]}", qUrl, prefixEngine.AccentHex);
                    return;
                }
            }

            string targetUrl = NormalizeUrlOrSearch(rawInput);
            await NavigateOrOpenTabAsync("Search", targetUrl, _activeSearchEngine.AccentHex);
        }

        private async Task NavigateOrOpenTabAsync(string title, string url, string hex)
        {
            if (_activeTab != null && _homeDashboardOverlay.Visibility != Visibility.Visible && _activeTab.WebView?.CoreWebView2 != null)
            {
                _activeTab.WebView.CoreWebView2.Navigate(url);
            }
            else
            {
                await OpenAppInTabAsync(title, url, "G", hex, switchToTab: true);
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
                Title = "Add Custom Web App (Unlimited)",
                Width = 420,
                Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = BgElevated,
                Foreground = TextPrimary,
                FontFamily = UiFont
            };
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "App Name:", Foreground = TextSecondary, Margin = new Thickness(0, 0, 0, 4) });
            var nameBox = new TextBox { Height = 30, Background = BgCard, Foreground = TextPrimary, BorderBrush = BorderSubtle, Padding = new Thickness(6, 4, 6, 4) };
            stack.Children.Add(nameBox);

            stack.Children.Add(new TextBlock { Text = "HTTPS URL:", Foreground = TextSecondary, Margin = new Thickness(0, 12, 0, 4) });
            var urlBox = new TextBox { Height = 30, Background = BgCard, Foreground = TextPrimary, BorderBrush = BorderSubtle, Padding = new Thickness(6, 4, 6, 4), Text = "https://" };
            stack.Children.Add(urlBox);

            var saveBtn = CreateToolbarButton("Save Custom App", "Add to Launcher Catalog", AccentGreen);
            saveBtn.Height = 34;
            saveBtn.Margin = new Thickness(0, 18, 0, 0);
            saveBtn.Click += (s, e) =>
            {
                string n = nameBox.Text.Trim();
                string u = urlBox.Text.Trim();
                if (!string.IsNullOrEmpty(n) && !string.IsNullOrEmpty(u))
                {
                    var custom = new GoogleAppDef
                    {
                        Id = "custom_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                        Name = n,
                        Url = u.StartsWith("http") ? u : "https://" + u,
                        Badge = n.Substring(0, Math.Min(3, n.Length)).ToUpperInvariant(),
                        AccentHex = "#81C995",
                        Category = "Custom Apps",
                        IsCustom = true,
                        IsFavorite = true
                    };
                    _config.CustomApps.Add(custom);
                    _config.FavoriteAppIds.Add(custom.Id);
                    SaveConfig();
                    RebuildAppCatalog();
                    RenderSidebarFavorites();
                    RenderCategoryPills();
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
                Title = "Create Isolated Workspace Profile",
                Width = 380,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = BgElevated,
                Foreground = TextPrimary,
                FontFamily = UiFont
            };
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Workspace Name (e.g., Client, Consulting, Sandbox):", Foreground = TextSecondary, Margin = new Thickness(0, 0, 0, 6) });
            var nameBox = new TextBox { Height = 32, Background = BgCard, Foreground = TextPrimary, BorderBrush = BorderSubtle, Padding = new Thickness(8, 5, 8, 5) };
            stack.Children.Add(nameBox);

            var createBtn = CreateToolbarButton("Create & Switch Profile", "Creates isolated WebView2 storage folder", AccentBlue);
            createBtn.Height = 34;
            createBtn.Margin = new Thickness(0, 16, 0, 0);
            createBtn.Click += async (s, e) =>
            {
                string n = nameBox.Text.Trim();
                if (!string.IsNullOrEmpty(n))
                {
                    string id = new string(n.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                    if (string.IsNullOrEmpty(id)) id = "ws_" + Guid.NewGuid().ToString("N").Substring(0, 5);
                    if (_config.Workspaces.All(w => w.Id != id))
                    {
                        _config.Workspaces.Add(new WorkspaceProfile { Id = id, Name = n, AccentHex = "#C58AF9" });
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

        private void OpenActiveUrlInSystemBrowser()
        {
            string url = _activeTab?.Url ?? "https://www.google.com";
            if (!string.IsNullOrWhiteSpace(url) && url != "about:blank")
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        // =====================================================================
        // LIFECYCLE, HOTKEYS, DWM DARK BACKDROP & TELEMETRY
        // =====================================================================
        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyWindows11DarkBackdrop();
            RegisterGlobalHotkeys();

            RenderWorkspacePills();
            RenderSidebarFavorites();
            RenderCategoryPills();
            RenderDashboardCards();
            RenderTabStrip();

            if (_config.AiDrawerOpen)
            {
                await NavigateAiDrawerAsync(_config.ActiveAiProviderUrl);
            }

            _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _telemetryTimer.Tick += (s, args) => UpdateMemoryStatus();
            _telemetryTimer.Start();
            UpdateMemoryStatus();
        }

        private void ApplyWindows11DarkBackdrop()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int dark = 1;
                DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
                int mica = 2;
                DwmSetWindowAttribute(hwnd, 38, ref mica, sizeof(int)); // DWMWA_SYSTEMBACKDROP_TYPE (Mica)
            }
            catch { }
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
                    OpenDedicatedPopoutWindow(_activeTab?.Title ?? "Google", _activeTab?.Url ?? "https://www.google.com");
                }
            };
        }

        private void UpdateMemoryStatus()
        {
            try
            {
                using var proc = Process.GetCurrentProcess();
                double mb = proc.WorkingSet64 / (1024.0 * 1024.0);
                int sleeping = _tabs.Count(t => t.IsHibernated);
                _memoryFooterText.Text = $"Host RAM: {mb:F1} MB | Tabs: {_tabs.Count} ({sleeping} sleeping)";
            }
            catch { }
        }

        private void ShowToast(string message)
        {
            _toastText.Text = message;
            _toastBanner.Visibility = Visibility.Visible;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
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
        private static Button CreateToolbarButton(string text, string tooltip, SolidColorBrush fg)
        {
            return new Button
            {
                Content = text,
                ToolTip = tooltip,
                Background = BgCard,
                Foreground = fg,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 11.5,
                Cursor = Cursors.Hand
            };
        }

        private static SolidColorBrush BrushFromHex(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
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

        private static string Truncate(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxChars ? value : value.Substring(0, maxChars - 1) + "…";
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
                File.WriteAllText(logFile, $"[{DateTime.Now:O}] Initializing GSuiteContainer...\r\n");
                var app = new Application();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    File.AppendAllText(logFile, $"[DISPATCHER_ERR] {e.Exception}\r\n");
                    e.Handled = true;
                };
                var win = new MainWindow();
                win.Loaded += (s, e) => File.AppendAllText(logFile, $"[{DateTime.Now:O}] MainWindow Loaded OK! Title={win.Title}\r\n");
                app.Run(win);
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"[FATAL] {ex}\r\n");
            }
        }
    }
}

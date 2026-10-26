# Graph Report - NexaPlay  (2026-10-26)

## Corpus Check
- 157 files · ~185,664 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2201 nodes · 4374 edges · 142 communities (122 shown, 20 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 310 edges (avg confidence: 0.76)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `83771bf6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- SqliteCatalogRepository
- LicenseInfo
- HomeViewModel
- SteamPlatformService
- Window
- Task
- MetadataService
- Audit Phase 2 — Cache, Games, Refresh
- SteamStoreService
- NexaPlayOverrideService
- BypassGamesViewModel
- DynamicFeedRefreshService
- GameDetailPage
- Application
- OnlineFixService
- .ApplyHwndIcon
- Page
- NexaPlay.Core.Enums
- AddGameService
- BypassTutorialVideoService
- BypassGameDetailViewModel
- MainWindow
- GameDetailViewModel
- LibraryViewModel
- BypassGameDetailPage
- GamesViewModel
- AccountReportLimitStore
- AppUpdateService
- What You Must Do When Invoked
- GamesPage
- NexaPlay.Core.Models
- CoverImageCacheService
- .LoadAsync
- NexaPlay.Core.Helpers
- LibraryPage
- AddGamePayloadBuilder
- .OnPageUnloaded
- Page
- SettingsPage
- HomePage
- Page
- Page
- INavigationService
- BypassProgressState
- Local SQLite Catalog
- IAppLogService
- BypassGamesPage
- ScreenshotEntry
- Audit Alur Data NexaPlay
- NexaPlay.csproj
- Page
- IAddGameService
- .NavigateTo
- Page
- .OnActivateClicked
- .ApplyFiltersAndPaginationAsync
- .OnNavigatedTo
- FakeSteam
- Atomic Last-Known-Good Publication
- .TutorialThumbnail_Tapped
- CatalogBootstrapService
- .TryCreate
- ISteamService
- AddGameChecks
- .PopularGamesGrid_SizeChanged
- WindowsDefenderService
- Grid
- LicenseStore
- SettingsViewModel
- Graphify Workflow
- .SelectManualFolderAsync
- CatalogManifestRules
- .Log
- .ShowAsync
- IWindowsDefenderService
- graphify reference: extra exports and benchmark
- .FindSiblingSkeletonOverlay
- .LoadGamesAsync
- NexaPlay Gold Lightning Logo (logo.png)
- DeviceIdHelper
- BoolToVisibilityConverter
- 2. Alur Startup
- .MediaCard_Tapped
- .OnNavChecked
- App.xaml.cs
- .FinalizeBypassAsync
- .GetOrBuildListingCardAsync
- Page
- graphify reference: query, path, explain
- Add Game: sumber API dan ZIP final
- Q: Analisis pemetaan frontend GamePageDetail dan GET keseluruhan metadata untuk implementasi HTML, JavaScript, dan CSS responsif, tanpa Add Game, Online-Fix, dan Restart Steam.
- SteamGuardService
- GameHub Feature Parity
- .AboutGameWebView_NavigationCompleted
- 17. Masalah Performa
- Final Data-Flow Optimization - Actual Result
- Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan
- Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?
- Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut
- Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama
- Q: NEXAPLAY Games Browse artwork eligibility filter
- Q: Analisa alur masing-masing API di api.json mendapatkan appid.zip dan pemrosesan manifest serta Lua sebelum dipindahkan
- AppConstants
- Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?
- NexaPlay.SelfCheck.csproj
- ObservableObject
- .InstallLuaFromZip
- RoutedEventArgs
- RootCardBorder
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- 10. Pengaturan
- 3. Alur Data Home
- Migration Status - Local SQLite Catalog
- StringExtensions.cs
- LibraryList
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 14. Inventaris Model / DTO
- 5. Alur Data Pencarian
- 7. Alur Data Library
- 8. Alur Data Bypass
- Minimalist Monochrome Branding
- CatalogProtectionResolver.cs
- AGENTS.md
- extraction-spec.md
- 18. Arsitektur Target yang Direkomendasikan
- Monochrome Branding
- notifyHost
- RTK Output Filtering
- Layered Service Architecture
- Small Batch Build Workflow
- Home Page Decision Archive
- Layout-Cycle-Safe WebView Height Feedback
- NexaPlay Wordmark
- Game Detail Stabilization Edit Log

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 86 edges
2. `GameDetailPage` - 83 edges
3. `BypassGameDetailViewModel` - 70 edges
4. `NexaPlay.Core.Models` - 67 edges
5. `MainWindow` - 62 edges
6. `GamesViewModel` - 62 edges
7. `Window` - 60 edges
8. `BypassGameDetailPage` - 56 edges
9. `NexaPlay.Contracts.Services` - 53 edges
10. `HomeViewModel` - 51 edges

## Surprising Connections (you probably didn't know these)
- `NexaPlay Performance Contract` --semantically_similar_to--> `Lazy Rich Detail Flow`  [INFERRED] [semantically similar]
  NexaPlay/AGENTS.md → CLAUDE.md
- `Bundled Catalog Seed` --conceptually_related_to--> `Local SQLite Catalog`  [INFERRED]
  NexaPlay/release/README.md → DATA_FLOW_AUDIT.md
- `FakeSteam` --implements--> `ISteamService`  [EXTRACTED]
  NexaPlay.SelfCheck/AddGameChecks.cs → NexaPlay/Contracts/Services/ISteamService.cs
- `NullOverrideService` --implements--> `INexaPlayOverrideService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/INexaPlayOverrideService.cs
- `NullLogService` --implements--> `IAppLogService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/IAppLogService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]

## Communities (142 total, 20 thin omitted)

### Community 0 - "SqliteCatalogRepository"
Cohesion: 0.05
Nodes (49): BrowseBlock, BrowseCandidate, CatalogManifest, CatalogPatchEntry, CatalogPatchRow, CancellationToken, Task, ICatalogBootstrapService (+41 more)

### Community 1 - "LicenseInfo"
Cohesion: 0.38
Nodes (4): CancellationToken, Task, ILicenseService, LicenseInfo

### Community 2 - "HomeViewModel"
Cohesion: 0.07
Nodes (30): CancellationToken, Task, ICoverImageCacheService, CancellationToken, IProgress, IReadOnlyList, Task, IMetadataService (+22 more)

### Community 3 - "SteamPlatformService"
Cohesion: 0.08
Nodes (19): appId, close, found, installdir, keyLine, name, CancellationToken, IReadOnlyList (+11 more)

### Community 4 - "Window"
Cohesion: 0.06
Nodes (54): AccountHeader, AppTitleBar, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames (+46 more)

### Community 5 - "Task"
Cohesion: 0.10
Nodes (19): HttpMessageHandler, BlockingHandler, CancellationToken, Dictionary, HttpRequestMessage, HttpResponseMessage, IReadOnlyDictionary, IReadOnlyList (+11 more)

### Community 6 - "MetadataService"
Cohesion: 0.12
Nodes (17): ICollection, ISet, CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task, INexaPlayOverrideService, MetadataWarmupProgress (+9 more)

### Community 7 - "Audit Phase 2 — Cache, Games, Refresh"
Cohesion: 0.05
Nodes (43): 10. Home Popular Current Flow, 11. Home Recent Current Flow, 12. Games SQLite Current Flow, 13. Games Default Ordering, 14. Daily Rotation, 15. Default Price Floor, 16. Price Distribution/Mix, 17. Games Paging (+35 more)

### Community 8 - "SteamStoreService"
Cohesion: 0.08
Nodes (22): HasTransientError, Json, JsonArray, JsonObject, CancellationToken, Task, ISteamStoreService, DateTime (+14 more)

### Community 9 - "NexaPlayOverrideService"
Cohesion: 0.11
Nodes (20): DetailHeroSelector, IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride, NexaPlayMovieOverride, NexaPlayScreenshotOverride, bool, CancellationToken (+12 more)

### Community 10 - "BypassGamesViewModel"
Cohesion: 0.07
Nodes (28): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, CancellationToken, IProgress, Task, IOnlineFixService (+20 more)

### Community 11 - "DynamicFeedRefreshService"
Cohesion: 0.10
Nodes (21): Change, Failed, FeedSpec, CancellationToken, Task, IDynamicFeedRefreshService, DynamicFeedChanges, DynamicFeedKind (+13 more)

### Community 12 - "GameDetailPage"
Cohesion: 0.07
Nodes (15): double, MediaSource, bool, CancellationTokenSource, Dictionary, int, JsonSerializerOptions, List (+7 more)

### Community 13 - "Application"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 14 - "OnlineFixService"
Cohesion: 0.15
Nodes (12): Dictionary, IReadOnlyDictionary, SemaphoreSlim, string, Task, AppliedStateStore, CancellationToken, HttpClient (+4 more)

### Community 15 - ".ApplyHwndIcon"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 16 - "Page"
Cohesion: 0.08
Nodes (34): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep (+26 more)

### Community 17 - "NexaPlay.Core.Enums"
Cohesion: 0.13
Nodes (5): NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Services, NexaPlay.Core.Enums, NexaPlay.Infrastructure.Platform

### Community 18 - "AddGameService"
Cohesion: 0.14
Nodes (8): ConcurrentDictionary, HttpClient, IProgress, IReadOnlyList, JsonElement, string, Task, AddGameService

### Community 19 - "BypassTutorialVideoService"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 20 - "BypassGameDetailViewModel"
Cohesion: 0.06
Nodes (24): error, fileName, meta, FixFile, Action, Func, HttpClient, HttpResponseMessage (+16 more)

### Community 21 - "MainWindow"
Cohesion: 0.14
Nodes (14): bool, ContentDialog, int, NavigationEventArgs, RoutedEventArgs, Storyboard, string, Task (+6 more)

### Community 22 - "GameDetailViewModel"
Cohesion: 0.05
Nodes (21): CancellationToken, CancellationTokenSource, Func, HashSet, int, IReadOnlyList, RelayCommand, Task (+13 more)

### Community 23 - "LibraryViewModel"
Cohesion: 0.12
Nodes (16): CancellationTokenSource, int, IReadOnlyDictionary, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel (+8 more)

### Community 24 - "BypassGameDetailPage"
Cohesion: 0.10
Nodes (10): RootLayout, bool, CoreWebView2WebMessageReceivedEventArgs, NavigationEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, SizeChangedEventArgs, Storyboard (+2 more)

### Community 25 - "GamesViewModel"
Cohesion: 0.09
Nodes (17): DateOnly, Action, bool, CancellationTokenSource, Dictionary, int, ObservableCollection, SemaphoreSlim (+9 more)

### Community 26 - "AccountReportLimitStore"
Cohesion: 0.11
Nodes (18): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, Dictionary, int, SemaphoreSlim (+10 more)

### Community 27 - "AppUpdateService"
Cohesion: 0.14
Nodes (14): DateTimeOffset, IReadOnlyList, AppUpdateCheckResult, DateTimeOffset, List, AppUpdateState, CancellationToken, HttpClient (+6 more)

### Community 28 - "What You Must Do When Invoked"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 29 - "GamesPage"
Cohesion: 0.11
Nodes (11): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, int, IReadOnlyList, ItemClickEventArgs (+3 more)

### Community 30 - "NexaPlay.Core.Models"
Cohesion: 0.15
Nodes (6): NexaPlay.Core.Models, NexaPlay.Contracts.Services, DateTimeOffset, List, AppUpdateManifest, NavigationItem

### Community 31 - "CoverImageCacheService"
Cohesion: 0.17
Nodes (11): CancellationToken, ConcurrentDictionary, HttpClient, int, long, SemaphoreSlim, string, Task (+3 more)

### Community 32 - ".LoadAsync"
Cohesion: 0.18
Nodes (4): IReadOnlyList, RemoveGameResult, RelayCommand, Task

### Community 33 - "NexaPlay.Core.Helpers"
Cohesion: 0.17
Nodes (6): NexaPlay.Core.Helpers, NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 34 - "LibraryPage"
Cohesion: 0.11
Nodes (10): Brush, DependencyObject, int, NavigationEventArgs, PropertyChangedEventArgs, RoutedEventArgs, string, Task (+2 more)

### Community 35 - "AddGamePayloadBuilder"
Cohesion: 0.12
Nodes (14): char, MemberMetadata, CancellationToken, IEnumerable, int, long, string, uint (+6 more)

### Community 36 - ".OnPageUnloaded"
Cohesion: 0.18
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 37 - "Page"
Cohesion: 0.13
Nodes (12): FilterOverlay, FilterOverlayTranslate, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, RoutedEventArgs (+4 more)

### Community 38 - "SettingsPage"
Cohesion: 0.18
Nodes (6): ContentDialog, NavigationEventArgs, RoutedEventArgs, Task, Visibility, SettingsPage

### Community 39 - "HomePage"
Cohesion: 0.10
Nodes (11): Border, bool, DependencyObject, DispatcherTimer, ImageSource, int, PointerRoutedEventArgs, Visibility (+3 more)

### Community 40 - "Page"
Cohesion: 0.15
Nodes (17): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+9 more)

### Community 41 - "Page"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 42 - "INavigationService"
Cohesion: 0.18
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 43 - "BypassProgressState"
Cohesion: 0.33
Nodes (5): IProgress, BypassStatus, BypassProgressState, CaptureProgress, List

### Community 44 - "Local SQLite Catalog"
Cohesion: 0.13
Nodes (16): Incremental Catalog Patches, Lazy R2 Game Metadata, Local SQLite Catalog, NexaPlay Data Flow Audit, NexaPlay Data Sources Query, New Games Download Timing, Load Games Download Cost, ETag Catalog Refresh (+8 more)

### Community 45 - "IAppLogService"
Cohesion: 0.12
Nodes (9): ConcurrentQueue, IReadOnlyList, Task, IAppLogService, int, IReadOnlyList, string, Task (+1 more)

### Community 46 - "BypassGamesPage"
Cohesion: 0.12
Nodes (9): Brush, DependencyObject, DispatcherTimer, int, NavigationEventArgs, RoutedEventArgs, SizeChangedEventArgs, Visibility (+1 more)

### Community 47 - "ScreenshotEntry"
Cohesion: 0.25
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 48 - "Audit Alur Data NexaPlay"
Cohesion: 0.13
Nodes (14): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif, 20. Rencana Migrasi yang Diusulkan (+6 more)

### Community 49 - "NexaPlay.csproj"
Cohesion: 0.17
Nodes (10): net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839), Microsoft.WindowsAppSDK (1.6.241114003) (+2 more)

### Community 50 - "Page"
Cohesion: 0.14
Nodes (13): CustomPipsContainer, HeroBypassButtonText, HeroCarousel, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, RoutedEventArgs (+5 more)

### Community 51 - "IAddGameService"
Cohesion: 0.20
Nodes (4): CancellationToken, IProgress, Task, IAddGameService

### Community 52 - ".NavigateTo"
Cohesion: 0.15
Nodes (9): BypassSubmenuSteam, BypassSubmenuThirdParty, SidebarShell, ValidationEnterKeyButton, ValidationRetryButton, PointerRoutedEventArgs, Button, RadioButton (+1 more)

### Community 53 - "Page"
Cohesion: 0.20
Nodes (8): GamesGrid, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, Border, GridView, Page

### Community 54 - ".OnActivateClicked"
Cohesion: 0.20
Nodes (4): Color, LicensePlan, LicenseStatus, ActivateButton

### Community 56 - ".OnNavigatedTo"
Cohesion: 0.18
Nodes (4): GamesGrid, NavigationEventArgs, SizeChangedEventArgs, GridView

### Community 57 - "FakeSteam"
Cohesion: 0.14
Nodes (7): DateTime, InstalledGame, CancellationToken, HttpRequestMessage, HttpResponseMessage, IReadOnlyList, FakeSteam

### Community 58 - "Atomic Last-Known-Good Publication"
Cohesion: 0.18
Nodes (12): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+4 more)

### Community 59 - ".TutorialThumbnail_Tapped"
Cohesion: 0.20
Nodes (7): CoreWebView2, CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper, TutorialThumbnailOverlay, TappedRoutedEventArgs

### Community 60 - "CatalogBootstrapService"
Cohesion: 0.36
Nodes (6): bool, CancellationToken, SemaphoreSlim, string, Task, CatalogBootstrapService

### Community 61 - ".TryCreate"
Cohesion: 0.17
Nodes (6): Uri, ImageUriResolver, Uri, ImageSource, ImageSource, ImageSource

### Community 63 - "AddGameChecks"
Cohesion: 0.33
Nodes (4): AddGameChecks, Action, Task, ZipArchive

### Community 64 - ".PopularGamesGrid_SizeChanged"
Cohesion: 0.40
Nodes (3): PopularGamesGrid, SizeChangedEventArgs, GridView

### Community 65 - "WindowsDefenderService"
Cohesion: 0.29
Nodes (5): AntivirusVendor, AntivirusInfo, IReadOnlyList, Task, WindowsDefenderService

### Community 66 - "Grid"
Cohesion: 0.50
Nodes (4): LicenseOverlay, ValidatingLicenseOverlay, VersionGrid, Grid

### Community 67 - "LicenseStore"
Cohesion: 0.29
Nodes (4): string, Task, LicenseStore, StoredLicense

### Community 68 - "SettingsViewModel"
Cohesion: 0.20
Nodes (8): CancellationToken, IProgress, Task, IAppUpdateService, IReadOnlyList, Task, SettingsViewModel, Get

### Community 69 - "Graphify Workflow"
Cohesion: 0.20
Nodes (10): Project Graphify Rules, Graphify Add and Watch, Graphify Export Formats, Graphify Semantic Extraction Spec, Graphify GitHub and Merge Flow, Graphify Hook Integration, Graphify Query Traversal, Graphify Media Transcription (+2 more)

### Community 70 - ".SelectManualFolderAsync"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 71 - "CatalogManifestRules"
Cohesion: 0.24
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 72 - ".Log"
Cohesion: 0.55
Nodes (3): CancellationToken, Task, LicenseService

### Community 73 - ".ShowAsync"
Cohesion: 0.29
Nodes (6): Task, LicenseAccessDialogHelper, Task, TappedRoutedEventArgs, UIElement, XamlRoot

### Community 74 - "IWindowsDefenderService"
Cohesion: 0.26
Nodes (4): IReadOnlyList, Task, IWindowsDefenderService, DefenderExclusionResult

### Community 75 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 76 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.16
Nodes (7): FrameworkElement, Image, HeroImage, MediaOverlayImage, ExceptionRoutedEventArgs, Image, TranslateTransform

### Community 77 - ".LoadGamesAsync"
Cohesion: 0.13
Nodes (8): Message, ICatalogRefreshState, CancellationToken, Task, ICatalogUpdateService, long, CatalogRefreshState, Success

### Community 78 - "NexaPlay Gold Lightning Logo (logo.png)"
Cohesion: 0.22
Nodes (9): NexaPlay Gold Lightning Logo (logo.png), NexaPlay Gold Lightning Logo (LockScreenLogo.scale-200.png), NexaPlay Gold Lightning Logo (logo.svg), NexaPlay Gold Lightning Logo (SplashScreen.scale-200.png), NexaPlay Gold Lightning Logo (Square150x150Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.targetsize-24_altform-unplated.png), NexaPlay Gold Lightning Logo (StoreLogo.png) (+1 more)

### Community 84 - "BoolToVisibilityConverter"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 85 - "2. Alur Startup"
Cohesion: 0.29
Nodes (7): 2. Alur Startup, Entry point dan service, Gerbang license, Klasifikasi blocking, Pemanasan metadata, Pramuat cover dan halaman, Rantai pemanggilan aktual

### Community 86 - ".MediaCard_Tapped"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 89 - ".OnNavChecked"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 90 - "App.xaml.cs"
Cohesion: 0.29
Nodes (3): NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Infrastructure.Logging

### Community 91 - ".FinalizeBypassAsync"
Cohesion: 0.29
Nodes (3): CancellationToken, Task, SteamFinalizeResult

### Community 92 - ".GetOrBuildListingCardAsync"
Cohesion: 0.24
Nodes (3): CancellationToken, IReadOnlyList, Task

### Community 93 - "Page"
Cohesion: 0.05
Nodes (33): AboutGameWebView, CompactState, DenuvoBadge, DenuvoBadgeGlowOverlay, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroTitleText (+25 more)

### Community 95 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 96 - "Add Game: sumber API dan ZIP final"
Cohesion: 0.33
Nodes (5): Add Game: sumber API dan ZIP final, Pemasangan, Persiapan khusus lima sumber GitHub, Urutan sumber, Verifikasi

### Community 97 - "Q: Analisis pemetaan frontend GamePageDetail dan GET keseluruhan metadata untuk implementasi HTML, JavaScript, dan CSS responsif, tanpa Add Game, Online-Fix, dan Restart Steam."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Analisis pemetaan frontend GamePageDetail dan GET keseluruhan metadata untuk implementasi HTML, JavaScript, dan CSS responsif, tanpa Add Game, Online-Fix, dan Restart Steam., Source Nodes

### Community 98 - "SteamGuardService"
Cohesion: 0.40
Nodes (3): Action, Task, SteamGuardService

### Community 100 - "GameHub Feature Parity"
Cohesion: 0.50
Nodes (5): NexaPlay Native Remake, GameHub Feature Parity, Enterprise-Clean UI Design Contract, Native Migration Strategy, NexaPlay Project Entrypoint

### Community 101 - ".AboutGameWebView_NavigationCompleted"
Cohesion: 0.40
Nodes (3): CoreWebView2NavigationCompletedEventArgs, CoreWebView2WebMessageReceivedEventArgs, WebView2

### Community 102 - "17. Masalah Performa"
Cohesion: 0.40
Nodes (5): 17. Masalah Performa, KRITIS, RENDAH, SEDANG, TINGGI

### Community 103 - "Final Data-Flow Optimization - Actual Result"
Cohesion: 0.40
Nodes (5): Final Data-Flow Optimization - Actual Result, Games Ordering dan Query, Listing dan Detail, Refresh dan Cache, Verification Status

### Community 104 - "Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan, Source Nodes

### Community 105 - "Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?, Source Nodes

### Community 106 - "Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut, Source Nodes

### Community 107 - "Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama, Source Nodes

### Community 108 - "Q: NEXAPLAY Games Browse artwork eligibility filter"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: NEXAPLAY Games Browse artwork eligibility filter, Source Nodes

### Community 109 - "Q: Analisa alur masing-masing API di api.json mendapatkan appid.zip dan pemrosesan manifest serta Lua sebelum dipindahkan"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Analisa alur masing-masing API di api.json mendapatkan appid.zip dan pemrosesan manifest serta Lua sebelum dipindahkan, Source Nodes

### Community 110 - "AppConstants"
Cohesion: 0.40
Nodes (3): string, TimeSpan, AppConstants

### Community 111 - "Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?, Source Nodes

### Community 114 - "NexaPlay.SelfCheck.csproj"
Cohesion: 0.50
Nodes (3): net8.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk

### Community 115 - "ObservableObject"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 117 - "RoutedEventArgs"
Cohesion: 0.13
Nodes (6): OfflineActivationCheckbox, ShimmerContainer, TutorialVideoOverlayCloseBtn, RoutedEventArgs, Button, CheckBox

### Community 118 - "RootCardBorder"
Cohesion: 0.50
Nodes (3): RootCardBorder, PointerRoutedEventArgs, Border

### Community 119 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 120 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 121 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 122 - "10. Pengaturan"
Cohesion: 0.50
Nodes (4): 10. Pengaturan, Bersihkan Cache, Bersihkan Data, Muat Games

### Community 123 - "3. Alur Data Home"
Cohesion: 0.50
Nodes (4): 3. Alur Data Home, Field yang benar-benar di-bind oleh Home, Jawaban, Rantai pemanggilan aktif

### Community 124 - "Migration Status - Local SQLite Catalog"
Cohesion: 0.50
Nodes (4): Arsitektur Aktif, Migration Status - Local SQLite Catalog, Source dan Network Classification, Startup Aktual

### Community 128 - "LibraryList"
Cohesion: 0.50
Nodes (3): LibraryList, ItemClickEventArgs, ListView

### Community 132 - "14. Inventaris Model / DTO"
Cohesion: 0.67
Nodes (3): 14. Inventaris Model / DTO, Matriks penggunaan field, Model utama

### Community 133 - "5. Alur Data Pencarian"
Cohesion: 0.67
Nodes (3): 5. Alur Data Pencarian, Pencarian lain, Rantai pemanggilan pencarian Games

### Community 134 - "7. Alur Data Library"
Cohesion: 0.67
Nodes (3): 7. Alur Data Library, Rantai pemuatan, Sumber kebenaran penyimpanan

### Community 135 - "8. Alur Data Bypass"
Cohesion: 0.67
Nodes (3): 8. Alur Data Bypass, Detail Bypass, Source daftar dan rantai pemanggilan

### Community 136 - "Minimalist Monochrome Branding"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

## Knowledge Gaps
- **302 isolated node(s):** `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `Microsoft.NET.Sdk`, `Viewbox`, `FontIcon` (+297 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `MetadataService` (4× useful, score=1.390911488)
- `SettingsViewModel` (3× useful, score=1.043581771)
- `BypassGamesDataService` (3× useful, score=1.043091052)
- `GameDetailViewModel` (2× useful, score=0.785026236)
- `SteamStoreService` (2× useful, score=0.744748396)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IAppLogService` connect `IAppLogService` to `SqliteCatalogRepository`, `HomeViewModel`, `SteamPlatformService`, `Task`, `MetadataService`, `SteamStoreService`, `NexaPlayOverrideService`, `BypassGamesViewModel`, `DynamicFeedRefreshService`, `GameDetailPage`, `OnlineFixService`, `AddGameService`, `BypassTutorialVideoService`, `BypassGameDetailViewModel`, `MainWindow`, `GameDetailViewModel`, `LibraryViewModel`, `BypassGameDetailPage`, `AccountReportLimitStore`, `AppUpdateService`, `CoverImageCacheService`, `CatalogBootstrapService`, `WindowsDefenderService`, `SettingsViewModel`, `.Log`?**
  _High betweenness centrality (0.213) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `MainWindow` to `LicenseInfo`, `HomeViewModel`, `Window`, `SettingsViewModel`, `INavigationService`, `BypassGamesViewModel`, `IAppLogService`, `.LoadGamesAsync`, `.ApplyHwndIcon`, `ObservableObject`, `.NavigateTo`, `.OnActivateClicked`, `GamesViewModel`, `.OnNavChecked`, `App.xaml.cs`, `ISteamService`?**
  _High betweenness centrality (0.122) - this node is a cross-community bridge._
- **Why does `BypassGameDetailViewModel` connect `BypassGameDetailViewModel` to `NexaPlay.Core.Helpers`, `LicenseInfo`, `HomeViewModel`, `MetadataService`, `SteamStoreService`, `BypassGamesViewModel`, `INavigationService`, `IWindowsDefenderService`, `IAppLogService`, `Page`, `BypassTutorialVideoService`, `ObservableObject`, `BypassGameDetailPage`, `AccountReportLimitStore`, `ISteamService`?**
  _High betweenness centrality (0.107) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _302 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `SqliteCatalogRepository` be split into smaller, more focused modules?**
  _Cohesion score 0.05479059093516925 - nodes in this community are weakly interconnected._
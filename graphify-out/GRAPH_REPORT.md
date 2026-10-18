# Graph Report - NexaPlay  (2026-09-16)

## Corpus Check
- 94 files · ~181,836 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2184 nodes · 4024 edges · 158 communities (129 shown, 29 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 138 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- MetadataService
- HomeViewModel
- BypassGamesDataService
- Window
- BypassGamesPage
- HomePage
- SteamPlatformService
- NexaPlayOverrideService
- LicenseInfo
- SqliteCatalogRepository
- Application
- Page
- GamesViewModel
- Audit Phase 2 — Cache, Games, Refresh
- Page
- SteamStoreService
- GameDetailPage
- BypassTutorialVideoService
- DynamicFeedRefreshService
- AddGameService
- BypassGameDetailViewModel
- GamesPage
- NexaPlay.Core.Models
- GameDetailViewModel
- LibraryPage
- MainWindow
- BypassGameDetailPage
- AppUpdateService
- BypassGamesViewModel
- AccountReportLimitStore
- What You Must Do When Invoked
- Page
- LibraryViewModel
- SettingsPage
- .ApplyFiltersAndPaginationAsync
- ScreenshotEntry
- Task
- Page
- Page
- NexaPlay.Presentation.ViewModels
- .OnNavigatedTo
- SettingsViewModel
- Authoritative New Games AppID List
- .FindSiblingSkeletonOverlay
- RoutedEventArgs
- NexaPlay.Core.Enums
- .ReportAsync
- IAddGameService
- .NavigateTo
- Page
- .LoadGamesAsync
- .TryCreate
- RoutedEventArgs
- INavigationService
- ISteamService
- .HandleOnlineFixPrimaryActionAsync
- NexaPlay.csproj
- OnlineFixService
- CatalogUpdateService
- LicenseStore
- .ExtractDownloadedArchives
- .RemoveFixAsync
- .StartBypassGameAsync
- .ApplyHwndIcon
- .GetAsync
- BoolToVisibilityConverter
- .MediaCard_Tapped
- .OnActivateClicked
- FixEntry
- PointerRoutedEventArgs
- .OnNavChecked
- graphify reference: extra exports and benchmark
- .Log
- .OnNavigatedTo
- GameHub Feature Parity
- .AboutGameWebView_NavigationCompleted
- ICatalogRefreshState
- SteamGuardService
- .ShowAsync
- RootCardBorder
- NexaPlay Wide Tile Logo
- CoverImageCacheService
- graphify reference: query, path, explain
- Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan
- Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?
- LibraryList
- NexaPlay.SelfCheck.csproj
- Minimalist Monochrome Branding
- NexaPlay Square App Logo
- Gray Framed X Symbol
- Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama
- Generate-AppIcon.ps1
- sign-build.ps1
- sign-installer.ps1
- Small Batch Build Workflow
- Letter N Monogram
- Lock Screen Logo Asset
- Monochrome Branding
- NexaPlay Square Application Logo
- notifyHost
- Manifest Publication Ordering
- RTK Output Filtering
- Layered Service Architecture
- Layout-Cycle-Safe WebView Height Feedback
- NexaPlay Wordmark
- NexaPlay Splash Screen
- NexaPlay Application Logo
- Generate-UpdateManifest.ps1
- verify-manifest-recovery.ps1
- Game Detail Stabilization Edit Log
- AppConstants
- .LoadAsync
- Final Data-Flow Optimization - Actual Result
- Audit Alur Data NexaPlay
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- AGENTS.md
- extraction-spec.md
- ICatalogRepository
- CatalogManifestRules
- Page
- .GetOrBuildListingCardAsync
- CatalogLoadCoordinator
- 2. Alur Startup
- 17. Masalah Performa
- 10. Pengaturan
- 3. Alur Data Home
- Migration Status - Local SQLite Catalog
- ObservableObject
- IWindowsDefenderService
- 14. Inventaris Model / DTO
- 5. Alur Data Pencarian
- 8. Alur Data Bypass
- WindowsDefenderService
- .HeroCarousel_SelectionChanged
- CoverArtImage
- RelayCommand
- .PopularGamesGrid_SizeChanged
- AppLogService
- AppUpdateManifest
- .SelectManualFolderAsync
- DeviceIdHelper
- .CollectOrderedAppIds
- IAppLogService
- .TutorialThumbnail_Tapped
- .RootLayout_SizeChanged

## God Nodes (most connected - your core abstractions)
1. `GameDetailPage` - 83 edges
2. `GameDetailViewModel` - 76 edges
3. `NexaPlay.Core.Models` - 66 edges
4. `BypassGameDetailViewModel` - 63 edges
5. `MainWindow` - 62 edges
6. `Window` - 58 edges
7. `BypassGameDetailPage` - 56 edges
8. `GamesViewModel` - 55 edges
9. `NexaPlay.Contracts.Services` - 52 edges
10. `HomeViewModel` - 49 edges

## Surprising Connections (you probably didn't know these)
- `NexaPlay Performance Contract` --semantically_similar_to--> `Lazy Rich Detail Flow`  [INFERRED] [semantically similar]
  NexaPlay/AGENTS.md → CLAUDE.md
- `Bundled Catalog Seed` --conceptually_related_to--> `Local SQLite Catalog`  [INFERRED]
  NexaPlay/release/README.md → DATA_FLOW_AUDIT.md
- `Lazy Rich Detail Flow` --shares_data_with--> `Listing Cover Priority`  [INFERRED]
  CLAUDE.md → docs/superpowers/specs/2026-08-17-fast-catalog-refresh-design.md
- `NullOverrideService` --implements--> `INexaPlayOverrideService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/INexaPlayOverrideService.cs
- `Project Graphify Rules` --references--> `Graphify Workflow`  [EXTRACTED]
  AGENTS.md → .codex/skills/graphify/SKILL.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]

## Communities (158 total, 29 thin omitted)

### Community 0 - "MetadataService"
Cohesion: 0.05
Nodes (53): BrowseBlock, BrowseCandidate, CatalogManifest, CatalogPatchEntry, CatalogPatchRow, CancellationToken, Task, ICatalogBootstrapService (+45 more)

### Community 1 - "HomeViewModel"
Cohesion: 0.05
Nodes (28): IReadOnlyList, Task, IAppLogService, CancellationToken, Task, ILicenseService, IReadOnlyList, Task (+20 more)

### Community 2 - "BypassGamesDataService"
Cohesion: 0.08
Nodes (31): CancellationToken, IProgress, IReadOnlyList, MetadataWarmupProgress, Task, IMetadataService, Action, Func (+23 more)

### Community 3 - "Window"
Cohesion: 0.06
Nodes (26): appId, close, found, installdir, keyLine, name, CancellationToken, IReadOnlyList (+18 more)

### Community 4 - "BypassGamesPage"
Cohesion: 0.06
Nodes (49): AccountHeader, AppTitleBar, ContentFrame, LblBypassGames, LblGames, LblHome, LblLibrary, LblSettings (+41 more)

### Community 5 - "HomePage"
Cohesion: 0.07
Nodes (24): ConcurrentQueue, HttpMessageHandler, HttpRequestMessage, IAppLogService, int, IReadOnlyList, string, Task (+16 more)

### Community 6 - "SteamPlatformService"
Cohesion: 0.11
Nodes (20): ICollection, ISet, CancellationToken, IReadOnlyDictionary, IReadOnlySet, NexaPlayCatalogOverride, NexaPlayDetailOverride, Task (+12 more)

### Community 7 - "NexaPlayOverrideService"
Cohesion: 0.05
Nodes (43): 10. Home Popular Current Flow, 11. Home Recent Current Flow, 12. Games SQLite Current Flow, 13. Games Default Ordering, 14. Daily Rotation, 15. Default Price Floor, 16. Price Distribution/Mix, 17. Games Paging (+35 more)

### Community 8 - "LicenseInfo"
Cohesion: 0.11
Nodes (19): HasTransientError, Json, JsonArray, JsonObject, CancellationToken, ConcurrentDictionary, DateTime, GameDetailEntry (+11 more)

### Community 9 - "SqliteCatalogRepository"
Cohesion: 0.12
Nodes (19): bool, CancellationToken, Dictionary, HttpClient, IAppLogService, IReadOnlyDictionary, IReadOnlyList, IReadOnlySet (+11 more)

### Community 10 - "Application"
Cohesion: 0.14
Nodes (16): IBypassGamesDataService, CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, GameCategory, IReadOnlyList, FixEntry (+8 more)

### Community 11 - "Page"
Cohesion: 0.09
Nodes (22): Change, Failed, FeedSpec, CancellationToken, Task, IDynamicFeedRefreshService, DynamicFeedChanges, DynamicFeedKind (+14 more)

### Community 12 - "GamesViewModel"
Cohesion: 0.08
Nodes (15): double, MediaSource, bool, CancellationTokenSource, Dictionary, IAppLogService, int, JsonSerializerOptions (+7 more)

### Community 13 - "Audit Phase 2 — Cache, Games, Refresh"
Cohesion: 0.08
Nodes (34): Application, CardScale, Checked, CheckedBorder, CheckStates, CommonStates, ContentPresenter, Disabled (+26 more)

### Community 14 - "Page"
Cohesion: 0.11
Nodes (16): CancellationToken, IProgress, Task, IOnlineFixService, Dictionary, IReadOnlyDictionary, SemaphoreSlim, string (+8 more)

### Community 15 - "SteamStoreService"
Cohesion: 0.08
Nodes (22): AppWindow, Color, DllImport, IntPtr, MainViewModel, bool, IAppLogService, IAppUpdateService (+14 more)

### Community 16 - "GameDetailPage"
Cohesion: 0.09
Nodes (27): BypassBtnGlow, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroShimmerSweep, HeroShimmerTranslate, HeroSkeletonOverlay (+19 more)

### Community 17 - "BypassTutorialVideoService"
Cohesion: 0.13
Nodes (6): NexaPlay, NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Logging, NexaPlay.Infrastructure.Services, NexaPlay.Infrastructure.Platform

### Community 18 - "DynamicFeedRefreshService"
Cohesion: 0.11
Nodes (12): BypassProgressState, IAddGameService, CancellationToken, ConcurrentDictionary, HttpResponseMessage, IAppLogService, IProgress, IReadOnlyList (+4 more)

### Community 19 - "AddGameService"
Cohesion: 0.14
Nodes (16): BypassTutorialVideo, IBypassTutorialVideoService, CancellationToken, DateTime, Dictionary, GameCategory, HttpClient, IAppLogService (+8 more)

### Community 20 - "BypassGameDetailViewModel"
Cohesion: 0.10
Nodes (15): IAccountReportService, Func, GameDetailEntry, HttpClient, IAppLogService, IBypassGamesDataService, IBypassTutorialVideoService, ILicenseService (+7 more)

### Community 21 - "GamesPage"
Cohesion: 0.16
Nodes (7): AppUpdateCheckResult, ContentDialog, LicensePlan, RoutedEventArgs, Task, Type, WindowActivatedEventArgs

### Community 22 - "NexaPlay.Core.Models"
Cohesion: 0.08
Nodes (17): BypassStatus, CancellationTokenSource, Func, GameDetailEntry, IAddGameService, IAppLogService, IBypassGamesDataService, ILicenseService (+9 more)

### Community 23 - "GameDetailViewModel"
Cohesion: 0.12
Nodes (14): CancellationTokenSource, IAddGameService, IAppLogService, INavigationService, int, IOnlineFixService, IReadOnlyDictionary, IReadOnlyList (+6 more)

### Community 24 - "LibraryPage"
Cohesion: 0.10
Nodes (11): RootLayout, ShimmerContainer, bool, CoreWebView2WebMessageReceivedEventArgs, IAppLogService, INavigationService, PropertyChangedEventArgs, SizeChangedEventArgs (+3 more)

### Community 25 - "MainWindow"
Cohesion: 0.11
Nodes (13): DateOnly, Action, bool, CancellationTokenSource, Dictionary, ICatalogRefreshState, ICoverImageCacheService, int (+5 more)

### Community 26 - "BypassGameDetailPage"
Cohesion: 0.11
Nodes (18): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, Dictionary, int, SemaphoreSlim (+10 more)

### Community 27 - "AppUpdateService"
Cohesion: 0.18
Nodes (12): AppUpdateState, IAppUpdateService, AppUpdateCheckResult, CancellationToken, HttpClient, IAppLogService, IProgress, JsonSerializerOptions (+4 more)

### Community 28 - "BypassGamesViewModel"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 29 - "AccountReportLimitStore"
Cohesion: 0.11
Nodes (12): IEnumerable, bool, Brush, DependencyObject, DispatcherTimer, INavigationService, int, IReadOnlyList (+4 more)

### Community 30 - "What You Must Do When Invoked"
Cohesion: 0.14
Nodes (4): NexaPlay.Core.Models, NexaPlay.Contracts.Services, MetadataWarmupProgress, NavigationItem

### Community 31 - "Page"
Cohesion: 0.15
Nodes (13): ICoverImageCacheService, CancellationToken, ConcurrentDictionary, HttpClient, IAppLogService, int, long, SemaphoreSlim (+5 more)

### Community 32 - "LibraryViewModel"
Cohesion: 0.15
Nodes (9): HeroImage, MediaOverlayImage, MediaScrollViewer, Page, RightColumnDefinition, RoutedEventArgs, Image, ColumnDefinition (+1 more)

### Community 33 - "SettingsPage"
Cohesion: 0.20
Nodes (6): NexaPlay.Core.Helpers, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, StringExtensions, SystemRequirementItem

### Community 34 - ".ApplyFiltersAndPaginationAsync"
Cohesion: 0.13
Nodes (9): Brush, DependencyObject, INavigationService, int, PropertyChangedEventArgs, RoutedEventArgs, string, Visibility (+1 more)

### Community 35 - "ScreenshotEntry"
Cohesion: 0.12
Nodes (14): AntivirusInfo, BypassStatus, CancellationTokenSource, IAppLogService, IBypassGamesDataService, int, IOnlineFixService, IReadOnlyList (+6 more)

### Community 36 - "Task"
Cohesion: 0.13
Nodes (7): CoverArtImage, HeroImage, OfflineActivationCheckbox, ExceptionRoutedEventArgs, RoutedEventArgs, Image, CheckBox

### Community 37 - "Page"
Cohesion: 0.15
Nodes (10): FilterOverlay, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, RoutedEventArgs, Border (+2 more)

### Community 38 - "Page"
Cohesion: 0.21
Nodes (5): ContentDialog, RoutedEventArgs, Task, Visibility, SettingsPage

### Community 39 - "NexaPlay.Presentation.ViewModels"
Cohesion: 0.13
Nodes (9): bool, DependencyObject, DispatcherTimer, ILicenseService, ImageSource, INavigationService, int, Visibility (+1 more)

### Community 40 - ".OnNavigatedTo"
Cohesion: 0.15
Nodes (16): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+8 more)

### Community 41 - "SettingsViewModel"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 42 - "Authoritative New Games AppID List"
Cohesion: 0.14
Nodes (8): NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Navigation, Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 43 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.14
Nodes (8): NexaPlay.Core.Enums, CancellationToken, Task, IBypassTutorialVideoService, BypassStatus, GameCategory, BypassProgressState, BypassTutorialVideo

### Community 44 - "RoutedEventArgs"
Cohesion: 0.13
Nodes (16): Incremental Catalog Patches, Lazy R2 Game Metadata, Local SQLite Catalog, NexaPlay Data Flow Audit, NexaPlay Data Sources Query, New Games Download Timing, Load Games Download Cost, ETag Catalog Refresh (+8 more)

### Community 45 - "NexaPlay.Core.Enums"
Cohesion: 0.14
Nodes (13): AntivirusInfo, AppUpdateCheckResult, IAppLogService, IAppUpdateService, IBypassGamesDataService, ICatalogRefreshState, ICoverImageCacheService, ILicenseService (+5 more)

### Community 46 - ".ReportAsync"
Cohesion: 0.16
Nodes (9): Brush, DependencyObject, DispatcherTimer, ILicenseService, INavigationService, int, Visibility, BypassGamesPage (+1 more)

### Community 47 - "IAddGameService"
Cohesion: 0.23
Nodes (4): GameDetailEntry, HashSet, IReadOnlyList, ScreenshotEntry

### Community 48 - ".NavigateTo"
Cohesion: 0.13
Nodes (14): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif, 20. Rencana Migrasi yang Diusulkan (+6 more)

### Community 49 - "Page"
Cohesion: 0.13
Nodes (12): net8.0, net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk, Microsoft.Data.Sqlite (8.0.20), CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8) (+4 more)

### Community 50 - ".LoadGamesAsync"
Cohesion: 0.17
Nodes (9): HeroBypassButtonText, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, RoutedEventArgs, TappedRoutedEventArgs, Border (+1 more)

### Community 51 - ".TryCreate"
Cohesion: 0.17
Nodes (6): CancellationToken, IProgress, IReadOnlyList, Task, IAddGameService, RemoveGameResult

### Community 52 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (8): ActivateButton, BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, Button, RadioButton, TextBlock

### Community 53 - "INavigationService"
Cohesion: 0.18
Nodes (7): HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, RoutedEventArgs, Border, Page

### Community 54 - "ISteamService"
Cohesion: 0.21
Nodes (3): RelayCommand, Task, Task

### Community 56 - "NexaPlay.csproj"
Cohesion: 0.15
Nodes (5): GamesGrid, NavigationEventArgs, SizeChangedEventArgs, PopularGamesGrid, GridView

### Community 57 - "OnlineFixService"
Cohesion: 0.20
Nodes (7): Application, IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Window, App, object

### Community 58 - "CatalogUpdateService"
Cohesion: 0.18
Nodes (12): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+4 more)

### Community 59 - "LicenseStore"
Cohesion: 0.20
Nodes (7): CoreWebView2, CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper, TutorialThumbnailOverlay, TappedRoutedEventArgs

### Community 60 - ".ExtractDownloadedArchives"
Cohesion: 0.27
Nodes (7): CancellationToken, IProgress, Task, IAppUpdateService, DateTimeOffset, IReadOnlyList, AppUpdateCheckResult

### Community 61 - ".RemoveFixAsync"
Cohesion: 0.17
Nodes (6): Uri, ImageUriResolver, Uri, ImageSource, ImageSource, ImageSource

### Community 63 - ".StartBypassGameAsync"
Cohesion: 0.24
Nodes (4): RemoveGameResult, Task, NavigationEventArgs, Task

### Community 64 - ".ApplyHwndIcon"
Cohesion: 0.22
Nodes (5): fileName, meta, FixFile, List, path

### Community 65 - ".GetAsync"
Cohesion: 0.18
Nodes (7): INotifyPropertyChanged, DateTime, IReadOnlyList, GameDetailEntry, MovieEntry, bool, ScreenshotEntry

### Community 66 - "BoolToVisibilityConverter"
Cohesion: 0.22
Nodes (5): CancellationToken, GameDetailEntry, Task, ISteamStoreService, CancellationToken

### Community 67 - ".MediaCard_Tapped"
Cohesion: 0.31
Nodes (7): bool, CancellationToken, IAppLogService, SemaphoreSlim, string, Task, CatalogBootstrapService

### Community 69 - "FixEntry"
Cohesion: 0.20
Nodes (10): Project Graphify Rules, Graphify Add and Watch, Graphify Export Formats, Graphify Semantic Extraction Spec, Graphify GitHub and Merge Flow, Graphify Hook Integration, Graphify Query Traversal, Graphify Media Transcription (+2 more)

### Community 70 - "PointerRoutedEventArgs"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 71 - ".OnNavChecked"
Cohesion: 0.27
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 72 - "graphify reference: extra exports and benchmark"
Cohesion: 0.20
Nodes (10): BypassSubmenuPanel, NavItemsPanel, ValidationErrorPanel, StackPanel, LeftContentPanel, MediaPanel, RequirementsMinimumPanel, RequirementsRecommendedPanel (+2 more)

### Community 73 - ".Log"
Cohesion: 0.42
Nodes (4): Task, LicenseAccessDialogHelper, UIElement, XamlRoot

### Community 74 - ".OnNavigatedTo"
Cohesion: 0.20
Nodes (5): HeroCarousel, NavigationEventArgs, SizeChangedEventArgs, SelectionChangedEventArgs, FlipView

### Community 75 - "GameHub Feature Parity"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 76 - ".AboutGameWebView_NavigationCompleted"
Cohesion: 0.25
Nodes (4): FrameworkElement, Image, ExceptionRoutedEventArgs, TranslateTransform

### Community 78 - "SteamGuardService"
Cohesion: 0.22
Nodes (9): NexaPlay Gold Lightning Logo (logo.png), NexaPlay Gold Lightning Logo (LockScreenLogo.scale-200.png), NexaPlay Gold Lightning Logo (logo.svg), NexaPlay Gold Lightning Logo (SplashScreen.scale-200.png), NexaPlay Gold Lightning Logo (Square150x150Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.targetsize-24_altform-unplated.png), NexaPlay Gold Lightning Logo (StoreLogo.png) (+1 more)

### Community 79 - ".ShowAsync"
Cohesion: 0.39
Nodes (3): Action, HttpResponseMessage, Task

### Community 81 - "NexaPlay Wide Tile Logo"
Cohesion: 0.22
Nodes (3): GamesGrid, NavigationEventArgs, SizeChangedEventArgs

### Community 82 - "CoverImageCacheService"
Cohesion: 0.25
Nodes (4): DenuvoBadge, DenuvoBadgeGlowOverlay, PropertyChangedEventArgs, Border

### Community 84 - "Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 85 - "Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?"
Cohesion: 0.29
Nodes (7): 2. Alur Startup, Entry point dan service, Gerbang license, Klasifikasi blocking, Pemanasan metadata, Pramuat cover dan halaman, Rantai pemanggilan aktual

### Community 86 - "LibraryList"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 88 - "Minimalist Monochrome Branding"
Cohesion: 0.29
Nodes (3): ICatalogRefreshState, long, CatalogRefreshState

### Community 89 - "NexaPlay Square App Logo"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 93 - "Generate-AppIcon.ps1"
Cohesion: 0.29
Nodes (5): DetailLayoutGrid, MediaOverlay, RootLayout, SizeChangedEventArgs, Grid

### Community 95 - "sign-installer.ps1"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 96 - "Small Batch Build Workflow"
Cohesion: 0.40
Nodes (3): CancellationToken, Task, ICoverImageCacheService

### Community 97 - "Letter N Monogram"
Cohesion: 0.53
Nodes (5): IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride, NexaPlayMovieOverride, NexaPlayScreenshotOverride

### Community 98 - "Lock Screen Logo Asset"
Cohesion: 0.40
Nodes (3): Action, Task, SteamGuardService

### Community 100 - "NexaPlay Square Application Logo"
Cohesion: 0.50
Nodes (5): NexaPlay Native Remake, GameHub Feature Parity, Enterprise-Clean UI Design Contract, Native Migration Strategy, NexaPlay Project Entrypoint

### Community 101 - "notifyHost"
Cohesion: 0.40
Nodes (3): CoreWebView2NavigationCompletedEventArgs, CoreWebView2WebMessageReceivedEventArgs, WebView2

### Community 102 - "Manifest Publication Ordering"
Cohesion: 0.40
Nodes (5): 17. Masalah Performa, KRITIS, RENDAH, SEDANG, TINGGI

### Community 103 - "RTK Output Filtering"
Cohesion: 0.40
Nodes (5): Final Data-Flow Optimization - Actual Result, Games Ordering dan Query, Listing dan Detail, Refresh dan Cache, Verification Status

### Community 104 - "Layered Service Architecture"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan, Source Nodes

### Community 105 - "Layout-Cycle-Safe WebView Height Feedback"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?, Source Nodes

### Community 106 - "NexaPlay Wordmark"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut, Source Nodes

### Community 107 - "NexaPlay Splash Screen"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama, Source Nodes

### Community 108 - "NexaPlay Application Logo"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: NEXAPLAY Games Browse artwork eligibility filter, Source Nodes

### Community 109 - "Generate-UpdateManifest.ps1"
Cohesion: 0.40
Nodes (5): IconElement, FontIcon, BypassChevron, LicenseStatusIcon, ValidationErrorIcon

### Community 110 - "verify-manifest-recovery.ps1"
Cohesion: 0.40
Nodes (3): string, TimeSpan, AppConstants

### Community 111 - "Game Detail Stabilization Edit Log"
Cohesion: 0.50
Nodes (3): GameDetailEntry, NexaPlayCatalogOverride, DetailHeroSelector

### Community 112 - "AppConstants"
Cohesion: 0.40
Nodes (5): ContentTopBarRow, WindowTopRow, RowDefinition, HeroRowDefinition, HeroRowDefinition

### Community 115 - "Audit Alur Data NexaPlay"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 118 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): RootCardBorder, PointerRoutedEventArgs, Border

### Community 119 - "graphify reference: GitHub clone and cross-repo merge"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 120 - "graphify reference: transcribe video and audio"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 121 - "AGENTS.md"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 122 - "extraction-spec.md"
Cohesion: 0.50
Nodes (4): 10. Pengaturan, Bersihkan Cache, Bersihkan Data, Muat Games

### Community 123 - "ICatalogRepository"
Cohesion: 0.50
Nodes (4): 3. Alur Data Home, Field yang benar-benar di-bind oleh Home, Jawaban, Rantai pemanggilan aktif

### Community 124 - "CatalogManifestRules"
Cohesion: 0.50
Nodes (4): Arsitektur Aktif, Migration Status - Local SQLite Catalog, Source dan Network Classification, Startup Aktual

### Community 125 - "Page"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

### Community 126 - ".GetOrBuildListingCardAsync"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateState

### Community 128 - "CatalogLoadCoordinator"
Cohesion: 0.50
Nodes (3): LibraryList, ItemClickEventArgs, ListView

### Community 129 - "2. Alur Startup"
Cohesion: 0.67
Nodes (3): List, RequestSnapshot, RouteHandler

### Community 132 - "3. Alur Data Home"
Cohesion: 0.67
Nodes (3): 14. Inventaris Model / DTO, Matriks penggunaan field, Model utama

### Community 133 - "Migration Status - Local SQLite Catalog"
Cohesion: 0.67
Nodes (3): 5. Alur Data Pencarian, Pencarian lain, Rantai pemanggilan pencarian Games

### Community 134 - "ObservableObject"
Cohesion: 0.67
Nodes (3): 7. Alur Data Library, Rantai pemuatan, Sumber kebenaran penyimpanan

### Community 135 - "IWindowsDefenderService"
Cohesion: 0.67
Nodes (3): 8. Alur Data Bypass, Detail Bypass, Source daftar dan rantai pemanggilan

### Community 136 - "14. Inventaris Model / DTO"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

### Community 139 - "8. Alur Data Bypass"
Cohesion: 0.67
Nodes (3): TutorialOverlayWebView, AboutGameWebView, WebView2

## Knowledge Gaps
- **241 isolated node(s):** `MetadataWarmupProgress`, `NavigationItem`, `TelegramResponse`, `ScaleTransform`, `Viewbox` (+236 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **29 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `NexaPlay.Core.Models` connect `What You Must Do When Invoked` to `MetadataService`, `HomeViewModel`, `.ApplyHwndIcon`, `.GetAsync`, `BypassGamesDataService`, `Letter N Monogram`, `Window`, `.OnNavChecked`, `SettingsPage`, `2. Alur Startup`, `.FindSiblingSkeletonOverlay`, `Page`, `BypassTutorialVideoService`, `.TryCreate`, `BypassGameDetailPage`, `.ExtractDownloadedArchives`, `Page`, `.GetOrBuildListingCardAsync`?**
  _High betweenness centrality (0.134) - this node is a cross-community bridge._
- **Why does `GameDetailViewModel` connect `NexaPlay.Core.Models` to `LibraryViewModel`, `SettingsPage`, `BypassGamesDataService`, `BoolToVisibilityConverter`, `SteamPlatformService`, `Application`, `GamesViewModel`, `Final Data-Flow Optimization - Actual Result`, `Audit Alur Data NexaPlay`, `ISteamService`, `LibraryList`, `Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut`, `.ViewModel_PropertyChanged`, `CatalogBootstrapService`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Why does `NexaPlay.Contracts.Services` connect `What You Must Do When Invoked` to `MetadataService`, `HomeViewModel`, `Small Batch Build Workflow`, `SettingsPage`, `2. Alur Startup`, `.FindSiblingSkeletonOverlay`, `BypassTutorialVideoService`, `.TryCreate`, `Minimalist Monochrome Branding`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **What connects `MetadataWarmupProgress`, `NavigationItem`, `TelegramResponse` to the rest of the system?**
  _241 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MetadataService` be split into smaller, more focused modules?**
  _Cohesion score 0.05030643513789581 - nodes in this community are weakly interconnected._
- **Should `HomeViewModel` be split into smaller, more focused modules?**
  _Cohesion score 0.05225576111652061 - nodes in this community are weakly interconnected._
- **Should `BypassGamesDataService` be split into smaller, more focused modules?**
  _Cohesion score 0.07645687645687646 - nodes in this community are weakly interconnected._
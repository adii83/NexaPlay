# Graph Report - NexaPlay  (2026-09-15)

## Corpus Check
- 148 files · ~180,248 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2091 nodes · 4183 edges · 153 communities (121 shown, 32 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 297 edges (avg confidence: 0.76)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3ab44172`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- MetadataService
- HomeViewModel
- FixEntry
- Window
- BypassGamesPage
- HomePage
- .Log
- NexaPlayOverrideService
- IAppLogService
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
- Border
- TextBlock
- Page
- NexaPlay.Presentation.ViewModels
- .OnNavigatedTo
- SettingsViewModel
- Authoritative New Games AppID List
- .FindSiblingSkeletonOverlay
- RoutedEventArgs
- NexaPlay.Core.Constants
- TelegramAccountReportService
- IAddGameService
- .NavigateTo
- Page
- .LoadGamesAsync
- .TryCreate
- RoutedEventArgs
- INavigationService
- ISteamService
- NexaPlay.csproj
- CatalogUpdateService
- IAppUpdateService
- .ExtractDownloadedArchives
- .RemoveFixAsync
- .ViewModel_PropertyChanged
- Task
- .ApplyHwndIcon
- .TutorialThumbnail_Tapped
- BoolToVisibilityConverter
- .MediaCard_Tapped
- .OnActivateClicked
- .LoadAsync
- PointerRoutedEventArgs
- .OnNavChecked
- graphify reference: extra exports and benchmark
- .ExtractWithExternalTool
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
- Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut
- Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama
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
- Game Detail Stabilization Edit Log
- AppConstants
- .LoadAsync
- Final Data-Flow Optimization - Actual Result
- Audit Alur Data NexaPlay
- graphify reference: add a URL and watch a folder
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
- CatalogBootstrapService
- CatalogLoadCoordinator
- 2. Alur Startup
- 17. Masalah Performa
- 10. Pengaturan
- 3. Alur Data Home
- Migration Status - Local SQLite Catalog
- ObservableObject
- .CheckAndApplyPatchesAsync
- 14. Inventaris Model / DTO
- 5. Alur Data Pencarian
- 7. Alur Data Library
- 8. Alur Data Bypass
- NexaPlay.Core.Helpers
- 18. Arsitektur Target yang Direkomendasikan
- .GamesGrid_SizeChanged
- IWindowsDefenderService
- .HeroCarousel_SelectionChanged
- LibraryRemoveConfirmOverlay
- Task
- .PopularGamesGrid_SizeChanged
- .OnViewModelPropertyChanged
- AppUpdateManifest
- RootPage
- SearchBox
- .CollectOrderedAppIds
- .FindDescendant

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 86 edges
2. `GameDetailPage` - 83 edges
3. `BypassGameDetailViewModel` - 70 edges
4. `NexaPlay.Core.Models` - 65 edges
5. `MainWindow` - 62 edges
6. `GamesViewModel` - 62 edges
7. `Window` - 60 edges
8. `BypassGameDetailPage` - 56 edges
9. `NexaPlay.Contracts.Services` - 52 edges
10. `HomeViewModel` - 51 edges

## Surprising Connections (you probably didn't know these)
- `NexaPlay Performance Contract` --semantically_similar_to--> `Lazy Rich Detail Flow`  [INFERRED] [semantically similar]
  NexaPlay/AGENTS.md → CLAUDE.md
- `Lazy Rich Detail Flow` --shares_data_with--> `Listing Cover Priority`  [INFERRED]
  CLAUDE.md → docs/superpowers/specs/2026-08-17-fast-catalog-refresh-design.md
- `NullOverrideService` --implements--> `INexaPlayOverrideService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/INexaPlayOverrideService.cs
- `NullLogService` --implements--> `IAppLogService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/IAppLogService.cs
- `Window` --references--> `MainViewModel`  [INFERRED]
  NexaPlay/MainWindow.xaml → NexaPlay/Presentation/ViewModels/MainViewModel.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]
- **NexaPlay Wide Tile Visual Composition** — nexaplay_assets_wide310x150logo_scale_200_stylized_x_emblem, nexaplay_assets_wide310x150logo_scale_200_rounded_square_frame, nexaplay_assets_wide310x150logo_scale_200_transparent_wide_tile_canvas [EXTRACTED 1.00]

## Communities (153 total, 32 thin omitted)

### Community 0 - "MetadataService"
Cohesion: 0.21
Nodes (9): MetadataWarmupProgress, CancellationToken, HashSet, IProgress, IReadOnlyDictionary, IReadOnlyList, string, Task (+1 more)

### Community 1 - "HomeViewModel"
Cohesion: 0.09
Nodes (24): CancellationToken, Task, ICoverImageCacheService, CancellationToken, IProgress, IReadOnlyList, Task, IMetadataService (+16 more)

### Community 2 - "FixEntry"
Cohesion: 0.15
Nodes (14): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, IReadOnlyList, FixEntry, CancellationToken, Func (+6 more)

### Community 3 - "Window"
Cohesion: 0.06
Nodes (51): AccountHeader, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames, LblHome (+43 more)

### Community 4 - "BypassGamesPage"
Cohesion: 0.19
Nodes (7): Brush, DependencyObject, DispatcherTimer, int, Visibility, BypassGamesPage, Page

### Community 5 - "HomePage"
Cohesion: 0.15
Nodes (7): bool, DependencyObject, DispatcherTimer, ImageSource, int, Visibility, HomePage

### Community 6 - ".Log"
Cohesion: 0.05
Nodes (33): appId, close, found, installdir, keyLine, name, DateTime, InstalledGame (+25 more)

### Community 7 - "NexaPlayOverrideService"
Cohesion: 0.06
Nodes (39): HttpMessageHandler, HttpRequestMessage, CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList (+31 more)

### Community 8 - "IAppLogService"
Cohesion: 0.05
Nodes (25): ConcurrentQueue, IReadOnlyList, Task, IAppLogService, CancellationToken, Task, ILicenseService, LicensePlan (+17 more)

### Community 9 - "SqliteCatalogRepository"
Cohesion: 0.14
Nodes (21): BrowseBlock, BrowseCandidate, CancellationToken, Task, CatalogQuery, CancellationToken, Dictionary, int (+13 more)

### Community 10 - "Application"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "Page"
Cohesion: 0.07
Nodes (29): AboutGameWebView, CompactState, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroTitleText, LeftContentPanel, MediaOverlay (+21 more)

### Community 12 - "GamesViewModel"
Cohesion: 0.09
Nodes (18): DateOnly, Action, bool, CancellationTokenSource, Dictionary, int, IReadOnlyList, ObservableCollection (+10 more)

### Community 13 - "Audit Phase 2 — Cache, Games, Refresh"
Cohesion: 0.05
Nodes (43): 10. Home Popular Current Flow, 11. Home Recent Current Flow, 12. Games SQLite Current Flow, 13. Games Default Ordering, 14. Daily Rotation, 15. Default Price Floor, 16. Price Distribution/Mix, 17. Games Paging (+35 more)

### Community 14 - "Page"
Cohesion: 0.08
Nodes (37): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep (+29 more)

### Community 15 - "SteamStoreService"
Cohesion: 0.08
Nodes (22): HasTransientError, Json, JsonArray, JsonObject, CancellationToken, Task, ISteamStoreService, DateTime (+14 more)

### Community 16 - "GameDetailPage"
Cohesion: 0.08
Nodes (14): double, MediaSource, bool, CancellationTokenSource, Dictionary, int, JsonSerializerOptions, List (+6 more)

### Community 17 - "BypassTutorialVideoService"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 18 - "DynamicFeedRefreshService"
Cohesion: 0.10
Nodes (20): Change, Failed, FeedSpec, CancellationToken, Task, IDynamicFeedRefreshService, DynamicFeedChanges, DynamicFeedKind (+12 more)

### Community 19 - "AddGameService"
Cohesion: 0.14
Nodes (8): BypassProgressState, CancellationToken, ConcurrentDictionary, HttpResponseMessage, IProgress, IReadOnlyList, Task, AddGameService

### Community 20 - "BypassGameDetailViewModel"
Cohesion: 0.10
Nodes (14): Func, HttpClient, int, IReadOnlyList, RelayCommand, string, BypassGameDetailViewModel, CopySteamGuardCodeCommand (+6 more)

### Community 21 - "GamesPage"
Cohesion: 0.11
Nodes (11): IEnumerable, bool, Brush, DependencyObject, DispatcherTimer, int, IReadOnlyList, ItemClickEventArgs (+3 more)

### Community 22 - "NexaPlay.Core.Models"
Cohesion: 0.15
Nodes (4): NexaPlay.Core.Models, NexaPlay.Contracts.Services, NexaPlay.Core.Enums, NavigationItem

### Community 23 - "GameDetailViewModel"
Cohesion: 0.07
Nodes (19): CancellationToken, CancellationTokenSource, Func, HashSet, int, IReadOnlyList, GameDetailViewModel, AddGameCommand (+11 more)

### Community 24 - "LibraryPage"
Cohesion: 0.12
Nodes (14): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, Page, Brush, int, NavigationEventArgs (+6 more)

### Community 25 - "MainWindow"
Cohesion: 0.14
Nodes (13): bool, ContentDialog, int, NavigationEventArgs, Storyboard, string, Task, TaskCompletionSource (+5 more)

### Community 26 - "BypassGameDetailPage"
Cohesion: 0.08
Nodes (11): ContentDialogButtonClickEventArgs, bool, ContentDialog, CoreWebView2WebMessageReceivedEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, SizeChangedEventArgs, Storyboard (+3 more)

### Community 27 - "AppUpdateService"
Cohesion: 0.14
Nodes (14): DateTimeOffset, IReadOnlyList, AppUpdateCheckResult, DateTimeOffset, List, AppUpdateState, CancellationToken, HttpClient (+6 more)

### Community 28 - "BypassGamesViewModel"
Cohesion: 0.13
Nodes (10): BypassStatus, CancellationTokenSource, int, IReadOnlyList, List, ObservableCollection, string, Task (+2 more)

### Community 29 - "AccountReportLimitStore"
Cohesion: 0.24
Nodes (8): CancellationToken, Dictionary, int, SemaphoreSlim, string, Task, AccountReportLimitStore, ReportLimitState

### Community 30 - "What You Must Do When Invoked"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 31 - "Page"
Cohesion: 0.13
Nodes (12): FilterOverlay, FilterOverlayTranslate, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, RoutedEventArgs (+4 more)

### Community 32 - "LibraryViewModel"
Cohesion: 0.12
Nodes (16): CancellationTokenSource, int, IReadOnlyDictionary, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel (+8 more)

### Community 33 - "SettingsPage"
Cohesion: 0.18
Nodes (5): ContentDialog, RoutedEventArgs, Task, Visibility, SettingsPage

### Community 35 - "ScreenshotEntry"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 36 - "Border"
Cohesion: 0.20
Nodes (9): AppTitleBar, LicenseBadge, LicenseStatusBorder, PageTopBar, SidebarShell, StartupOverlay, ValidationErrorBorder, PointerRoutedEventArgs (+1 more)

### Community 37 - "TextBlock"
Cohesion: 0.50
Nodes (4): LibraryRemoveConfirmMessageText, LibraryRemoveResultMessageText, LibraryRemoveResultTitleText, TextBlock

### Community 38 - "Page"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 39 - "NexaPlay.Presentation.ViewModels"
Cohesion: 0.28
Nodes (5): NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 40 - ".OnNavigatedTo"
Cohesion: 0.17
Nodes (4): GamesGrid, NavigationEventArgs, SizeChangedEventArgs, GridView

### Community 41 - "SettingsViewModel"
Cohesion: 0.22
Nodes (5): IReadOnlyList, Task, SettingsViewModel, Get, NavigationEventArgs

### Community 42 - "Authoritative New Games AppID List"
Cohesion: 0.14
Nodes (15): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+7 more)

### Community 43 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.16
Nodes (7): FrameworkElement, Image, HeroImage, MediaOverlayImage, ExceptionRoutedEventArgs, Image, TranslateTransform

### Community 44 - "RoutedEventArgs"
Cohesion: 0.11
Nodes (9): CoverArtImage, HeroImage, OfflineActivationCheckbox, TutorialVideoOverlayCloseBtn, ExceptionRoutedEventArgs, RoutedEventArgs, Button, Image (+1 more)

### Community 45 - "NexaPlay.Core.Constants"
Cohesion: 0.12
Nodes (7): NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Logging, NexaPlay.Infrastructure.Services, NexaPlay.Infrastructure.Platform

### Community 46 - "TelegramAccountReportService"
Cohesion: 0.15
Nodes (10): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, HttpClient, string, Task (+2 more)

### Community 47 - "IAddGameService"
Cohesion: 0.17
Nodes (5): CancellationToken, IProgress, Task, IAddGameService, RemoveGameResult

### Community 48 - ".NavigateTo"
Cohesion: 0.21
Nodes (7): BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, Button, RadioButton, TextBlock

### Community 49 - "Page"
Cohesion: 0.16
Nodes (11): CustomPipsContainer, HeroBypassButtonText, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, RoutedEventArgs, Border (+3 more)

### Community 51 - ".TryCreate"
Cohesion: 0.17
Nodes (6): Uri, ImageUriResolver, Uri, ImageSource, ImageSource, ImageSource

### Community 53 - "INavigationService"
Cohesion: 0.15
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 54 - "ISteamService"
Cohesion: 0.21
Nodes (5): CancellationToken, IReadOnlyList, Task, ISteamService, SteamFinalizeResult

### Community 56 - "NexaPlay.csproj"
Cohesion: 0.17
Nodes (10): net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839), Microsoft.WindowsAppSDK (1.6.241114003) (+2 more)

### Community 58 - "CatalogUpdateService"
Cohesion: 0.12
Nodes (17): CatalogManifest, CatalogPatchEntry, CatalogPatchRow, ICatalogBootstrapService, CancellationToken, HttpClient, IReadOnlyList, JsonElement (+9 more)

### Community 59 - "IAppUpdateService"
Cohesion: 0.42
Nodes (4): CancellationToken, IProgress, Task, IAppUpdateService

### Community 60 - ".ExtractDownloadedArchives"
Cohesion: 0.24
Nodes (5): fileName, meta, FixFile, List, path

### Community 61 - ".RemoveFixAsync"
Cohesion: 0.21
Nodes (4): CancellationToken, IProgress, Task, IOnlineFixService

### Community 62 - ".ViewModel_PropertyChanged"
Cohesion: 0.25
Nodes (4): DenuvoBadge, DenuvoBadgeGlowOverlay, PropertyChangedEventArgs, Border

### Community 63 - "Task"
Cohesion: 0.39
Nodes (3): Action, HttpResponseMessage, Task

### Community 64 - ".ApplyHwndIcon"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 65 - ".TutorialThumbnail_Tapped"
Cohesion: 0.22
Nodes (6): CoreWebView2, CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper, TappedRoutedEventArgs

### Community 66 - "BoolToVisibilityConverter"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 67 - ".MediaCard_Tapped"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 68 - ".OnActivateClicked"
Cohesion: 0.25
Nodes (3): Color, ActivateButton, RoutedEventArgs

### Community 71 - ".OnNavChecked"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 72 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 75 - "GameHub Feature Parity"
Cohesion: 0.50
Nodes (5): NexaPlay Native Remake, GameHub Feature Parity, Enterprise-Clean UI Design Contract, Native Migration Strategy, NexaPlay Project Entrypoint

### Community 76 - ".AboutGameWebView_NavigationCompleted"
Cohesion: 0.40
Nodes (3): CoreWebView2NavigationCompletedEventArgs, CoreWebView2WebMessageReceivedEventArgs, WebView2

### Community 77 - "ICatalogRefreshState"
Cohesion: 0.33
Nodes (3): ICatalogRefreshState, long, CatalogRefreshState

### Community 78 - "SteamGuardService"
Cohesion: 0.40
Nodes (3): Action, Task, SteamGuardService

### Community 79 - ".ShowAsync"
Cohesion: 0.29
Nodes (6): Task, LicenseAccessDialogHelper, Task, TappedRoutedEventArgs, UIElement, XamlRoot

### Community 80 - "RootCardBorder"
Cohesion: 0.50
Nodes (3): RootCardBorder, PointerRoutedEventArgs, Border

### Community 81 - "NexaPlay Wide Tile Logo"
Cohesion: 0.50
Nodes (4): NexaPlay Wide Tile Logo, Rounded Square Frame, Stylized X Emblem, Transparent Wide Tile Canvas

### Community 82 - "CoverImageCacheService"
Cohesion: 0.17
Nodes (11): CancellationToken, ConcurrentDictionary, HttpClient, int, long, SemaphoreSlim, string, Task (+3 more)

### Community 83 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 84 - "Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan, Source Nodes

### Community 85 - "Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?, Source Nodes

### Community 86 - "LibraryList"
Cohesion: 0.50
Nodes (3): LibraryList, ItemClickEventArgs, ListView

### Community 87 - "NexaPlay.SelfCheck.csproj"
Cohesion: 0.50
Nodes (3): net8.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk

### Community 88 - "Minimalist Monochrome Branding"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

### Community 89 - "NexaPlay Square App Logo"
Cohesion: 0.67
Nodes (3): Framed X Mark, NexaPlay Square App Logo, Transparent Monochrome Icon

### Community 90 - "Gray Framed X Symbol"
Cohesion: 0.67
Nodes (3): Gray Framed X Symbol, Store Logo, Unavailable Image Placeholder

### Community 91 - "Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut, Source Nodes

### Community 92 - "Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama, Source Nodes

### Community 112 - "AppConstants"
Cohesion: 0.40
Nodes (3): string, TimeSpan, AppConstants

### Community 113 - ".LoadAsync"
Cohesion: 0.24
Nodes (3): IReadOnlyList, RelayCommand, Task

### Community 114 - "Final Data-Flow Optimization - Actual Result"
Cohesion: 0.40
Nodes (5): Final Data-Flow Optimization - Actual Result, Games Ordering dan Query, Listing dan Detail, Refresh dan Cache, Verification Status

### Community 115 - "Audit Alur Data NexaPlay"
Cohesion: 0.13
Nodes (14): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif, 20. Rencana Migrasi yang Diusulkan (+6 more)

### Community 116 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 117 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 118 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 123 - "ICatalogRepository"
Cohesion: 0.18
Nodes (11): CancellationToken, IReadOnlyDictionary, IReadOnlyList, Task, ICatalogRepository, CatalogPage, CatalogPatch, CatalogPatchGame (+3 more)

### Community 124 - "CatalogManifestRules"
Cohesion: 0.24
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 125 - "Page"
Cohesion: 0.25
Nodes (6): HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, Border, Page

### Community 127 - "CatalogBootstrapService"
Cohesion: 0.36
Nodes (6): bool, CancellationToken, SemaphoreSlim, string, Task, CatalogBootstrapService

### Community 128 - "CatalogLoadCoordinator"
Cohesion: 0.25
Nodes (6): Action, Func, long, SemaphoreSlim, Task, CatalogLoadCoordinator

### Community 129 - "2. Alur Startup"
Cohesion: 0.29
Nodes (7): 2. Alur Startup, Entry point dan service, Gerbang license, Klasifikasi blocking, Pemanasan metadata, Pramuat cover dan halaman, Rantai pemanggilan aktual

### Community 130 - "17. Masalah Performa"
Cohesion: 0.40
Nodes (5): 17. Masalah Performa, KRITIS, RENDAH, SEDANG, TINGGI

### Community 131 - "10. Pengaturan"
Cohesion: 0.50
Nodes (4): 10. Pengaturan, Bersihkan Cache, Bersihkan Data, Muat Games

### Community 132 - "3. Alur Data Home"
Cohesion: 0.50
Nodes (4): 3. Alur Data Home, Field yang benar-benar di-bind oleh Home, Jawaban, Rantai pemanggilan aktif

### Community 133 - "Migration Status - Local SQLite Catalog"
Cohesion: 0.50
Nodes (4): Arsitektur Aktif, Migration Status - Local SQLite Catalog, Source dan Network Classification, Startup Aktual

### Community 134 - "ObservableObject"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 135 - ".CheckAndApplyPatchesAsync"
Cohesion: 0.50
Nodes (3): CancellationToken, Task, ICatalogUpdateService

### Community 136 - "14. Inventaris Model / DTO"
Cohesion: 0.67
Nodes (3): 14. Inventaris Model / DTO, Matriks penggunaan field, Model utama

### Community 137 - "5. Alur Data Pencarian"
Cohesion: 0.67
Nodes (3): 5. Alur Data Pencarian, Pencarian lain, Rantai pemanggilan pencarian Games

### Community 138 - "7. Alur Data Library"
Cohesion: 0.67
Nodes (3): 7. Alur Data Library, Rantai pemuatan, Sumber kebenaran penyimpanan

### Community 139 - "8. Alur Data Bypass"
Cohesion: 0.67
Nodes (3): 8. Alur Data Bypass, Detail Bypass, Source daftar dan rantai pemanggilan

### Community 140 - "NexaPlay.Core.Helpers"
Cohesion: 0.15
Nodes (3): NexaPlay.Core.Helpers, CatalogProtectionResolver, StringExtensions

### Community 142 - ".GamesGrid_SizeChanged"
Cohesion: 0.40
Nodes (3): GamesGrid, SizeChangedEventArgs, GridView

### Community 143 - "IWindowsDefenderService"
Cohesion: 0.15
Nodes (9): IReadOnlyList, Task, IWindowsDefenderService, AntivirusVendor, AntivirusInfo, DefenderExclusionResult, IReadOnlyList, Task (+1 more)

### Community 144 - ".HeroCarousel_SelectionChanged"
Cohesion: 0.40
Nodes (3): HeroCarousel, SelectionChangedEventArgs, FlipView

### Community 145 - "LibraryRemoveConfirmOverlay"
Cohesion: 0.67
Nodes (3): LibraryRemoveConfirmOverlay, LibraryRemoveResultOverlay, Grid

### Community 147 - ".PopularGamesGrid_SizeChanged"
Cohesion: 0.40
Nodes (3): PopularGamesGrid, SizeChangedEventArgs, GridView

### Community 149 - "AppUpdateManifest"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

### Community 152 - ".CollectOrderedAppIds"
Cohesion: 0.53
Nodes (3): ICollection, ISet, JsonElement

## Ambiguous Edges - Review These
- `Gray Framed X Symbol` → `Unavailable Image Placeholder`  [AMBIGUOUS]
  NexaPlay/Assets/StoreLogo.png · relation: conceptually_related_to

## Knowledge Gaps
- **278 isolated node(s):** `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `Microsoft.NET.Sdk`, `Viewbox`, `FontIcon` (+273 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **32 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `MetadataService` (4× useful, score=3.595589344)
- `SettingsViewModel` (3× useful, score=2.697721262)
- `BypassGamesDataService` (3× useful, score=2.696452725)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Gray Framed X Symbol` and `Unavailable Image Placeholder`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `IAppLogService` connect `IAppLogService` to `MetadataService`, `HomeViewModel`, `FixEntry`, `.Log`, `NexaPlayOverrideService`, `IWindowsDefenderService`, `SteamStoreService`, `BypassTutorialVideoService`, `DynamicFeedRefreshService`, `AddGameService`, `BypassGameDetailViewModel`, `GameDetailPage`, `GameDetailViewModel`, `MainWindow`, `BypassGameDetailPage`, `AppUpdateService`, `BypassGamesViewModel`, `LibraryViewModel`, `SettingsViewModel`, `TelegramAccountReportService`, `CatalogUpdateService`, `CoverImageCacheService`, `CatalogBootstrapService`?**
  _High betweenness centrality (0.231) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `MainWindow` to `.ApplyHwndIcon`, `HomeViewModel`, `FixEntry`, `Window`, `Border`, `.OnActivateClicked`, `ObservableObject`, `NexaPlay.Presentation.ViewModels`, `IAppLogService`, `.CheckAndApplyPatchesAsync`, `.OnNavChecked`, `GamesViewModel`, `ICatalogRefreshState`, `.NavigateTo`, `INavigationService`, `ISteamService`, `IAppUpdateService`, `BypassGamesViewModel`?**
  _High betweenness centrality (0.152) - this node is a cross-community bridge._
- **Why does `Window` connect `Window` to `.OnActivateClicked`, `Border`, `ObservableObject`, `.OnNavChecked`, `Application`, `.NavigateTo`, `MainWindow`?**
  _High betweenness centrality (0.108) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _278 weakly-connected nodes found - possible documentation gaps or missing edges._
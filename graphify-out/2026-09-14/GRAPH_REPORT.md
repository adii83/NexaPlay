# Graph Report - NexaPlay  (2026-09-14)

## Corpus Check
- 156 files · ~175,337 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2086 nodes · 4230 edges · 154 communities (120 shown, 34 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 323 edges (avg confidence: 0.76)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3ab44172`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- MetadataService
- HomeViewModel
- BypassGamesViewModel
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
- ListingCoverResolver
- Page
- SteamStoreService
- GameDetailPage
- BypassTutorialVideoService
- .PublishValidatedAsync
- AddGameService
- BypassGameDetailViewModel
- GamesPage
- NexaPlay.Core.Models
- GameDetailViewModel
- LibraryPage
- MainWindow
- BypassGameDetailPage
- AppUpdateService
- NewGamesCatalog
- IWindowsDefenderService
- What You Must Do When Invoked
- Page
- LibraryViewModel
- SettingsPage
- .ApplyFiltersAndPaginationAsync
- ScreenshotEntry
- Border
- Page
- Page
- NexaPlay.Core.Helpers
- OnlineFixService
- SettingsViewModel
- Authoritative New Games AppID List
- .FindSiblingSkeletonOverlay
- OfflineActivationCheckbox
- NexaPlay.Core.Enums
- AccountReportLimitStore
- IAddGameService
- .NavigateTo
- Page
- GameCoverIndexService
- .TryCreate
- RoutedEventArgs
- INavigationService
- ISteamService
- NexaPlay.csproj
- CoverArtImage
- CatalogUpdateService
- IAppUpdateService
- .LoadAsync
- .ViewModel_PropertyChanged
- Program.cs
- .ApplyHwndIcon
- .TutorialThumbnail_Tapped
- BoolToVisibilityConverter
- .MediaCard_Tapped
- App.xaml.cs
- .SelectManualFolderAsync
- CatalogCacheStamp.cs
- .OnNavChecked
- graphify reference: extra exports and benchmark
- PointerRoutedEventArgs
- .ShowUpdateAvailableDialogAsync
- GameHub Feature Parity
- .AboutGameWebView_NavigationCompleted
- IReadOnlyList
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
- CatalogSourceFile
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
- IAppLogService
- CatalogBootstrapService
- CatalogLoadCoordinator
- 2. Alur Startup
- 17. Masalah Performa
- 10. Pengaturan
- 3. Alur Data Home
- Migration Status - Local SQLite Catalog
- ObservableObject
- CatalogModels.cs
- 14. Inventaris Model / DTO
- 5. Alur Data Pencarian
- 7. Alur Data Library
- 8. Alur Data Bypass
- CatalogProtectionResolver.cs
- 18. Arsitektur Target yang Direkomendasikan
- 9. Protection / Premium / Standard
- .Log
- PointerRoutedEventArgs
- .RootLayout_SizeChanged
- RelayCommand
- .PopularGamesGrid_SizeChanged
- .GetAsync
- AppUpdateManifest
- BypassProgressState.cs
- .ShowHeroSkeleton
- .CollectAppIds
- MetadataWarmupProgress

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 86 edges
2. `GameDetailPage` - 83 edges
3. `BypassGameDetailViewModel` - 70 edges
4. `NexaPlay.Core.Models` - 64 edges
5. `MainWindow` - 64 edges
6. `Window` - 60 edges
7. `GamesViewModel` - 59 edges
8. `BypassGameDetailPage` - 56 edges
9. `NexaPlay.Contracts.Services` - 53 edges
10. `HomeViewModel` - 49 edges

## Surprising Connections (you probably didn't know these)
- `NexaPlay Performance Contract` --semantically_similar_to--> `Lazy Rich Detail Flow`  [INFERRED] [semantically similar]
  NexaPlay/AGENTS.md → CLAUDE.md
- `NullLogService` --implements--> `IAppLogService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/IAppLogService.cs
- `Lazy Rich Detail Flow` --shares_data_with--> `Listing Cover Priority`  [INFERRED]
  CLAUDE.md → docs/superpowers/specs/2026-08-17-fast-catalog-refresh-design.md
- `Window` --references--> `MainViewModel`  [INFERRED]
  NexaPlay/MainWindow.xaml → NexaPlay/Presentation/ViewModels/MainViewModel.cs
- `Page` --references--> `BypassGameDetailViewModel`  [INFERRED]
  NexaPlay/Presentation/Views/Pages/BypassGameDetailPage.xaml → NexaPlay/Presentation/ViewModels/BypassGameDetailViewModel.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]
- **NexaPlay Wide Tile Visual Composition** — nexaplay_assets_wide310x150logo_scale_200_stylized_x_emblem, nexaplay_assets_wide310x150logo_scale_200_rounded_square_frame, nexaplay_assets_wide310x150logo_scale_200_transparent_wide_tile_canvas [EXTRACTED 1.00]

## Communities (154 total, 34 thin omitted)

### Community 0 - "MetadataService"
Cohesion: 0.20
Nodes (10): CancellationToken, HashSet, HttpClient, IProgress, IReadOnlyDictionary, IReadOnlyList, SemaphoreSlim, string (+2 more)

### Community 1 - "HomeViewModel"
Cohesion: 0.06
Nodes (34): EventArgs, Message, ICatalogRefreshState, CancellationToken, Task, ICoverImageCacheService, CancellationToken, Task (+26 more)

### Community 2 - "BypassGamesViewModel"
Cohesion: 0.08
Nodes (28): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, CancellationToken, IProgress, Task, IOnlineFixService (+20 more)

### Community 3 - "Window"
Cohesion: 0.06
Nodes (51): AccountHeader, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames, LblHome (+43 more)

### Community 4 - "BypassGamesPage"
Cohesion: 0.12
Nodes (9): Brush, DependencyObject, DispatcherTimer, int, NavigationEventArgs, RoutedEventArgs, SizeChangedEventArgs, Visibility (+1 more)

### Community 5 - "HomePage"
Cohesion: 0.10
Nodes (11): Border, bool, DependencyObject, DispatcherTimer, ImageSource, int, PointerRoutedEventArgs, Visibility (+3 more)

### Community 6 - "SteamPlatformService"
Cohesion: 0.08
Nodes (19): appId, close, found, installdir, keyLine, name, CancellationToken, IReadOnlyList (+11 more)

### Community 7 - "NexaPlayOverrideService"
Cohesion: 0.10
Nodes (24): CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride (+16 more)

### Community 8 - "LicenseInfo"
Cohesion: 0.10
Nodes (14): CancellationToken, Task, ILicenseService, LicensePlan, LicenseStatus, LicenseInfo, string, Task (+6 more)

### Community 9 - "SqliteCatalogRepository"
Cohesion: 0.19
Nodes (13): CancellationToken, Task, ICatalogBootstrapService, CancellationToken, IReadOnlyDictionary, IReadOnlyList, string, Task (+5 more)

### Community 10 - "Application"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "Page"
Cohesion: 0.09
Nodes (27): AboutGameWebView, CompactState, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroTitleText, LeftContentPanel, MediaOverlay (+19 more)

### Community 12 - "GamesViewModel"
Cohesion: 0.10
Nodes (15): Action, bool, CancellationTokenSource, Dictionary, int, ObservableCollection, SemaphoreSlim, GamesViewModel (+7 more)

### Community 13 - "ListingCoverResolver"
Cohesion: 0.10
Nodes (17): CancellationToken, Task, IGameCoverIndexService, CancellationToken, Task, ISteamStoreService, ListingCoverPriority, CancellationToken (+9 more)

### Community 14 - "Page"
Cohesion: 0.08
Nodes (34): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep (+26 more)

### Community 15 - "SteamStoreService"
Cohesion: 0.13
Nodes (15): HasTransientError, Json, JsonArray, JsonObject, DateTime, IReadOnlyList, GameDetailEntry, MovieEntry (+7 more)

### Community 16 - "GameDetailPage"
Cohesion: 0.08
Nodes (14): double, MediaSource, bool, CancellationTokenSource, Dictionary, int, JsonSerializerOptions, List (+6 more)

### Community 17 - "BypassTutorialVideoService"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 18 - ".PublishValidatedAsync"
Cohesion: 0.36
Nodes (4): CancellationToken, JsonElement, Task, GameCoverIndexFile

### Community 19 - "AddGameService"
Cohesion: 0.11
Nodes (10): BypassStatus, BypassProgressState, RemoveGameResult, CancellationToken, ConcurrentDictionary, HttpResponseMessage, IProgress, IReadOnlyList (+2 more)

### Community 20 - "BypassGameDetailViewModel"
Cohesion: 0.06
Nodes (26): error, fileName, meta, FixFile, Action, CancellationToken, Func, HttpClient (+18 more)

### Community 21 - "GamesPage"
Cohesion: 0.09
Nodes (13): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, int, IReadOnlyList, ItemClickEventArgs (+5 more)

### Community 22 - "NexaPlay.Core.Models"
Cohesion: 0.21
Nodes (3): NexaPlay.Core.Models, NexaPlay.Contracts.Services, NavigationItem

### Community 23 - "GameDetailViewModel"
Cohesion: 0.09
Nodes (16): CancellationTokenSource, Func, int, GameDetailViewModel, AddGameCommand, ApplyFixCommand, CancelFixCommand, CheckBypassCommand (+8 more)

### Community 24 - "LibraryPage"
Cohesion: 0.13
Nodes (9): Brush, DependencyObject, int, NavigationEventArgs, PropertyChangedEventArgs, string, Task, Visibility (+1 more)

### Community 25 - "MainWindow"
Cohesion: 0.11
Nodes (15): Color, bool, ContentDialog, int, NavigationEventArgs, RoutedEventArgs, Storyboard, string (+7 more)

### Community 26 - "BypassGameDetailPage"
Cohesion: 0.12
Nodes (9): ShimmerContainer, TutorialVideoOverlayCloseBtn, bool, CoreWebView2WebMessageReceivedEventArgs, RoutedEventArgs, Storyboard, Visibility, BypassGameDetailPage (+1 more)

### Community 27 - "AppUpdateService"
Cohesion: 0.14
Nodes (14): DateTimeOffset, IReadOnlyList, AppUpdateCheckResult, DateTimeOffset, List, AppUpdateState, CancellationToken, HttpClient (+6 more)

### Community 28 - "NewGamesCatalog"
Cohesion: 0.13
Nodes (12): IEnumerable, IReadOnlyDictionary, ISet, JsonElement, JsonSerializerOptions, NewGamesCatalog, NewGamesCatalogEntry, NewGamesRefreshResult (+4 more)

### Community 29 - "IWindowsDefenderService"
Cohesion: 0.39
Nodes (3): IReadOnlyList, Task, IWindowsDefenderService

### Community 30 - "What You Must Do When Invoked"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 31 - "Page"
Cohesion: 0.12
Nodes (14): FilterOverlay, FilterOverlayTranslate, GamesGrid, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs (+6 more)

### Community 32 - "LibraryViewModel"
Cohesion: 0.12
Nodes (16): CancellationTokenSource, int, IReadOnlyDictionary, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel (+8 more)

### Community 33 - "SettingsPage"
Cohesion: 0.21
Nodes (3): RoutedEventArgs, Visibility, SettingsPage

### Community 34 - ".ApplyFiltersAndPaginationAsync"
Cohesion: 0.16
Nodes (4): CancellationToken, IReadOnlyList, RelayCommand, Task

### Community 35 - "ScreenshotEntry"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 36 - "Border"
Cohesion: 0.20
Nodes (9): AppTitleBar, LicenseBadge, LicenseStatusBorder, PageTopBar, SidebarShell, StartupOverlay, ValidationErrorBorder, PointerRoutedEventArgs (+1 more)

### Community 37 - "Page"
Cohesion: 0.12
Nodes (18): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+10 more)

### Community 38 - "Page"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 39 - "NexaPlay.Core.Helpers"
Cohesion: 0.18
Nodes (6): NexaPlay.Core.Helpers, NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 40 - "OnlineFixService"
Cohesion: 0.15
Nodes (12): Dictionary, IReadOnlyDictionary, SemaphoreSlim, string, Task, AppliedStateStore, CancellationToken, HttpClient (+4 more)

### Community 41 - "SettingsViewModel"
Cohesion: 0.23
Nodes (5): IReadOnlyList, Task, SettingsViewModel, Get, NavigationEventArgs

### Community 42 - "Authoritative New Games AppID List"
Cohesion: 0.14
Nodes (15): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+7 more)

### Community 43 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.21
Nodes (6): FrameworkElement, Image, HeroImage, MediaOverlayImage, ExceptionRoutedEventArgs, Image

### Community 45 - "NexaPlay.Core.Enums"
Cohesion: 0.16
Nodes (5): NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Services, NexaPlay.Core.Enums, NexaPlay.Infrastructure.Platform

### Community 46 - "AccountReportLimitStore"
Cohesion: 0.11
Nodes (18): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, Dictionary, int, SemaphoreSlim (+10 more)

### Community 47 - "IAddGameService"
Cohesion: 0.14
Nodes (5): CancellationToken, IProgress, Task, IAddGameService, Task

### Community 48 - ".NavigateTo"
Cohesion: 0.19
Nodes (8): ActivateButton, BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, Button, RadioButton, TextBlock

### Community 49 - "Page"
Cohesion: 0.14
Nodes (13): CustomPipsContainer, HeroBypassButtonText, HeroCarousel, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, RoutedEventArgs (+5 more)

### Community 50 - "GameCoverIndexService"
Cohesion: 0.22
Nodes (8): CancellationToken, Dictionary, HttpClient, JsonElement, SemaphoreSlim, string, Task, GameCoverIndexService

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

### Community 57 - "CoverArtImage"
Cohesion: 0.29
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 58 - "CatalogUpdateService"
Cohesion: 0.13
Nodes (17): CatalogManifest, CatalogPatchEntry, CatalogPatchRow, CatalogPatch, CancellationToken, HttpClient, IReadOnlyList, JsonElement (+9 more)

### Community 59 - "IAppUpdateService"
Cohesion: 0.42
Nodes (4): CancellationToken, IProgress, Task, IAppUpdateService

### Community 62 - ".ViewModel_PropertyChanged"
Cohesion: 0.25
Nodes (4): DenuvoBadge, DenuvoBadgeGlowOverlay, PropertyChangedEventArgs, Border

### Community 63 - "Program.cs"
Cohesion: 0.17
Nodes (11): HttpMessageHandler, HttpRequestMessage, CancellationToken, HttpResponseMessage, IReadOnlyList, List, Task, MapHandler (+3 more)

### Community 64 - ".ApplyHwndIcon"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 65 - ".TutorialThumbnail_Tapped"
Cohesion: 0.33
Nodes (3): CoreWebView2, TutorialThumbnailOverlay, TappedRoutedEventArgs

### Community 66 - "BoolToVisibilityConverter"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 67 - ".MediaCard_Tapped"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 68 - "App.xaml.cs"
Cohesion: 0.29
Nodes (3): NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Infrastructure.Logging

### Community 69 - ".SelectManualFolderAsync"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 70 - "CatalogCacheStamp.cs"
Cohesion: 0.40
Nodes (3): CatalogCacheEnvelope, CatalogCacheStamp, CatalogSourceStamp

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
Cohesion: 0.23
Nodes (9): CancellationToken, ConcurrentDictionary, HttpClient, SemaphoreSlim, string, Task, TimeSpan, Uri (+1 more)

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

### Community 114 - "CatalogSourceFile"
Cohesion: 0.19
Nodes (7): ActivePath, CandidatePath, CancellationToken, IReadOnlyList, JsonElement, Task, CatalogSourceFile

### Community 115 - "Audit Alur Data NexaPlay"
Cohesion: 0.15
Nodes (12): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif, 20. Rencana Migrasi yang Diusulkan (+4 more)

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
Cohesion: 0.28
Nodes (7): CancellationToken, IReadOnlyDictionary, IReadOnlyList, Task, ICatalogRepository, CatalogPage, CatalogRuntimeOverride

### Community 124 - "CatalogManifestRules"
Cohesion: 0.24
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 125 - "Page"
Cohesion: 0.20
Nodes (8): GamesGrid, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, Border, GridView, Page

### Community 126 - "IAppLogService"
Cohesion: 0.12
Nodes (9): IReadOnlyList, Task, IAppLogService, ConcurrentQueue, int, IReadOnlyList, string, Task (+1 more)

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

### Community 135 - "CatalogModels.cs"
Cohesion: 0.15
Nodes (10): CancellationToken, Task, ICatalogUpdateService, CatalogPatchGame, CatalogQuery, CatalogUpdateResult, CatalogUpdateStatus, Dictionary (+2 more)

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

### Community 143 - ".Log"
Cohesion: 0.24
Nodes (6): AntivirusVendor, AntivirusInfo, DefenderExclusionResult, IReadOnlyList, Task, WindowsDefenderService

### Community 147 - ".PopularGamesGrid_SizeChanged"
Cohesion: 0.40
Nodes (3): PopularGamesGrid, SizeChangedEventArgs, GridView

### Community 148 - ".GetAsync"
Cohesion: 0.38
Nodes (4): CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper

### Community 149 - "AppUpdateManifest"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

## Ambiguous Edges - Review These
- `Gray Framed X Symbol` → `Unavailable Image Placeholder`  [AMBIGUOUS]
  NexaPlay/Assets/StoreLogo.png · relation: conceptually_related_to

## Knowledge Gaps
- **231 isolated node(s):** `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `Microsoft.NET.Sdk`, `Viewbox`, `FontIcon` (+226 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **34 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `MetadataService` (4× useful, score=3.777481327) _(code changed — re-verify)_
- `SettingsViewModel` (3× useful, score=2.834192317) _(code changed — re-verify)_
- `BypassGamesDataService` (3× useful, score=2.832859608) _(code changed — re-verify)_
- `GameCoverIndexService` (2× useful, score=1.889570597) _(code changed — re-verify)_

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Gray Framed X Symbol` and `Unavailable Image Placeholder`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `IAppLogService` connect `IAppLogService` to `MetadataService`, `HomeViewModel`, `BypassGamesViewModel`, `SteamPlatformService`, `NexaPlayOverrideService`, `LicenseInfo`, `ListingCoverResolver`, `.Log`, `SteamStoreService`, `BypassTutorialVideoService`, `GameDetailPage`, `AddGameService`, `BypassGameDetailViewModel`, `GameDetailViewModel`, `MainWindow`, `BypassGameDetailPage`, `AppUpdateService`, `LibraryViewModel`, `OnlineFixService`, `SettingsViewModel`, `AccountReportLimitStore`, `GameCoverIndexService`, `CatalogUpdateService`, `Program.cs`, `CoverImageCacheService`, `CatalogBootstrapService`?**
  _High betweenness centrality (0.223) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `MainWindow` to `.ApplyHwndIcon`, `HomeViewModel`, `BypassGamesViewModel`, `Window`, `App.xaml.cs`, `Border`, `ObservableObject`, `CatalogModels.cs`, `LicenseInfo`, `.OnNavChecked`, `GamesViewModel`, `ListingCoverResolver`, `.NavigateTo`, `INavigationService`, `ISteamService`, `IAppUpdateService`, `IAppLogService`?**
  _High betweenness centrality (0.133) - this node is a cross-community bridge._
- **Why does `BypassGameDetailViewModel` connect `BypassGameDetailViewModel` to `HomeViewModel`, `BypassGamesViewModel`, `ObservableObject`, `NexaPlay.Core.Helpers`, `LicenseInfo`, `NexaPlayOverrideService`, `ListingCoverResolver`, `AccountReportLimitStore`, `SteamStoreService`, `Page`, `BypassTutorialVideoService`, `INavigationService`, `ISteamService`, `BypassGameDetailPage`, `IWindowsDefenderService`, `IAppLogService`?**
  _High betweenness centrality (0.116) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _231 weakly-connected nodes found - possible documentation gaps or missing edges._
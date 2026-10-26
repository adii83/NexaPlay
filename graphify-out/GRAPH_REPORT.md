# Graph Report - NexaPlay  (2026-10-26)

## Corpus Check
- 171 files · ~185,908 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2310 nodes · 4471 edges · 177 communities (139 shown, 38 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 304 edges (avg confidence: 0.76)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- NexaPlayOverrideService
- Bypass Games View Model
- MainWindow
- Add Game Payload
- Audit Phase 2 — Cache, Games, Refresh
- Library Page
- Window
- SteamStoreService
- AddGameService
- Bypass Games View Model
- Task Components
- Task Components
- Add Game Verification
- Page
- Page
- Scan Steam Library Async
- .FindSiblingSkeletonOverlay
- Application
- Add Game Payload
- Page
- DynamicFeedRefreshService
- Page
- SteamPlatformService
- GameDetailPage
- GameDetailViewModel
- BypassGamesPage
- BypassTutorialVideoService
- CatalogUpdateService
- GamesPage
- App Update Service
- NexaPlay.Core.Helpers
- Add Game Verification
- .NavigateTo
- ScreenshotEntry
- IWindowsDefenderService
- NexaPlay.Core.Constants
- GamesViewModel
- BypassGameDetailViewModel
- MetadataService
- OnlineFixService
- CoverImageCacheService
- BypassGameDetailPage
- Load Async
- AddGamePayloadBuilder
- .ApplyFiltersAndPaginationAsync
- I Add Game Service
- .PopularGamesGrid_SizeChanged
- Nexa Play Release Packaging
- Load Games Async
- Page
- Local SQLite Catalog
- Page
- INavigationService
- Graphify Workflow
- graphify reference: extra exports and benchmark
- Task Components
- .HandleOnlineFixPrimaryActionAsync
- LicenseInfo
- IAppUpdateService
- WindowsDefenderService
- Audit Alur Data NexaPlay
- SettingsPage
- I Steam Service
- Report Async
- .TryCreate
- On Navigated To
- Report Async
- App Log Service
- Grid
- AccountReportLimitStore
- .GetAsync
- .ShowAsync
- CatalogManifestRules
- App Log Service
- .OnPageUnloaded
- RoutedEventArgs
- Http Client
- HomeViewModel
- .ViewModel_PropertyChanged
- Catalog Bootstrap Service
- .SelectManualFolderAsync
- Catalog Bootstrap Service
- Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?
- On Navigated To
- I Steam Store Service
- Catalog Load Coordinator
- LibraryViewModel
- Atomic Last-Known-Good Publication
- .ExtractDownloadedArchives
- Catalog Load Coordinator
- .ApplyHwndIcon
- On Navigated To
- Add Game Async
- I App Log Service
- 2. Alur Startup
- graphify reference: query, path, explain
- Minimalist Monochrome Branding
- Generate-AppIcon.ps1
- .TutorialThumbnail_Tapped
- Pemetaan frontend Game Detail catatan
- Add Game: sumber API dan ZIP final
- StringExtensions.cs
- SteamGuardService
- GameHub Feature Parity
- .AboutGameWebView_NavigationCompleted
- 17. Masalah Performa
- Final Data-Flow Optimization - Actual Result
- Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan
- Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?
- Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut
- .CollectOrderedAppIds
- Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama
- Q: Analisis pemetaan frontend GamePageDetail dan GET keseluruhan metadata untuk implementasi HTML, JavaScript, dan CSS responsif, tanpa Add Game, Online-Fix, dan Restart Steam.
- NexaPlay Wordmark
- long Components
- I App Log Service
- .GetOrBuildListingCardAsync
- Hero Carousel Selection Changed
- LicenseStore
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- 10. Pengaturan
- 3. Alur Data Home
- Migration Status - Local SQLite Catalog
- Nexa Play Self Check csproj
- Nexa Play Self Check csproj
- Pemetaan frontend Game Detail catatan
- Alur Add Game lama snapshot
- Binding versi depot melalui set
- Verifikasi parity Lightning sebelum revisi
- launch Settings json
- DeviceIdHelper
- Verifikasi parity Lightning sebelum revisi
- ICatalogRefreshState
- Grid Components
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 14. Inventaris Model / DTO
- 5. Alur Data Pencarian
- 7. Alur Data Library
- DeviceIdHelper
- extraction-spec.md
- 18. Arsitektur Target yang Direkomendasikan
- Monochrome Branding
- notifyHost
- Root Split View
- hooks json
- RTK Output Filtering
- Layered Service Architecture
- Small Batch Build Workflow
- Home Page Decision Archive
- Layout-Cycle-Safe WebView Height Feedback
- Time Span
- I Enumerable
- int Components
- long Components
- uint Components
- Concurrent Dictionary
- Http Client
- Json Element
- Action Components
- Dictionary Components
- I Read Only Dictionary
- I Read Only Set
- Task Completion Source
- int Components

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 86 edges
2. `GameDetailPage` - 83 edges
3. `BypassGameDetailViewModel` - 70 edges
4. `NexaPlay.Core.Models` - 64 edges
5. `GamesViewModel` - 62 edges
6. `MainWindow` - 62 edges
7. `Window` - 60 edges
8. `BypassGameDetailPage` - 56 edges
9. `HomeViewModel` - 51 edges
10. `NexaPlay.Contracts.Services` - 50 edges

## Surprising Connections (you probably didn't know these)
- `NexaPlay Performance Contract` --semantically_similar_to--> `Lazy Rich Detail Flow`  [INFERRED] [semantically similar]
  NexaPlay/AGENTS.md → CLAUDE.md
- `Sequential Catalog Patch Transactions` --references--> `CatalogUpdateService`  [EXTRACTED]
  NexaPlay/MIGRATION_PARITY_MATRIX.md → NexaPlay/Infrastructure/Services/CatalogUpdateService.cs
- `Manifest Version Update Detection` --references--> `AppUpdateService`  [EXTRACTED]
  NexaPlay/release/README.md → NexaPlay/Infrastructure/Services/AppUpdateService.cs
- `Lazy Rich Detail Flow` --shares_data_with--> `Listing Cover Priority`  [INFERRED]
  CLAUDE.md → docs/superpowers/specs/2026-08-17-fast-catalog-refresh-design.md
- `Project Graphify Rules` --references--> `Graphify Workflow`  [EXTRACTED]
  AGENTS.md → .codex/skills/graphify/SKILL.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Add Game preparation and installation** — nexaplay_add_game_payload_github_lightning_payload_preparation, nexaplay_add_game_payload_flat_appid_zip_payload, nexaplay_add_game_payload_required_numeric_lua_selection, nexaplay_add_game_payload_byte_preserving_steam_installation, nexaplay_add_game_payload_add_game_staging_cleanup [EXTRACTED 1.00]
- **Signed release publication sequence** — nexaplay_release_readme_release_version_synchronization, nexaplay_release_readme_self_contained_release_publish, nexaplay_release_readme_ordered_build_and_installer_signing, nexaplay_release_readme_nexaplay_setup_installer, nexaplay_release_readme_upload_installer_before_update_manifest, nexaplay_release_readme_final_signed_installer_sha256 [EXTRACTED 1.00]
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]

## Communities (177 total, 38 thin omitted)

### Community 0 - "NexaPlayOverrideService"
Cohesion: 0.09
Nodes (25): CancellationToken, Task, ICoverImageCacheService, CancellationToken, IProgress, IReadOnlyList, Task, IMetadataService (+17 more)

### Community 1 - "Bypass Games View Model"
Cohesion: 0.08
Nodes (23): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, IReadOnlyList, FixEntry, FixFile, CancellationToken (+15 more)

### Community 2 - "MainWindow"
Cohesion: 0.08
Nodes (23): HasTransientError, Json, JsonArray, JsonObject, CancellationToken, Task, ISteamStoreService, DetailHeroSelector (+15 more)

### Community 3 - "Add Game Payload"
Cohesion: 0.06
Nodes (53): Add Game Payload, Add Game Self Check, Add Game Staging Cleanup, Atomic Per File Installation, Automatic Lua Processing Mode, Bounded Archive Validation, Byte Preserving Steam Installation, Flat AppID ZIP Payload (+45 more)

### Community 4 - "Audit Phase 2 — Cache, Games, Refresh"
Cohesion: 0.10
Nodes (24): CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride (+16 more)

### Community 5 - "Library Page"
Cohesion: 0.06
Nodes (51): AccountHeader, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames, LblHome (+43 more)

### Community 6 - "Window"
Cohesion: 0.08
Nodes (21): appId, close, found, installdir, keyLine, name, DateTime, InstalledGame (+13 more)

### Community 7 - "SteamStoreService"
Cohesion: 0.05
Nodes (43): 10. Home Popular Current Flow, 11. Home Recent Current Flow, 12. Games SQLite Current Flow, 13. Games Default Ordering, 14. Daily Rotation, 15. Default Price Floor, 16. Price Distribution/Mix, 17. Games Paging (+35 more)

### Community 8 - "AddGameService"
Cohesion: 0.11
Nodes (15): Color, bool, ContentDialog, int, NavigationEventArgs, RoutedEventArgs, Storyboard, string (+7 more)

### Community 9 - "Bypass Games View Model"
Cohesion: 0.14
Nodes (21): BrowseBlock, BrowseCandidate, CancellationToken, Task, CatalogQuery, CancellationToken, Dictionary, int (+13 more)

### Community 10 - "Task Components"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "Task Components"
Cohesion: 0.09
Nodes (23): Dictionary, HttpMessageHandler, IAppLogService, INexaPlayOverrideService, IReadOnlyDictionary, IReadOnlySet, BlockingHandler, CancellationToken (+15 more)

### Community 12 - "Add Game Verification"
Cohesion: 0.09
Nodes (16): Action, InstalledGame, IProgress, ISteamService, AddGameChecks, CaptureProgress, BypassProgressState, CancellationToken (+8 more)

### Community 13 - "Page"
Cohesion: 0.09
Nodes (20): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+12 more)

### Community 14 - "Page"
Cohesion: 0.11
Nodes (18): CancellationToken, IProgress, Task, IAppUpdateService, DateTimeOffset, IReadOnlyList, AppUpdateCheckResult, DateTimeOffset (+10 more)

### Community 15 - "Scan Steam Library Async"
Cohesion: 0.07
Nodes (29): AboutGameWebView, CompactState, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroTitleText, LeftContentPanel, MediaOverlay (+21 more)

### Community 16 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.09
Nodes (19): Border, CustomPipsContainer, HeroBypassButtonText, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, bool (+11 more)

### Community 17 - "Application"
Cohesion: 0.10
Nodes (21): Change, Failed, FeedSpec, CancellationToken, Task, IDynamicFeedRefreshService, DynamicFeedChanges, DynamicFeedKind (+13 more)

### Community 18 - "Add Game Payload"
Cohesion: 0.08
Nodes (14): double, MediaSource, bool, CancellationTokenSource, Dictionary, int, JsonSerializerOptions, List (+6 more)

### Community 19 - "Page"
Cohesion: 0.10
Nodes (15): ConcurrentDictionary, HttpClient, IAddGameService, JsonElement, BypassProgressState, CancellationToken, IAppLogService, IProgress (+7 more)

### Community 20 - "DynamicFeedRefreshService"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 21 - "Page"
Cohesion: 0.13
Nodes (6): NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Logging, NexaPlay.Infrastructure.Services, NexaPlay.Core.Enums, NexaPlay.Infrastructure.Platform

### Community 22 - "SteamPlatformService"
Cohesion: 0.10
Nodes (21): CatalogManifest, CatalogPatchEntry, CatalogPatchRow, ICatalogBootstrapService, CancellationToken, Task, ICatalogUpdateService, CatalogUpdateResult (+13 more)

### Community 23 - "GameDetailPage"
Cohesion: 0.10
Nodes (13): Func, HttpClient, int, IReadOnlyList, string, BypassGameDetailViewModel, CopySteamGuardCodeCommand, CopySteamPasswordCommand (+5 more)

### Community 24 - "GameDetailViewModel"
Cohesion: 0.10
Nodes (28): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep, HeroShimmerTranslate (+20 more)

### Community 25 - "BypassGamesPage"
Cohesion: 0.16
Nodes (8): NexaPlay.Core.Helpers, NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 26 - "BypassTutorialVideoService"
Cohesion: 0.09
Nodes (16): CancellationTokenSource, Func, int, GameDetailViewModel, AddGameCommand, ApplyFixCommand, CancelFixCommand, CheckBypassCommand (+8 more)

### Community 27 - "CatalogUpdateService"
Cohesion: 0.09
Nodes (18): DateOnly, Action, bool, CancellationTokenSource, Dictionary, int, IReadOnlyList, ObservableCollection (+10 more)

### Community 28 - "GamesPage"
Cohesion: 0.12
Nodes (14): char, IEnumerable, int, long, MemberMetadata, CancellationToken, string, ZipArchive (+6 more)

### Community 29 - "App Update Service"
Cohesion: 0.21
Nodes (9): MetadataWarmupProgress, CancellationToken, HashSet, IProgress, IReadOnlyDictionary, IReadOnlyList, string, Task (+1 more)

### Community 30 - "NexaPlay.Core.Helpers"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 31 - "Add Game Verification"
Cohesion: 0.15
Nodes (12): Dictionary, IReadOnlyDictionary, SemaphoreSlim, string, Task, AppliedStateStore, CancellationToken, HttpClient (+4 more)

### Community 32 - ".NavigateTo"
Cohesion: 0.11
Nodes (10): Brush, DependencyObject, DispatcherTimer, int, NavigationEventArgs, RoutedEventArgs, SizeChangedEventArgs, Visibility (+2 more)

### Community 33 - "ScreenshotEntry"
Cohesion: 0.12
Nodes (18): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+10 more)

### Community 34 - "IWindowsDefenderService"
Cohesion: 0.15
Nodes (7): Message, IReadOnlyList, Success, Task, SettingsViewModel, Get, NavigationEventArgs

### Community 35 - "NexaPlay.Core.Constants"
Cohesion: 0.12
Nodes (8): ShimmerContainer, bool, CoreWebView2WebMessageReceivedEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, Storyboard, Visibility, BypassGameDetailPage

### Community 36 - "GamesViewModel"
Cohesion: 0.11
Nodes (11): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, int, IReadOnlyList, ItemClickEventArgs (+3 more)

### Community 37 - "BypassGameDetailViewModel"
Cohesion: 0.14
Nodes (15): IAccountReportService, CancellationToken, Dictionary, int, SemaphoreSlim, string, Task, AccountReportLimitStore (+7 more)

### Community 38 - "MetadataService"
Cohesion: 0.17
Nodes (11): CancellationToken, ConcurrentDictionary, HttpClient, int, long, SemaphoreSlim, string, Task (+3 more)

### Community 39 - "OnlineFixService"
Cohesion: 0.12
Nodes (16): CancellationTokenSource, int, IReadOnlyDictionary, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel (+8 more)

### Community 40 - "CoverImageCacheService"
Cohesion: 0.13
Nodes (12): FilterOverlay, FilterOverlayTranslate, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, RoutedEventArgs (+4 more)

### Community 41 - "BypassGameDetailPage"
Cohesion: 0.13
Nodes (9): Brush, DependencyObject, int, NavigationEventArgs, PropertyChangedEventArgs, string, Task, Visibility (+1 more)

### Community 42 - "Load Async"
Cohesion: 0.18
Nodes (4): IReadOnlyList, RemoveGameResult, RelayCommand, Task

### Community 43 - "AddGamePayloadBuilder"
Cohesion: 0.20
Nodes (3): NexaPlay.Core.Models, NexaPlay.Contracts.Services, NavigationItem

### Community 44 - ".ApplyFiltersAndPaginationAsync"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 45 - "I Add Game Service"
Cohesion: 0.19
Nodes (10): CancellationToken, IReadOnlyDictionary, IReadOnlyList, Task, ICatalogRepository, CatalogPage, CatalogPatch, CatalogPatchGame (+2 more)

### Community 46 - ".PopularGamesGrid_SizeChanged"
Cohesion: 0.14
Nodes (5): OfflineActivationCheckbox, TutorialVideoOverlayCloseBtn, RoutedEventArgs, Button, CheckBox

### Community 47 - "Nexa Play Release Packaging"
Cohesion: 0.18
Nodes (15): string, AppConstants, Final Signed Installer SHA256, Illustrative Release Versions, Manifest Version Update Detection, NexaPlay Release Packaging, NexaPlay Setup Installer, Ordered Build And Installer Signing (+7 more)

### Community 49 - "Page"
Cohesion: 0.13
Nodes (14): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif, 20. Rencana Migrasi yang Diusulkan (+6 more)

### Community 50 - "Local SQLite Catalog"
Cohesion: 0.16
Nodes (7): FrameworkElement, Image, HeroImage, MediaOverlayImage, ExceptionRoutedEventArgs, Image, TranslateTransform

### Community 51 - "Page"
Cohesion: 0.38
Nodes (6): LicensePlan, LicenseStatus, LicenseInfo, CancellationToken, Task, LicenseService

### Community 52 - "INavigationService"
Cohesion: 0.13
Nodes (7): Uri, ImageUriResolver, Uri, ImageSource, ImageSource, ImageSource, ImageSource

### Community 53 - "Graphify Workflow"
Cohesion: 0.29
Nodes (6): Task, LicenseAccessDialogHelper, Task, TappedRoutedEventArgs, UIElement, XamlRoot

### Community 54 - "graphify reference: extra exports and benchmark"
Cohesion: 0.21
Nodes (5): Action, CancellationToken, HttpResponseMessage, Task, NavigationEventArgs

### Community 56 - ".HandleOnlineFixPrimaryActionAsync"
Cohesion: 0.29
Nodes (5): AntivirusVendor, AntivirusInfo, IReadOnlyList, Task, WindowsDefenderService

### Community 57 - "LicenseInfo"
Cohesion: 0.18
Nodes (9): AppTitleBar, LicenseBadge, LicenseStatusBorder, PageTopBar, SidebarShell, StartupOverlay, ValidationErrorBorder, PointerRoutedEventArgs (+1 more)

### Community 59 - "WindowsDefenderService"
Cohesion: 0.18
Nodes (12): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+4 more)

### Community 60 - "Audit Alur Data NexaPlay"
Cohesion: 0.17
Nodes (10): net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839), Microsoft.WindowsAppSDK (1.6.241114003) (+2 more)

### Community 61 - "SettingsPage"
Cohesion: 0.18
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 62 - "I Steam Service"
Cohesion: 0.21
Nodes (5): CancellationToken, IReadOnlyList, Task, ISteamService, SteamFinalizeResult

### Community 63 - "Report Async"
Cohesion: 0.29
Nodes (4): string, Task, LicenseStore, StoredLicense

### Community 64 - ".TryCreate"
Cohesion: 0.21
Nodes (8): ActivateButton, BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, Button, RadioButton, TextBlock

### Community 66 - "On Navigated To"
Cohesion: 0.17
Nodes (4): GamesGrid, NavigationEventArgs, SizeChangedEventArgs, GridView

### Community 67 - "Report Async"
Cohesion: 0.18
Nodes (4): CancellationToken, Task, AccountReportResult, RelayCommand

### Community 68 - "App Log Service"
Cohesion: 0.29
Nodes (4): IReadOnlyList, Task, IWindowsDefenderService, DefenderExclusionResult

### Community 69 - "Grid"
Cohesion: 0.24
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 70 - "AccountReportLimitStore"
Cohesion: 0.22
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 71 - ".GetAsync"
Cohesion: 0.18
Nodes (8): DetailLayoutGrid, HeroSkeletonOverlay, RootLayout, TopIconSkeletonOverlay, TutorialVideoContainer, TutorialVideoOverlayRoot, SizeChangedEventArgs, Grid

### Community 72 - ".ShowAsync"
Cohesion: 0.20
Nodes (8): GamesGrid, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, Border, GridView, Page

### Community 73 - "CatalogManifestRules"
Cohesion: 0.20
Nodes (10): Project Graphify Rules, Graphify Add and Watch, Graphify Export Formats, Graphify Semantic Extraction Spec, Graphify GitHub and Merge Flow, Graphify Hook Integration, Graphify Query Traversal, Graphify Media Transcription (+2 more)

### Community 74 - "App Log Service"
Cohesion: 0.20
Nodes (6): ConcurrentQueue, int, IReadOnlyList, string, Task, AppLogService

### Community 75 - ".OnPageUnloaded"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 76 - "RoutedEventArgs"
Cohesion: 0.20
Nodes (10): Incremental Catalog Patches, Lazy R2 Game Metadata, Local SQLite Catalog, NexaPlay Data Flow Audit, NexaPlay Data Sources Query, New Games Download Timing, Load Games Download Cost, ETag Catalog Refresh (+2 more)

### Community 78 - "HomeViewModel"
Cohesion: 0.36
Nodes (3): CancellationToken, Task, ILicenseService

### Community 80 - "Catalog Bootstrap Service"
Cohesion: 0.36
Nodes (6): bool, CancellationToken, SemaphoreSlim, string, Task, CatalogBootstrapService

### Community 81 - ".SelectManualFolderAsync"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 82 - "Catalog Bootstrap Service"
Cohesion: 0.22
Nodes (9): NexaPlay Gold Lightning Logo (logo.png), NexaPlay Gold Lightning Logo (LockScreenLogo.scale-200.png), NexaPlay Gold Lightning Logo (logo.svg), NexaPlay Gold Lightning Logo (SplashScreen.scale-200.png), NexaPlay Gold Lightning Logo (Square150x150Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.targetsize-24_altform-unplated.png), NexaPlay Gold Lightning Logo (StoreLogo.png) (+1 more)

### Community 83 - "Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?"
Cohesion: 0.25
Nodes (4): DenuvoBadge, DenuvoBadgeGlowOverlay, PropertyChangedEventArgs, Border

### Community 84 - "On Navigated To"
Cohesion: 0.32
Nodes (4): fileName, meta, List, path

### Community 85 - "I Steam Store Service"
Cohesion: 0.43
Nodes (4): CancellationToken, IProgress, Task, IOnlineFixService

### Community 86 - "Catalog Load Coordinator"
Cohesion: 0.25
Nodes (6): Action, Func, long, SemaphoreSlim, Task, CatalogLoadCoordinator

### Community 87 - "LibraryViewModel"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 88 - "Atomic Last-Known-Good Publication"
Cohesion: 0.38
Nodes (4): CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper

### Community 89 - ".ExtractDownloadedArchives"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 90 - "Catalog Load Coordinator"
Cohesion: 0.29
Nodes (7): 2. Alur Startup, Entry point dan service, Gerbang license, Klasifikasi blocking, Pemanasan metadata, Pramuat cover dan halaman, Rantai pemanggilan aktual

### Community 91 - ".ApplyHwndIcon"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 93 - "Add Game Async"
Cohesion: 0.29
Nodes (5): CancellationToken, IProgress, Task, BypassStatus, BypassProgressState

### Community 94 - "I App Log Service"
Cohesion: 0.29
Nodes (3): IReadOnlyList, Task, IAppLogService

### Community 95 - "2. Alur Startup"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 96 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 97 - "Minimalist Monochrome Branding"
Cohesion: 0.33
Nodes (3): CoreWebView2, TutorialThumbnailOverlay, TappedRoutedEventArgs

### Community 98 - "Generate-AppIcon.ps1"
Cohesion: 0.53
Nodes (3): ICollection, ISet, JsonElement

### Community 99 - ".TutorialThumbnail_Tapped"
Cohesion: 0.33
Nodes (5): Add Game: sumber API dan ZIP final, Pemasangan, Persiapan khusus lima sumber GitHub, Urutan sumber, Verifikasi

### Community 102 - "Add Game: sumber API dan ZIP final"
Cohesion: 0.40
Nodes (3): Action, Task, SteamGuardService

### Community 103 - "StringExtensions.cs"
Cohesion: 0.50
Nodes (5): NexaPlay Native Remake, GameHub Feature Parity, Enterprise-Clean UI Design Contract, Native Migration Strategy, NexaPlay Project Entrypoint

### Community 104 - "SteamGuardService"
Cohesion: 0.40
Nodes (3): CoreWebView2NavigationCompletedEventArgs, CoreWebView2WebMessageReceivedEventArgs, WebView2

### Community 105 - "GameHub Feature Parity"
Cohesion: 0.40
Nodes (5): 17. Masalah Performa, KRITIS, RENDAH, SEDANG, TINGGI

### Community 106 - ".AboutGameWebView_NavigationCompleted"
Cohesion: 0.40
Nodes (5): Final Data-Flow Optimization - Actual Result, Games Ordering dan Query, Listing dan Detail, Refresh dan Cache, Verification Status

### Community 107 - "17. Masalah Performa"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan, Source Nodes

### Community 108 - "Final Data-Flow Optimization - Actual Result"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?, Source Nodes

### Community 109 - "Q: saya ingin anda mengumpulkan semua metadata atau data dari apps nexapkay ini asalnya dari mana dan kelompokkan dengan baik secara jelas dan keseluruhan"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut, Source Nodes

### Community 110 - "Q: pas ngunduh data apakah dia mengunduh juga file data ini new_games.json atau file tersebut hanya diunduh saat load game saja yang berada di setting?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama, Source Nodes

### Community 111 - "Q: tombol load games ini mengunduh apa saja karena sangat lama dan kenapa mengunduh itu cari tau direadme atau di doc project apakah ada yang memberikan info tersebut"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: NEXAPLAY Games Browse artwork eligibility filter, Source Nodes

### Community 112 - ".CollectOrderedAppIds"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Analisis pemetaan frontend GamePageDetail dan GET keseluruhan metadata untuk implementasi HTML, JavaScript, dan CSS responsif, tanpa Add Game, Online-Fix, dan Restart Steam., Source Nodes

### Community 113 - "Q: file ini harus terbaru apakah memakai etag seperti new games dan bagaimana penerapan terbaik agar tidak lama"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Analisa alur masing-masing API di api.json mendapatkan appid.zip dan pemrosesan manifest serta Lua sebelum dipindahkan, Source Nodes

### Community 114 - "Q: Analisis pemetaan frontend GamePageDetail dan GET keseluruhan metadata untuk implementasi HTML, JavaScript, dan CSS responsif, tanpa Add Game, Online-Fix, dan Restart Steam."
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?, Source Nodes

### Community 115 - "NexaPlay Wordmark"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Apakah perilaku lima API terutama SPIN0ZAi identik dengan lampiran Lightning terbaru?, Source Nodes

### Community 118 - ".GetOrBuildListingCardAsync"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 119 - "Hero Carousel Selection Changed"
Cohesion: 0.40
Nodes (3): HeroCarousel, SelectionChangedEventArgs, FlipView

### Community 120 - "LicenseStore"
Cohesion: 0.40
Nodes (3): PopularGamesGrid, SizeChangedEventArgs, GridView

### Community 121 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): RootCardBorder, PointerRoutedEventArgs, Border

### Community 122 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 123 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 124 - "10. Pengaturan"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 125 - "3. Alur Data Home"
Cohesion: 0.50
Nodes (4): 10. Pengaturan, Bersihkan Cache, Bersihkan Data, Muat Games

### Community 126 - "Migration Status - Local SQLite Catalog"
Cohesion: 0.50
Nodes (4): 3. Alur Data Home, Field yang benar-benar di-bind oleh Home, Jawaban, Rantai pemanggilan aktif

### Community 127 - "Nexa Play Self Check csproj"
Cohesion: 0.50
Nodes (4): Arsitektur Aktif, Migration Status - Local SQLite Catalog, Source dan Network Classification, Startup Aktual

### Community 128 - "Nexa Play Self Check csproj"
Cohesion: 0.50
Nodes (3): net8.0, Microsoft.Data.Sqlite (8.0.20), Microsoft.NET.Sdk

### Community 129 - "Pemetaan frontend Game Detail catatan"
Cohesion: 0.67
Nodes (4): Cache detail R2 tujuh hari, PEMETAAN_FRONTEND_GAME_PAGE_DETAIL.md, GameDetailPage frontend mapping, Pemetaan frontend GameDetail - catatan 2026-09-16

### Community 130 - "Alur Add Game lama snapshot"
Cohesion: 0.50
Nodes (4): Fallback lama berhenti sesudah unduhan, Rantai lima API lama sebelum enam sumber baru, Pengeditan Lua lama yang sudah dihapus, Alur Add Game lama - snapshot sebelum perubahan

### Community 131 - "Binding versi depot melalui set"
Cohesion: 0.83
Nodes (4): Binding versi depot melalui setManifestid, Analisis historis tujuan setManifestid, Skrip no-internet-fix memasang binding kembali, Dugaan alasan awal mengomentari setManifestid

### Community 132 - "Verifikasi parity Lightning sebelum revisi"
Cohesion: 0.83
Nodes (4): Verifikasi parity Lightning - sebelum revisi footer, Lua numerik wajib untuk pemasangan NexaPlay, 16 payload cocok sebelum revisi footer, Parity filter SPIN0ZAi dengan include_manifests=True

### Community 133 - "launch Settings json"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

### Community 135 - "Verifikasi parity Lightning sebelum revisi"
Cohesion: 0.50
Nodes (3): LibraryList, ItemClickEventArgs, ListView

### Community 138 - "graphify reference: GitHub clone and cross-repo merge"
Cohesion: 0.67
Nodes (3): 14. Inventaris Model / DTO, Matriks penggunaan field, Model utama

### Community 139 - "graphify reference: transcribe video and audio"
Cohesion: 0.67
Nodes (3): 5. Alur Data Pencarian, Pencarian lain, Rantai pemanggilan pencarian Games

### Community 140 - "14. Inventaris Model / DTO"
Cohesion: 0.67
Nodes (3): 7. Alur Data Library, Rantai pemuatan, Sumber kebenaran penyimpanan

### Community 141 - "5. Alur Data Pencarian"
Cohesion: 0.67
Nodes (3): 8. Alur Data Bypass, Detail Bypass, Source daftar dan rantai pemanggilan

### Community 142 - "7. Alur Data Library"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

## Knowledge Gaps
- **322 isolated node(s):** `BrowseBlock`, `BrowseCandidate`, `OnlineFixDialogMode`, `FeedSpec`, `WebViewHeightPayload` (+317 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **38 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IAppLogService` connect `I App Log Service` to `NexaPlayOverrideService`, `Bypass Games View Model`, `MainWindow`, `Audit Phase 2 — Cache, Games, Refresh`, `Window`, `AddGameService`, `Page`, `Application`, `Add Game Payload`, `DynamicFeedRefreshService`, `SteamPlatformService`, `GameDetailPage`, `BypassTutorialVideoService`, `App Update Service`, `Add Game Verification`, `IWindowsDefenderService`, `NexaPlay.Core.Constants`, `BypassGameDetailViewModel`, `MetadataService`, `OnlineFixService`, `Page`, `.HandleOnlineFixPrimaryActionAsync`, `App Log Service`, `Catalog Bootstrap Service`?**
  _High betweenness centrality (0.191) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `AddGameService` to `NexaPlayOverrideService`, `Bypass Games View Model`, `.TryCreate`, `Library Page`, `Page`, `DeviceIdHelper`, `HomeViewModel`, `2. Alur Startup`, `SteamPlatformService`, `LibraryViewModel`, `I Steam Service`, `BypassGamesPage`, `.GetOrBuildListingCardAsync`, `CatalogUpdateService`, `SettingsPage`, `I App Log Service`, `LicenseInfo`?**
  _High betweenness centrality (0.122) - this node is a cross-community bridge._
- **Why does `GameDetailPage` connect `Add Game Payload` to `.NavigateTo`, `.ExtractDownloadedArchives`, `IAppUpdateService`, `SteamGuardService`, `.ApplyFiltersAndPaginationAsync`, `Http Client`, `Scan Steam Library Async`, `Local SQLite Catalog`, `Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?`, `Graphify Workflow`, `Task Components`, `BypassGamesPage`, `BypassTutorialVideoService`, `.ApplyHwndIcon`, `I App Log Service`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `GamesViewModel` (e.g. with `ClearFiltersCommand` and `Get`) actually correct?**
  _`GamesViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `BrowseBlock`, `BrowseCandidate`, `OnlineFixDialogMode` to the rest of the system?**
  _322 weakly-connected nodes found - possible documentation gaps or missing edges._
## Incremental Update Audit (2026-10-26)

- Refreshed 18 changed/new files: 10 code/config and 8 documents/memories.
- Local AST and JSON configuration indexing; document extraction by host agents. No additional external API calls.
- Token accounting: exact host-agent input/output usage was not exposed by collaboration tools. Zero token placeholders elsewhere in this report are unavailable usage, not zero session cost.
- Historical memories retain captured-time markers. Current behavior is documented in ADD_GAME_PAYLOAD.md: six additional sources, five GitHub payload modes, active setManifestid, optional manifests, and NexaPlay ID footer.
- Sensitive file skipped: NexaPlay/release/nexaplay.pfx.
- Graph health note: one pre-existing AST self-reference in BypassTutorialVideo.cs remains; no dangling or missing endpoints were found.

```text
[graphify] MultiDiGraph edge-collapse diagnostic
input: <in-memory>
input_stage: provided JSON (normal graph.json is post-build)
effective_directed: <direct-call>
nodes: 2310
unverified_code_nodes: 0
raw_edges: 4471
valid_candidate_edges: 4471
missing_endpoint_edges: 0
dangling_endpoint_edges: 0
self_loop_edges: 1
exact_duplicate_edges: 0
directed_unique_endpoint_pairs: 4471
directed_same_endpoint_collapsed_edges: 0
undirected_unique_endpoint_pairs: 4471
undirected_same_endpoint_collapsed_edges: 0
same_endpoint_group_count: 0
relation_variant_groups: 0
source_file_variant_groups: 0
source_location_variant_groups: 0
context_variant_groups: 0
post_build_graph_type: Graph
post_build_edges: 4471
producer_suppression_sites: 12
producer_suppression_examples:
  - L1154 seen_ids arity=unknown
  - L1420 seen_ids arity=unknown
  - L1422 seen_doc_refs arity=unknown
  - L1782 seen_ids arity=unknown
  - L1929 seen_ids arity=unknown
  - L2362 seen_keys arity=unknown
  - L2531 seen_keys arity=unknown
  - L3937 seen_ids arity=unknown
note: normal graph.json is post-build; raw producer loss must be measured earlier.
```

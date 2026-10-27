# Graph Report - NexaPlay  (2026-10-27)

## Corpus Check
- 166 files · ~192,791 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2446 nodes · 4887 edges · 177 communities (148 shown, 29 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 334 edges (avg confidence: 0.77)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d76c8415`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- HomeViewModel
- BypassGamesViewModel
- SteamStoreService
- Add Game Payload
- NexaPlayOverrideService
- Window
- SteamPlatformService
- SteamStoreService
- MainWindow
- Bypass Games View Model
- Task Components
- OnlineFixFileTransaction
- AddGameChecks
- SettingsPage
- AppUpdateService
- Page
- Page
- Application
- GameDetailPage
- AddGameService
- DynamicFeedRefreshService
- NexaPlay.Core.Constants
- SteamPlatformService
- BypassGameDetailViewModel
- Page
- NexaPlay.Core.Helpers
- GameDetailViewModel
- GamesViewModel
- AddGamePayloadBuilder
- App Update Service
- NexaPlay.Core.Helpers
- AppliedStateStore
- BypassGamesPage
- Page
- SettingsViewModel
- BypassGameDetailPage
- GamesPage
- AccountReportLimitStore
- CoverImageCacheService
- OnlineFixService
- Page
- LibraryPage
- Load Async
- NexaPlay.Core.Models
- .ApplyFiltersAndPaginationAsync
- I Add Game Service
- .PopularGamesGrid_SizeChanged
- OnlineFixChecks
- Load Games Async
- Page
- .FindSiblingSkeletonOverlay
- LicenseInfo
- .TryCreate
- Graphify Workflow
- FixEntry
- Task
- .Log
- .NavigateTo
- OnlineFixCatalogService
- WindowsDefenderService
- NexaPlay.csproj
- SettingsPage
- ISteamService
- .RunAsync
- .OnActivateClicked
- .ApplyFixAsync
- BypassGamesDataService
- Page
- IWindowsDefenderService
- Grid
- AccountReportLimitStore
- OnlineFixArchiveDownloader
- Page
- CatalogManifestRules
- IAppLogService
- FakeSteam
- RoutedEventArgs
- IAddGameService
- HomePage
- FakeSteam
- Catalog Bootstrap Service
- .SelectManualFolderAsync
- Catalog Bootstrap Service
- Q: Apa tujuan mengomentari setManifestid dan apakah ada dokumentasinya?
- .ExtractDownloadedArchives
- IOnlineFixService
- Catalog Load Coordinator
- LibraryViewModel
- .TutorialThumbnail_Tapped
- .ExtractDownloadedArchives
- Catalog Load Coordinator
- .ApplyHwndIcon
- OnlineFixModels.cs
- BypassProgressState
- .GetLibraryPaths
- 2. Alur Startup
- graphify reference: query, path, explain
- NexaPlay.Infrastructure.Persistence
- Generate-AppIcon.ps1
- .TutorialThumbnail_Tapped
- .LoadGamesAsync
- CatalogProtectionResolver.cs
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
- NexaPlay.SelfCheck.csproj
- Pemetaan frontend Game Detail catatan
- Alur Add Game lama snapshot
- Binding versi depot melalui set
- Verifikasi parity Lightning sebelum revisi
- IAppUpdateService
- NexaPlay.Core.Enums
- Verifikasi parity Lightning sebelum revisi
- ICatalogRefreshState
- Grid Components
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 14. Inventaris Model / DTO
- 5. Alur Data Pencarian
- 7. Alur Data Library
- ManagedManifestStore
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
- .LoadAsync
- PointerRoutedEventArgs
- Online-Fix — current implementation (2026-10-27)
- RootLayout
- PointerRoutedEventArgs
- ISteamStoreService
- FakeClock
- Q: tolong analisa alur online fix sekarang
- .InstallLuaFromZip
- Grid
- Journal
- StringExtensions.cs
- StartupLoadingLogoRotation
- int Components

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 91 edges
2. `GameDetailPage` - 83 edges
3. `NexaPlay.Core.Models` - 72 edges
4. `BypassGameDetailViewModel` - 70 edges
5. `MainWindow` - 64 edges
6. `GamesViewModel` - 62 edges
7. `Window` - 60 edges
8. `NexaPlay.Contracts.Services` - 56 edges
9. `BypassGameDetailPage` - 56 edges
10. `HomeViewModel` - 51 edges

## Surprising Connections (you probably didn't know these)
- `NexaPlay Performance Contract` --semantically_similar_to--> `Lazy Rich Detail Flow`  [INFERRED] [semantically similar]
  NexaPlay/AGENTS.md → CLAUDE.md
- `FakeSteam` --implements--> `ISteamService`  [EXTRACTED]
  NexaPlay.SelfCheck/AddGameChecks.cs → NexaPlay/Contracts/Services/ISteamService.cs
- `FakeCatalog` --implements--> `IOnlineFixCatalogService`  [EXTRACTED]
  NexaPlay.SelfCheck/OnlineFixChecks.cs → NexaPlay/Contracts/Services/IOnlineFixCatalogService.cs
- `FakeSteam` --implements--> `ISteamService`  [EXTRACTED]
  NexaPlay.SelfCheck/OnlineFixChecks.cs → NexaPlay/Contracts/Services/ISteamService.cs
- `NullLogService` --implements--> `IAppLogService`  [EXTRACTED]
  NexaPlay.SelfCheck/Program.cs → NexaPlay/Contracts/Services/IAppLogService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Add Game preparation and installation** — nexaplay_add_game_payload_github_lightning_payload_preparation, nexaplay_add_game_payload_flat_appid_zip_payload, nexaplay_add_game_payload_required_numeric_lua_selection, nexaplay_add_game_payload_byte_preserving_steam_installation, nexaplay_add_game_payload_add_game_staging_cleanup [EXTRACTED 1.00]
- **Signed release publication sequence** — nexaplay_release_readme_release_version_synchronization, nexaplay_release_readme_self_contained_release_publish, nexaplay_release_readme_ordered_build_and_installer_signing, nexaplay_release_readme_nexaplay_setup_installer, nexaplay_release_readme_upload_installer_before_update_manifest, nexaplay_release_readme_final_signed_installer_sha256 [EXTRACTED 1.00]
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]

## Communities (177 total, 29 thin omitted)

### Community 0 - "HomeViewModel"
Cohesion: 0.08
Nodes (27): ICatalogRefreshState, CancellationToken, Task, ICoverImageCacheService, CancellationToken, IProgress, IReadOnlyList, Task (+19 more)

### Community 1 - "BypassGamesViewModel"
Cohesion: 0.13
Nodes (10): BypassStatus, CancellationTokenSource, int, IReadOnlyList, List, ObservableCollection, string, Task (+2 more)

### Community 2 - "SteamStoreService"
Cohesion: 0.09
Nodes (20): HasTransientError, Json, JsonArray, JsonObject, DetailHeroSelector, DateTime, IReadOnlyList, GameDetailEntry (+12 more)

### Community 3 - "Add Game Payload"
Cohesion: 0.06
Nodes (53): Add Game Payload, Add Game Self Check, Add Game Staging Cleanup, Atomic Per File Installation, Automatic Lua Processing Mode, Bounded Archive Validation, Byte Preserving Steam Installation, Flat AppID ZIP Payload (+45 more)

### Community 4 - "NexaPlayOverrideService"
Cohesion: 0.05
Nodes (43): HttpMessageHandler, CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList, NexaPlayCatalogOverride (+35 more)

### Community 5 - "Window"
Cohesion: 0.06
Nodes (51): AccountHeader, AppTitleBar, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames (+43 more)

### Community 6 - "SteamPlatformService"
Cohesion: 0.15
Nodes (9): close, found, keyLine, CancellationToken, List, Task, SteamPlatformService, open (+1 more)

### Community 7 - "SteamStoreService"
Cohesion: 0.05
Nodes (43): 10. Home Popular Current Flow, 11. Home Recent Current Flow, 12. Games SQLite Current Flow, 13. Games Default Ordering, 14. Daily Rotation, 15. Default Price Floor, 16. Price Distribution/Mix, 17. Games Paging (+35 more)

### Community 8 - "MainWindow"
Cohesion: 0.13
Nodes (13): bool, ContentDialog, int, NavigationEventArgs, Storyboard, string, Task, TaskCompletionSource (+5 more)

### Community 9 - "Bypass Games View Model"
Cohesion: 0.14
Nodes (21): BrowseBlock, BrowseCandidate, CancellationToken, Task, CatalogQuery, CancellationToken, Dictionary, int (+13 more)

### Community 10 - "Task Components"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "OnlineFixFileTransaction"
Cohesion: 0.16
Nodes (10): Journal, Action, CancellationToken, int, IReadOnlyList, long, string, uint (+2 more)

### Community 12 - "AddGameChecks"
Cohesion: 0.33
Nodes (4): AddGameChecks, Action, Task, ZipArchive

### Community 13 - "SettingsPage"
Cohesion: 0.18
Nodes (5): ContentDialog, RoutedEventArgs, Task, Visibility, SettingsPage

### Community 14 - "AppUpdateService"
Cohesion: 0.08
Nodes (29): string, TimeSpan, AppConstants, DateTimeOffset, IReadOnlyList, AppUpdateCheckResult, DateTimeOffset, List (+21 more)

### Community 15 - "Page"
Cohesion: 0.08
Nodes (26): AboutGameWebView, CompactState, HeroDescriptionText, HeroImage, HeroRowDefinition, HeroTitleText, LeftContentPanel, MediaOverlayImage (+18 more)

### Community 16 - "Page"
Cohesion: 0.16
Nodes (11): CustomPipsContainer, HeroBypassButtonText, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, RoutedEventArgs, Border (+3 more)

### Community 17 - "Application"
Cohesion: 0.10
Nodes (21): Change, Failed, FeedSpec, CancellationToken, Task, IDynamicFeedRefreshService, DynamicFeedChanges, DynamicFeedKind (+13 more)

### Community 18 - "GameDetailPage"
Cohesion: 0.08
Nodes (14): double, MediaSource, bool, CancellationTokenSource, Dictionary, int, JsonSerializerOptions, List (+6 more)

### Community 19 - "AddGameService"
Cohesion: 0.14
Nodes (8): ConcurrentDictionary, HttpClient, IProgress, IReadOnlyList, JsonElement, string, Task, AddGameService

### Community 20 - "DynamicFeedRefreshService"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 22 - "SteamPlatformService"
Cohesion: 0.10
Nodes (21): CatalogManifest, CatalogPatchEntry, CatalogPatchRow, ICatalogBootstrapService, CancellationToken, Task, ICatalogUpdateService, CatalogUpdateResult (+13 more)

### Community 23 - "BypassGameDetailViewModel"
Cohesion: 0.09
Nodes (17): Action, Func, HttpClient, HttpResponseMessage, int, IReadOnlyList, RelayCommand, string (+9 more)

### Community 24 - "Page"
Cohesion: 0.08
Nodes (37): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep (+29 more)

### Community 25 - "NexaPlay.Core.Helpers"
Cohesion: 0.21
Nodes (8): NexaPlay.Core.Helpers, NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 26 - "GameDetailViewModel"
Cohesion: 0.08
Nodes (16): bool, Func, int, GameDetailViewModel, AddGameCommand, ApplyFixCommand, CancelFixCommand, CheckBypassCommand (+8 more)

### Community 27 - "GamesViewModel"
Cohesion: 0.09
Nodes (18): DateOnly, Action, bool, CancellationTokenSource, Dictionary, int, IReadOnlyList, ObservableCollection (+10 more)

### Community 28 - "AddGamePayloadBuilder"
Cohesion: 0.12
Nodes (14): char, MemberMetadata, CancellationToken, IEnumerable, int, long, string, uint (+6 more)

### Community 29 - "App Update Service"
Cohesion: 0.21
Nodes (9): MetadataWarmupProgress, CancellationToken, HashSet, IProgress, IReadOnlyDictionary, IReadOnlyList, string, Task (+1 more)

### Community 30 - "NexaPlay.Core.Helpers"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 31 - "AppliedStateStore"
Cohesion: 0.22
Nodes (6): Dictionary, IReadOnlyDictionary, SemaphoreSlim, string, Task, AppliedStateStore

### Community 32 - "BypassGamesPage"
Cohesion: 0.13
Nodes (9): Brush, DependencyObject, DispatcherTimer, int, NavigationEventArgs, SizeChangedEventArgs, Visibility, BypassGamesPage (+1 more)

### Community 33 - "Page"
Cohesion: 0.15
Nodes (17): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+9 more)

### Community 34 - "SettingsViewModel"
Cohesion: 0.23
Nodes (5): IReadOnlyList, Task, SettingsViewModel, Get, NavigationEventArgs

### Community 35 - "BypassGameDetailPage"
Cohesion: 0.08
Nodes (11): ContentDialogButtonClickEventArgs, bool, ContentDialog, CoreWebView2WebMessageReceivedEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, SizeChangedEventArgs, Storyboard (+3 more)

### Community 36 - "GamesPage"
Cohesion: 0.08
Nodes (13): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, int, IReadOnlyList, ItemClickEventArgs (+5 more)

### Community 37 - "AccountReportLimitStore"
Cohesion: 0.10
Nodes (18): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, Dictionary, int, SemaphoreSlim (+10 more)

### Community 38 - "CoverImageCacheService"
Cohesion: 0.17
Nodes (11): CancellationToken, ConcurrentDictionary, HttpClient, int, long, SemaphoreSlim, string, Task (+3 more)

### Community 39 - "OnlineFixService"
Cohesion: 0.12
Nodes (16): CancellationTokenSource, int, IReadOnlyDictionary, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel (+8 more)

### Community 40 - "Page"
Cohesion: 0.12
Nodes (14): FilterOverlay, FilterOverlayTranslate, GamesGrid, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs (+6 more)

### Community 41 - "LibraryPage"
Cohesion: 0.11
Nodes (10): Brush, DependencyObject, int, NavigationEventArgs, PropertyChangedEventArgs, RoutedEventArgs, string, Task (+2 more)

### Community 42 - "Load Async"
Cohesion: 0.18
Nodes (4): IReadOnlyList, RemoveGameResult, RelayCommand, Task

### Community 43 - "NexaPlay.Core.Models"
Cohesion: 0.11
Nodes (7): NexaPlay.Infrastructure.Logging, NexaPlay.Core.Models, NexaPlay.Contracts.Services, DateTimeOffset, List, AppUpdateManifest, NavigationItem

### Community 44 - ".ApplyFiltersAndPaginationAsync"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 45 - "I Add Game Service"
Cohesion: 0.19
Nodes (10): CancellationToken, IReadOnlyDictionary, IReadOnlyList, Task, ICatalogRepository, CatalogPage, CatalogPatch, CatalogPatchGame (+2 more)

### Community 46 - ".PopularGamesGrid_SizeChanged"
Cohesion: 0.14
Nodes (5): OfflineActivationCheckbox, TutorialVideoOverlayCloseBtn, RoutedEventArgs, Button, CheckBox

### Community 47 - "OnlineFixChecks"
Cohesion: 0.24
Nodes (6): Action, Func, int, string, Task, OnlineFixChecks

### Community 49 - "Page"
Cohesion: 0.13
Nodes (14): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif, 20. Rencana Migrasi yang Diusulkan (+6 more)

### Community 50 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.25
Nodes (4): FrameworkElement, Image, ExceptionRoutedEventArgs, TranslateTransform

### Community 51 - "LicenseInfo"
Cohesion: 0.10
Nodes (14): CancellationToken, Task, ILicenseService, LicensePlan, LicenseStatus, LicenseInfo, string, Task (+6 more)

### Community 52 - ".TryCreate"
Cohesion: 0.17
Nodes (6): Uri, ImageUriResolver, Uri, ImageSource, ImageSource, ImageSource

### Community 53 - "Graphify Workflow"
Cohesion: 0.29
Nodes (6): Task, LicenseAccessDialogHelper, Task, TappedRoutedEventArgs, UIElement, XamlRoot

### Community 54 - "FixEntry"
Cohesion: 0.20
Nodes (9): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, IReadOnlyList, FixEntry, CancellationToken, Dictionary (+1 more)

### Community 56 - ".Log"
Cohesion: 0.27
Nodes (5): AntivirusVendor, AntivirusInfo, IReadOnlyList, Task, WindowsDefenderService

### Community 57 - ".NavigateTo"
Cohesion: 0.24
Nodes (4): SidebarShell, PointerRoutedEventArgs, RadioButton, TextBlock

### Community 58 - "OnlineFixCatalogService"
Cohesion: 0.14
Nodes (17): CacheEnvelope, bool, CancellationToken, DateTimeOffset, Dictionary, HttpClient, HttpResponseMessage, int (+9 more)

### Community 59 - "WindowsDefenderService"
Cohesion: 0.18
Nodes (12): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+4 more)

### Community 60 - "NexaPlay.csproj"
Cohesion: 0.17
Nodes (10): net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.0.20), SharpCompress (0.48.1), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839) (+2 more)

### Community 61 - "SettingsPage"
Cohesion: 0.18
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 62 - "ISteamService"
Cohesion: 0.31
Nodes (4): CancellationToken, IReadOnlyList, Task, ISteamService

### Community 63 - ".RunAsync"
Cohesion: 0.20
Nodes (10): CancellationToken, Task, IOnlineFixCatalogService, OnlineFixResult, CancellationToken, IProgress, SemaphoreSlim, string (+2 more)

### Community 64 - ".OnActivateClicked"
Cohesion: 0.18
Nodes (8): Color, ActivateButton, BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, RoutedEventArgs, Button

### Community 66 - "BypassGamesDataService"
Cohesion: 0.28
Nodes (7): CancellationToken, Func, IReadOnlyList, SemaphoreSlim, string, Task, BypassGamesDataService

### Community 67 - "Page"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 68 - "IWindowsDefenderService"
Cohesion: 0.27
Nodes (4): IReadOnlyList, Task, IWindowsDefenderService, DefenderExclusionResult

### Community 69 - "Grid"
Cohesion: 0.24
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 70 - "AccountReportLimitStore"
Cohesion: 0.22
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 71 - "OnlineFixArchiveDownloader"
Cohesion: 0.19
Nodes (11): HttpContent, InvalidDataException, Action, CancellationToken, Dictionary, int, long, Task (+3 more)

### Community 72 - "Page"
Cohesion: 0.15
Nodes (9): GamesGrid, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, RoutedEventArgs, Border, GridView (+1 more)

### Community 73 - "CatalogManifestRules"
Cohesion: 0.20
Nodes (10): Project Graphify Rules, Graphify Add and Watch, Graphify Export Formats, Graphify Semantic Extraction Spec, Graphify GitHub and Merge Flow, Graphify Hook Integration, Graphify Query Traversal, Graphify Media Transcription (+2 more)

### Community 74 - "IAppLogService"
Cohesion: 0.12
Nodes (9): ConcurrentQueue, IReadOnlyList, Task, IAppLogService, int, IReadOnlyList, string, Task (+1 more)

### Community 75 - "FakeSteam"
Cohesion: 0.14
Nodes (7): DateTime, InstalledGame, CancellationToken, HttpRequestMessage, HttpResponseMessage, IReadOnlyList, FakeSteam

### Community 76 - "RoutedEventArgs"
Cohesion: 0.20
Nodes (10): Incremental Catalog Patches, Lazy R2 Game Metadata, Local SQLite Catalog, NexaPlay Data Flow Audit, NexaPlay Data Sources Query, New Games Download Timing, Load Games Download Cost, ETag Catalog Refresh (+2 more)

### Community 77 - "IAddGameService"
Cohesion: 0.22
Nodes (4): CancellationToken, IProgress, Task, IAddGameService

### Community 78 - "HomePage"
Cohesion: 0.15
Nodes (7): bool, DependencyObject, DispatcherTimer, ImageSource, int, Visibility, HomePage

### Community 79 - "FakeSteam"
Cohesion: 0.17
Nodes (3): SteamFinalizeResult, IReadOnlyList, FakeSteam

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

### Community 84 - ".ExtractDownloadedArchives"
Cohesion: 0.16
Nodes (7): error, fileName, meta, FixFile, List, success, path

### Community 85 - "IOnlineFixService"
Cohesion: 0.42
Nodes (4): CancellationToken, IProgress, Task, IOnlineFixService

### Community 86 - "Catalog Load Coordinator"
Cohesion: 0.25
Nodes (6): Action, Func, long, SemaphoreSlim, Task, CatalogLoadCoordinator

### Community 87 - "LibraryViewModel"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 88 - ".TutorialThumbnail_Tapped"
Cohesion: 0.22
Nodes (6): CoreWebView2, CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper, TappedRoutedEventArgs

### Community 89 - ".ExtractDownloadedArchives"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 90 - "Catalog Load Coordinator"
Cohesion: 0.29
Nodes (7): 2. Alur Startup, Entry point dan service, Gerbang license, Klasifikasi blocking, Pemanasan metadata, Pramuat cover dan halaman, Rantai pemanggilan aktual

### Community 91 - ".ApplyHwndIcon"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 92 - "OnlineFixModels.cs"
Cohesion: 0.18
Nodes (8): OnlineFixEntry, OnlineFixOutcome, OnlineFixPackage, AsyncHandler, CancellationToken, HttpRequestMessage, HttpResponseMessage, FakeCatalog

### Community 93 - "BypassProgressState"
Cohesion: 0.43
Nodes (5): IProgress, BypassProgressState, CaptureProgress, List, Capture

### Community 94 - ".GetLibraryPaths"
Cohesion: 0.27
Nodes (4): appId, installdir, name, IReadOnlyList

### Community 95 - "2. Alur Startup"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 96 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

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

### Community 128 - "NexaPlay.SelfCheck.csproj"
Cohesion: 0.40
Nodes (4): net8.0, Microsoft.Data.Sqlite (8.0.20), SharpCompress (0.48.1), Microsoft.NET.Sdk

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

### Community 133 - "IAppUpdateService"
Cohesion: 0.42
Nodes (4): CancellationToken, IProgress, Task, IAppUpdateService

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

### Community 143 - "ManagedManifestStore"
Cohesion: 0.32
Nodes (6): CancellationToken, IReadOnlyList, SemaphoreSlim, string, Task, ManagedManifestStore

### Community 161 - "Online-Fix — current implementation (2026-10-27)"
Cohesion: 0.29
Nodes (6): Freshness and request policy, Installation, Mapping contract, Online-Fix — current implementation (2026-10-27), Remove, recovery, and UI lifecycle, Verification

### Community 162 - "RootLayout"
Cohesion: 0.29
Nodes (5): DetailLayoutGrid, MediaOverlay, RootLayout, SizeChangedEventArgs, Grid

### Community 164 - "ISteamStoreService"
Cohesion: 0.47
Nodes (3): CancellationToken, Task, ISteamStoreService

### Community 165 - "FakeClock"
Cohesion: 0.40
Nodes (4): DateTimeOffset, TimeSpan, FakeClock, TimeProvider

### Community 166 - "Q: tolong analisa alur online fix sekarang"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: tolong analisa alur online fix sekarang, Source Nodes

### Community 171 - "Grid"
Cohesion: 0.50
Nodes (4): LicenseOverlay, ValidatingLicenseOverlay, VersionGrid, Grid

### Community 172 - "Journal"
Cohesion: 0.67
Nodes (3): JournalFile, List, Journal

### Community 174 - "StartupLoadingLogoRotation"
Cohesion: 0.67
Nodes (3): StartupLoadingLogoRotation, ValidationLoadingLogoRotation, RotateTransform

## Knowledge Gaps
- **334 isolated node(s):** `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `SharpCompress (0.48.1)`, `Microsoft.NET.Sdk`, `Viewbox` (+329 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **29 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Work-memory lessons

**Preferred sources** — corroborated by past sessions; start here.
- `MetadataService` (4× useful, score=1.389126958)
- `AddGameService` (3× useful, score=2.344502557)
- `SettingsViewModel` (3× useful, score=1.042242862)
- `BypassGamesDataService` (3× useful, score=1.041752773)
- `GameDetailViewModel` (2× useful, score=0.784019053) _(code changed — re-verify)_
- `SteamStoreService` (2× useful, score=0.743792889)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IAppLogService` connect `IAppLogService` to `HomeViewModel`, `BypassGamesViewModel`, `SteamStoreService`, `NexaPlayOverrideService`, `SteamPlatformService`, `MainWindow`, `AppUpdateService`, `Application`, `GameDetailPage`, `AddGameService`, `DynamicFeedRefreshService`, `SteamPlatformService`, `BypassGameDetailViewModel`, `GameDetailViewModel`, `App Update Service`, `SettingsViewModel`, `BypassGameDetailPage`, `AccountReportLimitStore`, `CoverImageCacheService`, `OnlineFixService`, `LicenseInfo`, `.Log`, `OnlineFixCatalogService`, `.RunAsync`, `BypassGamesDataService`, `Catalog Bootstrap Service`?**
  _High betweenness centrality (0.184) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `MainWindow` to `HomeViewModel`, `.OnActivateClicked`, `BypassGamesViewModel`, `Window`, `IAppUpdateService`, `IAppLogService`, `LicenseInfo`, `2. Alur Startup`, `IOnlineFixService`, `FixEntry`, `LibraryViewModel`, `SteamPlatformService`, `NexaPlay.Core.Helpers`, `.GetOrBuildListingCardAsync`, `GamesViewModel`, `SettingsPage`, `ISteamService`, `.NavigateTo`?**
  _High betweenness centrality (0.138) - this node is a cross-community bridge._
- **Why does `Window` connect `Window` to `.OnActivateClicked`, `MainWindow`, `Task Components`, `Grid`, `StartupLoadingLogoRotation`, `.GetOrBuildListingCardAsync`, `.NavigateTo`, `2. Alur Startup`?**
  _High betweenness centrality (0.110) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `SharpCompress (0.48.1)` to the rest of the system?**
  _334 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HomeViewModel` be split into smaller, more focused modules?**
  _Cohesion score 0.07598371777476255 - nodes in this community are weakly interconnected._
# Graph Report - NexaPlay  (2026-10-28)

## Corpus Check
- 172 files · ~195,784 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2482 nodes · 4951 edges · 185 communities (156 shown, 29 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 348 edges (avg confidence: 0.77)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `02f3d8e7`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- HomeViewModel
- BypassGamesViewModel
- SteamStoreService
- Add Game Payload
- Task
- Window
- SteamPlatformService
- Audit Phase 2 — Cache, Games, Refresh
- MainWindow
- SqliteCatalogRepository
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
- NexaPlay.Infrastructure.Services
- CatalogUpdateService
- BypassGameDetailViewModel
- Page
- NexaPlay.Core.Helpers
- GameDetailViewModel
- GamesViewModel
- AddGamePayloadBuilder
- MetadataService
- NexaPlay.Core.Helpers
- AppliedStateStore
- BypassGamesPage
- Page
- SettingsViewModel
- BypassGameDetailPage
- GamesPage
- TelegramAccountReportService
- CoverImageCacheService
- OnlineFixService
- Page
- LibraryPage
- .LoadAsync
- NexaPlay.Core.Models
- .ApplyFiltersAndPaginationAsync
- GameEntry
- RoutedEventArgs
- OnlineFixChecks
- Load Games Async
- Audit Alur Data NexaPlay
- NexaPlayOverrideService
- LicenseStore
- .TryCreate
- Graphify Workflow
- .LoadAsync
- .QueryDailyBrowseAsync
- WindowsDefenderService
- .NavigateTo
- OnlineFixCatalogService
- WindowsDefenderService
- NexaPlay.csproj
- INavigationService
- ISteamService
- .RunAsync
- .OnActivateClicked
- GameDetailEntry
- AccountReportLimitStore
- Page
- IWindowsDefenderService
- Grid
- .OnPageUnloaded
- OnlineFixArchiveDownloader
- Page
- CatalogManifestRules
- AppLogService
- FakeSteam
- RoutedEventArgs
- INexaPlayOverrideService
- HomePage
- .SendAsync
- Catalog Bootstrap Service
- .SelectManualFolderAsync
- Catalog Bootstrap Service
- App.xaml.cs
- .ExtractDownloadedArchives
- Task
- .ExtractWithExternalTool
- .ApplyHwndIcon
- .TutorialThumbnail_Tapped
- .ExtractDownloadedArchives
- Catalog Load Coordinator
- .ApplyHwndIcon
- IAddGameService
- BypassProgressState
- CancellationToken
- 2. Alur Startup
- graphify reference: query, path, explain
- ICatalogRefreshState
- Generate-AppIcon.ps1
- .TutorialThumbnail_Tapped
- .CheckArchivePasswordsAsync
- .Select
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
- Q: Untuk release aplikasi terbaru apakah catalog.sqlite harus terbaru atau boleh lama?
- I App Log Service
- .GetOrBuildListingCardAsync
- .SendAsync
- AppUpdateManifest
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
- .Log
- 37. Current vs Proposed Target Gap
- Verifikasi parity Lightning sebelum revisi
- ICatalogRefreshState
- Grid Components
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 14. Inventaris Model / DTO
- LicenseInfo
- 7. Alur Data Library
- .SelectManualFolderAsync
- sign-build.ps1
- extraction-spec.md
- 18. Arsitektur Target yang Direkomendasikan
- DeviceIdHelper
- notifyHost
- Root Split View
- hooks json
- RTK Output Filtering
- Layered Service Architecture
- Small Batch Build Workflow
- Home Page Decision Archive
- Layout-Cycle-Safe WebView Height Feedback
- .LoadGamesAsync
- IOnlineFixService
- Online-Fix — current implementation (2026-10-28)
- CatalogLoadCoordinator
- PointerRoutedEventArgs
- IAppLogService
- FakeClock
- Q: tolong analisa alur online fix sekarang
- .InstallLuaFromZip
- .OnNavigatedTo
- ISteamStoreService
- Q: System.Management.dll signing fails with SignerSign 0x80093102 while other files succeed
- .HeroCarousel_SelectionChanged
- int Components
- .PopularGamesGrid_SizeChanged
- Grid
- StringExtensions.cs
- StartupLoadingLogoRotation
- NavigationItem.cs
- .OnNavigatedTo
- generate.py
- README.md

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 91 edges
2. `GameDetailPage` - 83 edges
3. `NexaPlay.Core.Models` - 74 edges
4. `BypassGameDetailViewModel` - 70 edges
5. `MainWindow` - 64 edges
6. `GamesViewModel` - 62 edges
7. `Window` - 60 edges
8. `NexaPlay.Contracts.Services` - 57 edges
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
- `PasswordCheckLog` --implements--> `IAppLogService`  [EXTRACTED]
  NexaPlay.SelfCheck/OnlineFixPasswordChecks.cs → NexaPlay/Contracts/Services/IAppLogService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Add Game preparation and installation** — nexaplay_add_game_payload_github_lightning_payload_preparation, nexaplay_add_game_payload_flat_appid_zip_payload, nexaplay_add_game_payload_required_numeric_lua_selection, nexaplay_add_game_payload_byte_preserving_steam_installation, nexaplay_add_game_payload_add_game_staging_cleanup [EXTRACTED 1.00]
- **Signed release publication sequence** — nexaplay_release_readme_release_version_synchronization, nexaplay_release_readme_self_contained_release_publish, nexaplay_release_readme_ordered_build_and_installer_signing, nexaplay_release_readme_nexaplay_setup_installer, nexaplay_release_readme_upload_installer_before_update_manifest, nexaplay_release_readme_final_signed_installer_sha256 [EXTRACTED 1.00]
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]

## Communities (185 total, 29 thin omitted)

### Community 0 - "HomeViewModel"
Cohesion: 0.06
Nodes (35): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, CancellationToken, Task, ICoverImageCacheService, CancellationToken (+27 more)

### Community 1 - "BypassGamesViewModel"
Cohesion: 0.13
Nodes (10): BypassStatus, CancellationTokenSource, int, IReadOnlyList, List, ObservableCollection, string, Task (+2 more)

### Community 2 - "SteamStoreService"
Cohesion: 0.17
Nodes (11): JsonArray, JsonObject, ConcurrentDictionary, DateTime, HttpClient, int, IReadOnlyList, long (+3 more)

### Community 3 - "Add Game Payload"
Cohesion: 0.06
Nodes (53): Add Game Payload, Add Game Self Check, Add Game Staging Cleanup, Atomic Per File Installation, Automatic Lua Processing Mode, Bounded Archive Validation, Byte Preserving Steam Installation, Flat AppID ZIP Payload (+45 more)

### Community 4 - "Task"
Cohesion: 0.10
Nodes (19): HttpMessageHandler, AsyncHandler, CancellationToken, Dictionary, HttpRequestMessage, HttpResponseMessage, IReadOnlyDictionary, IReadOnlyList (+11 more)

### Community 5 - "Window"
Cohesion: 0.06
Nodes (51): AccountHeader, AppTitleBar, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames (+43 more)

### Community 6 - "SteamPlatformService"
Cohesion: 0.08
Nodes (19): appId, close, found, installdir, keyLine, name, CancellationToken, IReadOnlyList (+11 more)

### Community 7 - "Audit Phase 2 — Cache, Games, Refresh"
Cohesion: 0.05
Nodes (40): 10. Home Popular Current Flow, 11. Home Recent Current Flow, 12. Games SQLite Current Flow, 13. Games Default Ordering, 14. Daily Rotation, 15. Default Price Floor, 16. Price Distribution/Mix, 17. Games Paging (+32 more)

### Community 8 - "MainWindow"
Cohesion: 0.13
Nodes (13): bool, ContentDialog, int, NavigationEventArgs, Storyboard, string, Task, TaskCompletionSource (+5 more)

### Community 9 - "SqliteCatalogRepository"
Cohesion: 0.15
Nodes (14): BrowseCandidate, CancellationToken, Task, ICatalogBootstrapService, CancellationToken, int, IReadOnlyList, string (+6 more)

### Community 10 - "Task Components"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "OnlineFixFileTransaction"
Cohesion: 0.14
Nodes (13): Journal, JournalFile, Action, CancellationToken, int, IReadOnlyList, List, long (+5 more)

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
Cohesion: 0.05
Nodes (35): FrameworkElement, Image, AboutGameWebView, CompactState, DetailLayoutGrid, HeroDescriptionText, HeroImage, HeroRowDefinition (+27 more)

### Community 16 - "Page"
Cohesion: 0.16
Nodes (11): CustomPipsContainer, HeroBypassButtonText, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesHeaderGrid, RoutedEventArgs, Border (+3 more)

### Community 17 - "Application"
Cohesion: 0.10
Nodes (21): Change, Failed, FeedSpec, CancellationToken, Task, IDynamicFeedRefreshService, DynamicFeedChanges, DynamicFeedKind (+13 more)

### Community 18 - "GameDetailPage"
Cohesion: 0.06
Nodes (20): double, MediaSource, DenuvoBadge, DenuvoBadgeGlowOverlay, bool, CancellationTokenSource, Dictionary, int (+12 more)

### Community 19 - "AddGameService"
Cohesion: 0.13
Nodes (9): RemoveGameResult, ConcurrentDictionary, HttpClient, IProgress, IReadOnlyList, JsonElement, string, Task (+1 more)

### Community 20 - "DynamicFeedRefreshService"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 22 - "CatalogUpdateService"
Cohesion: 0.09
Nodes (23): CatalogManifest, CatalogPatchEntry, CatalogPatchRow, CancellationToken, Task, ICatalogUpdateService, CatalogPatch, CatalogPatchGame (+15 more)

### Community 23 - "BypassGameDetailViewModel"
Cohesion: 0.10
Nodes (14): Func, HttpClient, int, IReadOnlyList, RelayCommand, string, BypassGameDetailViewModel, CopySteamGuardCodeCommand (+6 more)

### Community 24 - "Page"
Cohesion: 0.08
Nodes (37): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep (+29 more)

### Community 25 - "NexaPlay.Core.Helpers"
Cohesion: 0.20
Nodes (7): NexaPlay.Core.Helpers, NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, CatalogProtectionResolver, SystemRequirementItem

### Community 26 - "GameDetailViewModel"
Cohesion: 0.05
Nodes (22): bool, CancellationToken, CancellationTokenSource, Func, HashSet, int, IReadOnlyList, RelayCommand (+14 more)

### Community 27 - "GamesViewModel"
Cohesion: 0.09
Nodes (18): DateOnly, Action, bool, CancellationTokenSource, Dictionary, int, IReadOnlyList, ObservableCollection (+10 more)

### Community 28 - "AddGamePayloadBuilder"
Cohesion: 0.12
Nodes (14): char, MemberMetadata, CancellationToken, IEnumerable, int, long, string, uint (+6 more)

### Community 29 - "MetadataService"
Cohesion: 0.22
Nodes (9): MetadataWarmupProgress, CancellationToken, HashSet, IProgress, IReadOnlyDictionary, IReadOnlyList, string, Task (+1 more)

### Community 30 - "NexaPlay.Core.Helpers"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 31 - "AppliedStateStore"
Cohesion: 0.22
Nodes (6): Dictionary, IReadOnlyDictionary, SemaphoreSlim, string, Task, AppliedStateStore

### Community 32 - "BypassGamesPage"
Cohesion: 0.13
Nodes (10): GamesGrid, Brush, DependencyObject, DispatcherTimer, int, SizeChangedEventArgs, Visibility, BypassGamesPage (+2 more)

### Community 33 - "Page"
Cohesion: 0.15
Nodes (17): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+9 more)

### Community 34 - "SettingsViewModel"
Cohesion: 0.20
Nodes (8): CancellationToken, IProgress, Task, IAppUpdateService, IReadOnlyList, Task, SettingsViewModel, Get

### Community 35 - "BypassGameDetailPage"
Cohesion: 0.11
Nodes (8): bool, CoreWebView2WebMessageReceivedEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, SizeChangedEventArgs, Storyboard, Visibility, BypassGameDetailPage

### Community 36 - "GamesPage"
Cohesion: 0.08
Nodes (13): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, int, IReadOnlyList, ItemClickEventArgs (+5 more)

### Community 37 - "TelegramAccountReportService"
Cohesion: 0.15
Nodes (10): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, HttpClient, string, Task (+2 more)

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

### Community 42 - ".LoadAsync"
Cohesion: 0.24
Nodes (3): IReadOnlyList, RelayCommand, Task

### Community 43 - "NexaPlay.Core.Models"
Cohesion: 0.14
Nodes (3): NexaPlay.Core.Models, NexaPlay.Contracts.Services, NexaPlay.Core.Enums

### Community 44 - ".ApplyFiltersAndPaginationAsync"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 45 - "GameEntry"
Cohesion: 0.20
Nodes (9): CancellationToken, IReadOnlyDictionary, IReadOnlyList, Task, ICatalogRepository, CatalogRuntimeOverride, IReadOnlyList, string (+1 more)

### Community 46 - "RoutedEventArgs"
Cohesion: 0.17
Nodes (5): OfflineActivationCheckbox, TutorialVideoOverlayCloseBtn, RoutedEventArgs, Button, CheckBox

### Community 47 - "OnlineFixChecks"
Cohesion: 0.23
Nodes (7): Action, Func, int, string, Task, OnlineFixChecks, string

### Community 49 - "Audit Alur Data NexaPlay"
Cohesion: 0.10
Nodes (19): 11. Inventaris Source Data, 12. Inventaris Request Jaringan, 13. Inventaris Cache, 15. Data Duplikat, 16. Diagram Arsitektur Saat Ini, 18. Arsitektur Target yang Direkomendasikan, 19. Keputusan JSON vs SQLite, 1. Ringkasan Eksekutif (+11 more)

### Community 50 - "NexaPlayOverrideService"
Cohesion: 0.22
Nodes (9): bool, Dictionary, HttpClient, IReadOnlyList, JsonElement, SemaphoreSlim, string, NexaPlayOverrideService (+1 more)

### Community 51 - "LicenseStore"
Cohesion: 0.21
Nodes (6): LicensePlan, LicenseStatus, string, Task, LicenseStore, StoredLicense

### Community 52 - ".TryCreate"
Cohesion: 0.17
Nodes (6): Uri, ImageUriResolver, Uri, ImageSource, ImageSource, ImageSource

### Community 53 - "Graphify Workflow"
Cohesion: 0.29
Nodes (6): Task, LicenseAccessDialogHelper, Task, TappedRoutedEventArgs, UIElement, XamlRoot

### Community 55 - ".QueryDailyBrowseAsync"
Cohesion: 0.27
Nodes (9): BrowseBlock, CatalogPage, CatalogQuery, Dictionary, IReadOnlyDictionary, Parameters, Sql, SqliteCommand (+1 more)

### Community 56 - "WindowsDefenderService"
Cohesion: 0.42
Nodes (3): IReadOnlyList, Task, WindowsDefenderService

### Community 57 - ".NavigateTo"
Cohesion: 0.24
Nodes (4): SidebarShell, PointerRoutedEventArgs, RadioButton, TextBlock

### Community 58 - "OnlineFixCatalogService"
Cohesion: 0.12
Nodes (19): CacheEnvelope, string, OnlineFixEntry, bool, CancellationToken, DateTimeOffset, Dictionary, HttpClient (+11 more)

### Community 59 - "WindowsDefenderService"
Cohesion: 0.18
Nodes (12): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+4 more)

### Community 60 - "NexaPlay.csproj"
Cohesion: 0.17
Nodes (10): net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.0.20), SharpCompress (0.48.1), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839) (+2 more)

### Community 61 - "INavigationService"
Cohesion: 0.17
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 62 - "ISteamService"
Cohesion: 0.27
Nodes (4): CancellationToken, IReadOnlyList, Task, ISteamService

### Community 63 - ".RunAsync"
Cohesion: 0.20
Nodes (10): CancellationToken, Task, IOnlineFixCatalogService, OnlineFixResult, CancellationToken, IProgress, SemaphoreSlim, string (+2 more)

### Community 64 - ".OnActivateClicked"
Cohesion: 0.18
Nodes (8): Color, ActivateButton, BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, RoutedEventArgs, Button

### Community 65 - "GameDetailEntry"
Cohesion: 0.17
Nodes (8): HasTransientError, Json, DateTime, IReadOnlyList, GameDetailEntry, MovieEntry, CancellationToken, Task

### Community 66 - "AccountReportLimitStore"
Cohesion: 0.24
Nodes (8): CancellationToken, Dictionary, int, SemaphoreSlim, string, Task, AccountReportLimitStore, ReportLimitState

### Community 67 - "Page"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 68 - "IWindowsDefenderService"
Cohesion: 0.21
Nodes (6): IReadOnlyList, Task, IWindowsDefenderService, AntivirusVendor, AntivirusInfo, DefenderExclusionResult

### Community 69 - "Grid"
Cohesion: 0.24
Nodes (6): IReadOnlyDictionary, IReadOnlySet, CatalogManifestRules, CatalogFileState, CatalogManifestDocument, CatalogManifestEntry

### Community 70 - ".OnPageUnloaded"
Cohesion: 0.18
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 71 - "OnlineFixArchiveDownloader"
Cohesion: 0.15
Nodes (13): HttpContent, InvalidDataException, OnlineFixOutcome, OnlineFixPackage, Action, CancellationToken, Dictionary, int (+5 more)

### Community 72 - "Page"
Cohesion: 0.25
Nodes (6): HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs, Border, Page

### Community 73 - "CatalogManifestRules"
Cohesion: 0.20
Nodes (10): Project Graphify Rules, Graphify Add and Watch, Graphify Export Formats, Graphify Semantic Extraction Spec, Graphify GitHub and Merge Flow, Graphify Hook Integration, Graphify Query Traversal, Graphify Media Transcription (+2 more)

### Community 74 - "AppLogService"
Cohesion: 0.20
Nodes (6): ConcurrentQueue, int, IReadOnlyList, string, Task, AppLogService

### Community 75 - "FakeSteam"
Cohesion: 0.10
Nodes (7): DateTime, InstalledGame, SteamFinalizeResult, IReadOnlyList, FakeSteam, IReadOnlyList, FakeSteam

### Community 76 - "RoutedEventArgs"
Cohesion: 0.20
Nodes (10): Incremental Catalog Patches, Lazy R2 Game Metadata, Local SQLite Catalog, NexaPlay Data Flow Audit, NexaPlay Data Sources Query, New Games Download Timing, Load Games Download Cost, ETag Catalog Refresh (+2 more)

### Community 77 - "INexaPlayOverrideService"
Cohesion: 0.22
Nodes (11): CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride (+3 more)

### Community 78 - "HomePage"
Cohesion: 0.15
Nodes (7): bool, DependencyObject, DispatcherTimer, ImageSource, int, Visibility, HomePage

### Community 79 - ".SendAsync"
Cohesion: 0.29
Nodes (4): CancellationToken, HttpRequestMessage, HttpResponseMessage, FakeCatalog

### Community 80 - "Catalog Bootstrap Service"
Cohesion: 0.36
Nodes (6): bool, CancellationToken, SemaphoreSlim, string, Task, CatalogBootstrapService

### Community 81 - ".SelectManualFolderAsync"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 82 - "Catalog Bootstrap Service"
Cohesion: 0.22
Nodes (9): NexaPlay Gold Lightning Logo (logo.png), NexaPlay Gold Lightning Logo (LockScreenLogo.scale-200.png), NexaPlay Gold Lightning Logo (logo.svg), NexaPlay Gold Lightning Logo (SplashScreen.scale-200.png), NexaPlay Gold Lightning Logo (Square150x150Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.scale-200.png), NexaPlay Gold Lightning Logo (Square44x44Logo.targetsize-24_altform-unplated.png), NexaPlay Gold Lightning Logo (StoreLogo.png) (+1 more)

### Community 83 - "App.xaml.cs"
Cohesion: 0.15
Nodes (5): NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Logging, NexaPlay.Infrastructure.Platform

### Community 84 - ".ExtractDownloadedArchives"
Cohesion: 0.24
Nodes (5): fileName, meta, FixFile, List, path

### Community 85 - "Task"
Cohesion: 0.39
Nodes (3): Action, HttpResponseMessage, Task

### Community 87 - ".ApplyHwndIcon"
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

### Community 92 - "IAddGameService"
Cohesion: 0.15
Nodes (4): CancellationToken, IProgress, Task, IAddGameService

### Community 93 - "BypassProgressState"
Cohesion: 0.31
Nodes (6): IProgress, BypassProgressState, CaptureProgress, List, Capture, CancelAtExtraction

### Community 94 - "CancellationToken"
Cohesion: 0.32
Nodes (4): CancellationToken, IReadOnlyDictionary, IReadOnlySet, Task

### Community 95 - "2. Alur Startup"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 96 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 97 - "ICatalogRefreshState"
Cohesion: 0.33
Nodes (3): ICatalogRefreshState, long, CatalogRefreshState

### Community 98 - "Generate-AppIcon.ps1"
Cohesion: 0.53
Nodes (3): ICollection, ISet, JsonElement

### Community 99 - ".TutorialThumbnail_Tapped"
Cohesion: 0.33
Nodes (5): Add Game: sumber API dan ZIP final, Pemasangan, Persiapan khusus lima sumber GitHub, Urutan sumber, Verifikasi

### Community 100 - ".CheckArchivePasswordsAsync"
Cohesion: 0.24
Nodes (4): ConcurrentQueue, IReadOnlyList, Task, PasswordCheckLog

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

### Community 116 - "Q: Untuk release aplikasi terbaru apakah catalog.sqlite harus terbaru atau boleh lama?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Untuk release aplikasi terbaru apakah catalog.sqlite harus terbaru atau boleh lama?, Source Nodes

### Community 118 - ".GetOrBuildListingCardAsync"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 119 - ".SendAsync"
Cohesion: 0.33
Nodes (4): BlockingHandler, CancellationToken, HttpRequestMessage, HttpResponseMessage

### Community 120 - "AppUpdateManifest"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

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

### Community 133 - ".Log"
Cohesion: 0.55
Nodes (3): CancellationToken, Task, LicenseService

### Community 134 - "37. Current vs Proposed Target Gap"
Cohesion: 0.67
Nodes (3): 37. Current vs Proposed Target Gap, Architecture target, Clear Cache target

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

### Community 141 - "LicenseInfo"
Cohesion: 0.38
Nodes (4): CancellationToken, Task, ILicenseService, LicenseInfo

### Community 142 - "7. Alur Data Library"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

### Community 143 - ".SelectManualFolderAsync"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 145 - "sign-build.ps1"
Cohesion: 0.40
Nodes (3): Invoke-SignFile(), Test-FileSignature(), Start-Sleep()

### Community 160 - "IOnlineFixService"
Cohesion: 0.46
Nodes (4): CancellationToken, IProgress, Task, IOnlineFixService

### Community 161 - "Online-Fix — current implementation (2026-10-28)"
Cohesion: 0.25
Nodes (7): Freshness and request policy, Installation, Mapping contract, Online-Fix — current implementation (2026-10-28), Optional archive password, Remove, recovery, and UI lifecycle, Verification

### Community 162 - "CatalogLoadCoordinator"
Cohesion: 0.25
Nodes (6): Action, Func, long, SemaphoreSlim, Task, CatalogLoadCoordinator

### Community 164 - "IAppLogService"
Cohesion: 0.29
Nodes (3): IReadOnlyList, Task, IAppLogService

### Community 165 - "FakeClock"
Cohesion: 0.40
Nodes (4): DateTimeOffset, TimeSpan, FakeClock, TimeProvider

### Community 166 - "Q: tolong analisa alur online fix sekarang"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: tolong analisa alur online fix sekarang, Source Nodes

### Community 172 - "ISteamStoreService"
Cohesion: 0.47
Nodes (3): CancellationToken, Task, ISteamStoreService

### Community 173 - "Q: System.Management.dll signing fails with SignerSign 0x80093102 while other files succeed"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: System.Management.dll signing fails with SignerSign 0x80093102 while other files succeed, Source Nodes

### Community 174 - ".HeroCarousel_SelectionChanged"
Cohesion: 0.40
Nodes (3): HeroCarousel, SelectionChangedEventArgs, FlipView

### Community 177 - ".PopularGamesGrid_SizeChanged"
Cohesion: 0.40
Nodes (3): PopularGamesGrid, SizeChangedEventArgs, GridView

### Community 178 - "Grid"
Cohesion: 0.50
Nodes (4): LicenseOverlay, ValidatingLicenseOverlay, VersionGrid, Grid

### Community 180 - "StartupLoadingLogoRotation"
Cohesion: 0.67
Nodes (3): StartupLoadingLogoRotation, ValidationLoadingLogoRotation, RotateTransform

## Knowledge Gaps
- **341 isolated node(s):** `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `SharpCompress (0.48.1)`, `Microsoft.NET.Sdk`, `Viewbox` (+336 more)
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

- **Why does `IAppLogService` connect `IAppLogService` to `HomeViewModel`, `BypassGamesViewModel`, `SteamStoreService`, `Task`, `.Log`, `SteamPlatformService`, `MainWindow`, `AppUpdateService`, `Application`, `GameDetailPage`, `AddGameService`, `DynamicFeedRefreshService`, `CatalogUpdateService`, `BypassGameDetailViewModel`, `GameDetailViewModel`, `MetadataService`, `SettingsViewModel`, `BypassGameDetailPage`, `TelegramAccountReportService`, `CoverImageCacheService`, `OnlineFixService`, `NexaPlayOverrideService`, `WindowsDefenderService`, `OnlineFixCatalogService`, `.RunAsync`, `AppLogService`, `Catalog Bootstrap Service`, `.CheckArchivePasswordsAsync`?**
  _High betweenness centrality (0.246) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `MainWindow` to `HomeViewModel`, `ICatalogRefreshState`, `SettingsViewModel`, `IOnlineFixService`, `IAppLogService`, `Window`, `.OnActivateClicked`, `BypassGamesViewModel`, `LicenseInfo`, `2. Alur Startup`, `CatalogUpdateService`, `.ApplyHwndIcon`, `.GetOrBuildListingCardAsync`, `NexaPlay.Core.Helpers`, `GamesViewModel`, `INavigationService`, `ISteamService`, `.NavigateTo`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Why does `GameDetailViewModel` connect `GameDetailViewModel` to `HomeViewModel`, `IOnlineFixService`, `BypassGamesViewModel`, `GameDetailEntry`, `IAppLogService`, `ISteamStoreService`, `LicenseInfo`, `INexaPlayOverrideService`, `GameEntry`, `.ApplyFiltersAndPaginationAsync`, `Page`, `GameDetailPage`, `.GetOrBuildListingCardAsync`, `NexaPlay.Core.Helpers`, `.ApplyHwndIcon`, `IAddGameService`, `INavigationService`, `ISteamService`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net8.0`, `Microsoft.Data.Sqlite (8.0.20)`, `SharpCompress (0.48.1)` to the rest of the system?**
  _341 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HomeViewModel` be split into smaller, more focused modules?**
  _Cohesion score 0.05971173644474949 - nodes in this community are weakly interconnected._
# Graph Report - NexaPlay  (2026-09-02)

## Corpus Check
- 128 files · ~152,157 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1849 nodes · 3931 edges · 110 communities (86 shown, 24 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 304 edges (avg confidence: 0.76)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8c8b8e4d`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .Log
- HomeViewModel
- BypassGamesViewModel
- Window
- BypassGamesPage
- HomePage
- SteamPlatformService
- NexaPlayOverrideService
- LicenseInfo
- IAppLogService
- Application
- Page
- GamesViewModel
- ListingCoverResolver
- Page
- SteamStoreService
- GameDetailPage
- BypassTutorialVideoService
- GameCoverIndexService
- AddGameService
- BypassGameDetailViewModel
- GamesPage
- NexaPlay.Core.Enums
- GameDetailViewModel
- LibraryPage
- MainWindow
- BypassGameDetailPage
- AppUpdateService
- NexaPlay.Presentation.ViewModels
- IWindowsDefenderService
- NexaPlay.Core.Models
- Page
- LibraryViewModel
- SettingsPage
- Task
- ScreenshotEntry
- Border
- Page
- Page
- .ApplyFiltersAndPaginationAsync
- CoverImageCacheService
- SettingsViewModel
- Authoritative New Games AppID List
- .FindSiblingSkeletonOverlay
- RoutedEventArgs
- NullLogService
- TelegramAccountReportService
- IAddGameService
- .NavigateTo
- .LoadGamesAsync
- AppUpdateCheckResult
- AccountReportLimitStore
- RoutedEventArgs
- INavigationService
- ISteamService
- NexaPlay.csproj
- CoverArtImage
- .SelectManualFolderAsync
- .ExtractDownloadedArchives
- .LoadAsync
- Task
- .ViewModel_PropertyChanged
- .LoadAsync
- .ApplyHwndIcon
- .GetAsync
- BoolToVisibilityConverter
- .MediaCard_Tapped
- .ExtractWithExternalTool
- RelayCommand
- CatalogCacheStamp.cs
- .OnNavChecked
- .TutorialThumbnail_Tapped
- SteamGuardService
- .LoadAsync
- GameHub Feature Parity
- .AboutGameWebView_NavigationCompleted
- IReadOnlyList
- ObservableObject
- .RootLayout_SizeChanged
- RootCardBorder
- NexaPlay Wide Tile Logo
- AppConstants
- AppUpdateManifest
- LibraryList
- NexaPlay.SelfCheck.csproj
- Minimalist Monochrome Branding
- NexaPlay Square App Logo
- Gray Framed X Symbol
- StringExtensions.cs
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

## God Nodes (most connected - your core abstractions)
1. `GameDetailViewModel` - 86 edges
2. `GameDetailPage` - 83 edges
3. `BypassGameDetailViewModel` - 70 edges
4. `GamesViewModel` - 66 edges
5. `Window` - 60 edges
6. `MainWindow` - 58 edges
7. `NexaPlay.Core.Models` - 56 edges
8. `BypassGameDetailPage` - 56 edges
9. `MetadataService` - 55 edges
10. `HomeViewModel` - 50 edges

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

## Communities (110 total, 24 thin omitted)

### Community 0 - ".Log"
Cohesion: 0.07
Nodes (32): DownloadResult, IReadOnlyCollection, IEnumerable, IReadOnlyDictionary, ISet, JsonElement, JsonSerializerOptions, NewGamesCatalog (+24 more)

### Community 1 - "HomeViewModel"
Cohesion: 0.06
Nodes (35): CancellationToken, Task, ICoverImageCacheService, CancellationToken, Task, IListingCoverResolver, CancellationToken, IProgress (+27 more)

### Community 2 - "BypassGamesViewModel"
Cohesion: 0.08
Nodes (28): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, CancellationToken, IProgress, Task, IOnlineFixService (+20 more)

### Community 3 - "Window"
Cohesion: 0.06
Nodes (51): AccountHeader, BypassChevron, BypassSubmenuPanel, ContentFrame, ContentTopBarRow, LblBypassGames, LblGames, LblHome (+43 more)

### Community 4 - "BypassGamesPage"
Cohesion: 0.06
Nodes (25): Task, LicenseAccessDialogHelper, GamesGrid, HoverTitleLayer, Page, RootPage, Brush, DependencyObject (+17 more)

### Community 5 - "HomePage"
Cohesion: 0.06
Nodes (27): Border, CustomPipsContainer, HeroBypassButtonText, HeroCarousel, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesGrid (+19 more)

### Community 6 - "SteamPlatformService"
Cohesion: 0.08
Nodes (21): appId, close, found, installdir, keyLine, name, DateTime, InstalledGame (+13 more)

### Community 7 - "NexaPlayOverrideService"
Cohesion: 0.10
Nodes (22): CancellationToken, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride, NexaPlayMovieOverride (+14 more)

### Community 8 - "LicenseInfo"
Cohesion: 0.10
Nodes (14): CancellationToken, Task, ILicenseService, LicensePlan, LicenseStatus, LicenseInfo, string, Task (+6 more)

### Community 9 - "IAppLogService"
Cohesion: 0.07
Nodes (21): IReadOnlyList, Task, IAppLogService, ConcurrentQueue, int, IReadOnlyList, string, Task (+13 more)

### Community 10 - "Application"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "Page"
Cohesion: 0.07
Nodes (29): AboutGameWebView, CompactState, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroTitleText, LeftContentPanel, MediaOverlay (+21 more)

### Community 12 - "GamesViewModel"
Cohesion: 0.09
Nodes (18): bool, CancellationTokenSource, Dictionary, int, ObservableCollection, RelayCommand, SemaphoreSlim, string (+10 more)

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

### Community 18 - "GameCoverIndexService"
Cohesion: 0.15
Nodes (12): CancellationToken, JsonElement, Task, GameCoverIndexFile, CancellationToken, Dictionary, HttpClient, JsonElement (+4 more)

### Community 19 - "AddGameService"
Cohesion: 0.11
Nodes (10): BypassStatus, BypassProgressState, RemoveGameResult, CancellationToken, ConcurrentDictionary, HttpResponseMessage, IProgress, IReadOnlyList (+2 more)

### Community 20 - "BypassGameDetailViewModel"
Cohesion: 0.10
Nodes (14): Func, HttpClient, int, IReadOnlyList, RelayCommand, string, BypassGameDetailViewModel, CopySteamGuardCodeCommand (+6 more)

### Community 21 - "GamesPage"
Cohesion: 0.09
Nodes (14): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, ImageSource, int, IReadOnlyList (+6 more)

### Community 22 - "NexaPlay.Core.Enums"
Cohesion: 0.14
Nodes (5): NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Services, NexaPlay.Core.Enums, NexaPlay.Infrastructure.Platform

### Community 23 - "GameDetailViewModel"
Cohesion: 0.09
Nodes (16): CancellationTokenSource, Func, int, GameDetailViewModel, AddGameCommand, ApplyFixCommand, CancelFixCommand, CheckBypassCommand (+8 more)

### Community 24 - "LibraryPage"
Cohesion: 0.10
Nodes (11): Brush, DependencyObject, ImageSource, int, NavigationEventArgs, PropertyChangedEventArgs, RoutedEventArgs, string (+3 more)

### Community 25 - "MainWindow"
Cohesion: 0.12
Nodes (15): Color, bool, ContentDialog, int, NavigationEventArgs, RoutedEventArgs, Storyboard, string (+7 more)

### Community 26 - "BypassGameDetailPage"
Cohesion: 0.10
Nodes (9): ShimmerContainer, bool, CoreWebView2WebMessageReceivedEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, Storyboard, Uri, Visibility (+1 more)

### Community 27 - "AppUpdateService"
Cohesion: 0.16
Nodes (11): DateTimeOffset, List, AppUpdateState, CancellationToken, HttpClient, IProgress, JsonSerializerOptions, string (+3 more)

### Community 28 - "NexaPlay.Presentation.ViewModels"
Cohesion: 0.17
Nodes (8): NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Infrastructure.Logging, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 29 - "IWindowsDefenderService"
Cohesion: 0.15
Nodes (9): IReadOnlyList, Task, IWindowsDefenderService, AntivirusVendor, AntivirusInfo, DefenderExclusionResult, IReadOnlyList, Task (+1 more)

### Community 30 - "NexaPlay.Core.Models"
Cohesion: 0.16
Nodes (4): NexaPlay.Core.Helpers, NexaPlay.Core.Models, NexaPlay.Contracts.Services, NavigationItem

### Community 31 - "Page"
Cohesion: 0.12
Nodes (14): FilterOverlay, FilterOverlayTranslate, GamesGrid, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs (+6 more)

### Community 32 - "LibraryViewModel"
Cohesion: 0.13
Nodes (15): CancellationTokenSource, int, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel, Get (+7 more)

### Community 33 - "SettingsPage"
Cohesion: 0.19
Nodes (5): ContentDialog, RoutedEventArgs, Task, Visibility, SettingsPage

### Community 34 - "Task"
Cohesion: 0.22
Nodes (6): GameFilterIndex, CancellationToken, HashSet, IReadOnlyList, List, Task

### Community 35 - "ScreenshotEntry"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 36 - "Border"
Cohesion: 0.20
Nodes (9): AppTitleBar, LicenseBadge, LicenseStatusBorder, PageTopBar, SidebarShell, StartupOverlay, ValidationErrorBorder, PointerRoutedEventArgs (+1 more)

### Community 37 - "Page"
Cohesion: 0.15
Nodes (17): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+9 more)

### Community 38 - "Page"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 39 - ".ApplyFiltersAndPaginationAsync"
Cohesion: 0.22
Nodes (3): IReadOnlyList, RelayCommand, Task

### Community 40 - "CoverImageCacheService"
Cohesion: 0.23
Nodes (9): CancellationToken, ConcurrentDictionary, HttpClient, SemaphoreSlim, string, Task, TimeSpan, Uri (+1 more)

### Community 41 - "SettingsViewModel"
Cohesion: 0.19
Nodes (5): IReadOnlyList, Task, SettingsViewModel, Get, NavigationEventArgs

### Community 42 - "Authoritative New Games AppID List"
Cohesion: 0.14
Nodes (15): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+7 more)

### Community 43 - ".FindSiblingSkeletonOverlay"
Cohesion: 0.16
Nodes (7): FrameworkElement, Image, HeroImage, MediaOverlayImage, ExceptionRoutedEventArgs, Image, TranslateTransform

### Community 44 - "RoutedEventArgs"
Cohesion: 0.16
Nodes (5): OfflineActivationCheckbox, TutorialVideoOverlayCloseBtn, RoutedEventArgs, Button, CheckBox

### Community 45 - "NullLogService"
Cohesion: 0.15
Nodes (8): HttpMessageHandler, HttpRequestMessage, CancellationToken, HttpResponseMessage, IReadOnlyList, Task, NullLogService, StatusCodeHandler

### Community 46 - "TelegramAccountReportService"
Cohesion: 0.15
Nodes (10): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, HttpClient, string, Task (+2 more)

### Community 47 - "IAddGameService"
Cohesion: 0.20
Nodes (5): CancellationToken, IProgress, Task, IAddGameService, Task

### Community 48 - ".NavigateTo"
Cohesion: 0.19
Nodes (8): ActivateButton, BypassSubmenuSteam, BypassSubmenuThirdParty, ValidationEnterKeyButton, ValidationRetryButton, Button, RadioButton, TextBlock

### Community 49 - ".LoadGamesAsync"
Cohesion: 0.19
Nodes (5): Message, ICatalogRefreshState, long, CatalogRefreshState, Success

### Community 50 - "AppUpdateCheckResult"
Cohesion: 0.27
Nodes (7): CancellationToken, IProgress, Task, IAppUpdateService, DateTimeOffset, IReadOnlyList, AppUpdateCheckResult

### Community 51 - "AccountReportLimitStore"
Cohesion: 0.24
Nodes (8): CancellationToken, Dictionary, int, SemaphoreSlim, string, Task, AccountReportLimitStore, ReportLimitState

### Community 53 - "INavigationService"
Cohesion: 0.18
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 54 - "ISteamService"
Cohesion: 0.21
Nodes (5): CancellationToken, IReadOnlyList, Task, ISteamService, SteamFinalizeResult

### Community 56 - "NexaPlay.csproj"
Cohesion: 0.18
Nodes (9): net8.0-windows10.0.19041.0, Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839), Microsoft.WindowsAppSDK (1.6.241114003), SharpCompress (0.48.1) (+1 more)

### Community 57 - "CoverArtImage"
Cohesion: 0.22
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 58 - ".SelectManualFolderAsync"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 59 - ".ExtractDownloadedArchives"
Cohesion: 0.24
Nodes (5): fileName, meta, FixFile, List, path

### Community 61 - "Task"
Cohesion: 0.39
Nodes (3): Action, HttpResponseMessage, Task

### Community 62 - ".ViewModel_PropertyChanged"
Cohesion: 0.25
Nodes (4): DenuvoBadge, DenuvoBadgeGlowOverlay, PropertyChangedEventArgs, Border

### Community 64 - ".ApplyHwndIcon"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 65 - ".GetAsync"
Cohesion: 0.38
Nodes (4): CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper

### Community 66 - "BoolToVisibilityConverter"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 67 - ".MediaCard_Tapped"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 70 - "CatalogCacheStamp.cs"
Cohesion: 0.33
Nodes (3): CatalogCacheEnvelope, CatalogCacheStamp, CatalogSourceStamp

### Community 71 - ".OnNavChecked"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 72 - ".TutorialThumbnail_Tapped"
Cohesion: 0.33
Nodes (3): CoreWebView2, TutorialThumbnailOverlay, TappedRoutedEventArgs

### Community 73 - "SteamGuardService"
Cohesion: 0.40
Nodes (3): Action, Task, SteamGuardService

### Community 75 - "GameHub Feature Parity"
Cohesion: 0.50
Nodes (5): NexaPlay Native Remake, GameHub Feature Parity, Enterprise-Clean UI Design Contract, Native Migration Strategy, NexaPlay Project Entrypoint

### Community 76 - ".AboutGameWebView_NavigationCompleted"
Cohesion: 0.40
Nodes (3): CoreWebView2NavigationCompletedEventArgs, CoreWebView2WebMessageReceivedEventArgs, WebView2

### Community 78 - "ObservableObject"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 80 - "RootCardBorder"
Cohesion: 0.50
Nodes (3): RootCardBorder, PointerRoutedEventArgs, Border

### Community 81 - "NexaPlay Wide Tile Logo"
Cohesion: 0.50
Nodes (4): NexaPlay Wide Tile Logo, Rounded Square Frame, Stylized X Emblem, Transparent Wide Tile Canvas

### Community 83 - "AppConstants"
Cohesion: 0.50
Nodes (3): string, TimeSpan, AppConstants

### Community 84 - "AppUpdateManifest"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

### Community 86 - "LibraryList"
Cohesion: 0.50
Nodes (3): LibraryList, ItemClickEventArgs, ListView

### Community 88 - "Minimalist Monochrome Branding"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

### Community 89 - "NexaPlay Square App Logo"
Cohesion: 0.67
Nodes (3): Framed X Mark, NexaPlay Square App Logo, Transparent Monochrome Icon

### Community 90 - "Gray Framed X Symbol"
Cohesion: 0.67
Nodes (3): Gray Framed X Symbol, Store Logo, Unavailable Image Placeholder

## Ambiguous Edges - Review These
- `Gray Framed X Symbol` → `Unavailable Image Placeholder`  [AMBIGUOUS]
  NexaPlay/Assets/StoreLogo.png · relation: conceptually_related_to

## Knowledge Gaps
- **135 isolated node(s):** `net8.0`, `Microsoft.NET.Sdk`, `Viewbox`, `FontIcon`, `ScaleTransform` (+130 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **24 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Gray Framed X Symbol` and `Unavailable Image Placeholder`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `IAppLogService` connect `IAppLogService` to `.Log`, `HomeViewModel`, `BypassGamesViewModel`, `SteamPlatformService`, `NexaPlayOverrideService`, `LicenseInfo`, `ListingCoverResolver`, `SteamStoreService`, `GameDetailPage`, `BypassTutorialVideoService`, `GameCoverIndexService`, `AddGameService`, `BypassGameDetailViewModel`, `GameDetailViewModel`, `MainWindow`, `BypassGameDetailPage`, `AppUpdateService`, `IWindowsDefenderService`, `LibraryViewModel`, `CoverImageCacheService`, `SettingsViewModel`, `NullLogService`, `TelegramAccountReportService`?**
  _High betweenness centrality (0.272) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `MainWindow` to `.ApplyHwndIcon`, `HomeViewModel`, `Window`, `Border`, `.OnNavChecked`, `LicenseInfo`, `IAppLogService`, `GamesViewModel`, `ListingCoverResolver`, `ObservableObject`, `.NavigateTo`, `AppUpdateCheckResult`, `INavigationService`, `ISteamService`, `NexaPlay.Presentation.ViewModels`?**
  _High betweenness centrality (0.128) - this node is a cross-community bridge._
- **Why does `GameDetailPage` connect `GameDetailPage` to `.MediaCard_Tapped`, `ScreenshotEntry`, `BypassGamesPage`, `IAppLogService`, `Page`, `.AboutGameWebView_NavigationCompleted`, `.FindSiblingSkeletonOverlay`, `IAddGameService`, `.HandleAddGameDialogAction`, `RoutedEventArgs`, `.HandleOnlineFixPrimaryActionAsync`, `GameDetailViewModel`, `NexaPlay.Presentation.ViewModels`, `.ViewModel_PropertyChanged`?**
  _High betweenness centrality (0.118) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `GamesViewModel` (e.g. with `ClearFiltersCommand` and `Get`) actually correct?**
  _`GamesViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
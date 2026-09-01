# Graph Report - NexaPlay  (2026-09-02)

## Corpus Check
- 142 files · ~114,332 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1849 nodes · 3931 edges · 112 communities (88 shown, 24 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 304 edges (avg confidence: 0.76)
- Token cost: 11,486 input · 1,653 output

## Community Hubs (Navigation)
- Metadata Catalog
- Metadata Catalog
- Home Catalog UI
- Bypass Games
- Bypass Games
- Bypass Games
- Bypass Games
- Game Catalog
- License Management
- Online Fix Flow
- App Composition
- Game Detail
- App Updates
- Cover Resolution
- Bypass Games
- Metadata Catalog
- Metadata Catalog
- Bypass Games
- Download Pipeline
- Bypass Games
- Bypass Games
- Brand Image Assets
- Bypass Games
- Bypass Games
- Navigation Shell
- Metadata Catalog
- Bypass Games
- App Updates
- Bypass Games
- Bypass Games
- Metadata Catalog
- Games Catalog UI
- Steam Integration
- License Management
- Games Catalog UI
- Game Detail
- License Management
- Librarypage
- App Updates
- Steam Integration
- Cover Image Cache
- Steam Integration
- Archive Handling
- Metadata Catalog
- Bypass Games
- Async Cancellation
- Bypass Games
- Game Detail
- Bypass Games
- Metadata Catalog
- App Updates
- Async Cancellation
- Metadata Catalog
- Navigation Shell
- Bypass Games
- 0 19041 0
- Bypass Games
- Bypass Games
- Bypass Games
- Bypass Games
- Bypass Games
- Game Detail
- Bypass Games
- Application Shell
- Web Content
- UI Converters
- Game Detail
- Bypass Games
- Bypass Games
- Game Catalog
- Bypass Games
- Bypass Games
- Steam Integration
- App Updates
- Claude Native Remake
- Game Detail
- Game Detail
- String Data Types
- Bypass Games
- Collection Types
- Brand Logo Assets
- String Data Types
- App Updates
- Bypass Games
- Collection Types
- Framework Net8 0
- Brand Logo Assets
- App Composition
- Brand Image Assets
- String Data Types
- Application Shell
- Small Batch Workflow
- Brand Logo Assets
- Brand Logo Assets
- Brand Logo Assets
- Brand Logo Assets
- YouTube Player
- Release Packaging
- Rtk Output Filtering
- Layered Service Architecture
- Web Content
- Assets Wordmark
- 200 Splash Screen
- Brand Logo Assets
- Detail Stabilization Log

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
- **Native Migration Governance** — nexaplay_agents_feature_parity, nexaplay_agents_small_batch_workflow, nexaplay_agents_ui_design_contract, nexaplay_agents_performance_contract [EXTRACTED 1.00]
- **Fast Catalog Refresh Mechanisms** — docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_listing_cover_priority, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_catalog_generation_invalidation, docs_superpowers_specs_2026_08_17_fast_catalog_refresh_design_tombstone_cache_cleanup [EXTRACTED 1.00]
- **Additive New Games Flow** — docs_superpowers_specs_2026_08_17_new_games_catalog_design_authoritative_appid_list, docs_superpowers_specs_2026_08_17_new_games_catalog_design_bounded_r2_materialization, docs_superpowers_specs_2026_08_17_new_games_catalog_design_atomic_last_known_good_publication, docs_superpowers_specs_2026_08_17_new_games_catalog_design_additive_snapshot_precedence [EXTRACTED 1.00]
- **NexaPlay Wide Tile Visual Composition** — nexaplay_assets_wide310x150logo_scale_200_stylized_x_emblem, nexaplay_assets_wide310x150logo_scale_200_rounded_square_frame, nexaplay_assets_wide310x150logo_scale_200_transparent_wide_tile_canvas [EXTRACTED 1.00]

## Communities (112 total, 24 thin omitted)

### Community 0 - "Metadata Catalog"
Cohesion: 0.07
Nodes (32): DownloadResult, IReadOnlyCollection, IEnumerable, IReadOnlyDictionary, ISet, JsonElement, JsonSerializerOptions, NewGamesCatalog (+24 more)

### Community 1 - "Metadata Catalog"
Cohesion: 0.06
Nodes (35): CancellationToken, Task, ICoverImageCacheService, CancellationToken, Task, IListingCoverResolver, CancellationToken, IProgress (+27 more)

### Community 2 - "Home Catalog UI"
Cohesion: 0.08
Nodes (28): CancellationToken, IReadOnlyList, Task, IBypassGamesDataService, CancellationToken, IProgress, Task, IOnlineFixService (+20 more)

### Community 3 - "Bypass Games"
Cohesion: 0.06
Nodes (51): AccountHeader, AppTitleBar, BypassChevron, ContentFrame, LblBypassGames, LblGames, LblHome, LblLibrary (+43 more)

### Community 4 - "Bypass Games"
Cohesion: 0.06
Nodes (25): Task, LicenseAccessDialogHelper, GamesGrid, HoverTitleLayer, Page, RootPage, Brush, DependencyObject (+17 more)

### Community 5 - "Bypass Games"
Cohesion: 0.06
Nodes (27): Border, CustomPipsContainer, HeroBypassButtonText, HeroCarousel, HoverTitleLayer, NewBypassHeroContainer, Page, PopularGamesGrid (+19 more)

### Community 6 - "Bypass Games"
Cohesion: 0.08
Nodes (21): appId, close, found, installdir, keyLine, name, DateTime, InstalledGame (+13 more)

### Community 7 - "Game Catalog"
Cohesion: 0.10
Nodes (22): CancellationToken, IReadOnlySet, Task, INexaPlayOverrideService, IReadOnlyList, NexaPlayCatalogOverride, NexaPlayDetailOverride, NexaPlayMovieOverride (+14 more)

### Community 8 - "License Management"
Cohesion: 0.10
Nodes (14): CancellationToken, Task, ILicenseService, LicensePlan, LicenseStatus, LicenseInfo, string, Task (+6 more)

### Community 9 - "Online Fix Flow"
Cohesion: 0.07
Nodes (21): IReadOnlyList, Task, IAppLogService, ConcurrentQueue, int, IReadOnlyList, string, Task (+13 more)

### Community 10 - "App Composition"
Cohesion: 0.07
Nodes (35): IServiceCollection, IServiceProvider, LaunchActivatedEventArgs, Application, CardScale, Checked, CheckedBorder, CheckStates (+27 more)

### Community 11 - "Game Detail"
Cohesion: 0.07
Nodes (29): AboutGameWebView, CompactState, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroTitleText, LeftContentPanel, MediaOverlay (+21 more)

### Community 12 - "App Updates"
Cohesion: 0.09
Nodes (18): bool, CancellationTokenSource, Dictionary, int, ObservableCollection, RelayCommand, SemaphoreSlim, string (+10 more)

### Community 13 - "Cover Resolution"
Cohesion: 0.10
Nodes (17): CancellationToken, Task, IGameCoverIndexService, CancellationToken, Task, ISteamStoreService, ListingCoverPriority, CancellationToken (+9 more)

### Community 14 - "Bypass Games"
Cohesion: 0.08
Nodes (34): BypassBtnGlow, CompactState, CoverArtHoverOverlay, CoverArtSkeleton, DetailLayoutGrid, HeroDescriptionText, HeroRowDefinition, HeroShimmerSweep (+26 more)

### Community 15 - "Metadata Catalog"
Cohesion: 0.13
Nodes (15): HasTransientError, Json, JsonArray, JsonObject, DateTime, IReadOnlyList, GameDetailEntry, MovieEntry (+7 more)

### Community 16 - "Metadata Catalog"
Cohesion: 0.08
Nodes (14): double, MediaSource, bool, CancellationTokenSource, Dictionary, int, JsonSerializerOptions, List (+6 more)

### Community 17 - "Bypass Games"
Cohesion: 0.11
Nodes (17): CancellationToken, Task, IBypassTutorialVideoService, GameCategory, BypassTutorialVideo, CancellationToken, DateTime, Dictionary (+9 more)

### Community 18 - "Download Pipeline"
Cohesion: 0.15
Nodes (12): CancellationToken, JsonElement, Task, GameCoverIndexFile, CancellationToken, Dictionary, HttpClient, JsonElement (+4 more)

### Community 19 - "Bypass Games"
Cohesion: 0.11
Nodes (10): BypassStatus, BypassProgressState, RemoveGameResult, CancellationToken, ConcurrentDictionary, HttpResponseMessage, IProgress, IReadOnlyList (+2 more)

### Community 20 - "Bypass Games"
Cohesion: 0.10
Nodes (14): Func, HttpClient, int, IReadOnlyList, RelayCommand, string, BypassGameDetailViewModel, CopySteamGuardCodeCommand (+6 more)

### Community 21 - "Brand Image Assets"
Cohesion: 0.09
Nodes (14): bool, Brush, DependencyObject, DispatcherTimer, IEnumerable, ImageSource, int, IReadOnlyList (+6 more)

### Community 22 - "Bypass Games"
Cohesion: 0.14
Nodes (5): NexaPlay.Core.Constants, NexaPlay.Infrastructure.Persistence, NexaPlay.Infrastructure.Services, NexaPlay.Core.Enums, NexaPlay.Infrastructure.Platform

### Community 23 - "Bypass Games"
Cohesion: 0.09
Nodes (16): CancellationTokenSource, Func, int, GameDetailViewModel, AddGameCommand, ApplyFixCommand, CancelFixCommand, CheckBypassCommand (+8 more)

### Community 24 - "Navigation Shell"
Cohesion: 0.10
Nodes (11): Brush, DependencyObject, ImageSource, int, NavigationEventArgs, PropertyChangedEventArgs, RoutedEventArgs, string (+3 more)

### Community 25 - "Metadata Catalog"
Cohesion: 0.13
Nodes (12): Color, bool, ContentDialog, int, Storyboard, string, Task, MainWindow (+4 more)

### Community 26 - "Bypass Games"
Cohesion: 0.10
Nodes (9): ShimmerContainer, bool, CoreWebView2WebMessageReceivedEventArgs, PointerRoutedEventArgs, PropertyChangedEventArgs, Storyboard, Uri, Visibility (+1 more)

### Community 27 - "App Updates"
Cohesion: 0.16
Nodes (11): DateTimeOffset, List, AppUpdateState, CancellationToken, HttpClient, IProgress, JsonSerializerOptions, string (+3 more)

### Community 28 - "Bypass Games"
Cohesion: 0.17
Nodes (8): NexaPlay.Contracts.Navigation, NexaPlay.Presentation.Navigation, NexaPlay, NexaPlay.Infrastructure.Logging, NexaPlay.Presentation.Helpers, NexaPlay.Presentation.ViewModels, NexaPlay.Presentation.Views.Pages, SystemRequirementItem

### Community 29 - "Bypass Games"
Cohesion: 0.15
Nodes (9): IReadOnlyList, Task, IWindowsDefenderService, AntivirusVendor, AntivirusInfo, DefenderExclusionResult, IReadOnlyList, Task (+1 more)

### Community 30 - "Metadata Catalog"
Cohesion: 0.16
Nodes (4): NexaPlay.Core.Helpers, NexaPlay.Core.Models, NexaPlay.Contracts.Services, NavigationItem

### Community 31 - "Games Catalog UI"
Cohesion: 0.12
Nodes (14): FilterOverlay, FilterOverlayTranslate, GamesGrid, GenreItemsControl, HoverTitleLayer, Page, RootPage, PointerRoutedEventArgs (+6 more)

### Community 32 - "Steam Integration"
Cohesion: 0.13
Nodes (15): CancellationTokenSource, int, IReadOnlyList, List, ObservableCollection, LibraryGameCard, LibraryViewModel, Get (+7 more)

### Community 33 - "License Management"
Cohesion: 0.19
Nodes (5): ContentDialog, RoutedEventArgs, Task, Visibility, SettingsPage

### Community 34 - "Games Catalog UI"
Cohesion: 0.22
Nodes (6): GameFilterIndex, CancellationToken, HashSet, IReadOnlyList, List, Task

### Community 35 - "Game Detail"
Cohesion: 0.19
Nodes (5): INotifyPropertyChanged, bool, ScreenshotEntry, HashSet, IReadOnlyList

### Community 36 - "License Management"
Cohesion: 0.20
Nodes (7): ActivateButton, ValidationEnterKeyButton, ValidationRetryButton, NavigationEventArgs, RoutedEventArgs, Type, Button

### Community 37 - "Librarypage"
Cohesion: 0.15
Nodes (17): BgGlowLeft, BgGlowRight, BorderGlowLeft, BorderGlowRight, LibraryRemoveConfirmMessageText, LibraryRemoveConfirmOverlay, LibraryRemoveResultMessageText, LibraryRemoveResultOverlay (+9 more)

### Community 38 - "App Updates"
Cohesion: 0.12
Nodes (15): ActivationMessage, CurrentAppVersion, DownloadProgress, DownloadProgressText, IsUpdateAvailable, LastUpdateCheckedText, LatestAppVersion, UpdateDownloadProgress (+7 more)

### Community 39 - "Steam Integration"
Cohesion: 0.22
Nodes (3): IReadOnlyList, RelayCommand, Task

### Community 40 - "Cover Image Cache"
Cohesion: 0.23
Nodes (9): CancellationToken, ConcurrentDictionary, HttpClient, SemaphoreSlim, string, Task, TimeSpan, Uri (+1 more)

### Community 41 - "Steam Integration"
Cohesion: 0.19
Nodes (5): IReadOnlyList, Task, SettingsViewModel, Get, NavigationEventArgs

### Community 42 - "Archive Handling"
Cohesion: 0.14
Nodes (15): Lazy Rich Detail Flow, Runtime Catalog Precedence, Fast Catalog Refresh Implementation Plan, New Games Catalog Implementation Plan, Catalog Generation Invalidation, Listing Cover Priority, Tombstone Cache Cleanup, Additive Snapshot Precedence (+7 more)

### Community 43 - "Metadata Catalog"
Cohesion: 0.16
Nodes (7): FrameworkElement, Image, HeroImage, MediaOverlayImage, ExceptionRoutedEventArgs, Image, TranslateTransform

### Community 44 - "Bypass Games"
Cohesion: 0.16
Nodes (5): OfflineActivationCheckbox, TutorialVideoOverlayCloseBtn, RoutedEventArgs, Button, CheckBox

### Community 45 - "Async Cancellation"
Cohesion: 0.15
Nodes (8): HttpMessageHandler, HttpRequestMessage, CancellationToken, HttpResponseMessage, IReadOnlyList, Task, NullLogService, StatusCodeHandler

### Community 46 - "Bypass Games"
Cohesion: 0.15
Nodes (10): CancellationToken, Task, IAccountReportService, AccountReportResult, CancellationToken, HttpClient, string, Task (+2 more)

### Community 47 - "Game Detail"
Cohesion: 0.20
Nodes (5): CancellationToken, IProgress, Task, IAddGameService, Task

### Community 48 - "Bypass Games"
Cohesion: 0.19
Nodes (6): BypassSubmenuSteam, BypassSubmenuThirdParty, SidebarShell, PointerRoutedEventArgs, RadioButton, TextBlock

### Community 49 - "Metadata Catalog"
Cohesion: 0.19
Nodes (5): Message, ICatalogRefreshState, long, CatalogRefreshState, Success

### Community 50 - "App Updates"
Cohesion: 0.27
Nodes (7): CancellationToken, IProgress, Task, IAppUpdateService, DateTimeOffset, IReadOnlyList, AppUpdateCheckResult

### Community 51 - "Async Cancellation"
Cohesion: 0.24
Nodes (8): CancellationToken, Dictionary, int, SemaphoreSlim, string, Task, AccountReportLimitStore, ReportLimitState

### Community 53 - "Navigation Shell"
Cohesion: 0.18
Nodes (6): Frame, Type, INavigationService, Frame, Type, NavigationService

### Community 54 - "Bypass Games"
Cohesion: 0.21
Nodes (5): CancellationToken, IReadOnlyList, Task, ISteamService, SteamFinalizeResult

### Community 56 - "0 19041 0"
Cohesion: 0.18
Nodes (9): net8.0-windows10.0.19041.0, Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), MailKit (4.16.0), Microsoft.Extensions.DependencyInjection (10.0.8), Microsoft.Windows.SDK.BuildTools (10.0.28000.1839), Microsoft.WindowsAppSDK (1.6.241114003), SharpCompress (0.48.1) (+1 more)

### Community 57 - "Bypass Games"
Cohesion: 0.22
Nodes (4): CoverArtImage, HeroImage, ExceptionRoutedEventArgs, Image

### Community 58 - "Bypass Games"
Cohesion: 0.24
Nodes (3): ContentDialogButtonClickEventArgs, ContentDialog, Task

### Community 59 - "Bypass Games"
Cohesion: 0.24
Nodes (5): fileName, meta, FixFile, List, path

### Community 61 - "Bypass Games"
Cohesion: 0.39
Nodes (3): Action, HttpResponseMessage, Task

### Community 62 - "Game Detail"
Cohesion: 0.25
Nodes (4): DenuvoBadge, DenuvoBadgeGlowOverlay, PropertyChangedEventArgs, Border

### Community 64 - "Application Shell"
Cohesion: 0.48
Nodes (3): AppWindow, DllImport, IntPtr

### Community 65 - "Web Content"
Cohesion: 0.38
Nodes (4): CoreWebView2Environment, Lazy, Task, WebView2EnvironmentHelper

### Community 66 - "UI Converters"
Cohesion: 0.33
Nodes (4): NexaPlay.Presentation.Converters, IValueConverter, Type, BoolToVisibilityConverter

### Community 67 - "Game Detail"
Cohesion: 0.29
Nodes (3): DispatcherQueueTimer, TappedRoutedEventArgs, Uri

### Community 70 - "Game Catalog"
Cohesion: 0.33
Nodes (3): CatalogCacheEnvelope, CatalogCacheStamp, CatalogSourceStamp

### Community 71 - "Bypass Games"
Cohesion: 0.48
Nodes (6): NavBypass, NavGames, NavHome, NavLibrary, NavSettings, RadioButton

### Community 72 - "Bypass Games"
Cohesion: 0.33
Nodes (3): CoreWebView2, TutorialThumbnailOverlay, TappedRoutedEventArgs

### Community 73 - "Steam Integration"
Cohesion: 0.40
Nodes (3): Action, Task, SteamGuardService

### Community 75 - "Claude Native Remake"
Cohesion: 0.50
Nodes (5): NexaPlay Native Remake, GameHub Feature Parity, Enterprise-Clean UI Design Contract, Native Migration Strategy, NexaPlay Project Entrypoint

### Community 76 - "Game Detail"
Cohesion: 0.40
Nodes (3): CoreWebView2NavigationCompletedEventArgs, CoreWebView2WebMessageReceivedEventArgs, WebView2

### Community 78 - "String Data Types"
Cohesion: 0.40
Nodes (3): string, MainViewModel, ObservableObject

### Community 80 - "Collection Types"
Cohesion: 0.50
Nodes (3): RootCardBorder, PointerRoutedEventArgs, Border

### Community 81 - "Brand Logo Assets"
Cohesion: 0.50
Nodes (4): NexaPlay Wide Tile Logo, Rounded Square Frame, Stylized X Emblem, Transparent Wide Tile Canvas

### Community 83 - "String Data Types"
Cohesion: 0.50
Nodes (3): string, TimeSpan, AppConstants

### Community 84 - "App Updates"
Cohesion: 0.50
Nodes (3): DateTimeOffset, List, AppUpdateManifest

### Community 85 - "Bypass Games"
Cohesion: 0.50
Nodes (4): BypassSubmenuPanel, NavItemsPanel, ValidationErrorPanel, StackPanel

### Community 86 - "Collection Types"
Cohesion: 0.50
Nodes (3): LibraryList, ItemClickEventArgs, ListView

### Community 88 - "Brand Logo Assets"
Cohesion: 1.00
Nodes (3): Minimalist Monochrome Branding, NexaPlay Logo, Stylized N Monogram

### Community 89 - "App Composition"
Cohesion: 0.67
Nodes (3): Framed X Mark, NexaPlay Square App Logo, Transparent Monochrome Icon

### Community 90 - "Brand Image Assets"
Cohesion: 0.67
Nodes (3): Gray Framed X Symbol, Store Logo, Unavailable Image Placeholder

### Community 92 - "Application Shell"
Cohesion: 0.67
Nodes (3): ContentTopBarRow, WindowTopRow, RowDefinition

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
- **Why does `IAppLogService` connect `Online Fix Flow` to `Metadata Catalog`, `Metadata Catalog`, `Home Catalog UI`, `Bypass Games`, `Game Catalog`, `License Management`, `Cover Resolution`, `Metadata Catalog`, `Metadata Catalog`, `Bypass Games`, `Download Pipeline`, `Bypass Games`, `Bypass Games`, `Bypass Games`, `Metadata Catalog`, `Bypass Games`, `App Updates`, `Bypass Games`, `Steam Integration`, `Cover Image Cache`, `Steam Integration`, `Async Cancellation`, `Bypass Games`?**
  _High betweenness centrality (0.272) - this node is a cross-community bridge._
- **Why does `MainWindow` connect `Metadata Catalog` to `Application Shell`, `Metadata Catalog`, `Bypass Games`, `License Management`, `Bypass Games`, `License Management`, `Online Fix Flow`, `App Updates`, `Cover Resolution`, `String Data Types`, `Bypass Games`, `App Updates`, `Navigation Shell`, `Bypass Games`, `Bypass Games`?**
  _High betweenness centrality (0.128) - this node is a cross-community bridge._
- **Why does `GameDetailPage` connect `Metadata Catalog` to `Game Detail`, `Game Detail`, `Bypass Games`, `Online Fix Flow`, `Game Detail`, `Game Detail`, `Metadata Catalog`, `Game Detail`, `Game Detail`, `Metadata Catalog`, `Online Fix Flow`, `Bypass Games`, `Bypass Games`, `Game Detail`?**
  _High betweenness centrality (0.118) - this node is a cross-community bridge._
- **Are the 11 inferred relationships involving `GameDetailViewModel` (e.g. with `AddGameCommand` and `ApplyFixCommand`) actually correct?**
  _`GameDetailViewModel` has 11 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `BypassGameDetailViewModel` (e.g. with `CopySteamGuardCodeCommand` and `CopySteamPasswordCommand`) actually correct?**
  _`BypassGameDetailViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `GamesViewModel` (e.g. with `ClearFiltersCommand` and `Get`) actually correct?**
  _`GamesViewModel` has 8 INFERRED edges - model-reasoned connections that need verification._
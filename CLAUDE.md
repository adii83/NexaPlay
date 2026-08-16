# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository scope

- `NexaPlay/` is the active Windows desktop application: a native WinUI 3 remake of GameHub targeting `.NET 8`.
- `gamehub/` is the local legacy reference. Read it to recover behavior and edge cases, but port intent into native C#/XAML; do not restore its WebView/HTML/JS-bridge architecture.
- Preserve GameHub feature parity. Work one page or small behavior batch at a time and build after each batch.
- Product terminology is `Home` and user-facing `Bypass Games`; older `Fix Games` names may remain as migration debt.

Before changing code, read these in order:

1. `NexaPlay/README.md`
2. `NexaPlay/AGENTS.md`
3. `NexaPlay/ONBOARDING_ZERO_TO_PARITY.md`
4. `NexaPlay/MIGRATION_PARITY_MATRIX.md`
5. `NexaPlay/AI_HANDOFF_PROMPT.md`
6. `NexaPlay/AI_HANDOFF_HOME_HISTORY.md`

The parity matrix is the feature-status source of truth. Add a checkpoint immediately above `## 10. Update Log Ringkas` in `NexaPlay/AI_HANDOFF_PROMPT.md` after an important batch.

## Commands

Run commands from the repository root in PowerShell.

### Required Debug x64 build gate

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  '.\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64
```

If the normal output is locked by a running `NexaPlay.exe`, either close it or validate into a separate directory:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  '.\NexaPlay\NexaPlay.csproj' /restore /p:Configuration=Debug /p:Platform=x64 `
  /p:OutDir="$PWD\NexaPlay\bin\x64\Debug-preview\"
```

### Build and run with crash capture

```powershell
.\run_nexaplay.bat
```

This script force-closes an existing `NexaPlay.exe`, builds Debug x64, launches the app, waits for exit, and writes crash context to the repository root. For interactive UI development, F5 in Visual Studio is the normal smoke-test path.

### Release publish

From `NexaPlay/`:

```powershell
dotnet publish .\NexaPlay.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true
```

Release packaging and manifest ordering are documented in `NexaPlay/release/README.md`. Release builds deliberately keep trimming disabled because current WinUI/JSON runtime paths are not trim-safe.

### Tests and lint

There is currently no test project, test SDK, dedicated lint command, or repository analyzer configuration. Therefore:

- the Debug x64 build is the available compile/static gate;
- there is no valid single-test command yet;
- runtime changes require an app smoke test (launch, sidebar navigation, and the changed flow).

If a test project is added later, document both the full-suite and filtered single-test commands here using that project's actual framework.

## Architecture

### Layering and composition

- `NexaPlay/Core/` contains domain models, enums, helpers, and constants.
- `NexaPlay/Contracts/` contains service and navigation interfaces. Presentation code should depend on these contracts, not concrete infrastructure.
- `NexaPlay/Infrastructure/` owns HTTP, filesystem, Steam/Windows integration, persistence, logging, licensing, update, metadata, and bypass implementations.
- `NexaPlay/Presentation/` contains CommunityToolkit.Mvvm ViewModels, WinUI pages, converters, helpers, and `NavigationService`.
- `NexaPlay/App.xaml.cs` is the DI composition root. Infrastructure and navigation services are singletons; `HomeViewModel` and `GamesViewModel` are singletons so startup preloading and page instances share state; most other page ViewModels are transient.

ViewModels orchestrate UI state and commands. Filesystem, network, registry/platform, archive, and Steam operations belong behind interfaces in `Contracts/` and implementations in `Infrastructure/`.

### Startup, shell, and navigation

`App.OnLaunched` resolves `MainWindow`. `MainWindow` owns the startup sequence: license validation/activation, metadata and cover-index warmup, Home/Games card preload, navigation to Home, then update prompting. It also owns sidebar state and switches to immersive chrome for `GameDetailPage` and `BypassGameDetailPage`.

Top-level sidebar navigation uses `MainWindow.ContentFrame` directly. Navigation initiated inside pages/ViewModels uses `INavigationService`, which is initialized with that same frame. Pages resolve their ViewModels from `App.GetRequiredService<T>()` and set `DataContext` where needed.

Navigation parameters are contracts between caller and destination:

- `GameDetailPage` receives an `int appId`.
- `BypassGameDetailPage` accepts either `int appId` or `(int appId, FixEntry? selectedEntry)`; the tuple preserves the exact card/category source and its premium/status fields.
- `BypassGamesPage` receives category strings such as `all` and `steam-sharing`.

Keep XAML and code-behind element contracts synchronized. WinUI parser/binding faults can surface as fatal `0xC000027B` failures rather than ordinary managed exceptions.

### Metadata and cached data flow

`IMetadataService`/`MetadataService` provide the lightweight catalog used by Home and Games. Runtime catalog precedence is:

1. `steam_data.json.gz`
2. `steam_data.json`
3. `override_data.json`
4. sparse NexaPlay catalog overrides

Protection/Denuvo membership is additionally derived from bypass source lists. Rich detail is lazy: `ISteamStoreService` fetches per-AppID JSON from the NexaPlay metadata endpoint, caches it for seven days, then applies sparse detail overrides. Home/Games use the lightweight cover index and disk image cache rather than fetching rich detail for every card.

Most mutable state and downloaded data live under `%LOCALAPPDATA%\NexaPlay`, including runtime catalog sources, detail metadata, covers, license data, applied state, update state/downloads, and logs. Clearing or changing one cache may require coordinating the corresponding service and Settings reset flow.

### Game and bypass flows

- `AddGameService` ports GameHub's Add/Remove script behavior and detects the Library through Steam `config\stplug-in\*.lua` files.
- `OnlineFixService`, `BypassGamesDataService`, Steam/Defender platform services, and persistence stores implement bypass actions and applied state.
- `GameDetailViewModel` and `BypassGameDetailViewModel` coordinate these services, cancellation, progress, license gates, and dialogs; page code-behind remains responsible for WinUI-only concerns such as pickers, animations, WebView2 rendering, and visual dialog callbacks.

Keep long-running work async and cancellable, throttle progress updates, avoid heavy constructor/startup work, and preserve virtualization/paging for the large Games catalog.

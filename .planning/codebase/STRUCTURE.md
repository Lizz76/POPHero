# Structure

**Analysis Date:** 2026-04-09

## Repository Layout

**Root directories:**
- `Assets/` - Unity scene, scripts, and runtime game content.
- `Packages/` - UPM dependency declarations and resolved lockfile.
- `ProjectSettings/` - Editor and engine settings, build scenes, package registry config.
- `docs/` - Maintainer-facing handoff notes and development log.
- `.codex/` - Repo-local Codex MCP configuration.
- `Library/`, `Logs/`, `Temp/`, `UserSettings/` - Unity-generated local state, not source-controlled artifacts.

## Key Runtime Content

**Scene entry:**
- `Assets/Scenes/SampleScene.unity` - Only enabled scene in `ProjectSettings/EditorBuildSettings.asset`.

**Scripts root:**
- `Assets/Scripts/POPHero/` - Entire gameplay code lives under a single namespace tree.

## Script Directory Breakdown

**Core (`Assets/Scripts/POPHero/Core/`):**
- `PopHeroBootstrap.cs` - Scene bootstrap hook.
- `PopHeroGame.cs` - Composition root and orchestration center.
- `PopHeroPrototypeConfig.cs` - Typed runtime config schema.
- `GameContracts.cs` - UI, combat, and service-facing interfaces.
- `GameplayTypes.cs` - Enums and mutable runtime data types.

**Flow (`Assets/Scripts/POPHero/Flow/`):**
- `GameFlowControllers.cs` - Phase/state facades plus flow controllers.
- `RoundController.cs` - Round-scoring, sticker state, and round resolution.
- `RoundResolveResult.cs` - Result payload for round completion.

**Combat (`Assets/Scripts/POPHero/Combat/`):**
- `PlayerLauncher.cs` - Input-to-launch bridge.
- `AimInputStrategies.cs` - PC/mobile aiming strategy variants.
- `AimStateController.cs` - Aim lock behavior and thresholds.
- `TrajectoryPredictor.cs` - Preview path generation.
- `BounceStepSolver.cs` - Shared bounce/corner/recovery logic.
- `BallController.cs` - Real projectile movement and hit processing.
- `ArenaSurfaceMarker.cs` - Wall/bottom tagging for collision meaning.

**Board (`Assets/Scripts/POPHero/Board/`):**
- `BoardManager.cs` - Public board facade.
- `BoardServices.cs` - Internal services for block collection, rewards, and runtime board generation.
- `BoardBlock.cs` plus typed subclasses - Runtime block visuals and hit effects.

**Characters (`Assets/Scripts/POPHero/Characters/`):**
- `PlayerData.cs`, `EnemyData.cs` - Runtime combatants.
- `PlayerPresenter.cs`, `EnemyController.cs` - Character-side presentation.

**Systems (`Assets/Scripts/POPHero/Systems/`):**
- `StickerCatalog.cs` - Sticker data definitions and factory behavior.
- `StickerExecution.cs`, `StickerFlow.cs`, `StickerRuntime.cs` - Sticker runtime logic.
- `ModShopSystems.cs` - Global mod system, growth rewards, and shop.
- `BuffManager.cs` - Legacy compatibility placeholder only.

**UI (`Assets/Scripts/POPHero/UI/`):**
- `PopHeroHud.cs` - IMGUI gameplay UI and menus.
- `HudPresenters.cs` - Presenter/view-model helpers for HUD sections.
- `PrototypeVisualFactory.cs` - Runtime-generated sprites, text, and visual helpers.

## Non-Code Project Files

**Package management:**
- `Packages/manifest.json` - Direct dependency declarations.
- `Packages/packages-lock.json` - Fully resolved dependency graph and registries.

**Project settings:**
- `ProjectSettings/ProjectVersion.txt` - Editor version.
- `ProjectSettings/EditorBuildSettings.asset` - Enabled scenes.
- `ProjectSettings/PackageManagerSettings.asset` - UPM registry settings.

**Developer docs:**
- `README.md` - Lightweight prototype overview and play instructions.
- `docs/AI_HANDOFF.md` - Maintainer and AI onboarding reference.
- `docs/DEVLOG_2026-03-31.md` - Snapshot of a prior gameplay iteration.

## Notable Structural Characteristics

**Runtime-built instead of scene-authored:**
- `PopHeroGame` creates the camera, board, ball, enemy layer, HUD, and supporting runtime objects in code.
- There are very few committed authored assets beyond the scene and scripts.

**Single-scene prototype:**
- The project currently pivots around one scene, one bootstrap path, and one main runtime graph.

**Config gap:**
- `PopHeroGame` looks for `Resources/PopHeroPrototypeConfig`, but `Assets/Resources/` is currently empty.
- That means the code structure anticipates an asset-backed config, but the checkout behaves more like a code-default prototype.

**Generated .NET solution files are absent:**
- No committed `*.sln` or `*.csproj` are present even though docs mention `dotnet build`.
- This structure implies Unity-generated project files are intentionally local-only.

## Maintenance Landmarks

These files are the fastest way to reorient when returning to the repo:
- `docs/AI_HANDOFF.md` - Best summary of intended ownership boundaries.
- `Assets/Scripts/POPHero/Core/PopHeroGame.cs` - Top-level gameplay orchestration.
- `Assets/Scripts/POPHero/Board/BoardServices.cs` - Board/build data model behavior.
- `Assets/Scripts/POPHero/Systems/StickerExecution.cs` and `Assets/Scripts/POPHero/Systems/StickerFlow.cs` - Sticker behavior pipeline.
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` - Player-facing UI surface and debug controls.

---
*Structure analysis: 2026-04-09*
*Update after major file moves, scene additions, or subsystem extraction*

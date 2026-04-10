# Architecture

**Analysis Date:** 2026-04-09

## Pattern Overview

**Overall:** Runtime-composed Unity prototype with a composition-root `MonoBehaviour`, service/facade subsystems, and in-memory session state.

**Key Characteristics:**
- Scene starts nearly empty and is assembled at runtime from code.
- `PopHeroGame` acts as the composition root and high-level orchestrator.
- Board, combat, stickers, mods, rewards, and UI are split into subsystems, but some roots are still large.
- Gameplay state is session-local and rebuilt on each run; there is no persistence layer.

## Layers

**Bootstrap Layer:**
- Purpose: Enter the gameplay stack after scene load.
- Contains: `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs`.
- Depends on: Unity scene lifecycle only.
- Used by: `Assets/Scenes/SampleScene.unity`.

**Composition / Application Layer:**
- Purpose: Wire subsystems together, bridge Unity lifecycle, coordinate state transitions.
- Contains: `Assets/Scripts/POPHero/Core/PopHeroGame.cs`, `Assets/Scripts/POPHero/Flow/GameFlowControllers.cs`.
- Depends on: All lower layers.
- Used by: Runtime-created systems, HUD, presenters, and input.

**Domain State and Contracts Layer:**
- Purpose: Define shared game state, enums, contracts, and round-resolution rules.
- Contains: `Assets/Scripts/POPHero/Core/GameContracts.cs`, `Assets/Scripts/POPHero/Core/GameplayTypes.cs`, `Assets/Scripts/POPHero/Flow/RoundController.cs`, `Assets/Scripts/POPHero/Flow/RoundResolveResult.cs`, `Assets/Scripts/POPHero/Characters/PlayerData.cs`, `Assets/Scripts/POPHero/Characters/EnemyData.cs`.
- Depends on: Minimal Unity primitives plus domain models.
- Used by: Combat, board, systems, and UI layers.

**Subsystem Service Layer:**
- Purpose: Own focused gameplay domains behind facades or helper services.
- Contains:
  - `Assets/Scripts/POPHero/Board/BoardManager.cs`
  - `Assets/Scripts/POPHero/Board/BoardServices.cs`
  - `Assets/Scripts/POPHero/Combat/BounceStepSolver.cs`
  - `Assets/Scripts/POPHero/Systems/StickerCatalog.cs`
  - `Assets/Scripts/POPHero/Systems/StickerExecution.cs`
  - `Assets/Scripts/POPHero/Systems/StickerFlow.cs`
  - `Assets/Scripts/POPHero/Systems/StickerRuntime.cs`
  - `Assets/Scripts/POPHero/Systems/ModShopSystems.cs`
- Depends on: Domain state/contracts and Unity runtime types.
- Used by: `PopHeroGame` and presentation components.

**Presentation Layer:**
- Purpose: Present world actors, HUD panels, tooltips, and runtime visuals.
- Contains:
  - `Assets/Scripts/POPHero/Characters/EnemyController.cs`
  - `Assets/Scripts/POPHero/Characters/PlayerPresenter.cs`
  - `Assets/Scripts/POPHero/UI/PopHeroHud.cs`
  - `Assets/Scripts/POPHero/UI/HudPresenters.cs`
  - `Assets/Scripts/POPHero/UI/PrototypeVisualFactory.cs`
- Depends on: Read models, command sinks, Unity GUI APIs.
- Used by: Player-facing runtime only.

## Data Flow

**Startup Flow:**
1. Unity opens `Assets/Scenes/SampleScene.unity`.
2. `PopHeroBootstrap` checks whether a `PopHeroGame` exists.
3. If not, it creates a `POPHeroGame` object and attaches `PopHeroGame`.
4. `PopHeroGame.Awake()` loads or creates `PopHeroPrototypeConfig`, caches arena rects, sets up the camera, builds runtime objects, and starts the prototype flow.
5. The game begins at main menu state and waits for HUD commands.

**Combat Loop:**
1. HUD command starts a session through `GameSessionController`.
2. `PopHeroGame` initializes player state, board state, enemy progression, and launch state.
3. `PlayerLauncher` plus `AimInputStrategies` and `AimStateController` collect aim input.
4. `TrajectoryPredictor` and `BounceStepSolver` preview the path.
5. `BallController` advances the real trajectory using the same solver concepts.
6. `RoundController` accumulates attack, shield, hit, sticker, and token state.
7. When the ball returns, `RoundController.ResolveRound()` produces a `RoundResolveResult`.
8. `PopHeroGame` plays presentation feedback, applies defeat/win checks, and either loops combat or enters intermission.

**Intermission Loop:**
1. Enemy defeat triggers block rewards, sticker/mod/growth rewards, shop, and loadout phases.
2. `BoardManager` and `BoardServices` manage active/reserve cards and reward claims.
3. `StickerInventory`, `ModManager`, and `ShopManager` mutate the player's build state.
4. HUD commands feed back through `IHudCommandSink` to advance the session.
5. The next enemy encounter starts with the updated deck/build state.

**State Management:**
- Session state is centralized in `PopHeroGame`, `RoundController`, `PlayerData`, `EnemyData`, and collection objects like `PlayerBlockCollection`.
- There is no file save/load, server sync, or deterministic replay layer.

## Key Abstractions

**Composition Root:**
- Purpose: Own runtime assembly and orchestration.
- Example: `Assets/Scripts/POPHero/Core/PopHeroGame.cs`.
- Pattern: Large orchestration object with Unity lifecycle hooks plus facade-style methods.

**Facade + Service Split:**
- Purpose: Preserve a stable public surface while moving logic into smaller internal services.
- Examples: `BoardManager` over `BlockCollectionService`, `BlockRewardService`, and `RuntimeBoardService`; service facades in `Assets/Scripts/POPHero/Flow/GameFlowControllers.cs`.
- Pattern: Public facade backed by focused internal classes.

**Read Model / Command Sink:**
- Purpose: Decouple UI reads from gameplay writes.
- Examples: `IGameReadModel` and `IHudCommandSink` in `Assets/Scripts/POPHero/Core/GameContracts.cs`.
- Pattern: UI reads state through interfaces and writes via explicit commands.

**Event Hub:**
- Purpose: Broadcast combat events for sticker and system reactions.
- Example: `CombatEventHub` in `Assets/Scripts/POPHero/Core/GameContracts.cs`.
- Pattern: In-process pub/sub without async messaging.

**Runtime-authored card/build state:**
- Purpose: Treat blocks, stickers, mods, and shop entries as mutable session objects.
- Examples: `BlockCardState`, `StickerInstance`, `ModInstance`, `ShopItemEntry` in `Assets/Scripts/POPHero/Core/GameplayTypes.cs` and `Assets/Scripts/POPHero/Systems/`.
- Pattern: Data bags plus service-driven mutation.

## Entry Points

**Gameplay bootstrap:**
- Location: `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs`
- Trigger: Unity runtime load after scene load.
- Responsibilities: Ensure the game root exists.

**Main composition root:**
- Location: `Assets/Scripts/POPHero/Core/PopHeroGame.cs`
- Trigger: Unity lifecycle (`Awake`, `Update`).
- Responsibilities: Configure runtime, build subsystems, manage state transitions, own orchestration.

**HUD rendering/input:**
- Location: `Assets/Scripts/POPHero/UI/PopHeroHud.cs`
- Trigger: Unity `OnGUI()`.
- Responsibilities: Render menus, combat HUD, rewards, shop, and block management; emit commands.

## Error Handling

**Strategy:** Defensive guard clauses and `bool` + `out failReason` flows instead of exception-driven domain logic.

**Patterns:**
- Invalid player actions return `false` with a user-facing reason, especially in `BoardServices.cs` and `ModShopSystems.cs`.
- Runtime flow methods early-return when state is invalid or the game is in the wrong phase.
- There is little centralized top-level error reporting beyond gameplay state gating.

## Cross-Cutting Concerns

**Configuration:**
- Centralized in `PopHeroPrototypeConfig`, but currently loaded from a missing-or-fallback `Resources` asset path.

**Debugging:**
- Debug controls are surfaced directly in `PopHeroHud`.
- Debug trajectory visualization is built into `BallController`.

**Localization / text:**
- Player-facing strings are largely Chinese.
- Some files show encoding corruption, so text quality and source encoding are an architectural maintenance concern.

**Documentation handoff:**
- `docs/AI_HANDOFF.md` is effectively part of the working architecture because it documents intended ownership boundaries and anti-patterns for future edits.

---
*Architecture analysis: 2026-04-09*
*Update when the composition root, state ownership, or UI architecture changes materially*

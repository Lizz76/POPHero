<!-- GSD:project-start source:PROJECT.md -->
## Project

**POPHero**

POPHero is a Unity 2022.3 single-scene prototype for a marble-trajectory roguelike combat loop. The player aims and launches one ball, converts bounce hits into round-scoped damage and shield, then moves through block rewards, sticker/mod growth, shop, and loadout management between enemies. This GSD project is being initialized as a brownfield maintenance workspace for the existing prototype rather than a greenfield rebuild.

**Core Value:** Each run should make a bounced path feel strategically meaningful and worth iterating on.

### Constraints

- **Tech stack**: Unity 2022.3.62f2c1 + C# — project settings pin this editor/runtime baseline.
- **Architecture**: Runtime-composed scene graph and IMGUI HUD — major changes must respect this current shape before replacing it.
- **Maintenance invariant**: Bounce preview and live flight should continue sharing one solver path — avoid split fixes across preview/runtime implementations.
- **Build environment**: `*.sln` and `*.csproj` are gitignored/generated locally — CLI validation depends on local Unity regeneration.
- **Text quality**: Player-facing copy is Chinese, but some source strings already show encoding damage — text edits need extra care.
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# - All gameplay, UI, and runtime assembly code under `Assets/Scripts/POPHero/`.
- YAML - Unity project settings and scene metadata in `ProjectSettings/*.asset` and `Assets/Scenes/SampleScene.unity`.
- JSON - Package manifests and lockfiles in `Packages/manifest.json` and `Packages/packages-lock.json`.
- TOML - Codex MCP configuration in `.codex/config.toml`.
- Markdown - Project-facing docs in `README.md` and `docs/*.md`.
## Runtime
- Unity 2022.3.62f2c1 - Declared in `ProjectSettings/ProjectVersion.txt` and used as the gameplay runtime/editor baseline.
- Unity 2D physics runtime - `Rigidbody2D`, `CircleCollider2D`, `Collision2D`, and custom bounce solving drive combat in `Assets/Scripts/POPHero/Combat/BallController.cs` and `Assets/Scripts/POPHero/Combat/BounceStepSolver.cs`.
- In-memory session state - No save system or backend persistence is present; session state lives in `PopHeroGame`, `PlayerData`, `EnemyData`, and `PlayerBlockCollection`.
- Unity Package Manager (UPM) - Dependency declarations live in `Packages/manifest.json`.
- Lockfile present - `Packages/packages-lock.json` captures resolved package versions and registries.
- Registries:
## Frameworks
- UnityEngine / MonoBehaviour - Main application framework for all gameplay entry points such as `Assets/Scripts/POPHero/Core/PopHeroGame.cs`.
- Unity 2D feature set - Enabled through `com.unity.feature.2d` in `Packages/manifest.json`.
- IMGUI - The primary gameplay HUD is hand-authored in `Assets/Scripts/POPHero/UI/PopHeroHud.cs`.
- Unity Test Framework 1.1.33 - Installed via `com.unity.test-framework`, but no committed test assemblies or test folders were found under `Assets/`.
- NUnit extension 1.0.6 - Brought in transitively for Unity tests.
- Rider integration 3.0.36 - `com.unity.ide.rider`.
- Visual Studio integration 2.0.22 - `com.unity.ide.visualstudio`.
- Unity MCP package 0.61.0 - `com.ivanmurzak.unity.mcp`, paired with local Codex MCP config in `.codex/config.toml`.
## Key Dependencies
- `com.unity.feature.2d` 2.0.1 - Provides the 2D feature bundle this prototype is built around.
- `com.unity.test-framework` 1.1.33 - Makes edit mode / play mode tests possible even though they are not yet authored.
- `com.unity.textmeshpro` 3.0.7 - Available in the project, though the current runtime HUD still leans on IMGUI and `TextMesh`.
- `com.ivanmurzak.unity.mcp` 0.61.0 - Adds AI/MCP integration capabilities for editor-side tooling.
- `com.unity.collab-proxy` 2.12.4 - Version control/collaboration support package.
- `com.unity.visualscripting` 1.9.4 - Installed, though no gameplay code currently depends on visual scripting assets.
- `com.unity.ugui` 1.0.0 - Installed, but the observed runtime UI is not Canvas-driven.
## Configuration
- Project version is pinned in `ProjectSettings/ProjectVersion.txt`.
- Package sources are configured in `Packages/manifest.json` and `ProjectSettings/PackageManagerSettings.asset`.
- Local AI tooling is configured in `.codex/config.toml`.
- Typed runtime knobs live in `Assets/Scripts/POPHero/Core/PopHeroPrototypeConfig.cs`.
- `PopHeroGame` attempts to load `Resources/PopHeroPrototypeConfig` from `Assets/Resources`, then falls back to `CreateRuntimeDefault()` if no asset exists.
- `Assets/Resources/` is currently empty, so the fallback path appears to be the active configuration path in this checkout.
## Platform Requirements
- Unity Editor 2022.3.62f2c1 is the declared baseline in project settings.
- Windows appears to be the primary authoring environment:
- IDE support is expected through Rider or Visual Studio packages.
- A standalone game release pipeline is not defined in-repo.
- The current project behaves as a local prototype started from `Assets/Scenes/SampleScene.unity`.
- No cloud deployment, backend hosting, or external live service runtime was observed.
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming and Organization
- Gameplay code consistently uses the `POPHero` namespace.
- Scripts are grouped by domain responsibility under `Assets/Scripts/POPHero/`:
- `*Controller` usually coordinates stateful behavior (`RoundController`, `AimStateController`, `EnemyController`).
- `*Manager` usually owns a subsystem facade (`BoardManager`, `ModManager`, `ShopManager`).
- `*Presenter` converts state into UI or world-space presentation (`PlayerPresenter`, HUD presenters).
- `*Service` or facade classes encapsulate focused logic behind a thin public wrapper (`BlockCollectionService`, `RuntimeBoardServiceFacade`).
## Code Style
- Methods frequently early-return on invalid state rather than nesting deeply.
- Examples: `PopHeroGame`, `RoundController`, `BoardManager`, `ShopManager`.
- `Initialize(...)` for one-time setup.
- `Build*` for runtime object creation or catalog population.
- `Refresh*` for presentation recomputation.
- `Try*` for operations that can fail without throwing.
- `Handle*` for orchestration callbacks and event responses.
- Domain mutations commonly use `bool` plus `out string failReason` instead of throwing exceptions.
- This is especially common in `Assets/Scripts/POPHero/Board/BoardServices.cs` and `Assets/Scripts/POPHero/Systems/ModShopSystems.cs`.
## State Modeling
- Many gameplay entities are mutable classes with public fields rather than strict property encapsulation.
- Examples: `BlockCardState`, `SocketSlotState`, `RewardChoiceEntry`, `ShopItemEntry`, `ModData`.
- UI reads through `IGameReadModel`.
- UI writes through `IHudCommandSink` and `HudCommand`.
- This keeps `PopHeroHud` from directly mutating every subsystem, even though the HUD still contains significant logic.
- Phase and content types are strongly enum-based:
- State transitions are explicit and centralized rather than string-based.
## Composition Conventions
- `PopHeroGame.BuildPrototype()` creates objects and attaches components in code.
- Dependencies are passed explicitly via `Initialize(...)` calls rather than serialized inspector references.
- Runtime components usually receive `PopHeroGame` or a context object during setup.
- `BoardServices.cs` uses a shared `BoardManagerContext` object to avoid passing many parameters around.
- Public APIs are kept stable while detailed logic moves into internal services.
- `docs/AI_HANDOFF.md` explicitly recommends extending services first and pruning facades later.
## Gameplay Rules Conventions
- Round math belongs in `Assets/Scripts/POPHero/Flow/RoundController.cs`.
- Bounce/path logic belongs in `Assets/Scripts/POPHero/Combat/BounceStepSolver.cs`.
- Block inventory/reward/runtime generation belongs in `Assets/Scripts/POPHero/Board/BoardServices.cs`.
- Sticker logic should prefer the sticker runtime/execution stack instead of embedding sticker IDs into round logic.
- `Assets/Scripts/POPHero/Systems/BuffManager.cs` is intentionally a compatibility shell.
- New progression/features should favor `Sticker`, `Mod`, and `Shop` flows, matching `docs/AI_HANDOFF.md`.
## UI Conventions
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` renders menus, combat HUD, intermission panels, and tooltips with IMGUI.
- HUD sections are gradually being moved into presenter-generated models in `Assets/Scripts/POPHero/UI/HudPresenters.cs`.
- Runtime sprites and text should go through `Assets/Scripts/POPHero/UI/PrototypeVisualFactory.cs`.
- This is especially important for consistent text/font behavior.
- Buttons and click areas emit `HudCommand` values rather than reaching directly into every service.
## Text and Localization Conventions
- Most gameplay copy is Chinese.
- Some files contain encoding-damaged strings (mojibake), so contributors should be careful about file encoding when editing text-heavy source files.
- Tooltip bodies are usually assembled from state plus `detailLines`.
- Card and sticker text is composed from runtime state, not stored as prefab-authored localized assets.
## Testing and Validation Conventions
- Manual playtesting in `Assets/Scenes/SampleScene.unity`.
- Documentation-backed validation through `README.md`, `docs/DEVLOG_2026-03-31.md`, and `docs/AI_HANDOFF.md`.
- `com.unity.test-framework` is installed, but there are no edit mode / play mode tests in the repo.
## Practical Contributor Rules
- Prefer extending the focused subsystem before adding more orchestration to `PopHeroGame`.
- Prefer presenter/service extraction over more branching inside `PopHeroHud`.
- Do not split bounce behavior between preview and actual flight; keep shared logic in `BounceStepSolver`.
- Treat blocks, stickers, and mods as runtime instances, not static templates only.
- Preserve `.meta` files and remember Unity-generated project files are local, not committed.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern Overview
- Scene starts nearly empty and is assembled at runtime from code.
- `PopHeroGame` acts as the composition root and high-level orchestrator.
- Board, combat, stickers, mods, rewards, and UI are split into subsystems, but some roots are still large.
- Gameplay state is session-local and rebuilt on each run; there is no persistence layer.
## Layers
- Purpose: Enter the gameplay stack after scene load.
- Contains: `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs`.
- Depends on: Unity scene lifecycle only.
- Used by: `Assets/Scenes/SampleScene.unity`.
- Purpose: Wire subsystems together, bridge Unity lifecycle, coordinate state transitions.
- Contains: `Assets/Scripts/POPHero/Core/PopHeroGame.cs`, `Assets/Scripts/POPHero/Flow/GameFlowControllers.cs`.
- Depends on: All lower layers.
- Used by: Runtime-created systems, HUD, presenters, and input.
- Purpose: Define shared game state, enums, contracts, and round-resolution rules.
- Contains: `Assets/Scripts/POPHero/Core/GameContracts.cs`, `Assets/Scripts/POPHero/Core/GameplayTypes.cs`, `Assets/Scripts/POPHero/Flow/RoundController.cs`, `Assets/Scripts/POPHero/Flow/RoundResolveResult.cs`, `Assets/Scripts/POPHero/Characters/PlayerData.cs`, `Assets/Scripts/POPHero/Characters/EnemyData.cs`.
- Depends on: Minimal Unity primitives plus domain models.
- Used by: Combat, board, systems, and UI layers.
- Purpose: Own focused gameplay domains behind facades or helper services.
- Contains:
- Depends on: Domain state/contracts and Unity runtime types.
- Used by: `PopHeroGame` and presentation components.
- Purpose: Present world actors, HUD panels, tooltips, and runtime visuals.
- Contains:
- Depends on: Read models, command sinks, Unity GUI APIs.
- Used by: Player-facing runtime only.
## Data Flow
- Session state is centralized in `PopHeroGame`, `RoundController`, `PlayerData`, `EnemyData`, and collection objects like `PlayerBlockCollection`.
- There is no file save/load, server sync, or deterministic replay layer.
## Key Abstractions
- Purpose: Own runtime assembly and orchestration.
- Example: `Assets/Scripts/POPHero/Core/PopHeroGame.cs`.
- Pattern: Large orchestration object with Unity lifecycle hooks plus facade-style methods.
- Purpose: Preserve a stable public surface while moving logic into smaller internal services.
- Examples: `BoardManager` over `BlockCollectionService`, `BlockRewardService`, and `RuntimeBoardService`; service facades in `Assets/Scripts/POPHero/Flow/GameFlowControllers.cs`.
- Pattern: Public facade backed by focused internal classes.
- Purpose: Decouple UI reads from gameplay writes.
- Examples: `IGameReadModel` and `IHudCommandSink` in `Assets/Scripts/POPHero/Core/GameContracts.cs`.
- Pattern: UI reads state through interfaces and writes via explicit commands.
- Purpose: Broadcast combat events for sticker and system reactions.
- Example: `CombatEventHub` in `Assets/Scripts/POPHero/Core/GameContracts.cs`.
- Pattern: In-process pub/sub without async messaging.
- Purpose: Treat blocks, stickers, mods, and shop entries as mutable session objects.
- Examples: `BlockCardState`, `StickerInstance`, `ModInstance`, `ShopItemEntry` in `Assets/Scripts/POPHero/Core/GameplayTypes.cs` and `Assets/Scripts/POPHero/Systems/`.
- Pattern: Data bags plus service-driven mutation.
## Entry Points
- Location: `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs`
- Trigger: Unity runtime load after scene load.
- Responsibilities: Ensure the game root exists.
- Location: `Assets/Scripts/POPHero/Core/PopHeroGame.cs`
- Trigger: Unity lifecycle (`Awake`, `Update`).
- Responsibilities: Configure runtime, build subsystems, manage state transitions, own orchestration.
- Location: `Assets/Scripts/POPHero/UI/PopHeroHud.cs`
- Trigger: Unity `OnGUI()`.
- Responsibilities: Render menus, combat HUD, rewards, shop, and block management; emit commands.
## Error Handling
- Invalid player actions return `false` with a user-facing reason, especially in `BoardServices.cs` and `ModShopSystems.cs`.
- Runtime flow methods early-return when state is invalid or the game is in the wrong phase.
- There is little centralized top-level error reporting beyond gameplay state gating.
## Cross-Cutting Concerns
- Centralized in `PopHeroPrototypeConfig`, but currently loaded from a missing-or-fallback `Resources` asset path.
- Debug controls are surfaced directly in `PopHeroHud`.
- Debug trajectory visualization is built into `BallController`.
- Player-facing strings are largely Chinese.
- Some files show encoding corruption, so text quality and source encoding are an architectural maintenance concern.
- `docs/AI_HANDOFF.md` is effectively part of the working architecture because it documents intended ownership boundaries and anti-patterns for future edits.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->

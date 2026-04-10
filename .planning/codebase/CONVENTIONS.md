# Conventions

**Analysis Date:** 2026-04-09

## Naming and Organization

**Namespace:**
- Gameplay code consistently uses the `POPHero` namespace.

**Folder-by-responsibility layout:**
- Scripts are grouped by domain responsibility under `Assets/Scripts/POPHero/`:
  - `Core`
  - `Flow`
  - `Combat`
  - `Board`
  - `Characters`
  - `Systems`
  - `UI`

**Type naming patterns:**
- `*Controller` usually coordinates stateful behavior (`RoundController`, `AimStateController`, `EnemyController`).
- `*Manager` usually owns a subsystem facade (`BoardManager`, `ModManager`, `ShopManager`).
- `*Presenter` converts state into UI or world-space presentation (`PlayerPresenter`, HUD presenters).
- `*Service` or facade classes encapsulate focused logic behind a thin public wrapper (`BlockCollectionService`, `RuntimeBoardServiceFacade`).

## Code Style

**Guard-clause heavy flow:**
- Methods frequently early-return on invalid state rather than nesting deeply.
- Examples: `PopHeroGame`, `RoundController`, `BoardManager`, `ShopManager`.

**Action verb patterns:**
- `Initialize(...)` for one-time setup.
- `Build*` for runtime object creation or catalog population.
- `Refresh*` for presentation recomputation.
- `Try*` for operations that can fail without throwing.
- `Handle*` for orchestration callbacks and event responses.

**Failure reporting:**
- Domain mutations commonly use `bool` plus `out string failReason` instead of throwing exceptions.
- This is especially common in `Assets/Scripts/POPHero/Board/BoardServices.cs` and `Assets/Scripts/POPHero/Systems/ModShopSystems.cs`.

## State Modeling

**Data-bag style runtime state:**
- Many gameplay entities are mutable classes with public fields rather than strict property encapsulation.
- Examples: `BlockCardState`, `SocketSlotState`, `RewardChoiceEntry`, `ShopItemEntry`, `ModData`.

**Read model / command sink split:**
- UI reads through `IGameReadModel`.
- UI writes through `IHudCommandSink` and `HudCommand`.
- This keeps `PopHeroHud` from directly mutating every subsystem, even though the HUD still contains significant logic.

**Enum-driven flow:**
- Phase and content types are strongly enum-based:
  - `RoundState`
  - `BoardBlockType`
  - `StickerTriggerType`
  - `ShopItemKind`
  - `GrowthRewardType`
- State transitions are explicit and centralized rather than string-based.

## Composition Conventions

**Runtime wiring over inspector wiring:**
- `PopHeroGame.BuildPrototype()` creates objects and attaches components in code.
- Dependencies are passed explicitly via `Initialize(...)` calls rather than serialized inspector references.

**Owner/context injection:**
- Runtime components usually receive `PopHeroGame` or a context object during setup.
- `BoardServices.cs` uses a shared `BoardManagerContext` object to avoid passing many parameters around.

**Facade compatibility pattern:**
- Public APIs are kept stable while detailed logic moves into internal services.
- `docs/AI_HANDOFF.md` explicitly recommends extending services first and pruning facades later.

## Gameplay Rules Conventions

**Core rule extensions belong in focused layers:**
- Round math belongs in `Assets/Scripts/POPHero/Flow/RoundController.cs`.
- Bounce/path logic belongs in `Assets/Scripts/POPHero/Combat/BounceStepSolver.cs`.
- Block inventory/reward/runtime generation belongs in `Assets/Scripts/POPHero/Board/BoardServices.cs`.
- Sticker logic should prefer the sticker runtime/execution stack instead of embedding sticker IDs into round logic.

**Legacy system boundary:**
- `Assets/Scripts/POPHero/Systems/BuffManager.cs` is intentionally a compatibility shell.
- New progression/features should favor `Sticker`, `Mod`, and `Shop` flows, matching `docs/AI_HANDOFF.md`.

## UI Conventions

**IMGUI-first gameplay UI:**
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` renders menus, combat HUD, intermission panels, and tooltips with IMGUI.
- HUD sections are gradually being moved into presenter-generated models in `Assets/Scripts/POPHero/UI/HudPresenters.cs`.

**World-space visuals via factory helpers:**
- Runtime sprites and text should go through `Assets/Scripts/POPHero/UI/PrototypeVisualFactory.cs`.
- This is especially important for consistent text/font behavior.

**Command-oriented HUD interactions:**
- Buttons and click areas emit `HudCommand` values rather than reaching directly into every service.

## Text and Localization Conventions

**Player-facing language:**
- Most gameplay copy is Chinese.
- Some files contain encoding-damaged strings (mojibake), so contributors should be careful about file encoding when editing text-heavy source files.

**Formatting in tooltips/cards:**
- Tooltip bodies are usually assembled from state plus `detailLines`.
- Card and sticker text is composed from runtime state, not stored as prefab-authored localized assets.

## Testing and Validation Conventions

**Current de facto validation style:**
- Manual playtesting in `Assets/Scenes/SampleScene.unity`.
- Documentation-backed validation through `README.md`, `docs/DEVLOG_2026-03-31.md`, and `docs/AI_HANDOFF.md`.

**No committed automated testing convention yet:**
- `com.unity.test-framework` is installed, but there are no edit mode / play mode tests in the repo.

## Practical Contributor Rules

When making changes, these repo-specific norms appear to matter most:
- Prefer extending the focused subsystem before adding more orchestration to `PopHeroGame`.
- Prefer presenter/service extraction over more branching inside `PopHeroHud`.
- Do not split bounce behavior between preview and actual flight; keep shared logic in `BounceStepSolver`.
- Treat blocks, stickers, and mods as runtime instances, not static templates only.
- Preserve `.meta` files and remember Unity-generated project files are local, not committed.

---
*Conventions analysis: 2026-04-09*
*Update when UI architecture, service boundaries, or text/localization practices change*

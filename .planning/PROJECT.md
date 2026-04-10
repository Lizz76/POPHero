# POPHero

## What This Is

POPHero is a Unity 2022.3 single-scene prototype for a marble-trajectory roguelike combat loop. The player aims and launches one ball, converts bounce hits into round-scoped damage and shield, then moves through block rewards, sticker/mod growth, shop, and loadout management between enemies. This GSD project is being initialized as a brownfield maintenance workspace for the existing prototype rather than a greenfield rebuild.

## Core Value

Each run should make a bounced path feel strategically meaningful and worth iterating on.

## Requirements

### Validated

- ✓ Player can start the prototype from `Assets/Scenes/SampleScene.unity` and reach gameplay without manual scene wiring — existing
- ✓ Player can preview a trajectory, launch a ball, and resolve delayed round damage through the current combat loop — existing
- ✓ Player can move through block rewards, sticker/mod/growth rewards, shop, and loadout management between enemy encounters — existing
- ✓ The project already has subsystem boundaries for combat, board/build state, stickers, mods, shop, and UI that future maintenance can build on — existing

### Active

- [ ] Preserve the current combat -> reward -> shop -> loadout loop while making the codebase safer for long-term maintenance.
- [ ] Reduce regression risk in bounce, build-state, and UI flow hotspots before layering in more prototype complexity.
- [ ] Establish committed planning and validation artifacts so future contributors can land changes in the right layer.

### Out of Scope

- Online services, multiplayer, or account systems — the current repo is a local single-player prototype with no backend surface.
- Full production UI migration to Canvas/TMP — maintenance should stabilize the current prototype before a whole-UI rewrite.
- Save/meta-progression systems — useful later, but not required to maintain the current run-based prototype loop.
- Art/content pipeline overhaul — gameplay maintainability and correctness are the immediate priority.

## Context

- The runtime is largely assembled in code by `Assets/Scripts/POPHero/Core/PopHeroGame.cs`, not by an authored scene hierarchy.
- `docs/AI_HANDOFF.md` is currently the most reliable contributor-facing guide to subsystem ownership and known pitfalls.
- The codebase already includes larger prototype systems than the README suggests, especially stickers, mods, shop, reserve-vs-active blocks, and intermission flow.
- The repository currently has unstaged gameplay/package changes, so planning artifacts should stay isolated from unrelated code edits.
- `Assets/Resources/` is empty even though the game attempts to load `Resources/PopHeroPrototypeConfig`, which suggests the current checkout relies on runtime defaults.

## Constraints

- **Tech stack**: Unity 2022.3.62f2c1 + C# — project settings pin this editor/runtime baseline.
- **Architecture**: Runtime-composed scene graph and IMGUI HUD — major changes must respect this current shape before replacing it.
- **Maintenance invariant**: Bounce preview and live flight should continue sharing one solver path — avoid split fixes across preview/runtime implementations.
- **Build environment**: `*.sln` and `*.csproj` are gitignored/generated locally — CLI validation depends on local Unity regeneration.
- **Text quality**: Player-facing copy is Chinese, but some source strings already show encoding damage — text edits need extra care.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Treat POPHero as a brownfield GSD project | Official GSD guidance for existing repos is map-codebase first, then initialize project context | ✓ Good |
| Default this GSD workspace to interactive + balanced + committed docs | Long-term maintenance benefits from approvals, readable planning history, and safer defaults | — Pending |
| Start with maintenance baseline, stability, and validation phases before net-new feature expansion | The biggest current risks are regressions in oversized orchestration/UI files and missing safety rails | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition:**
1. Requirements invalidated? -> Move them to Out of Scope with a reason.
2. Requirements validated? -> Move them to Validated with a phase reference.
3. New maintenance or feature requirements emerged? -> Add them to Active.
4. Decisions to log? -> Add them to Key Decisions.
5. "What This Is" still accurate? -> Update it if the prototype drifts materially.

**After each milestone:**
1. Review every section against the actual codebase state.
2. Re-check whether the current Core Value is still the right priority.
3. Audit Out of Scope items and confirm the reasons still hold.
4. Refresh Context with the current maintenance reality, not historical assumptions.

---
*Last updated: 2026-04-09 after initialization*

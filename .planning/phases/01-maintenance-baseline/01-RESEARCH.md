# Phase 1: Maintenance Baseline - Research

**Date:** 2026-04-09
**Phase:** 1 - Maintenance Baseline
**Goal:** Lock the brownfield prototype into a documented, reproducible maintenance baseline before deeper refactors begin.

## Research Question

What does this repo need so Phase 1 can produce a real maintenance baseline, while preserving room for the user's later prefab-driven presentation migration?

## Inputs Reviewed

- `.planning/phases/01-maintenance-baseline/01-CONTEXT.md`
- `.planning/ROADMAP.md`
- `.planning/REQUIREMENTS.md`
- `.planning/STATE.md`
- `.planning/codebase/TESTING.md`
- `.planning/codebase/CONCERNS.md`
- `README.md`
- `docs/AI_HANDOFF.md`
- `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs`
- `Assets/Scripts/POPHero/Core/PopHeroGame.cs`
- `Assets/Scripts/POPHero/Core/GameContracts.cs`
- `Assets/Scripts/POPHero/UI/HudPresenters.cs`
- `Assets/Scripts/POPHero/UI/PrototypeVisualFactory.cs`
- `Assets/Scripts/POPHero/Core/PopHeroPrototypeConfig.cs`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`

## Findings

### 1. The repo already has the right code seams for "prep work before prefab migration"

- `GameContracts.cs` already exposes `IGameReadModel` and `IHudCommandSink`.
- `HudPresenters.cs` already moves some UI-facing formatting and model building out of `PopHeroHud`.
- `PrototypeVisualFactory.cs` is the current runtime-authored surface for sprites and `TextMesh`, which makes it the clearest documentation seam before any prefab migration.
- `PopHeroBootstrap.cs` plus `PopHeroGame.cs` define the actual startup path today: open `Assets/Scenes/SampleScene.unity`, let `PopHeroBootstrap` self-create `PopHeroGame`, and let `PopHeroGame` compose the runtime world in code.

Implication:
- Phase 1 does not need to invent a new architecture.
- It should document the current seams and explicitly mark them as the later prefab-migration handoff points.

### 2. Documentation authority is currently ambiguous unless Phase 1 resolves it

- `docs/AI_HANDOFF.md` is the most complete maintainer document.
- `README.md` is lighter and does not carry the same implementation detail.
- `.planning/codebase/CONCERNS.md` already flags documentation drift.
- `README.md` and some source/docs also show encoding risk in terminal output, which increases the chance that future maintainers trust the wrong file or avoid editing docs.

Implication:
- Phase 1 should not rewrite the whole documentation set.
- It should add a narrow maintainer-routing layer that says which document is authoritative and which are secondary/historical.

### 3. Validation needs to be manual-first for this phase

- `.planning/codebase/TESTING.md` confirms there are no committed automated tests.
- `com.unity.test-framework` is installed in `Packages/manifest.json`, so the project is test-capable, but not test-protected yet.
- `PopHeroHud.cs` already exposes useful debug buttons:
  - start game from main menu
  - toggle aim mode
  - shuffle board
  - add gold
  - kill enemy
  - damage player

Implication:
- Phase 1 should ship a committed manual regression checklist.
- The checklist should use the existing debug controls to accelerate traversal, but still describe player-visible outcomes so it remains valid after UI changes.

### 4. The environment story needs explicit caveats

- `ProjectVersion.txt` declares `2022.3.62f2c1`.
- `Packages/manifest.json` depends on both Unity packages and the OpenUPM scoped registry for `com.ivanmurzak.unity.mcp`.
- `PopHeroGame.cs` loads `Resources/PopHeroPrototypeConfig` and falls back to `PopHeroPrototypeConfig.CreateRuntimeDefault()`.
- `Assets/Resources/PopHeroPrototypeConfig.asset` is missing in this checkout.
- `.gitignore` excludes generated `.sln` and `.csproj` files, so CLI build steps only work after local Unity regeneration.

Implication:
- Phase 1 docs must explicitly say:
  - which Unity editor version is expected
  - that package restore depends on the OpenUPM registry
  - that generated solution/project files are local artifacts
  - that the missing config asset is a known maintenance risk, not a hidden assumption

### 5. Phase 1 should avoid hot gameplay files wherever possible

- `.planning/codebase/CONCERNS.md` flags `PopHeroGame.cs`, `PopHeroHud.cs`, and `BoardServices.cs` as the highest-risk maintenance hotspots.
- `git status` already shows unrelated local edits in those same hotspots.

Implication:
- The best Phase 1 plan split is doc-heavy and write-set-light.
- Any enabling guidance for later prefab migration should be captured in docs now, not by touching runtime behavior yet.

## Recommended Phase Shape

Phase 1 should be split into three autonomous plans in one wave:

1. `01-01-PLAN.md`
   - establish maintainer routing and document authority
   - keep edits limited to `README.md` and `docs/AI_HANDOFF.md`

2. `01-02-PLAN.md`
   - create a committed regression checklist
   - keep edits limited to `docs/REGRESSION_CHECKLIST.md`

3. `01-03-PLAN.md`
   - create a prefab-readiness seam map for later presentation migration
   - keep edits limited to `docs/PREFAB_READINESS_MAP.md`

This split covers all Phase 1 requirements without touching the hot gameplay files already under local modification.

## Requirement Mapping

| Requirement | What Phase 1 should do |
|-------------|------------------------|
| `LOOP-01` | Document startup/bootstrap path and create a checklist that validates the repo can reach first aim state from `SampleScene` |
| `MAINT-01` | Define document authority, maintainer entry points, subsystem boundaries, and prefab-readiness seams |
| `VAL-01` | Commit a repeatable regression checklist with explicit sections and expected outcomes |

## Validation Architecture

Phase 1 is a documentation-first phase, so the validation contract should mix doc verification and manual smoke coverage:

- **Fast automated checks during execution**
  - file existence
  - heading presence
  - exact string/regex checks in the new docs
  - `git diff --check`

- **Manual-only end-to-end verification**
  - open `Assets/Scenes/SampleScene.unity`
  - enter Play Mode
  - start a run from the main menu
  - reach the first aim state without manual hierarchy repair
  - traverse combat/intermission/debug flows using the checklist

- **Why this is enough for Phase 1**
  - The phase goal is to create the baseline artifacts, not to add automation yet.
  - The manual checklist becomes the contract that later automation in Phase 5 can target.

## Risks To Carry Forward

- The missing `Assets/Resources/PopHeroPrototypeConfig.asset` should stay visible in all maintenance docs until a committed config asset exists.
- Encoding drift in `README.md`, `docs/AI_HANDOFF.md`, and some runtime strings should be treated as a future cleanup lane, not casually mixed into this baseline phase unless a plan explicitly scopes it.
- The long-term prefab migration should preserve:
  - the `PopHeroBootstrap` -> `PopHeroGame` bootstrap contract until intentionally replaced
  - `IGameReadModel` / `IHudCommandSink` style boundaries
  - the shared bounce path through `BounceStepSolver`

## Planning Conclusion

Phase 1 should create a maintainer baseline through documentation and verification artifacts, not through runtime refactors. That is the smallest correct move that supports the user's later path:

- authored Unity windows / entities / prefabs
- stronger decoupling before deeper gameplay expansion
- slower, safer maintenance with room to brainstorm systems later

---
*Research completed: 2026-04-09*

# Phase 1: Maintenance Baseline - Context

**Gathered:** 2026-04-09
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase establishes the maintainer-facing baseline for the current brownfield prototype. It defines the authoritative docs, validation checklist, environment expectations, and a narrow allowance for low-risk enabling changes that prepare later prefab-driven presentation work without actually delivering the prefab migration itself or changing the gameplay loop.

</domain>

<decisions>
## Implementation Decisions

### Documentation authority
- **D-01:** `docs/AI_HANDOFF.md` is the maintainer-facing source of truth for current subsystem ownership, runtime invariants, and maintenance pitfalls.
- **D-02:** `README.md` stays lightweight and onboarding-oriented; it should not be treated as the canonical implementation reference.
- **D-03:** `docs/DEVLOG_2026-03-31.md` is historical context only. When it conflicts with current behavior, downstream work should follow `docs/AI_HANDOFF.md` plus current planning artifacts.

### Phase 1 change budget
- **D-04:** Phase 1 may include low-risk, maintenance-oriented code changes that prepare later prefab/entity-based presentation migration, such as clarifying entry points, isolating resource hookup seams, exposing layout/config touchpoints, and documenting integration boundaries.
- **D-05:** Phase 1 must not try to complete the prefab/window/entity migration itself, and it must avoid broad gameplay refactors or net-new feature work.
- **D-06:** Because the repo already has unrelated local gameplay edits in hot files, planning should keep the Phase 1 write set narrow and avoid mixing baseline work with larger behavior changes.

### Regression checklist shape
- **D-07:** Phase 1 should produce a structured manual regression checklist instead of immediate automated tests.
- **D-08:** The checklist should be organized into four sections: startup/bootstrap, combat loop, build/intermission flow, and UI/debug interactions.
- **D-09:** Existing in-game debug controls may be used to shorten checklist execution, but the checklist must still describe the expected player-visible outcome for each step.

### Maintenance environment target
- **D-10:** The baseline should target a handoff-friendly setup where a maintainer can open the project, enter `Assets/Scenes/SampleScene.unity`, and reach the first aim state without manual hierarchy repair.
- **D-11:** Environment documentation should explicitly call out the Unity version expectation, package registry dependencies, and the fact that `.sln` and `.csproj` files must be regenerated locally before CLI build commands can work.
- **D-12:** Missing `Resources/PopHeroPrototypeConfig` should be treated as a documented maintenance risk and seam to stabilize later, not as a hidden assumption.

### Future architecture direction
- **D-13:** The next major presentation direction is to move away from code-generated runtime presentation toward Unity-authored windows, entities, and prefabs that make art hookup and spatial/layout tuning easier.
- **D-14:** That presentation migration should happen only after this baseline exists, so later phases can refactor against documented seams instead of the current implicit runtime wiring.
- **D-15:** Lua is a possible future architecture path, but for now it remains an explicit evaluation item rather than a current design constraint.

### the agent's Discretion
- Choose the exact artifact split for the Phase 1 baseline, as long as it preserves the decisions above and keeps planning docs isolated from unrelated gameplay edits.
- Choose the most maintainable format for the checklist and handoff cross-links.
- Recommend, but do not prematurely implement, the cleanest seam candidates for later prefab migration.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project scope and phase intent
- `.planning/PROJECT.md` - Core value, constraints, maintenance priorities, and the long-term brownfield direction for the prototype.
- `.planning/REQUIREMENTS.md` - Phase 1 requirements `LOOP-01`, `MAINT-01`, and `VAL-01`, plus the out-of-scope boundary.
- `.planning/ROADMAP.md` - Phase 1 goal, success criteria, and the current phase ordering that treats baseline work as prerequisite to later hardening.
- `.planning/STATE.md` - Current blockers, maintenance hotspots, and active session position.

### Maintainer authority and gameplay invariants
- `docs/AI_HANDOFF.md` - Current subsystem ownership, runtime flow, active/reserve semantics, bounce-solver invariants, IMGUI constraints, and contributor guidance.
- `README.md` - Lightweight project overview and current manual run instructions; useful as secondary onboarding only.
- `docs/DEVLOG_2026-03-31.md` - Historical gameplay context only; use to understand drift, not as current implementation authority.

### Codebase maps for planning
- `.planning/codebase/ARCHITECTURE.md` - Composition root, runtime bootstrap, and high-level subsystem boundaries.
- `.planning/codebase/CONCERNS.md` - Current regression traps, missing config asset risk, dirty worktree hotspots, and documentation drift risks.
- `.planning/codebase/TESTING.md` - Existing validation posture, manual test seams, and realistic test strategy starting points.
- `.planning/codebase/CONVENTIONS.md` - Repo-specific contributor rules, especially bounce-logic ownership, HUD interaction patterns, and service-extension guidance.
- `.planning/codebase/STRUCTURE.md` - Fast orientation map for scene entry, key scripts, and maintenance landmarks.
- `.planning/codebase/INTEGRATIONS.md` - Environment dependencies, package registries, generated project file expectations, and config asset loading behavior.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/Scripts/POPHero/Core/GameContracts.cs` - Already provides `IGameReadModel` and `IHudCommandSink`, which are useful seams for later presentation decoupling.
- `Assets/Scripts/POPHero/UI/HudPresenters.cs` - Existing presenter extraction work can be extended instead of pushing more logic into `PopHeroHud`.
- `Assets/Scripts/POPHero/UI/PrototypeVisualFactory.cs` - Current world-space visual factory is a likely transition seam when moving from fully code-authored visuals toward authored assets and prefabs.
- `Assets/Scripts/POPHero/Core/PopHeroPrototypeConfig.cs` - Central typed config surface that can later become a stable authored tuning asset once the missing `Resources` asset is addressed.
- Existing HUD debug controls in `Assets/Scripts/POPHero/UI/PopHeroHud.cs` - Useful for fast manual regression coverage during the baseline phase.

### Established Patterns
- The runtime is composed from `PopHeroGame` rather than scene-authored inspector wiring; any baseline work must document and respect that current bootstrap path.
- Bounce preview and live flight already share a single solver path through `BounceStepSolver`; baseline artifacts should reinforce this as a non-negotiable maintenance invariant.
- Blocks, stickers, mods, and shop items are runtime instances, not static authored templates only; future presentation refactors must preserve those state semantics.
- The project currently uses IMGUI plus runtime-authored visuals; Phase 1 should document this as the current reality while preparing for a later authored-presentation migration.

### Integration Points
- `Assets/Scripts/POPHero/Core/PopHeroGame.cs` - Main composition root and likely seam map for future prefab-aware presentation extraction.
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` - Current player-facing interaction surface and the highest-risk UI hotspot to fence with docs and checklists.
- `Assets/Scripts/POPHero/Board/BoardServices.cs` - Key boundary for active/reserve and reward/runtime board rules that later UI and presentation changes must not corrupt.
- `Assets/Scenes/SampleScene.unity` plus `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs` - Startup path that the baseline checklist must validate every time.

</code_context>

<specifics>
## Specific Ideas

- The user wants the project to evolve toward Unity-authored windows, entities, and prefab-driven references so art hookup and position tuning can happen directly in the editor instead of through purely code-generated objects.
- The user expects front-end and presentation conversion to happen before deeper gameplay expansion, because the current code-generated presentation shape makes later asset iteration awkward.
- The user wants the codebase to become more decoupled and maintainable before pushing harder on build depth, content volume, and system brainstorming.
- Some front-end interface work already exists on another machine; later planning should treat that work as a likely input once it is synced into this repo.

</specifics>

<deferred>
## Deferred Ideas

- Full presentation migration from code-generated runtime objects to Unity-authored windows, entities, and prefabs - future phase, after the maintenance baseline is in place.
- Broad design-pattern cleanup and deeper subsystem decoupling beyond low-risk enabling changes - future architecture-focused phase.
- Lua-based runtime and refactor path - future evaluation item; not committed for the current roadmap yet.
- Gameplay depth, build variety, and wider system brainstorming - future design phases after the project is easier to maintain.

</deferred>

---

*Phase: 01-maintenance-baseline*
*Context gathered: 2026-04-09*

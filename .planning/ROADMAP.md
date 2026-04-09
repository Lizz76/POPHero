# Roadmap: POPHero

## Overview

This roadmap treats POPHero as an existing Unity prototype that now needs durable maintenance guardrails before it expands further. The sequence starts by locking in a safe maintenance baseline, then hardens the combat loop and build/intermission systems, then reduces configuration and surface-area traps, and finally adds repeatable regression coverage for long-term iteration.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions if maintenance discovers blocking issues midstream

- [ ] **Phase 1: Maintenance Baseline** - Commit the operating context, invariants, and validation checklist needed to maintain the existing prototype safely.
- [ ] **Phase 2: Combat Stability** - Harden shared bounce, preview, and round-resolution behavior without changing the core feel.
- [ ] **Phase 3: Build and Intermission Integrity** - Protect active/reserve, sticker, mod, reward, shop, and loadout state transitions.
- [ ] **Phase 4: Configuration and Surface Cleanup** - Replace hidden maintenance traps with explicit configuration and safer edit surfaces.
- [ ] **Phase 5: Regression Safety Net** - Add repeatable automated or scriptable coverage around the riskiest prototype systems.

## Phase Details

### Phase 1: Maintenance Baseline
**Goal**: Lock the brownfield prototype into a documented, reproducible maintenance baseline before deeper refactors begin.
**Depends on**: Nothing (first phase)
**Requirements**: [LOOP-01, MAINT-01, VAL-01]
**Success Criteria** (what must be TRUE):
  1. Contributor can open the repo, read the committed planning/handoff docs, and quickly identify where a change belongs.
  2. Opening `Assets/Scenes/SampleScene.unity` and starting a run still reaches the first aim state without manual hierarchy repair.
  3. A committed regression checklist exists for combat, build-state, and UI flows.
**Plans**: 3 planned

Plans:
- [ ] `01-01`: Maintainer Entry and Authority Baseline
- [ ] `01-02`: Manual Regression Checklist
- [ ] `01-03`: Prefab Readiness Map

### Phase 2: Combat Stability
**Goal**: Preserve and harden the core bounce, preview, and round-resolution loop that defines the prototype's fun.
**Depends on**: Phase 1
**Requirements**: [LOOP-02, COMB-01, COMB-02]
**Success Criteria** (what must be TRUE):
  1. Preview and runtime bounce behavior agree across wall, corner, and recovery-heavy trajectories.
  2. Damage presentation still occurs at the intended resolve timing rather than leaking into raw flight updates.
  3. Shared bounce behavior remains centralized instead of splitting between preview and live-flight code.
**Plans**: TBD

Plans:
- [ ] TBD: Created during `/gsd-plan-phase 2`

### Phase 3: Build and Intermission Integrity
**Goal**: Make block/build and intermission systems safe to extend without corrupting runtime player state.
**Depends on**: Phase 2
**Requirements**: [LOOP-03, BUILD-01, BUILD-02]
**Success Criteria** (what must be TRUE):
  1. Reward, shop, and loadout transitions preserve active/reserve cards, sockets, stickers, and mods consistently.
  2. Swaps, removals, and reward claims continue to operate on full card instances rather than partial data copies.
  3. Future build-rule work clearly lands in sticker/mod/shop systems instead of reviving the legacy Buff placeholder.
**Plans**: TBD

Plans:
- [ ] TBD: Created during `/gsd-plan-phase 3`

### Phase 4: Configuration and Surface Cleanup
**Goal**: Replace hidden maintenance traps with explicit configuration and narrower change surfaces.
**Depends on**: Phase 3
**Requirements**: [MAINT-02]
**Success Criteria** (what must be TRUE):
  1. Prototype tuning has a documented, committed source of truth instead of relying on hidden fallback defaults.
  2. Encoding/text hotspots are either cleaned up or clearly fenced so future edits do not silently degrade UI copy.
  3. The change surface of `PopHeroGame` and `PopHeroHud` is safer than the current baseline.
**Plans**: TBD

Plans:
- [ ] TBD: Created during `/gsd-plan-phase 4`

### Phase 5: Regression Safety Net
**Goal**: Add repeatable automated or scriptable validation around the systems most likely to regress during long-term maintenance.
**Depends on**: Phase 4
**Requirements**: [VAL-02]
**Success Criteria** (what must be TRUE):
  1. At least one automated or scriptable check covers bounce/combat integrity.
  2. At least one automated or scriptable check covers build/intermission integrity.
  3. Contributors have a lightweight validation path they can run before future refactors or balancing work.
**Plans**: TBD

Plans:
- [ ] TBD: Created during `/gsd-plan-phase 5`

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Maintenance Baseline | 0/3 | Planned | - |
| 2. Combat Stability | 0/TBD | Not started | - |
| 3. Build and Intermission Integrity | 0/TBD | Not started | - |
| 4. Configuration and Surface Cleanup | 0/TBD | Not started | - |
| 5. Regression Safety Net | 0/TBD | Not started | - |

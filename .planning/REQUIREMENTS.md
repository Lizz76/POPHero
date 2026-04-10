# Requirements: POPHero

**Defined:** 2026-04-09
**Core Value:** Each run should make a bounced path feel strategically meaningful and worth iterating on.

## v1 Requirements

### Gameplay Loop

- [ ] **LOOP-01**: Player can start a run from the main menu and reach the first aim phase in `Assets/Scenes/SampleScene.unity` without manual scene object setup.
- [ ] **LOOP-02**: Player can finish a combat round and see damage applied only during the intended resolve presentation, not during raw flight accumulation.
- [ ] **LOOP-03**: Player can transition from enemy defeat through block reward, reward choice, shop, loadout, and next encounter without orphaned gameplay state.

### Combat Integrity

- [ ] **COMB-01**: Aim preview and actual ball flight remain behaviorally aligned for wall bounces, corner bounces, and recovery cases.
- [ ] **COMB-02**: Shared bounce rules stay implemented from one source of truth instead of diverging between preview and runtime flight code paths.

### Build Systems

- [ ] **BUILD-01**: Active and reserve block collections preserve full card instances, including sockets and installed stickers, through rewards, swaps, removals, and reshuffles.
- [ ] **BUILD-02**: Sticker, mod, and shop systems remain the supported build layers for future changes, without reintroducing dependency on the legacy Buff placeholder.

### Maintainability

- [ ] **MAINT-01**: Contributor can identify the correct subsystem boundary for combat, board, system, and UI changes from committed docs and planning artifacts.
- [ ] **MAINT-02**: Contributor can adjust prototype tuning from a stable, documented configuration source instead of hidden fallback defaults.

### Validation

- [ ] **VAL-01**: Contributor can execute a repeatable regression checklist for combat, build-state, and UI flows before or after refactors.
- [ ] **VAL-02**: High-risk systems gain initial automated or scriptable regression coverage over time.

## v2 Requirements

### Progression

- **PROG-01**: Player can carry progression or meta-unlocks across runs.

### UI Stack

- **UI-01**: Prototype UI is migrated from IMGUI-heavy runtime code to a production-ready Canvas/TMP stack.

### Live Features

- **LIVE-01**: Project integrates analytics, remote config, or online services where needed.

### Content Pipeline

- **CONT-01**: Core presentation and balance data are driven by committed authored assets instead of mostly code-created runtime content.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Multiplayer or online accounts | No backend or networking surface exists in the current maintenance target |
| Full UI rewrite during onboarding | Too large for the first maintenance roadmap; stabilize current behavior first |
| Save system / meta progression right now | Not required to preserve or maintain the current prototype loop |
| Art/content production overhaul | Gameplay safety and architecture clarity are higher leverage first |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LOOP-01 | Phase 1 | Pending |
| MAINT-01 | Phase 1 | Pending |
| VAL-01 | Phase 1 | Pending |
| LOOP-02 | Phase 2 | Pending |
| COMB-01 | Phase 2 | Pending |
| COMB-02 | Phase 2 | Pending |
| LOOP-03 | Phase 3 | Pending |
| BUILD-01 | Phase 3 | Pending |
| BUILD-02 | Phase 3 | Pending |
| MAINT-02 | Phase 4 | Pending |
| VAL-02 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 11 total
- Mapped to phases: 11
- Unmapped: 0

---
*Requirements defined: 2026-04-09*
*Last updated: 2026-04-09 after initial definition*

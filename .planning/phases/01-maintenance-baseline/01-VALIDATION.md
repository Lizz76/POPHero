---
phase: 1
slug: maintenance-baseline
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-09
---

# Phase 1 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Shell verification plus Unity manual smoke |
| **Config file** | none - doc-first phase |
| **Quick run command** | `git diff --check && rg -n "Maintainer Entry|Document Authority|Environment Expectations|Startup Bootstrap|Combat Loop|Build and Intermission|UI and Debug Interactions|Runtime Authored Surfaces|Prefab Migration Seams|Deferred Migration Rules" README.md docs/AI_HANDOFF.md docs/REGRESSION_CHECKLIST.md docs/PREFAB_READINESS_MAP.md` |
| **Full suite command** | `manual: follow docs/REGRESSION_CHECKLIST.md in Unity Editor` |
| **Estimated runtime** | ~10 minutes |

---

## Sampling Rate

- **After every task commit:** Run `git diff --check` plus the relevant `rg` checks for the edited docs
- **After every plan wave:** Run `manual: follow docs/REGRESSION_CHECKLIST.md in Unity Editor`
- **Before `/gsd-verify-work`:** Manual checklist should be green and all plan artifact checks should pass
- **Max feedback latency:** 10 minutes

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | MAINT-01 | T-1-01 | Maintainer docs route contributors to the correct authority before editing | doc grep | `rg -n "## Maintainer Entry|docs/AI_HANDOFF.md|.planning/" README.md docs/AI_HANDOFF.md` | expected | pending |
| 1-01-02 | 01 | 1 | LOOP-01 | T-1-02 | Startup path and environment caveats are documented, not hidden | doc grep | `rg -n "SampleScene|PopHeroBootstrap|PopHeroGame|2022.3.62f2c1|OpenUPM|.sln|.csproj|PopHeroPrototypeConfig" docs/AI_HANDOFF.md` | expected | pending |
| 1-02-01 | 02 | 1 | VAL-01 | T-1-03 | Regression checklist preserves manual smoke coverage across startup, combat, build, and UI flows | doc grep | `rg -n "^## Startup Bootstrap$|^## Combat Loop$|^## Build and Intermission$|^## UI and Debug Interactions$" docs/REGRESSION_CHECKLIST.md` | expected | pending |
| 1-02-02 | 02 | 1 | LOOP-01 | T-1-04 | Checklist contains player-visible pass conditions and debug acceleration notes | doc grep | `rg -n "Pass condition|Debug acceleration" docs/REGRESSION_CHECKLIST.md` | expected | pending |
| 1-03-01 | 03 | 1 | MAINT-01 | T-1-05 | Prefab migration guidance names current runtime-authored surfaces and approved seams | doc grep | `rg -n "^## Runtime Authored Surfaces$|^## Prefab Migration Seams$|^## Deferred Migration Rules$|PopHeroBootstrap|IGameReadModel|IHudCommandSink|PrototypeVisualFactory|PopHeroPrototypeConfig" docs/PREFAB_READINESS_MAP.md` | expected | pending |

*Status: pending -> green -> red -> flaky*

---

## Wave 0 Requirements

- [ ] Existing infrastructure covers all Phase 1 requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Start a run from `Assets/Scenes/SampleScene.unity` and reach the first aim state without hierarchy repair | `LOOP-01` | Requires Unity Play Mode and the current runtime bootstrap path | Open `SampleScene`, enter Play Mode, click the main-menu start button, and confirm the run enters the initial block draft and then the first aim-capable combat flow without hand-placing objects in the hierarchy |
| Traverse combat, reward, shop, loadout, and debug controls once using the committed checklist | `VAL-01` | The project currently relies on editor-visible gameplay flows and IMGUI controls | Follow `docs/REGRESSION_CHECKLIST.md` end to end, using the listed debug controls only as accelerators, and confirm each section's pass conditions hold |

---

## Validation Sign-Off

- [ ] All tasks have grep-verifiable acceptance criteria
- [ ] Sampling continuity: no plan lacks an automated doc check
- [ ] Wave 0 covers all missing references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10 minutes
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending

# Phase 1: Maintenance Baseline - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-04-09
**Phase:** 01-maintenance-baseline
**Areas discussed:** documentation authority, phase 1 change budget, regression checklist shape, future architecture direction

---

## Documentation authority

| Option | Description | Selected |
|--------|-------------|----------|
| `docs/AI_HANDOFF.md` as canonical | Treat the handoff doc as the maintainer source of truth, keep `README.md` lightweight, and use the devlog only as history. | Yes |
| Split authority | Keep `README.md`, `docs/AI_HANDOFF.md`, and devlog as equal references and reconcile drift ad hoc. | |
| Rewrite first | Pause Phase 1 until all project docs are rewritten into a new combined authority doc. | |

**User's choice:** Follow the recommended option.
**Notes:** The maintainer needs a single reliable entry point because the current project is already more complex than the lightweight public-facing docs suggest.

---

## Phase 1 change budget

| Option | Description | Selected |
|--------|-------------|----------|
| A | Only produce docs, maintenance rules, and validation checklists; do not touch code structure. | |
| B | Allow low-risk enabling changes that prepare later prefab migration, such as clarifying entry points, reference seams, layout/config touchpoints, and resource hook points. | Yes |
| C | Pull a minimal prefab presentation skeleton directly into Phase 1, even if that starts touching `PopHeroGame` and `PopHeroHud` heavily. | |

**User's choice:** Follow the recommended option.
**Notes:** The user wants the game to move toward Unity-authored windows, entities, and prefabs for art hookup and transform tuning, but does not want the entire presentation migration forced into the maintenance-baseline phase.

---

## Regression checklist shape

| Option | Description | Selected |
|--------|-------------|----------|
| Loose smoke pass | Keep validation lightweight and informal, with only a short manual sanity pass. | |
| Structured manual checklist | Write a maintainer-friendly checklist split across startup, combat, build/intermission, and UI/debug interactions. | Yes |
| Start automation now | Spend Phase 1 creating automated smoke coverage instead of relying on manual validation first. | |

**User's choice:** Follow the recommended option.
**Notes:** Existing debug controls are acceptable helpers, but the checklist still needs to describe player-visible outcomes so future maintainers can catch regressions consistently.

---

## Future architecture direction

| Option | Description | Selected |
|--------|-------------|----------|
| Stay code-authored | Keep the current code-generated presentation model as the main direction and defer editor-authored presentation concerns. | |
| Baseline first, presentation migration next | Use Phase 1 to document seams and constraints, then move toward prefab and editor-authored presentation later; treat Lua as a future evaluation item only. | Yes |
| Commit now to immediate migration | Treat prefab conversion and a possible Lua migration as current commitments that should shape all Phase 1 work. | |

**User's choice:** Follow the recommended option.
**Notes:** The user wants front-end and presentation conversion before deeper gameplay expansion, wants stronger decoupling before long-term content growth, and sees Lua as a possible future acceleration path rather than an immediate requirement.

---

## the agent's Discretion

- Decide the exact artifact split for Phase 1 baseline docs and checklists.
- Recommend the cleanest seam candidates for later prefab migration without implementing that migration yet.
- Keep planning artifacts isolated from the repo's unrelated local gameplay edits.

## Deferred Ideas

- Full prefab and editor-authored presentation migration.
- Broader design-pattern cleanup and stronger subsystem decoupling.
- Lua runtime or tooling migration evaluation.
- Deeper build design, gameplay depth expansion, and later system brainstorming.

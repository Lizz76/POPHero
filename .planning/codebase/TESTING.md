# Testing

**Analysis Date:** 2026-04-09

## Current Testing Posture

**Automated tests in repo:**
- No committed test folders, test assemblies, or `Tests` directories were found under `Assets/`.
- No CI workflow, test runner script, or automated validation pipeline was found in the repo root.

**Installed tooling:**
- `Packages/manifest.json` includes `com.unity.test-framework` 1.1.33.
- `Packages/packages-lock.json` resolves `com.unity.ext.nunit` 1.0.6.
- This means the project is test-capable in Unity, but the test surface has not been authored yet.

## Observed Validation Practices

**Primary manual test entry:**
- Open `Assets/Scenes/SampleScene.unity`.
- Enter Play Mode and validate behavior manually.

**Manual scenarios already documented:**
- `README.md` describes the expected player loop and debug actions.
- `docs/DEVLOG_2026-03-31.md` captures prior milestone test points.
- `docs/AI_HANDOFF.md` includes a maintenance checklist and the key invariants that should not regress.

**Debug tooling available in-game:**
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` exposes direct debug controls such as:
  - shuffle board
  - add gold
  - kill enemy
  - damage player
- `Assets/Scripts/POPHero/Combat/BallController.cs` includes debug trajectory comparison visuals.

## Build Validation

**Documented approach:**
- `docs/AI_HANDOFF.md` recommends `dotnet build ...POPHero.sln`.

**Current repo reality:**
- No committed `*.sln` or `*.csproj` files are present in this checkout.
- `.gitignore` excludes those generated files.
- That means CLI build validation depends on Unity locally regenerating project files first.

**Practical implication:**
- Build checks are possible, but not reproducible from a clean checkout without local Unity/IDE regeneration steps.

## Good Test Seams for Future Coverage

These files are the best candidates for early automated coverage because they contain high-value logic with relatively clear inputs/outputs:

**Pure-ish gameplay logic:**
- `Assets/Scripts/POPHero/Flow/RoundController.cs`
  - attack accumulation
  - shield handling
  - token state
  - enemy counter-damage resolution

**Bounce and trajectory behavior:**
- `Assets/Scripts/POPHero/Combat/BounceStepSolver.cs`
  - wall bounce
  - corner bounce
  - embedded recovery

**Board/build-state rules:**
- `Assets/Scripts/POPHero/Board/BoardServices.cs`
  - active vs reserve inventory rules
  - reward claiming
  - safe-zone and block-shuffle placement rules

**Sticker execution rules:**
- `Assets/Scripts/POPHero/Systems/StickerExecution.cs`
- `Assets/Scripts/POPHero/Systems/StickerFlow.cs`
- `Assets/Scripts/POPHero/Systems/StickerRuntime.cs`

**Shop/mod economics:**
- `Assets/Scripts/POPHero/Systems/ModShopSystems.cs`

## High-Risk Areas Without Coverage

- `Assets/Scripts/POPHero/Core/PopHeroGame.cs` - Large orchestration surface with many state transitions.
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` - IMGUI interaction flow, panel state, drag/install/remove paths.
- `Assets/Scripts/POPHero/Board/BoardServices.cs` - Multiple responsibilities in one file increase regression risk.
- Source text/encoding behavior - Some strings are already corrupted, so text regressions are easy to miss without explicit checks.

## Suggested Testing Strategy

**Edit Mode first:**
- Add small logic tests around `RoundController`, board inventory, and sticker effect primitives.
- Use these to protect the prototype's core rules before more refactoring.

**Play Mode smoke tests next:**
- Bootstrap `SampleScene`, confirm `PopHeroGame` self-creates, and verify the session can start.
- Add at least one combat loop smoke test:
  - start session
  - launch ball
  - resolve round
  - transition to either next aim state or intermission

**Manual regression checklist to keep even after tests exist:**
- Ball preview and real flight still agree closely.
- Enemy HP changes only at intended presentation timing.
- Blocks only spawn from active inventory, not reserve inventory.
- Shop/remove/swap/install flows still preserve runtime card instances correctly.

## Missing Infrastructure

- No test assembly definitions committed.
- No documented headless test command in the repo.
- No CI status gate.
- No golden-path smoke test for scene bootstrap or UI command handling.

---
*Testing analysis: 2026-04-09*
*Update after the first Unity test assemblies or CI automation are added*

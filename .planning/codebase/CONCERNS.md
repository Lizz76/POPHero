# Concerns

**Analysis Date:** 2026-04-09

## High-Risk Maintenance Areas

**1. Oversized orchestration root**
- `Assets/Scripts/POPHero/Core/PopHeroGame.cs` is 1512 lines.
- It still owns composition, state transitions, rewards flow, debug actions, presentation timing, and multiple facade methods.
- This file is the highest regression-risk location for future feature work and refactors.

**2. Oversized IMGUI surface**
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` is 1187 lines.
- It still mixes rendering, hover state, drag/drop behavior, command emission, layout math, and tooltip construction.
- Even with presenter extraction underway, UI changes can still easily leak business logic back into the HUD.

**3. Multi-responsibility board services file**
- `Assets/Scripts/POPHero/Board/BoardServices.cs` is 716 lines and contains:
  - board context
  - presentation helpers
  - block inventory rules
  - reward generation
  - runtime board generation
- This increases the chance that unrelated board edits collide or regress one another.

## Configuration and Environment Risks

**4. Missing committed config asset**
- `PopHeroGame` tries to load `Resources/PopHeroPrototypeConfig`.
- `Assets/Resources/` is empty in this checkout.
- The game therefore appears to run from fallback defaults rather than a committed tuning asset.
- This makes balance and environment parity harder to reason about across machines.

**5. Editor-version mismatch**
- The project declares `2022.3.62f2c1` in `ProjectSettings/ProjectVersion.txt`.
- The filesystem path you provided is `D:\Unity 2022.3.0f1c1\POPhero\POPHero`.
- If the project is actually being opened with an older local editor install path, editor/package compatibility drift is a real risk.

**6. Generated build artifacts are local-only**
- `docs/AI_HANDOFF.md` references `dotnet build ...POPHero.sln`.
- No committed `*.sln` or `*.csproj` files exist, and `.gitignore` excludes them.
- Build reproducibility therefore depends on Unity regenerating those files locally before CLI validation can run.

## Quality and Regression Risks

**7. No automated test coverage**
- `com.unity.test-framework` is installed, but no tests are committed.
- The repo currently relies on manual playtesting and maintainer docs.
- Any refactor of bounce logic, reward flow, or IMGUI interaction currently has weak regression protection.

**8. Documentation drift**
- `docs/AI_HANDOFF.md` says the project has moved away from the older "simple buff three-choice" and level-based block-count model.
- `docs/DEVLOG_2026-03-31.md` still describes older growth assumptions such as block count scaling with level.
- `README.md` is simpler and does not reflect the full sticker/mod/shop architecture.
- Future maintainers could make wrong decisions if they treat all three docs as equally current.

## Text and Encoding Risks

**9. Source-string encoding corruption**
- Several source files display mojibake-like text when read as UTF-8 or default console output, especially:
  - `Assets/Scripts/POPHero/Systems/ModShopSystems.cs`
  - `Assets/Scripts/POPHero/Board/BoardServices.cs`
  - parts of `Assets/Scripts/POPHero/Core/PopHeroPrototypeConfig.cs`
- This can cause broken player-facing text and makes safe editing harder.

**10. Mixed text systems**
- The project uses IMGUI labels, `TextMesh`, and runtime-created visuals rather than one unified localized UI stack.
- `docs/AI_HANDOFF.md` already warns that Chinese font rendering is sensitive to the creation path.
- Text bugs may surface differently across different UI surfaces.

## Architectural Risks

**11. Transitional facade state**
- The repo is intentionally mid-refactor:
  - new services exist
  - old public methods still remain
  - docs explicitly recommend a gradual migration
- This is a sensible strategy, but it means contributors can accidentally add logic in the wrong layer unless they follow the documented boundaries.

**12. Legacy shell still present**
- `Assets/Scripts/POPHero/Systems/BuffManager.cs` is a placeholder compatibility file.
- It is harmless now, but it is also a trap for future contributors who do not read the handoff doc and start reusing the old buff mental model.

## Tooling / Dependency Risks

**13. Extra package surface for a local prototype**
- `com.ivanmurzak.unity.mcp` brings in a large transitive dependency graph through OpenUPM.
- None of the committed gameplay code appears to rely on networked MCP behavior at runtime.
- This increases restore complexity and editor/package surface area for a prototype that otherwise has no backend.

**14. Registry dependency**
- Package restore depends on both `packages.unity.cn` and `package.openupm.com`.
- Any registry outage or local network restriction can block a fresh setup.

## Working Tree Risk Right Now

**15. Existing unstaged game code changes**
- The repo currently has local modifications in:
  - `Assets/Scripts/POPHero/Board/BoardManager.cs`
  - `Assets/Scripts/POPHero/Board/BoardServices.cs`
  - `Assets/Scripts/POPHero/Core/GameContracts.cs`
  - `Assets/Scripts/POPHero/Core/GameplayTypes.cs`
  - `Assets/Scripts/POPHero/Core/PopHeroGame.cs`
  - `Assets/Scripts/POPHero/UI/PopHeroHud.cs`
  - `Packages/manifest.json`
  - `Packages/packages-lock.json`
  - `ProjectSettings/PackageManagerSettings.asset`
  - `ProjectSettings/ProjectVersion.txt`
- Brownfield planning is still fine, but any future commit strategy must avoid accidentally mixing planning/docs with unrelated gameplay edits.

## What To Protect First

If this project is going to be maintained long-term, the first safety rails should protect:
- shared bounce logic staying unified between preview and actual flight
- active/reserve block inventory semantics
- reward/shop/loadout intermission transitions
- text rendering and encoding hygiene
- `PopHeroGame` and `PopHeroHud` size creep

---
*Concerns analysis: 2026-04-09*
*Update after major refactors, test adoption, or config asset stabilization*

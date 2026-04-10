# Technology Stack

**Analysis Date:** 2026-04-09

## Languages

**Primary:**
- C# - All gameplay, UI, and runtime assembly code under `Assets/Scripts/POPHero/`.

**Secondary:**
- YAML - Unity project settings and scene metadata in `ProjectSettings/*.asset` and `Assets/Scenes/SampleScene.unity`.
- JSON - Package manifests and lockfiles in `Packages/manifest.json` and `Packages/packages-lock.json`.
- TOML - Codex MCP configuration in `.codex/config.toml`.
- Markdown - Project-facing docs in `README.md` and `docs/*.md`.

## Runtime

**Environment:**
- Unity 2022.3.62f2c1 - Declared in `ProjectSettings/ProjectVersion.txt` and used as the gameplay runtime/editor baseline.
- Unity 2D physics runtime - `Rigidbody2D`, `CircleCollider2D`, `Collision2D`, and custom bounce solving drive combat in `Assets/Scripts/POPHero/Combat/BallController.cs` and `Assets/Scripts/POPHero/Combat/BounceStepSolver.cs`.
- In-memory session state - No save system or backend persistence is present; session state lives in `PopHeroGame`, `PlayerData`, `EnemyData`, and `PlayerBlockCollection`.

**Package Manager:**
- Unity Package Manager (UPM) - Dependency declarations live in `Packages/manifest.json`.
- Lockfile present - `Packages/packages-lock.json` captures resolved package versions and registries.
- Registries:
  - `https://packages.unity.cn` - Default package source in `ProjectSettings/PackageManagerSettings.asset`.
  - `https://package.openupm.com` - Scoped registry for `com.ivanmurzak` and related packages.

## Frameworks

**Core:**
- UnityEngine / MonoBehaviour - Main application framework for all gameplay entry points such as `Assets/Scripts/POPHero/Core/PopHeroGame.cs`.
- Unity 2D feature set - Enabled through `com.unity.feature.2d` in `Packages/manifest.json`.
- IMGUI - The primary gameplay HUD is hand-authored in `Assets/Scripts/POPHero/UI/PopHeroHud.cs`.

**Testing:**
- Unity Test Framework 1.1.33 - Installed via `com.unity.test-framework`, but no committed test assemblies or test folders were found under `Assets/`.
- NUnit extension 1.0.6 - Brought in transitively for Unity tests.

**Build/Dev:**
- Rider integration 3.0.36 - `com.unity.ide.rider`.
- Visual Studio integration 2.0.22 - `com.unity.ide.visualstudio`.
- Unity MCP package 0.61.0 - `com.ivanmurzak.unity.mcp`, paired with local Codex MCP config in `.codex/config.toml`.

## Key Dependencies

**Critical:**
- `com.unity.feature.2d` 2.0.1 - Provides the 2D feature bundle this prototype is built around.
- `com.unity.test-framework` 1.1.33 - Makes edit mode / play mode tests possible even though they are not yet authored.
- `com.unity.textmeshpro` 3.0.7 - Available in the project, though the current runtime HUD still leans on IMGUI and `TextMesh`.
- `com.ivanmurzak.unity.mcp` 0.61.0 - Adds AI/MCP integration capabilities for editor-side tooling.

**Infrastructure:**
- `com.unity.collab-proxy` 2.12.4 - Version control/collaboration support package.
- `com.unity.visualscripting` 1.9.4 - Installed, though no gameplay code currently depends on visual scripting assets.
- `com.unity.ugui` 1.0.0 - Installed, but the observed runtime UI is not Canvas-driven.

## Configuration

**Environment:**
- Project version is pinned in `ProjectSettings/ProjectVersion.txt`.
- Package sources are configured in `Packages/manifest.json` and `ProjectSettings/PackageManagerSettings.asset`.
- Local AI tooling is configured in `.codex/config.toml`.

**Gameplay/runtime config:**
- Typed runtime knobs live in `Assets/Scripts/POPHero/Core/PopHeroPrototypeConfig.cs`.
- `PopHeroGame` attempts to load `Resources/PopHeroPrototypeConfig` from `Assets/Resources`, then falls back to `CreateRuntimeDefault()` if no asset exists.
- `Assets/Resources/` is currently empty, so the fallback path appears to be the active configuration path in this checkout.

## Platform Requirements

**Development:**
- Unity Editor 2022.3.62f2c1 is the declared baseline in project settings.
- Windows appears to be the primary authoring environment:
  - `.codex/config.toml` points at a local `http://localhost:27412` MCP server.
  - `docs/AI_HANDOFF.md` calls out Windows font fallback behavior for Chinese text.
- IDE support is expected through Rider or Visual Studio packages.

**Production / target runtime:**
- A standalone game release pipeline is not defined in-repo.
- The current project behaves as a local prototype started from `Assets/Scenes/SampleScene.unity`.
- No cloud deployment, backend hosting, or external live service runtime was observed.

---
*Stack analysis: 2026-04-09*
*Update after editor upgrades, package changes, or UI/runtime architecture changes*

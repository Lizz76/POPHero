# Integrations

**Analysis Date:** 2026-04-09

## Runtime Service Integrations

**Observed status:**
- No gameplay backend, authentication provider, analytics SDK wiring, payments flow, or remote content service was found in the committed gameplay code under `Assets/Scripts/POPHero/`.
- The prototype currently runs as a self-contained local Unity game loop.

## Package and Registry Integrations

**Unity package registry:**
- `ProjectSettings/PackageManagerSettings.asset` points the default registry to `https://packages.unity.cn`.
- `Packages/manifest.json` declares the scoped registry `https://package.openupm.com` for:
  - `com.ivanmurzak`
  - `extensions.unity`
  - `org.nuget.com.ivanmurzak`
  - `org.nuget.microsoft`
  - `org.nuget.system`
  - `org.nuget.r3`

**Implications:**
- Restoring packages depends on both Unity China registry access and OpenUPM availability.
- Package resolution is not purely offline; a fresh checkout without cached packages still needs registry access.

## AI / Tooling Integrations

**Unity MCP package:**
- `Packages/manifest.json` includes `com.ivanmurzak.unity.mcp` 0.61.0.
- `Packages/packages-lock.json` shows that this package pulls in SignalR, JSON, and Microsoft extension libraries through OpenUPM.

**Local Codex MCP server:**
- `.codex/config.toml` enables an MCP server named `ai-game-developer`.
- That config points to `http://localhost:27412`.
- This is a local-machine dependency, not a portable cloud integration.

**Repository docs alignment:**
- `docs/AI_HANDOFF.md` is written as an onboarding/integration guide for AI tooling and future contributors.
- The handoff doc is part of the practical workflow surface area for this repo, even though it is not executable code.

## IDE / Build Integrations

**IDE packages:**
- `com.unity.ide.rider` and `com.unity.ide.visualstudio` are installed through UPM.
- These packages support solution/project generation and IDE synchronization.

**Generated .NET project files:**
- `docs/AI_HANDOFF.md` recommends `dotnet build ...POPHero.sln`.
- No committed `*.sln` or `*.csproj` files exist in the repo root during this analysis.
- `.gitignore` excludes `*.csproj` and `*.sln`, so build files are expected to be Unity-generated per machine.

**Implications:**
- CLI build validation is environment-dependent.
- Any workflow that depends on `dotnet build` first requires Unity to regenerate solution/project files locally.

## Engine / Project Integrations

**Scene bootstrapping:**
- `ProjectSettings/EditorBuildSettings.asset` enables only `Assets/Scenes/SampleScene.unity`.
- `Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs` injects `PopHeroGame` automatically after scene load if one is not already present.

**Config asset loading:**
- `Assets/Scripts/POPHero/Core/PopHeroGame.cs` attempts to load `Resources/PopHeroPrototypeConfig`.
- `Assets/Resources/` is empty in this checkout, so the runtime falls back to programmatic defaults.

## UI / Presentation Integrations

**World-space text and font behavior:**
- `docs/AI_HANDOFF.md` explicitly calls out Windows Chinese font chain behavior through `PrototypeVisualFactory`.
- The active HUD is split between IMGUI (`Assets/Scripts/POPHero/UI/PopHeroHud.cs`) and runtime-created scene objects (`Assets/Scripts/POPHero/UI/PrototypeVisualFactory.cs`).

**Known text risk:**
- Some source files and older docs contain mojibake-like strings, especially in `Assets/Scripts/POPHero/Systems/ModShopSystems.cs` and parts of `Assets/Scripts/POPHero/Board/BoardServices.cs`.
- Text rendering and encoding are therefore part of the effective integration surface for user-facing content.

## Not Present

- No HTTP client calls in gameplay code.
- No database driver usage.
- No cloud save provider.
- No ads, crash reporting, or telemetry SDK wiring observed.
- No webhooks, OAuth, or multiplayer transport integration in committed gameplay code.

## Integration Hotspots

If future work introduces external systems, the most natural insertion points appear to be:
- `Assets/Scripts/POPHero/Core/PopHeroGame.cs` for composition-root wiring.
- `Assets/Scripts/POPHero/Core/GameContracts.cs` for interface contracts.
- `Assets/Scripts/POPHero/Flow/GameFlowControllers.cs` for orchestration hooks.
- `Assets/Scripts/POPHero/UI/PopHeroHud.cs` for any surfaced player feedback.

---
*Integration analysis: 2026-04-09*
*Update after adding live services, analytics, persistence, or editor automation dependencies*

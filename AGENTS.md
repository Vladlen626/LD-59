# Agent Instructions

## Read First (Architecture Entry Points)
- Start with current project composition root:
  - `Assets/_Project/Code/Bootstrap/Bootstrap.cs`
  - `Assets/_Project/Code/Bootstrap/GameRoot.cs`
  - `Assets/_Project/Code/Infrastructure/Startup.cs`
  - `Assets/_Project/Code/Infrastructure/GameLaunchSettings*.cs`
  - `Assets/_Project/Code/Infrastructure/MultiplayerRuntimeBridge.cs`
- Then check PlatformCore architecture sources of truth:
  - `C:/Users/vladl/projects/PlatformCore/README.md`
  - `C:/Users/vladl/projects/PlatformCore/Documentation~/SETUP.md`
  - `C:/Users/vladl/projects/PlatformCore/Documentation~/ARCHITECTURE.md`
  - `C:/Users/vladl/projects/PlatformCore/Documentation~/DOCS_STATUS.md`
- Validate behavior in runtime code (not only docs):
  - `C:/Users/vladl/projects/PlatformCore/Runtime/Infrastructure/BaseBootstrap.cs`
  - `C:/Users/vladl/projects/PlatformCore/Runtime/Infrastructure/BaseGameRoot.cs`
  - `C:/Users/vladl/projects/PlatformCore/Runtime/Infrastructure/Lifecycle/LifecycleService.cs`
  - `C:/Users/vladl/projects/PlatformCore/Runtime/Infrastructure/Composition/Composite*.cs`

## Architecture Rules
- Follow existing flow first; do not invent parallel bootstrap/composition pipelines.
- Use current PlatformCore structure (`Runtime/Core`, `Runtime/Infrastructure`, `Runtime/Services`, `Runtime/Gameplay`) as baseline.
- Do not rely on legacy paths/patterns from old projects if they are not present in this repo.
- Keep responsibilities strict:
  - Controllers: orchestration, subscriptions, lifecycle.
  - Services: reusable global features, no lifecycle registration as controllers.
  - Views: rendering + input forwarding only.
  - Models: state + domain events.

## Lifecycle vs Composite
- `LifecycleService` registers controllers only.
- Services are registered in `ServiceLocator` and initialized via `ISyncInitializable` / `IAsyncInitializable`.
- `Composite` is not mandatory for every feature.
- For a small local feature, prefer direct controller registration in `LifecycleService` when no extra orchestration boundary is needed.
- Use `Composite` only when there is clear value:
  - one activation/deactivation boundary for multiple runtime parts,
  - owned disposables / nested composition ownership,
  - reusable grouped orchestration.
- Never add `Composite` mechanically "for architecture compliance".

## Implementation Defaults
- Before coding a feature, check existing bootstrap/composition/lifecycle rules and reuse them.
- Prefer constructor injection and explicit dependency passing.
- Keep subscribe/unsubscribe symmetric.
- Use `IResourceService` / `IObjectFactory` / centralized path constants; avoid hardcoded runtime resource paths.
- Keep solutions small and local; avoid speculative abstractions.

## Scope Hygiene
- Read only files needed for the task.
- Prioritize `Assets/`, `ProjectSettings/`, `Packages/manifest.json`.
- Skip generated/build folders (`Library/`, `Temp/`, `Obj/`, `Logs/`, `Builds/`, `.git/`, `.vs/`, `.idea/`, `UserSettings/`).

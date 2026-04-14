# Multiplayer Quickstart (FishNet, Minimal)

This is a narrow vertical slice for template validation, not a multiplayer framework.

## 1. Install FishNet

Use `Len/Installer -> Install Optional Dependencies` and confirm FishNet is imported.

## 2. Scene Setup (Persistent)

Add FishNet `NetworkManager` object to `Persistent` scene.

Required on the same manager object:
- FishNet manager components (default NetworkManager setup).
- `PlayerSpawner` component.

## 3. Player Prefab Setup

Use `Assets/Resources/Characters/Player.prefab` as player prefab and ensure it has:
- `NetworkObject`
- `NetworkTransform`
- `NetworkOwnedPlayerBootstrap` (from `Assets/_Project/Code/Network`)

Assign this prefab in FishNet `PlayerSpawner`.

## 4. Launch Modes

- Host: run build/editor with `-host`
- Client: run build/editor with `-client -address 127.0.0.1 -port 7770`
- Singleplayer default: run without args

## 5. Expected Behavior

- Host + client spawn separate player objects.
- Input is owner-only (only local owner can move its player).
- Camera is owner-only (local owner camera attaches to local player only).
- Transform sync comes from `NetworkTransform` on player prefab.

## 6. Smoke Validation

Follow `PlatformCore/Documentation~/NETWORK_SMOKE_TEST_PLAN.md`.

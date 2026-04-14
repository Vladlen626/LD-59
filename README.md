# GameTemplate

Thin starter project on top of `com.len.platformcore`.

## Goal

- Provide minimal bootstrap flow.
- Provide a working sample scene startup path.
- Keep game-specific systems in `_Project`.

## Package Baseline

- `com.len.platformcore` (local package)
- `com.cysharp.unitask`
- `com.kyrylokuzyk.primetween`

## Runtime Entry Points

- `Assets/_Project/Code/Bootstrap/Bootstrap.cs`
- `Assets/_Project/Code/Bootstrap/GameRoot.cs`
- `Assets/_Project/Code/Infrastructure/Startup.cs`

## Scene Entry Points

- `Assets/_Project/Scenes/Persistent.unity` (bootstrap scene)
- `Assets/_Project/Scenes/SampleScene.unity` (sample gameplay scene)

## First Run

1. Open `Persistent.unity`.
2. Ensure Build Settings has `Persistent` and `SampleScene` enabled.
3. In Unity menu run `Len/Installer -> Validate Setup`.
4. Optionally run:
   - `Len/Resources/Generate ResourcePaths`
   - `Len/Audio/Generate SoundNames` (if FMOD is installed)

## Start Modes

GameTemplate now supports launch-mode arguments:
- default (no args): singleplayer path
- `-host`: multiplayer host path
- `-client`: multiplayer client path
- optional with client/host: `-address <ip> -port <port>`

See [MULTIPLAYER_QUICKSTART.md](./MULTIPLAYER_QUICKSTART.md) for required FishNet scene/prefab setup.
See [START_NEW_GAME.md](./START_NEW_GAME.md) for concise start checklist.

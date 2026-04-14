# Start New Game

## Singleplayer Baseline

1. Open `Assets/_Project/Scenes/Persistent.unity`.
2. Ensure Build Settings include `Persistent` and `SampleScene`.
3. Run game without command-line args.
4. Replace sample gameplay from `_Project/Code/Gameplay` with your game-specific systems.

## Multiplayer Baseline (Optional, FishNet)

1. Complete `MULTIPLAYER_QUICKSTART.md` setup.
2. Host run: `-host`.
3. Client run: `-client -address 127.0.0.1 -port 7770`.
4. Verify owner-only input/camera and transform sync.

## Keep Architecture Boundaries

- PlatformCore: foundation services and runtime infrastructure.
- GameTemplate: game-specific flow/controllers/content.

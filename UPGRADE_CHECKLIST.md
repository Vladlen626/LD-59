# Upgrade Checklist

- [ ] Commit current project state.
- [ ] Update `com.len.platformcore` package reference.
- [ ] Reopen Unity and wait for package reimport.
- [ ] Run `Len/Installer -> Validate Setup`.
- [ ] Re-check FMOD integration if used.
- [ ] Re-run `Len/Resources/Generate ResourcePaths`.
- [ ] Re-run `Len/Audio/Generate SoundNames` if FMOD events changed.
- [ ] Build solution and smoke-test startup from `Persistent`.
- [ ] If multiplayer path is used: verify FishNet setup and rerun `MULTIPLAYER_QUICKSTART.md` smoke.

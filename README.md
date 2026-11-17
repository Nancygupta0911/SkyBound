# SKYBOUND

A stylized 3D floating-island platformer built in Unity 6 (URP).

## Play in your browser

**https://nancygupta0911.github.io/SkyBound/**

The WebGL build is published from the `gh-pages` branch and runs in any
desktop browser with WebGL 2.

## Play in the editor

1. Open the project in Unity 6 (6000.6.9f1).
2. Open `Assets/_Project/Scenes/Skybound.unity`.
3. Press **Play**, then press **PLAY** on the title screen.

## Build

The project ships one WebGL build profile: `Assets/Settings/Build Profiles/Skybound - Desktop - Release.asset`.

```
unity build --profile "Skybound - Desktop - Release" --output Builds/WebGL --allow-dirty-build
```

Publishing: copy `Builds/WebGL` into a clean branch, push it as `gh-pages`
(the build is Gzip-compressed with decompression fallback, so it works on
static hosts without custom headers).

## Controls

| Action | Input |
| --- | --- |
| Move | `WASD` / arrow keys |
| Jump | `Space` (hold for a higher jump) |
| Orbit camera | Mouse |

## Goal

Collect all **58 coins** across the islands without falling into the open sky below.
Touching a coin adds it to the HUD counter with a particle burst. Falling below the
level shows **YOU FELL!** with your final score; **RESTART** resets the run, the
coins and the player.

## Project layout

```
Assets/_Project/
  Art/          Kenney + Quaternius models, textures, audio and licenses
  Materials/    URP Lit materials, skybox and post-process volume
  Prefabs/      Platforms, coins, props, player and pickup effect
  Scenes/       Skybound.unity
  Scripts/      Gameplay and UI code
  UI/           Generated sprites and the TMP font asset
  CREDITS.md    Asset attribution and licenses
```

## Scripts

| Script | Responsibility |
| --- | --- |
| `PlayerController` | Camera-relative movement, coyote time, jump buffering, animation |
| `CameraFollow` | Smooth third-person orbit with sphere-cast collision |
| `Coin` | Spin/bob, pickup burst and reset |
| `GameManager` | Run state, score, fall detection, spawn and restart |
| `UIManager` | Title screen, HUD, Game Over and UI animation |
| `DeathZone` | Trigger volume that ends the run below the level |

All art is CC0 or CC-BY; see `Assets/_Project/CREDITS.md` for details.

# SKYBOUND

A stylized 3D floating-island platformer built in Unity 6 (URP), playable flat
in a browser or in VR with a headset.

## Play in your browser

**https://nancygupta0911.github.io/SkyBound/**

The WebGL build is published from the `gh-pages` branch and runs in any
desktop browser with WebGL 2.

## Play in VR

`Builds/WindowsVR/SKYBOUND.exe` is a Windows VR build (OpenXR). Start your
headset's OpenXR runtime first (Meta Quest Link / SteamVR / Windows Mixed
Reality), then launch the exe. The game picks VR automatically when a headset
is running; without one it falls back to the flat camera and the same exe
plays like the desktop build.

The camera keeps the flat game's third-person framing and adds your head
rotation on top, so you can look around the island while the character stays
centred. The HUD becomes a panel that floats in front of you.

| Action | Input |
| --- | --- |
| Move | Left stick |
| Snap turn (30°) | Right stick left/right |
| Jump / confirm | A (right) or X (left) |
| Start run / restart | A or X while a menu is on screen |
| Mute / unmute | B (right) or Y (left) |
| Keyboard fallback | `WASD`, `Space`, `Q`/`E` to turn, mouse to look |

A soft vignette fades in while you move, which is the usual comfort trick for
third-person VR.

### VR without a headset (editor)

The editor always starts in **VR preview**: the VR rig drives the camera,
mouse-look replaces head tracking and `Q`/`E` snap turn. That means the rig,
world-space HUD and controller buttons can all be tested with no hardware.

To compare against the flat camera, use the menu
**Skybound ▸ VR ▸ Use Flat Camera In Editor** and enter play mode.

## Play in the editor

1. Open the project in Unity 6 (6000.6.9f1).
2. Open `Assets/_Project/Scenes/Skybound.unity`.
3. Press **Play**, then press **PLAY** on the title screen.

## Build

The project ships one WebGL build profile: `Assets/Settings/Build Profiles/Skybound - Desktop - Release.asset`.

```
unity build --profile "Skybound - Desktop - Release" --output Builds/WebGL --allow-dirty-build
```

Windows VR build (needs the Windows build support module; no extra XR module):

```
unity build --target StandaloneWindows64 --output Builds/WindowsVR --allow-dirty-build
```

OpenXR is configured for the Standalone build target only, so the WebGL build
stays flat and unaffected.

Publishing: copy `Builds/WebGL` into a clean branch, push it as `gh-pages`
(the build is Gzip-compressed with decompression fallback, so it works on
static hosts without custom headers).

## Controls

| Action | Input |
| --- | --- |
| Move | `WASD` / arrow keys |
| Jump | `Space` (hold for a higher jump) |
| Orbit camera | Mouse |
| Mute / unmute | `M` or the on-screen sound button |

VR controls are listed in [Play in VR](#play-in-vr).

## Goal

Collect all **58 coins** across the islands without falling into the open sky below.
The HUD shows a live `COINS: n / 58` counter that pulses on every pickup. Touching
a coin adds it to the counter with a particle burst; falling below the level shows
**YOU FELL!** with your final score, and collecting every coin ends the run on
**YOU WIN!**. **RESTART** resets the run, the coins and the player.

Music and sound effects (pickup, jump, landing, footsteps, fall, win) are CC0:
Kenney packs plus an original theme synthesized for this project.

## Project layout

```
Assets/_Project/
  Art/          Kenney + Quaternius models, textures and licenses
  Audio/        Music loop and sound effects
  Materials/    URP Lit materials, skybox and post-process volume
  Prefabs/      Platforms, coins, props, player and pickup effect
  Scenes/       Skybound.unity
  Scripts/      Gameplay and UI code
  Scripts/VR/   XR session, rig, world-space canvas and comfort vignette
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
| `UIManager` | Title screen, coin HUD, result screens, mute button and UI animation |
| `AudioManager` | Music loop, pooled SFX, persisted mute toggle |
| `DeathZone` | Trigger volume that ends the run below the level |
| `VRSession` | Detects the headset, waits for XR init, exposes the active rig/camera |
| `VRRig` | Third-person orbit rig, head rotation, snap turn, VR menu input |
| `VRWorldCanvas` | Turns the overlay UI into a billboarded world-space panel |
| `VRComfortVignette` | Procedural vignette that fades in while moving |

Head tracking reads `CommonUsages.deviceRotation` from the centre-eye
`InputDevice` instead of using a `TrackedPoseDriver`. That keeps the editor
preview (mouse-look, no headset) predictable while still using the OpenXR
head pose in a build.

All art is CC0 or CC-BY; see `Assets/_Project/CREDITS.md` for details.

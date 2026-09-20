# FrugalTargeting

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Nuclear Option** that makes multi-target weapon launches smarter, so you stop wasting missiles and bombs.

In the base game, when you lock several targets the fire indicator only shows SHOOT if *every* target is in range and arc, and one press fires at all of them regardless. FrugalTargeting judges each target on its own, marks the ones that won't be fired at, and (optionally) only launches at the ones that qualify. Lock targets in two different directions, turn toward one group, and fire at just that group.

## Features

### Launch Authorization modes

Press the toggle key (default `C`) to cycle through three modes. The game always starts in **Default**, and an on-screen message confirms each switch.

| Mode | What it does |
|---|---|
| **Default** | Vanilla firing. Pressing fire launches at every locked target. Targets that wouldn't be fired at under the stricter modes are still marked. |
| **Strict** | Fires only at targets that currently qualify. If none qualify, pressing fire does nothing. |
| **Strict Fire Once** | Strict, plus each target is fired at only once. Fired targets are marked. When every locked target has been fired at, the next fire press only clears the marks and launches nothing; press fire again for a fresh round. |

In Strict modes each launch of a salvo is re-checked at the moment it happens. If you have turned away from a target by its turn in the salvo, that shot is skipped, and the target is not marked as fired.

### Marker colors

Selected targets are recolored on your HUD every frame:

| Color | Meaning |
|---|---|
| Green | Selected and will be fired at (the game's normal selected color) |
| Yellow | Selected but won't be fired at (out of range, arc, window, or not lased) |
| Magenta | Already fired at (Strict Fire Once) |

Both colors are configurable. The yellow marking works in every mode, including Default.

### Weapon support

| Weapon | Rule for a target to qualify |
|---|---|
| **Missiles** | Minimum range, launch speed, arc (`minAlignment`), and the missile's own range at that target's distance and altitude. |
| **Bombs** | The target must be ahead of and below you, and the release countdown (the HUD's `REL`) must be within a window of zero. Default is 2 s, the same range where the game's `REL` text turns green. |
| **Laser-guided weapons** | The target must be lased by your own designator. Laser bombs use the bomb rule as well. Other laser weapons use the game's laser-HUD range and arc rule. |
| **Glide bombs** | Use the missile rule. |
| **Guns, cargo, slings** | Never filtered. |

### Fire indicator

On the missile HUD, in Strict modes, the SHOOT indicator lights when *any* locked target qualifies. It shows `SHOOT n/m` when only some do. In Strict Fire Once it shows `ALL FIRED - PRESS FIRE TO RESET` when every target has been fired at. Bomb and laser HUDs have no SHOOT text of their own here, so the marker colors are the feedback.

## Requirements

- Nuclear Option (Steam)
- [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases), **x64 Mono**
- Optional: [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) to edit settings in game with `F1`

If you use ConfigurationManager, set `HideManagerGameObject = true` under `[Chainloader]` in `BepInEx\config\BepInEx.cfg`.

## Install

1. Install BepInEx 5.x into your Nuclear Option folder (it should contain `winhttp.dll` and a `BepInEx` folder).
2. Create `Nuclear Option\BepInEx\plugins\FrugalTargeting\`.
3. Copy `FrugalTargeting.dll` into it.
4. Launch the game. `BepInEx\LogOutput.log` should contain `Frugal Targeting 0.1.0 loaded`.

To uninstall, delete the `FrugalTargeting` folder from `BepInEx\plugins\`.

## Configuration

The config file is created on first run at `BepInEx\config\com.heck0.frugaltargeting.cfg`. Every setting is also available in the `F1` menu.

| Section | Setting | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Master switch for all behavior. |
| Targeting | `ToggleModeKey` | `C` | Key that cycles the launch authorization mode. Only the main key and any modifiers written into the binding are required; other held keys (like flight controls) are ignored. |
| Targeting | `BombReleaseWindowSeconds` | `2` | Bombs qualify when the `REL` countdown is within this many seconds of zero (0.5 to 15). |
| Targeting | `LaserAllowFactionLasing` | `false` | Also accept targets lased by a teammate. By default only your own designator counts. |
| HUD | `ColorUnengageableTargets` | `true` | Recolor selected targets that won't be fired at. |
| HUD | `UnengageableColor` | yellow | Marker color for those targets. |
| HUD | `FiredColor` | magenta | Marker color for targets already fired at (Strict Fire Once). |
| Debug | `LogWeaponInfo` | `true` | Log each weapon's type flags and target requirements when you switch station. |

## Notes and limitations

- **Client-side only.** Launches are owned by the firing player's game, so nothing has to be installed on the server or on other players' machines. Whether a given server allows mods is up to that server.
- **"Fired" means launched, not hit.** A target turns magenta when a round leaves the rail. A missed shot still counts.
- **Fired marks are shared across weapons.** A target hit with a missile is also skipped by bombs until you reset. Deselecting and re-locking a target resets it, and changing mode clears all marks.
- **The target list is fixed when you press fire.** A target that becomes eligible mid-salvo is not added, but one that stops being eligible is skipped.
- **Bomb accuracy is limited by the game's own release estimate**, which ignores drag and assumes you hold your speed and heading. If bombs land off target, try lowering `BombReleaseWindowSeconds`.
- **Guns** never turn a target magenta and are not blocked.
- **Other mods** that change target selection or recolor HUD markers may conflict.

## Building

Requires the .NET SDK. The project references the game's assemblies from your install, using the `NUCLEAR_OPTION_DIR` environment variable or falling back to `C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option`. Building also copies the DLL into `BepInEx\plugins\FrugalTargeting\` in that folder.

```
dotnet build FrugalTargeting\FrugalTargeting.csproj -c Release
```

To build for a different install location without setting the environment variable:

```
dotnet build FrugalTargeting\FrugalTargeting.csproj -c Release -p:GameDir="D:\Games\Nuclear Option"
```

Close the game first, because Windows will not overwrite a loaded DLL.

# LaunchAuthorization+

*Multi-target weapons release authorization for Nuclear Option.*

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Nuclear Option**.

## 1. Purpose

In the unmodified game, releasing on a multi-target set is all-or-nothing. The release cue shows `SHOOT` only when every designated target is within range and arc, and a single trigger press launches against every designated target regardless of whether it is within parameters.

LaunchAuthorization+ evaluates each designated target independently against its launch acceptable region (LAR), the envelope of range, arc and timing in which a weapon can be employed against it. Targets outside their LAR are flagged, and in the stricter modes launch is restricted to targets that are inside it. This allows the operator to designate targets on different bearings, turn toward one group, and launch against that group only.

## 2. Release Authorization Modes

Tap the mode key (default `C`) to cycle through three modes. The mode changes when the key is released. The system initializes in **Default** at every game start, and an on-screen advisory confirms each mode change.

| Mode | Description |
|---|---|
| **Default** | Unmodified release. One trigger press launches against every designated target. Targets outside parameters are still flagged. |
| **Strict** | Launch is authorized only against targets currently within parameters. If none are, the trigger press is inhibited. |
| **Strict Fire Once** | Strict, with a limit of one launch per target per engagement cycle. See [Section 3](#3-strict-fire-once). |

In both Strict modes, each launch in a salvo is re-evaluated at the moment it is made. If the aircraft has rotated out of parameters on a target by that shot's turn in the salvo, the launch is skipped and the target is not flagged as engaged.

## 3. Strict Fire Once

Strict Fire Once applies all Strict criteria and adds a re-attack lockout: **each designated target is engaged once, and only once, per cycle.** This allows a target set too large or too widely spread to be prosecuted from a single attack heading to be worked across multiple passes, without expending a second weapon on a target already engaged.

Example: a large number of ground targets is spread across a fairly wide area, and no single heading lines up with all of them. Designate the full set, then make an attack pass and press the trigger. Weapons are launched against the targets within parameters at that moment, and those targets are flagged magenta. Reposition onto another part of the area and press the trigger again. Flagged targets are skipped, so no weapon is expended on them, and only targets not yet engaged receive weapons. Continue making passes until every target is flagged.

### 3.1 Procedure

1. Designate all targets.
2. Make an attack pass and press the trigger. A weapon is launched against each target within parameters at that instant. Each engaged target is flagged **magenta**.
3. Reposition and press the trigger again. Flagged targets are locked out. Only targets not yet engaged and currently within parameters receive weapons.
4. Targets outside parameters (yellow) are left alone and remain unengaged, so they can be prosecuted once the aircraft is in position.
5. Repeat until all designated targets are flagged.

A target is flagged only when a weapon physically leaves the station. A launch skipped because the aircraft had rotated out of parameters by that shot's turn in the salvo does not flag the target.

Until every designated target is flagged, a trigger press launches only against targets that are both unengaged and currently within parameters. If there are none, the trigger press is inhibited, as in Strict.

### 3.2 Cycle Reset

| Method | Procedure |
|---|---|
| **Complete** | When every designated target is flagged, the missile HUD displays `ALL ENGD - RESET`. The next trigger press clears all flags and inhibits launch. Press the trigger again to begin a new cycle on the same targets. |
| **Manual** | Hold the mode key for approximately 0.5 seconds (`ResetHoldSeconds`). All flags are cleared, the mode remains Strict Fire Once, and a "Fired marks reset" advisory is displayed. A quick tap still cycles the mode. Holding the key in the other modes has no effect. |
| **Single target** | Deselect the target and designate it again to clear its flag only. |
| **Mode change** | Cycling modes clears all flags. |

### 3.3 Notes

- **"Fired" means launched, not a confirmed hit.** A launch that misses is still counted and remains flagged until reset.
- **Flags are shared across weapons.** A target engaged with a missile is also skipped by bombs until reset.
- **Guns do not flag targets.** Only missiles, bombs and laser-guided weapons do.

## 4. Target Flagging

Designated targets are recolored on the HUD every frame.

| Color | Meaning |
|---|---|
| Green | Designated, within parameters (the game's normal selected color). |
| Yellow | Designated, outside parameters (range, arc, release window, or not lased). |
| Magenta | Already engaged (Strict Fire Once). |

The yellow and magenta colors are configurable. Yellow flagging is active in every mode, including Default.

## 5. Weapons Employment

Each target is evaluated independently, following the game's own indicators.

| Weapon | Behavior |
|---|---|
| **Missiles** | Mirrors when the game shows the `SHOOT` indicator. |
| **Bombs** | Mirrors when the game's `REL` indicator turns green. |
| **Laser-guided weapons** | Follow the missile or bomb rules, and the target must also be lased by the operator's own designator. |
| **Glide bombs** | Mirror when the game shows the `SHOOT` indicator. |
| **Guns** | Unchanged. |
| **Cargo and slings** | Unchanged. |

## 6. Fire Indicator

On the missile HUD, in both Strict modes, the following cues are displayed:

| Cue | Meaning |
|---|---|
| `SHOOT` | Every designated target is within the LAR. |
| `SHOOT n/m` | Only `n` of `m` designated targets are within the LAR. |
| `NO TGT IN LAR` | No designated target is within the LAR. The trigger press is inhibited. |
| `ALL ENGD - RESET` | Strict Fire Once only. Every designated target has been engaged. The next trigger press resets the cycle. |

The bomb and laser HUDs have no `SHOOT` text of their own, so target flagging (Section 4) is the cue on those weapons.

## 7. Requirements

- Nuclear Option (Steam)
- [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases), **x64 Mono**
- Optional: [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager), for editing settings in game with `F1`

If ConfigurationManager is used, set `HideManagerGameObject = true` under `[Chainloader]` in `BepInEx\config\BepInEx.cfg`.

## 8. Installation

1. Install BepInEx 5.x into the Nuclear Option folder. The folder should contain `winhttp.dll` and a `BepInEx` folder.
2. Create `Nuclear Option\BepInEx\plugins\LaunchAuthorizationPlus\`.
3. Copy `LaunchAuthorizationPlus.dll` into it.
4. Launch the game. `BepInEx\LogOutput.log` should contain `LaunchAuthorization+ 0.1.0 loaded`.

To uninstall, delete the `LaunchAuthorizationPlus` folder from `BepInEx\plugins\`.

## 9. Configuration

The config file is created on first run at `BepInEx\config\com.heck0.launchauthorizationplus.cfg`. Every setting is also available in the `F1` menu.

| Section | Setting | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Master switch for all behavior. |
| Targeting | `ToggleModeKey` | `C` | Tap to cycle the release authorization mode; hold to reset flags in Strict Fire Once. Only the main key and any modifiers written into the binding are required; other held keys (such as flight controls) are ignored. |
| Targeting | `ResetHoldSeconds` | `0.5` | Hold time for the manual reset (0.2 to 3). A shorter press is a tap and cycles the mode. |
| Targeting | `BombReleaseWindowSeconds` | `2` | Bombs are within parameters when the `REL` countdown is within this many seconds of zero (0.5 to 15). |
| Targeting | `LaserAllowFactionLasing` | `false` | Also accept targets lased by a teammate. By default only the operator's own designator counts. |
| HUD | `ColorUnengageableTargets` | `true` | Recolor designated targets that are outside parameters. |
| HUD | `UnengageableColor` | yellow | Flag color for targets outside parameters. |
| HUD | `FiredColor` | magenta | Flag color for targets already engaged (Strict Fire Once). |
| Debug | `LogWeaponInfo` | `true` | Log each weapon's type flags and target requirements when the weapon station is changed. |

## 10. Notes and Limitations

- **Client-side only.** Launches are owned by the launching player's game, so nothing has to be installed on the server or on other players' machines. Whether a given server permits mods is determined by that server.
- **The target set is fixed at the trigger press.** A target that comes within parameters mid-salvo is not added. A target that leaves parameters mid-salvo is skipped.
- **Bomb accuracy is limited by the game's own release estimate**, which ignores drag and assumes the aircraft holds its speed and heading. If bombs land off target, lower `BombReleaseWindowSeconds`.
- **Other mods** that change target selection or recolor HUD markers may conflict.

## 11. Building

Requires the .NET SDK. The project references the game's assemblies from the installation, using the `NUCLEAR_OPTION_DIR` environment variable or falling back to `C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option`. Building also copies the DLL into `BepInEx\plugins\LaunchAuthorizationPlus\` in that folder.

```
dotnet build LaunchAuthorizationPlus\LaunchAuthorizationPlus.csproj -c Release
```

To build against a different installation without setting the environment variable:

```
dotnet build LaunchAuthorizationPlus\LaunchAuthorizationPlus.csproj -c Release -p:GameDir="D:\Games\Nuclear Option"
```

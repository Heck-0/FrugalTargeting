# LaunchAuthorization+

*Multi-target weapons release authorization for Nuclear Option.*

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Nuclear Option**.

## 1. Purpose

In the unmodified game, a multi-target launch is all-or-nothing: `SHOOT` appears only when every selected target is within range and arc, and one fire button press launches against all of them. LaunchAuthorization+ evaluates each selected target against its launch acceptable region (LAR), flags those outside it, and in the stricter modes launches only against those inside it. This allows the operator to select targets on different bearings, turn toward one group, and fire on that group only.

## 2. Release Authorization Modes

Tap the mode key (default `C`) to cycle through three modes. The mode changes when the key is released. The system initializes in **Default** at every game start, and an on-screen advisory confirms each mode change.

| Mode | Description |
|---|---|
| **Default** | Unmodified release. One fire button press launches against every selected target. Targets outside parameters are still flagged. |
| **Strict** | Launch is authorized only against targets currently within parameters. If none are, the fire button press is inhibited. |
| **Strict Fire Once** | Strict, with a limit of one launch per target per engagement cycle. See [Section 3](#3-strict-fire-once). |

In both Strict modes, each launch in a salvo is re-evaluated at the moment it is made. If the aircraft has rotated out of parameters on a target by that shot's turn in the salvo, the launch is skipped and the target is not flagged as engaged.

## 3. Strict Fire Once

Strict Fire Once applies all Strict criteria and adds a re-attack lockout: **each selected target is engaged once, and only once, per cycle.** This allows a target set too large or too widely spread to be prosecuted from a single attack heading to be worked across multiple passes, without expending a second weapon on a target already engaged.

Example use case: a large number of ground targets is spread across a fairly wide area, and no single heading lines up with all of them. Select the full set, then make an attack pass and press the fire button. Weapons are launched against the targets within parameters at that moment, and those targets are flagged magenta. Reposition onto another part of the area and press the fire button again. Flagged targets are skipped, so no weapon is expended on them, and only targets not yet engaged receive weapons. Continue making passes until every target is flagged.

### 3.1 Procedure

1. Select all targets.
2. Make an attack pass and press the fire button. A weapon is launched against each target within parameters at that instant. Each engaged target is flagged **magenta**.
3. Reposition and press the fire button again. Flagged targets are locked out. Only targets not yet engaged and currently within parameters receive weapons.
4. Targets outside parameters (yellow) are left alone and remain unengaged, so they can be prosecuted once the aircraft is in position.
5. Repeat until all selected targets are flagged.

A target is flagged only when a weapon physically leaves the station. A launch skipped because the aircraft had rotated out of parameters by that shot's turn in the salvo does not flag the target.

Until every selected target is flagged, a fire button press launches only against targets that are both unengaged and currently within parameters. If there are none, the fire button press is inhibited, as in Strict.

### 3.2 Cycle Reset

| Method | Procedure |
|---|---|
| **Complete** | When every selected target is flagged, the missile HUD displays `ALL ENGD - RESET`. The next press of fire button clears all flags without launching anyting. Press the fire button to begin a new cycle on the same targets. |
| **Manual** | Hold the mode key for approximately 0.5 seconds (`ResetHoldSeconds`). All flags are cleared, the mode remains Strict Fire Once, and a "Fired marks reset" advisory is displayed. A quick tap still cycles the mode. Holding the key in the other modes has no effect. |
| **Single target** | Deselect the target and select it again to clear its flag only. |
| **Mode change** | Cycling modes clears all flags. |

### 3.3 Notes

- **"Fired" means launched, not a confirmed hit.** A launch that misses is still counted and remains flagged until reset.
- **Flags are shared across weapons.** A target engaged with a missile is also skipped by bombs until reset.
- **Guns do not flag targets.** Only missiles, bombs and laser-guided weapons do.

## 4. Target Flagging

Selected targets are recolored on the HUD every frame.

| Color | Meaning |
|---|---|
| Green | Selected, within parameters (the game's normal selected color). |
| Yellow | Selected, outside parameters (range, arc, release window, or not lased). |
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
| `SHOOT` | Every selected target is within the LAR. |
| `SHOOT n/m` | Only `n` of `m` selected targets are within the LAR. |
| `NO TGT IN LAR` | No selected target is within the LAR. The fire button press is inhibited. |
| `ALL ENGD - RESET` | Strict Fire Once only. Every selected target has been engaged. The next fire button press resets the cycle. |

The bomb and laser HUDs have no `SHOOT` text of their own, so target flagging (Section 4) is the cue on those weapons.

## 7. Requirements

- Nuclear Option (Steam)
- [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases), **x64 Mono**
- Optional: [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager), for editing settings in game with `F1`

If ConfigurationManager is used, set `HideManagerGameObject = true` under `[Chainloader]` in `BepInEx\config\BepInEx.cfg`.

## 8. Installation

1. Confirm BepInEx 5.x is installed (Section 7).
2. Download `LaunchAuthorizationPlus.dll` from the [latest release](https://github.com/Heck-0/LaunchAuthorizationPlus/releases/latest).
3. Create the folder `Nuclear Option\BepInEx\plugins\LaunchAuthorizationPlus\` and place the DLL in it.
4. Launch the game. `BepInEx\LogOutput.log` should contain `LaunchAuthorization+` followed by the version and `loaded`.

To uninstall, delete the `LaunchAuthorizationPlus` folder from `BepInEx\plugins\`.

## 9. Configuration

The config file is created on first run at `BepInEx\config\com.heck0.launchauthorizationplus.cfg`. Every setting is also available in the `F1` menu.

| Section | Setting | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Master switch. |
| Targeting | `ToggleModeKey` | `C` | Tap to cycle modes; hold to reset flags in Strict Fire Once. Other held keys are ignored. |
| Targeting | `ResetHoldSeconds` | `0.5` | Hold time for the manual reset, in seconds (0.2 to 3). |
| Targeting | `BombReleaseWindowSeconds` | `2` | Bombs qualify within this many seconds of zero on the `REL` countdown (0.5 to 15). |
| Targeting | `LaserAllowFactionLasing` | `false` | Also accept targets lased by a teammate, not only your own designator. |
| HUD | `ColorUnengageableTargets` | `true` | Recolor targets outside parameters. |
| HUD | `UnengageableColor` | yellow | Color for targets outside parameters. |
| HUD | `FiredColor` | magenta | Color for engaged targets (Strict Fire Once). |
| Debug | `LogWeaponInfo` | `true` | Log weapon flags and requirements when the weapon station changes. |

## 10. Notes and Limitations

- **Client-side only.** Launches are owned by the launching player's game, so nothing has to be installed on the server or on other players' machines. Whether a given server permits mods is determined by that server.
- **The target set is fixed at the fire button press.** A target that comes within parameters mid-salvo is not added. A target that leaves parameters mid-salvo is skipped.
- **Bomb accuracy is limited by the game's own release estimate**, which ignores drag and assumes the aircraft holds its speed and heading. If bombs land off target, lower `BombReleaseWindowSeconds`.
- **Other mods** that change target selection or recolor HUD markers may conflict.

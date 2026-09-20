using System.Collections.Generic;
using HarmonyLib;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FrugalTargeting
{
    /// <summary>
    /// The game only shows SHOOT when every selected target passes the range/arc checks.
    /// Show it when at least one does, along with how many.
    /// </summary>
    [HarmonyPatch(typeof(HUDMissileState), "DisplayText")]
    internal static class HudIndicatorPatch
    {
        // True while our override is on screen, so it can be undone the moment no target qualifies.
        private static bool forced;

        static void Postfix(
            ref bool ___allRequirementsMet, bool ___hidden,
            Image ___noShoot, TextMeshProUGUI ___hint, Aircraft ___aircraft,
            WeaponStation ___weaponStation, List<Unit> ___targetList)
        {
            if (!Plugin.Strict || ___hidden || ___targetList.Count == 0)
            {
                forced = false;
                return;
            }

            if (Plugin.FireOnce && FiredTracker.AllFired(___targetList))
            {
                forced = false;
                ___allRequirementsMet = false;
                ___noShoot.enabled = true;
                ___hint.enabled = true;
                ___hint.text = "ALL FIRED - PRESS FIRE TO RESET";
                return;
            }

            var ok = Engageability.ThisFrame(___aircraft, ___weaponStation, ___targetList);
            if (ok.Count == 0)
            {
                if (forced)
                {
                    // The game's own text is stale (it refreshes on its own tick), so show a neutral reason.
                    forced = false;
                    ___allRequirementsMet = false;
                    ___noShoot.enabled = true;
                    ___hint.enabled = true;
                    ___hint.text = "NO VALID TARGET";
                }
                return;
            }

            forced = true;
            ___allRequirementsMet = true;
            ___noShoot.enabled = false;
            ___hint.enabled = true;
            int total = ___targetList.Count - (Plugin.FireOnce ? FiredTracker.CountIn(___targetList) : 0);
            ___hint.text = ok.Count < total ? $"SHOOT {ok.Count}/{total}" : "SHOOT";
        }
    }

    /// <summary>Tint selected markers when the target won't be fired at.</summary>
    [HarmonyPatch(typeof(CombatHUD), "LateUpdate")]
    internal static class MarkerColorPatch
    {
        static void Postfix(
            Aircraft ___aircraft, WeaponStation ___currentWeaponStation,
            List<Unit> ___targetList, Dictionary<Unit, HUDUnitMarker> ___markerLookup)
        {
            FiredTracker.Prune(___targetList);

            if (!Plugin.Enabled.Value || !Plugin.ColorUnengageable.Value) return;
            if (___aircraft == null || ___currentWeaponStation == null || ___targetList.Count == 0) return;

            Engageability.ThisFrame(___aircraft, ___currentWeaponStation, ___targetList);

            var selected = ThemeManager.Active.ColorTheme.HudUnitSelected;
            foreach (var unit in ___targetList)
            {
                if (unit == null || !___markerLookup.TryGetValue(unit, out var marker) || marker.image == null) continue;

                if (Plugin.FireOnce && FiredTracker.IsFired(unit))
                    marker.image.color = Plugin.FiredColor.Value;
                else
                    marker.image.color = Engageability.EngageableThisFrame(unit) ? selected : Plugin.UnengageableColor.Value;
            }
        }
    }

    /// <summary>
    /// Restrict the salvo to targets engageable at the moment of the press, so we don't spend salvo time on
    /// targets that are obviously out (each shot is re-checked again at launch by LaunchTrackPatch).
    /// </summary>
    [HarmonyPatch(typeof(WeaponManager), "SalvoFire")]
    internal static class SalvoFilterPatch
    {
        static void Prefix(ref List<Unit> targets, Aircraft ___aircraft, WeaponStation ___currentWeaponStation)
        {
            if (!Plugin.Strict) return;
            var ok = Engageability.Filter(___aircraft, ___currentWeaponStation, targets);
            if (ok.Count > 0) targets = ok;
        }
    }

    /// <summary>Strict launch authorization: do nothing when targets are selected but none can be engaged.</summary>
    [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.Fire))]
    internal static class NoEngageableTargetsPatch
    {
        static bool Prefix(Aircraft ___aircraft, WeaponStation ___currentWeaponStation, List<Unit> ___targetList)
        {
            if (!Plugin.Strict || ___currentWeaponStation == null || ___targetList.Count == 0) return true;

            // Fire Once: with every target already fired on, this press only resets the marks.
            if (Plugin.FireOnce && Engageability.IsFilteredWeapon(___currentWeaponStation)
                && FiredTracker.AllFired(___targetList))
            {
                FiredTracker.Clear();
                return false;
            }

            return Engageability.Filter(___aircraft, ___currentWeaponStation, ___targetList).Count > 0;
        }
    }

    /// <summary>
    /// Every launch, including each shot of a salvo, goes through LaunchMount.
    /// Strict modes: re-check the target at the moment of launch and skip the shot if it no longer qualifies
    /// (the salvo is spread over several seconds, during which the aircraft may have turned away).
    /// Strict Fire Once: remember which target each launch was aimed at.
    /// </summary>
    [HarmonyPatch(typeof(WeaponStation), nameof(WeaponStation.LaunchMount))]
    internal static class LaunchTrackPatch
    {
        static bool Prefix(WeaponStation __instance, Unit owner, Unit target, out int __state)
        {
            __state = __instance.Ammo;
            if (!Plugin.Strict || target == null) return true;

            var hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null || owner != hud.aircraft) return true;
            if (!Engageability.IsFilteredWeapon(__instance)) return true;

            return Engageability.IsEngageable(hud.aircraft, __instance, target);
        }

        static void Postfix(WeaponStation __instance, Unit owner, Unit target, int __state)
        {
            if (!Plugin.FireOnce || target == null) return;
            var hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || owner != hud.aircraft) return;
            // Only count launches that actually used a round (a skipped or failed launch leaves ammo unchanged).
            if (__instance.Ammo >= __state || !Engageability.IsFilteredWeapon(__instance)) return;
            FiredTracker.Mark(target);
        }
    }

    /// <summary>Logs each weapon's type flags and target requirements when the station changes.</summary>
    [HarmonyPatch(typeof(CombatHUD), nameof(CombatHUD.ShowWeaponStation))]
    internal static class WeaponInfoLogPatch
    {
        static void Postfix(WeaponStation weaponStation)
        {
            if (!Plugin.LogWeaponInfo.Value || weaponStation == null) return;
            var i = weaponStation.WeaponInfo;
            var r = i.targetRequirements;
            bool hasMissile = i.weaponPrefab != null && i.weaponPrefab.GetComponent<Missile>() != null;
            Plugin.Log.LogInfo(
                $"Weapon '{i.weaponName}': missile={i.missile} bomb={i.bomb} glideBomb={i.glideBomb} " +
                $"laserGuided={i.laserGuided} boresight={i.boresight} gun={i.gun} fireInterval={i.fireInterval} " +
                $"hasMissileComponent={hasMissile} | req: minRange={r.minRange} maxRange={r.maxRange} " +
                $"minAlignment={r.minAlignment} minOwnerSpeed={r.minOwnerSpeed}");
        }
    }
}

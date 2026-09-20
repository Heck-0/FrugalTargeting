using System.Collections.Generic;
using HarmonyLib;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LaunchAuthorizationPlus
{
    [HarmonyPatch(typeof(HUDMissileState), "DisplayText")]
    internal static class HudIndicatorPatch
    {
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
                ___hint.text = "ALL ENGD - RESET";
                return;
            }

            var ok = Engageability.ThisFrame(___aircraft, ___weaponStation, ___targetList);
            if (ok.Count == 0)
            {
                if (forced)
                {
                    forced = false;
                    ___allRequirementsMet = false;
                    ___noShoot.enabled = true;
                    ___hint.enabled = true;
                    ___hint.text = "NO TGT IN LAR";
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

    [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.Fire))]
    internal static class NoEngageableTargetsPatch
    {
        static bool Prefix(Aircraft ___aircraft, WeaponStation ___currentWeaponStation, List<Unit> ___targetList)
        {
            if (!Plugin.Strict || ___currentWeaponStation == null || ___targetList.Count == 0) return true;

            if (Plugin.FireOnce && Engageability.IsFilteredWeapon(___currentWeaponStation)
                && FiredTracker.AllFired(___targetList))
            {
                FiredTracker.Clear();
                return false;
            }

            return Engageability.Filter(___aircraft, ___currentWeaponStation, ___targetList).Count > 0;
        }
    }

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
            if (__instance.Ammo >= __state || !Engageability.IsFilteredWeapon(__instance)) return;
            FiredTracker.Mark(target);
        }
    }

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

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
            if (!Plugin.Selective || ___hidden || ___targetList.Count == 0)
            {
                forced = false;
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
            ___hint.text = ok.Count < ___targetList.Count ? $"SHOOT {ok.Count}/{___targetList.Count}" : "SHOOT";
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
            if (!Plugin.Enabled.Value || !Plugin.ColorUnengageable.Value) return;
            if (___aircraft == null || ___currentWeaponStation == null || ___targetList.Count == 0) return;

            Engageability.ThisFrame(___aircraft, ___currentWeaponStation, ___targetList);

            var selected = ThemeManager.Active.ColorTheme.HudUnitSelected;
            foreach (var unit in ___targetList)
            {
                if (unit == null || !___markerLookup.TryGetValue(unit, out var marker) || marker.image == null) continue;
                marker.image.color = Engageability.EngageableThisFrame(unit) ? selected : Plugin.UnengageableColor.Value;
            }
        }
    }

    /// <summary>Restrict the salvo to engageable targets (leave vanilla behaviour if none qualify).</summary>
    [HarmonyPatch(typeof(WeaponManager), "SalvoFire")]
    internal static class SalvoFilterPatch
    {
        static void Prefix(ref List<Unit> targets, Aircraft ___aircraft, WeaponStation ___currentWeaponStation)
        {
            if (!Plugin.Selective) return;
            var ok = Engageability.Filter(___aircraft, ___currentWeaponStation, targets);
            if (ok.Count > 0) targets = ok;
        }
    }

    /// <summary>Selective fire: do nothing when targets are selected but none can be engaged.</summary>
    [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.Fire))]
    internal static class NoEngageableTargetsPatch
    {
        static bool Prefix(Aircraft ___aircraft, WeaponStation ___currentWeaponStation, List<Unit> ___targetList)
        {
            if (!Plugin.Selective || ___currentWeaponStation == null || ___targetList.Count == 0) return true;
            return Engageability.Filter(___aircraft, ___currentWeaponStation, ___targetList).Count > 0;
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

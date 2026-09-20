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
        static void Postfix(
            ref bool ___allRequirementsMet, bool ___hidden, float ___lastTextDisplay,
            Image ___noShoot, TextMeshProUGUI ___hint, Aircraft ___aircraft,
            WeaponStation ___weaponStation, List<Unit> ___targetList)
        {
            if (!Plugin.Selective || ___hidden || ___targetList.Count == 0) return;
            // Only refresh right after the game's own (throttled) text update.
            if (___lastTextDisplay != Time.timeSinceLevelLoad) return;

            int ok = Engageability.Filter(___aircraft, ___weaponStation, ___targetList).Count;
            if (ok == 0) return;

            ___allRequirementsMet = true;
            ___noShoot.enabled = false;
            ___hint.enabled = true;
            ___hint.text = ok < ___targetList.Count ? $"SHOOT {ok}/{___targetList.Count}" : "SHOOT";
        }
    }

    /// <summary>Tint selected markers red when the target won't be fired at.</summary>
    [HarmonyPatch(typeof(CombatHUD), "LateUpdate")]
    internal static class MarkerColorPatch
    {
        private static readonly HashSet<Unit> Engageable = new HashSet<Unit>();
        private static float lastCalc;

        static void Postfix(
            Aircraft ___aircraft, WeaponStation ___currentWeaponStation,
            List<Unit> ___targetList, Dictionary<Unit, HUDUnitMarker> ___markerLookup)
        {
            if (!Plugin.Enabled.Value || !Plugin.ColorUnengageable.Value) return;
            if (___aircraft == null || ___currentWeaponStation == null || ___targetList.Count == 0) return;

            if (Time.timeSinceLevelLoad - lastCalc > 0.1f)
            {
                lastCalc = Time.timeSinceLevelLoad;
                Engageable.Clear();
                foreach (var u in Engageability.Filter(___aircraft, ___currentWeaponStation, ___targetList))
                    Engageable.Add(u);
            }

            var selected = ThemeManager.Active.ColorTheme.HudUnitSelected;
            foreach (var unit in ___targetList)
            {
                if (unit == null || !___markerLookup.TryGetValue(unit, out var marker) || marker.image == null) continue;
                marker.image.color = Engageable.Contains(unit) ? selected : Plugin.UnengageableColor.Value;
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
}

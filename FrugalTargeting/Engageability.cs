using System.Collections.Generic;
using UnityEngine;

namespace FrugalTargeting
{
    internal static class Engageability
    {
        /// <summary>
        /// Per-target version of the game's aggregate check (range / min range / arc / speed).
        /// Non-missile weapons are never filtered.
        /// </summary>
        public static bool IsEngageable(Aircraft aircraft, WeaponStation station, Unit target)
        {
            var prefab = station.WeaponInfo.weaponPrefab;
            var missile = prefab != null ? prefab.GetComponent<Missile>() : null;
            if (missile == null) return true;
            if (!aircraft.NetworkHQ.TryGetKnownPosition(target, out var pos)) return false;

            var req = station.WeaponInfo.targetRequirements;
            var self = aircraft.GlobalPosition();
            float dist = FastMath.Distance(pos, self);

            if (dist < req.minRange) return false;
            if (aircraft.speed < req.minOwnerSpeed) return false;
            if (Vector3.Angle(pos - self, aircraft.transform.forward) > req.minAlignment) return false;

            float maxRange = missile.CalcRange(aircraft.speed, self.y, pos.y, dist, target.speed, out _);
            return dist <= maxRange;
        }

        public static List<Unit> Filter(Aircraft aircraft, WeaponStation station, List<Unit> targets)
        {
            var result = new List<Unit>(targets.Count);
            foreach (var t in targets)
                if (t != null && IsEngageable(aircraft, station, t))
                    result.Add(t);
            return result;
        }
    }
}

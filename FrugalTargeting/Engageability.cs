using System.Collections.Generic;
using UnityEngine;

namespace FrugalTargeting
{
    internal static class Engageability
    {
        /// <summary>
        /// Per-target version of the game's aggregate check (range / min range / arc / speed).
        /// Bombs use the release-time window instead. Other weapons are never filtered.
        /// </summary>
        public static bool IsEngageable(Aircraft aircraft, WeaponStation station, Unit target)
        {
            var info = station.WeaponInfo;
            if (info.bomb && !info.glideBomb)
                return !AlreadyFired(target) && BombInReleaseWindow(aircraft, info, target);

            var prefab = station.WeaponInfo.weaponPrefab;
            var missile = prefab != null ? prefab.GetComponent<Missile>() : null;
            if (missile == null) return true;
            if (AlreadyFired(target)) return false;
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

        private static bool AlreadyFired(Unit target) => Plugin.FireOnce && FiredTracker.IsFired(target);

        /// <summary>True for weapons the engageability rules apply to (missiles and bombs).</summary>
        public static bool IsFilteredWeapon(WeaponStation station)
        {
            var info = station.WeaponInfo;
            if (info.bomb && !info.glideBomb) return true;
            var prefab = info.weaponPrefab;
            return prefab != null && prefab.GetComponent<Missile>() != null;
        }

        /// <summary>
        /// Same release-time maths as the game's HUDBombingState ("REL t"): true when the target is
        /// ahead of and below the aircraft and the release moment is within the configured window.
        /// </summary>
        private static bool BombInReleaseWindow(Aircraft aircraft, WeaponInfo info, Unit target)
        {
            if (!aircraft.NetworkHQ.TryGetKnownPosition(target, out var pos)) return false;

            var toTarget = (pos + Vector3.up * info.airburstHeight) - aircraft.GlobalPosition();
            var flat = toTarget;
            flat.y = 0f;
            if (toTarget.y >= 0f || flat.sqrMagnitude < 1f) return false;

            var forward = aircraft.transform.forward;
            forward.y = 0f;
            if (Vector3.Dot(forward, flat) <= 0f) return false;

            var vel = aircraft.rb.velocity;
            float fall = Kinematics.FallTime(-toTarget.y, vel.y);
            if (fall < 0f) return false;

            float closing = Vector3.Dot(vel.normalized, flat.normalized);
            if (Mathf.Abs(closing) < 0.001f) closing = 0.001f;
            float dropTime = flat.magnitude / (closing * vel.magnitude) - fall;

            return Mathf.Abs(dropTime) <= Plugin.BombReleaseWindow.Value;
        }

        private static readonly List<Unit> FrameList = new List<Unit>();
        private static readonly HashSet<Unit> FrameSet = new HashSet<Unit>();
        private static int frameStamp = -1;

        /// <summary>
        /// Engageable targets for the current frame, computed once and shared by the HUD patches.
        /// The returned list is reused; don't keep it.
        /// </summary>
        public static List<Unit> ThisFrame(Aircraft aircraft, WeaponStation station, List<Unit> targets)
        {
            if (frameStamp != Time.frameCount)
            {
                frameStamp = Time.frameCount;
                FrameList.Clear();
                FrameSet.Clear();
                foreach (var t in targets)
                {
                    if (t == null || !IsEngageable(aircraft, station, t)) continue;
                    FrameList.Add(t);
                    FrameSet.Add(t);
                }
            }
            return FrameList;
        }

        public static bool EngageableThisFrame(Unit unit) => FrameSet.Contains(unit);

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

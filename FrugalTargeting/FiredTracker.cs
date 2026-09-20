using System.Collections.Generic;

namespace FrugalTargeting
{
    /// <summary>Targets already launched at during Fire Once mode.</summary>
    internal static class FiredTracker
    {
        private static readonly HashSet<Unit> Fired = new HashSet<Unit>();

        public static bool IsFired(Unit unit) => Fired.Contains(unit);

        public static void Mark(Unit unit) => Fired.Add(unit);

        public static void Clear() => Fired.Clear();

        /// <summary>Forget targets that are no longer selected (deselecting and re-locking resets a target).</summary>
        public static void Prune(List<Unit> selected)
        {
            if (Fired.Count == 0) return;
            Fired.RemoveWhere(u => u == null || !selected.Contains(u));
        }

        public static int CountIn(List<Unit> selected)
        {
            int n = 0;
            foreach (var u in selected)
                if (u != null && Fired.Contains(u)) n++;
            return n;
        }

        /// <summary>True when there are selected targets and every one has been fired on.</summary>
        public static bool AllFired(List<Unit> selected) => selected.Count > 0 && CountIn(selected) == selected.Count;
    }
}

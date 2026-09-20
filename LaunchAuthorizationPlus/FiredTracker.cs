using System.Collections.Generic;

namespace LaunchAuthorizationPlus
{
    internal static class FiredTracker
    {
        private static readonly HashSet<Unit> Fired = new HashSet<Unit>();

        public static bool IsFired(Unit unit) => Fired.Contains(unit);

        public static void Mark(Unit unit) => Fired.Add(unit);

        public static void Clear() => Fired.Clear();

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

        public static bool AllFired(List<Unit> selected) => selected.Count > 0 && CountIn(selected) == selected.Count;
    }
}

using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal static class ShuttleTickUtility
    {
        internal static int TicksGameOrZero()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        internal static int TicksGameOrMinusOne()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
        }
    }
}

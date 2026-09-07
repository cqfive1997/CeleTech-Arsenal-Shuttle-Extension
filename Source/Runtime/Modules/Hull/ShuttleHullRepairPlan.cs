using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullRepairPlan
    {
        internal float RepairHitPoints;
        internal int WorkTicks;
        internal List<ThingDefCountClass> CostList;
        internal string CostSummary;
    }
}

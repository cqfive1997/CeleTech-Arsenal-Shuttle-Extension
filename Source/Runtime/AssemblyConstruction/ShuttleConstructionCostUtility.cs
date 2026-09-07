using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleConstructionCostUtility
    {
        private const int FallbackSegmentWorkTicks = 9000;
        private const int FallbackModuleWorkTicks = 3000;

        private static readonly HashSet<string> warnedFallbackCosts = new HashSet<string>();
        private static readonly HashSet<string> warnedFallbackWork = new HashSet<string>();

        internal static List<ThingDefCountClass> GetSegmentConstructionCost(ShuttleSegmentBaseDef def)
        {
            if (def != null && HasCost(def.constructionCostList))
            {
                return def.constructionCostList;
            }

            WarnFallbackCost(def);
            return BuildCost(new CostEntry("Steel", 300), new CostEntry("ComponentIndustrial", 4));
        }

        internal static List<ThingDefCountClass> GetModuleConstructionCost(ShuttleModuleBaseDef def)
        {
            if (def != null && HasCost(def.constructionCostList))
            {
                return def.constructionCostList;
            }

            WarnFallbackCost(def);
            return BuildCost(new CostEntry("Steel", 60), new CostEntry("ComponentIndustrial", 1));
        }

        internal static List<ThingDefCountClass> GetModuleConstructionCost(
            ShuttleModuleBaseDef def,
            ThingDef selectedStuffDef)
        {
            List<ThingDefCountClass> costs = CloneValidCostList(GetModuleConstructionCost(def));
            ShuttleHullPlatingModuleDef hullDef = def as ShuttleHullPlatingModuleDef;
            if (hullDef == null ||
                hullDef.stuffCostCount <= 0 ||
                !ShuttleHullArmorStuffUtility.IsValidHullArmorStuff(selectedStuffDef))
            {
                return costs;
            }

            AddOrMergeCost(costs, selectedStuffDef, hullDef.stuffCostCount);
            return costs;
        }

        internal static int GetSegmentConstructionWorkTicks(ShuttleSegmentBaseDef def)
        {
            return ShuttleConstructionWorkTuning.ApplyToAuthoredWork(
                GetSegmentBaseConstructionWorkTicks(def));
        }

        internal static int GetSegmentBaseConstructionWorkTicks(ShuttleSegmentBaseDef def)
        {
            if (def != null && def.constructionWorkTicks > 0)
            {
                return def.constructionWorkTicks;
            }

            WarnFallbackWork(def, FallbackSegmentWorkTicks);
            return FallbackSegmentWorkTicks;
        }

        internal static int GetModuleConstructionWorkTicks(ShuttleModuleBaseDef def)
        {
            return ShuttleConstructionWorkTuning.ApplyToAuthoredWork(
                GetModuleBaseConstructionWorkTicks(def));
        }

        internal static int GetModuleBaseConstructionWorkTicks(ShuttleModuleBaseDef def)
        {
            if (def != null && def.constructionWorkTicks > 0)
            {
                return def.constructionWorkTicks;
            }

            WarnFallbackWork(def, FallbackModuleWorkTicks);
            return FallbackModuleWorkTicks;
        }

        internal static string BuildCostSummary(IReadOnlyList<ThingDefCountClass> costList)
        {
            if (costList == null || costList.Count == 0)
            {
                return "CT_Shuttle_AssemblyConstruction_NoMaterials".Translate().ToString();
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(cost.thingDef.LabelCap);
                builder.Append(" x");
                builder.Append(cost.count);
            }

            return builder.Length > 0
                ? builder.ToString()
                : "CT_Shuttle_AssemblyConstruction_NoMaterials".Translate().ToString();
        }

        internal static List<ThingDefCountClass> CloneValidCostList(IReadOnlyList<ThingDefCountClass> costList)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            if (costList == null)
            {
                return result;
            }

            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                result.Add(new ThingDefCountClass(cost.thingDef, cost.count));
            }

            return result;
        }

        private static bool HasCost(List<ThingDefCountClass> costList)
        {
            if (costList == null)
            {
                return false;
            }

            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (cost != null && cost.thingDef != null && cost.count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<ThingDefCountClass> BuildCost(params CostEntry[] entries)
        {
            List<ThingDefCountClass> costs = new List<ThingDefCountClass>();
            for (int i = 0; i < entries.Length; i++)
            {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(entries[i].ThingDefName);
                if (thingDef == null || entries[i].Count <= 0)
                {
                    continue;
                }

                costs.Add(new ThingDefCountClass(thingDef, entries[i].Count));
            }

            if (costs.Count == 0)
            {
                costs.Add(new ThingDefCountClass(ThingDefOf.Steel, 100));
            }

            return costs;
        }

        private static void AddOrMergeCost(
            List<ThingDefCountClass> costs,
            ThingDef thingDef,
            int count)
        {
            if (costs == null || thingDef == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass existing = costs[i];
                if (existing != null && existing.thingDef == thingDef)
                {
                    existing.count = int.MaxValue - existing.count < count
                        ? int.MaxValue
                        : existing.count + count;
                    return;
                }
            }

            costs.Add(new ThingDefCountClass(thingDef, count));
        }

        private static void WarnFallbackCost(Def def)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            string defName = def != null ? def.defName : "<null>";
            if (warnedFallbackCosts.Add(defName))
            {
                Log.Warning("[CeleTech Shuttle] " + defName +
                    " has no constructionCostList; using fallback construction cost.");
            }
        }

        private static void WarnFallbackWork(Def def, int fallbackTicks)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            string defName = def != null ? def.defName : "<null>";
            if (warnedFallbackWork.Add(defName))
            {
                Log.Warning("[CeleTech Shuttle] " + defName +
                    " has constructionWorkTicks <= 0; using fallback construction work ticks: " + fallbackTicks + ".");
            }
        }

        private struct CostEntry
        {
            internal readonly string ThingDefName;
            internal readonly int Count;

            internal CostEntry(string thingDefName, int count)
            {
                this.ThingDefName = thingDefName;
                this.Count = count;
            }
        }
    }
}

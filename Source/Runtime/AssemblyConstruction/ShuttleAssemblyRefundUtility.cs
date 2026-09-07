using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyRefundUtility
    {
        internal static List<ThingDefCountClass> BuildModuleRefundList(ShuttleModule module)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            if (module == null || module.ModuleDef == null)
            {
                return result;
            }

            List<ThingDefCountClass> costList;
            ShuttleHullPlatingModuleDef hullDef = module.ModuleDef as ShuttleHullPlatingModuleDef;
            if (hullDef != null)
            {
                ThingDef selectedStuff = !string.IsNullOrEmpty(module.SelectedStuffDefName)
                    ? DefDatabase<ThingDef>.GetNamedSilentFail(module.SelectedStuffDefName)
                    : null;
                costList = ShuttleConstructionCostUtility.GetModuleConstructionCost(
                    module.ModuleDef,
                    selectedStuff);
            }
            else
            {
                costList = ShuttleConstructionCostUtility.GetModuleConstructionCost(module.ModuleDef);
            }

            AddFullRefundCosts(result, costList);
            return result;
        }

        internal static List<ThingDefCountClass> BuildSegmentRefundList(ShuttleSegment segment)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            if (segment == null || segment.SegmentDef == null)
            {
                return result;
            }

            AddFullRefundCosts(
                result,
                ShuttleConstructionCostUtility.GetSegmentConstructionCost(segment.SegmentDef));
            return result;
        }

        internal static bool TryCreateRefundThings(
            ThingOwner<Thing> destination,
            IReadOnlyList<ThingDefCountClass> refunds,
            string failureReasonKey,
            out string failureReason)
        {
            failureReason = null;
            if (destination == null)
            {
                failureReason = TranslateFailure(failureReasonKey);
                return false;
            }

            if (refunds == null || refunds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < refunds.Count; i++)
            {
                ThingDefCountClass refund = refunds[i];
                if (refund == null || refund.thingDef == null || refund.count <= 0)
                {
                    continue;
                }

                int remaining = refund.count;
                while (remaining > 0)
                {
                    int count = remaining;
                    if (refund.thingDef.stackLimit > 0)
                    {
                        count = System.Math.Min(remaining, refund.thingDef.stackLimit);
                    }

                    Thing thing = ThingMaker.MakeThing(refund.thingDef);
                    if (thing == null)
                    {
                        failureReason = TranslateFailure(failureReasonKey);
                        return false;
                    }

                    thing.stackCount = count;
                    if (!destination.TryAddOrTransfer(thing, false))
                    {
                        if (!thing.Destroyed)
                        {
                            thing.Destroy(DestroyMode.Vanish);
                        }

                        failureReason = TranslateFailure(failureReasonKey);
                        return false;
                    }

                    remaining -= count;
                }
            }

            return true;
        }

        private static void AddFullRefundCosts(
            List<ThingDefCountClass> result,
            IReadOnlyList<ThingDefCountClass> costList)
        {
            if (result == null || costList == null)
            {
                return;
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
        }

        private static string TranslateFailure(string failureReasonKey)
        {
            return string.IsNullOrEmpty(failureReasonKey)
                ? "CT_Shuttle_Command_ModuleRemovalRefundFailed".Translate().ToString()
                : failureReasonKey.Translate().ToString();
        }
    }
}

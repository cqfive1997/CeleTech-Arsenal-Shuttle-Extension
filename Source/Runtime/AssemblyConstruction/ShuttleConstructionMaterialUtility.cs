using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleConstructionMaterialUtility
    {
        internal static int CountDelivered(ThingOwner<Thing> stagedIngredients, ThingDef thingDef)
        {
            if (stagedIngredients == null || thingDef == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < stagedIngredients.Count; i++)
            {
                Thing thing = stagedIngredients[i];
                if (thing != null && !thing.Destroyed && thing.def == thingDef)
                {
                    count += Mathf.Max(0, thing.stackCount);
                }
            }

            return count;
        }

        internal static int CountRequired(IReadOnlyList<ThingDefCountClass> requiredCosts, ThingDef thingDef)
        {
            if (requiredCosts == null || thingDef == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < requiredCosts.Count; i++)
            {
                ThingDefCountClass cost = requiredCosts[i];
                if (cost != null && cost.thingDef == thingDef && cost.count > 0)
                {
                    count += cost.count;
                }
            }

            return count;
        }

        internal static int CountMissing(
            ShuttleAssemblyConstructionOrder order,
            ThingOwner<Thing> stagedIngredients,
            ThingDef thingDef)
        {
            if (order == null || thingDef == null)
            {
                return 0;
            }

            int required = CountRequired(order.RequiredCostList, thingDef);
            int delivered = CountDelivered(stagedIngredients, thingDef);
            return Mathf.Max(0, required - delivered);
        }

        internal static bool HasAllRequiredMaterials(
            ShuttleAssemblyConstructionOrder order,
            ThingOwner<Thing> stagedIngredients)
        {
            if (order == null || order.RequiredCostList == null || order.RequiredCostList.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < order.RequiredCostList.Count; i++)
            {
                ThingDefCountClass cost = order.RequiredCostList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                if (CountMissing(order, stagedIngredients, cost.thingDef) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        internal static float GetMaterialProgress01(
            ShuttleAssemblyConstructionOrder order,
            ThingOwner<Thing> stagedIngredients)
        {
            if (order == null || order.RequiredCostList == null || order.RequiredCostList.Count == 0)
            {
                return 1f;
            }

            int requiredTotal = 0;
            int deliveredTotal = 0;
            for (int i = 0; i < order.RequiredCostList.Count; i++)
            {
                ThingDefCountClass cost = order.RequiredCostList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                requiredTotal += cost.count;
                deliveredTotal += Mathf.Min(cost.count, CountDelivered(stagedIngredients, cost.thingDef));
            }

            return requiredTotal > 0 ? Mathf.Clamp01(deliveredTotal / (float)requiredTotal) : 1f;
        }

        internal static string BuildMaterialProgressSummary(
            ShuttleAssemblyConstructionOrder order,
            ThingOwner<Thing> stagedIngredients)
        {
            if (order == null || order.RequiredCostList == null || order.RequiredCostList.Count == 0)
            {
                return "CT_Shuttle_AssemblyConstruction_MaterialSummaryEmpty".Translate().ToString();
            }

            StringBuilder builder = new StringBuilder();
            string separator =
                "CT_Shuttle_AssemblyConstruction_MaterialSummarySeparator".Translate().ToString();
            bool appended = false;
            for (int i = 0; i < order.RequiredCostList.Count; i++)
            {
                ThingDefCountClass cost = order.RequiredCostList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                if (appended)
                {
                    builder.Append(separator);
                }

                appended = true;
                int delivered = Mathf.Min(cost.count, CountDelivered(stagedIngredients, cost.thingDef));
                builder.Append(cost.thingDef.LabelCap);
                builder.Append(" ");
                builder.Append(delivered);
                builder.Append("/");
                builder.Append(cost.count);
            }

            return appended
                ? "CT_Shuttle_AssemblyConstruction_MaterialSummary"
                    .Translate(builder.ToString())
                    .ToString()
                : "CT_Shuttle_AssemblyConstruction_MaterialSummaryEmpty".Translate().ToString();
        }

        internal static void RefreshMaterialState(
            ShuttleAssemblyConstructionOrder order,
            ThingOwner<Thing> stagedIngredients)
        {
            if (order == null)
            {
                return;
            }

            if (order.Status == ShuttleAssemblyConstructionStatus.Completing ||
                order.Status == ShuttleAssemblyConstructionStatus.Failed)
            {
                return;
            }

            if (HasAllRequiredMaterials(order, stagedIngredients))
            {
                order.MarkWorking();
            }
            else
            {
                order.MarkAwaitingMaterials();
            }
        }
    }
}

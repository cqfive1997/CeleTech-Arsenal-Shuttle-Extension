using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal sealed class ShuttleConstructionResourceBroker
    {
        internal bool CanAfford(Map map, List<ThingDefCountClass> costList, out string failureReason)
        {
            failureReason = null;
            if (map == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_BuildMapUnavailable".Translate().ToString();
                return false;
            }

            if (costList == null || costList.Count == 0)
            {
                return true;
            }

            List<string> missing = new List<string>();
            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (!this.IsValidCost(cost))
                {
                    continue;
                }

                int available = this.CountAvailable(map, cost.thingDef);
                if (available < cost.count)
                {
                    missing.Add(this.FormatCost(cost.thingDef, cost.count - available));
                }
            }

            if (missing.Count == 0)
            {
                return true;
            }

            failureReason = "CT_Shuttle_AssemblyConstruction_MaterialsInsufficient"
                .Translate(string.Join(", ", missing.ToArray()))
                .ToString();
            return false;
        }

        internal bool TryTakeToOwner(
            Map map,
            List<ThingDefCountClass> costList,
            ThingOwner<Thing> destination,
            out string failureReason)
        {
            failureReason = null;
            if (destination == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_StagedIngredientOwnerUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if (!this.CanAfford(map, costList, out failureReason))
            {
                return false;
            }

            if (costList == null || costList.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (!this.IsValidCost(cost))
                {
                    continue;
                }

                int remaining = cost.count;
                List<Thing> candidates = this.GetCandidates(map, cost.thingDef);
                for (int thingIndex = 0; thingIndex < candidates.Count && remaining > 0; thingIndex++)
                {
                    Thing thing = candidates[thingIndex];
                    if (thing == null || thing.Destroyed || !thing.Spawned)
                    {
                        continue;
                    }

                    int takeCount = Mathf.Min(remaining, thing.stackCount);
                    Thing taken = thing.SplitOff(takeCount);
                    if (taken.Spawned)
                    {
                        taken.DeSpawn(DestroyMode.Vanish);
                    }

                    if (!destination.TryAddOrTransfer(taken, false))
                    {
                        GenPlace.TryPlaceThing(taken, this.GetDropCell(map), map, ThingPlaceMode.Near);
                        failureReason = "CT_Shuttle_AssemblyConstruction_StageMaterialFailed"
                            .Translate()
                            .ToString();
                        return false;
                    }

                    remaining -= takeCount;
                }

                if (remaining > 0)
                {
                    failureReason = "CT_Shuttle_AssemblyConstruction_MaterialTakeIncomplete"
                        .Translate(this.FormatCost(cost.thingDef, remaining))
                        .ToString();
                    return false;
                }
            }

            return true;
        }

        internal bool TryReturnToMap(
            ThingWithComps host,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            failureReason = null;
            if (source == null || source.Count == 0)
            {
                return true;
            }

            Map map = host != null ? host.Map : null;
            if (map == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_ReturnMapUnavailable".Translate().ToString();
                return false;
            }

            IntVec3 cell = host != null && host.Spawned ? host.Position : this.GetDropCell(map);
            bool result = source.TryDropAll(cell, map, ThingPlaceMode.Near);
            if (!result)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_ReturnMaterialsPartialFailure"
                    .Translate()
                    .ToString();
            }

            return result;
        }

        internal void TryDestroyStaged(ThingOwner<Thing> stagedIngredients)
        {
            if (stagedIngredients == null)
            {
                return;
            }

            while (stagedIngredients.Count > 0)
            {
                Thing thing = stagedIngredients[0];
                stagedIngredients.Remove(thing);
                if (thing != null && !thing.Destroyed)
                {
                    thing.Destroy();
                }
            }
        }

        internal string BuildCostSummary(List<ThingDefCountClass> costList)
        {
            if (costList == null || costList.Count == 0)
            {
                return "CT_Shuttle_AssemblyConstruction_NoMaterials".Translate().ToString();
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (!this.IsValidCost(cost))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(this.FormatCost(cost.thingDef, cost.count));
            }

            return builder.Length > 0
                ? builder.ToString()
                : "CT_Shuttle_AssemblyConstruction_NoMaterials".Translate().ToString();
        }

        private List<Thing> GetCandidates(Map map, ThingDef thingDef)
        {
            List<Thing> result = new List<Thing>();
            if (map == null || thingDef == null)
            {
                return result;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(thingDef);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (this.IsUsableMapResource(thing))
                {
                    result.Add(thing);
                }
            }

            return result;
        }

        private int CountAvailable(Map map, ThingDef thingDef)
        {
            int count = 0;
            List<Thing> candidates = this.GetCandidates(map, thingDef);
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing thing = candidates[i];
                if (thing != null)
                {
                    count += thing.stackCount;
                }
            }

            return count;
        }

        private bool IsUsableMapResource(Thing thing)
        {
            return thing != null &&
                !thing.Destroyed &&
                thing.Spawned &&
                thing.def != null &&
                thing.def.category == ThingCategory.Item &&
                !thing.IsForbidden(Faction.OfPlayer);
        }

        private bool IsValidCost(ThingDefCountClass cost)
        {
            return cost != null && cost.thingDef != null && cost.count > 0;
        }

        private string FormatCost(ThingDef thingDef, int count)
        {
            string label = thingDef != null ? thingDef.LabelCap.ToString() : "-";
            return label + " x" + count;
        }

        private IntVec3 GetDropCell(Map map)
        {
            return map != null ? map.Center : IntVec3.Invalid;
        }
    }
}

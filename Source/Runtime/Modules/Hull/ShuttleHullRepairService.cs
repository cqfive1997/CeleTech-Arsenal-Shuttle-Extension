using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullRepairService
    {
        internal const float DefaultRepairBatchHitPoints = 50f;
        internal const int DefaultRepairWorkTicks = 600;
        internal const int DefaultRepairStuffCountPerBatch = 20;
        internal const int DefaultRepairComponentCountPerBatch = 1;

        public bool NeedsRepair(ShuttleProfile profile, ShuttleRuntimeState runtimeState)
        {
            return profile != null &&
                profile.Hull != null &&
                profile.Hull.MaxHitPoints > 0 &&
                runtimeState != null &&
                runtimeState.Hull != null &&
                runtimeState.Hull.CurrentHitPoints < profile.Hull.MaxHitPoints;
        }

        public float GetMissingHitPoints(ShuttleProfile profile, ShuttleRuntimeState runtimeState)
        {
            if (profile == null ||
                profile.Hull == null ||
                profile.Hull.MaxHitPoints <= 0 ||
                runtimeState == null ||
                runtimeState.Hull == null)
            {
                return 0f;
            }

            return Math.Max(0f, profile.Hull.MaxHitPoints - runtimeState.Hull.CurrentHitPoints);
        }

        public float GetRepairBatchHitPoints(ShuttleProfile profile, ShuttleRuntimeState runtimeState)
        {
            float missingHitPoints = this.GetMissingHitPoints(profile, runtimeState);
            if (missingHitPoints <= 0f)
            {
                return 0f;
            }

            return Math.Min(DefaultRepairBatchHitPoints, missingHitPoints);
        }

        public float GetFullRepairHitPoints(ShuttleProfile profile, ShuttleRuntimeState runtimeState)
        {
            return this.GetMissingHitPoints(profile, runtimeState);
        }

        public int GetRepairWorkTicks(float repairHitPoints)
        {
            if (!this.IsFinitePositive(repairHitPoints))
            {
                return 0;
            }

            int batches = this.CeilToPositiveInt(repairHitPoints / DefaultRepairBatchHitPoints);
            int ticks = this.SaturatingMultiply(batches, DefaultRepairWorkTicks);
            return ticks > 0 ? ticks : DefaultRepairWorkTicks;
        }

        public bool TryBuildRepairPlan(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleAssemblyState assemblyState,
            out ShuttleHullRepairPlan plan)
        {
            plan = null;
            float repairHitPoints = this.GetFullRepairHitPoints(profile, runtimeState);
            if (!this.IsFinitePositive(repairHitPoints))
            {
                return false;
            }

            int workTicks = this.GetRepairWorkTicks(repairHitPoints);
            if (workTicks <= 0)
            {
                return false;
            }

            List<ThingDefCountClass> costList = this.BuildRepairCost(assemblyState, repairHitPoints);
            plan = new ShuttleHullRepairPlan
            {
                RepairHitPoints = repairHitPoints,
                WorkTicks = workTicks,
                CostList = costList,
                CostSummary = this.BuildCostSummary(costList)
            };
            return true;
        }

        public ThingDef ResolvePrimaryRepairStuff(ShuttleAssemblyState assemblyState)
        {
            ThingDef bestStuff = null;
            int bestCount = 0;
            Dictionary<ThingDef, int> stuffCounts = new Dictionary<ThingDef, int>();

            IReadOnlyList<ShuttleModule> modules = assemblyState != null ? assemblyState.Modules : null;
            if (modules != null)
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    ShuttleModule module = modules[i];
                    if (module == null || !(module.ModuleDef is ShuttleHullPlatingModuleDef))
                    {
                        continue;
                    }

                    ThingDef stuffDef =
                        ShuttleHullArmorStuffUtility.ResolveSelectedStuffOrFallback(module.SelectedStuffDefName);
                    if (!ShuttleHullArmorStuffUtility.IsValidHullArmorStuff(stuffDef))
                    {
                        continue;
                    }

                    int count;
                    stuffCounts.TryGetValue(stuffDef, out count);
                    count++;
                    stuffCounts[stuffDef] = count;
                    if (count > bestCount)
                    {
                        bestStuff = stuffDef;
                        bestCount = count;
                    }
                }
            }

            return bestStuff ?? ShuttleHullArmorStuffUtility.ResolveSelectedStuffOrFallback(null);
        }

        public List<ThingDefCountClass> BuildRepairCost(
            ShuttleAssemblyState assemblyState,
            float repairHitPoints)
        {
            List<ThingDefCountClass> costs = new List<ThingDefCountClass>();
            if (!this.IsFinitePositive(repairHitPoints))
            {
                return costs;
            }

            int batches = this.CeilToPositiveInt(repairHitPoints / DefaultRepairBatchHitPoints);
            if (batches <= 0)
            {
                return costs;
            }

            ThingDef primaryStuff = this.ResolvePrimaryRepairStuff(assemblyState);
            if (primaryStuff != null)
            {
                this.AddOrMergeCost(
                    costs,
                    primaryStuff,
                    this.SaturatingMultiply(DefaultRepairStuffCountPerBatch, batches));
            }

            ThingDef componentDef = DefDatabase<ThingDef>.GetNamedSilentFail("ComponentIndustrial");
            if (componentDef != null)
            {
                this.AddOrMergeCost(
                    costs,
                    componentDef,
                    this.SaturatingMultiply(DefaultRepairComponentCountPerBatch, batches));
            }

            return costs;
        }

        public bool HasRepairMaterialsOnMap(
            Pawn pawn,
            ThingWithComps shuttle,
            IReadOnlyList<ThingDefCountClass> costs,
            out string reason)
        {
            List<Thing> materialStacks;
            return this.TryFindRepairMaterialStacks(pawn, shuttle, costs, out materialStacks, out reason);
        }

        public bool TryFindRepairMaterialStacks(
            Pawn pawn,
            ThingWithComps shuttle,
            IReadOnlyList<ThingDefCountClass> costs,
            out List<Thing> materialStacks,
            out string reason)
        {
            materialStacks = new List<Thing>();
            reason = null;
            if (costs == null || costs.Count == 0)
            {
                return true;
            }

            Map map = pawn != null ? pawn.Map : null;
            if (pawn == null ||
                shuttle == null ||
                map == null ||
                shuttle.Map != map)
            {
                reason = "CT_Shuttle_HullRepair_Unreachable".Translate().ToString();
                return false;
            }

            List<ThingDefCountClass> requirements = this.BuildMergedCostRequirements(costs);
            List<ThingDefCountClass> missing = new List<ThingDefCountClass>();
            for (int i = 0; i < requirements.Count; i++)
            {
                ThingDefCountClass cost = requirements[i];
                int available = 0;
                List<Thing> candidates = this.GetReachableMapResources(
                    pawn,
                    map,
                    cost.thingDef,
                    null);
                for (int thingIndex = 0; thingIndex < candidates.Count && available < cost.count; thingIndex++)
                {
                    Thing candidate = candidates[thingIndex];
                    if (candidate == null)
                    {
                        continue;
                    }

                    available = this.SaturatingAdd(available, candidate.stackCount);
                    if (!materialStacks.Contains(candidate))
                    {
                        materialStacks.Add(candidate);
                    }
                }

                if (available < cost.count)
                {
                    missing.Add(new ThingDefCountClass(cost.thingDef, cost.count - available));
                }
            }

            if (missing.Count == 0)
            {
                return true;
            }

            reason = "CT_Shuttle_HullRepair_MissingMaterials".Translate(
                this.BuildCostSummary(missing)).ToString();
            return false;
        }

        public bool TryFindNextRepairMaterialForHaul(
            Pawn pawn,
            ThingWithComps shuttle,
            IReadOnlyList<ThingDefCountClass> costs,
            ShuttleHullRepairMaterialLedger stagedMaterials,
            out Thing material,
            out int count,
            out bool materialsLoaded,
            out string reason)
        {
            material = null;
            count = 0;
            materialsLoaded = false;
            reason = null;

            Map map = pawn != null ? pawn.Map : null;
            if (pawn == null || shuttle == null || map == null || shuttle.Map != map)
            {
                reason = "CT_Shuttle_HullRepair_Unreachable".Translate().ToString();
                return false;
            }

            if (stagedMaterials == null)
            {
                reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                return false;
            }

            List<ThingDefCountClass> missing = this.BuildMissingTrackedCostRequirements(
                map,
                costs,
                stagedMaterials);
            if (missing.Count == 0)
            {
                materialsLoaded = true;
                return true;
            }

            for (int i = 0; i < missing.Count; i++)
            {
                ThingDefCountClass cost = missing[i];
                Thing candidate = this.FindClosestReachableMapResourceForHaul(
                    pawn,
                    map,
                    cost.thingDef,
                    stagedMaterials);
                if (candidate == null)
                {
                    continue;
                }

                int availableStackSpace = pawn.carryTracker != null
                    ? pawn.carryTracker.AvailableStackSpace(candidate.def)
                    : 0;
                int takeCount = Math.Min(cost.count, Math.Min(candidate.stackCount, availableStackSpace));
                if (takeCount <= 0)
                {
                    continue;
                }

                material = candidate;
                count = takeCount;
                return true;
            }

            reason = "CT_Shuttle_HullRepair_MissingMaterials".Translate(
                this.BuildCostSummary(missing)).ToString();
            return false;
        }

        public bool TryStageCarriedRepairMaterial(
            Pawn pawn,
            ThingWithComps shuttle,
            IReadOnlyList<ThingDefCountClass> costs,
            ShuttleHullRepairMaterialLedger stagedMaterials,
            out string reason)
        {
            reason = null;
            if (pawn == null ||
                pawn.carryTracker == null ||
                shuttle == null ||
                shuttle.Map == null ||
                stagedMaterials == null)
            {
                reason = "CT_Shuttle_HullRepair_Unreachable".Translate().ToString();
                return false;
            }

            Thing carried = pawn.carryTracker.CarriedThing;
            if (carried == null || carried.Destroyed || carried.stackCount <= 0)
            {
                reason = "CT_Shuttle_HullRepair_MissingMaterials".Translate(
                    this.BuildCostSummary(null)).ToString();
                return false;
            }

            int requiredCount = this.GetRequiredCount(costs, carried.def);
            int stagedCount = stagedMaterials.CountAvailable(carried.def, shuttle.Map);
            int transferCount = Math.Min(carried.stackCount, Math.Max(0, requiredCount - stagedCount));
            if (transferCount <= 0)
            {
                reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                return false;
            }

            List<Thing> newlyTrackedThings = new List<Thing>();
            int recordedCount = 0;
            Action<Thing, int> placedAction = delegate(Thing placedThing, int placedCount)
            {
                int acceptedCount = Math.Min(
                    Math.Max(0, placedCount),
                    Math.Max(0, transferCount - recordedCount));
                if (placedThing == null || acceptedCount <= 0)
                {
                    return;
                }

                bool alreadyTracked = stagedMaterials.Contains(placedThing);
                stagedMaterials.RecordDelivery(placedThing, acceptedCount);
                recordedCount = this.SaturatingAdd(recordedCount, acceptedCount);
                if (!alreadyTracked && !newlyTrackedThings.Contains(placedThing))
                {
                    newlyTrackedThings.Add(placedThing);
                }
            };

            Thing dropped;
            bool dropSucceeded = pawn.carryTracker.TryDropCarriedThing(
                this.GetRepairMaterialStagingCell(shuttle),
                transferCount,
                ThingPlaceMode.Near,
                out dropped,
                placedAction);
            if (recordedCount <= 0 && dropSucceeded && dropped != null && !dropped.Destroyed)
            {
                int fallbackCount = Math.Min(transferCount, dropped.stackCount);
                if (fallbackCount > 0)
                {
                    bool alreadyTracked = stagedMaterials.Contains(dropped);
                    stagedMaterials.RecordDelivery(dropped, fallbackCount);
                    recordedCount = fallbackCount;
                    if (!alreadyTracked)
                    {
                        newlyTrackedThings.Add(dropped);
                    }
                }
            }

            if (!dropSucceeded || recordedCount < transferCount)
            {
                reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                return false;
            }

            Job activeJob = pawn.CurJob;
            for (int i = 0; i < newlyTrackedThings.Count; i++)
            {
                Thing trackedThing = newlyTrackedThings[i];
                if (trackedThing == null ||
                    trackedThing.Destroyed ||
                    !trackedThing.Spawned ||
                    activeJob == null)
                {
                    continue;
                }

                if (!pawn.Reserve(trackedThing, activeJob, 1, -1, null, false, false))
                {
                    reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                    return false;
                }
            }

            return true;
        }

        public bool TryApplyRepairWithTrackedMaterials(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ThingWithComps shuttle,
            float requestedHitPoints,
            IReadOnlyList<ThingDefCountClass> costs,
            ShuttleHullRepairMaterialLedger stagedMaterials,
            out float actualRepair,
            out string reason)
        {
            actualRepair = 0f;
            reason = null;
            Map map = shuttle != null ? shuttle.Map : null;
            if (map == null || stagedMaterials == null)
            {
                reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                return false;
            }

            List<ThingDefCountClass> requirements = this.BuildMergedCostRequirements(costs);
            if (!stagedMaterials.CanSatisfy(requirements, map))
            {
                reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                return false;
            }

            actualRepair = Math.Min(
                requestedHitPoints,
                this.GetMissingHitPoints(profile, runtimeState));
            if (!this.IsFinitePositive(actualRepair))
            {
                reason = "CT_Shuttle_HullRepair_Failed".Translate().ToString();
                return false;
            }

            if (!stagedMaterials.TryConsume(requirements, map))
            {
                actualRepair = 0f;
                reason = "CT_Shuttle_HullRepair_NoStagedMaterials".Translate().ToString();
                return false;
            }

            runtimeState.Hull.Repair(actualRepair);
            return true;
        }

        public float ApplyRepair(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            float requestedHitPoints)
        {
            if (!this.IsFinitePositive(requestedHitPoints) ||
                profile == null ||
                profile.Hull == null ||
                profile.Hull.MaxHitPoints <= 0 ||
                runtimeState == null ||
                runtimeState.Hull == null)
            {
                return 0f;
            }

            float missingHitPoints = this.GetMissingHitPoints(profile, runtimeState);
            float actualRepair = Math.Min(requestedHitPoints, missingHitPoints);
            if (actualRepair <= 0f)
            {
                return 0f;
            }

            runtimeState.Hull.Repair(actualRepair);
            return actualRepair;
        }

        public string BuildCostSummary(IReadOnlyList<ThingDefCountClass> costList)
        {
            List<ThingDefCountClass> requirements = this.BuildMergedCostRequirements(costList);
            if (requirements.Count == 0)
            {
                return "CT_Shuttle_HullRepair_CostSummaryEmpty".Translate().ToString();
            }

            List<string> entries = new List<string>();
            for (int i = 0; i < requirements.Count; i++)
            {
                ThingDefCountClass cost = requirements[i];
                string label = cost.thingDef != null ? cost.thingDef.LabelCap.ToString() : "-";
                entries.Add("CT_Shuttle_HullRepair_CostSummaryItem".Translate(
                    label,
                    cost.count.ToString()).ToString());
            }

            return string.Join(", ", entries.ToArray());
        }

        private List<ThingDefCountClass> BuildMergedCostRequirements(IReadOnlyList<ThingDefCountClass> costs)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            if (costs == null)
            {
                return result;
            }

            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass cost = costs[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                this.AddOrMergeCost(result, cost.thingDef, cost.count);
            }

            return result;
        }

        private List<Thing> GetReachableMapResources(
            Pawn pawn,
            Map map,
            ThingDef thingDef,
            ShuttleHullRepairMaterialLedger stagedMaterials)
        {
            List<Thing> result = new List<Thing>();
            if (pawn == null || map == null || thingDef == null || map.listerThings == null)
            {
                return result;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(thingDef);
            if (things == null)
            {
                return result;
            }

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (this.IsUsableReachableMapResource(pawn, map, thing) &&
                    (stagedMaterials == null || !stagedMaterials.Contains(thing)))
                {
                    result.Add(thing);
                }
            }

            return result;
        }

        private Thing FindClosestReachableMapResourceForHaul(
            Pawn pawn,
            Map map,
            ThingDef thingDef,
            ShuttleHullRepairMaterialLedger stagedMaterials)
        {
            List<Thing> candidates = this.GetReachableMapResources(
                pawn,
                map,
                thingDef,
                stagedMaterials);
            Thing bestThing = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing thing = candidates[i];
                if (thing == null)
                {
                    continue;
                }

                float distance = (pawn.Position - thing.Position).LengthHorizontalSquared;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestThing = thing;
                }
            }

            return bestThing;
        }

        private bool IsUsableReachableMapResource(Pawn pawn, Map map, Thing thing)
        {
            return pawn != null &&
                map != null &&
                thing != null &&
                !thing.Destroyed &&
                thing.Spawned &&
                thing.Map == map &&
                thing.def != null &&
                thing.def.category == ThingCategory.Item &&
                thing.stackCount > 0 &&
                !thing.IsForbidden(pawn) &&
                pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly);
        }

        private List<ThingDefCountClass> BuildMissingTrackedCostRequirements(
            Map map,
            IReadOnlyList<ThingDefCountClass> costs,
            ShuttleHullRepairMaterialLedger stagedMaterials)
        {
            List<ThingDefCountClass> missing = new List<ThingDefCountClass>();
            List<ThingDefCountClass> requirements = this.BuildMergedCostRequirements(costs);
            for (int i = 0; i < requirements.Count; i++)
            {
                ThingDefCountClass cost = requirements[i];
                int staged = stagedMaterials != null
                    ? stagedMaterials.CountAvailable(cost.thingDef, map)
                    : 0;
                if (staged < cost.count)
                {
                    missing.Add(new ThingDefCountClass(cost.thingDef, cost.count - staged));
                }
            }

            return missing;
        }

        private int GetRequiredCount(
            IReadOnlyList<ThingDefCountClass> costs,
            ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return 0;
            }

            List<ThingDefCountClass> requirements = this.BuildMergedCostRequirements(costs);
            for (int i = 0; i < requirements.Count; i++)
            {
                ThingDefCountClass requirement = requirements[i];
                if (requirement != null && requirement.thingDef == thingDef)
                {
                    return requirement.count;
                }
            }

            return 0;
        }

        private IntVec3 GetRepairMaterialStagingCell(ThingWithComps shuttle)
        {
            if (shuttle == null)
            {
                return IntVec3.Invalid;
            }

            Map map = shuttle.Map;
            IntVec3 interactionCell = shuttle.InteractionCell;
            if (map != null && interactionCell.IsValid && interactionCell.InBounds(map))
            {
                return interactionCell;
            }

            return shuttle.Spawned ? shuttle.Position : IntVec3.Invalid;
        }

        private void AddOrMergeCost(
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

        private int CeilToPositiveInt(float value)
        {
            if (!this.IsFinitePositive(value))
            {
                return 0;
            }

            double rounded = Math.Ceiling(value);
            return rounded > int.MaxValue ? int.MaxValue : (int)rounded;
        }

        private int SaturatingMultiply(int left, int right)
        {
            if (left <= 0 || right <= 0)
            {
                return 0;
            }

            return left > int.MaxValue / right ? int.MaxValue : left * right;
        }

        private int SaturatingAdd(int left, int right)
        {
            if (right <= 0)
            {
                return left;
            }

            return int.MaxValue - left < right ? int.MaxValue : left + right;
        }

        private bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}

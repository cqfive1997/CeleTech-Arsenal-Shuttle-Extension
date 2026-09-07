using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyConstructionHaulUtility
    {
        internal const string FillMaterialJobDefName = "CT_Shuttle_FillAssemblyConstructionMaterial";
        internal static IEnumerable<Thing> AllShuttlesAwaitingMaterials(Map map)
        {
            if (map == null)
            {
                yield break;
            }

            List<Thing> shuttleHosts = ShuttleHostCandidateUtility.GetModularShuttleHosts(map);

            for (int i = 0; i < shuttleHosts.Count; i++)
            {
                ThingWithComps thing = shuttleHosts[i] as ThingWithComps;
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }

                CompModularShuttleCore core = thing.TryGetComp<CompModularShuttleCore>();
                if (core == null || core.Controller == null)
                {
                    continue;
                }

                if (IsShuttleAwaitingMaterials(thing))
                {
                    yield return thing;
                }
            }
        }

        internal static bool IsShuttleAwaitingMaterials(Thing shuttleHost)
        {
            ShuttleAssemblyConstructionState state;
            return TryGetConstructionState(shuttleHost, out state) &&
                state.HasActiveOrder &&
                state.ActiveOrder != null &&
                state.ActiveOrder.Status == ShuttleAssemblyConstructionStatus.AwaitingMaterials;
        }

        internal static bool TryFindNextMaterialForShuttle(
            Pawn pawn,
            Thing shuttleHost,
            out Thing material,
            out int count)
        {
            material = null;
            count = 0;
            if (pawn == null || pawn.Map == null || shuttleHost == null || shuttleHost.Map != pawn.Map)
            {
                return false;
            }

            ShuttleAssemblyConstructionState state;
            if (!TryGetConstructionState(shuttleHost, out state) ||
                state.ActiveOrder == null ||
                state.ActiveOrder.RequiredCostList == null)
            {
                return false;
            }

            IReadOnlyList<ThingDefCountClass> requiredCosts = state.ActiveOrder.RequiredCostList;
            for (int i = 0; i < requiredCosts.Count; i++)
            {
                ThingDefCountClass cost = requiredCosts[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                int missing = ShuttleAssemblyConstructionEnrouteUtility.SpaceRemainingWithEnroute(
                    pawn,
                    shuttleHost,
                    cost.thingDef);
                if (missing <= 0)
                {
                    continue;
                }

                Thing candidate = FindClosestReachableMaterial(pawn, cost.thingDef);
                if (candidate == null)
                {
                    continue;
                }

                material = candidate;
                int carryCapacity = pawn.carryTracker != null
                    ? pawn.carryTracker.MaxStackSpaceEver(candidate.def)
                    : 0;
                count = Mathf.Min(missing, candidate.stackCount, carryCapacity);
                return count > 0;
            }

            return false;
        }

        internal static bool CanFillWithMaterial(Pawn pawn, Thing shuttleHost, Thing material)
        {
            if (pawn == null || shuttleHost == null || material == null)
            {
                return false;
            }

            ShuttleAssemblyConstructionState state;
            if (!TryGetConstructionState(shuttleHost, out state) ||
                state.ActiveOrder == null ||
                state.ActiveOrder.Status != ShuttleAssemblyConstructionStatus.AwaitingMaterials)
            {
                return false;
            }

            return material.def != null &&
                ShuttleConstructionMaterialUtility.CountMissing(
                    state.ActiveOrder,
                    state.StagedIngredients,
                    material.def) > 0;
        }

        internal static bool TryReserveForFillJob(Pawn pawn, Job job, Thing material, Thing shuttleHost, bool errorOnFailed)
        {
            if (pawn == null || job == null || material == null || shuttleHost == null)
            {
                return false;
            }

            return ShuttleAssemblyConstructionEnrouteUtility.TryReserveMaterialAndRegister(
                pawn,
                job,
                material,
                shuttleHost,
                errorOnFailed);
        }

        internal static bool TryStageCarriedMaterial(
            Pawn pawn,
            Thing shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (pawn == null || pawn.carryTracker == null || shuttleHost == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_HaulMissingPawnOrHost".Translate().ToString();
                return false;
            }

            Thing carried = pawn.carryTracker.CarriedThing;
            if (carried == null || carried.Destroyed || carried.stackCount <= 0 || carried.def == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_PawnNotCarryingMaterial".Translate().ToString();
                return false;
            }

            ShuttleAssemblyConstructionState state;
            if (!TryGetConstructionState(shuttleHost, out state) ||
                state.ActiveOrder == null ||
                state.ActiveOrder.Status != ShuttleAssemblyConstructionStatus.AwaitingMaterials)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_OrderNoLongerAwaitingMaterials"
                    .Translate()
                    .ToString();
                return false;
            }

            int needed = ShuttleConstructionMaterialUtility.CountMissing(
                state.ActiveOrder,
                state.StagedIngredients,
                carried.def);
            int transferCount = Mathf.Min(needed, carried.stackCount);
            if (transferCount <= 0)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_CarriedMaterialNoLongerNeeded"
                    .Translate()
                    .ToString();
                return false;
            }

            ThingOwner<Thing> carriedContainer = pawn.carryTracker.innerContainer;
            if (carriedContainer == null || !carriedContainer.Contains(carried))
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_CarriedContainerUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            Thing stagedThing;
            int moved = carriedContainer.TryTransferToContainer(
                carried,
                state.StagedIngredients,
                transferCount,
                out stagedThing,
                false);
            if (moved <= 0)
            {
                if (stagedThing != null && !stagedThing.Destroyed && !state.StagedIngredients.Contains(stagedThing))
                {
                    TryPlaceNearShuttle(stagedThing, shuttleHost);
                }

                failureReason = "CT_Shuttle_AssemblyConstruction_CouldNotStageMaterial".Translate().ToString();
                return false;
            }

            if (moved < transferCount)
            {
                ShuttleLog.WarnOnce(
                    "AssemblyConstruction",
                    "partial-material-stage-transfer",
                    "Partial shuttle construction material transfer staged " + moved + " of " +
                    transferCount + " " + carried.def.defName + ". Keeping the successful staged amount.");
            }

            ShuttleConstructionMaterialUtility.RefreshMaterialState(state.ActiveOrder, state.StagedIngredients);
            DropNoLongerNeededCarriedMaterial(pawn, shuttleHost);
            return true;
        }

        internal static bool TryGetConstructionState(
            Thing shuttleHost,
            out ShuttleAssemblyConstructionState state)
        {
            state = null;
            ThingWithComps withComps = shuttleHost as ThingWithComps;
            CompModularShuttleCore core = withComps != null ? withComps.TryGetComp<CompModularShuttleCore>() : null;
            ShuttleController controller = core != null ? core.Controller : null;
            ShuttleRuntimeState runtimeState = controller != null ? controller.GetLaunchRuntimeState() : null;
            if (runtimeState == null)
            {
                return false;
            }

            runtimeState.EnsureInitialized();
            state = runtimeState.AssemblyConstruction;
            if (state == null)
            {
                return false;
            }

            state.EnsureInitialized();
            return true;
        }

        private static Thing FindClosestReachableMaterial(Pawn pawn, ThingDef thingDef)
        {
            if (pawn == null || pawn.Map == null || thingDef == null)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(thingDef),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f,
                delegate(Thing thing)
                {
                    return IsUsableMaterialForPawn(pawn, thing);
                });
        }

        private static bool IsUsableMaterialForPawn(Pawn pawn, Thing thing)
        {
            return pawn != null &&
                thing != null &&
                !thing.Destroyed &&
                thing.Spawned &&
                thing.def != null &&
                thing.def.category == ThingCategory.Item &&
                thing.stackCount > 0 &&
                !thing.IsForbidden(pawn) &&
                pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly);
        }

        private static void DropNoLongerNeededCarriedMaterial(Pawn pawn, Thing shuttleHost)
        {
            if (pawn == null || pawn.carryTracker == null)
            {
                return;
            }

            Thing carried = pawn.carryTracker.CarriedThing;
            if (carried == null)
            {
                return;
            }

            if (CanFillWithMaterial(pawn, shuttleHost, carried))
            {
                return;
            }

            Thing dropped;
            pawn.carryTracker.TryDropCarriedThing(
                shuttleHost != null ? shuttleHost.Position : pawn.Position,
                ThingPlaceMode.Near,
                out dropped);
        }

        private static void TryPlaceNearShuttle(Thing thing, Thing shuttleHost)
        {
            if (thing == null || thing.Destroyed)
            {
                return;
            }

            Map map = shuttleHost != null ? shuttleHost.Map : null;
            if (map == null)
            {
                thing.Destroy();
                return;
            }

            GenPlace.TryPlaceThing(
                thing,
                shuttleHost.Spawned ? shuttleHost.Position : map.Center,
                map,
                ThingPlaceMode.Near);
        }
    }
}

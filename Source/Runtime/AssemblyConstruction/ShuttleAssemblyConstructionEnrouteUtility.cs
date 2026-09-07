using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyConstructionEnrouteUtility
    {
        internal static int SpaceRemainingFor(Thing shuttleHost, ThingDef materialDef)
        {
            if (shuttleHost == null || materialDef == null)
            {
                return 0;
            }

            ShuttleAssemblyConstructionState state;
            if (!ShuttleAssemblyConstructionHaulUtility.TryGetConstructionState(
                    shuttleHost,
                    out state) ||
                !state.HasActiveOrder ||
                state.ActiveOrder == null ||
                state.ActiveOrder.Status != ShuttleAssemblyConstructionStatus.AwaitingMaterials)
            {
                return 0;
            }

            return Mathf.Max(
                ShuttleConstructionMaterialUtility.CountMissing(
                    state.ActiveOrder,
                    state.StagedIngredients,
                    materialDef),
                0);
        }

        internal static int SpaceRemainingWithEnroute(
            Pawn pawn,
            Thing shuttleHost,
            ThingDef materialDef)
        {
            IHaulEnroute destination = shuttleHost as IHaulEnroute;
            if (destination == null || destination.Map == null || materialDef == null)
            {
                return 0;
            }

            int constructionDemand = SpaceRemainingFor(shuttleHost, materialDef);
            int constructionEnroute = destination.Map.enrouteManager.GetEnroute(
                destination,
                materialDef,
                pawn);
            return Mathf.Max(constructionDemand - constructionEnroute, 0);
        }

        internal static bool TryReserveMaterialAndRegister(
            Pawn pawn,
            Job job,
            Thing material,
            Thing shuttleHost,
            bool errorOnFailed)
        {
            IHaulEnroute destination = shuttleHost as IHaulEnroute;
            if (pawn == null ||
                pawn.Map == null ||
                job == null ||
                material == null ||
                material.def == null ||
                destination == null ||
                destination.Map != pawn.Map)
            {
                return false;
            }

            int available = SpaceRemainingWithEnroute(
                null,
                shuttleHost,
                material.def);
            int carryCapacity = pawn.carryTracker != null
                ? pawn.carryTracker.MaxStackSpaceEver(material.def)
                : 0;
            int count = Mathf.Min(
                Mathf.Max(job.count, 0),
                material.stackCount,
                available,
                carryCapacity);
            if (count <= 0)
            {
                return false;
            }

            job.count = count;
            if (!pawn.Reserve(material, job, 1, count, null, errorOnFailed, false))
            {
                return false;
            }

            destination.Map.enrouteManager.AddEnroute(
                destination,
                pawn,
                material.def,
                count);
            return true;
        }

        internal static void InterruptEnrouteHaulers(Thing shuttleHost)
        {
            IHaulEnroute destination = shuttleHost as IHaulEnroute;
            if (destination == null || destination.Map == null)
            {
                return;
            }

            destination.Map.enrouteManager.InterruptEnroutePawns(destination, null);
        }
    }
}

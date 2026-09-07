using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyConstructionWorkUtility
    {
        internal const string ConstructOrderJobDefName = "CT_Shuttle_ConstructAssemblyOrder";
        internal const int MaxShuttleConstructors = 1;

        internal static IEnumerable<Thing> AllShuttlesReadyForConstruction(Map map)
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

                if (IsShuttleReadyForConstruction(thing))
                {
                    yield return thing;
                }
            }
        }

        internal static bool IsShuttleReadyForConstruction(Thing shuttleHost)
        {
            ShuttleAssemblyConstructionState state;
            if (!ShuttleAssemblyConstructionHaulUtility.TryGetConstructionState(shuttleHost, out state) ||
                state.ActiveOrder == null ||
                state.ActiveOrder.Status != ShuttleAssemblyConstructionStatus.Working)
            {
                return false;
            }

            return state.ActiveOrder.WorkDone < state.ActiveOrder.WorkTotal &&
                ShuttleConstructionMaterialUtility.HasAllRequiredMaterials(
                    state.ActiveOrder,
                    state.StagedIngredients);
        }

        internal static ShuttleAssemblyConstructionOrder GetActiveOrder(Thing shuttleHost)
        {
            ShuttleAssemblyConstructionState state;
            if (!ShuttleAssemblyConstructionHaulUtility.TryGetConstructionState(
                    shuttleHost,
                    out state))
            {
                return null;
            }

            return state.ActiveOrder;
        }

        internal static bool TryGetController(Thing shuttleHost, out ShuttleController controller)
        {
            controller = null;
            ThingWithComps withComps = shuttleHost as ThingWithComps;
            CompModularShuttleCore core = withComps != null ? withComps.TryGetComp<CompModularShuttleCore>() : null;
            controller = core != null ? core.Controller : null;
            return controller != null;
        }

        internal static bool TryReserveForConstructionJob(
            Pawn pawn,
            Job job,
            Thing shuttleHost,
            bool errorOnFailed)
        {
            if (pawn == null || job == null || shuttleHost == null)
            {
                return false;
            }

            return pawn.Reserve(
                shuttleHost,
                job,
                MaxShuttleConstructors,
                1,
                null,
                errorOnFailed,
                false);
        }
    }
}

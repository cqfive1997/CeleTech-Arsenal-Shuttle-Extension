using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    internal static class ShuttleModuleRemovalWorkUtility
    {
        internal const string DeconstructModuleJobDefName = "CT_Shuttle_DeconstructModule";
        internal const int MaxShuttleDeconstructors = 1;

        internal static IEnumerable<Thing> AllShuttlesReadyForRemovalWork(Map map)
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

                if (IsShuttleReadyForRemovalWork(thing))
                {
                    yield return thing;
                }
            }
        }

        internal static bool IsShuttleReadyForRemovalWork(Thing shuttleHost)
        {
            ShuttleController controller;
            return TryGetController(shuttleHost, out controller) &&
                controller.IsReadyForModuleRemovalWork();
        }

        internal static bool CanPawnTakeRemovalWork(Pawn pawn, Thing shuttleHost, bool forced)
        {
            if (pawn == null ||
                shuttleHost == null ||
                !shuttleHost.Spawned ||
                pawn.Downed ||
                pawn.Drafted ||
                pawn.jobs == null)
            {
                return false;
            }

            if (!IsShuttleReadyForRemovalWork(shuttleHost))
            {
                return false;
            }

            return pawn.CanReserveAndReach(
                shuttleHost,
                PathEndMode.InteractionCell,
                Danger.Deadly,
                MaxShuttleDeconstructors,
                1,
                null,
                forced);
        }

        internal static bool TryGetController(Thing shuttleHost, out ShuttleController controller)
        {
            controller = null;
            ThingWithComps withComps = shuttleHost as ThingWithComps;
            CompModularShuttleCore core = withComps != null ? withComps.TryGetComp<CompModularShuttleCore>() : null;
            controller = core != null ? core.Controller : null;
            return controller != null;
        }

        internal static bool TryReserveForRemovalJob(
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
                MaxShuttleDeconstructors,
                1,
                null,
                errorOnFailed,
                false);
        }
    }
}

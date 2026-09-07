using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    public sealed class WorkGiver_ConstructShuttleAssemblyOrder : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ShuttleAssemblyConstructionWorkUtility.AllShuttlesReadyForConstruction(
                pawn != null ? pawn.Map : null);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || !t.Spawned || pawn.Downed || pawn.Drafted)
            {
                return false;
            }

            if (!ShuttleAssemblyConstructionWorkUtility.IsShuttleReadyForConstruction(t))
            {
                return false;
            }

            return pawn.CanReserveAndReach(
                t,
                PathEndMode.InteractionCell,
                Danger.Deadly,
                ShuttleAssemblyConstructionWorkUtility.MaxShuttleConstructors,
                1,
                null,
                forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                ShuttleAssemblyConstructionWorkUtility.ConstructOrderJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return null;
            }

            return JobMaker.MakeJob(jobDef, t);
        }
    }
}

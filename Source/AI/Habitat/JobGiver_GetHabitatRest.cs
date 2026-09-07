using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    public sealed class JobGiver_GetHabitatRest : ThinkNode_JobGiver
    {
        // Vanilla rest uses priority 8 for both bed and ground sleep. Once the existing
        // onboard-facility policy admits Habitat use, prefer it as the rest destination.
        private const float RestPriority = 8.1f;
        private const float LowRestThreshold = 0.3f;
        private const float MeditateRestThreshold = 0.16f;
        private const string HabitatRestJobDefName = "CT_Shuttle_HabitatRest";

        public override float GetPriority(Pawn pawn)
        {
            Thing shuttleHost;
            float priority;
            return TryFindRestHabitat(pawn, out shuttleHost, out priority) ? priority : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing shuttleHost;
            float priority;
            if (!TryFindRestHabitat(pawn, out shuttleHost, out priority))
            {
                return null;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(HabitatRestJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                // Fail closed if the JobDef XML is missing or incomplete.
                return null;
            }

            Job job = JobMaker.MakeJob(jobDef, shuttleHost);
            job.targetA = shuttleHost;
            return job;
        }

        private static bool TryFindRestHabitat(Pawn pawn, out Thing shuttleHost, out float priority)
        {
            shuttleHost = null;
            priority = 0f;
            if (!IsEligiblePawn(pawn))
            {
                return false;
            }

            Need_Rest rest = pawn.needs.rest;
            float desiredPriority = GetTimetableRestPriority(pawn, rest.CurLevel);
            if (desiredPriority <= 0f)
            {
                return false;
            }

            shuttleHost = HabitatUtility.FindBestHabitatForSleep(pawn);
            if (shuttleHost == null || shuttleHost.Destroyed)
            {
                shuttleHost = null;
                return false;
            }

            priority = desiredPriority;
            return true;
        }

        private static bool IsEligiblePawn(Pawn pawn)
        {
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                !pawn.Spawned ||
                pawn.Map == null ||
                pawn.mindState == null ||
                pawn.Drafted ||
                pawn.Downed ||
                pawn.needs == null ||
                pawn.needs.rest == null ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike ||
                pawn.Faction != Faction.OfPlayer ||
                !pawn.IsFreeColonist ||
                pawn.IsPrisonerOfColony ||
                pawn.IsSlaveOfColony)
            {
                return false;
            }

            if (pawn.InMentalState && (pawn.MentalState == null || !pawn.MentalState.AllowRestingInBed))
            {
                return false;
            }

            Lord lord = pawn.GetLord();
            if (lord != null && lord.CurLordToil != null && !lord.CurLordToil.AllowSatisfyLongNeeds)
            {
                return false;
            }

            return Find.TickManager.TicksGame >= pawn.mindState.canSleepTick &&
                RestUtility.CanFallAsleep(pawn);
        }

        private static float GetTimetableRestPriority(Pawn pawn, float restLevel)
        {
            TimeAssignmentDef assignment = pawn.timetable != null
                ? pawn.timetable.CurrentAssignment
                : TimeAssignmentDefOf.Anything;

            if (assignment == TimeAssignmentDefOf.Sleep)
            {
                return RestPriority;
            }

            if (assignment == TimeAssignmentDefOf.Anything || assignment == TimeAssignmentDefOf.Joy)
            {
                return restLevel < LowRestThreshold ? RestPriority : 0f;
            }

            if (assignment == TimeAssignmentDefOf.Meditate)
            {
                return restLevel < MeditateRestThreshold ? RestPriority : 0f;
            }

            return 0f;
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    /// <summary>
    /// Read-only Habitat/Recreation query helpers for job givers and job drivers.
    /// These helpers do not reserve, create jobs, or mutate runtime state.
    /// </summary>
    internal static class HabitatUtility
    {
        private const string HabitatRestJobDefName = "CT_Shuttle_HabitatRest";
        internal const string HabitatIngestJobDefName = "CT_Shuttle_HabitatIngest";
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";

        internal static bool TryGetShuttleController(Thing shuttleHost, out ShuttleController controller)
        {
            controller = null;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                return false;
            }

            CompModularShuttleCore core = shuttleWithComps.TryGetComp<CompModularShuttleCore>();
            if (core == null || core.Controller == null)
            {
                return false;
            }

            controller = core.Controller;
            return true;
        }

        internal static bool TryGetHabitatOccupancy(
            Thing shuttleHost,
            out CompShuttleHabitatOccupancy occupancy)
        {
            occupancy = null;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                return false;
            }

            occupancy = shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>();
            return occupancy != null;
        }

        internal static bool TryGetPoweredHabitatProfile(
            Thing shuttleHost,
            out ShuttleProfile profile,
            out HabitatProfile habitat)
        {
            profile = null;
            habitat = null;

            ShuttleController controller;
            if (!TryGetShuttleController(shuttleHost, out controller))
            {
                return false;
            }

            profile = controller.GetProfileForRead();
            habitat = profile != null ? profile.Habitat : null;
            if (habitat == null || !habitat.HasHabitat)
            {
                return false;
            }

            ShuttlePowerRuntimeSnapshot powerSnapshot = controller.BuildPowerRuntimeSnapshot();
            return powerSnapshot != null && powerSnapshot.InternalBusPowered;
        }

        internal static bool TryGetHabitatFoodSource(
            Thing shuttleHost,
            out IShuttleHabitatFoodSource foodSource)
        {
            foodSource = null;
            ShuttleController controller;
            if (!TryGetShuttleController(shuttleHost, out controller))
            {
                return false;
            }

            foodSource = controller.BuildHabitatFoodSource();
            return foodSource != null;
        }

        internal static bool CanUseHabitatForSleep(Pawn pawn, Thing shuttleHost, out string failReason)
        {
            failReason = null;
            if (pawn == null || shuttleHost == null)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (IsForbiddenToSpawnedPawn(pawn, shuttleHost))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (!ShuttleOnboardDeviceUsePolicy.AllowsColonistFacilityUse(
                pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                failReason = "CT_Shuttle_HabitatNotPowered".Translate().ToString();
                return false;
            }

            if (!habitat.SupportsSleep || habitat.SleepSlots <= 0)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static int CountCurrentHabitatSleepUsers(Thing shuttleHost)
        {
            return CountCurrentHabitatSleepUsers(shuttleHost, null);
        }

        internal static int CountCurrentHabitatSleepUsers(Thing shuttleHost, Pawn ignoredPawn)
        {
            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null)
            {
                return 0;
            }

            int count = 0;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            CompShuttleHabitatOccupancy occupancy = shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (occupancy != null)
            {
                count += occupancy.SleepingOccupantCount;
                if (ignoredPawn != null && occupancy.ContainsSleepingPawn(ignoredPawn))
                {
                    count--;
                }
            }

            JobDef habitatRestJobDef = DefDatabase<JobDef>.GetNamedSilentFail(HabitatRestJobDefName);
            if (habitatRestJobDef == null)
            {
                return count < 0 ? 0 : count;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns != null
                ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return count < 0 ? 0 : count;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == ignoredPawn)
                {
                    continue;
                }

                Job job = pawn != null ? pawn.CurJob : null;
                if (job == null || job.def != habitatRestJobDef)
                {
                    continue;
                }

                if (TargetReferences(job.GetTarget(TargetIndex.A), shuttleHost) ||
                    TargetReferences(job.GetTarget(TargetIndex.B), shuttleHost))
                {
                    count++;
                }
            }

            return count < 0 ? 0 : count;
        }

        internal static bool HasAvailableSleepSlot(Pawn pawn, Thing shuttleHost, out string failReason)
        {
            failReason = null;

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!CanUseHabitatForSleep(pawn, shuttleHost, out failReason) ||
                !TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                return false;
            }

            int currentUsers = CountCurrentHabitatSleepUsers(shuttleHost);
            if (currentUsers >= habitat.SleepSlots)
            {
                failReason = "CT_Shuttle_HabitatSlotsFull".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanUseHabitatForDining(Pawn pawn, Thing shuttleHost, out string failReason)
        {
            failReason = null;
            if (pawn == null || shuttleHost == null)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (IsForbiddenToSpawnedPawn(pawn, shuttleHost))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (!ShuttleOnboardDeviceUsePolicy.AllowsColonistFacilityUse(
                pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                failReason = "CT_Shuttle_HabitatNotPowered".Translate().ToString();
                return false;
            }

            if (!habitat.SupportsDining || habitat.DiningSlots <= 0)
            {
                failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                return false;
            }

            if (!habitat.AllowsInventoryFood && !habitat.AllowsCargoFoodWithdrawal)
            {
                failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static int CountCurrentHabitatDiningUsers(Thing shuttleHost)
        {
            return CountCurrentHabitatDiningUsers(shuttleHost, null);
        }

        internal static int CountCurrentHabitatDiningUsers(Thing shuttleHost, Pawn ignoredPawn)
        {
            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null)
            {
                return 0;
            }

            int count = 0;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            CompShuttleHabitatOccupancy occupancy = shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (occupancy != null)
            {
                count += occupancy.DiningOccupantCount;
                if (ignoredPawn != null && occupancy.ContainsDiningPawn(ignoredPawn))
                {
                    count--;
                }
            }

            JobDef habitatIngestJobDef = DefDatabase<JobDef>.GetNamedSilentFail(HabitatIngestJobDefName);
            if (habitatIngestJobDef == null)
            {
                return count < 0 ? 0 : count;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns != null
                ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return count < 0 ? 0 : count;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == ignoredPawn)
                {
                    continue;
                }

                Job job = pawn.CurJob;
                if (job == null || job.def != habitatIngestJobDef)
                {
                    continue;
                }

                if (TargetReferences(job.GetTarget(TargetIndex.A), shuttleHost))
                {
                    count++;
                }
            }

            return count < 0 ? 0 : count;
        }

        internal static bool HasAvailableDiningSlot(Pawn pawn, Thing shuttleHost, out string failReason)
        {
            failReason = null;

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!CanUseHabitatForDining(pawn, shuttleHost, out failReason) ||
                !TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                return false;
            }

            int currentUsers = CountCurrentHabitatDiningUsers(shuttleHost);
            if (currentUsers >= habitat.DiningSlots)
            {
                failReason = "CT_Shuttle_HabitatDiningSlotsFull".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanUseHabitatForJoy(
            Pawn pawn,
            Thing shuttleHost,
            JoyKindDef joyKind,
            out string failReason)
        {
            failReason = null;
            if (pawn == null || shuttleHost == null || joyKind == null)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (IsForbiddenToSpawnedPawn(pawn, shuttleHost))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (!ShuttleOnboardDeviceUsePolicy.AllowsColonistFacilityUse(
                pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (!pawn.Spawned ||
                !shuttleHost.Spawned ||
                pawn.Map == null ||
                shuttleHost.Map == null ||
                pawn.Map != shuttleHost.Map ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                failReason = "CT_Shuttle_HabitatNotPowered".Translate().ToString();
                return false;
            }

            if (!habitat.SupportsJoy || habitat.JoySlots <= 0 || !habitat.SupportsJoyKind(joyKind))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            int currentUsers = CountCurrentHabitatJoyUsers(shuttleHost, pawn);
            if (currentUsers >= habitat.JoySlots)
            {
                failReason = "CT_Shuttle_HabitatSlotsFull".Translate().ToString();
                return false;
            }

            if (!pawn.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            if (!pawn.CanReserve(shuttleHost, Max(1, habitat.JoySlots), -1, null, false))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static int CountCurrentHabitatJoyUsers(Thing shuttleHost)
        {
            return CountCurrentHabitatJoyUsers(shuttleHost, null);
        }

        internal static int CountCurrentHabitatJoyUsers(Thing shuttleHost, Pawn ignoredPawn)
        {
            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null)
            {
                return 0;
            }

            int count = 0;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            CompShuttleHabitatOccupancy occupancy = shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (occupancy != null)
            {
                count += occupancy.JoyOccupantCount;
                if (ignoredPawn != null && occupancy.ContainsJoyPawn(ignoredPawn))
                {
                    count--;
                }
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns != null
                ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return count < 0 ? 0 : count;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == ignoredPawn)
                {
                    continue;
                }

                Job job = pawn.CurJob;
                if (job == null ||
                    job.def == null ||
                    job.def.driverClass != typeof(JobDriver_HabitatJoy))
                {
                    continue;
                }

                if (TargetReferences(job.GetTarget(TargetIndex.A), shuttleHost))
                {
                    count++;
                }
            }

            return count < 0 ? 0 : count;
        }

        internal static bool HasAvailableJoySlot(
            Pawn pawn,
            Thing shuttleHost,
            JoyKindDef joyKind,
            out string failReason)
        {
            return CanUseHabitatForJoy(pawn, shuttleHost, joyKind, out failReason);
        }

        internal static Thing FindBestHabitatForJoy(Pawn pawn, JoyKindDef joyKind)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || joyKind == null)
            {
                return null;
            }

            ThingDef shuttleDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            if (shuttleDef == null)
            {
                return null;
            }

            List<Thing> candidates = pawn.Map.listerThings.ThingsOfDef(shuttleDef);
            if (candidates == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistanceSquared = 0f;
            int bestJoyThoughtStageIndex = -1;
            float bestJoyGainFactor = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing candidate = candidates[i];
                if (!IsCandidateUsableForJoy(pawn, candidate, joyKind))
                {
                    continue;
                }

                ShuttleProfile profile;
                HabitatProfile habitat;
                if (!TryGetPoweredHabitatProfile(candidate, out profile, out habitat))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(candidate.Position);
                bool isBetter =
                    best == null ||
                    distanceSquared < bestDistanceSquared ||
                    (distanceSquared == bestDistanceSquared &&
                     habitat.JoyThoughtStageIndex > bestJoyThoughtStageIndex) ||
                    (distanceSquared == bestDistanceSquared &&
                     habitat.JoyThoughtStageIndex == bestJoyThoughtStageIndex &&
                     habitat.JoyGainFactor > bestJoyGainFactor);
                if (!isBetter)
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
                bestJoyThoughtStageIndex = habitat.JoyThoughtStageIndex;
                bestJoyGainFactor = habitat.JoyGainFactor;
            }

            return best;
        }

        internal static Thing FindBestHabitatForDining(Pawn pawn)
        {
            return FindBestHabitatForDining(pawn, null);
        }

        internal static Thing FindBestHabitatForDining(
            Pawn pawn,
            HabitatDiningCandidatePredicate extraPredicate)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return null;
            }

            ThingDef shuttleDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            if (shuttleDef == null)
            {
                return null;
            }

            List<Thing> candidates = pawn.Map.listerThings.ThingsOfDef(shuttleDef);
            if (candidates == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistanceSquared = 0f;
            int bestDiningThoughtStageIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing candidate = candidates[i];
                if (!IsCandidateUsableForDining(pawn, candidate))
                {
                    continue;
                }

                ShuttleProfile profile;
                HabitatProfile habitat;
                if (!TryGetPoweredHabitatProfile(candidate, out profile, out habitat))
                {
                    continue;
                }

                if (extraPredicate != null && !extraPredicate(candidate, habitat))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(candidate.Position);
                bool isBetter =
                    best == null ||
                    distanceSquared < bestDistanceSquared ||
                    (distanceSquared == bestDistanceSquared &&
                     habitat.DiningThoughtStageIndex > bestDiningThoughtStageIndex);
                if (!isBetter)
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
                bestDiningThoughtStageIndex = habitat.DiningThoughtStageIndex;
            }

            return best;
        }

        internal static Thing FindBestHabitatForSleep(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return null;
            }

            ThingDef shuttleDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            if (shuttleDef == null)
            {
                return null;
            }

            List<Thing> candidates = pawn.Map.listerThings.ThingsOfDef(shuttleDef);
            if (candidates == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistanceSquared = 0f;
            int bestSleepThoughtStageIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing candidate = candidates[i];
                if (!IsCandidateUsableForSleep(pawn, candidate))
                {
                    continue;
                }

                ShuttleProfile profile;
                HabitatProfile habitat;
                if (!TryGetPoweredHabitatProfile(candidate, out profile, out habitat))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(candidate.Position);
                bool isBetter =
                    best == null ||
                    distanceSquared < bestDistanceSquared ||
                    (distanceSquared == bestDistanceSquared &&
                     habitat.SleepThoughtStageIndex > bestSleepThoughtStageIndex);
                if (!isBetter)
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
                bestSleepThoughtStageIndex = habitat.SleepThoughtStageIndex;
            }

            return best;
        }

        private static bool IsCandidateUsableForSleep(Pawn pawn, Thing candidate)
        {
            if (candidate == null ||
                candidate.Destroyed ||
                !candidate.Spawned ||
                candidate.Map != pawn.Map ||
                candidate.Faction != Faction.OfPlayer)
            {
                return false;
            }

            string failReason;
            if (!HasAvailableSleepSlot(pawn, candidate, out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!TryGetPoweredHabitatProfile(candidate, out profile, out habitat))
            {
                return false;
            }

            if (!pawn.CanReach(candidate, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                return false;
            }

            return pawn.CanReserve(candidate, Max(1, habitat.SleepSlots), -1, null, false);
        }

        private static bool IsCandidateUsableForDining(Pawn pawn, Thing candidate)
        {
            if (candidate == null ||
                candidate.Destroyed ||
                !candidate.Spawned ||
                candidate.Map != pawn.Map ||
                candidate.Faction != Faction.OfPlayer)
            {
                return false;
            }

            string failReason;
            if (!HasAvailableDiningSlot(pawn, candidate, out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!TryGetPoweredHabitatProfile(candidate, out profile, out habitat))
            {
                return false;
            }

            if (!pawn.CanReach(candidate, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                return false;
            }

            return pawn.CanReserve(candidate, Max(1, habitat.DiningSlots), -1, null, false);
        }

        private static bool IsCandidateUsableForJoy(Pawn pawn, Thing candidate, JoyKindDef joyKind)
        {
            if (candidate == null ||
                candidate.Destroyed ||
                !candidate.Spawned ||
                candidate.Map != pawn.Map ||
                candidate.Faction != Faction.OfPlayer)
            {
                return false;
            }

            string failReason;
            if (!HasAvailableJoySlot(pawn, candidate, joyKind, out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!TryGetPoweredHabitatProfile(candidate, out profile, out habitat))
            {
                return false;
            }

            if (!pawn.CanReach(candidate, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                return false;
            }

            return pawn.CanReserve(candidate, Max(1, habitat.JoySlots), -1, null, false);
        }

        private static bool TargetReferences(LocalTargetInfo target, Thing shuttleHost)
        {
            return target.IsValid && target.HasThing && target.Thing == shuttleHost;
        }

        private static bool IsForbiddenToSpawnedPawn(Pawn pawn, Thing shuttleHost)
        {
            return pawn != null &&
                shuttleHost != null &&
                pawn.Spawned &&
                shuttleHost.Spawned &&
                pawn.Map == shuttleHost.Map &&
                shuttleHost.IsForbidden(pawn);
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        internal delegate bool HabitatDiningCandidatePredicate(Thing shuttleHost, HabitatProfile habitat);
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ModularShuttleCaravanUtility
    {
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";
        private static int cachedTick = -1;
        private static int cachedCaravanHash = -1;
        private static ThingWithComps cachedShuttle;
        private static bool cachedHasShuttle;

        internal static bool IsModularShuttleThing(Thing thing)
        {
            Thing inner = thing != null ? thing.GetInnerIfMinified() : null;
            ThingWithComps withComps = inner as ThingWithComps;
            return withComps != null &&
                (withComps.TryGetComp<CompModularShuttleCore>() != null ||
                    (withComps.def != null && withComps.def.defName == ModularShuttleHostDefName));
        }

        internal static bool TryFindHeldModularShuttleCached(Caravan caravan, out ThingWithComps shuttle)
        {
            shuttle = null;
            int tick = ShuttleTickUtility.TicksGameOrZero();
            int caravanHash = caravan != null ? caravan.GetHashCode() : 0;
            if (tick == cachedTick && caravanHash == cachedCaravanHash)
            {
                shuttle = cachedHasShuttle && cachedShuttle != null && !cachedShuttle.Destroyed
                    ? cachedShuttle
                    : null;
                return shuttle != null;
            }

            cachedTick = tick;
            cachedCaravanHash = caravanHash;
            cachedHasShuttle = TryFindHeldModularShuttle(caravan, out cachedShuttle);
            shuttle = cachedHasShuttle ? cachedShuttle : null;
            return shuttle != null;
        }

        internal static bool TryFindHeldModularShuttle(Caravan caravan, out ThingWithComps shuttle)
        {
            shuttle = null;
            if (caravan == null)
            {
                return false;
            }

            List<Thing> inventory = CaravanInventoryUtility.AllInventoryItems(caravan);
            if (inventory == null)
            {
                return false;
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                Thing candidate = inventory[i];
                if (IsModularShuttleThing(candidate))
                {
                    shuttle = candidate.GetInnerIfMinified() as ThingWithComps;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryRemoveHeldModularShuttle(
            Caravan caravan,
            out ThingWithComps shuttle,
            out Pawn owner)
        {
            shuttle = null;
            owner = null;
            if (caravan == null || caravan.PawnsListForReading == null)
            {
                return false;
            }

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.inventory == null || pawn.inventory.innerContainer == null)
                {
                    continue;
                }

                ThingOwner<Thing> inventory = pawn.inventory.innerContainer;
                for (int j = inventory.Count - 1; j >= 0; j--)
                {
                    Thing candidate = inventory[j];
                    if (!IsModularShuttleThing(candidate))
                    {
                        continue;
                    }

                    ThingWithComps candidateShuttle = candidate.GetInnerIfMinified() as ThingWithComps;
                    if (candidateShuttle == null || !ReferenceEquals(candidate, candidateShuttle))
                    {
                        continue;
                    }

                    inventory.Remove(candidate);
                    shuttle = candidateShuttle;
                    owner = pawn;
                    ClearCachedHeldModularShuttle();
                    return true;
                }
            }

            return false;
        }

        internal static void ClearCachedHeldModularShuttle()
        {
            cachedTick = -1;
            cachedCaravanHash = -1;
            cachedShuttle = null;
            cachedHasShuttle = false;
        }

        internal static bool HasNonDownedPlayerController(Caravan caravan)
        {
            if (caravan == null || caravan.PawnsListForReading == null)
            {
                return false;
            }

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (IsValidPlayerColonistController(pawn))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasLaunchController(
            Caravan caravan,
            ShuttleCargoSnapshot shuttleCargoSnapshot)
        {
            return ShuttleLaunchCrewRequirementUtility.HasLoadedColonistInCockpit(shuttleCargoSnapshot) ||
                HasNonDownedPlayerController(caravan);
        }

        internal static bool HasLaunchController(
            Caravan caravan,
            ShuttleCargoSnapshot shuttleCargoSnapshot,
            ShuttleProfile profile)
        {
            return ShuttleLaunchCrewRequirementUtility.ProfileAllowsAutonomousLaunch(profile) ||
                HasLaunchController(caravan, shuttleCargoSnapshot);
        }

        internal static ShuttleCargoSnapshot BuildCaravanCargoSnapshot(
            Caravan caravan,
            ThingWithComps shuttle,
            ShuttleProfile profile,
            ShuttleCargoSnapshot baseSnapshot)
        {
            ShuttleCargoSnapshot snapshot = baseSnapshot ?? new ShuttleCargoSnapshot();
            float caravanPayloadMassKg = GetCaravanPayloadMassKg(caravan, shuttle);
            int caravanThingCount = CountCaravanThings(caravan);
            snapshot.HasTransporter = true;
            if (snapshot.CargoRegionCount <= 0)
            {
                snapshot.CargoRegionCount = 1;
            }

            if (snapshot.StructuralMass <= 0f && profile != null && profile.Mass != null)
            {
                snapshot.StructuralMass = profile.Mass.TotalMass;
            }

            snapshot.LoadedMassKg += caravanPayloadMassKg;
            bool hasRefrigeratedCapacity = snapshot.RefrigeratedMassKg > 0f;
            for (int i = 0;
                snapshot.RefrigeratedCargoModules != null &&
                    i < snapshot.RefrigeratedCargoModules.Count;
                i++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    snapshot.RefrigeratedCargoModules[i];
                hasRefrigeratedCapacity |= module != null && module.IsEnabled;
            }

            snapshot.RefrigeratedMassSharesOverallCapacity =
                ShuttleRefrigeratedCargoCapacityPolicy.SharesOverallCapacity;
            snapshot.RefrigeratedMassCapacityKg = hasRefrigeratedCapacity
                ? ShuttleRefrigeratedCargoCapacityPolicy
                    .GetRefrigeratedPoolCapacityKg(profile)
                : 0f;
            snapshot.MassCapacity =
                ShuttleRefrigeratedCargoCapacityPolicy.GetEffectiveTotalCapacityKg(
                    profile,
                    hasRefrigeratedCapacity);
            snapshot.LoadedThingCount += caravanThingCount;
            snapshot.LoadedStackCount += caravanThingCount;
            snapshot.ThingCount += caravanThingCount;
            snapshot.StackCount += caravanThingCount;
            RecalculateMass(snapshot);
            return snapshot;
        }

        private static float GetCaravanPayloadMassKg(Caravan caravan, ThingWithComps shuttle)
        {
            if (caravan == null)
            {
                return 0f;
            }

            float mass = CollectionsMassCalculator.MassUsage<Pawn>(
                caravan.PawnsListForReading,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                true,
                false);
            if (shuttle != null)
            {
                mass -= shuttle.GetStatValue(StatDefOf.Mass, true, -1);
            }

            return mass > 0f ? mass : 0f;
        }

        private static int CountCaravanThings(Caravan caravan)
        {
            if (caravan == null)
            {
                return 0;
            }

            int count = caravan.PawnsListForReading != null ? caravan.PawnsListForReading.Count : 0;
            List<Thing> inventory = CaravanInventoryUtility.AllInventoryItems(caravan);
            if (inventory == null)
            {
                return count;
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                if (!IsModularShuttleThing(inventory[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsValidPlayerColonistController(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Dead &&
                !pawn.Downed &&
                !pawn.InMentalState &&
                pawn.Faction == Faction.OfPlayer &&
                (pawn.IsColonist || pawn.IsColonistPlayerControlled);
        }

        private static void RecalculateMass(ShuttleCargoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.CargoMassUsage = snapshot.LoadedMassKg +
                snapshot.QueuedMassKg +
                snapshot.RefrigeratedMassKg +
                snapshot.MedicalBayPatientMassKg +
                snapshot.ExternalRuntimeMassKg;
            snapshot.TotalPlannedMassKg = snapshot.CargoMassUsage;
            snapshot.MassUsage = snapshot.StructuralMass + snapshot.CargoMassUsage;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;
using Verse.AI;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.MechCharging
{
    internal static class ShuttleMechChargerUtility
    {
        internal static bool HasUsableVanillaMechCharger(Pawn mech)
        {
            if (!ModsConfig.BiotechActive ||
                mech == null ||
                !mech.Spawned ||
                mech.Map == null ||
                mech.needs == null ||
                mech.needs.energy == null)
            {
                return false;
            }

            Thing charger = JobGiver_GetEnergy_Charger.GetClosestCharger(mech, mech, false);
            return charger != null &&
                mech.CanReach(charger, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn) &&
                mech.CanReserve(charger, 1, -1, null, false);
        }

        internal static Thing FindBestAvailableShuttleMechCharger(Pawn mech)
        {
            if (!ModsConfig.BiotechActive ||
                mech == null ||
                !mech.Spawned ||
                mech.Map == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistanceSquared = float.MaxValue;
            System.Collections.Generic.List<Thing> shuttleHosts =
                ShuttleHostCandidateUtility.GetModularShuttleHosts(mech.Map);

            for (int i = 0; i < shuttleHosts.Count; i++)
            {
                Thing thing = shuttleHosts[i];
                if (!IsCandidateShuttleHost(thing) ||
                    thing.IsForbidden(mech))
                {
                    continue;
                }

                string failReason;
                if (!MechChargerAdmissionValidator.CanUseShuttleMechChargerForCharging(
                    mech,
                    thing,
                    out failReason,
                    mech))
                {
                    continue;
                }

                float distanceSquared = (thing.Position - mech.Position).LengthHorizontalSquared;
                if (best == null || distanceSquared < bestDistanceSquared)
                {
                    best = thing;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return best;
        }

        private static bool IsCandidateShuttleHost(Thing thing)
        {
            if (thing == null || !thing.Spawned || thing.Destroyed)
            {
                return false;
            }

            ThingWithComps shuttleWithComps = thing as ThingWithComps;
            return shuttleWithComps != null &&
                shuttleWithComps.TryGetComp<CompModularShuttleCore>() != null;
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.MechCharging
{
    /// <summary>
    /// Read-only validator for shuttle mech charger entry. It does not reserve jobs,
    /// move pawns, or mutate holder state.
    /// </summary>
    internal static class MechChargerAdmissionValidator
    {
        internal const string ShuttleMechChargeJobDefName = "CT_ShuttleMechCharge";

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

        internal static bool TryGetMechChargerOccupancy(
            Thing shuttleHost,
            out CompShuttleMechChargerOccupancy occupancy)
        {
            occupancy = null;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                return false;
            }

            occupancy = shuttleWithComps.TryGetComp<CompShuttleMechChargerOccupancy>();
            return occupancy != null;
        }

        internal static bool TryGetMechChargerProfile(
            Thing shuttleHost,
            out ShuttleProfile profile,
            out MechChargerProfile mechCharger)
        {
            profile = null;
            mechCharger = null;

            ShuttleController controller;
            if (!TryGetShuttleController(shuttleHost, out controller))
            {
                return false;
            }

            profile = controller.GetProfileForRead();
            mechCharger = profile != null ? profile.MechCharger : null;
            return mechCharger != null && mechCharger.HasMechCharger;
        }

        internal static bool IsMechChargerPowered(Thing shuttleHost)
        {
            ShuttleController controller;
            if (!TryGetShuttleController(shuttleHost, out controller))
            {
                return false;
            }

            ShuttlePowerRuntimeSnapshot powerSnapshot = controller.BuildPowerRuntimeSnapshot();
            return powerSnapshot != null && powerSnapshot.InternalBusPowered;
        }

        internal static bool TryGetPoweredMechChargerProfile(
            Thing shuttleHost,
            out ShuttleProfile profile,
            out MechChargerProfile mechCharger)
        {
            if (!TryGetMechChargerProfile(shuttleHost, out profile, out mechCharger))
            {
                return false;
            }

            return IsMechChargerPowered(shuttleHost);
        }

        internal static bool CanUseShuttleMechChargerForCharging(
            Pawn mech,
            Thing shuttleHost,
            out string failReason)
        {
            return CanUseShuttleMechChargerForCharging(mech, shuttleHost, out failReason, null);
        }

        internal static bool CanUseShuttleMechChargerForCharging(
            Pawn mech,
            Thing shuttleHost,
            out string failReason,
            Pawn ignoredJobPawn)
        {
            failReason = null;
            if (!IsValidChargingMech(mech, out failReason))
            {
                return false;
            }

            if (!IsValidShuttleHostForCharging(mech, shuttleHost, out failReason))
            {
                return false;
            }

            if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                mech,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (!ShuttleOnboardDeviceUsePolicy.AllowsMechFacilityUse(
                mech,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (ShuttleMechChargerUtility.HasUsableVanillaMechCharger(mech))
            {
                failReason = "CT_Shuttle_MechCharger_NormalChargerAvailable".Translate().ToString();
                return false;
            }

            ShuttleProfile profile;
            MechChargerProfile mechCharger;
            if (!TryGetMechChargerProfile(shuttleHost, out profile, out mechCharger))
            {
                failReason = "CT_Shuttle_MechCharger_NoCharger".Translate().ToString();
                return false;
            }

            if (!IsMechChargerPowered(shuttleHost))
            {
                failReason = "CT_Shuttle_MechCharger_Unpowered".Translate().ToString();
                return false;
            }

            if (mechCharger.MechChargeSlots <= 0)
            {
                failReason = "CT_Shuttle_MechCharger_NoSlots".Translate().ToString();
                return false;
            }

            CompShuttleMechChargerOccupancy occupancy;
            if (!TryGetMechChargerOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_MechCharger_NoCharger".Translate().ToString();
                return false;
            }

            if (occupancy.ContainsChargingMech(mech))
            {
                failReason = "CT_Shuttle_MechCharger_AlreadyCharging".Translate().ToString();
                return false;
            }

            if (CountCurrentShuttleMechChargeUsers(shuttleHost, mech, ignoredJobPawn) >= mechCharger.MechChargeSlots)
            {
                failReason = "CT_Shuttle_MechCharger_Full".Translate().ToString();
                return false;
            }

            if (!mech.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_MechCharger_Unreachable".Translate().ToString();
                return false;
            }

            if (!mech.CanReserve(shuttleHost, Max(1, mechCharger.MechChargeSlots), 1, null, false))
            {
                failReason = "CT_Shuttle_MechCharger_Reserved".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static int CountCurrentShuttleMechChargeUsers(Thing shuttleHost)
        {
            return CountCurrentShuttleMechChargeUsers(shuttleHost, null, null);
        }

        internal static int CountCurrentShuttleMechChargeUsers(
            Thing shuttleHost,
            Pawn ignoredMech)
        {
            return CountCurrentShuttleMechChargeUsers(shuttleHost, ignoredMech, null);
        }

        internal static int CountCurrentShuttleMechChargeUsers(
            Thing shuttleHost,
            Pawn ignoredMech,
            Pawn ignoredJobPawn)
        {
            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null)
            {
                return 0;
            }

            int count = 0;
            CompShuttleMechChargerOccupancy occupancy;
            if (TryGetMechChargerOccupancy(shuttleHost, out occupancy))
            {
                count += occupancy.ChargingMechCount;
                if (ignoredMech != null && occupancy.ContainsChargingMech(ignoredMech))
                {
                    count--;
                }

                count += occupancy.CountPendingChargingReservations(ignoredMech);
            }

            JobDef chargeJobDef = DefDatabase<JobDef>.GetNamedSilentFail(ShuttleMechChargeJobDefName);
            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns != null
                ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                : null;
            if (chargeJobDef == null || pawns == null)
            {
                return count < 0 ? 0 : count;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == ignoredMech || pawn == ignoredJobPawn)
                {
                    continue;
                }

                Job job = pawn.CurJob;
                if (job == null ||
                    job.def != chargeJobDef ||
                    !TargetReferences(job.GetTarget(TargetIndex.A), shuttleHost))
                {
                    continue;
                }

                if (occupancy != null && occupancy.HasChargingReservationForMechID(pawn.thingIDNumber))
                {
                    continue;
                }

                count++;
            }

            return count < 0 ? 0 : count;
        }

        private static bool IsValidChargingMech(Pawn mech, out string failReason)
        {
            failReason = null;
            if (!ModsConfig.BiotechActive)
            {
                failReason = "CT_Shuttle_MechCharger_BiotechRequired".Translate().ToString();
                return false;
            }

            if (mech == null || mech.Destroyed || mech.Dead)
            {
                failReason = "CT_Shuttle_MechCharger_InvalidMech".Translate().ToString();
                return false;
            }

            if (mech.RaceProps == null || !mech.RaceProps.IsMechanoid || !mech.IsColonyMech)
            {
                failReason = "CT_Shuttle_MechCharger_InvalidMech".Translate().ToString();
                return false;
            }

            if (!ShuttleMechChargeNeedUtility.HasChargeNeed(mech))
            {
                failReason = "CT_Shuttle_MechCharger_NoEnergyNeed".Translate().ToString();
                return false;
            }

            if (!mech.Spawned || mech.Map == null)
            {
                failReason = "CT_Shuttle_MechCharger_MechAlreadyHeld".Translate().ToString();
                return false;
            }

            if (mech.Downed || mech.Drafted || mech.MentalState != null)
            {
                failReason = "CT_Shuttle_MechCharger_MechUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool IsValidShuttleHostForCharging(
            Pawn mech,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_MechCharger_NoCharger".Translate().ToString();
                return false;
            }

            if (mech == null || mech.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_MechCharger_Unreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool TargetReferences(LocalTargetInfo target, Thing thing)
        {
            return thing != null && target.IsValid && target.Thing == thing;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}

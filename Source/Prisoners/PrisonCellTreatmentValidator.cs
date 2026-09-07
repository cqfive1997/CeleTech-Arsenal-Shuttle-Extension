using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Read-only rules for tending held shuttle prisoners. This validator never
    /// consumes medicine, edits health, touches holders, or assigns jobs.
    /// </summary>
    internal sealed class PrisonCellTreatmentValidator
    {
        internal const string TendPrisonerJobDefName =
            "CT_Shuttle_TendPrisonerInShuttleCell";

        private readonly ShuttleLoadedCargoMedicineSupplySource medicineSource =
            new ShuttleLoadedCargoMedicineSupplySource();

        internal bool CanAssignTendJob(
            ThingWithComps shuttleHost,
            Pawn doctor,
            Pawn prisoner,
            out string failReason)
        {
            if (!this.CanTendHeldPrisoner(shuttleHost, doctor, prisoner, out failReason))
            {
                return false;
            }

            if (!doctor.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                return false;
            }

            if (!doctor.CanReserve(shuttleHost, 1, 1, null, false))
            {
                failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanTendHeldPrisoner(
            ThingWithComps shuttleHost,
            Pawn doctor,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.CanUseShuttle(shuttleHost, out failReason))
            {
                return false;
            }

            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.TryGetPrisonCellOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (!this.CanUsePrisonerForTending(prisoner, out failReason))
            {
                return false;
            }

            if (!this.CanUseDoctor(shuttleHost, doctor, out failReason))
            {
                return false;
            }

            return true;
        }

        internal bool TryGetPrisonCellOccupancy(
            ThingWithComps shuttleHost,
            out CompShuttlePrisonCellOccupancy occupancy)
        {
            occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            return occupancy != null;
        }

        internal bool CanUseDoctor(
            ThingWithComps shuttleHost,
            Pawn doctor,
            out string failReason)
        {
            failReason = null;
            if (doctor == null)
            {
                failReason = "CT_Shuttle_PrisonCell_NoDoctor".Translate().ToString();
                return false;
            }

            if (doctor.Destroyed ||
                doctor.Dead ||
                !doctor.Spawned ||
                doctor.Map == null ||
                doctor.Downed ||
                doctor.Drafted ||
                doctor.MentalState != null ||
                doctor.RaceProps == null ||
                !doctor.RaceProps.Humanlike ||
                doctor.Faction != Faction.OfPlayer ||
                !doctor.IsColonist ||
                doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) ||
                doctor.WorkTagIsDisabled(WorkTags.Caring))
            {
                failReason = "CT_Shuttle_PrisonCell_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (!CanUseCapacity(doctor, PawnCapacityDefOf.Moving) ||
                !CanUseCapacity(doctor, PawnCapacityDefOf.Manipulation))
            {
                failReason = "CT_Shuttle_PrisonCell_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                doctor.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanUsePrisonerForTending(Pawn prisoner, out string failReason)
        {
            failReason = null;
            if (prisoner == null ||
                prisoner.Destroyed ||
                prisoner.Dead ||
                prisoner.RaceProps == null ||
                !prisoner.RaceProps.Humanlike ||
                prisoner.health == null ||
                prisoner.health.hediffSet == null)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (!PrisonerNeedsTending(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_NoTendableInjury".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool HasLoadedMedicineAvailable(ThingWithComps shuttleHost, Pawn prisoner)
        {
            string failReason;
            return this.medicineSource != null &&
                this.medicineSource.CanProvideLoadedMedicineForPatient(
                    shuttleHost,
                    prisoner,
                    out failReason,
                    allowMedicineWhenPatientSettingsMissing: true);
        }

        internal static bool PrisonerNeedsTending(Pawn prisoner)
        {
            if (prisoner == null ||
                prisoner.health == null ||
                prisoner.health.hediffSet == null)
            {
                return false;
            }

            return prisoner.health.HasHediffsNeedingTend(false) &&
                CountUntendedTendableHediffs(prisoner) > 0;
        }

        internal static int CountUntendedTendableHediffs(Pawn prisoner)
        {
            if (prisoner == null ||
                prisoner.health == null ||
                prisoner.health.hediffSet == null ||
                prisoner.health.hediffSet.hediffs == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < prisoner.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = prisoner.health.hediffSet.hediffs[i];
                if (hediff == null || !hediff.TendableNow(false))
                {
                    continue;
                }

                HediffComp_TendDuration tendComp = GetTendDurationComp(hediff);
                if (tendComp == null || !tendComp.IsTended)
                {
                    count++;
                }
            }

            return count;
        }

        private bool CanUseShuttle(ThingWithComps shuttleHost, out string failReason)
        {
            failReason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            PrisonCellProfile prisonCell;
            if (!this.TryGetPrisonCellProfile(shuttleHost, out prisonCell) ||
                prisonCell == null ||
                !prisonCell.HasPrisonCell ||
                prisonCell.PrisonerSlots <= 0)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (!prisonCell.SupportsTending)
            {
                failReason = "CT_Shuttle_PrisonCell_TendingUnsupported".Translate().ToString();
                return false;
            }

            CompShuttleHolderLaunchTransferState transferState =
                shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (transferState != null && transferState.HasAnyActiveOrRecoveryTransfer)
            {
                failReason = "CT_Shuttle_PrisonCell_TransferBusy".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool TryGetPrisonCellProfile(
            ThingWithComps shuttleHost,
            out PrisonCellProfile prisonCell)
        {
            prisonCell = null;
            CompModularShuttleCore core = shuttleHost != null
                ? shuttleHost.TryGetComp<CompModularShuttleCore>()
                : null;
            ShuttleProfile profile = core != null && core.Controller != null
                ? core.Controller.GetProfileForRead()
                : null;
            prisonCell = profile != null ? profile.PrisonCell : null;
            return prisonCell != null;
        }

        private static HediffComp_TendDuration GetTendDurationComp(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null ? withComps.GetComp<HediffComp_TendDuration>() : null;
        }

        private static bool CanUseCapacity(Pawn pawn, PawnCapacityDef capacityDef)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                capacityDef != null &&
                pawn.health.capacities.CapableOf(capacityDef);
        }
    }
}

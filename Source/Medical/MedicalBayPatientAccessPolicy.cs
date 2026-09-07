using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Defines who may receive Medical Bay care and who may depart inside its holder.
    /// It is read-only and deliberately separate from holder transfer mechanics.
    /// </summary>
    internal static class MedicalBayPatientAccessPolicy
    {
        internal static bool CanReceiveCare(Pawn patient, out string failReason)
        {
            failReason = null;
            if (patient == null ||
                patient.Destroyed ||
                patient.Dead ||
                patient.RaceProps == null ||
                !patient.RaceProps.Humanlike)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (patient.IsPrisonerOfColony || patient.HostileTo(Faction.OfPlayer))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientNotFriendly".Translate().ToString();
                return false;
            }

            if (patient.Faction == Faction.OfPlayer ||
                (patient.guest != null && patient.guest.HostFaction == Faction.OfPlayer) ||
                patient.IsQuestLodger())
            {
                return true;
            }

            if (patient.Faction != null && !patient.Faction.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            failReason = "CT_Shuttle_MedicalBay_PatientNotFriendly".Translate().ToString();
            return false;
        }

        internal static bool MayDepartWithPlayer(Pawn patient)
        {
            if (patient == null ||
                patient.Destroyed ||
                patient.Dead ||
                patient.Faction != Faction.OfPlayer ||
                patient.IsPrisonerOfColony ||
                patient.IsQuestLodger())
            {
                return false;
            }

            return patient.IsSlaveOfColony ||
                patient.guest == null ||
                patient.guest.HostFaction == null;
        }

        internal static bool TryFindNonDepartingPatient(
            IReadOnlyList<Pawn> patients,
            out Pawn nonDepartingPatient)
        {
            nonDepartingPatient = null;
            if (patients == null)
            {
                return false;
            }

            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient != null && !MayDepartWithPlayer(patient))
                {
                    nonDepartingPatient = patient;
                    return true;
                }
            }

            return false;
        }
    }
}

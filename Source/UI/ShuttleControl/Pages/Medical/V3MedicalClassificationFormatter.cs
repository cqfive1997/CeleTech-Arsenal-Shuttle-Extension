using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalClassificationFormatter
    {
        private const float SevereBleedRateThreshold = 0.30f;

        internal V3MedicalPatientKind GetPatientKind(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps == null)
            {
                return V3MedicalPatientKind.Human;
            }

            if (pawn.RaceProps.IsMechanoid)
            {
                return V3MedicalPatientKind.Mechanoid;
            }

            if (pawn.RaceProps.Animal)
            {
                return V3MedicalPatientKind.Animal;
            }

            return V3MedicalPatientKind.Human;
        }

        internal string GetKindKey(Pawn pawn, V3MedicalPatientKind kind)
        {
            if (kind == V3MedicalPatientKind.Mechanoid)
            {
                return "Mechanoid";
            }

            if (kind == V3MedicalPatientKind.Animal)
            {
                return "Animal";
            }

            if (pawn != null && pawn.IsPrisonerOfColony)
            {
                return "Prisoner";
            }

            if (pawn != null && pawn.IsSlaveOfColony)
            {
                return "Slave";
            }

            if (pawn != null && pawn.IsColonist)
            {
                return "Colonist";
            }

            return "Human";
        }

        internal string GetKindFallbackLabel(
            string kindKey,
            V3MedicalPatientKind kind)
        {
            if (kindKey == "Prisoner")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Prisoner");
            }

            if (kindKey == "Slave")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Slave");
            }

            if (kindKey == "Colonist")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Colonist");
            }

            if (kind == V3MedicalPatientKind.Mechanoid)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Mechanoid");
            }

            if (kind == V3MedicalPatientKind.Animal)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Animal");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Crew_Human");
        }

        internal string GetCandidateKindFallbackLabel(string kindKey)
        {
            if (kindKey == "Prisoner")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Prisoner");
            }

            if (kindKey == "Slave")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Slave");
            }

            if (kindKey == "Colonist")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Colonist");
            }

            if (kindKey == "Mechanoid")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Mechanoid");
            }

            if (kindKey == "Animal")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Animal");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Crew_Human");
        }

        internal string GetStateKey(
            ShuttleMedicalPatientReadModel source,
            V3MedicalPatientKind kind)
        {
            if (kind == V3MedicalPatientKind.Mechanoid)
            {
                return source != null && source.NeedsTend ? "Repairing" : "Standby";
            }

            if (source != null && (source.NeedsTend || source.HasUntendedInjury))
            {
                return "TreatmentPending";
            }

            if (source != null && source.IsDowned)
            {
                return "Resting";
            }

            return "Recovering";
        }

        internal string GetStateFallbackLabel(string stateKey)
        {
            if (stateKey == "Repairing")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_State_Repairing");
            }

            if (stateKey == "TreatmentPending")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_State_TreatmentPending");
            }

            if (stateKey == "Resting")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Activity_Resting");
            }

            if (stateKey == "Standby")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Activity_Standby");
            }

            return ShuttleUIText.Tr("CT_ShuttleCrew_Recovering");
        }

        internal string GetTriageKey(ShuttleMedicalPatientReadModel source)
        {
            if (source == null)
            {
                return "Stable";
            }

            if (source.HasMedicalEmergency &&
                (source.BleedRateTotal >= SevereBleedRateThreshold ||
                    (source.ConsciousnessPct >= 0f && source.ConsciousnessPct < 0.25f) ||
                    source.PainPct >= 0.85f ||
                    (source.IsDowned && source.NeedsTend)))
            {
                return "Critical";
            }

            if (source.HasMedicalEmergency ||
                source.BleedRateTotal >= 0.15f ||
                source.UntendedHediffCount >= 2)
            {
                return "Severe";
            }

            if (source.NeedsTend ||
                source.IsBleeding ||
                source.HasInfectionLikeHediff ||
                source.PainPct >= 0.40f)
            {
                return "Moderate";
            }

            return "Stable";
        }

        internal string GetTriageFallbackLabel(string triageKey)
        {
            if (triageKey == "Critical")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Hull_Status_Critical");
            }

            if (triageKey == "Severe")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_Triage_Severe");
            }

            if (triageKey == "Moderate")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_Triage_Observation");
            }

            return ShuttleUIText.Tr("CT_Shuttle_MedicalBay_HealthStable");
        }

        internal string GetAdmissionModeFallbackLabel(string modeKey)
        {
            if (modeKey == "SelfEnter")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_SelfEnter");
            }

            if (modeKey == "NeedsCarry")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_NeedsCarry");
            }

            if (modeKey == "NotReceivable")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_NotReceivable");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Medical_Unavailable");
        }

    }
}

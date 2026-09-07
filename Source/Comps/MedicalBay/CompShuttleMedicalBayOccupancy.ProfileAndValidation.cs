using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        private bool TryGetMedicalBayProfile(out MedicalBayProfile medicalBay)
        {
            medicalBay = null;
            ThingWithComps host = this.parent as ThingWithComps;
            CompModularShuttleCore core = host != null
                ? host.TryGetComp<CompModularShuttleCore>()
                : null;
            ShuttleProfile profile = core != null && core.Controller != null
                ? core.Controller.GetProfileForRead()
                : null;
            medicalBay = profile != null ? profile.MedicalBay : null;
            return medicalBay != null;
        }

        private int GetMedicalPatientSlots()
        {
            MedicalBayProfile medicalBay;
            return this.TryGetMedicalBayProfile(out medicalBay) && medicalBay.HasMedicalBay
                ? medicalBay.MedicalPatientSlots
                : 0;
        }

        private bool PatientHasUntendedTendableHediff(Pawn patient)
        {
            if (patient == null ||
                patient.health == null ||
                patient.health.hediffSet == null ||
                patient.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < patient.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = patient.health.hediffSet.hediffs[i];
                if (hediff != null && this.IsHediffTendable(hediff) && !this.IsHediffTended(hediff))
                {
                    return true;
                }
            }

            return false;
        }

        private bool PatientHasBleeding(Pawn patient)
        {
            if (patient == null || patient.health == null || patient.health.hediffSet == null)
            {
                return false;
            }

            if (patient.health.hediffSet.BleedRateTotal > 0f)
            {
                return true;
            }

            if (patient.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < patient.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = patient.health.hediffSet.hediffs[i];
                if (hediff != null && hediff.Bleeding)
                {
                    return true;
                }
            }

            return false;
        }

        private bool PatientHasInfectionLikeHediff(Pawn patient)
        {
            if (patient == null ||
                patient.health == null ||
                patient.health.hediffSet == null ||
                patient.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < patient.health.hediffSet.hediffs.Count; i++)
            {
                HediffWithComps hediff = patient.health.hediffSet.hediffs[i] as HediffWithComps;
                if (hediff != null && hediff.GetComp<HediffComp_Immunizable>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool PatientHasMajorInjury(Pawn patient)
        {
            if (patient == null ||
                patient.health == null ||
                patient.health.hediffSet == null ||
                patient.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < patient.health.hediffSet.hediffs.Count; i++)
            {
                Hediff_Injury injury = patient.health.hediffSet.hediffs[i] as Hediff_Injury;
                if (injury != null && injury.Severity >= MajorInjuryDischargeSeverity)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetCapacityLevel(Pawn patient, PawnCapacityDef capacityDef)
        {
            if (patient == null || patient.health == null || patient.health.capacities == null || capacityDef == null)
            {
                return 1f;
            }

            return patient.health.capacities.GetLevel(capacityDef);
        }

        private float GetPainTotal(Pawn patient)
        {
            if (patient == null || patient.health == null || patient.health.hediffSet == null)
            {
                return 0f;
            }

            return patient.health.hediffSet.PainTotal;
        }

        private bool IsHediffTendable(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            return hediff.TendableNow(false);
        }

        private bool IsHediffTended(Hediff hediff)
        {
            HediffComp_TendDuration tendComp = this.GetTendDurationComp(hediff);
            return tendComp != null && tendComp.IsTended;
        }

        private HediffComp_TendDuration GetTendDurationComp(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null ? withComps.GetComp<HediffComp_TendDuration>() : null;
        }
    }
}

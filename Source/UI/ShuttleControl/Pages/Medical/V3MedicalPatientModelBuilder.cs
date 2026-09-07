using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPatientModelBuilder
    {
        private readonly V3MedicalClassificationFormatter classificationFormatter;
        private readonly V3MedicalReadModelFormatter formatter;
        private readonly V3MedicalVitalModelBuilder vitalBuilder;
        private readonly V3MedicalPatientDetailModelBuilder detailBuilder;
        private readonly V3MedicalProcedureModelBuilder procedureBuilder;

        internal V3MedicalPatientModelBuilder(
            V3MedicalClassificationFormatter classificationFormatter,
            V3MedicalReadModelFormatter formatter,
            V3MedicalVitalModelBuilder vitalBuilder,
            V3MedicalPatientDetailModelBuilder detailBuilder,
            V3MedicalProcedureModelBuilder procedureBuilder)
        {
            this.classificationFormatter = classificationFormatter;
            this.formatter = formatter;
            this.vitalBuilder = vitalBuilder;
            this.detailBuilder = detailBuilder;
            this.procedureBuilder = procedureBuilder;
        }

        internal void BuildPatients(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            this.BuildPatients(pageModel, medicalBay, pawnPresenceSnapshot, null);
        }

        internal void BuildPatients(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            if (pageModel == null || medicalBay == null || medicalBay.Patients == null)
            {
                return;
            }

            for (int i = 0; i < medicalBay.Patients.Count; i++)
            {
                ShuttleMedicalPatientReadModel source = medicalBay.Patients[i];
                if (source == null)
                {
                    continue;
                }

                V3MedicalPatientCardModel patient =
                    this.BuildPatient(
                        source,
                        pawnPresenceSnapshot,
                        pawnDynamicStatusSnapshot);
                pageModel.Patients.Add(patient);
                pageModel.PendingTreatmentCount += Mathf.Max(
                    0,
                    patient.PendingTreatmentCount);
                if (patient.TriageKey == "Severe" || patient.TriageKey == "Critical")
                {
                    pageModel.CriticalPatientCount++;
                }
            }
        }

        internal void ApplyActiveProcedureToPatients(
            V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null || pageModel.Patients == null)
            {
                return;
            }

            for (int i = 0; i < pageModel.Patients.Count; i++)
            {
                this.ApplyActiveProcedureToPatient(pageModel, pageModel.Patients[i]);
            }
        }

        private void ApplyActiveProcedureToPatient(
            V3MedicalPageReadModel pageModel,
            V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return;
            }

            patient.TreatmentBlockedByActiveProcedure = pageModel.HasActiveProcedure;
            if (!pageModel.HasActiveProcedure ||
                pageModel.ActiveProcedurePatientThingID <= 0 ||
                patient.PatientThingID != pageModel.ActiveProcedurePatientThingID)
            {
                return;
            }

            patient.IsActiveProcedurePatient = true;
            patient.ActiveProcedureStatusLabel = pageModel.ActiveProcedureStatusLabel;
            patient.ActiveProcedureTooltip = pageModel.ActiveProcedureTooltip;
            V3MedicalDetailLineModel line =
                this.procedureBuilder.BuildActiveProcedureDetail(pageModel);
            if (line != null)
            {
                patient.Details.Insert(0, line);
            }
        }

        private V3MedicalPatientCardModel BuildPatient(
            ShuttleMedicalPatientReadModel source,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            ShuttlePawnPresenceRecord presence = this.FindPresence(
                pawnPresenceSnapshot,
                source.PawnThingID,
                ShuttlePawnPresenceKind.MedicalPatient);
            ShuttlePawnDynamicStatusRecord dynamicStatus =
                this.FindDynamicStatus(
                    pawnDynamicStatusSnapshot,
                    source.PawnThingID);
            Thing displayThing = presence != null && presence.DisplayThing != null
                ? presence.DisplayThing
                : source.DisplayThing;
            Pawn pawn = displayThing as Pawn;
            V3MedicalPatientCardModel patient = new V3MedicalPatientCardModel();
            patient.PatientThingID = source.PawnThingID;
            patient.PatientKey = source.PawnThingID > 0
                ? source.PawnThingID.ToString()
                : (!string.IsNullOrEmpty(source.PawnLabel) ? source.PawnLabel : "patient");
            patient.Label = presence != null && !string.IsNullOrEmpty(presence.Label)
                ? presence.Label
                : (!string.IsNullOrEmpty(source.PawnLabel)
                    ? source.PawnLabel
                    : ShuttleUIText.Tr("CT_Shuttle_Medical_UnknownPatient"));
            patient.DisplayThing = displayThing;
            this.ApplyPatientKindAndState(patient, source, pawn, presence);
            this.ApplyPatientTreatmentState(patient, source, dynamicStatus);
            this.ApplyPatientVitalsSource(patient, source, pawn, dynamicStatus);
            this.CopySurgeryOptions(source, patient);
            this.vitalBuilder.BuildPatientVitals(patient);
            this.detailBuilder.BuildDetails(patient, source, pawn);
            return patient;
        }

        private ShuttlePawnPresenceRecord FindPresence(
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            int pawnThingID,
            ShuttlePawnPresenceKind kind)
        {
            return pawnPresenceSnapshot != null
                ? pawnPresenceSnapshot.FindByPawnThingIDAndKind(pawnThingID, kind)
                : null;
        }

        private ShuttlePawnDynamicStatusRecord FindDynamicStatus(
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot,
            int pawnThingID)
        {
            return pawnDynamicStatusSnapshot != null
                ? pawnDynamicStatusSnapshot.FindByPawnThingID(pawnThingID)
                : null;
        }

        private void ApplyPatientKindAndState(
            V3MedicalPatientCardModel patient,
            ShuttleMedicalPatientReadModel source,
            Pawn pawn,
            ShuttlePawnPresenceRecord presence)
        {
            patient.Kind = this.GetPresencePatientKind(presence, pawn);
            patient.KindKey = this.GetPresenceKindKey(
                presence,
                pawn,
                patient.Kind);
            patient.KindFallbackLabel =
                this.classificationFormatter.GetKindFallbackLabel(
                    patient.KindKey,
                    patient.Kind);
            patient.StateKey = this.classificationFormatter.GetStateKey(source, patient.Kind);
            patient.StateFallbackLabel =
                this.classificationFormatter.GetStateFallbackLabel(patient.StateKey);
            patient.TriageKey = this.classificationFormatter.GetTriageKey(source);
            patient.TriageFallbackLabel =
                this.classificationFormatter.GetTriageFallbackLabel(patient.TriageKey);
            patient.LocationText =
                ShuttleUIText.Tr("CT_Shuttle_Crew_Status_MedicalBay");
            patient.MedicineNeedText = !string.IsNullOrEmpty(source.MedicineNeedLabel)
                ? source.MedicineNeedLabel
                : ShuttleUIText.Tr("CT_Shuttle_Medical_MedicineNeedUnavailable");
        }

        private V3MedicalPatientKind GetPresencePatientKind(
            ShuttlePawnPresenceRecord presence,
            Pawn pawn)
        {
            if (presence != null)
            {
                if (presence.IsMech)
                {
                    return V3MedicalPatientKind.Mechanoid;
                }

                if (presence.IsAnimal)
                {
                    return V3MedicalPatientKind.Animal;
                }

                if (presence.IsHumanlike)
                {
                    return V3MedicalPatientKind.Human;
                }
            }

            return this.classificationFormatter.GetPatientKind(pawn);
        }

        private string GetPresenceKindKey(
            ShuttlePawnPresenceRecord presence,
            Pawn pawn,
            V3MedicalPatientKind kind)
        {
            if (presence != null)
            {
                if (presence.IsMech)
                {
                    return "Mechanoid";
                }

                if (presence.IsAnimal)
                {
                    return "Animal";
                }

                if (presence.IsPrisoner)
                {
                    return "Prisoner";
                }

                if (presence.IsSlave)
                {
                    return "Slave";
                }

                if (presence.IsColonist)
                {
                    return "Colonist";
                }

                if (presence.IsHumanlike)
                {
                    return "Human";
                }
            }

            return this.classificationFormatter.GetKindKey(pawn, kind);
        }

        private void ApplyPatientTreatmentState(
            V3MedicalPatientCardModel patient,
            ShuttleMedicalPatientReadModel source,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            patient.IsActiveProcedurePatient = source.IsActiveProcedurePatient;
            patient.ActiveProcedureStatusLabel = source.ActiveProcedureStatusLabel;
            patient.ActiveProcedureTooltip = source.ActiveProcedureTooltip;
            patient.IsDowned = dynamicStatus != null
                ? dynamicStatus.IsDowned
                : source.IsDowned;
            patient.NeedsTreatment = source.NeedsTend || source.HasUntendedInjury;
            patient.PendingTreatmentCount = Mathf.Max(0, source.UntendedHediffCount);
            if (patient.PendingTreatmentCount <= 0 && source.NeedsTend)
            {
                patient.PendingTreatmentCount = 1;
            }
            else if (patient.PendingTreatmentCount <= 0 &&
                     (source.IsBleeding ||
                      source.HasInfectionLikeHediff ||
                      source.HasMedicalEmergency))
            {
                patient.PendingTreatmentCount = 1;
            }
        }

        private void ApplyPatientVitalsSource(
            V3MedicalPatientCardModel patient,
            ShuttleMedicalPatientReadModel source,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            patient.PainPct = this.GetKnownPct(
                dynamicStatus != null ? dynamicStatus.PainPct : -1f,
                source.PainPct);
            patient.ConsciousnessPct = this.GetKnownPct(
                dynamicStatus != null ? dynamicStatus.ConsciousnessPct : -1f,
                source.ConsciousnessPct);
            patient.BleedSeverityPct = this.GetKnownPct(
                dynamicStatus != null ? dynamicStatus.BleedSeverityPct : -1f,
                source.IsBleeding
                    ? Mathf.Clamp01(Mathf.Max(
                        source.BleedRateTotal,
                        source.BleedingHediffCount * 0.20f))
                    : 0f);
            patient.MovingPct = this.GetKnownPct(
                dynamicStatus != null ? dynamicStatus.MovingPct : -1f,
                this.formatter.GetCapacityPct(pawn, PawnCapacityDefOf.Moving));
            patient.FoodPct = this.GetKnownPct(
                dynamicStatus != null ? dynamicStatus.FoodPct : -1f,
                source.FoodPct);
            this.ApplyPatientMechVitals(patient, source, dynamicStatus);
        }

        private void ApplyPatientMechVitals(
            V3MedicalPatientCardModel patient,
            ShuttleMedicalPatientReadModel source,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            patient.HasMechDurability = dynamicStatus != null &&
                dynamicStatus.HasMechDurability
                    ? true
                    : source.HasMechDurability;
            patient.DurabilityPct = patient.HasMechDurability
                ? this.GetKnownPct(
                    dynamicStatus != null ? dynamicStatus.DurabilityPct : -1f,
                    source.DurabilityPct)
                : -1f;
            patient.DamagePct = patient.HasMechDurability
                ? this.GetKnownPct(
                    dynamicStatus != null ? dynamicStatus.DamagePct : -1f,
                    source.DamagePct)
                : -1f;
            patient.HasMechEnergy = dynamicStatus != null &&
                dynamicStatus.HasMechEnergy
                    ? true
                    : source.HasMechEnergy;
            patient.EnergyPct = patient.HasMechEnergy
                ? this.GetKnownPct(
                    dynamicStatus != null ? dynamicStatus.EnergyPct : -1f,
                    source.EnergyPct)
                : -1f;
        }

        private float GetKnownPct(float preferred, float fallback)
        {
            return preferred >= 0f ? Mathf.Clamp01(preferred) : fallback;
        }

        private void CopySurgeryOptions(
            ShuttleMedicalPatientReadModel source,
            V3MedicalPatientCardModel patient)
        {
            if (source == null || patient == null || source.SurgeryOptions == null)
            {
                return;
            }

            for (int i = 0; i < source.SurgeryOptions.Count; i++)
            {
                ShuttleMedicalSurgeryOptionReadModel option = source.SurgeryOptions[i];
                if (option == null)
                {
                    continue;
                }

                V3MedicalSurgeryOptionModel target = new V3MedicalSurgeryOptionModel();
                target.RecipeDefName = option.RecipeDefName;
                target.Label = option.Label;
                target.Description = option.Description;
                target.BodyPartIndex = option.BodyPartIndex;
                target.BodyPartLabel = option.BodyPartLabel;
                target.CanSchedule = option.CanSchedule;
                target.DisabledReason = option.DisabledReason;
                target.WorkTicks = option.WorkTicks;
                target.RequiredMaterialsSummary = option.RequiredMaterialsSummary;
                target.MissingMaterialsSummary = option.MissingMaterialsSummary;
                target.HasMissingMaterials = option.HasMissingMaterials;
                patient.SurgeryOptions.Add(target);
            }
        }

    }
}

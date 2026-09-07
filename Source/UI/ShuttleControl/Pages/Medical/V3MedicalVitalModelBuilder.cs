using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalVitalModelBuilder
    {
        private readonly V3MedicalReadModelFormatter formatter;

        internal V3MedicalVitalModelBuilder(
            V3MedicalReadModelFormatter formatter)
        {
            this.formatter = formatter;
        }

        internal void BuildPatientVitals(V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return;
            }

            if (patient.Kind == V3MedicalPatientKind.Mechanoid)
            {
                this.BuildMechVitals(patient);
                return;
            }

            this.AddVital(patient, ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Pain"), patient.PainPct, "Moderate");
            if (patient.Kind == V3MedicalPatientKind.Human)
            {
                this.AddVital(
                    patient,
                    ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Consciousness"),
                    patient.ConsciousnessPct,
                    "Stable");
            }

            this.AddVital(patient, ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Bleed"), patient.BleedSeverityPct, "Severe");
            this.AddVital(patient, ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Move"), patient.MovingPct, "Stable");
            this.AddVital(
                patient,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Hunger"),
                patient.FoodPct,
                this.formatter.GetPositiveVitalSeverity(patient.FoodPct));
        }

        internal void BuildDoctorVitals(
            V3MedicalOccupantCardModel occupant,
            Pawn doctor)
        {
            this.BuildDoctorVitals(occupant, doctor, null);
        }

        internal void BuildDoctorVitals(
            V3MedicalOccupantCardModel occupant,
            Pawn doctor,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (occupant == null || doctor == null || doctor.needs == null)
            {
                return;
            }

            if (doctor.needs.food != null)
            {
                this.AddDoctorVital(
                    occupant,
                    doctor.needs.food.def.LabelCap,
                    this.GetKnownPct(
                        dynamicStatus != null ? dynamicStatus.FoodPct : -1f,
                        doctor.needs.food.CurLevelPercentage));
            }

            if (doctor.needs.rest != null)
            {
                this.AddDoctorVital(
                    occupant,
                    doctor.needs.rest.def.LabelCap,
                    this.GetKnownPct(
                        dynamicStatus != null ? dynamicStatus.RestPct : -1f,
                        doctor.needs.rest.CurLevelPercentage));
            }

            if (doctor.needs.mood != null)
            {
                this.AddDoctorVital(
                    occupant,
                    doctor.needs.mood.def.LabelCap,
                    this.GetKnownPct(
                        dynamicStatus != null ? dynamicStatus.MoodPct : -1f,
                        doctor.needs.mood.CurLevelPercentage));
            }

            if (doctor.needs.joy != null)
            {
                this.AddDoctorVital(
                    occupant,
                    doctor.needs.joy.def.LabelCap,
                    this.GetKnownPct(
                        dynamicStatus != null ? dynamicStatus.JoyPct : -1f,
                        doctor.needs.joy.CurLevelPercentage));
            }
        }

        private void BuildMechVitals(V3MedicalPatientCardModel patient)
        {
            this.AddVital(
                patient,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Durability"),
                patient.DurabilityPct,
                this.formatter.GetPositiveVitalSeverity(patient.DurabilityPct));
            this.AddVital(
                patient,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Damage"),
                patient.DamagePct,
                this.formatter.GetNegativeVitalSeverity(patient.DamagePct));
            this.AddVital(
                patient,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Vital_Power"),
                patient.EnergyPct,
                this.formatter.GetPositiveVitalSeverity(patient.EnergyPct));
        }

        private void AddDoctorVital(
            V3MedicalOccupantCardModel occupant,
            string label,
            float value01)
        {
            if (occupant == null || string.IsNullOrEmpty(label))
            {
                return;
            }

            V3MedicalVitalLineModel vital = new V3MedicalVitalLineModel();
            vital.Label = label;
            vital.Value01 = value01 >= 0f ? Mathf.Clamp01(value01) : -1f;
            vital.ValueText = vital.Value01 >= 0f
                ? Mathf.RoundToInt(vital.Value01 * 100f).ToString() + "%"
                : ShuttleUIText.Tr("CT_Shuttle_Medical_Unavailable");
            vital.SeverityKey = this.formatter.GetPositiveVitalSeverity(vital.Value01);
            occupant.Vitals.Add(vital);
        }

        private float GetKnownPct(float preferred, float fallback)
        {
            return preferred >= 0f ? Mathf.Clamp01(preferred) : fallback;
        }

        private void AddVital(
            V3MedicalPatientCardModel patient,
            string label,
            float value01,
            string severityKey)
        {
            V3MedicalVitalLineModel vital = new V3MedicalVitalLineModel();
            vital.Label = label;
            vital.Value01 = value01;
            vital.ValueText = value01 >= 0f
                ? Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f).ToString() + "%"
                : ShuttleUIText.Tr("CT_Shuttle_Medical_Unavailable");
            vital.SeverityKey = severityKey;
            patient.Vitals.Add(vital);
        }

    }
}

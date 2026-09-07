using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalOccupantModelBuilder
    {
        private readonly V3MedicalClassificationFormatter classificationFormatter;
        private readonly V3MedicalReadModelFormatter formatter;
        private readonly V3MedicalVitalModelBuilder vitalBuilder;

        internal V3MedicalOccupantModelBuilder(
            V3MedicalClassificationFormatter classificationFormatter,
            V3MedicalReadModelFormatter formatter,
            V3MedicalVitalModelBuilder vitalBuilder)
        {
            this.classificationFormatter = classificationFormatter;
            this.formatter = formatter;
            this.vitalBuilder = vitalBuilder;
        }

        internal void BuildOccupants(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            this.BuildOccupants(pageModel, medicalBay, pawnPresenceSnapshot, null);
        }

        internal void BuildOccupants(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            if (pageModel == null)
            {
                return;
            }

            for (int i = 0; i < pageModel.Patients.Count; i++)
            {
                V3MedicalOccupantCardModel occupant =
                    this.BuildPatientOccupant(pageModel.Patients[i]);
                if (occupant != null)
                {
                    pageModel.Occupants.Add(occupant);
                }
            }

            V3MedicalOccupantCardModel doctor =
                this.BuildActiveDoctorOccupant(
                    pageModel,
                    medicalBay,
                    pawnPresenceSnapshot,
                    pawnDynamicStatusSnapshot);
            if (doctor != null)
            {
                pageModel.Occupants.Add(doctor);
            }
        }

        private V3MedicalOccupantCardModel BuildPatientOccupant(
            V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return null;
            }

            V3MedicalOccupantCardModel occupant = new V3MedicalOccupantCardModel();
            occupant.Kind = V3MedicalOccupantKind.Patient;
            occupant.PawnThingID = patient.PatientThingID;
            occupant.PawnKey = patient.PatientKey;
            occupant.Label = patient.Label;
            occupant.DisplayThing = patient.DisplayThing;
            occupant.RoleLabel = this.GetPatientRoleLabel(patient);
            occupant.CurrentStatusLabel = this.GetPatientStateLabel(patient);
            occupant.ActivityLabel = patient.MedicineNeedText;
            occupant.CanSelectAsPatient = true;
            occupant.CanEjectFromMedicalBay = true;
            occupant.Patient = patient;
            return occupant;
        }

        private V3MedicalOccupantCardModel BuildActiveDoctorOccupant(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            if (pageModel == null ||
                medicalBay == null ||
                !pageModel.HasActiveProcedure ||
                pageModel.ActiveProcedureDoctorThingID <= 0)
            {
                return null;
            }

            ShuttlePawnPresenceRecord presence = pawnPresenceSnapshot != null
                ? pawnPresenceSnapshot.FindByPawnThingIDAndKind(
                    pageModel.ActiveProcedureDoctorThingID,
                    ShuttlePawnPresenceKind.MedicalActiveDoctor)
                : null;
            Thing displayThing = presence != null && presence.DisplayThing != null
                ? presence.DisplayThing
                : medicalBay.ActiveProcedureDoctorDisplayThing;
            ShuttlePawnDynamicStatusRecord dynamicStatus =
                pawnDynamicStatusSnapshot != null
                    ? pawnDynamicStatusSnapshot.FindByPawnThingID(
                        pageModel.ActiveProcedureDoctorThingID)
                    : null;
            V3MedicalOccupantCardModel occupant = new V3MedicalOccupantCardModel();
            occupant.Kind = V3MedicalOccupantKind.Doctor;
            occupant.PawnThingID = pageModel.ActiveProcedureDoctorThingID;
            occupant.PawnKey = "doctor-" + pageModel.ActiveProcedureDoctorThingID.ToString();
            occupant.Label = presence != null && !string.IsNullOrEmpty(presence.Label)
                ? presence.Label
                : this.formatter.ValueOrFallback(
                    pageModel.ActiveProcedureDoctorLabel,
                    ShuttleUIText.Tr("CT_Shuttle_Medical_Doctor"));
            occupant.DisplayThing = displayThing;
            occupant.RoleLabel = ShuttleUIText.Tr("CT_Shuttle_Medical_Doctor");
            occupant.CurrentStatusLabel = this.formatter.ValueOrFallback(
                pageModel.ActiveProcedureStatusLabel,
                ShuttleUIText.Tr("CT_Shuttle_MedicalProcedure_Status_Tending"));
            occupant.TreatingPatientThingID = pageModel.ActiveProcedurePatientThingID;
            occupant.TreatingPatientLabel = this.formatter.ValueOrFallback(
                pageModel.ActiveProcedurePatientLabel,
                "-");
            occupant.ActivityLabel = occupant.TreatingPatientLabel != "-"
                ? ShuttleUIText.Tr(
                    "CT_Shuttle_MedicalProcedure_TreatingPatient",
                    occupant.TreatingPatientLabel)
                : occupant.CurrentStatusLabel;
            occupant.CanSelectAsPatient = false;
            occupant.CanEjectFromMedicalBay = false;
            occupant.IsActiveDoctor = true;
            this.vitalBuilder.BuildDoctorVitals(
                occupant,
                displayThing as Pawn,
                dynamicStatus);
            return occupant;
        }

        private string GetPatientRoleLabel(V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Patient");
            }

            return this.classificationFormatter.GetKindFallbackLabel(
                patient.KindKey,
                patient.Kind);
        }

        private string GetPatientStateLabel(V3MedicalPatientCardModel patient)
        {
            if (patient != null &&
                patient.IsActiveProcedurePatient &&
                !string.IsNullOrEmpty(patient.ActiveProcedureStatusLabel))
            {
                return patient.ActiveProcedureStatusLabel;
            }

            return patient != null && !string.IsNullOrEmpty(patient.StateFallbackLabel)
                ? patient.StateFallbackLabel
                : "-";
        }
    }
}

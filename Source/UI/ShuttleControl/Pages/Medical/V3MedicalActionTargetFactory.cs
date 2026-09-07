using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal static class V3MedicalActionTargetFactory
    {
        internal static ShuttleMedicalPageActionContext CreatePageContext(
            V3MedicalPageReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleMedicalPageActionContext target =
                new ShuttleMedicalPageActionContext();
            target.HasMedicalBay = source.HasMedicalBay;
            target.MedicalBayInstalled = source.MedicalBayInstalled;
            target.MedicalBayEnabled = source.MedicalBayEnabled;
            target.MedicalBayPowered = source.MedicalBayPowered;
            target.MedicalBayFull = source.MedicalBayFull;
            target.CanOpenAdmissionDialog = source.CanOpenAdmissionDialog;
            target.AdmissionBlockedReason = source.AdmissionBlockedReason;
            target.PatientCount = source.PatientCount;
            target.OccupiedBeds = source.OccupiedBeds;
            target.TotalBeds = source.TotalBeds;
            target.FreeBeds = source.FreeBeds;
            target.HasActiveProcedure = source.HasActiveProcedure;
            target.ActiveProcedureType = source.ActiveProcedureType;
            target.ActiveProcedureStatus = source.ActiveProcedureStatus;
            target.ActiveProcedureID = source.ActiveProcedureID;
            target.ActiveProcedureDoctorThingID = source.ActiveProcedureDoctorThingID;
            target.ActiveProcedurePatientThingID = source.ActiveProcedurePatientThingID;
            target.ActiveProcedureDoctorLabel = source.ActiveProcedureDoctorLabel;
            target.ActiveProcedurePatientLabel = source.ActiveProcedurePatientLabel;
            target.ActiveProcedureStatusLabel = source.ActiveProcedureStatusLabel;
            target.ActiveProcedureTooltip = source.ActiveProcedureTooltip;
            CopyPatients(source, target);
            CopyAdmissionCandidates(source, target);
            return target;
        }

        internal static ShuttleMedicalPatientActionTarget CreatePatient(
            V3MedicalPatientCardModel source,
            V3MedicalPageReadModel pageModel)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleMedicalPatientActionTarget target =
                new ShuttleMedicalPatientActionTarget();
            target.PatientThingID = source.PatientThingID;
            target.PatientKey = source.PatientKey;
            target.Label = source.Label;
            target.DisplayThing = source.DisplayThing;
            target.Kind = ConvertPatientKind(source.Kind);
            target.KindKey = source.KindKey;
            target.KindFallbackLabel = source.KindFallbackLabel;
            target.StateKey = source.StateKey;
            target.StateFallbackLabel = source.StateFallbackLabel;
            target.TriageKey = source.TriageKey;
            target.TriageFallbackLabel = source.TriageFallbackLabel;
            target.IsActiveProcedurePatient = source.IsActiveProcedurePatient;
            target.TreatmentBlockedByActiveProcedure =
                source.TreatmentBlockedByActiveProcedure;
            target.ActiveProcedureStatusLabel = source.ActiveProcedureStatusLabel;
            target.ActiveProcedureTooltip = source.ActiveProcedureTooltip;
            target.NeedsTreatment = source.NeedsTreatment;
            target.PendingTreatmentCount = source.PendingTreatmentCount;
            CopySurgeryOptions(source, target);

            if (pageModel != null &&
                pageModel.HasActiveProcedure &&
                pageModel.ActiveProcedurePatientThingID == source.PatientThingID)
            {
                target.ActiveProcedureID = pageModel.ActiveProcedureID;
                target.ActiveProcedureStatus = pageModel.ActiveProcedureStatus;
            }

            return target;
        }

        private static void CopySurgeryOptions(
            V3MedicalPatientCardModel source,
            ShuttleMedicalPatientActionTarget target)
        {
            if (source.SurgeryOptions == null)
            {
                return;
            }

            for (int i = 0; i < source.SurgeryOptions.Count; i++)
            {
                V3MedicalSurgeryOptionModel sourceOption = source.SurgeryOptions[i];
                if (sourceOption == null)
                {
                    continue;
                }

                ShuttleMedicalSurgeryOptionActionTarget option =
                    new ShuttleMedicalSurgeryOptionActionTarget();
                option.RecipeDefName = sourceOption.RecipeDefName;
                option.Label = sourceOption.Label;
                option.Description = sourceOption.Description;
                option.BodyPartIndex = sourceOption.BodyPartIndex;
                option.BodyPartLabel = sourceOption.BodyPartLabel;
                option.CanSchedule = sourceOption.CanSchedule;
                option.DisabledReason = sourceOption.DisabledReason;
                option.WorkTicks = sourceOption.WorkTicks;
                option.RequiredMaterialsSummary = sourceOption.RequiredMaterialsSummary;
                option.MissingMaterialsSummary = sourceOption.MissingMaterialsSummary;
                option.HasMissingMaterials = sourceOption.HasMissingMaterials;
                target.SurgeryOptions.Add(option);
            }
        }

        internal static ShuttleMedicalProcedureActionTarget CreateProcedure(
            V3MedicalPageReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleMedicalProcedureActionTarget target =
                new ShuttleMedicalProcedureActionTarget();
            target.HasActiveProcedure = source.HasActiveProcedure;
            target.ActiveProcedureType = source.ActiveProcedureType;
            target.ActiveProcedureStatus = source.ActiveProcedureStatus;
            target.ActiveProcedureID = source.ActiveProcedureID;
            target.PatientThingID = source.ActiveProcedurePatientThingID;
            target.DoctorThingID = source.ActiveProcedureDoctorThingID;
            target.PatientLabel = source.ActiveProcedurePatientLabel;
            target.DoctorLabel = source.ActiveProcedureDoctorLabel;
            target.StatusLabel = source.ActiveProcedureStatusLabel;
            target.Tooltip = source.ActiveProcedureTooltip;
            return target;
        }

        private static void CopyPatients(
            V3MedicalPageReadModel source,
            ShuttleMedicalPageActionContext target)
        {
            if (source.Patients == null)
            {
                return;
            }

            for (int i = 0; i < source.Patients.Count; i++)
            {
                ShuttleMedicalPatientActionTarget patient =
                    CreatePatient(source.Patients[i], source);
                if (patient != null)
                {
                    target.Patients.Add(patient);
                }
            }
        }

        private static void CopyAdmissionCandidates(
            V3MedicalPageReadModel source,
            ShuttleMedicalPageActionContext target)
        {
            if (source.AdmissionCandidates == null)
            {
                return;
            }

            for (int i = 0; i < source.AdmissionCandidates.Count; i++)
            {
                ShuttleMedicalAdmissionCandidateActionTarget candidate =
                    CreateAdmissionCandidate(source.AdmissionCandidates[i]);
                if (candidate != null)
                {
                    target.AdmissionCandidates.Add(candidate);
                }
            }
        }

        private static ShuttleMedicalAdmissionCandidateActionTarget CreateAdmissionCandidate(
            V3MedicalAdmissionCandidateModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleMedicalAdmissionCandidateActionTarget target =
                new ShuttleMedicalAdmissionCandidateActionTarget();
            target.PawnThingID = source.PawnThingID;
            target.Id = source.Id;
            target.Label = source.Label;
            target.DisplayThing = source.DisplayThing;
            target.KindKey = source.KindKey;
            target.KindLabel = source.KindLabel;
            target.TriageKey = source.TriageKey;
            target.TriageLabel = source.TriageLabel;
            target.AdmissionModeKey = source.AdmissionModeKey;
            target.AdmissionModeLabel = source.AdmissionModeLabel;
            target.ReasonText = source.ReasonText;
            target.CanAdmit = source.CanAdmit;
            target.NeedsCarry = source.NeedsCarry;
            target.IsAlreadyInMedicalBay = source.IsAlreadyInMedicalBay;
            target.Tooltip = source.Tooltip;
            CopyAdmissionCarriers(source, target);
            return target;
        }

        private static void CopyAdmissionCarriers(
            V3MedicalAdmissionCandidateModel source,
            ShuttleMedicalAdmissionCandidateActionTarget target)
        {
            if (source.CarryCandidates == null)
            {
                return;
            }

            for (int i = 0; i < source.CarryCandidates.Count; i++)
            {
                V3MedicalAdmissionCarrierModel sourceCarrier =
                    source.CarryCandidates[i];
                if (sourceCarrier == null)
                {
                    continue;
                }

                ShuttleMedicalAdmissionCarrierActionTarget carrier =
                    new ShuttleMedicalAdmissionCarrierActionTarget();
                carrier.PawnThingID = sourceCarrier.PawnThingID;
                carrier.Id = sourceCarrier.Id;
                carrier.Label = sourceCarrier.Label;
                carrier.DisplayThing = sourceCarrier.DisplayThing;
                carrier.CanCarry = sourceCarrier.CanCarry;
                carrier.StatusText = sourceCarrier.StatusText;
                carrier.ReasonText = sourceCarrier.ReasonText;
                carrier.Tooltip = sourceCarrier.Tooltip;
                target.CarryCandidates.Add(carrier);
            }
        }

        private static ShuttleMedicalActionPatientKind ConvertPatientKind(
            V3MedicalPatientKind kind)
        {
            if (kind == V3MedicalPatientKind.Animal)
            {
                return ShuttleMedicalActionPatientKind.Animal;
            }

            if (kind == V3MedicalPatientKind.Mechanoid)
            {
                return ShuttleMedicalActionPatientKind.Mechanoid;
            }

            if (kind == V3MedicalPatientKind.Other)
            {
                return ShuttleMedicalActionPatientKind.Other;
            }

            return ShuttleMedicalActionPatientKind.Human;
        }
    }
}

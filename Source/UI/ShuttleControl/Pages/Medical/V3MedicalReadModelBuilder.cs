using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalReadModelBuilder
    {
        private readonly V3MedicalStatusModelBuilder statusBuilder;
        private readonly V3MedicalProcedureModelBuilder procedureBuilder;
        private readonly V3MedicalPatientModelBuilder patientBuilder;
        private readonly V3MedicalAdmissionModelBuilder admissionBuilder;
        private readonly V3MedicalOccupantModelBuilder occupantBuilder;
        private readonly V3MedicalMetricModelBuilder metricBuilder;

        internal V3MedicalReadModelBuilder()
        {
            V3MedicalClassificationFormatter classificationFormatter =
                new V3MedicalClassificationFormatter();
            V3MedicalReadModelFormatter formatter =
                new V3MedicalReadModelFormatter();
            V3MedicalVitalModelBuilder vitalBuilder =
                new V3MedicalVitalModelBuilder(formatter);
            this.procedureBuilder =
                new V3MedicalProcedureModelBuilder(formatter);
            this.statusBuilder =
                new V3MedicalStatusModelBuilder(formatter);
            this.patientBuilder =
                new V3MedicalPatientModelBuilder(
                    classificationFormatter,
                    formatter,
                    vitalBuilder,
                    new V3MedicalPatientDetailModelBuilder(formatter),
                    this.procedureBuilder);
            this.admissionBuilder =
                new V3MedicalAdmissionModelBuilder(classificationFormatter);
            this.occupantBuilder =
                new V3MedicalOccupantModelBuilder(
                    classificationFormatter,
                    formatter,
                    vitalBuilder);
            this.metricBuilder =
                new V3MedicalMetricModelBuilder(formatter);
        }

        internal V3MedicalPageReadModel Build(
            ShuttleControlReadModel controlModel)
        {
            return this.Build(controlModel, null, null);
        }

        internal V3MedicalPageReadModel Build(
            ShuttleControlReadModel controlModel,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            return this.Build(controlModel, pawnPresenceSnapshot, null);
        }

        internal V3MedicalPageReadModel Build(
            ShuttleControlReadModel controlModel,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            controlModel = controlModel ?? new ShuttleControlReadModel();
            ShuttleMedicalBayReadModel medicalBay = controlModel.MedicalBay != null
                ? controlModel.MedicalBay
                : new ShuttleMedicalBayReadModel();

            V3MedicalPageReadModel pageModel = new V3MedicalPageReadModel();
            this.statusBuilder.ApplyMedicalBayInstallStatus(
                pageModel,
                controlModel,
                medicalBay);
            this.statusBuilder.ApplyCapacityAndSupply(pageModel, medicalBay);
            this.procedureBuilder.ApplyActiveProcedureState(pageModel, medicalBay);
            this.patientBuilder.BuildPatients(
                pageModel,
                medicalBay,
                pawnPresenceSnapshot,
                pawnDynamicStatusSnapshot);
            this.patientBuilder.ApplyActiveProcedureToPatients(pageModel);
            this.occupantBuilder.BuildOccupants(
                pageModel,
                medicalBay,
                pawnPresenceSnapshot,
                pawnDynamicStatusSnapshot);
            this.admissionBuilder.BuildAdmissionCandidates(
                pageModel,
                medicalBay,
                pawnPresenceSnapshot);
            this.statusBuilder.ApplyPostPatientStatus(pageModel);
            this.metricBuilder.BuildSummaryMetrics(pageModel);
            return pageModel;
        }
    }
}

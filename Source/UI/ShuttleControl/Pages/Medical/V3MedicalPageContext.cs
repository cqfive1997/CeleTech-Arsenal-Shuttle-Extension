using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPageContext
    {
        internal readonly IShuttleMedicalAdmissionUIActions AdmissionActions;
        internal readonly IShuttleMedicalOccupantUIActions OccupantActions;
        internal readonly IShuttleMedicalProcedureUIActions ProcedureActions;
        internal readonly IShuttleMedicalTreatmentUIActions TreatmentActions;
        internal readonly IShuttleMedicalSurgeryUIActions SurgeryActions;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3MedicalPageContext(
            IShuttleMedicalAdmissionUIActions admissionActions,
            IShuttleMedicalOccupantUIActions occupantActions,
            IShuttleMedicalProcedureUIActions procedureActions,
            IShuttleMedicalTreatmentUIActions treatmentActions,
            IShuttleMedicalSurgeryUIActions surgeryActions,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.AdmissionActions = admissionActions;
            this.OccupantActions = occupantActions;
            this.ProcedureActions = procedureActions;
            this.TreatmentActions = treatmentActions;
            this.SurgeryActions = surgeryActions;
            this.TutorialTargets = tutorialTargets;
        }
    }
}

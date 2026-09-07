using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical.Dialogs
{
    internal sealed class ShuttleMedicalDialogLauncher :
        IShuttleMedicalDialogLauncher
    {
        public void OpenAdmission(
            ShuttleMedicalPageActionContext pageContext,
            IShuttleMedicalAdmissionCandidateUIActions actions)
        {
            if (pageContext == null || actions == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleMedicalAdmissionV3(
                pageContext,
                actions));
        }

        public void OpenSurgerySelector(
            IShuttleMedicalSurgeryUIActions actions,
            ShuttleMedicalPatientActionTarget patient)
        {
            if (actions == null || patient == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleMedicalSurgerySelectorV3(
                actions,
                patient));
        }
    }
}

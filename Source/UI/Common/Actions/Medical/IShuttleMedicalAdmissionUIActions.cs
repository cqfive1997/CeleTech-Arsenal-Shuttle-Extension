namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal interface IShuttleMedicalAdmissionUIActions
    {
        bool CanOpenAdmissionDialog(ShuttleMedicalPageActionContext pageContext);

        void OpenAdmissionDialog(ShuttleMedicalPageActionContext pageContext);

        string GetAdmissionDialogTooltip(ShuttleMedicalPageActionContext pageContext);
    }
}

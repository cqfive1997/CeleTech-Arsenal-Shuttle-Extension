using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal interface IShuttleMedicalAdmissionCandidateUIActions
    {
        bool CanSelfAdmit(ShuttleMedicalAdmissionCandidateActionTarget candidate);

        bool SelfAdmit(ShuttleMedicalAdmissionCandidateActionTarget candidate);

        string GetSelfAdmitTooltip(ShuttleMedicalAdmissionCandidateActionTarget candidate);

        bool CanCarryAdmit(ShuttleMedicalAdmissionCandidateActionTarget candidate);

        void OpenCarrierMenu(
            ShuttleMedicalAdmissionCandidateActionTarget candidate,
            Action onSuccess);

        bool CarryAdmit(
            ShuttleMedicalAdmissionCandidateActionTarget candidate,
            ShuttleMedicalAdmissionCarrierActionTarget carrier);

        string GetCarryAdmitTooltip(ShuttleMedicalAdmissionCandidateActionTarget candidate);
    }
}

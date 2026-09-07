using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Medical
{
    internal sealed class ShuttleMedicalAdmissionUIActions :
        IShuttleMedicalAdmissionUIActions,
        IShuttleMedicalAdmissionCandidateUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action<ShuttleMedicalPageActionContext, IShuttleMedicalAdmissionCandidateUIActions> openAdmissionDialog;

        internal ShuttleMedicalAdmissionUIActions(
            IShuttleCommandExecutor commandExecutor,
            Action<ShuttleMedicalPageActionContext, IShuttleMedicalAdmissionCandidateUIActions> openAdmissionDialog)
        {
            this.commandExecutor = commandExecutor;
            this.openAdmissionDialog = openAdmissionDialog;
        }

        public bool CanOpenAdmissionDialog(ShuttleMedicalPageActionContext pageContext)
        {
            return pageContext != null &&
                pageContext.CanOpenAdmissionDialog &&
                this.openAdmissionDialog != null;
        }

        public void OpenAdmissionDialog(ShuttleMedicalPageActionContext pageContext)
        {
            if (!this.CanOpenAdmissionDialog(pageContext))
            {
                this.ShowReject(this.GetAdmissionDialogTooltip(pageContext), false);
                return;
            }

            this.openAdmissionDialog(pageContext, this);
        }

        public string GetAdmissionDialogTooltip(ShuttleMedicalPageActionContext pageContext)
        {
            if (pageContext == null)
            {
                return this.Tr("CT_Shuttle_Medical_StateUnavailableCannotAdmit");
            }

            if (!string.IsNullOrEmpty(pageContext.AdmissionBlockedReason))
            {
                return pageContext.AdmissionBlockedReason;
            }

            return pageContext.CanOpenAdmissionDialog
                ? this.Tr("CT_Shuttle_Medical_AdmitTooltip")
                : this.Tr("CT_Shuttle_Medical_CannotAdmitNow");
        }

        public bool CanSelfAdmit(ShuttleMedicalAdmissionCandidateActionTarget candidate)
        {
            return this.commandExecutor != null &&
                candidate != null &&
                candidate.CanAdmit &&
                candidate.AdmissionModeKey == "SelfEnter" &&
                !candidate.NeedsCarry &&
                candidate.PawnThingID > 0;
        }

        public bool SelfAdmit(ShuttleMedicalAdmissionCandidateActionTarget candidate)
        {
            if (!this.CanSelfAdmit(candidate))
            {
                this.ShowReject(this.GetSelfAdmitTooltip(candidate));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new AdmitPatientSelfCommand(candidate.PawnThingID));
            return this.ShowResult(result);
        }

        public string GetSelfAdmitTooltip(ShuttleMedicalAdmissionCandidateActionTarget candidate)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (candidate == null)
            {
                return this.Tr("CT_Shuttle_MedicalBay_InvalidPatient");
            }

            if (candidate.PawnThingID <= 0)
            {
                return this.Tr("CT_Shuttle_Medical_AdmitCandidateInfoMissing");
            }

            if (candidate.AdmissionModeKey != "SelfEnter" || candidate.NeedsCarry)
            {
                return this.Tr("CT_Shuttle_Medical_AdmitUnavailable");
            }

            if (!candidate.CanAdmit)
            {
                return !string.IsNullOrEmpty(candidate.ReasonText)
                    ? candidate.ReasonText
                    : this.Tr("CT_Shuttle_Medical_AdmitUnavailable");
            }

            return this.Tr("CT_Shuttle_Medical_AdmitSelfTooltip");
        }

        public bool CanCarryAdmit(ShuttleMedicalAdmissionCandidateActionTarget candidate)
        {
            return this.commandExecutor != null &&
                candidate != null &&
                candidate.CanAdmit &&
                (candidate.AdmissionModeKey == "NeedsCarry" || candidate.NeedsCarry) &&
                candidate.PawnThingID > 0;
        }

        public void OpenCarrierMenu(
            ShuttleMedicalAdmissionCandidateActionTarget candidate,
            Action onSuccess)
        {
            if (!this.CanCarryAdmit(candidate))
            {
                this.ShowReject(this.GetCarryAdmitTooltip(candidate));
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            bool hasAvailableCarrier = false;
            if (candidate.CarryCandidates != null)
            {
                for (int i = 0; i < candidate.CarryCandidates.Count; i++)
                {
                    ShuttleMedicalAdmissionCarrierActionTarget carrier =
                        candidate.CarryCandidates[i];
                    if (carrier == null)
                    {
                        continue;
                    }

                    string label = this.GetCarrierMenuLabel(carrier);
                    if (carrier.CanCarry && carrier.PawnThingID > 0)
                    {
                        hasAvailableCarrier = true;
                        ShuttleMedicalAdmissionCarrierActionTarget capturedCarrier = carrier;
                        options.Add(new FloatMenuOption(label, delegate
                        {
                            if (this.CarryAdmit(candidate, capturedCarrier) &&
                                onSuccess != null)
                            {
                                onSuccess();
                            }
                        }));
                    }
                    else
                    {
                        options.Add(new FloatMenuOption(label, null));
                    }
                }
            }

            if (!hasAvailableCarrier)
            {
                options.Insert(0, new FloatMenuOption(
                    this.Tr("CT_Shuttle_MedicalBay_CarrierUnavailable"),
                    null));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption(
                    this.Tr("CT_Shuttle_MedicalBay_CarrierUnavailable"),
                    null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public bool CarryAdmit(
            ShuttleMedicalAdmissionCandidateActionTarget candidate,
            ShuttleMedicalAdmissionCarrierActionTarget carrier)
        {
            if (!this.CanCarryAdmit(candidate) ||
                carrier == null ||
                !carrier.CanCarry ||
                carrier.PawnThingID <= 0)
            {
                this.ShowReject(this.GetCarryRejectReason(candidate, carrier));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CarryPatientToMedicalBayCommand(
                    candidate.PawnThingID,
                    carrier.PawnThingID));
            return this.ShowResult(result);
        }

        public string GetCarryAdmitTooltip(ShuttleMedicalAdmissionCandidateActionTarget candidate)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (candidate == null)
            {
                return this.Tr("CT_Shuttle_MedicalBay_InvalidPatient");
            }

            if (candidate.PawnThingID <= 0)
            {
                return this.Tr("CT_Shuttle_Medical_AdmitCandidateInfoMissing");
            }

            if (candidate.AdmissionModeKey != "NeedsCarry" && !candidate.NeedsCarry)
            {
                return this.Tr("CT_Shuttle_Medical_AdmitUnavailable");
            }

            if (!candidate.CanAdmit)
            {
                return !string.IsNullOrEmpty(candidate.ReasonText)
                    ? candidate.ReasonText
                    : this.Tr("CT_Shuttle_Medical_AdmitUnavailable");
            }

            return this.Tr("CT_Shuttle_Medical_AdmitCarryTooltip");
        }

        private string GetCarrierMenuLabel(
            ShuttleMedicalAdmissionCarrierActionTarget carrier)
        {
            if (carrier == null)
            {
                return this.Tr("CT_Shuttle_MedicalBay_CarrierUnavailable");
            }

            string label = !string.IsNullOrEmpty(carrier.Label)
                ? carrier.Label
                : this.Tr("CT_Shuttle_MedicalBay_CarrierUnavailable");
            if (!string.IsNullOrEmpty(carrier.StatusText))
            {
                label += " - " + carrier.StatusText;
            }

            if (!carrier.CanCarry && !string.IsNullOrEmpty(carrier.ReasonText))
            {
                label += " (" + carrier.ReasonText + ")";
            }

            return label;
        }

        private string GetCarryRejectReason(
            ShuttleMedicalAdmissionCandidateActionTarget candidate,
            ShuttleMedicalAdmissionCarrierActionTarget carrier)
        {
            if (!this.CanCarryAdmit(candidate))
            {
                return this.GetCarryAdmitTooltip(candidate);
            }

            if (carrier == null || carrier.PawnThingID <= 0)
            {
                return this.Tr("CT_Shuttle_MedicalBay_CarrierUnavailable");
            }

            if (!carrier.CanCarry)
            {
                return !string.IsNullOrEmpty(carrier.ReasonText)
                    ? carrier.ReasonText
                    : this.Tr("CT_Shuttle_MedicalBay_CarrierUnavailable");
            }

            return this.Tr("CT_Shuttle_MedicalBay_CarriedAdmitFailed");
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private void ShowReject(string message, bool playSound)
        {
            ShuttleUICommandFeedback.ShowReject(message, playSound);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}

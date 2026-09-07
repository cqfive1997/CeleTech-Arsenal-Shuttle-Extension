using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoBayConfigV3 : Window
    {
        private readonly ShuttleCargoBayActionTarget bay;
        private readonly IShuttleCargoBayUIActions actions;
        private readonly ShuttleCargoBayConfigActionTarget originalConfig;
        private readonly ShuttleCargoBayConfigActionTarget workingConfig;
        private readonly V3CargoBayConfigDialogDrawer drawer;

        internal Dialog_ShuttleCargoBayConfigV3(
            ShuttleCargoBayActionTarget bay,
            IShuttleCargoBayUIActions actions)
        {
            this.bay = bay;
            this.actions = actions;
            this.originalConfig = ShuttleCargoBayConfigActionTarget.FromBay(bay);
            this.workingConfig = this.originalConfig.Copy();
            this.drawer = new V3CargoBayConfigDialogDrawer(
                bay,
                this.originalConfig,
                this.workingConfig);
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(760f, 610f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            bool canApply = this.actions != null &&
                this.actions.CanApplyBayConfig(this.bay, this.workingConfig);
            this.drawer.Draw(
                inRect,
                canApply,
                this.GetApplyTooltip(canApply),
                this.ApplyAndClose,
                this.ResetWorkingCopy,
                this.CloseWithoutApply);
        }

        private void CloseWithoutApply()
        {
            this.Close(false);
        }

        private void ApplyAndClose()
        {
            if (!this.HasUnsavedChanges())
            {
                this.Close(false);
                return;
            }

            if (this.actions == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable"),
                    false);
                return;
            }

            if (!this.actions.CanApplyBayConfig(this.bay, this.workingConfig))
            {
                ShuttleUICommandFeedback.ShowReject(this.GetApplyTooltip(false), false);
                return;
            }

            if (this.actions.ApplyBayConfig(this.bay, this.workingConfig.Copy()))
            {
                this.Close(false);
            }
        }

        private void ResetWorkingCopy()
        {
            this.workingConfig.BayKey = this.originalConfig.BayKey;
            this.workingConfig.Label = this.originalConfig.Label;
            this.workingConfig.IsRefrigerated = this.originalConfig.IsRefrigerated;
            this.workingConfig.AllowHumans = this.originalConfig.AllowHumans;
            this.workingConfig.AllowAnimals = this.originalConfig.AllowAnimals;
            this.workingConfig.AllowMechs = this.originalConfig.AllowMechs;
            this.workingConfig.RegionIndex = this.originalConfig.RegionIndex;
            this.workingConfig.ModuleInstanceID = this.originalConfig.ModuleInstanceID;
            this.workingConfig.AutoTransferEnabled = this.originalConfig.AutoTransferEnabled;
            this.workingConfig.HasCustomAutoTransferFilter =
                this.originalConfig.HasCustomAutoTransferFilter;
            this.workingConfig.ItemFilter =
                ShuttleCargoThingFilterUtility.CopyFilter(this.originalConfig.ItemFilter);
            this.workingConfig.AutoTransferFilter =
                ShuttleCargoThingFilterUtility.CopyFilter(this.originalConfig.AutoTransferFilter);
        }

        private bool HasUnsavedChanges()
        {
            if (this.IsRefrigerated())
            {
                return !string.Equals(
                        this.GetSubmittedLabel(this.workingConfig.Label),
                        this.GetSubmittedLabel(this.originalConfig.Label),
                        System.StringComparison.Ordinal) ||
                    this.workingConfig.AutoTransferEnabled !=
                        this.originalConfig.AutoTransferEnabled ||
                    this.workingConfig.HasCustomAutoTransferFilter !=
                        this.originalConfig.HasCustomAutoTransferFilter ||
                    !ShuttleCargoThingFilterUtility.FiltersEquivalent(
                        this.originalConfig.AutoTransferFilter,
                        this.workingConfig.AutoTransferFilter);
            }

            return !string.Equals(
                    this.GetSubmittedLabel(this.workingConfig.Label),
                    this.GetSubmittedLabel(this.originalConfig.Label),
                    System.StringComparison.Ordinal) ||
                this.workingConfig.AllowHumans != this.originalConfig.AllowHumans ||
                this.workingConfig.AllowAnimals != this.originalConfig.AllowAnimals ||
                this.workingConfig.AllowMechs != this.originalConfig.AllowMechs ||
                !ShuttleCargoThingFilterUtility.FiltersEquivalent(
                    this.originalConfig.ItemFilter,
                    this.workingConfig.ItemFilter);
        }

        private string GetApplyTooltip(bool canApply)
        {
            if (this.actions == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (!canApply)
            {
                return this.IsRefrigerated()
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdSettingsUnavailable")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterSettingsUnavailable");
            }

            if (this.IsRefrigerated())
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdApplyTooltip");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_NormalApplyTooltip");
        }

        private bool IsRefrigerated()
        {
            return this.bay != null && this.bay.IsRefrigerated;
        }

        private string GetBayLabel()
        {
            return this.bay != null && !string.IsNullOrEmpty(this.bay.Label)
                ? this.bay.Label
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Bay");
        }

        private string GetSubmittedLabel(string label)
        {
            string trimmed = (label ?? string.Empty).Trim();
            return !string.IsNullOrEmpty(trimmed)
                ? trimmed
                : this.GetBayLabel();
        }
    }
}

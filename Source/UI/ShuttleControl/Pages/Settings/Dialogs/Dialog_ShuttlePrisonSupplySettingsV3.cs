using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    /// <summary>
    /// Edits the per-shuttle Prison Cell cargo supply policy through one command.
    /// </summary>
    internal sealed class Dialog_ShuttlePrisonSupplySettingsV3 : Window, IShuttleSettingsHubSection
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action onPolicyChanged;
        private readonly bool hasPrisonCell;
        private readonly bool supportsRefrigeratedSupply;
        private readonly bool requiresCargoLogistics;
        private bool cargoFoodSupplyEnabled;
        private bool refrigeratedFoodSupplyEnabled;
        private FoodPreferability maximumFoodPreferability;
        private bool appliedCargoFoodSupplyEnabled;
        private bool appliedRefrigeratedFoodSupplyEnabled;
        private FoodPreferability appliedMaximumFoodPreferability;

        internal Dialog_ShuttlePrisonSupplySettingsV3(
            ShuttlePrisonCellReadModel prisonCell,
            IShuttleCommandExecutor commandExecutor,
            Action onPolicyChanged)
        {
            this.commandExecutor = commandExecutor;
            this.onPolicyChanged = onPolicyChanged;
            this.hasPrisonCell = prisonCell != null && prisonCell.HasPrisonCell;
            this.supportsRefrigeratedSupply = !this.hasPrisonCell || prisonCell == null ||
                prisonCell.SupportsRefrigeratedCargoFoodSupply;
            this.requiresCargoLogistics = !this.hasPrisonCell || prisonCell == null ||
                prisonCell.RequiresCargoLogisticsForFoodSupply;
            this.cargoFoodSupplyEnabled = prisonCell == null ||
                prisonCell.CargoFoodSupplyConfiguredEnabled;
            this.refrigeratedFoodSupplyEnabled = prisonCell == null ||
                prisonCell.RefrigeratedFoodSupplyConfiguredEnabled;
            this.maximumFoodPreferability = prisonCell != null
                ? prisonCell.MaximumCargoFoodPreferability
                : FoodPreferability.MealAwful;
            this.CaptureAppliedValues();
            this.forcePause = false;
            this.doCloseX = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(680f, 500f); }
        }

        public ShuttleSettingsHubCommitMode CommitMode
        {
            get { return ShuttleSettingsHubCommitMode.Explicit; }
        }

        public bool CanApply
        {
            get { return this.hasPrisonCell && this.commandExecutor != null; }
        }

        public bool HasPendingChanges
        {
            get
            {
                return this.cargoFoodSupplyEnabled != this.appliedCargoFoodSupplyEnabled ||
                    this.refrigeratedFoodSupplyEnabled != this.appliedRefrigeratedFoodSupplyEnabled ||
                    this.maximumFoodPreferability != this.appliedMaximumFoodPreferability;
            }
        }

        public string ApplyDisabledReason
        {
            get
            {
                if (!this.hasPrisonCell)
                {
                    return "CT_Shuttle_PrisonSupply_NotInstalledHint".Translate().ToString();
                }

                return this.commandExecutor == null
                    ? "CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString()
                    : null;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 8f, inRect.width - 24f, 58f);
            Rect bottomRect = new Rect(inRect.x + 12f, inRect.yMax - 50f, inRect.width - 24f, 42f);
            Rect bodyRect = new Rect(
                inRect.x + 12f,
                headerRect.yMax + 8f,
                inRect.width - 24f,
                bottomRect.y - headerRect.yMax - 16f);

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawBottom(bottomRect);
        }

        public void Draw(Rect rect)
        {
            this.DrawBody(rect);
        }

        public void Reset()
        {
            this.ResetWorkingCopy();
        }

        public bool Apply()
        {
            return this.ApplyWorkingCopy();
        }

        public void OnHubClosed()
        {
        }

        private void DrawHeader(Rect rect)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Medium;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                Widgets.Label(
                    new Rect(rect.x, rect.y, rect.width, 30f),
                    "CT_Shuttle_PrisonSupply_Title".Translate().ToString());
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                Widgets.Label(
                    new Rect(rect.x, rect.y + 34f, rect.width, 22f),
                    "CT_Shuttle_PrisonSupply_Subtitle".Translate().ToString());
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private void DrawBody(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleV3DialogStyle.SettingsInfoRowColor);
            Rect inner = new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f);
            float y = inner.y;

            if (!this.hasPrisonCell)
            {
                this.DrawInfoRow(
                    new Rect(inner.x, y, inner.width, 42f),
                    "CT_Shuttle_PrisonSupply_NotInstalledHint".Translate().ToString());
                y += 50f;
            }

            Rect enabledRect = new Rect(inner.x, y, inner.width, 30f);
            Widgets.CheckboxLabeled(
                enabledRect,
                "CT_Shuttle_PrisonSupply_Enable".Translate().ToString(),
                ref this.cargoFoodSupplyEnabled);
            TooltipHandler.TipRegion(
                enabledRect,
                "CT_Shuttle_PrisonSupply_EnableTooltip".Translate().ToString());
            y += 40f;

            Rect refrigeratedRect = new Rect(inner.x, y, inner.width, 30f);
            bool previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled &&
                this.cargoFoodSupplyEnabled &&
                this.supportsRefrigeratedSupply;
            Widgets.CheckboxLabeled(
                refrigeratedRect,
                "CT_Shuttle_PrisonSupply_EnableRefrigerated".Translate().ToString(),
                ref this.refrigeratedFoodSupplyEnabled);
            GUI.enabled = previousGuiEnabled;
            TooltipHandler.TipRegion(
                refrigeratedRect,
                "CT_Shuttle_PrisonSupply_EnableRefrigeratedTooltip".Translate().ToString());
            y += 46f;

            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(inner.x, y, inner.width, 26f),
                "CT_Shuttle_PrisonSupply_MaximumTier".Translate().ToString());
            y += 30f;

            float gap = 8f;
            float buttonWidth = (inner.width - (gap * 3f)) / 4f;
            this.DrawTierButton(
                new Rect(inner.x, y, buttonWidth, 40f),
                FoodPreferability.MealAwful);
            this.DrawTierButton(
                new Rect(inner.x + buttonWidth + gap, y, buttonWidth, 40f),
                FoodPreferability.MealSimple);
            this.DrawTierButton(
                new Rect(inner.x + ((buttonWidth + gap) * 2f), y, buttonWidth, 40f),
                FoodPreferability.MealFine);
            this.DrawTierButton(
                new Rect(inner.x + ((buttonWidth + gap) * 3f), y, buttonWidth, 40f),
                FoodPreferability.MealLavish);
            y += 52f;

            string logisticsText = this.requiresCargoLogistics
                ? "CT_Shuttle_PrisonSupply_LogisticsRequired".Translate().ToString()
                : "CT_Shuttle_PrisonSupply_LogisticsOptional".Translate().ToString();
            this.DrawInfoRow(new Rect(inner.x, y, inner.width, 54f), logisticsText);
        }

        private void DrawTierButton(Rect rect, FoodPreferability preferability)
        {
            bool selected = this.maximumFoodPreferability == preferability;
            if (ShuttleV3DialogLayout.DrawTintedButton(
                rect,
                ShuttleFoodPreferabilityText.GetMaximumTierLabel(preferability),
                true,
                selected
                    ? ShuttleV3DialogStyle.SelectedColor
                    : ShuttleV3DialogStyle.ButtonBgColor,
                selected
                    ? ShuttleV3DialogStyle.BlueStatusColor
                    : ShuttleV3DialogStyle.ButtonBorderColor,
                ShuttleV3DialogStyle.ButtonTextColor,
                "CT_Shuttle_PrisonSupply_MaximumTierTooltip".Translate().ToString()))
            {
                this.maximumFoodPreferability = preferability;
            }
        }

        private void DrawInfoRow(Rect rect, string text)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleV3DialogStyle.SettingsInfoRowColor);
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.WordWrap = true;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                Widgets.Label(
                    new Rect(rect.x + 8f, rect.y + 7f, rect.width - 16f, rect.height - 14f),
                    text);
            }
            finally
            {
                Text.Font = oldFont;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        private void DrawBottom(Rect rect)
        {
            Rect resetRect = new Rect(rect.x, rect.y + 4f, 150f, 34f);
            Rect cancelRect = new Rect(rect.xMax - 250f, rect.y + 4f, 112f, 34f);
            Rect applyRect = new Rect(rect.xMax - 128f, rect.y + 4f, 128f, 34f);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                resetRect,
                "CT_Shuttle_Settings_ResetDefaults".Translate().ToString(),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.ResetWorkingCopy();
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                cancelRect,
                "CT_Shuttle_UI_Cancel".Translate().ToString(),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.Close(false);
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                applyRect,
                "CT_Shuttle_UI_Apply".Translate().ToString(),
                this.commandExecutor != null,
                ShuttleV3DialogButtonKind.Primary,
                null))
            {
                if (this.ApplyWorkingCopy())
                {
                    this.Close(false);
                }
            }
        }

        private void ResetWorkingCopy()
        {
            this.cargoFoodSupplyEnabled = true;
            this.refrigeratedFoodSupplyEnabled = true;
            this.maximumFoodPreferability = FoodPreferability.MealAwful;
        }

        private bool ApplyWorkingCopy()
        {
            if (this.commandExecutor == null)
            {
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetShuttlePrisonCellSupplyPolicyCommand(
                    this.cargoFoodSupplyEnabled,
                    this.refrigeratedFoodSupplyEnabled,
                    this.maximumFoodPreferability));
            if (result == null || !result.Success)
            {
                Messages.Message(
                    result != null && !string.IsNullOrEmpty(result.Message)
                        ? result.Message
                        : "CT_Shuttle_Command_AssemblyUnavailable".Translate().ToString(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            Messages.Message(result.Message, MessageTypeDefOf.NeutralEvent, false);
            if (this.onPolicyChanged != null)
            {
                this.onPolicyChanged();
            }

            this.CaptureAppliedValues();
            return true;
        }

        private void CaptureAppliedValues()
        {
            this.appliedCargoFoodSupplyEnabled = this.cargoFoodSupplyEnabled;
            this.appliedRefrigeratedFoodSupplyEnabled = this.refrigeratedFoodSupplyEnabled;
            this.appliedMaximumFoodPreferability = this.maximumFoodPreferability;
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;
using CommonPatient = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical.ShuttleMedicalPatientActionTarget;
using CommonSurgeryActions = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical.IShuttleMedicalSurgeryUIActions;
using CommonSurgeryOption = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical.ShuttleMedicalSurgeryOptionActionTarget;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical.Dialogs
{
    internal sealed class Dialog_ShuttleMedicalSurgerySelectorV3 : Window
    {
        private const float HeaderHeight = 54f;
        private const float FooterHeight = 34f;
        private const float Gap = 10f;
        private const float CardHeight = 92f;

        private readonly CommonSurgeryActions actionRouter;
        private readonly CommonPatient patient;
        private Vector2 scrollPosition;

        internal Dialog_ShuttleMedicalSurgerySelectorV3(
            CommonSurgeryActions actionRouter,
            CommonPatient patient)
        {
            this.actionRouter = actionRouter;
            this.patient = patient;
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
                return new Vector2(720f, 640f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 10f, inRect.width - 24f, HeaderHeight);
            Rect footerRect = new Rect(inRect.x + 12f, inRect.yMax - FooterHeight - 8f, inRect.width - 24f, FooterHeight);
            Rect bodyRect = new Rect(
                inRect.x + 12f,
                headerRect.yMax + Gap,
                inRect.width - 24f,
                footerRect.y - headerRect.yMax - (Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawFooter(footerRect);
        }

        private void DrawHeader(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleV3DialogStyle.HeaderColor);
            ShuttleV3DialogLayout.DrawRectBorder(rect, ShuttleV3DialogStyle.BorderColor, 2f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.86f, 0.94f, 1f, 1f);
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 26f),
                "CT_Shuttle_MedicalSurgery_SelectOperation".Translate().ToString());
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 33f, rect.width - 24f, 18f),
                this.GetPatientLabel());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect)
        {
            IReadOnlyList<CommonSurgeryOption> options =
                this.patient != null ? this.patient.SurgeryOptions : null;
            if (options == null || options.Count == 0)
            {
                this.DrawEmpty(rect);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                rect.width - 16f,
                Mathf.Max(rect.height, options.Count * (CardHeight + Gap)));
            Widgets.BeginScrollView(rect, ref this.scrollPosition, viewRect);
            float y = 0f;
            for (int i = 0; i < options.Count; i++)
            {
                CommonSurgeryOption option = options[i];
                if (option == null)
                {
                    continue;
                }

                Rect cardRect = new Rect(0f, y, viewRect.width, CardHeight);
                this.DrawOptionCard(cardRect, option);
                y += CardHeight + Gap;
            }

            Widgets.EndScrollView();
        }

        private void DrawEmpty(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, true, ShuttleV3DialogStyle.DisabledColor);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                rect.ContractedBy(12f),
                "CT_Shuttle_MedicalSurgery_NoOptions".Translate().ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawOptionCard(Rect rect, CommonSurgeryOption option)
        {
            bool available = option != null && option.CanSchedule;
            Color accent = available ? ShuttleV3DialogStyle.GreenStatusColor : ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                !available,
                new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 0.94f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 3f, rect.height), accent);

            Rect titleRect = new Rect(rect.x + 12f, rect.y + 7f, rect.width - 160f, 22f);
            Rect partRect = new Rect(rect.xMax - 138f, rect.y + 7f, 126f, 22f);
            Rect metaRect = new Rect(rect.x + 12f, rect.y + 32f, rect.width - 160f, 18f);
            Rect descRect = new Rect(rect.x + 12f, rect.y + 54f, rect.width - 160f, 30f);
            Rect buttonRect = new Rect(rect.xMax - 128f, rect.yMax - 34f, 116f, 26f);

            Text.Font = GameFont.Small;
            GUI.color = available ? Color.white : ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(titleRect, this.FitLabelText(this.GetOptionLabel(option), titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(partRect, this.FitLabelText(this.GetBodyPartLabel(option), partRect.width));
            ShuttleV3DialogLayout.SafeLabel(metaRect, this.FitLabelText(this.GetMetaLine(option), metaRect.width));
            GUI.color = available ? ShuttleV3DialogStyle.MutedTextColor : ShuttleV3DialogStyle.YellowStatusColor;
            ShuttleV3DialogLayout.SafeLabel(descRect, this.FitLabelText(this.GetDescriptionLine(option), descRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            string buttonLabel = available
                ? "CT_Shuttle_MedicalSurgery_Select".Translate().ToString()
                : "CT_Shuttle_MedicalSurgery_Unavailable".Translate().ToString();
            bool clicked;
            ShuttleV3DialogLayout.DrawAccentButton(
                buttonRect,
                buttonLabel,
                available,
                available ? ShuttleV3DialogStyle.GreenStatusColor : ShuttleV3DialogStyle.MutedTextColor,
                available
                    ? "CT_Shuttle_MedicalSurgery_SelectDoctor".Translate().ToString()
                    : this.GetUnavailableTooltip(option),
                out clicked);
            if (clicked)
            {
                if (available && this.actionRouter != null)
                {
                    this.Close(false);
                    this.actionRouter.OpenSurgeryDoctorMenu(this.patient, option);
                }
                else
                {
                    Messages.Message(this.GetUnavailableTooltip(option), MessageTypeDefOf.RejectInput, false);
                }
            }

            if (Widgets.ButtonInvisible(new Rect(rect.x, rect.y, rect.width - 140f, rect.height)) &&
                available &&
                this.actionRouter != null)
            {
                this.Close(false);
                this.actionRouter.OpenSurgeryDoctorMenu(this.patient, option);
            }

            TooltipHandler.TipRegion(rect, this.BuildTooltip(option));
        }

        private void DrawFooter(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                rect,
                "CT_Shuttle_MedicalSurgery_SelectDoctor".Translate().ToString());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private string BuildTooltip(CommonSurgeryOption option)
        {
            string tooltip = this.GetOptionLabel(option) +
                "\n" + this.GetBodyPartLabel(option) +
                "\n" + this.GetMetaLine(option);
            if (option != null && option.HasMissingMaterials)
            {
                tooltip += "\n" + "CT_Shuttle_MedicalSurgery_MissingMaterials"
                    .Translate(option.MissingMaterialsSummary)
                    .ToString();
            }
            else
            {
                tooltip += "\n" + "CT_Shuttle_MedicalSurgery_MaterialsAvailable".Translate().ToString();
            }

            if (!string.IsNullOrEmpty(option != null ? option.Description : null))
            {
                tooltip += "\n\n" + option.Description;
            }

            if (option != null && !option.CanSchedule)
            {
                tooltip += "\n\n" + this.GetUnavailableTooltip(option);
            }

            return tooltip;
        }

        private string GetUnavailableTooltip(CommonSurgeryOption option)
        {
            return "CT_Shuttle_MedicalSurgery_OptionDisabledTooltip"
                .Translate(this.GetDisabledReason(option))
                .ToString();
        }

        private string GetMetaLine(CommonSurgeryOption option)
        {
            string materials = option != null && !string.IsNullOrEmpty(option.RequiredMaterialsSummary)
                ? option.RequiredMaterialsSummary
                : "-";
            int workTicks = option != null ? Mathf.Max(0, option.WorkTicks) : 0;
            return "CT_Shuttle_MedicalSurgery_RequiredMaterials".Translate(materials).ToString() +
                " / " +
                "CT_Shuttle_MedicalSurgery_WorkTicks".Translate(workTicks).ToString();
        }

        private string GetDescriptionLine(CommonSurgeryOption option)
        {
            if (option != null && !option.CanSchedule)
            {
                return this.GetDisabledReason(option);
            }

            if (option != null && option.HasMissingMaterials)
            {
                return "CT_Shuttle_MedicalSurgery_MissingMaterials"
                    .Translate(option.MissingMaterialsSummary)
                    .ToString();
            }

            return option != null && !string.IsNullOrEmpty(option.Description)
                ? option.Description
                : this.GetMetaLine(option);
        }

        private string GetDisabledReason(CommonSurgeryOption option)
        {
            return option != null && !string.IsNullOrEmpty(option.DisabledReason)
                ? option.DisabledReason
                : "CT_Shuttle_MedicalSurgery_Unavailable".Translate().ToString();
        }

        private string GetOptionLabel(CommonSurgeryOption option)
        {
            if (option == null)
            {
                return "-";
            }

            if (!string.IsNullOrEmpty(option.Label))
            {
                return option.Label;
            }

            return !string.IsNullOrEmpty(option.RecipeDefName)
                ? option.RecipeDefName
                : "CT_Shuttle_MedicalSurgery_Schedule".Translate().ToString();
        }

        private string GetBodyPartLabel(CommonSurgeryOption option)
        {
            return option != null && !string.IsNullOrEmpty(option.BodyPartLabel)
                ? option.BodyPartLabel
                : "CT_Shuttle_MedicalSurgery_NoBodyPart".Translate().ToString();
        }

        private string GetPatientLabel()
        {
            return this.patient != null && !string.IsNullOrEmpty(this.patient.Label)
                ? this.patient.Label
                : "-";
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width, string.Empty, 0f);
        }

    }
}

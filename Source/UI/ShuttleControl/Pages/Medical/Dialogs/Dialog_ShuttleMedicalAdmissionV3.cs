using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;
using CommonAdmissionActions = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical.IShuttleMedicalAdmissionCandidateUIActions;
using CommonCandidate = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical.ShuttleMedicalAdmissionCandidateActionTarget;
using CommonPage = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical.ShuttleMedicalPageActionContext;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical.Dialogs
{
    /// <summary>
    /// Admission candidate preview. Candidate discovery comes from read models, and the only
    /// wired actions are SelfEnter and NeedsCarry through command/service boundaries. This dialog must not scan
    /// maps, assign pawn jobs, move holders, or mutate medical bay state directly.
    /// </summary>
    internal sealed class Dialog_ShuttleMedicalAdmissionV3 : Window
    {
        private const float Gap = 10f;
        private const float CandidateCardHeight = 104f;

        private readonly CommonPage model;
        private readonly CommonAdmissionActions admissionActions;
        private Vector2 candidateScroll;

        internal Dialog_ShuttleMedicalAdmissionV3(
            CommonPage model,
            CommonAdmissionActions admissionActions)
        {
            this.model = model ?? new CommonPage();
            this.admissionActions = admissionActions;
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
                return new Vector2(760f, 560f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 46f);
            Rect bodyRect = new Rect(
                inRect.x,
                titleRect.yMax + Gap,
                inRect.width,
                inRect.height - titleRect.height - Gap - 40f);
            Rect buttonRect = new Rect(inRect.x, bodyRect.yMax + Gap, inRect.width, 30f);

            this.DrawTitle(titleRect);
            this.DrawBody(bodyRect);
            this.DrawButtons(buttonRect);
        }

        private void DrawTitle(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleV3DialogStyle.HeaderColor);
            ShuttleV3DialogLayout.DrawRectBorder(rect, ShuttleV3DialogStyle.BorderColor, 2f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.86f, 0.94f, 1f, 1f);
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 26f),
                ShuttleUIText.Tr("CT_Shuttle_Medical_AdmitPatient"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 32f);
            ShuttleV3DialogLayout.DrawSectionHeader(
                headerRect,
                ShuttleUIText.Tr("CT_Shuttle_Medical_AdmitWindowSubtitle"));
            Rect inner = new Rect(rect.x + 8f, rect.y + 42f, rect.width - 16f, rect.height - 50f);

            List<CommonCandidate> candidates =
                this.model != null ? this.model.AdmissionCandidates : null;
            if (candidates == null || candidates.Count == 0)
            {
                this.DrawEmptyState(inner);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                inner.width - 16f,
                Mathf.Max(inner.height, candidates.Count * CandidateCardHeight));
            Widgets.BeginScrollView(inner, ref this.candidateScroll, viewRect);
            for (int i = 0; i < candidates.Count; i++)
            {
                CommonCandidate candidate = candidates[i];
                Rect rowRect = new Rect(0f, i * CandidateCardHeight, viewRect.width, CandidateCardHeight - 7f);
                this.DrawCandidateCard(rowRect, candidate);
            }

            Widgets.EndScrollView();
        }

        private void DrawEmptyState(Rect rect)
        {
            Rect cardRect = new Rect(rect.x, rect.y, rect.width, 118f);
            ShuttleV3DialogLayout.DrawCardBackground(cardRect, false, true);
            Widgets.DrawBoxSolid(new Rect(cardRect.x, cardRect.y, 4f, cardRect.height), ShuttleV3DialogStyle.YellowStatusColor);
            Text.Font = GameFont.Small;
            GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(cardRect.x + 12f, cardRect.y + 12f, cardRect.width - 24f, 24f),
                ShuttleUIText.Tr("CT_Shuttle_Medical_AdmitNoCandidates"));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(cardRect.x + 12f, cardRect.y + 42f, cardRect.width - 24f, 62f),
                "CT_Shuttle_Medical_AdmitNoCandidatesTooltip".Translate().ToString());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.AddTooltip(
                cardRect,
                "CT_Shuttle_Medical_AdmitNoCandidatesTooltip".Translate().ToString());
        }

        private void DrawCandidateCard(Rect rect, CommonCandidate candidate)
        {
            if (candidate == null)
            {
                return;
            }

            ShuttleV3DialogLayout.DrawCardBackground(rect, false, !candidate.CanAdmit, ShuttleV3DialogStyle.CardColor);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 11f, 46f, 46f);
            this.DrawAppearance(iconRect, candidate.DisplayThing, this.GetFallbackText(candidate));

            Rect badgeRect = new Rect(rect.xMax - 108f, rect.y + 8f, 100f, 22f);
            this.DrawStatusBadge(badgeRect, candidate.AdmissionModeLabel, this.GetAdmissionModeColor(candidate));

            Rect titleRect = new Rect(iconRect.xMax + 8f, rect.y + 7f, badgeRect.x - iconRect.xMax - 12f, 22f);
            Text.Font = GameFont.Small;
            ShuttleV3DialogLayout.SafeLabel(titleRect, this.FitLabelText(candidate.Label, titleRect.width));

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 31f, rect.width - 128f, 17f),
                this.FitLabelText(candidate.KindLabel + " / " + candidate.TriageLabel, rect.width - 128f));
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 64f, rect.width - 136f, 18f),
                this.FitLabelText(candidate.ReasonText, rect.width - 136f));
            GUI.color = Color.white;

            Rect actionRect = new Rect(rect.xMax - 116f, rect.yMax - 32f, 108f, 24f);
            this.DrawAdmissionActionButton(actionRect, candidate);
            Text.Font = GameFont.Small;
            this.AddTooltip(rect, this.GetCandidateTooltip(candidate));
        }

        private void DrawAdmissionActionButton(Rect rect, CommonCandidate candidate)
        {
            bool selfEnter = candidate != null && candidate.AdmissionModeKey == "SelfEnter";
            bool needsCarry = candidate != null && (candidate.AdmissionModeKey == "NeedsCarry" || candidate.NeedsCarry);
            bool enabled = selfEnter
                ? (this.admissionActions != null && this.admissionActions.CanSelfAdmit(candidate))
                : (needsCarry && this.admissionActions != null && this.admissionActions.CanCarryAdmit(candidate));
            Color color = this.GetActionButtonColor(candidate, enabled);
            string tooltip = this.GetAdmissionCommandTooltip(candidate);
            bool clicked;
            ShuttleV3DialogLayout.DrawAccentButton(
                rect,
                this.GetActionLabel(candidate),
                enabled,
                color,
                tooltip,
                out clicked);

            if (clicked)
            {
                if (!enabled)
                {
                    Messages.Message(tooltip, MessageTypeDefOf.RejectInput, false);
                }
                else if (selfEnter)
                {
                    if (this.admissionActions != null && this.admissionActions.SelfAdmit(candidate))
                    {
                        this.Close();
                    }
                }
                else
                {
                    this.admissionActions.OpenCarrierMenu(candidate, delegate
                    {
                        this.Close();
                    });
                }
            }
        }

        private void DrawButtons(Rect rect)
        {
            Rect closeRect = new Rect(rect.xMax - 96f, rect.y, 96f, rect.height);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                closeRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.Close();
            }
        }

        private void DrawAppearance(Rect rect, Thing displayThing, string fallbackText)
        {
            if (ShuttleThingIconDrawer.Draw(rect, displayThing))
            {
                return;
            }

            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.24f));
            ShuttleV3DialogLayout.DrawRectBorder(rect, ShuttleV3DialogStyle.SubtleBorderColor, 1f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            ShuttleV3DialogLayout.SafeLabel(rect, fallbackText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawStatusBadge(Rect rect, string label, Color color)
        {
            Color oldColor = GUI.color;
            Widgets.DrawBoxSolid(rect, new Color(color.r, color.g, color.b, 0.28f));
            ShuttleV3DialogLayout.DrawRectBorder(rect, color, 1f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = color;
            ShuttleV3DialogLayout.SafeLabel(rect, this.FitLabelText(label, rect.width - 4f));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = oldColor;
        }

        private string GetActionLabel(CommonCandidate candidate)
        {
            if (candidate == null || !candidate.CanAdmit)
            {
                if (candidate != null && candidate.AdmissionModeKey == "Pending")
                {
                    return ShuttleUIText.Tr("CT_Shuttle_Medical_Unavailable");
                }

                return ShuttleUIText.Tr("CT_Shuttle_Medical_NotReceivable");
            }

            if (candidate.AdmissionModeKey == "NeedsCarry" || candidate.NeedsCarry)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_AssignCarry");
            }

            if (candidate.AdmissionModeKey == "SelfEnter")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_SelfEnter");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Medical_ActionAdmit");
        }

        private Color GetAdmissionModeColor(CommonCandidate candidate)
        {
            if (candidate == null)
            {
                return ShuttleV3DialogStyle.MutedTextColor;
            }

            if (candidate.AdmissionModeKey == "SelfEnter")
            {
                return ShuttleV3DialogStyle.GreenStatusColor;
            }

            if (candidate.AdmissionModeKey == "NeedsCarry")
            {
                return ShuttleV3DialogStyle.YellowStatusColor;
            }

            if (candidate.AdmissionModeKey == "NotReceivable")
            {
                return ShuttleV3DialogStyle.RedStatusColor;
            }

            if (candidate.AdmissionModeKey == "Pending")
            {
                return ShuttleV3DialogStyle.YellowStatusColor;
            }

            return ShuttleV3DialogStyle.MutedTextColor;
        }

        private Color GetActionButtonColor(CommonCandidate candidate, bool enabled)
        {
            if (candidate == null)
            {
                return ShuttleV3DialogStyle.MutedTextColor;
            }

            if (candidate.AdmissionModeKey == "SelfEnter")
            {
                return enabled ? ShuttleV3DialogStyle.BlueStatusColor : ShuttleV3DialogStyle.MutedTextColor;
            }

            if (candidate.AdmissionModeKey == "NeedsCarry")
            {
                return enabled ? ShuttleV3DialogStyle.YellowStatusColor : ShuttleV3DialogStyle.MutedTextColor;
            }

            if (candidate.AdmissionModeKey == "NotReceivable")
            {
                return ShuttleV3DialogStyle.RedStatusColor;
            }

            if (candidate.AdmissionModeKey == "Pending")
            {
                return ShuttleV3DialogStyle.YellowStatusColor;
            }

            return enabled ? ShuttleV3DialogStyle.BlueStatusColor : ShuttleV3DialogStyle.MutedTextColor;
        }

        private string GetFallbackText(CommonCandidate candidate)
        {
            if (candidate == null || string.IsNullOrEmpty(candidate.KindKey))
            {
                return "?";
            }

            if (candidate.KindKey == "Mechanoid")
            {
                return "M";
            }

            if (candidate.KindKey == "Animal")
            {
                return "A";
            }

            return "H";
        }

        private string GetCandidateTooltip(CommonCandidate candidate)
        {
            if (candidate == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(candidate.Tooltip))
            {
                return candidate.Tooltip;
            }

            return candidate.Label + "\n" +
                candidate.KindLabel + " / " + candidate.TriageLabel + "\n" +
                candidate.AdmissionModeLabel + "\n" +
                candidate.ReasonText;
        }

        private string GetAdmissionCommandTooltip(CommonCandidate candidate)
        {
            if (candidate != null && candidate.AdmissionModeKey == "SelfEnter")
            {
                return this.admissionActions != null
                    ? this.admissionActions.GetSelfAdmitTooltip(candidate)
                    : "CT_Shuttle_Medical_AdmitSelfUnavailable".Translate().ToString();
            }

            if (candidate != null && candidate.AdmissionModeKey == "NeedsCarry")
            {
                return this.admissionActions != null
                    ? this.admissionActions.GetCarryAdmitTooltip(candidate)
                    : "CT_Shuttle_Medical_AdmitCarryUnavailable".Translate().ToString();
            }

            if (candidate != null && candidate.AdmissionModeKey == "Pending")
            {
                return "CT_Shuttle_Medical_AdmitCandidatePending".Translate().ToString();
            }

            if (candidate != null && candidate.AdmissionModeKey == "NotReceivable")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_AdmitNotReceivableReadOnly");
            }

            return "CT_Shuttle_Medical_AdmitUnavailable".Translate().ToString();
        }

        private void AddTooltip(Rect rect, string tooltip)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width, "-", 8f);
        }

    }
}

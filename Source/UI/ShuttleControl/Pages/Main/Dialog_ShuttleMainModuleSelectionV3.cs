using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal class Dialog_ShuttleMainModuleSelectionV3 : Window
    {
        private const float Gap = 10f;
        private const float HeaderHeight = 58f;
        private const float FooterHeight = 40f;
        private const float CardHeight = 132f;

        private readonly Func<List<V3MainInstallCandidateModel>> candidateProvider;
        private readonly Func<int> getResearchFingerprint;
        private readonly Action<V3MainInstallCandidateModel> onInstall;
        private readonly bool replacing;
        private readonly V3MainText text = new V3MainText();
        private readonly V3MainPanelDrawer panel;
        private readonly V3MainIconKeyResolver iconKeys = new V3MainIconKeyResolver();
        private readonly ShuttleControlIconRegistry icons = new ShuttleControlIconRegistry();
        private List<V3MainInstallCandidateModel> candidates;
        private Vector2 scrollPosition;
        private int lastResearchFingerprint = int.MinValue;

        internal Dialog_ShuttleMainModuleSelectionV3(
            Func<List<V3MainInstallCandidateModel>> candidateProvider,
            Func<int> getResearchFingerprint,
            Action<V3MainInstallCandidateModel> onInstall,
            bool replacing)
        {
            this.candidateProvider = candidateProvider;
            this.getResearchFingerprint = getResearchFingerprint;
            this.onInstall = onInstall;
            this.replacing = replacing;
            this.panel = new V3MainPanelDrawer(this.text);
            this.candidates = this.BuildCandidatesFromProvider();
            this.lastResearchFingerprint = this.ReadResearchFingerprint();
            this.ConfigureWindow();
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(780f, 620f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.RefreshCandidatesIfNeeded();
            ShuttleUILayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(
                inRect.x + 12f,
                inRect.y + 10f,
                inRect.width - 24f,
                HeaderHeight);
            Rect footerRect = new Rect(
                inRect.x + 12f,
                inRect.yMax - FooterHeight - 8f,
                inRect.width - 24f,
                FooterHeight);
            Rect bodyRect = new Rect(
                inRect.x + 12f,
                headerRect.yMax + Gap,
                inRect.width - 24f,
                footerRect.y - headerRect.yMax - (Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawFooter(footerRect);
        }

        private void ConfigureWindow()
        {
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = false;
        }

        private void DrawHeader(Rect rect)
        {
            ShuttleUILayout.DrawCardBackground(rect, true, false, V3MainText.StrongCardColor);
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 26f),
                this.text.Tr("CT_Shuttle_UI_ModuleReplace_SelectModuleTitle"));

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            string phaseLabel = this.replacing
                ? this.text.Tr("CT_Shuttle_UI_ReplaceModule")
                : this.text.Tr("CT_Shuttle_UI_InstallModule");
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 35f, rect.width - 24f, 18f),
                this.text.Tr("CT_Shuttle_UI_ModuleReplace_CurrentPhase", phaseLabel));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawBody(Rect rect)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false, V3MainText.MutedCardColor);
            Rect listRect = new Rect(
                rect.x + 8f,
                rect.y + 8f,
                rect.width - 16f,
                rect.height - 16f);
            if (this.candidates == null || this.candidates.Count == 0)
            {
                this.DrawEmptyState(listRect);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, this.candidates.Count * CardHeight));
            Widgets.BeginScrollView(listRect, ref this.scrollPosition, viewRect);
            for (int i = 0; i < this.candidates.Count; i++)
            {
                Rect cardRect = new Rect(0f, i * CardHeight, viewRect.width, CardHeight - 8f);
                this.DrawCandidateCard(cardRect, this.candidates[i]);
            }

            Widgets.EndScrollView();
        }

        private void DrawEmptyState(Rect rect)
        {
            Rect cardRect = new Rect(rect.x, rect.y, rect.width, 96f);
            this.panel.DrawCardFrame(cardRect, V3MainText.YellowColor, false, false);
            Text.Font = GameFont.Small;
            GUI.color = V3MainText.YellowColor;
            ShuttleUILayout.SafeLabel(
                new Rect(cardRect.x + 12f, cardRect.y + 12f, cardRect.width - 24f, 24f),
                this.text.Tr("CT_Shuttle_UI_ModuleReplace_Empty"));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawCandidateCard(
            Rect rect,
            V3MainInstallCandidateModel candidate)
        {
            if (candidate == null)
            {
                return;
            }

            Color accent = this.GetStatusColor(candidate);
            this.panel.DrawCardFrame(rect, accent, false, !candidate.CanInstall);

            Rect glyphRect = new Rect(rect.x + 10f, rect.y + 13f, 52f, 52f);
            this.panel.DrawFramelessIcon(
                glyphRect,
                this.icons.GetIcon(this.iconKeys.GetCandidateIconKey(candidate, false)),
                this.GetFallbackGlyph(candidate),
                accent,
                !candidate.CanInstall);

            Rect statusRect = new Rect(rect.xMax - 118f, rect.y + 10f, 108f, 22f);
            this.panel.DrawStatusBadge(
                statusRect,
                V3MainInstallCandidateText.GetStatusLabel(candidate),
                accent);

            Rect titleRect = new Rect(glyphRect.xMax + 10f, rect.y + 8f, statusRect.x - glyphRect.xMax - 18f, 24f);
            Text.Font = GameFont.Small;
            GUI.color = candidate.CanInstall ? Color.white : ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                this.text.FitLabelText(candidate.DisplayLabel, titleRect.width));

            Text.Font = GameFont.Tiny;
            GUI.color = candidate.CanInstall
                ? ShuttleUIStyle.MutedTextColor
                : new Color(0.55f, 0.58f, 0.62f, 1f);
            ShuttleUILayout.SafeLabel(
                new Rect(glyphRect.xMax + 10f, rect.y + 35f, rect.width - 156f, 36f),
                V3MainInstallCandidateText.ValueOrDash(candidate.DisplayDescription));

            string costText = V3MainInstallCandidateText.GetCandidateMaterialsText(candidate);
            GUI.color = candidate.CanInstall ? V3MainText.YellowColor : ShuttleUIStyle.MutedTextColor;
            Rect costRect = new Rect(glyphRect.xMax + 10f, rect.y + 75f, rect.width - 156f, 18f);
            ShuttleUILayout.SafeLabel(
                costRect,
                this.text.FitLabelText(costText, costRect.width));

            Rect buttonRect = new Rect(rect.xMax - 118f, rect.yMax - 34f, 108f, 26f);
            Action installAction = candidate.CanInstall && this.onInstall != null
                ? delegate
                {
                    this.onInstall(candidate);
                    this.Close();
                }
                : (Action)null;
            this.panel.DrawButton(
                buttonRect,
                this.text.Tr(this.replacing
                    ? "CT_Shuttle_UI_ReplaceModule"
                    : "CT_Shuttle_UI_ModuleReplace_Install"),
                V3MainInstallCandidateText.BuildTooltip(candidate),
                installAction,
                ShuttleUIButtonKind.Primary);
            this.text.AddTooltip(rect, V3MainInstallCandidateText.BuildTooltip(candidate));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawFooter(Rect rect)
        {
            Rect closeRect = new Rect(rect.xMax - 104f, rect.y + 5f, 104f, 30f);
            this.panel.DrawButton(
                closeRect,
                this.text.Tr("CT_Shuttle_UI_ModuleReplace_Close"),
                null,
                delegate { this.Close(); });
        }

        private void RefreshCandidatesIfNeeded()
        {
            if (this.candidateProvider == null || this.getResearchFingerprint == null)
            {
                return;
            }

            int currentFingerprint = this.ReadResearchFingerprint();
            if (currentFingerprint == this.lastResearchFingerprint)
            {
                return;
            }

            this.lastResearchFingerprint = currentFingerprint;
            this.candidates = this.BuildCandidatesFromProvider();
        }

        private List<V3MainInstallCandidateModel> BuildCandidatesFromProvider()
        {
            if (this.candidateProvider == null)
            {
                return new List<V3MainInstallCandidateModel>();
            }

            try
            {
                return this.candidateProvider() ?? new List<V3MainInstallCandidateModel>();
            }
            catch (Exception exception)
            {
                Log.Warning("[CeleTech Shuttle] V3 module install candidate refresh failed: " + exception.Message);
                return new List<V3MainInstallCandidateModel>();
            }
        }

        private int ReadResearchFingerprint()
        {
            if (this.getResearchFingerprint == null)
            {
                return int.MinValue;
            }

            try
            {
                return this.getResearchFingerprint();
            }
            catch (Exception exception)
            {
                Log.Warning("[CeleTech Shuttle] V3 module install fingerprint read failed: " + exception.Message);
                return this.lastResearchFingerprint;
            }
        }

        private Color GetStatusColor(V3MainInstallCandidateModel candidate)
        {
            if (candidate == null)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            switch (candidate.Status)
            {
                case V3MainInstallCandidateStatus.Available:
                    return V3MainText.GreenColor;
                case V3MainInstallCandidateStatus.Locked:
                case V3MainInstallCandidateStatus.Constructing:
                    return V3MainText.YellowColor;
                default:
                    return ShuttleUIStyle.MutedTextColor;
            }
        }

        private string GetFallbackGlyph(V3MainInstallCandidateModel candidate)
        {
            string label = candidate != null ? candidate.DisplayLabel : null;
            return string.IsNullOrEmpty(label) ? "M" : label.Substring(0, 1).ToUpperInvariant();
        }
    }
}

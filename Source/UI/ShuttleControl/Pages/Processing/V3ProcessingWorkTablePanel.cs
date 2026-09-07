using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingWorkTablePanel
    {
        private const float WorkbenchCardHeight = 88f;
        private const float PanelContentTopOffset = 44f;

        private readonly V3ProcessingText text;
        private readonly V3ProcessingPanelDrawer panelDrawer;

        internal V3ProcessingWorkTablePanel(V3ProcessingText text)
        {
            this.text = text;
            this.panelDrawer = new V3ProcessingPanelDrawer(text);
        }

        internal void Draw(
            Rect rect,
            V3ProcessingPageModel model,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            this.panelDrawer.DrawPanelTitle(rect, ShuttleUIText.Tr("CT_Shuttle_Processing_WorkbenchSelectionPanel"));
            Rect listRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3ProcessingReadModel processingModel =
                model != null ? model.ProcessingModel : null;
            List<V3ProcessingWorkbenchModel> workbenches =
                processingModel != null ? processingModel.Workbenches : null;
            if (workbenches == null || workbenches.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_NoModules"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, workbenches.Count * WorkbenchCardHeight));
            Widgets.BeginScrollView(listRect, ref state.WorkbenchScroll, viewRect);
            try
            {
                for (int i = 0; i < workbenches.Count; i++)
                {
                    V3ProcessingWorkbenchModel workbench = workbenches[i];
                    Rect rowRect = new Rect(0f, i * WorkbenchCardHeight, viewRect.width, WorkbenchCardHeight - 6f);
                    bool selected = processingModel != null &&
                        processingModel.SelectedWorkbench != null &&
                        workbench != null &&
                        workbench.Id == processingModel.SelectedWorkbench.Id;
                    this.DrawWorkbenchCard(rowRect, workbench, selected, state, context);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawWorkbenchCard(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            bool selected,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            if (workbench == null)
            {
                return;
            }

            this.panelDrawer.DrawCardBackground(
                rect,
                selected,
                false,
                ShuttleUIStyle.RightBottomCardColor);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 11f, 40f, 40f);
            this.panelDrawer.DrawIcon(iconRect, context, workbench.IconKey, "W");

            Rect statusRect = new Rect(rect.xMax - 72f, rect.y + 8f, 64f, 22f);
            this.panelDrawer.DrawStatusBadge(
                statusRect,
                workbench.StatusLabel,
                this.text.GetWorkbenchStatusColor(workbench.StatusKey));

            Rect titleRect = new Rect(iconRect.xMax + 8f, rect.y + 7f, statusRect.x - iconRect.xMax - 12f, 21f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                titleRect,
                workbench.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                workbench.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 31f, rect.width - 90f, 17f),
                workbench.KindLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                workbench.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 57f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_CurrentRecipe") + ": " +
                this.text.CurrentRecipeSummary(workbench) +
                "   " + ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Draw") + ": " +
                this.text.FormatWatts(workbench.PowerWatts),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                workbench.Tooltip,
                TextAnchor.MiddleLeft);

            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                if (state.SelectedWorkbenchId != workbench.Id)
                {
                    state.SelectedWorkbenchId = workbench.Id;
                    state.BillScroll = Vector2.zero;
                    state.RecipeScroll = Vector2.zero;
                }
            }

            this.text.AddTooltip(rect, workbench.Tooltip);
        }

    }
}

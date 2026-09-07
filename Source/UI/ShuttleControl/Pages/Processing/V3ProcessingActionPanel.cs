using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingActionPanel
    {
        private const float BillCardHeight = 176f;
        private const float RecipeCardHeight = 116f;
        private const float PanelContentTopOffset = 44f;
        private const float RecipeSearchHeight = 36f;
        private const float RecipeSearchGap = 8f;
        private const string RecipeSearchControlName =
            "CT_Shuttle_Processing_RecipeSearch";

        private readonly V3ProcessingText text;
        private readonly V3ProcessingPanelDrawer panelDrawer;

        internal V3ProcessingActionPanel(V3ProcessingText text)
        {
            this.text = text;
            this.panelDrawer = new V3ProcessingPanelDrawer(text);
        }

        internal void DrawBills(
            Rect rect,
            V3ProcessingPageModel model,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ProcessingQueueDraw))
            {
                this.DrawBillsCore(rect, model, state, context);
            }
        }

        private void DrawBillsCore(
            Rect rect,
            V3ProcessingPageModel model,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            this.panelDrawer.DrawPanelTitle(rect, ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_ProductionQueue"));
            Rect listRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3ProcessingWorkbenchModel selected = model != null && model.ProcessingModel != null
                ? model.ProcessingModel.SelectedWorkbench
                : null;
            if (selected == null)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    ShuttleUIText.Tr("CT_Shuttle_Processing_SelectWorkbenchPrompt"));
                return;
            }

            if (selected.Bills == null || selected.Bills.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    ShuttleUIText.Tr("CT_Shuttle_Processing_NoCurrentRecipePrompt"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, selected.Bills.Count * BillCardHeight));
            Widgets.BeginScrollView(listRect, ref state.BillScroll, viewRect);
            try
            {
                int firstIndex;
                int lastIndexExclusive;
                V3ProcessingPanelDrawer.GetVisibleRowRange(
                    state.BillScroll,
                    listRect.height,
                    selected.Bills.Count,
                    BillCardHeight,
                    out firstIndex,
                    out lastIndexExclusive);
                for (int i = firstIndex; i < lastIndexExclusive; i++)
                {
                    V3ProcessingBillModel bill = selected.Bills[i];
                    Rect rowRect = new Rect(0f, i * BillCardHeight, viewRect.width, BillCardHeight - 7f);
                    this.DrawBillCard(rowRect, selected, bill, context);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        internal void DrawRecipes(
            Rect rect,
            V3ProcessingPageModel model,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ProcessingRecipeDraw))
            {
                this.DrawRecipesCore(rect, model, state, context);
            }
        }

        private void DrawRecipesCore(
            Rect rect,
            V3ProcessingPageModel model,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            this.panelDrawer.DrawPanelTitle(rect, ShuttleUIText.Tr("CT_Shuttle_Processing_SelectableRecipesPanel"));
            V3ProcessingWorkbenchModel selected = model != null && model.ProcessingModel != null
                ? model.ProcessingModel.SelectedWorkbench
                : null;
            if (selected == null)
            {
                Rect emptyRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
                this.panelDrawer.DrawEmptyPanelMessage(
                    emptyRect,
                    ShuttleUIText.Tr("CT_Shuttle_Processing_SelectWorkbenchPrompt"));
                return;
            }

            if (selected.Recipes == null || selected.Recipes.Count == 0)
            {
                Rect emptyRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
                this.panelDrawer.DrawEmptyPanelMessage(
                    emptyRect,
                    ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_NoRecipes"));
                return;
            }

            Rect searchRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            searchRect.height = Mathf.Min(RecipeSearchHeight, searchRect.height);
            Rect listRect = this.panelDrawer.GetPanelInnerRect(
                rect,
                PanelContentTopOffset + RecipeSearchHeight + RecipeSearchGap);

            this.RefreshRecipeFilterIfNeeded(selected, state);
            if (this.DrawRecipeSearch(
                searchRect,
                selected,
                state,
                state.FilteredRecipeIndices.Count,
                selected.Recipes.Count))
            {
                this.RefreshRecipeFilter(selected, state);
            }

            if (state.FilteredRecipeIndices.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    string.IsNullOrEmpty(
                        V3ProcessingRecipeSearchFilter.GetSearchNeedle(
                            state.RecipeSearchText))
                        ? ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_NoRecipes")
                        : ShuttleUIText.Tr("CT_Shuttle_Processing_SearchNoResults"));
                return;
            }

            this.DrawFilteredRecipeRows(listRect, selected, state, context);
        }

        private void DrawBillCard(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            V3ProcessingBillModel bill,
            ShuttlePageDrawContext context)
        {
            if (bill == null)
            {
                return;
            }

            this.panelDrawer.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.LeftColumnCardColor);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 38f, 38f);
            this.panelDrawer.DrawIcon(iconRect, context, bill.IconKey, "B");

            Rect statusRect = new Rect(rect.xMax - 82f, rect.y + 8f, 74f, 22f);
            this.panelDrawer.DrawStatusBadge(statusRect, bill.StatusLabel, this.text.GetStatusColor(bill.StatusKey));

            Rect titleRect = new Rect(iconRect.xMax + 8f, rect.y + 7f, statusRect.x - iconRect.xMax - 12f, 21f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                titleRect,
                bill.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                bill.Tooltip,
                TextAnchor.MiddleLeft);

            float buttonY = rect.yMax - 32f;
            Rect textAreaRect = new Rect(rect.x + 8f, rect.y + 52f, rect.width - 16f, buttonY - rect.y - 58f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 31f, rect.width - 64f, 17f),
                ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Type") + ": " + bill.RepeatModeLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                bill.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textAreaRect.x, textAreaRect.y, textAreaRect.width, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Progress") + ": " + bill.ProgressText,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                bill.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textAreaRect.x, textAreaRect.y + 20f, textAreaRect.width, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Ingredients") + ": " + bill.IngredientStatusLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                bill.Tooltip,
                TextAnchor.MiddleLeft);

            Rect noteRect = new Rect(textAreaRect.x, textAreaRect.y + 42f, textAreaRect.width, textAreaRect.height - 42f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                noteRect,
                bill.IsPlaceholder
                    ? ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_CurrentPolicyNote")
                    : ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_QueuedOrder"),
                GameFont.Tiny,
                GameFont.Tiny,
                bill.IsPlaceholder ? V3ProcessingText.YellowColor : ShuttleUIStyle.MutedTextColor,
                bill.Tooltip,
                TextAnchor.MiddleLeft);

            if (Mouse.IsOver(rect))
            {
                // Buttons are built only for the hovered visible row. This avoids paying GUI
                // control cost for an entire queue on every repaint.
                this.DrawOrderButtons(
                    new Rect(rect.x + 8f, buttonY, rect.width - 16f, 24f),
                    workbench,
                    bill,
                    context);
            }

            this.text.AddTooltip(rect, bill.Tooltip);
        }

        private void DrawOrderButtons(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            V3ProcessingBillModel bill,
            ShuttlePageDrawContext context)
        {
            IShuttleProcessingUIActions actions = this.GetProcessingActions(context);
            ShuttleProcessingOrderActionTarget target =
                V3ProcessingActionTargetFactory.CreateOrderTarget(workbench, bill);
            if (actions == null || target == null)
            {
                return;
            }

            float gap = 4f;
            float smallWidth = 30f;
            float remaining = rect.width - smallWidth * 4f - gap * 5f;
            float wideWidth = Mathf.Max(58f, remaining / 2f);
            float x = rect.x;
            for (int i = 0; i < 6; i++)
            {
                float width = i == 3 || i == 4 ? wideWidth : smallWidth;
                bool enabled = !bill.IsActive;
                if (i == 0)
                {
                    enabled = bill.CanMoveUp;
                }
                else if (i == 1)
                {
                    enabled = bill.CanMoveDown;
                }
                else if (i == 2)
                {
                    enabled = true;
                }

                Rect buttonRect = new Rect(x, rect.y, width, rect.height);
                bool clicked = this.panelDrawer.DrawButton(
                    buttonRect,
                    this.GetOrderButtonLabel(i, bill),
                    enabled,
                    i == 5,
                    this.GetOrderButtonTooltip(i, bill));
                if (clicked && enabled)
                {
                    if (i == 0)
                    {
                        actions.MoveOrder(target, -1);
                    }
                    else if (i == 1)
                    {
                        actions.MoveOrder(target, 1);
                    }
                    else if (i == 2)
                    {
                        actions.SetOrderSuspended(target, !bill.Suspended);
                    }
                    else if (i == 3)
                    {
                        actions.OpenOrderProductionPolicyDialog(target);
                    }
                    else if (i == 4)
                    {
                        actions.OpenOrderIngredientFilterDialog(target);
                    }
                    else
                    {
                        actions.RemoveOrder(target);
                    }
                }

                x += width + gap;
            }
        }

        private string GetOrderButtonLabel(int index, V3ProcessingBillModel bill)
        {
            if (index == 0)
            {
                return "↑";
            }

            if (index == 1)
            {
                return "↓";
            }

            if (index == 2)
            {
                return bill != null && bill.Suspended ? "▶" : "Ⅱ";
            }

            if (index == 3)
            {
                return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_ConfigPolicy");
            }

            if (index == 4)
            {
                return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_IngredientFilter");
            }

            return "×";
        }

        private string GetOrderButtonTooltip(int index, V3ProcessingBillModel bill)
        {
            if (index == 0)
            {
                return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_MoveOrderUp");
            }

            if (index == 1)
            {
                return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_MoveOrderDown");
            }

            if (index == 2)
            {
                return ShuttleUIText.Tr(
                    bill != null && bill.Suspended
                        ? "CT_Shuttle_AutoWorkTable_ResumeOrder"
                        : "CT_Shuttle_AutoWorkTable_SuspendOrder");
            }

            if (index == 3)
            {
                return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_ProductionPolicyTooltip");
            }

            if (index == 4)
            {
                return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_IngredientFilterTooltip");
            }

            return ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_RemoveOrder");
        }

        private void DrawRecipeCard(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            V3ProcessingRecipeModel recipe,
            ShuttlePageDrawContext context)
        {
            if (recipe == null)
            {
                return;
            }

            bool disabled = !recipe.CanAdd;
            this.panelDrawer.DrawCardBackground(rect, false, disabled, ShuttleUIStyle.CardColor);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 38f, 38f);
            this.panelDrawer.DrawProductIcon(
                iconRect,
                recipe.ProductIcon,
                context,
                recipe.IconKey,
                "R");

            Rect addRect = new Rect(rect.xMax - 70f, rect.y + 8f, 62f, 24f);
            this.DrawSelectRecipeButton(addRect, workbench, recipe, context);

            Rect titleRect = new Rect(iconRect.xMax + 8f, rect.y + 7f, addRect.x - iconRect.xMax - 12f, 21f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                titleRect,
                recipe.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                recipe.Tooltip,
                TextAnchor.MiddleLeft);

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 31f, rect.width - 88f, 17f),
                recipe.AvailabilityLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                this.text.GetRecipeAvailabilityColor(recipe.AvailabilityKey),
                recipe.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 54f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Product") + ": " +
                recipe.ProductLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                recipe.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 72f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Ingredients") + ": " +
                recipe.IngredientSummary,
                GameFont.Tiny,
                GameFont.Tiny,
                this.text.GetRecipeIngredientColor(recipe),
                recipe.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 90f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Processing_Label_Work") + ": " +
                recipe.WorkAmountLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                recipe.Tooltip,
                TextAnchor.MiddleLeft);

            this.text.AddTooltip(rect, recipe.Tooltip);
        }

        private void DrawClearRecipeButton(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            ShuttlePageDrawContext context)
        {
            IShuttleProcessingUIActions actions = this.GetProcessingActions(context);
            ShuttleProcessingWorkbenchActionTarget workbenchTarget =
                V3ProcessingActionTargetFactory.CreateWorkbenchTarget(workbench);
            bool enabled = actions != null &&
                workbenchTarget != null &&
                actions.CanClearSelectedRecipe(workbenchTarget);
            string tooltip = actions != null && workbenchTarget != null
                ? actions.GetClearSelectedRecipeTooltip(workbenchTarget)
                : ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            if (this.panelDrawer.DrawButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_ClearRecipe"),
                enabled,
                true,
                tooltip))
            {
                if (enabled)
                {
                    actions.ClearSelectedRecipe(workbenchTarget);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }
        }

        private void DrawPauseResumeButton(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            ShuttlePageDrawContext context)
        {
            IShuttleProcessingUIActions actions = this.GetProcessingActions(context);
            ShuttleProcessingWorkbenchActionTarget workbenchTarget =
                V3ProcessingActionTargetFactory.CreateWorkbenchTarget(workbench);
            bool enabled = actions != null &&
                workbenchTarget != null &&
                actions.CanSetPaused(workbenchTarget);
            string tooltip = actions != null && workbenchTarget != null
                ? actions.GetPausedTooltip(workbenchTarget)
                : ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            bool paused = workbench != null && workbench.Paused;
            string label = paused
                ? ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_Resume")
                : ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_Pause");
            if (this.panelDrawer.DrawButton(
                rect,
                label,
                enabled,
                false,
                tooltip))
            {
                if (enabled)
                {
                    actions.SetPaused(workbenchTarget, !paused);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }
        }

        private void DrawProductionPolicyButton(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            ShuttlePageDrawContext context)
        {
            IShuttleProcessingUIActions actions = this.GetProcessingActions(context);
            ShuttleProcessingWorkbenchActionTarget workbenchTarget =
                V3ProcessingActionTargetFactory.CreateWorkbenchTarget(workbench);
            bool enabled = actions != null &&
                workbenchTarget != null &&
                actions.CanOpenProductionPolicy(workbenchTarget);
            string tooltip = actions != null && workbenchTarget != null
                ? actions.GetProductionPolicyTooltip(workbenchTarget)
                : ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            if (this.panelDrawer.DrawButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_ConfigPolicy"),
                enabled,
                false,
                tooltip))
            {
                if (enabled)
                {
                    actions.OpenProductionPolicyDialog(workbenchTarget);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }
        }

        private void DrawSelectRecipeButton(
            Rect rect,
            V3ProcessingWorkbenchModel workbench,
            V3ProcessingRecipeModel recipe,
            ShuttlePageDrawContext context)
        {
            IShuttleProcessingUIActions actions = this.GetProcessingActions(context);
            ShuttleProcessingWorkbenchActionTarget workbenchTarget =
                V3ProcessingActionTargetFactory.CreateWorkbenchTarget(workbench);
            ShuttleProcessingRecipeActionTarget recipeTarget =
                V3ProcessingActionTargetFactory.CreateRecipeTarget(recipe);
            bool enabled = actions != null &&
                workbenchTarget != null &&
                recipeTarget != null &&
                actions.CanSetSelectedRecipe(workbenchTarget, recipeTarget);
            string tooltip = actions != null &&
                workbenchTarget != null &&
                recipeTarget != null
                    ? actions.GetSetSelectedRecipeTooltip(workbenchTarget, recipeTarget)
                    : ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_RecipeInfoMissing");
            string label = ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_AddToQueue");
            if (this.panelDrawer.DrawButton(
                rect,
                label,
                enabled,
                false,
                tooltip))
            {
                if (enabled)
                {
                    actions.SetSelectedRecipe(workbenchTarget, recipeTarget);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }
        }

        private IShuttleProcessingUIActions GetProcessingActions(ShuttlePageDrawContext context)
        {
            if (context != null && context.ProcessingPageContext != null)
            {
                return context.ProcessingPageContext.ProcessingActions;
            }

            return null;
        }

        private bool DrawRecipeSearch(
            Rect rect,
            V3ProcessingWorkbenchModel selected,
            V3ProcessingPageState state,
            int matchedCount,
            int totalCount)
        {
            if (state == null)
            {
                return false;
            }

            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.LeftColumnCardColor);

            float clearWidth = 58f;
            float filterWidth = 72f;
            float countWidth = 74f;
            Rect clearRect = new Rect(
                rect.xMax - clearWidth - 6f,
                rect.y + 4f,
                clearWidth,
                rect.height - 8f);
            Rect countRect = new Rect(
                clearRect.x - countWidth - 6f,
                rect.y + 5f,
                countWidth,
                rect.height - 10f);
            Rect filterRect = new Rect(
                countRect.x - filterWidth - 6f,
                rect.y + 4f,
                filterWidth,
                rect.height - 8f);
            Rect fieldRect = new Rect(
                rect.x + 8f,
                rect.y + 4f,
                Mathf.Max(40f, filterRect.x - rect.x - 14f),
                rect.height - 8f);

            string before = state.RecipeSearchText ?? string.Empty;
            GUI.SetNextControlName(RecipeSearchControlName);
            state.RecipeSearchText = Widgets.TextField(fieldRect, before);
            bool changed = !string.Equals(
                before,
                state.RecipeSearchText ?? string.Empty,
                StringComparison.Ordinal);
            if (changed)
            {
                state.RecipeScroll = Vector2.zero;
                state.LastRecipeSearchText = null;
            }

            if (string.IsNullOrEmpty(state.RecipeSearchText))
            {
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    new Rect(
                        fieldRect.x + 6f,
                        fieldRect.y + 8f,
                        fieldRect.width - 12f,
                        18f),
                    ShuttleUILayout.FitSingleLineLabelText(
                        ShuttleUIText.Tr("CT_Shuttle_Processing_SearchPlaceholder"),
                        fieldRect.width - 12f));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                countRect,
                ShuttleUIText.Tr(
                    "CT_Shuttle_Processing_SearchCount",
                    matchedCount,
                    totalCount),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleRight);

            bool facetActive = state.RecipeAvailabilityFacet != "All" ||
                state.RecipeProductCategoryFacet != "All" ||
                state.RecipeSourceBenchFacet != "All";
            if (this.panelDrawer.DrawButton(
                filterRect,
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_Filter"),
                true,
                facetActive,
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_FilterTooltip")))
            {
                this.OpenRecipeFacetMenu(selected, state);
            }

            bool canClear = !string.IsNullOrEmpty(state.RecipeSearchText);
            if (this.panelDrawer.DrawButton(
                clearRect,
                ShuttleUIText.Tr("CT_Shuttle_Processing_SearchClear"),
                canClear,
                false,
                ShuttleUIText.Tr("CT_Shuttle_Processing_SearchClearTooltip")) &&
                canClear)
            {
                state.RecipeSearchText = string.Empty;
                state.RecipeScroll = Vector2.zero;
                state.LastRecipeSearchText = null;
                GUI.FocusControl(null);
                changed = true;
            }

            ShuttleUITooltip.Tip(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Processing_SearchTooltip"));
            return changed;
        }

        private void OpenRecipeFacetMenu(
            V3ProcessingWorkbenchModel selected,
            V3ProcessingPageState state)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption(
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_FilterReset"),
                delegate
                {
                    state.RecipeAvailabilityFacet = "All";
                    state.RecipeProductCategoryFacet = "All";
                    state.RecipeSourceBenchFacet = "All";
                    state.LastRecipeFacetKey = null;
                }));
            this.AddFacetOption(options, state, "Availability", "All");
            this.AddFacetOption(options, state, "Availability", "Ready");
            this.AddFacetOption(options, state, "Availability", "Missing");

            string[] categories = new string[]
            {
                "All", "Materials", "Weapons", "Apparel", "Medicine", "Food", "Other"
            };
            for (int i = 0; i < categories.Length; i++)
            {
                this.AddFacetOption(options, state, "Category", categories[i]);
            }

            HashSet<string> sourceDefs = new HashSet<string>();
            options.Add(new FloatMenuOption(
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_FilterSourceAll"),
                delegate
                {
                    state.RecipeSourceBenchFacet = "All";
                    state.LastRecipeFacetKey = null;
                }));
            if (selected != null && selected.Recipes != null)
            {
                for (int i = 0; i < selected.Recipes.Count; i++)
                {
                    V3ProcessingRecipeModel recipe = selected.Recipes[i];
                    if (recipe == null ||
                        string.IsNullOrEmpty(recipe.SourceBenchDefName) ||
                        sourceDefs.Contains(recipe.SourceBenchDefName))
                    {
                        continue;
                    }

                    sourceDefs.Add(recipe.SourceBenchDefName);
                    string sourceDefName = recipe.SourceBenchDefName;
                    string sourceLabel = recipe.SourceBenchLabel;
                    options.Add(new FloatMenuOption(
                        ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_FilterSource", sourceLabel),
                        delegate
                        {
                            state.RecipeSourceBenchFacet = sourceDefName;
                            state.LastRecipeFacetKey = null;
                        }));
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void AddFacetOption(
            List<FloatMenuOption> options,
            V3ProcessingPageState state,
            string facet,
            string value)
        {
            options.Add(new FloatMenuOption(
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_Filter" + facet + value),
                delegate
                {
                    if (facet == "Availability")
                    {
                        state.RecipeAvailabilityFacet = value;
                    }
                    else
                    {
                        state.RecipeProductCategoryFacet = value;
                    }

                    state.LastRecipeFacetKey = null;
                }));
        }

        private void DrawFilteredRecipeRows(
            Rect listRect,
            V3ProcessingWorkbenchModel selected,
            V3ProcessingPageState state,
            ShuttlePageDrawContext context)
        {
            if (selected == null ||
                selected.Recipes == null ||
                state == null ||
                state.FilteredRecipeIndices == null)
            {
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(
                    listRect.height,
                    state.FilteredRecipeIndices.Count * RecipeCardHeight));
            Widgets.BeginScrollView(listRect, ref state.RecipeScroll, viewRect);
            try
            {
                int firstIndex;
                int lastIndexExclusive;
                V3ProcessingPanelDrawer.GetVisibleRowRange(
                    state.RecipeScroll,
                    listRect.height,
                    state.FilteredRecipeIndices.Count,
                    RecipeCardHeight,
                    out firstIndex,
                    out lastIndexExclusive);
                for (int i = firstIndex; i < lastIndexExclusive; i++)
                {
                    int sourceIndex = state.FilteredRecipeIndices[i];
                    if (sourceIndex < 0 || sourceIndex >= selected.Recipes.Count)
                    {
                        continue;
                    }

                    V3ProcessingRecipeModel recipe = selected.Recipes[sourceIndex];
                    Rect rowRect = new Rect(
                        0f,
                        i * RecipeCardHeight,
                        viewRect.width,
                        RecipeCardHeight - 7f);
                    this.DrawRecipeCard(rowRect, selected, recipe, context);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void RefreshRecipeFilterIfNeeded(
            V3ProcessingWorkbenchModel selected,
            V3ProcessingPageState state)
        {
            if (state == null)
            {
                return;
            }

            string searchText = state.RecipeSearchText ?? string.Empty;
            string workbenchId = selected != null ? selected.Id : null;
            if (!string.IsNullOrEmpty(state.LastRecipeSearchWorkbenchId) &&
                !string.Equals(
                    state.LastRecipeSearchWorkbenchId,
                    workbenchId,
                    StringComparison.Ordinal))
            {
                // Source-bench facets are local to the selected module's catalog. Keeping a
                // stale Def name here would make another module appear to have no recipes.
                state.RecipeSourceBenchFacet = "All";
            }
            string facetKey = state.RecipeAvailabilityFacet + "|" +
                state.RecipeProductCategoryFacet + "|" +
                state.RecipeSourceBenchFacet;
            int recipeCount = selected != null && selected.Recipes != null
                ? selected.Recipes.Count
                : 0;
            if (state.LastRecipeSearchCount == recipeCount &&
                string.Equals(
                    state.LastRecipeSearchWorkbenchId,
                    workbenchId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    state.LastRecipeSearchText,
                    searchText,
                    StringComparison.Ordinal) &&
                string.Equals(state.LastRecipeFacetKey, facetKey, StringComparison.Ordinal))
            {
                return;
            }

            this.RefreshRecipeFilter(selected, state);
        }

        private void RefreshRecipeFilter(
            V3ProcessingWorkbenchModel selected,
            V3ProcessingPageState state)
        {
            if (state == null)
            {
                return;
            }

            List<V3ProcessingRecipeModel> recipes =
                selected != null ? selected.Recipes : null;
            V3ProcessingRecipeSearchFilter.BuildFilteredRows(
                recipes,
                state.RecipeSearchText,
                state.RecipeAvailabilityFacet,
                state.RecipeProductCategoryFacet,
                state.RecipeSourceBenchFacet,
                state.FilteredRecipeIndices);
            state.LastRecipeSearchWorkbenchId = selected != null ? selected.Id : null;
            state.LastRecipeSearchText = state.RecipeSearchText ?? string.Empty;
            state.LastRecipeSearchCount = recipes != null ? recipes.Count : 0;
            state.LastRecipeFacetKey = state.RecipeAvailabilityFacet + "|" +
                state.RecipeProductCategoryFacet + "|" +
                state.RecipeSourceBenchFacet;
        }
    }
}

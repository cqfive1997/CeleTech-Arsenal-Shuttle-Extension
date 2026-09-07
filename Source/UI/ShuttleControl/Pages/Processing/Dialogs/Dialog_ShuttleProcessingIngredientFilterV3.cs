using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing.Dialogs
{
    /// <summary>
    /// Per-order ingredient restriction editor. The working copy is detached from RuntimeState;
    /// only Apply crosses the command boundary.
    /// </summary>
    internal sealed class Dialog_ShuttleProcessingIngredientFilterV3 : Window
    {
        private const float HeaderHeight = 58f;
        private const float ToolbarHeight = 34f;
        private const float FooterHeight = 38f;
        private const float Gap = 10f;

        private readonly IShuttleProcessingIngredientFilterActions actions;
        private readonly ShuttleProcessingOrderActionTarget order;
        private readonly RecipeDef recipeDef;
        private readonly ShuttleProcessingIngredientFilterSource filterSource;
        private readonly ShuttleProcessingIngredientFilterDrawer filterDrawer =
            new ShuttleProcessingIngredientFilterDrawer();
        private ThingFilter workingFilter;

        internal Dialog_ShuttleProcessingIngredientFilterV3(
            IShuttleProcessingIngredientFilterActions actions,
            ShuttleProcessingOrderActionTarget order)
        {
            this.actions = actions;
            this.order = order;
            this.recipeDef = order != null && !string.IsNullOrEmpty(order.RecipeDefName)
                ? DefDatabase<RecipeDef>.GetNamedSilentFail(order.RecipeDefName)
                : null;
            this.filterSource = new ShuttleProcessingIngredientFilterSource(this.recipeDef);
            this.workingFilter = order != null && order.HasCustomIngredientFilter
                ? AutoWorkTableIngredientFilterUtility.CopyOrNull(order.IngredientFilter)
                : AutoWorkTableIngredientFilterUtility.BuildRecipeDefault(this.recipeDef);
            if (this.workingFilter == null)
            {
                this.workingFilter = new ThingFilter();
            }

            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = true;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(720f, 760f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 10f, inRect.width - 24f, HeaderHeight);
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

        private void DrawHeader(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleV3DialogStyle.HeaderColor);
            ShuttleV3DialogLayout.DrawRectBorder(
                rect,
                ShuttleV3DialogStyle.BorderColor,
                ShuttleV3DialogMetrics.ThickBorder);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 27f),
                V3ProcessingDisplayTextResolver.Tr("CT_Shuttle_AutoWorkTable_IngredientFilter"),
                GameFont.Medium,
                GameFont.Small,
                ShuttleV3DialogStyle.HeaderTitleTextColor);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 34f, rect.width - 24f, 18f),
                this.order != null ? this.order.Label : string.Empty,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor);
        }

        private void DrawBody(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false);
            ShuttleV3DialogLayout.DrawSectionHeader(
                new Rect(rect.x, rect.y, rect.width, 30f),
                V3ProcessingDisplayTextResolver.Tr("CT_Shuttle_AutoWorkTable_SelectableIngredients"));

            Rect innerRect = new Rect(rect.x + 10f, rect.y + 38f, rect.width - 20f, rect.height - 48f);
            if (this.filterSource.ConfigurableIngredientCount <= 0)
            {
                this.DrawNoConfigurableIngredients(innerRect);
                return;
            }

            Rect toolbarRect = new Rect(innerRect.x, innerRect.y, innerRect.width, ToolbarHeight);
            Rect filterRect = new Rect(
                innerRect.x,
                toolbarRect.yMax + Gap,
                innerRect.width,
                innerRect.yMax - toolbarRect.yMax - Gap);
            this.DrawToolbar(toolbarRect);
            this.filterDrawer.Draw(
                filterRect,
                this.workingFilter,
                this.filterSource.ConfigurableIngredients,
                this.recipeDef);
        }

        private void DrawToolbar(Rect rect)
        {
            float countWidth = Mathf.Max(150f, rect.width - 236f);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, countWidth, rect.height),
                V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_SelectableIngredientCount",
                    this.filterSource.ConfigurableIngredientCount),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor);

            Rect allowRect = new Rect(rect.xMax - 110f, rect.y + 2f, 110f, 30f);
            Rect clearRect = new Rect(allowRect.x - 8f - 110f, rect.y + 2f, 110f, 30f);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                clearRect,
                V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_DisallowAllIngredients"),
                true,
                ShuttleV3DialogButtonKind.Danger,
                null))
            {
                this.workingFilter.SetDisallowAll();
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                allowRect,
                V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_AllowAllIngredients"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.workingFilter.SetAllowAll(this.filterSource.ConfigurableIngredients, false);
            }
        }

        private void DrawNoConfigurableIngredients(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleV3DialogStyle.SettingsPreviewCardColor);
            Rect accentRect = new Rect(rect.x, rect.y, 4f, rect.height);
            Widgets.DrawBoxSolid(accentRect, ShuttleV3DialogStyle.YellowStatusColor);

            Text.Font = GameFont.Small;
            GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, 28f),
                V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_NoSelectableIngredients"));

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            string detail = V3ProcessingDisplayTextResolver.Tr(
                "CT_Shuttle_AutoWorkTable_FixedIngredientsCannotFilter");
            string fixedIngredientSummary = this.filterSource.GetFixedIngredientSummary(
                V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_IngredientListSeparator"));
            if (!string.IsNullOrEmpty(fixedIngredientSummary))
            {
                detail += "\n\n" + V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_FixedIngredientsList",
                    fixedIngredientSummary);
            }

            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 14f, rect.y + 50f, rect.width - 28f, rect.height - 64f),
                detail);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawFooter(Rect footerRect)
        {
            float gap = 8f;
            float width = 124f;
            Rect applyRect = new Rect(footerRect.xMax - width, footerRect.y + 4f, width, 30f);
            Rect cancelRect = new Rect(applyRect.x - gap - width, footerRect.y + 4f, width, 30f);
            Rect defaultRect = new Rect(cancelRect.x - gap - width, footerRect.y + 4f, width, 30f);

            if (ShuttleV3DialogLayout.DrawDialogButton(
                defaultRect,
                V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_UseRecipeDefault"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                if (this.actions != null && this.actions.SaveIngredientFilter(this.order, null, true))
                {
                    this.Close(false);
                }
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                cancelRect,
                V3ProcessingDisplayTextResolver.Tr("CT_Shuttle_UI_Cancel"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.Close(false);
            }

            bool hasConfigurableIngredients = this.filterSource.ConfigurableIngredientCount > 0;
            if (ShuttleV3DialogLayout.DrawDialogButton(
                applyRect,
                V3ProcessingDisplayTextResolver.Tr("CT_Shuttle_UI_Apply"),
                hasConfigurableIngredients,
                ShuttleV3DialogButtonKind.Primary,
                hasConfigurableIngredients
                    ? null
                    : V3ProcessingDisplayTextResolver.Tr(
                        "CT_Shuttle_AutoWorkTable_FixedIngredientsCannotFilter")))
            {
                if (this.actions != null &&
                    this.actions.SaveIngredientFilter(this.order, this.workingFilter, false))
                {
                    this.Close(false);
                }
            }
        }
    }
}

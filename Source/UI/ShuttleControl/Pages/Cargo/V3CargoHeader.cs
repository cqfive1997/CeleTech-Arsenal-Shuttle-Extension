using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoHeader
    {
        private readonly V3CargoText text;
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();
        private readonly List<ShuttleHeaderMetricSpec> emptyHeaderMetrics =
            new List<ShuttleHeaderMetricSpec>();
        private List<V3CargoCategorySummaryModel> cachedOrderedSummarySource;
        private List<V3CargoCategorySummaryModel> cachedOrderedCategorySummaries;

        internal V3CargoHeader(V3CargoText text)
        {
            this.text = text;
        }

        internal void Draw(
            Rect rect,
            V3CargoPageModel model,
            V3CargoPageState state,
            ShuttlePageDrawContext context)
        {
            if (model == null || context == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                model.ControlModel ?? new ShuttleControlReadModel();
            ShuttleCargoSnapshot cargoSnapshot =
                model.CargoSnapshot ?? new ShuttleCargoSnapshot();
            ShuttleWeaponBayReadModel weaponBayModel =
                context.ReadModels != null && context.ReadModels.WeaponBayModel != null
                    ? context.ReadModels.WeaponBayModel
                    : ShuttleWeaponBayReadModel.Empty;

            ShuttleControlHeaderDrawer.Draw(
                rect,
                this.identitySpecBuilder.GetTitle(controlModel),
                this.identitySpecBuilder.GetLogo(context),
                this.statusSpecBuilder.Build(
                    controlModel,
                    cargoSnapshot,
                    weaponBayModel,
                    ShuttlePageHeaderShieldMode.AggregateAnyMaxHitPoints),
                this.commandSpecBuilder.Build(context),
                new ShuttleHeaderRibbonSpec(
                    ShuttleHeaderProgressSpec.Empty,
                    this.emptyHeaderMetrics));

            this.DrawCategoryRow(
                ShuttleControlHeaderLayout.GetSummaryRibbonRect(rect),
                model.CargoPageModel,
                state,
                context);
        }

        internal void DrawCategoryRow(
            Rect rect,
            V3CargoPageReadModel cargoModel,
            V3CargoPageState state,
            ShuttlePageDrawContext context)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false, ShuttleUIStyle.CardColor);
            if (cargoModel == null || cargoModel.CategorySummaries == null)
            {
                return;
            }

            List<V3CargoCategorySummaryModel> summaries =
                this.GetOrderedCategorySummaries(cargoModel.CategorySummaries);
            int count = summaries.Count;
            if (count <= 0)
            {
                return;
            }

            float gap = 4f;
            float cellWidth = (rect.width - 16f - (gap * (count - 1))) / count;
            for (int i = 0; i < count; i++)
            {
                V3CargoCategorySummaryModel summary = summaries[i];
                if (summary == null)
                {
                    continue;
                }

                Rect cellRect = new Rect(
                    rect.x + 8f + ((cellWidth + gap) * i),
                    rect.y + 6f,
                    cellWidth,
                    rect.height - 12f);
                this.DrawCategoryCell(cellRect, summary, state);
            }

            IShuttleTutorialTargetService tutorialTargets =
                context != null && context.CargoPageContext != null
                    ? context.CargoPageContext.TutorialTargets
                    : null;
            if (tutorialTargets != null)
            {
                tutorialTargets.Register(
                    ShuttleTutorialTargetIds.LoadingCategorySummaryPanel,
                    rect);
            }
        }

        private void DrawCategoryCell(
            Rect rect,
            V3CargoCategorySummaryModel summary,
            V3CargoPageState state)
        {
            bool selected = state != null && state.SelectedCargoCategory == summary.Category;
            Color color = this.text.GetCategoryColor(summary.Category);
            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.12f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.52f),
                ShuttleUIStyle.ThinBorder);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 4f, rect.yMax - 2f, Mathf.Max(0f, rect.width - 8f), 1f),
                ShuttleUIStyle.WithAlpha(color, 0.62f));
            if (selected)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 4f, rect.yMax - 4f, Mathf.Max(0f, rect.width - 8f), 2f),
                    color);
            }

            string label = this.text.GetCategoryLabel(summary.Category, summary.Label);
            string cardLabel = this.text.GetCategoryCardLabel(summary.Category, summary.Label, rect.width - 12f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 7f, rect.y + 4f, rect.width - 12f, 17f),
                cardLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                selected ? color : ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleCenter);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 7f, rect.y + 22f, rect.width - 12f, 18f),
                summary.ThingCount.ToString(),
                GameFont.Small,
                GameFont.Tiny,
                summary.ThingCount > 0 ? color : ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleCenter);
            if (!Mouse.IsOver(rect))
            {
                return;
            }

            this.text.AddTooltip(
                rect,
                label + ": " + this.text.FormatStackCount(summary.ThingCount));
            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedCargoCategory = selected
                    ? V3CargoCategory.None
                    : summary.Category;
                state.CargoStatsScroll = Vector2.zero;
                state.CargoStackListScroll = Vector2.zero;
            }
        }

        private List<V3CargoCategorySummaryModel> GetOrderedCategorySummaries(
            List<V3CargoCategorySummaryModel> summaries)
        {
            if (object.ReferenceEquals(this.cachedOrderedSummarySource, summaries) &&
                this.cachedOrderedCategorySummaries != null)
            {
                return this.cachedOrderedCategorySummaries;
            }

            List<V3CargoCategorySummaryModel> ordered =
                summaries != null
                    ? new List<V3CargoCategorySummaryModel>(summaries)
                    : new List<V3CargoCategorySummaryModel>();
            ordered.Sort(delegate(V3CargoCategorySummaryModel left, V3CargoCategorySummaryModel right)
            {
                V3CargoCategory leftCategory = left != null ? left.Category : V3CargoCategory.None;
                V3CargoCategory rightCategory = right != null ? right.Category : V3CargoCategory.None;
                int orderCompare = this.text.GetCategoryOrder(leftCategory)
                    .CompareTo(this.text.GetCategoryOrder(rightCategory));
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                return leftCategory.CompareTo(rightCategory);
            });
            this.cachedOrderedSummarySource = summaries;
            this.cachedOrderedCategorySummaries = ordered;
            return ordered;
        }
    }
}

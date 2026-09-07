using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoStackDetailPanel
    {
        private const float PanelContentTopOffset = 44f;
        private const float StatHeaderHeight = 30f;
        private const float StatRowStride = 34f;
        private const float StatRowHeight = 30f;

        private readonly V3CargoText text;
        private readonly V3CargoPanelDrawer panel;

        internal V3CargoStackDetailPanel(
            V3CargoText text,
            V3CargoPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3CargoPageModel pageModel,
            V3CargoPageState state,
            ShuttlePageDrawContext context,
            V3CargoVisibleStackModel selectedStack)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Cargo_ItemStats"));
            Rect inner = this.panel.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3CargoCategory selected = state != null
                ? state.SelectedCargoCategory
                : V3CargoCategory.None;

            if (selected == V3CargoCategory.None)
            {
                this.DrawCategoryPrompt(inner);
                return;
            }

            V3CargoPageReadModel cargoModel = pageModel != null ? pageModel.CargoPageModel : null;
            V3CargoCategorySummaryModel selectedSummary =
                this.FindCategorySummary(cargoModel, selected);
            this.DrawSelectedCategoryHeader(inner, selected, selectedSummary);

            List<V3CargoStatLineModel> stats = cargoModel != null ? cargoModel.ItemStats : null;
            int matchingCount = this.CountStats(stats, selected);
            if (matchingCount <= 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    new Rect(inner.x, inner.y + StatHeaderHeight, inner.width, inner.height - StatHeaderHeight),
                    this.text.Tr("CT_Shuttle_Cargo_NoItems"));
                return;
            }

            Rect listRect = new Rect(
                inner.x,
                inner.y + StatHeaderHeight,
                inner.width,
                Mathf.Max(0f, inner.height - StatHeaderHeight));
            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, matchingCount * StatRowStride));
            Widgets.BeginScrollView(listRect, ref state.CargoStatsScroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            GetVisibleRowRange(
                state.CargoStatsScroll,
                listRect.height,
                matchingCount,
                StatRowStride,
                out firstIndex,
                out lastIndexExclusive);
            int drawn = 0;
            for (int i = 0; stats != null && i < stats.Count; i++)
            {
                V3CargoStatLineModel stat = stats[i];
                if (stat == null || stat.Category != selected)
                {
                    continue;
                }

                if (drawn >= lastIndexExclusive)
                {
                    break;
                }

                if (drawn >= firstIndex)
                {
                    this.DrawStatRow(
                        new Rect(0f, drawn * StatRowStride, viewRect.width, StatRowHeight),
                        stat);
                }

                drawn++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCategoryPrompt(Rect inner)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(inner.x + 8f, inner.y + 10f, inner.width - 16f, 24f),
                this.text.Tr("CT_Shuttle_Cargo_NoCategorySelected"),
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                null,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(inner.x + 8f, inner.y + 38f, inner.width - 16f, 24f),
                this.text.Tr("CT_Shuttle_Cargo_CategoryPrompt"),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleLeft);
        }

        private void DrawSelectedCategoryHeader(
            Rect inner,
            V3CargoCategory selected,
            V3CargoCategorySummaryModel selectedSummary)
        {
            int selectedCount = selectedSummary != null ? selectedSummary.ThingCount : 0;
            string selectedLabel = this.text.GetCategoryLabel(
                selected,
                selectedSummary != null ? selectedSummary.Label : null);
            Color accent = this.text.GetCategoryColor(selected);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(inner.x + 6f, inner.y + 2f, inner.width - 12f, 22f),
                selectedLabel + "  " + this.text.FormatStackCount(selectedCount),
                GameFont.Small,
                GameFont.Tiny,
                accent,
                selectedLabel,
                TextAnchor.MiddleLeft);
        }

        private void DrawStatRow(Rect rect, V3CargoStatLineModel stat)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.078f, 0.094f, 0.118f, 0.92f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            Rect iconRect = new Rect(rect.x + 5f, rect.y + 4f, 22f, 22f);
            if (ShouldDrawCargoIcons())
            {
                this.panel.DrawThingIcon(iconRect, this.ToStack(stat));
            }
            else
            {
                this.DrawFallbackIcon(iconRect, stat);
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(iconRect.xMax + 7f, rect.y + 6f, rect.width - 96f, 18f),
                stat.Label,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                stat.Label,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.xMax - 80f, rect.y + 6f, 74f, 18f),
                this.text.FormatStackCount(stat.ThingCount),
                GameFont.Tiny,
                GameFont.Tiny,
                V3CargoText.AccentColor,
                stat.Label,
                TextAnchor.MiddleLeft);
            this.text.AddTooltip(
                rect,
                delegate
                {
                    return stat.Label + ": " +
                        this.text.FormatStackCount(stat.ThingCount);
                });
        }

        private void DrawFallbackIcon(Rect rect, V3CargoStatLineModel stat)
        {
            Widgets.DrawBoxSolid(rect, V3CargoText.MutedCardColor);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(rect, this.GetCargoStatFallbackText(stat));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        private static bool ShouldDrawCargoIcons()
        {
            ShuttleEffectiveSettings settings = CeleTechShuttleMod.EffectiveSettings;
            return settings == null ||
                (settings.UseCargoIcons && !settings.ForceCompactCargoList);
        }

        private string GetCargoStatFallbackText(V3CargoStatLineModel stat)
        {
            if (stat == null)
            {
                return "?";
            }

            if (stat.Category == V3CargoCategory.Food)
            {
                return "F";
            }

            if (stat.Category == V3CargoCategory.Weapons)
            {
                return "W";
            }

            if (stat.Category == V3CargoCategory.Apparel)
            {
                return "A";
            }

            if (stat.Category == V3CargoCategory.Buildings)
            {
                return "B";
            }

            return "I";
        }

        private int CountStats(List<V3CargoStatLineModel> stats, V3CargoCategory selected)
        {
            int count = 0;
            for (int i = 0; stats != null && i < stats.Count; i++)
            {
                if (stats[i] != null && stats[i].Category == selected)
                {
                    count++;
                }
            }

            return count;
        }

        private V3CargoCategorySummaryModel FindCategorySummary(
            V3CargoPageReadModel model,
            V3CargoCategory category)
        {
            if (model == null || model.CategorySummaries == null)
            {
                return null;
            }

            for (int i = 0; i < model.CategorySummaries.Count; i++)
            {
                V3CargoCategorySummaryModel summary = model.CategorySummaries[i];
                if (summary != null && summary.Category == category)
                {
                    return summary;
                }
            }

            return null;
        }

        private V3CargoStackCardModel ToStack(V3CargoStatLineModel stat)
        {
            V3CargoStackCardModel stack = new V3CargoStackCardModel();
            if (stat == null)
            {
                return stack;
            }

            stack.Label = stat.Label;
            stack.Category = stat.Category;
            stack.DisplayThing = stat.DisplayThing;
            stack.StackCount = stat.ThingCount;
            return stack;
        }

        private static void GetVisibleRowRange(
            Vector2 scroll,
            float viewportHeight,
            int rowCount,
            float rowStride,
            out int firstIndex,
            out int lastIndexExclusive)
        {
            if (rowCount <= 0 || rowStride <= 0f)
            {
                firstIndex = 0;
                lastIndexExclusive = 0;
                return;
            }

            firstIndex = Mathf.Clamp(Mathf.FloorToInt(scroll.y / rowStride), 0, rowCount - 1);
            int visibleRows = Mathf.CeilToInt(viewportHeight / rowStride) + 2;
            lastIndexExclusive = Mathf.Clamp(firstIndex + visibleRows, firstIndex, rowCount);
        }
    }
}

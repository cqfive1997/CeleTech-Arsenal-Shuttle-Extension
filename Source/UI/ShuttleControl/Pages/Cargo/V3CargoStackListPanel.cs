using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoStackListPanel
    {
        private const float PanelContentTopOffset = 58f;
        private const float CargoBayCardHeight = 164f;
        private const float CargoGridGap = 8f;
        private const int CargoGridColumns = 3;

        private readonly V3CargoText text;
        private readonly V3CargoPanelDrawer panel;
        private readonly V3CargoSelection selection;
        private readonly Dictionary<V3CargoBayCardModel, int> bayItemCountCache =
            new Dictionary<V3CargoBayCardModel, int>();
        private V3CargoPageReadModel cachedCargoModel;

        internal V3CargoStackListPanel(
            V3CargoText text,
            V3CargoPanelDrawer panel,
            V3CargoSelection selection)
        {
            this.text = text;
            this.panel = panel;
            this.selection = selection;
        }

        internal void Draw(
            Rect rect,
            V3CargoPageModel pageModel,
            V3CargoPageState state,
            ShuttlePageDrawContext context,
            List<V3CargoVisibleStackModel> visibleStacks)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Cargo_Overview"));
            V3CargoPageReadModel cargoModel = pageModel != null ? pageModel.CargoPageModel : null;
            this.DrawRefrigeratedCapacitySummary(rect, cargoModel);
            Rect inner = this.panel.GetPanelInnerRect(
                rect,
                cargoModel != null && cargoModel.HasRefrigeratedCapacitySummary
                    ? PanelContentTopOffset
                    : 44f);
            if (cargoModel == null || cargoModel.Bays == null || cargoModel.Bays.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(inner, this.text.Tr("CT_Shuttle_Cargo_NoBays"));
                return;
            }

            this.TrackCargoModel(cargoModel);

            float contentWidth = Mathf.Max(0f, inner.width - 16f);
            float cardWidth = (contentWidth - (CargoGridGap * (CargoGridColumns - 1))) / CargoGridColumns;
            float contentHeight = this.CalculateCargoGridContentHeight(cargoModel.Bays);
            Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(inner.height, contentHeight));

            Widgets.BeginScrollView(inner, ref state.CargoStackListScroll, viewRect);
            float viewportTop = state.CargoStackListScroll.y;
            float viewportBottom = viewportTop + inner.height;
            float y = 0f;
            int index = 0;
            while (index < cargoModel.Bays.Count)
            {
                int rowCount = Mathf.Min(CargoGridColumns, cargoModel.Bays.Count - index);
                if (y > viewportBottom + CargoBayCardHeight + CargoGridGap)
                {
                    break;
                }

                if (y + CargoBayCardHeight >= viewportTop - CargoBayCardHeight)
                {
                    for (int column = 0; column < rowCount; column++)
                    {
                        Rect cardRect = new Rect(
                            column * (cardWidth + CargoGridGap),
                            y,
                            cardWidth,
                            CargoBayCardHeight);
                        this.DrawBayCard(
                            cardRect,
                            cargoModel.Bays[index + column],
                            pageModel,
                            context);
                    }
                }

                y += CargoBayCardHeight + CargoGridGap;
                index += rowCount;
            }

            Widgets.EndScrollView();
        }

        private float CalculateCargoGridContentHeight(List<V3CargoBayCardModel> bays)
        {
            int count = bays != null ? bays.Count : 0;
            if (count <= 0)
            {
                return CargoBayCardHeight;
            }

            int rows = Mathf.CeilToInt(count / (float)CargoGridColumns);
            return (rows * CargoBayCardHeight) + (Mathf.Max(0, rows - 1) * CargoGridGap);
        }

        private void DrawBayCard(
            Rect rect,
            V3CargoBayCardModel bay,
            V3CargoPageModel pageModel,
            ShuttlePageDrawContext context)
        {
            if (bay == null)
            {
                return;
            }

            bool hovered = Mouse.IsOver(rect);
            Color accent = bay.IsRefrigerated ? V3CargoText.ColdColor : V3CargoText.AccentColor;
            Color cardColor = bay.IsRefrigerated
                ? new Color(0.060f, 0.135f, 0.172f, 0.94f)
                : V3CargoText.CardColor;
            this.panel.DrawCardBackground(rect, false, !bay.IsEnabled, cardColor);
            if (bay.IsRefrigerated)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            }

            this.DrawBayHeader(rect, bay, pageModel, context, accent, hovered);
            this.DrawBaySummary(rect, bay);
            this.text.AddTooltip(rect, delegate
            {
                return this.BuildBayTooltip(bay);
            });
        }

        private void DrawBayHeader(
            Rect rect,
            V3CargoBayCardModel bay,
            V3CargoPageModel pageModel,
            ShuttlePageDrawContext context,
            Color accent,
            bool hovered)
        {
            float buttonWidth = 52f;
            float buttonHeight = 24f;
            float buttonGap = 6f;
            Rect detailsRect = new Rect(rect.xMax - 10f - buttonWidth, rect.y + 6f, buttonWidth, buttonHeight);
            Rect configRect = new Rect(detailsRect.x - buttonGap - buttonWidth, detailsRect.y, buttonWidth, buttonHeight);
            float actionStartX = bay.IsRecoveryBay ? detailsRect.x : configRect.x;
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 7f, actionStartX - rect.x - 18f, 22f);

            ShuttleUILayout.DrawFittedSingleLineLabel(
                titleRect,
                bay.Label,
                GameFont.Small,
                GameFont.Tiny,
                bay.IsRefrigerated ? V3CargoText.ColdColor : Color.white,
                !string.IsNullOrEmpty(bay.HeaderTooltip)
                    ? bay.HeaderTooltip
                    : bay.Label + "\n" + this.text.ValueOrDash(bay.StatusText),
                TextAnchor.MiddleLeft);

            if (!hovered)
            {
                return;
            }

            if (!bay.IsRecoveryBay && this.panel.DrawButton(
                configRect,
                this.text.Tr("CT_Shuttle_Cargo_Action_Config"),
                true,
                V3CargoText.AccentColor,
                bay.IsRefrigerated
                    ? this.text.Tr("CT_Shuttle_Cargo_ColdConfigTooltip")
                    : this.text.Tr("CT_Shuttle_Cargo_FilterConfigTooltip")))
            {
                IShuttleCargoBayUIActions actions = GetBayActions(context);
                if (actions != null)
                {
                    actions.OpenBayConfig(V3CargoActionTargetFactory.CreateBay(bay));
                }
            }

            if (this.panel.DrawButton(
                detailsRect,
                this.text.Tr("CT_Shuttle_Cargo_Action_Details"),
                true,
                V3CargoText.AccentColor,
                this.text.Tr("CT_Shuttle_Cargo_DetailsTooltip")))
            {
                IShuttleCargoBayUIActions actions = GetBayActions(context);
                if (actions != null)
                {
                    actions.OpenBayContents(
                        V3CargoActionTargetFactory.CreateBay(bay),
                        V3CargoActionTargetFactory.CreatePageContext(
                            pageModel != null ? pageModel.CargoPageModel : null));
                }
            }
        }

        private void DrawBaySummary(Rect rect, V3CargoBayCardModel bay)
        {
            Rect filterRect = new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, 18f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                filterRect,
                !string.IsNullOrEmpty(bay.FilterDisplayText)
                    ? bay.FilterDisplayText
                    : this.text.ValueOrDash(bay.FilterSummary),
                GameFont.Tiny,
                GameFont.Tiny,
                bay.IsFilterSummaryPlaceholder
                    ? V3CargoText.YellowColor
                    : ShuttleUIStyle.MutedTextColor,
                bay.FilterSummary,
                TextAnchor.MiddleLeft);

            Rect massRect = new Rect(rect.x + 10f, rect.y + 54f, rect.width - 20f, 17f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                massRect,
                !string.IsNullOrEmpty(bay.MassSummary)
                    ? bay.MassSummary
                    : this.text.FormatKgPair(bay.UsedMassKg, bay.CapacityKg),
                GameFont.Tiny,
                GameFont.Tiny,
                bay.CapacityKg > 0f && bay.UsedMassKg > bay.CapacityKg
                    ? V3CargoText.RedColor
                    : V3CargoText.YellowColor,
                null,
                TextAnchor.MiddleLeft);

            if (!bay.IsRefrigerated)
            {
                this.panel.DrawMeter(
                    new Rect(rect.x + 10f, rect.y + 76f, rect.width - 20f, 9f),
                    bay.UsedMassKg,
                    bay.CapacityKg,
                    V3CargoText.YellowColor);
            }

            Rect summaryRect = new Rect(rect.x + 10f, rect.y + 96f, rect.width - 20f, 42f);
            Widgets.DrawBoxSolid(summaryRect, new Color(0.045f, 0.055f, 0.070f, 0.72f));
            ShuttleUILayout.DrawRectBorder(
                summaryRect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);

            string countMassSummary =
                this.ResolveCountMassSummary(bay);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(summaryRect.x + 8f, summaryRect.y + 5f, summaryRect.width - 16f, 17f),
                countMassSummary,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(summaryRect.x + 8f, summaryRect.y + 23f, summaryRect.width - 16f, 16f),
                this.ResolveBayModeSummary(bay),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleLeft);
        }

        private void DrawRefrigeratedCapacitySummary(
            Rect rect,
            V3CargoPageReadModel cargoModel)
        {
            if (cargoModel == null ||
                !cargoModel.HasRefrigeratedCapacitySummary ||
                string.IsNullOrEmpty(cargoModel.RefrigeratedCapacitySummary))
            {
                return;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 14f, rect.y + 31f, rect.width - 28f, 20f),
                cargoModel.RefrigeratedCapacitySummary,
                GameFont.Tiny,
                GameFont.Tiny,
                V3CargoText.ColdColor,
                cargoModel.RefrigeratedCapacityTooltip,
                TextAnchor.MiddleLeft);
        }

        private string ResolveCountMassSummary(V3CargoBayCardModel bay)
        {
            if (bay == null)
            {
                return "-";
            }

            if (!string.IsNullOrEmpty(bay.CountMassSummary))
            {
                return bay.CountMassSummary;
            }

            int itemCount = this.CountBayItemsCached(bay);
            string counts = !string.IsNullOrEmpty(bay.CountSummary)
                ? bay.CountSummary
                : this.text.Tr(
                    "CT_Shuttle_Cargo_CountPairFormat",
                    bay.Items != null ? bay.Items.Count : 0,
                    itemCount);
            string massSummary = !string.IsNullOrEmpty(bay.MassSummary)
                ? bay.MassSummary
                : this.text.FormatKgPair(bay.UsedMassKg, bay.CapacityKg);
            return counts + " / " + massSummary;
        }

        private void TrackCargoModel(V3CargoPageReadModel cargoModel)
        {
            if (object.ReferenceEquals(this.cachedCargoModel, cargoModel))
            {
                return;
            }

            this.cachedCargoModel = cargoModel;
            this.bayItemCountCache.Clear();
        }

        private int CountBayItemsCached(V3CargoBayCardModel bay)
        {
            if (bay == null)
            {
                return 0;
            }

            int cachedCount;
            if (this.bayItemCountCache.TryGetValue(bay, out cachedCount))
            {
                return cachedCount;
            }

            int count = this.CountBayItems(bay);
            this.bayItemCountCache[bay] = count;
            return count;
        }

        private int CountBayItems(V3CargoBayCardModel bay)
        {
            int count = 0;
            for (int i = 0; bay != null && bay.Items != null && i < bay.Items.Count; i++)
            {
                V3CargoStackCardModel stack = bay.Items[i];
                if (stack != null && stack.StackCount > 0)
                {
                    count += stack.StackCount;
                }
            }

            return count;
        }

        private string ResolveBayModeSummary(V3CargoBayCardModel bay)
        {
            if (bay == null)
            {
                return this.text.Tr("CT_Shuttle_Cargo_Unknown");
            }

            if (!string.IsNullOrEmpty(bay.ModeSummary))
            {
                return bay.ModeSummary;
            }

            if (bay.IsRefrigerated)
            {
                string mode = !bay.IsEnabled
                    ? this.text.Tr("CT_Shuttle_Cargo_RefrigeratedModuleDisabled")
                    : bay.AutoTransferEnabled
                    ? this.text.Tr("CT_Shuttle_Cargo_AutoTransferOn")
                    : this.text.Tr("CT_Shuttle_Cargo_AutoTransferOff");
                string filter = bay.HasCustomAutoTransferFilter
                    ? this.text.Tr("CT_Shuttle_Cargo_FilterCustom")
                    : this.text.Tr("CT_Shuttle_Cargo_FilterDefault");
                return mode + " / " + filter;
            }

            return this.text.ValueOrDash(bay.FilterSummary);
        }

        private string BuildBayTooltip(V3CargoBayCardModel bay)
        {
            if (bay == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(bay.Tooltip))
            {
                return bay.Tooltip;
            }

            return this.text.ValueOrDash(bay.Label) + "\n" +
                this.text.ValueOrDash(bay.StatusText) + "\n" +
                this.ResolveBayModeSummary(bay) + "\n" +
                (!string.IsNullOrEmpty(bay.MassSummary)
                    ? bay.MassSummary
                    : this.text.FormatKgPair(bay.UsedMassKg, bay.CapacityKg));
        }

        private static IShuttleCargoBayUIActions GetBayActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.CargoPageContext != null
                ? context.CargoPageContext.CargoBayActions
                : null;
        }
    }
}

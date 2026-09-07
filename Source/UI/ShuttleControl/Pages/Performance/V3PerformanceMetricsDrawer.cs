using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Performance
{
    internal sealed class V3PerformanceMetricsDrawer
    {
        private const float TabHeight = 34f;
        private const float HeaderHeight = 28f;
        private const float RowHeight = 38f;

        internal void Draw(
            Rect rect,
            ShuttlePerformanceDashboardReadModel model,
            V3PerformancePageState state)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false);
            if (state == null)
            {
                return;
            }

            Rect moduleTab = new Rect(rect.x + 8f, rect.y + 6f, 150f, TabHeight);
            Rect controllerTab = new Rect(moduleTab.xMax + 6f, moduleTab.y, 170f, TabHeight);
            if (ShuttleUILayout.DrawButton(
                    moduleTab,
                    ShuttleUIText.Tr("CT_Shuttle_Performance_ModuleCosts"),
                    true,
                    state.ShowControllerPhases
                        ? ShuttleUIButtonKind.Normal
                        : ShuttleUIButtonKind.Primary,
                    null))
            {
                state.ShowControllerPhases = false;
                state.MetricScroll = Vector2.zero;
            }

            if (ShuttleUILayout.DrawButton(
                    controllerTab,
                    ShuttleUIText.Tr("CT_Shuttle_Performance_ControllerPhases"),
                    true,
                    state.ShowControllerPhases
                        ? ShuttleUIButtonKind.Primary
                        : ShuttleUIButtonKind.Normal,
                    ShuttleUIText.Tr(
                        "CT_Shuttle_Performance_ControllerPhasesTooltip")))
            {
                state.ShowControllerPhases = true;
                state.MetricScroll = Vector2.zero;
            }

            Rect tableRect = new Rect(
                rect.x + 8f,
                moduleTab.yMax + 7f,
                rect.width - 16f,
                rect.height - TabHeight - 21f);
            this.DrawTable(tableRect, model, state);
        }

        private void DrawTable(
            Rect rect,
            ShuttlePerformanceDashboardReadModel model,
            V3PerformancePageState state)
        {
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, ShuttleUIStyle.MutedCardColor);
            this.DrawHeaderCells(headerRect, state.ShowControllerPhases);

            Rect outRect = new Rect(
                rect.x,
                headerRect.yMax,
                rect.width,
                Mathf.Max(0f, rect.height - HeaderHeight));
            int rowCount = state.ShowControllerPhases
                ? GetControllerCount(model)
                : GetModuleCount(model);
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, outRect.width - 16f),
                Mathf.Max(outRect.height, rowCount * RowHeight));
            Widgets.BeginScrollView(outRect, ref state.MetricScroll, viewRect);
            try
            {
                for (int i = 0; i < rowCount; i++)
                {
                    Rect rowRect = new Rect(
                        0f,
                        i * RowHeight,
                        viewRect.width,
                        RowHeight);
                    if (state.ShowControllerPhases)
                    {
                        this.DrawControllerRow(rowRect, model.ControllerPhases[i], i);
                    }
                    else
                    {
                        this.DrawModuleRow(rowRect, model.ModuleMetrics[i], i);
                    }
                }

                if (rowCount == 0)
                {
                    ShuttleUILayout.DrawFittedSingleLineLabel(
                        new Rect(8f, 12f, viewRect.width - 16f, 28f),
                        ShuttleUIText.Tr("CT_Shuttle_Performance_WaitingForSamples"),
                        GameFont.Small,
                        GameFont.Tiny,
                        ShuttleUIStyle.MutedTextColor,
                        null,
                        TextAnchor.MiddleCenter);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawHeaderCells(Rect rect, bool controllerPhases)
        {
            ColumnLayout columns = ColumnLayout.FromRect(rect);
            DrawCell(columns.Name, controllerPhases
                ? ShuttleUIText.Tr("CT_Shuttle_Performance_Phase")
                : ShuttleUIText.Tr("CT_Shuttle_Performance_Module"));
            DrawCell(columns.System, ShuttleUIText.Tr("CT_Shuttle_Performance_System"));
            DrawCell(
                columns.AverageTick,
                ShuttleUIText.Tr("CT_Shuttle_Performance_MsPerTick"));
            DrawCell(columns.AverageCall, ShuttleUIText.Tr("CT_Shuttle_Performance_MsPerCall"));
            DrawCell(columns.Peak, ShuttleUIText.Tr("CT_Shuttle_Performance_Peak"));
            DrawCell(columns.Calls, ShuttleUIText.Tr("CT_Shuttle_Performance_Calls"));
            DrawCell(columns.Share, ShuttleUIText.Tr("CT_Shuttle_Performance_Share"));
        }

        private void DrawModuleRow(
            Rect rect,
            ShuttlePerformanceModuleMetricReadModel row,
            int index)
        {
            if (row == null)
            {
                return;
            }

            this.DrawRowBackground(rect, index);
            ColumnLayout columns = ColumnLayout.FromRect(rect);
            DrawCell(columns.Name, ResolveText(row.ModuleLabel), row.ModuleInstanceID);
            DrawCell(columns.System, ShortSystemKey(row.RuntimeSystemKey), row.RuntimeSystemKey);
            DrawCell(columns.AverageTick, row.AverageMsPerTick.ToString("0.000"));
            DrawCell(columns.AverageCall, row.AverageMsPerCall.ToString("0.000"));
            DrawCell(columns.Peak, row.PeakMs.ToString("0.000"));
            DrawCell(columns.Calls, row.CallCount.ToString());
            DrawShareCell(columns.Share, row.ControllerShare01);
            TooltipHandler.TipRegion(
                rect,
                ResolveText(row.ModuleLabel) + "\n" +
                row.ModuleInstanceID + "\n" +
                row.RuntimeSystemKey + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Performance_TickInterval") + ": " +
                row.TickInterval);
        }

        private void DrawControllerRow(
            Rect rect,
            ShuttlePerformanceControllerPhaseReadModel row,
            int index)
        {
            if (row == null)
            {
                return;
            }

            this.DrawRowBackground(rect, index);
            ColumnLayout columns = ColumnLayout.FromRect(rect);
            string label = GetControllerSectionLabel(row.SectionKey);
            DrawCell(columns.Name, label, row.SectionKey);
            DrawCell(
                columns.System,
                ShuttleUIText.Tr("CT_Shuttle_Performance_ControllerSystem"));
            DrawCell(columns.AverageTick, row.AverageMsPerTick.ToString("0.000"));
            DrawCell(columns.AverageCall, row.AverageMsPerCall.ToString("0.000"));
            DrawCell(columns.Peak, row.PeakMs.ToString("0.000"));
            DrawCell(columns.Calls, row.CallCount.ToString());
            DrawShareCell(columns.Share, row.ControllerShare01);
        }

        private void DrawRowBackground(Rect rect, int index)
        {
            Widgets.DrawBoxSolid(
                rect,
                index % 2 == 0
                    ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.CardColor, 0.52f)
                    : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, 0.42f));
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.45f));
        }

        private static void DrawCell(Rect rect, string text)
        {
            DrawCell(rect, text, null);
        }

        private static void DrawCell(Rect rect, string text, string tooltip)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 5f, rect.y + 1f, rect.width - 10f, rect.height - 2f),
                ResolveText(text),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.HeaderTitleTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
        }

        private static void DrawShareCell(Rect rect, float share01)
        {
            float clamped = Mathf.Clamp01(share01);
            Rect track = new Rect(
                rect.x + 4f,
                rect.y + 11f,
                Mathf.Max(0f, rect.width - 8f),
                16f);
            Widgets.DrawBoxSolid(track, new Color(0f, 0f, 0f, 0.28f));
            Widgets.DrawBoxSolid(
                new Rect(track.x, track.y, track.width * clamped, track.height),
                clamped >= 0.25f
                    ? ShuttleUIStyle.YellowStatusColor
                    : ShuttleUIStyle.BlueStatusColor);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                track,
                (clamped * 100f).ToString("0.0") + "%",
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                null,
                TextAnchor.MiddleCenter);
        }

        private static int GetModuleCount(ShuttlePerformanceDashboardReadModel model)
        {
            return model != null && model.ModuleMetrics != null
                ? model.ModuleMetrics.Count
                : 0;
        }

        private static int GetControllerCount(ShuttlePerformanceDashboardReadModel model)
        {
            return model != null && model.ControllerPhases != null
                ? model.ControllerPhases.Count
                : 0;
        }

        private static string ShortSystemKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "-";
            }

            int slash = key.LastIndexOf('/');
            string value = slash >= 0 && slash + 1 < key.Length
                ? key.Substring(slash + 1)
                : key;
            return value.Length > 18 ? value.Substring(0, 18) : value;
        }

        private static string GetControllerSectionLabel(string sectionKey)
        {
            string translationKey;
            switch (sectionKey)
            {
                case "runtime/profile bootstrap":
                    translationKey = "CT_Shuttle_Performance_Phase_RuntimeBootstrap";
                    break;
                case "prisoner runtime tick":
                    translationKey = "CT_Shuttle_Performance_Phase_Prisoner";
                    break;
                case "profile/runtime sync":
                    translationKey = "CT_Shuttle_Performance_Phase_ProfileSync";
                    break;
                case "external host/runtime sync":
                    translationKey = "CT_Shuttle_Performance_Phase_ExternalSync";
                    break;
                case "power demand collection":
                    translationKey = "CT_Shuttle_Performance_Phase_PowerDemand";
                    break;
                case "power tick":
                    translationKey = "CT_Shuttle_Performance_Phase_PowerTick";
                    break;
                case "module removal tick":
                    translationKey = "CT_Shuttle_Performance_Phase_Removal";
                    break;
                case "assembly construction tick":
                    translationKey = "CT_Shuttle_Performance_Phase_Construction";
                    break;
                case "medical procedure tick":
                    translationKey = "CT_Shuttle_Performance_Phase_Medical";
                    break;
                case "refrigerated cargo registry reconcile":
                    translationKey = "CT_Shuttle_Performance_Phase_ColdRegistry";
                    break;
                case "cargo broker/cold transfer service":
                    translationKey = "CT_Shuttle_Performance_Phase_CargoServices";
                    break;
                case "global cargo unload tick":
                    translationKey = "CT_Shuttle_Performance_Phase_CargoUnload";
                    break;
                case "passenger boarding intent reconcile":
                    translationKey = "CT_Shuttle_Performance_Phase_Boarding";
                    break;
                case "module runtime dispatcher tick":
                    translationKey = "CT_Shuttle_Performance_Phase_ModuleDispatcher";
                    break;
                case "post module runtime cache invalidation":
                    translationKey = "CT_Shuttle_Performance_Phase_CacheInvalidation";
                    break;
                case "module runtime tick":
                    translationKey = "CT_Shuttle_Performance_Phase_ModuleRuntime";
                    break;
                case "power plant output apply":
                    translationKey = "CT_Shuttle_Performance_Phase_PowerOutput";
                    break;
                case "runtimeState.NotifyTick":
                    translationKey = "CT_Shuttle_Performance_Phase_RuntimeNotify";
                    break;
                default:
                    return ResolveText(sectionKey);
            }

            return ShuttleUIText.Tr(translationKey);
        }

        private static string ResolveText(string text)
        {
            return string.IsNullOrEmpty(text) ? "-" : text;
        }

        private struct ColumnLayout
        {
            internal Rect Name;
            internal Rect System;
            internal Rect AverageTick;
            internal Rect AverageCall;
            internal Rect Peak;
            internal Rect Calls;
            internal Rect Share;

            internal static ColumnLayout FromRect(Rect rect)
            {
                ColumnLayout layout = new ColumnLayout();
                float x = rect.x;
                layout.Name = Next(ref x, rect, 0.27f);
                layout.System = Next(ref x, rect, 0.15f);
                layout.AverageTick = Next(ref x, rect, 0.12f);
                layout.AverageCall = Next(ref x, rect, 0.13f);
                layout.Peak = Next(ref x, rect, 0.11f);
                layout.Calls = Next(ref x, rect, 0.09f);
                layout.Share = new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x), rect.height);
                return layout;
            }

            private static Rect Next(ref float x, Rect source, float fraction)
            {
                float width = source.width * fraction;
                Rect result = new Rect(x, source.y, width, source.height);
                x += width;
                return result;
            }
        }
    }
}

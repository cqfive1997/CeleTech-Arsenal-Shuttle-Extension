using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Performance
{
    internal sealed class V3PerformanceSummaryDrawer
    {
        internal const float Height = 86f;

        internal void Draw(Rect rect, ShuttlePerformanceDashboardReadModel model)
        {
            ShuttlePerformanceDashboardReadModel dashboard = model ??
                ShuttlePerformanceDashboardReadModel.Empty;
            const int cardCount = 5;
            const float gap = 8f;
            float cardWidth = Mathf.Max(
                0f,
                (rect.width - (gap * (cardCount - 1))) / cardCount);

            this.DrawCard(
                new Rect(rect.x, rect.y, cardWidth, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_ControllerTick"),
                FormatMs(dashboard.ControllerAverageMsPerTick),
                ShuttleUIStyle.BlueStatusColor);
            this.DrawCard(
                new Rect(rect.x + (cardWidth + gap), rect.y, cardWidth, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_ModuleRuntime"),
                FormatMs(dashboard.ModuleAverageMsPerTick),
                ShuttleUIStyle.GreenStatusColor);
            this.DrawCard(
                new Rect(rect.x + ((cardWidth + gap) * 2f), rect.y, cardWidth, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_CoreLogic"),
                FormatMs(dashboard.CoreAverageMsPerTick),
                new Color(0.66f, 0.48f, 0.88f, 1f));
            this.DrawCard(
                new Rect(rect.x + ((cardWidth + gap) * 3f), rect.y, cardWidth, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_Peak"),
                FormatMs(dashboard.ControllerPeakMs),
                ShuttleUIStyle.YellowStatusColor);
            this.DrawCard(
                new Rect(rect.x + ((cardWidth + gap) * 4f), rect.y, cardWidth, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_ActiveModules"),
                dashboard.ModuleMetrics != null
                    ? dashboard.ModuleMetrics.Count.ToString()
                    : "0",
                ShuttleUIStyle.GreenStatusColor);
        }

        private void DrawCard(Rect rect, string label, string value, Color accent)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 5f, rect.y + 8f, 3f, rect.height - 16f),
                accent);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 16f, rect.y + 10f, rect.width - 24f, 20f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 16f, rect.y + 37f, rect.width - 24f, 30f),
                value,
                GameFont.Medium,
                GameFont.Small,
                ShuttleUIStyle.HeaderTitleTextColor,
                label + ": " + value,
                TextAnchor.MiddleLeft);
        }

        private static string FormatMs(float value)
        {
            return value.ToString("0.000") + " ms";
        }
    }
}

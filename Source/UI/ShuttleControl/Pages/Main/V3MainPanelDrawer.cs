using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainPanelDrawer
    {
        private readonly V3MainText text;

        internal V3MainPanelDrawer(V3MainText text)
        {
            this.text = text;
        }

        internal void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleUIPanelChrome.DrawPanelTitle(rect, title);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        internal Rect GetPanelInnerRect(Rect rect, float topOffset)
        {
            return ShuttleUIPanelChrome.GetPanelInnerRect(rect, topOffset);
        }

        internal void DrawEmptyPanelMessage(Rect rect, string label)
        {
            ShuttleUIPanelChrome.DrawCenteredEmptyPanelMessage(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        internal void DrawMeterCard(
            Rect rect,
            string label,
            string value,
            float value01,
            Color accent,
            string tooltip)
        {
            this.DrawMeterCard(rect, null, null, label, value, value01, accent, tooltip);
        }

        internal void DrawMeterCard(
            Rect rect,
            Texture2D icon,
            string fallback,
            string label,
            string value,
            float value01,
            Color accent,
            string tooltip)
        {
            V3MainFrameDrawer.DrawMetricFrame(rect, accent);
            this.DrawTinyIndicator(rect, accent, tooltip);

            float textX = rect.x + 10f;
            float textWidth = rect.width - 20f;
            if (icon != null || !string.IsNullOrEmpty(fallback))
            {
                float iconSize = Mathf.Min(34f, Mathf.Max(22f, rect.height - 14f));
                Rect iconRect = new Rect(
                    rect.x + 10f,
                    rect.y + Mathf.Floor((rect.height - iconSize) * 0.5f),
                    iconSize,
                    iconSize);
                this.DrawIconTile(iconRect, icon, fallback, accent, false);
                textX = iconRect.xMax + 8f;
                textWidth = Mathf.Max(0f, rect.xMax - textX - 18f);
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textX, rect.y + 6f, textWidth, 15f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textX, rect.y + 23f, textWidth, 18f),
                value,
                rect.height <= 52f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                accent,
                tooltip,
                TextAnchor.MiddleLeft);
            Rect meterRect = new Rect(rect.x + 10f, rect.yMax - 13f, rect.width - 20f, 6f);
            this.DrawMeter(meterRect, value01, accent);
            this.text.AddTooltip(rect, tooltip);
        }

        internal void DrawIconTile(
            Rect rect,
            Texture2D icon,
            string fallback,
            Color accent,
            bool disabled)
        {
            V3MainIconTileDrawer.DrawHudTile(rect, icon, fallback, accent, disabled);
        }

        internal void DrawFramelessIcon(
            Rect rect,
            Texture2D icon,
            string fallback,
            Color accent,
            bool disabled)
        {
            V3MainIconTileDrawer.DrawFrameless(rect, icon, fallback, accent, disabled, 1.08f);
        }

        internal void DrawSummaryCard(
            Rect rect,
            string label,
            string value,
            Color accent,
            string tooltip)
        {
            V3MainFrameDrawer.DrawSummaryFrame(rect, accent, false, false);
            this.DrawTinyIndicator(rect, accent, tooltip);

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 10f, rect.y + 5f, rect.width - 28f, 14f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 10f, rect.y + 21f, rect.width - 20f, 17f),
                value,
                GameFont.Tiny,
                GameFont.Tiny,
                accent,
                tooltip,
                TextAnchor.MiddleLeft);
            this.text.AddTooltip(rect, tooltip);
        }

        internal bool DrawButton(Rect rect, string label, string tooltip, System.Action action)
        {
            return this.DrawButton(rect, label, tooltip, action, ShuttleUIButtonKind.Normal);
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            string tooltip,
            System.Action action,
            ShuttleUIButtonKind kind)
        {
            if (ShuttleUIActionButtonDrawer.DrawButton(
                rect,
                label,
                action != null,
                kind,
                tooltip) &&
                action != null)
            {
                action();
                return true;
            }

            return false;
        }

        internal void DrawStatusBadge(Rect rect, string label, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawBadge(rect, label, color, true, true, null);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        internal void DrawProgressBar(Rect rect, float value01, string label, Color accent)
        {
            ShuttleUILayout.DrawLinearMeter(rect, value01, accent);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(rect, this.text.FitLabelText(label, rect.width - 6f));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        internal void DrawCardFrame(Rect rect, Color accent, bool selected, bool disabled)
        {
            V3MainFrameDrawer.DrawSummaryFrame(rect, accent, selected, disabled);
        }

        internal void DrawSegmentCardFrame(Rect rect, bool selected, bool hovered, bool menuOpen)
        {
            V3MainFrameDrawer.DrawSegmentCardFrame(rect, selected, hovered, menuOpen);
        }

        internal void DrawModuleRowFrame(
            Rect rect,
            bool installed,
            bool enabled,
            bool selected,
            bool activeOperation)
        {
            V3MainFrameDrawer.DrawModuleRowFrame(rect, installed, enabled, selected, activeOperation);
        }

        internal void DrawSegmentNodeFrame(
            Rect rect,
            bool selected,
            bool hovered,
            bool installed,
            bool optional)
        {
            V3MainFrameDrawer.DrawSegmentNodeFrame(rect, selected, hovered, installed, optional);
        }

        private void DrawMeter(Rect rect, float value01, Color accent)
        {
            ShuttleUILayout.DrawLinearMeter(rect, value01, accent);
        }

        private void DrawTinyIndicator(Rect rect, Color accent, string tooltip)
        {
            Rect indicatorRect = new Rect(rect.xMax - 16f, rect.y + 7f, 7f, 7f);
            ShuttleUIStatusBadgeDrawer.DrawSquareIndicator(indicatorRect, accent, tooltip);
        }
    }
}

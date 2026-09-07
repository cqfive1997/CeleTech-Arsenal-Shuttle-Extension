using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPanelDrawer
    {
        private readonly V3MedicalText text;

        internal V3MedicalPanelDrawer(V3MedicalText text)
        {
            this.text = text;
        }

        internal void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleUIPanelChrome.DrawPanelTitle(rect, title);
        }

        internal Rect GetPanelInnerRect(Rect rect, float topOffset)
        {
            return ShuttleUIPanelChrome.GetPanelInnerRect(rect, topOffset);
        }

        internal void DrawEmptyPanelMessage(Rect rect, string label)
        {
            ShuttleUIPanelChrome.DrawEmptyPanelMessage(rect, label);
        }

        internal void DrawCardBackground(Rect rect, bool selected, bool disabled, Color cardColor)
        {
            Color color = cardColor.a > 0f ? cardColor : V3MedicalText.CardColor;
            if (selected)
            {
                color = V3MedicalText.StrongCardColor;
            }

            if (disabled)
            {
                color = new Color(color.r, color.g, color.b, color.a * 0.58f);
            }

            Widgets.DrawBoxSolid(rect, color);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), V3MedicalText.AccentColor);
            }

            ShuttleUILayout.DrawRectBorder(
                rect,
                selected ? V3MedicalText.AccentColor : ShuttleUIStyle.SubtleBorderColor,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            string tooltip)
        {
            return this.DrawButton(
                rect,
                label,
                enabled,
                danger ? V3MedicalText.RedColor : V3MedicalText.GreenColor,
                tooltip);
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            bool hovered = enabled && Mouse.IsOver(rect);
            Color background = enabled
                ? new Color(accent.r * 0.20f, accent.g * 0.20f, accent.b * 0.20f, 0.94f)
                : ShuttleUIStyle.ActionRowDisabledBgColor;
            if (hovered)
            {
                background = new Color(accent.r * 0.26f, accent.g * 0.26f, accent.b * 0.26f, 0.98f);
            }

            Color border = enabled
                ? new Color(
                    Mathf.Min(1f, accent.r * 0.62f + 0.10f),
                    Mathf.Min(1f, accent.g * 0.62f + 0.10f),
                    Mathf.Min(1f, accent.b * 0.62f + 0.10f),
                    0.88f)
                : ShuttleUIStyle.SubtleBorderColor;
            Color textColor = enabled ? accent : ShuttleUIStyle.MutedTextColor;

            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), textColor);
            this.text.AddTooltip(rect, tooltip);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 10f, rect.y + 1f, Mathf.Max(0f, rect.width - 18f), rect.height - 2f),
                label,
                rect.height <= 24f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                textColor,
                tooltip,
                TextAnchor.MiddleCenter);

            return Widgets.ButtonInvisible(rect);
        }

        internal void DrawStatusBadge(Rect rect, string label, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawBadge(rect, label, color, null);
        }

        internal void DrawMeter(Rect rect, float value01, Color color)
        {
            ShuttleUILayout.DrawLinearMeter(rect, value01, color);
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPanelDrawer
    {
        private readonly V3CrewText text;

        internal V3CrewPanelDrawer(V3CrewText text)
        {
            this.text = text;
        }

        internal void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 10f, rect.y + 9f, rect.width - 20f, 24f),
                title,
                GameFont.Small,
                GameFont.Tiny,
                ShuttleUIStyle.HeaderTitleTextColor,
                title,
                TextAnchor.MiddleLeft);
        }

        internal Rect GetPanelInnerRect(Rect rect, float topOffset)
        {
            return new Rect(rect.x + 8f, rect.y + topOffset, rect.width - 16f, rect.height - topOffset - 8f);
        }

        internal void DrawEmptyPanelMessage(Rect rect, string label)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 48f), V3CrewText.DisabledCardColor);
            ShuttleUILayout.DrawRectBorder(
                new Rect(rect.x, rect.y, rect.width, 48f),
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 10f, rect.y + 14f, rect.width - 20f, 20f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleLeft);
        }

        internal void DrawCardBackground(Rect rect, bool selected, bool disabled, Color cardColor)
        {
            Color color = cardColor.a > 0f ? cardColor : V3CrewText.CardColor;
            if (selected)
            {
                color = V3CrewText.StrongCardColor;
            }

            if (disabled)
            {
                color = new Color(color.r, color.g, color.b, color.a * 0.56f);
            }

            Widgets.DrawBoxSolid(rect, color);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), V3CrewText.AccentColor);
            }

            ShuttleUILayout.DrawRectBorder(
                rect,
                selected ? V3CrewText.AccentColor : ShuttleUIStyle.SubtleBorderColor,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            string tooltip)
        {
            Color background = danger
                ? ShuttleUIStyle.ActionRowDangerBgColor
                : ShuttleUIStyle.ActionRowBgColor;
            Color border = danger
                ? ShuttleUIStyle.ActionRowDangerBorderColor
                : ShuttleUIStyle.ActionRowBorderColor;
            Color accent = danger
                ? ShuttleUIStyle.ActionRowAccentDangerColor
                : ShuttleUIStyle.ActionRowAccentNormalColor;
            Color textColor = danger
                ? ShuttleUIStyle.DangerButtonTextColor
                : ShuttleUIStyle.ActionRowTextColor;
            if (!enabled)
            {
                background = ShuttleUIStyle.ActionRowDisabledBgColor;
                border = ShuttleUIStyle.SubtleBorderColor;
                accent = ShuttleUIStyle.MutedTextColor;
                textColor = ShuttleUIStyle.MutedTextColor;
            }
            else if (Mouse.IsOver(rect))
            {
                background = danger
                    ? ShuttleUIStyle.ActionRowDangerBgHoverColor
                    : ShuttleUIStyle.ActionRowBgHoverColor;
            }

            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                enabled && Mouse.IsOver(rect) ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 5f, rect.height), accent);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 16f, rect.y + 1f, Mathf.Max(0f, rect.width - 24f), rect.height - 2f),
                label,
                rect.height <= 24f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                textColor,
                tooltip,
                TextAnchor.MiddleLeft);
            this.text.AddTooltip(rect, tooltip);
            return Widgets.ButtonInvisible(rect);
        }
    }
}

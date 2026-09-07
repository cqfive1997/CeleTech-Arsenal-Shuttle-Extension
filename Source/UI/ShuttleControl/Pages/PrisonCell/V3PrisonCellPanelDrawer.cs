using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellPanelDrawer
    {
        private readonly V3PrisonCellText text;

        internal V3PrisonCellPanelDrawer(V3PrisonCellText text)
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

        internal void DrawCardBackground(Rect rect, bool selected, bool disabled)
        {
            this.DrawCardBackground(rect, selected, disabled, ShuttleUIStyle.CardColor);
        }

        internal void DrawCardBackground(
            Rect rect,
            bool selected,
            bool disabled,
            Color normalColor)
        {
            ShuttleUILayout.DrawCardBackground(rect, selected, disabled, normalColor);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), V3PrisonCellText.AccentColor);
            }
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            string tooltip)
        {
            ShuttleUIButtonKind kind = danger
                ? ShuttleUIButtonKind.Danger
                : ShuttleUIButtonKind.Primary;
            this.DrawButtonVisual(rect, label, enabled, kind, tooltip);
            return Widgets.ButtonInvisible(rect);
        }

        private void DrawButtonVisual(
            Rect rect,
            string label,
            bool enabled,
            ShuttleUIButtonKind kind,
            string tooltip)
        {
            Color background = kind == ShuttleUIButtonKind.Danger
                ? ShuttleUIStyle.DangerButtonBgColor
                : ShuttleUIStyle.PrimaryButtonBgColor;
            Color border = kind == ShuttleUIButtonKind.Danger
                ? ShuttleUIStyle.DangerButtonBorderColor
                : ShuttleUIStyle.PrimaryButtonBorderColor;
            Color textColor = kind == ShuttleUIButtonKind.Danger
                ? ShuttleUIStyle.DangerButtonTextColor
                : ShuttleUIStyle.PrimaryButtonTextColor;

            bool hovered = enabled && Mouse.IsOver(rect);
            if (!enabled)
            {
                background = ShuttleUIStyle.ButtonDisabledBgColor;
                border = ShuttleUIStyle.SubtleBorderColor;
                textColor = ShuttleUIStyle.MutedTextColor;
            }
            else if (hovered)
            {
                background = ShuttleUIStyle.WithAlpha(background, ShuttleUIStyle.ButtonHoverAlpha);
            }

            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            if (enabled)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), 1f),
                    ShuttleUIStyle.ButtonInnerHighlightColor);
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(6f),
                label,
                rect.height <= 24f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                textColor,
                tooltip,
                TextAnchor.MiddleCenter);
            this.text.AddTooltip(rect, tooltip);
        }
    }
}

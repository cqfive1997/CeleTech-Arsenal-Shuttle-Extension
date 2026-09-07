using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesPanelDrawer
    {
        private readonly V3ExternalModulesText text;

        internal V3ExternalModulesPanelDrawer(V3ExternalModulesText text)
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

        internal void DrawCardBackground(Rect rect, bool selected, bool disabled, Color accent)
        {
            this.DrawCardBackground(rect, selected, disabled, accent, ShuttleUIStyle.CardColor);
        }

        internal void DrawCardBackground(
            Rect rect,
            bool selected,
            bool disabled,
            Color accent,
            Color normalColor)
        {
            ShuttleUILayout.DrawCardBackground(rect, selected, disabled, normalColor);
            if (accent.a > 0f)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            }
        }

        internal void DrawStatusBadge(Rect rect, string label, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawBadge(rect, label, color, label);
        }

        internal void DrawStatusDot(Rect rect, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawSquareIndicator(rect, color, null);
        }

        internal bool DrawButton(Rect rect, string label, bool enabled, Color accent, string tooltip)
        {
            this.DrawButtonVisual(rect, label, enabled, accent, tooltip);
            bool clicked = Widgets.ButtonInvisible(rect);
            if (clicked && !enabled)
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? this.text.Tr("CT_Shuttle_UI_ActionUnavailableYet")
                        : tooltip,
                    false);
                return false;
            }

            return clicked;
        }

        private void DrawButtonVisual(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            Color background = new Color(
                accent.r * 0.20f,
                accent.g * 0.20f,
                accent.b * 0.20f,
                0.88f);
            Color border = new Color(
                Mathf.Min(1f, accent.r * 0.72f + 0.10f),
                Mathf.Min(1f, accent.g * 0.72f + 0.10f),
                Mathf.Min(1f, accent.b * 0.72f + 0.10f),
                0.94f);
            Color textColor = enabled ? accent : ShuttleUIStyle.MutedTextColor;
            bool hovered = enabled && Mouse.IsOver(rect);
            if (!enabled)
            {
                background = ShuttleUIStyle.ButtonDisabledBgColor;
                border = ShuttleUIStyle.SubtleBorderColor;
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
                rect.ContractedBy(4f),
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

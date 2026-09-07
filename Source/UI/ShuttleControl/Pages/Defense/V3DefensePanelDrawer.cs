using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefensePanelDrawer
    {
        private readonly V3DefenseText text;

        internal V3DefensePanelDrawer(V3DefenseText text)
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

        internal void DrawCompactPanelTitle(Rect rect, string title)
        {
            ShuttleUIPanelChrome.DrawCompactPanelTitle(rect, title);
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
        }

        internal void DrawCardBackground(Rect rect, bool selected, bool disabled, Color color)
        {
            Color cardColor = color.a > 0f ? color : V3DefenseText.CardColor;
            if (selected)
            {
                cardColor = V3DefenseText.StrongCardColor;
            }

            if (disabled)
            {
                cardColor = new Color(cardColor.r, cardColor.g, cardColor.b, cardColor.a * 0.58f);
            }

            Widgets.DrawBoxSolid(rect, cardColor);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), V3DefenseText.AccentColor);
            }

            ShuttleUILayout.DrawRectBorder(
                rect,
                selected ? V3DefenseText.AccentColor : ShuttleUIStyle.SubtleBorderColor,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
        }

        internal void DrawIcon(Rect rect, ShuttlePageDrawContext context, string iconKey)
        {
            this.DrawIcon(rect, context, iconKey, "-", 1f);
        }

        internal void DrawIcon(
            Rect rect,
            ShuttlePageDrawContext context,
            string iconKey,
            string fallbackText,
            float scale)
        {
            Texture2D icon = context != null &&
                context.DefensePageContext != null &&
                context.DefensePageContext.Icons != null
                    ? context.DefensePageContext.Icons.GetIcon(iconKey)
                    : null;
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            if (icon != null)
            {
                ShuttleUILayout.DrawIconOrFallback(
                    rect.ContractedBy(4f),
                    icon,
                    fallbackText,
                    scale);
                return;
            }

            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            GUI.color = V3DefenseText.GreenColor;
            Text.Font = rect.height <= 42f ? GameFont.Small : GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            ShuttleUILayout.SafeLabel(rect, string.IsNullOrEmpty(fallbackText) ? "-" : fallbackText);
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        internal void DrawStatusBadge(Rect rect, string label, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawBadge(rect, label, color, null);
        }

        internal bool DrawMainPowerStatusIcon(Rect rect, string statusKey, string tooltip)
        {
            bool enabled;
            bool activeOperation;
            if (!TryResolvePowerStatus(statusKey, out enabled, out activeOperation))
            {
                return false;
            }

            V3MainCardActionRailDrawer.DrawStatusIndicator(
                rect,
                true,
                enabled,
                activeOperation,
                tooltip);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            return true;
        }

        internal void DrawMeter(Rect rect, float value, Color color)
        {
            ShuttleUILayout.DrawLinearMeter(rect, value, color);
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

            bool clicked = Widgets.ButtonInvisible(rect);
            if (!clicked)
            {
                return false;
            }

            if (!enabled)
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? this.text.Tr("CT_Shuttle_Defense_CommandUnavailable")
                        : tooltip,
                    false);
                return false;
            }

            return true;
        }

        private static bool TryResolvePowerStatus(
            string statusKey,
            out bool enabled,
            out bool activeOperation)
        {
            enabled = false;
            activeOperation = false;
            if (statusKey == ShuttleUIText.StatusOnline ||
                statusKey == ShuttleUIText.StatusNormal ||
                statusKey == ShuttleUIText.StatusFull)
            {
                enabled = true;
                return true;
            }

            if (statusKey == ShuttleUIText.StatusHoldFire ||
                statusKey == ShuttleUIText.StatusDormant)
            {
                enabled = false;
                activeOperation = true;
                return true;
            }

            if (statusKey == ShuttleUIText.StatusOffline ||
                statusKey == ShuttleUIText.StatusDisabled ||
                statusKey == ShuttleUIText.StatusUnpowered ||
                statusKey == ShuttleUIText.StatusDown)
            {
                enabled = false;
                return true;
            }

            return false;
        }
    }
}

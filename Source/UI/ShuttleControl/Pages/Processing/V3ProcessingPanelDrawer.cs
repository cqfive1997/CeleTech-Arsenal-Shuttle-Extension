using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingPanelDrawer
    {
        private readonly V3ProcessingText text;

        internal V3ProcessingPanelDrawer(V3ProcessingText text)
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
            this.DrawCardBackground(rect, selected, disabled, V3ProcessingText.CardColor);
        }

        internal void DrawCardBackground(
            Rect rect,
            bool selected,
            bool disabled,
            Color normalColor)
        {
            Color color = selected ? V3ProcessingText.StrongCardColor : V3ProcessingText.CardColor;
            if (!selected)
            {
                color = normalColor;
            }

            ShuttleUILayout.DrawCardBackground(rect, selected, disabled, color);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), V3ProcessingText.AccentColor);
            }
        }

        internal void DrawIcon(Rect rect, ShuttlePageDrawContext context, string iconKey)
        {
            this.DrawIcon(rect, context, iconKey, null);
        }

        internal void DrawIcon(
            Rect rect,
            ShuttlePageDrawContext context,
            string iconKey,
            string fallback)
        {
            Texture2D icon = context != null &&
                context.ProcessingPageContext != null &&
                context.ProcessingPageContext.Icons != null
                ? context.ProcessingPageContext.Icons.GetIcon(iconKey)
                : null;
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            if (icon != null)
            {
                GUI.DrawTexture(rect.ContractedBy(4f), icon, ScaleMode.ScaleToFit);
                return;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(4f),
                fallback,
                GameFont.Small,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleCenter);
        }

        internal void DrawProductIcon(
            Rect rect,
            Texture2D productIcon,
            ShuttlePageDrawContext context,
            string fallbackIconKey,
            string fallback)
        {
            if (productIcon == null)
            {
                this.DrawIcon(rect, context, fallbackIconKey, fallback);
                return;
            }

            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            GUI.DrawTexture(rect.ContractedBy(3f), productIcon, ScaleMode.ScaleToFit);
        }

        internal void DrawStatusBadge(Rect rect, string label, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawBadge(rect, label, color, null);
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            string tooltip)
        {
            Color accent = danger ? V3ProcessingText.RedColor : V3ProcessingText.AccentColor;
            Color background = enabled
                ? new Color(accent.r * 0.20f, accent.g * 0.20f, accent.b * 0.20f, 0.94f)
                : ShuttleUIStyle.ActionRowDisabledBgColor;
            Color border = enabled
                ? new Color(
                    Mathf.Min(1f, accent.r * 0.62f + 0.10f),
                    Mathf.Min(1f, accent.g * 0.62f + 0.10f),
                    Mathf.Min(1f, accent.b * 0.62f + 0.10f),
                    0.86f)
                : ShuttleUIStyle.SubtleBorderColor;
            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(rect, border, ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(5f),
                label,
                rect.height <= 24f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                enabled ? accent : ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleCenter);
            return Widgets.ButtonInvisible(rect);
        }

        internal static void GetVisibleRowRange(
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

            firstIndex = Mathf.Clamp(
                Mathf.FloorToInt(scroll.y / rowStride),
                0,
                rowCount - 1);
            int visibleRows = Mathf.CeilToInt(viewportHeight / rowStride) + 2;
            lastIndexExclusive = Mathf.Clamp(firstIndex + visibleRows, firstIndex, rowCount);
        }
    }
}

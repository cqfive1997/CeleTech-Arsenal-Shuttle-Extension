using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoPanelDrawer
    {
        private readonly V3CargoText text;

        internal V3CargoPanelDrawer(V3CargoText text)
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

        internal void DrawCardBackground(
            Rect rect,
            bool selected,
            bool disabled,
            Color accent)
        {
            Color fill = selected ? ShuttleUIStyle.SelectedColor : V3CargoText.CardColor;
            ShuttleUILayout.DrawCardBackground(rect, selected, disabled, fill);
            if (accent.a > 0f)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            }
        }

        internal void DrawStatusBadge(Rect rect, string label, Color color)
        {
            ShuttleUIStatusBadgeDrawer.DrawBadge(rect, label, color, null);
        }

        internal void DrawMeter(Rect rect, float current, float capacity, Color color)
        {
            ShuttleUILayout.DrawLinearMeter(
                rect,
                capacity > 0f ? current / capacity : 0f,
                color);
        }

        internal void DrawThingIcon(Rect rect, V3CargoStackCardModel stack)
        {
            Widgets.DrawBoxSolid(rect, V3CargoText.MutedCardColor);
            if (stack != null && ShuttleThingIconDrawer.Draw(rect.ContractedBy(2f), stack.DisplayThing))
            {
                ShuttleUILayout.DrawRectBorder(
                    rect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(rect, this.text.GetStackFallbackText(stack));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        internal bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
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
            if (!enabled)
            {
                accent = ShuttleUIStyle.MutedTextColor;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(5f),
                label,
                rect.height <= 24f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                accent,
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
                        ? this.text.Tr("CT_Shuttle_UI_ActionUnavailableYet")
                        : tooltip,
                    false);
                return false;
            }

            return true;
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
            Texture2D icon = this.GetIcon(context, iconKey);
            Widgets.DrawBoxSolid(rect, V3CargoText.MutedCardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);

            Color oldColor = GUI.color;
            GUI.color = Color.white;
            ShuttleUILayout.DrawIconOrFallback(
                rect.ContractedBy(4f),
                icon,
                fallbackText,
                scale);
            GUI.color = oldColor;
        }

        internal void DrawFramelessIcon(
            Rect rect,
            ShuttlePageDrawContext context,
            string iconKey,
            string fallbackText,
            Color fallbackColor,
            bool disabled)
        {
            this.DrawFramelessIcon(
                rect,
                this.GetIcon(context, iconKey),
                fallbackText,
                fallbackColor,
                disabled);
        }

        internal void DrawFramelessIcon(
            Rect rect,
            Texture2D icon,
            string fallbackText,
            Color fallbackColor,
            bool disabled)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float padding = Mathf.Min(3f, Mathf.Min(rect.width, rect.height) * 0.10f);
            Rect iconRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                Mathf.Max(0f, rect.width - (padding * 2f)),
                Mathf.Max(0f, rect.height - (padding * 2f)));

            Color oldColor = GUI.color;
            GUI.color = icon != null
                ? (disabled ? ShuttleUIStyle.WithAlpha(Color.white, 0.42f) : Color.white)
                : (disabled
                    ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.58f)
                    : fallbackColor);
            ShuttleUILayout.DrawIconOrFallback(iconRect, icon, fallbackText, 1.08f);
            GUI.color = oldColor;
        }

        private Texture2D GetIcon(ShuttlePageDrawContext context, string iconKey)
        {
            return context != null &&
                context.CargoPageContext != null &&
                context.CargoPageContext.Icons != null
                    ? context.CargoPageContext.Icons.GetIcon(iconKey)
                    : null;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Owns the visual layout of one weapon card; command dispatch stays in the action drawer.
    /// </summary>
    internal sealed class V3DefenseWeaponCardDrawer
    {
        internal const float CardPitch = 196f;
        internal const float CardHeight = 190f;

        private const float CardPadding = 10f;
        private const float DetailLineHeight = 16f;

        private readonly V3DefenseText text;
        private readonly V3DefenseWeaponCardText cardText;
        private readonly V3DefenseWeaponCardActionDrawer actionDrawer;
        private readonly V3DefenseWeaponPowerButtonDrawer powerButtonDrawer;

        internal V3DefenseWeaponCardDrawer(V3DefenseText text)
        {
            this.text = text ?? new V3DefenseText();
            this.cardText = new V3DefenseWeaponCardText(this.text);
            this.actionDrawer =
                new V3DefenseWeaponCardActionDrawer(this.text, this.cardText);
            this.powerButtonDrawer = new V3DefenseWeaponPowerButtonDrawer(this.text);
        }

        internal void Draw(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            bool selected,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            if (weapon == null)
            {
                return;
            }

            this.DrawFrame(rect, selected, Mouse.IsOver(rect));
            Color statusColor = this.text.GetWeaponStatusColor(weapon.StatusKey);
            Rect powerRect = new Rect(rect.xMax - 40f, rect.y + 10f, 30f, 30f);
            Rect iconRect = new Rect(rect.x + CardPadding, rect.y + CardPadding, 62f, 62f);
            this.DrawIcon(iconRect, context, weapon.IconKey, statusColor);

            float textX = iconRect.xMax + 10f;
            float titleRight = powerRect.x - 8f;
            this.DrawTitle(
                new Rect(textX, rect.y + 8f, Mathf.Max(0f, titleRight - textX), 20f),
                weapon,
                selected);
            this.DrawHeaderLine(
                new Rect(textX, rect.y + 30f, Mathf.Max(0f, titleRight - textX), 15f),
                this.cardText.GetSlotLine(weapon),
                weapon.Tooltip,
                selected ? 0.94f : 0.76f);
            this.DrawHeaderLine(
                new Rect(textX, rect.y + 45f, Mathf.Max(0f, titleRight - textX), 15f),
                this.cardText.GetTypeStatusLine(weapon),
                weapon.Tooltip,
                selected ? 0.90f : 0.72f);
            this.DrawHeaderLine(
                new Rect(textX, rect.y + 60f, Mathf.Max(0f, rect.xMax - CardPadding - textX), 15f),
                this.cardText.GetPerformanceLine(weapon),
                weapon.Tooltip,
                selected ? 0.84f : 0.66f);

            Widgets.DrawBoxSolid(
                new Rect(
                    rect.x + CardPadding,
                    rect.y + 79f,
                    Mathf.Max(0f, rect.width - (CardPadding * 2f)),
                    1f),
                ShuttleUIStyle.WithAlpha(
                    ShuttleUIStyle.SubtleBorderColor,
                    selected ? 0.30f : 0.18f));

            Rect detailsRect = new Rect(
                rect.x + CardPadding,
                rect.y + 83f,
                Mathf.Max(0f, rect.width - (CardPadding * 2f)),
                DetailLineHeight * 4f);
            this.DrawDetailLine(
                new Rect(detailsRect.x, detailsRect.y, detailsRect.width, DetailLineHeight),
                this.cardText.GetFireControlLine(weapon),
                weapon.FireControlStatusTooltip,
                selected ? 0.88f : 0.72f);
            this.DrawDetailLine(
                new Rect(
                    detailsRect.x,
                    detailsRect.y + DetailLineHeight,
                    detailsRect.width,
                    DetailLineHeight),
                this.cardText.GetAmmoLine(weapon),
                weapon.Tooltip,
                selected ? 0.88f : 0.72f);
            this.DrawDetailLine(
                new Rect(
                    detailsRect.x,
                    detailsRect.y + (DetailLineHeight * 2f),
                    detailsRect.width,
                    DetailLineHeight),
                this.cardText.GetReloadLine(weapon),
                this.cardText.GetReloadCommandTooltip(weapon),
                selected ? 0.82f : 0.66f);
            this.DrawDetailLine(
                new Rect(
                    detailsRect.x,
                    detailsRect.y + (DetailLineHeight * 3f),
                    detailsRect.width,
                    DetailLineHeight),
                this.cardText.GetReloadPolicyLine(weapon),
                weapon.Tooltip,
                selected ? 0.78f : 0.62f);

            Rect actionsRect = new Rect(
                rect.x + CardPadding,
                rect.yMax - 30f,
                Mathf.Max(0f, rect.width - (CardPadding * 2f)),
                22f);
            Rect selectRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, powerRect.x - rect.x - 4f),
                Mathf.Max(0f, actionsRect.y - rect.y - 3f));
            if (Widgets.ButtonInvisible(selectRect) && state != null)
            {
                state.SelectedWeaponId = weapon.Id;
            }

            this.text.AddTooltip(selectRect, weapon.Tooltip);
            this.powerButtonDrawer.Draw(powerRect, weapon, context);
            this.actionDrawer.DrawActions(actionsRect, weapon, context);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawTitle(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            bool selected)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                weapon.Label,
                GameFont.Small,
                GameFont.Tiny,
                selected
                    ? Color.white
                    : ShuttleUIStyle.WithAlpha(Color.white, 0.90f),
                weapon.Tooltip,
                TextAnchor.MiddleLeft);
        }

        private void DrawHeaderLine(
            Rect rect,
            string label,
            string tooltip,
            float alpha)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, alpha),
                tooltip,
                TextAnchor.MiddleLeft);
        }

        private void DrawDetailLine(
            Rect rect,
            string label,
            string tooltip,
            float alpha)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(Color.white, alpha),
                tooltip,
                TextAnchor.MiddleLeft);
        }

        private void DrawFrame(Rect rect, bool selected, bool hovered)
        {
            Color background = selected
                ? new Color(0.070f, 0.145f, 0.190f, 0.96f)
                : (hovered
                    ? new Color(0.060f, 0.115f, 0.150f, 0.95f)
                    : new Color(0.042f, 0.070f, 0.095f, 0.93f));
            Color border = selected
                ? ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, 0.98f)
                : ShuttleUIStyle.WithAlpha(
                    V3DefenseText.BlueColor,
                    hovered ? 0.62f : 0.34f);
            Widgets.DrawBoxSolid(rect, background);
            Widgets.DrawBoxSolid(
                new Rect(
                    rect.x + 2f,
                    rect.y + 2f,
                    Mathf.Max(0f, rect.width - 4f),
                    1f),
                ShuttleUIStyle.WithAlpha(
                    V3DefenseText.BlueColor,
                    hovered || selected ? 0.22f : 0.10f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            this.DrawTopCornerLines(rect, border, 14f, 2f);
            if (selected)
            {
                Widgets.DrawBoxSolid(
                    new Rect(
                        rect.x + 2f,
                        rect.yMax - 3f,
                        Mathf.Max(0f, rect.width - 4f),
                        2f),
                    ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, 0.42f));
            }
        }

        private void DrawIcon(
            Rect rect,
            ShuttlePageDrawContext context,
            string iconKey,
            Color fallbackColor)
        {
            Texture2D icon = context != null &&
                context.DefensePageContext != null &&
                context.DefensePageContext.Icons != null
                    ? context.DefensePageContext.Icons.GetIcon(iconKey)
                    : null;
            float padding = Mathf.Min(3f, Mathf.Min(rect.width, rect.height) * 0.10f);
            Rect iconRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                Mathf.Max(0f, rect.width - (padding * 2f)),
                Mathf.Max(0f, rect.height - (padding * 2f)));
            Color oldColor = GUI.color;
            GUI.color = icon != null ? Color.white : fallbackColor;
            ShuttleUILayout.DrawIconOrFallback(
                iconRect,
                icon,
                this.text.Tr("CT_Shuttle_Defense_WeaponIconFallback"),
                1.08f);
            GUI.color = oldColor;
        }

        private void DrawTopCornerLines(
            Rect rect,
            Color color,
            float length,
            float thickness)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, length, thickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, thickness, length), color);
            Widgets.DrawBoxSolid(
                new Rect(rect.xMax - length, rect.y, length, thickness),
                color);
            Widgets.DrawBoxSolid(
                new Rect(rect.xMax - thickness, rect.y, thickness, length),
                color);
        }
    }
}

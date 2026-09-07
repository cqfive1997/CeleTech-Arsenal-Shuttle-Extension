using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws one full-width weapon row; commands stay in focused action drawers.
    /// </summary>
    internal sealed class V3DefenseWeaponRowDrawer
    {
        internal const float RowHeight = 114f;
        internal const float RowPitch = 120f;

        private const float Padding = 10f;
        private const float RegionGap = 10f;

        private readonly V3DefenseText text;
        private readonly V3DefenseWeaponCardText cardText;
        private readonly V3DefenseWeaponCardActionDrawer actionDrawer;
        private readonly V3DefenseWeaponPowerButtonDrawer powerButtonDrawer;
        private readonly V3DefenseWeaponAmmoMeterDrawer ammoMeterDrawer;

        internal V3DefenseWeaponRowDrawer(V3DefenseText text)
        {
            this.text = text ?? new V3DefenseText();
            this.cardText = new V3DefenseWeaponCardText(this.text);
            this.actionDrawer =
                new V3DefenseWeaponCardActionDrawer(this.text, this.cardText);
            this.powerButtonDrawer = new V3DefenseWeaponPowerButtonDrawer(this.text);
            this.ammoMeterDrawer =
                new V3DefenseWeaponAmmoMeterDrawer(this.text, this.cardText);
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

            bool hovered = Mouse.IsOver(rect);
            this.DrawFrame(rect, selected, hovered);
            Color statusColor = this.text.GetWeaponStatusColor(weapon.StatusKey);
            Rect iconRect = new Rect(rect.x + Padding, rect.y + Padding, 58f, 58f);
            this.DrawIcon(iconRect, context, weapon.IconKey, statusColor);
            Rect powerRect = new Rect(rect.xMax - 40f, rect.y + Padding, 30f, 30f);

            float contentX = iconRect.xMax + RegionGap;
            float contentRight = powerRect.x - RegionGap;
            float contentWidth = Mathf.Max(0f, contentRight - contentX);
            float identityWidth = Mathf.Clamp(
                contentWidth * 0.32f,
                165f,
                245f);
            float fireControlWidth = Mathf.Clamp(
                contentWidth * 0.31f,
                170f,
                245f);
            float ammoWidth = Mathf.Max(
                120f,
                contentWidth - identityWidth - fireControlWidth - (RegionGap * 2f));

            Rect identityRect = new Rect(
                contentX,
                rect.y + 7f,
                identityWidth,
                76f);
            Rect fireControlRect = new Rect(
                identityRect.xMax + RegionGap,
                rect.y + 8f,
                fireControlWidth,
                72f);
            Rect ammoRect = new Rect(
                fireControlRect.xMax + RegionGap,
                rect.y + 8f,
                Mathf.Max(0f, Mathf.Min(ammoWidth, contentRight - fireControlRect.xMax - RegionGap)),
                70f);
            this.DrawIdentity(identityRect, weapon, selected);
            this.DrawFireControl(fireControlRect, weapon, selected);
            this.ammoMeterDrawer.Draw(ammoRect, weapon, selected);
            this.DrawZoneDivider(
                identityRect.xMax + (RegionGap * 0.5f),
                rect.y + 9f,
                68f);
            this.DrawZoneDivider(
                fireControlRect.xMax + (RegionGap * 0.5f),
                rect.y + 9f,
                68f);

            Rect actionsRect = new Rect(
                fireControlRect.x,
                rect.yMax - 29f,
                Mathf.Max(0f, rect.xMax - Padding - fireControlRect.x),
                20f);

            Rect selectRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, powerRect.x - rect.x - 4f),
                Mathf.Max(0f, actionsRect.y - rect.y - 2f));
            if (Widgets.ButtonInvisible(selectRect) && state != null)
            {
                state.SelectedWeaponId = weapon.Id;
            }

            this.text.AddTooltip(selectRect, weapon.Tooltip);
            if (hovered)
            {
                this.DrawActionRailDivider(actionsRect);
                this.powerButtonDrawer.Draw(powerRect, weapon, context);
                this.actionDrawer.DrawActions(actionsRect, weapon, context);
            }
            else
            {
                this.powerButtonDrawer.DrawPassiveStatus(powerRect, weapon);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawIdentity(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            bool selected)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, rect.width, 21f),
                weapon.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);
            this.DrawMutedLine(
                new Rect(rect.x, rect.y + 22f, rect.width, 16f),
                this.cardText.GetSlotLine(weapon),
                weapon.Tooltip,
                selected);
            this.DrawMutedLine(
                new Rect(rect.x, rect.y + 38f, rect.width, 16f),
                this.cardText.GetTypeStatusLine(weapon),
                weapon.Tooltip,
                selected);
            this.DrawMutedLine(
                new Rect(rect.x, rect.y + 54f, rect.width, 16f),
                this.cardText.GetPerformanceLine(weapon),
                weapon.Tooltip,
                selected);
        }

        private void DrawFireControl(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            bool selected)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, rect.width, 16f),
                this.text.Tr("CT_Shuttle_Type_Module_FireControl"),
                GameFont.Tiny,
                GameFont.Tiny,
                V3DefenseText.BlueColor,
                weapon.FireControlStatusTooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y + 17f, rect.width, 17f),
                this.cardText.GetFireControlPurposeLine(weapon),
                GameFont.Tiny,
                GameFont.Tiny,
                selected
                    ? Color.white
                    : ShuttleUIStyle.WithAlpha(Color.white, 0.82f),
                weapon.FireControlStatusTooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y + 35f, rect.width, 16f),
                this.cardText.GetReloadLine(weapon),
                GameFont.Tiny,
                GameFont.Tiny,
                selected
                    ? Color.white
                    : ShuttleUIStyle.MutedTextColor,
                weapon.FireControlStatusTooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y + 52f, rect.width, 16f),
                this.cardText.GetReloadPolicyLine(weapon),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);
        }

        private void DrawZoneDivider(float x, float y, float height)
        {
            Widgets.DrawBoxSolid(
                new Rect(x, y, 1f, Mathf.Max(0f, height)),
                ShuttleUIStyle.WithAlpha(
                    ShuttleUIStyle.SubtleBorderColor,
                    0.34f));
        }

        private void DrawActionRailDivider(Rect actionsRect)
        {
            Widgets.DrawBoxSolid(
                new Rect(
                    actionsRect.x,
                    actionsRect.y - 5f,
                    actionsRect.width,
                    1f),
                ShuttleUIStyle.WithAlpha(
                    V3DefenseText.BlueColor,
                    0.24f));
        }

        private void DrawMutedLine(
            Rect rect,
            string value,
            string tooltip,
            bool selected)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                value,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(
                    ShuttleUIStyle.MutedTextColor,
                    selected ? 0.92f : 0.72f),
                tooltip,
                TextAnchor.MiddleLeft);
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
            Color oldColor = GUI.color;
            GUI.color = icon != null ? Color.white : fallbackColor;
            ShuttleUILayout.DrawIconOrFallback(
                rect.ContractedBy(3f),
                icon,
                this.text.Tr("CT_Shuttle_Defense_WeaponIconFallback"),
                1.08f);
            GUI.color = oldColor;
        }

        private void DrawFrame(Rect rect, bool selected, bool hovered)
        {
            Color background = selected
                ? new Color(0.070f, 0.145f, 0.190f, 0.96f)
                : (hovered
                    ? new Color(0.060f, 0.115f, 0.150f, 0.95f)
                    : new Color(0.042f, 0.070f, 0.095f, 0.93f));
            Color border = ShuttleUIStyle.WithAlpha(
                V3DefenseText.BlueColor,
                selected ? 0.98f : (hovered ? 0.62f : 0.34f));
            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            if (selected)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x, rect.y, 4f, rect.height),
                    V3DefenseText.BlueColor);
            }
        }
    }
}

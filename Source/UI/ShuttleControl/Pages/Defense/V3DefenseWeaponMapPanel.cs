using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws a compact, fixed-east shuttle preview with selectable weapon mounts.
    /// </summary>
    internal sealed class V3DefenseWeaponMapPanel
    {
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;
        private readonly V3DefenseWeaponMapProjection projection =
            new V3DefenseWeaponMapProjection();

        internal V3DefenseWeaponMapPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3DefensePageModel model,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(
                rect,
                this.text.Tr("CT_Shuttle_Defense_WeaponMap"));
            Rect inner = this.panel.GetPanelInnerRect(rect, 44f);
            this.panel.DrawCardBackground(
                inner,
                false,
                false,
                V3DefenseText.StrongCardColor);

            Rect shipRect = new Rect(
                inner.x + 8f,
                inner.y + 8f,
                Mathf.Max(0f, inner.width - 16f),
                Mathf.Max(0f, inner.height - 16f));
            this.DrawShip(shipRect, model, context);

            List<V3DefenseWeaponEntryModel> weapons =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.Weapons
                    : null;
            if (weapons == null || weapons.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    shipRect,
                    this.text.Tr("CT_Shuttle_Defense_NoWeaponModules"));
                return;
            }

            Rect bodyRect = this.projection.GetEastPreviewBodyRect(shipRect);
            for (int i = 0; i < weapons.Count; i++)
            {
                V3DefenseWeaponEntryModel weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                Vector2 anchor = this.projection.ResolveEastAnchor(weapon, i);
                Vector2 center = new Vector2(
                    bodyRect.x + (bodyRect.width * anchor.x),
                    bodyRect.y + (bodyRect.height * anchor.y));
                this.DrawHotspot(
                    new Rect(center.x - 11f, center.y - 11f, 22f, 22f),
                    weapon,
                    i,
                    IsSelected(weapon, model.DefenseModel),
                    state);
            }
        }

        private void DrawShip(
            Rect rect,
            V3DefensePageModel model,
            ShuttlePageDrawContext context)
        {
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, 0.24f));
            Texture preview;
            if (context != null &&
                context.DefensePageContext != null &&
                context.DefensePageContext.PaintPreviewProvider != null &&
                model != null &&
                model.ControlModel != null &&
                context.DefensePageContext.PaintPreviewProvider.TryGetPreview(
                    model.ControlModel.PaintScheme,
                    ShuttlePaintPreviewWeaponStationMask.FromControlModel(
                        model.ControlModel),
                    Rot4.East,
                    out preview) &&
                preview != null)
            {
                ShuttlePaintPreviewDrawer.Draw(rect, preview);
                return;
            }

            this.DrawEastSchematic(rect);
        }

        private void DrawEastSchematic(Rect rect)
        {
            float width = Mathf.Min(rect.width * 0.78f, rect.height * 1.35f);
            float height = Mathf.Min(rect.height * 0.42f, rect.width * 0.38f);
            Rect body = new Rect(
                rect.center.x - (width * 0.46f),
                rect.center.y - (height * 0.5f),
                width * 0.84f,
                height);
            Color bodyColor = new Color(0.110f, 0.170f, 0.220f, 0.74f);
            Widgets.DrawBoxSolid(body, bodyColor);
            Widgets.DrawBoxSolid(
                new Rect(
                    body.xMax - (width * 0.02f),
                    body.y + (body.height * 0.18f),
                    width * 0.15f,
                    body.height * 0.64f),
                bodyColor);
            Widgets.DrawBoxSolid(
                new Rect(
                    body.x + (body.width * 0.50f),
                    body.y - (body.height * 0.26f),
                    body.width * 0.20f,
                    body.height * 1.52f),
                new Color(0.080f, 0.125f, 0.170f, 0.76f));
            ShuttleUILayout.DrawRectBorder(
                body,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        private void DrawHotspot(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            int index,
            bool selected,
            V3DefensePageState state)
        {
            Color status = this.text.GetWeaponStatusColor(weapon.StatusKey);
            bool hovered = Mouse.IsOver(rect);
            Color background = selected
                ? ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, 0.82f)
                : ShuttleUIStyle.WithAlpha(
                    ShuttleUIStyle.MutedCardColor,
                    hovered ? 0.94f : 0.82f);
            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                selected ? V3DefenseText.BlueColor : status,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(2f),
                GetHotspotLabel(weapon, index),
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                weapon.Tooltip,
                TextAnchor.MiddleCenter);
            this.text.AddTooltip(rect, weapon.Tooltip);
            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedWeaponId = weapon.Id;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static string GetHotspotLabel(
            V3DefenseWeaponEntryModel weapon,
            int index)
        {
            return weapon != null && weapon.SlotIndex > 0
                ? weapon.SlotIndex.ToString()
                : (index + 1).ToString();
        }

        private static bool IsSelected(
            V3DefenseWeaponEntryModel weapon,
            V3DefensePageReadModel model)
        {
            return weapon != null &&
                model != null &&
                model.SelectedWeapon != null &&
                weapon.Id == model.SelectedWeapon.Id;
        }
    }
}

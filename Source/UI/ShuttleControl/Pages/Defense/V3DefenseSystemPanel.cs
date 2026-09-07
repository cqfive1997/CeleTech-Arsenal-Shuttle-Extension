using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseSystemPanel
    {
        private const string HostThingDefName = "CT_ModularShuttleHost";
        private const float MinimumDrawSize = 0.05f;
        private const float BrowserPreviewWidth = 512f;
        private const float BrowserPreviewHeight = 360f;
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;
        private readonly V3DefenseWeaponRowDrawer weaponRowDrawer;
        private bool turretAnchorDataResolved;
        private Vector2 hostDrawSize = new Vector2(7f, 9f);
        private List<ShuttleWeaponTurretVisualMount> turretMounts;

        internal V3DefenseSystemPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
            this.weaponRowDrawer = new V3DefenseWeaponRowDrawer(this.text);
        }

        internal void DrawWeaponBrief(
            Rect rect,
            V3DefensePageModel model,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Defense_WeaponBrief"));
            Rect listRect = this.panel.GetPanelInnerRect(rect, 44f);
            List<V3DefenseWeaponEntryModel> weapons =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.Weapons
                    : null;
            if (weapons == null || weapons.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(listRect, this.text.Tr("CT_Shuttle_Defense_NoWeapons"));
                return;
            }

            float contentHeight =
                ((weapons.Count - 1) * V3DefenseWeaponRowDrawer.RowPitch) +
                V3DefenseWeaponRowDrawer.RowHeight;
            bool requiresScroll = contentHeight > listRect.height;
            float viewWidth = Mathf.Max(
                1f,
                listRect.width - (requiresScroll ? 16f : 0f));
            Rect viewRect = new Rect(
                0f,
                0f,
                viewWidth,
                Mathf.Max(listRect.height, contentHeight));
            Vector2 scroll = state != null ? state.WeaponScroll : Vector2.zero;
            if (!requiresScroll)
            {
                scroll = Vector2.zero;
            }

            Widgets.BeginScrollView(listRect, ref scroll, viewRect);
            for (int i = 0; i < weapons.Count; i++)
            {
                V3DefenseWeaponEntryModel weapon = weapons[i];
                Rect rowRect = new Rect(
                    0f,
                    i * V3DefenseWeaponRowDrawer.RowPitch,
                    viewWidth,
                    V3DefenseWeaponRowDrawer.RowHeight);
                this.weaponRowDrawer.Draw(
                    rowRect,
                    weapon,
                    IsSelectedWeapon(weapon, model != null ? model.DefenseModel : null),
                    state,
                    context);
            }

            Widgets.EndScrollView();
            if (state != null)
            {
                state.WeaponScroll = scroll;
            }
        }

        internal void DrawOverview(
            Rect rect,
            V3DefensePageModel model,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawCompactPanelTitle(rect, this.text.Tr("CT_Shuttle_Defense_Overview"));
            this.DrawDefenseLegend(this.GetTitleLegendRect(rect));
            Rect inner = this.panel.GetPanelInnerRect(rect, 34f);
            Rect shipRect = this.GetShipRect(inner);
            this.DrawShipPreview(shipRect, model, context);
            Rot4 previewRotation = ResolvePreviewRotation(context);
            this.DrawHullOverviewStrip(
                new Rect(shipRect.x + 8f, inner.yMax - 50f, Mathf.Max(0f, shipRect.width - 16f), 26f),
                model != null && model.DefenseModel != null ? model.DefenseModel.Hull : null);

            List<V3DefenseWeaponEntryModel> weapons =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.Weapons
                    : null;
            if (weapons == null || weapons.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    new Rect(shipRect.x + 18f, inner.yMax - 72f, shipRect.width - 36f, 24f),
                    this.text.Tr("CT_Shuttle_Defense_NoWeaponModules"));
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                V3DefenseWeaponEntryModel weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                bool selected = IsSelectedWeapon(weapon, model.DefenseModel);
                Rect nodeRect = this.GetWeaponHotspotRect(inner, weapon, i, previewRotation);
                Vector2 shipAnchor = this.GetWeaponShipAnchor(shipRect, weapon, i, previewRotation);
                this.DrawConnector(this.GetRectEdgePointTowards(nodeRect, shipAnchor), shipAnchor, selected);
                this.DrawWeaponHotspot(nodeRect, weapon, selected, state);
            }
        }

        private string GetWeaponSlotLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return "-";
            }

            if (!string.IsNullOrEmpty(weapon.SlotShortLabel))
            {
                return weapon.SlotShortLabel;
            }

            return !string.IsNullOrEmpty(weapon.SlotLabel) ? weapon.SlotLabel : "-";
        }

        private Rect GetShipRect(Rect inner)
        {
            float shipInsetX = Mathf.Max(44f, inner.width * 0.10f);
            return new Rect(
                inner.x + shipInsetX,
                inner.y + 8f,
                Mathf.Max(120f, inner.width - (shipInsetX * 2f)),
                Mathf.Max(90f, inner.height - 64f));
        }

        private void DrawShipPreview(
            Rect shipRect,
            V3DefensePageModel model,
            ShuttlePageDrawContext context)
        {
            Texture preview;
            if (context != null &&
                context.DefensePageContext != null &&
                context.DefensePageContext.PaintPreviewProvider != null &&
                model != null &&
                model.ControlModel != null &&
                context.DefensePageContext.PaintPreviewProvider.TryGetPreview(
                    model.ControlModel.PaintScheme,
                    ShuttlePaintPreviewWeaponStationMask.FromControlModel(model.ControlModel),
                    context.PreviewRotation,
                    out preview) &&
                preview != null)
            {
                ShuttlePaintPreviewDrawer.Draw(shipRect, preview);
                return;
            }

            this.DrawSchematicHull(shipRect);
        }

        private void DrawSchematicHull(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.035f, 0.050f, 0.070f, 0.70f));
            Widgets.DrawBox(rect, 1);
            Rect body = new Rect(
                rect.x + rect.width * 0.18f,
                rect.y + rect.height * 0.24f,
                rect.width * 0.64f,
                rect.height * 0.52f);
            Widgets.DrawBoxSolid(body, new Color(0.110f, 0.170f, 0.220f, 0.70f));
            Widgets.DrawBox(body, 1);
            Widgets.DrawBoxSolid(
                new Rect(body.x + body.width * 0.38f, rect.y + rect.height * 0.14f, body.width * 0.24f, body.height * 0.28f),
                new Color(0.145f, 0.220f, 0.280f, 0.70f));
            Widgets.DrawBoxSolid(
                new Rect(body.x - body.width * 0.18f, body.y + body.height * 0.42f, body.width * 0.22f, body.height * 0.20f),
                new Color(0.080f, 0.125f, 0.170f, 0.72f));
            Widgets.DrawBoxSolid(
                new Rect(body.xMax - body.width * 0.04f, body.y + body.height * 0.42f, body.width * 0.22f, body.height * 0.20f),
                new Color(0.080f, 0.125f, 0.170f, 0.72f));
        }

        private void DrawHullOverviewStrip(Rect rect, ShuttleHullReadModel hull)
        {
            if (hull == null || !hull.HasHull)
            {
                return;
            }

            Color accent = this.text.GetHullIntegrityColor(hull);
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, 0.42f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.38f),
                ShuttleUIStyle.ThinBorder);

            string tooltipLabel = ShuttleUIText.Tr(
                "CT_Shuttle_Hull_IntegrityLabel",
                this.text.FormatHullHitPoints(hull.CurrentHitPoints),
                hull.MaxHitPoints,
                hull.IntegrityPctRounded);
            string armorLabel = this.text.Tr("CT_Shuttle_Hull_ArmorMetricLabel");
            string valueLabel = this.text.FormatHullHitPoints(hull.CurrentHitPoints) +
                " / " +
                hull.MaxHitPoints;
            string pctLabel = hull.IntegrityPctRounded.ToString() + "%";

            Rect contentRect = new Rect(
                rect.x + 8f,
                rect.y + 4f,
                Mathf.Max(0f, rect.width - 16f),
                Mathf.Max(0f, rect.height - 8f));
            float gap = 6f;
            float chipWidth = Mathf.Clamp(contentRect.width * 0.18f, 52f, 70f);
            float labelWidth = Mathf.Clamp(contentRect.width * 0.16f, 44f, 74f);
            float valueWidth = Mathf.Clamp(contentRect.width * 0.24f, 66f, 104f);
            float pctWidth = Mathf.Clamp(contentRect.width * 0.10f, 32f, 40f);
            float meterWidth =
                contentRect.width -
                chipWidth -
                labelWidth -
                valueWidth -
                pctWidth -
                (gap * 4f);
            if (meterWidth < 34f)
            {
                float deficit = 34f - meterWidth;
                valueWidth = Mathf.Max(58f, valueWidth - (deficit * 0.45f));
                labelWidth = Mathf.Max(40f, labelWidth - (deficit * 0.35f));
                chipWidth = Mathf.Max(48f, chipWidth - (deficit * 0.20f));
                meterWidth =
                    contentRect.width -
                    chipWidth -
                    labelWidth -
                    valueWidth -
                    pctWidth -
                    (gap * 4f);
            }

            Rect chipRect = new Rect(
                contentRect.xMax - chipWidth,
                contentRect.y + Mathf.Max(0f, (contentRect.height - 16f) * 0.5f),
                chipWidth,
                Mathf.Min(16f, contentRect.height));
            Rect labelRect = new Rect(contentRect.x, contentRect.y, labelWidth, contentRect.height);
            Rect valueRect = new Rect(labelRect.xMax + gap, contentRect.y, valueWidth, contentRect.height);
            Rect pctRect = new Rect(valueRect.xMax + gap, contentRect.y, pctWidth, contentRect.height);
            Rect meterRect = new Rect(
                pctRect.xMax + gap,
                contentRect.y + 8f,
                Mathf.Max(0f, chipRect.x - pctRect.xMax - (gap * 2f)),
                3f);

            Text.Font = GameFont.Tiny;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                armorLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltipLabel,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                valueRect,
                valueLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                tooltipLabel,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                pctRect,
                pctLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                accent,
                tooltipLabel,
                TextAnchor.MiddleLeft);
            this.panel.DrawMeter(meterRect, hull.IntegrityPct, accent);
            this.DrawLightStatusChip(chipRect, hull.StatusLabel, accent, tooltipLabel);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            this.text.AddTooltip(rect, this.BuildHullTooltip(hull, tooltipLabel));
        }

        private string BuildHullTooltip(ShuttleHullReadModel hull, string label)
        {
            string tooltip = label + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Hull_StatusLabel", hull.StatusLabel);
            if (!string.IsNullOrEmpty(hull.WarningLabel))
            {
                tooltip += "\n" + hull.WarningLabel;
            }

            if (!hull.NeedsRepair)
            {
                return tooltip + "\n" + ShuttleUIText.Tr("CT_Shuttle_HullRepair_TooltipNotNeeded");
            }

            string costSummary = !string.IsNullOrEmpty(hull.RepairCostSummary)
                ? hull.RepairCostSummary
                : ShuttleUIText.Tr("CT_Shuttle_HullRepair_CostSummaryEmpty");
            return tooltip + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_HullRepair_TooltipCost", costSummary) +
                "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_HullRepair_TooltipWork",
                    Mathf.Max(0, hull.RepairWorkTicks).ToString());
        }

        private Rect GetWeaponHotspotRect(
            Rect inner,
            V3DefenseWeaponEntryModel weapon,
            int index,
            Rot4 rotation)
        {
            Vector2 anchor = this.ResolveWeaponPreviewAnchor(weapon, index, rotation);
            float width = Mathf.Min(100f, Mathf.Max(84f, inner.width * 0.17f));
            float height = 26f;
            Vector2 spreadOffset = this.GetRepeatedHotspotOffset(index);
            float x = inner.x + (inner.width * anchor.x) - (width * 0.5f) + spreadOffset.x;
            float y = inner.y + (inner.height * anchor.y) - (height * 0.5f) + spreadOffset.y;
            x = Mathf.Clamp(x, inner.x + 8f, inner.xMax - width - 8f);
            y = Mathf.Clamp(y, inner.y + 8f, inner.yMax - height - 54f);
            return new Rect(x, y, width, height);
        }

        private Vector2 GetRepeatedHotspotOffset(int index)
        {
            int ring = Mathf.Abs(index) / 8;
            if (ring <= 0)
            {
                return Vector2.zero;
            }

            float direction = index % 2 == 0 ? -1f : 1f;
            float vertical = index % 3 == 0 ? -1f : 1f;
            return new Vector2(direction * 18f * ring, vertical * 12f * ring);
        }

        private Vector2 GetWeaponShipAnchor(
            Rect shipRect,
            V3DefenseWeaponEntryModel weapon,
            int index,
            Rot4 rotation)
        {
            Vector2 anchor = this.ResolveWeaponPreviewAnchor(weapon, index, rotation);
            Rect bodyRect = this.GetShipPreviewBodyRect(shipRect, rotation);
            float x = bodyRect.x + (bodyRect.width * anchor.x);
            float y = bodyRect.y + (bodyRect.height * anchor.y);
            return new Vector2(x, y);
        }

        private Vector2 ResolveWeaponPreviewAnchor(
            V3DefenseWeaponEntryModel weapon,
            int index,
            Rot4 rotation)
        {
            Vector2 anchor;
            if (this.TryResolveTurretMountAnchor(weapon, rotation, out anchor))
            {
                return anchor;
            }

            anchor = weapon != null ? weapon.UIAnchor : Vector2.zero;
            if (anchor == Vector2.zero)
            {
                anchor = this.GetFallbackAnchor(index);
            }

            return ClampAnchor(RotateEastAuthoredAnchor(anchor, rotation));
        }

        private bool TryResolveTurretMountAnchor(
            V3DefenseWeaponEntryModel weapon,
            Rot4 rotation,
            out Vector2 anchor)
        {
            anchor = Vector2.zero;
            if (weapon == null ||
                weapon.SlotIndex <= 0 ||
                this.hostDrawSize.x <= MinimumDrawSize ||
                this.hostDrawSize.y <= MinimumDrawSize)
            {
                return false;
            }

            this.EnsureTurretAnchorDataResolved();
            ShuttleWeaponTurretVisualMount mount = this.FindMountForSlotIndex(weapon.SlotIndex);
            if (mount == null)
            {
                return false;
            }

            Vector3 offset = ShuttleWeaponTurretAirframe.ResolveRotatedMountOffset(
                mount,
                rotation);
            anchor = ClampAnchor(new Vector2(
                0.5f + (offset.x / this.hostDrawSize.x),
                0.5f - (offset.z / this.hostDrawSize.y)));
            return true;
        }

        private void EnsureTurretAnchorDataResolved()
        {
            if (this.turretAnchorDataResolved)
            {
                return;
            }

            this.turretAnchorDataResolved = true;
            ThingDef hostDef = DefDatabase<ThingDef>.GetNamedSilentFail(HostThingDefName);
            if (hostDef != null &&
                hostDef.graphicData != null &&
                hostDef.graphicData.drawSize.x > MinimumDrawSize &&
                hostDef.graphicData.drawSize.y > MinimumDrawSize)
            {
                this.hostDrawSize = hostDef.graphicData.drawSize;
            }

            CompProperties_ModularShuttleWeaponTurretVisual turretProps =
                FindCompProperties<CompProperties_ModularShuttleWeaponTurretVisual>(hostDef);
            this.turretMounts = turretProps != null ? turretProps.mounts : null;
        }

        private ShuttleWeaponTurretVisualMount FindMountForSlotIndex(int slotIndex)
        {
            if (this.turretMounts == null)
            {
                return null;
            }

            for (int i = 0; i < this.turretMounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = this.turretMounts[i];
                if (mount != null && mount.parentSlotIndex == slotIndex)
                {
                    return mount;
                }
            }

            return null;
        }

        private Rect GetShipPreviewBodyRect(Rect shipRect, Rot4 rotation)
        {
            Rect previewRect = GetAspectFitRect(shipRect, BrowserPreviewWidth, BrowserPreviewHeight);
            Texture2D sourceTexture = ContentFinder<Texture2D>.Get(
                ShuttlePaintVisualAssetSet.GetLandedBrowserPaintSourceTexturePath(rotation),
                false);
            if (sourceTexture == null)
            {
                return previewRect;
            }

            return GetAspectFitRect(previewRect, sourceTexture.width, sourceTexture.height);
        }

        private static Vector2 RotateEastAuthoredAnchor(Vector2 anchor, Rot4 rotation)
        {
            Vector3 normalizedOffset = new Vector3(
                anchor.x - 0.5f,
                0f,
                0.5f - anchor.y);
            Vector3 rotatedOffset = ShuttleWeaponTurretAirframe.RotateEastAuthoredOffset(
                normalizedOffset,
                rotation);
            return new Vector2(
                0.5f + rotatedOffset.x,
                0.5f - rotatedOffset.z);
        }

        private static Vector2 ClampAnchor(Vector2 anchor)
        {
            return new Vector2(
                Mathf.Clamp01(anchor.x),
                Mathf.Clamp01(anchor.y));
        }

        private static Rect GetAspectFitRect(Rect outer, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth <= MinimumDrawSize ||
                sourceHeight <= MinimumDrawSize ||
                outer.width <= MinimumDrawSize ||
                outer.height <= MinimumDrawSize)
            {
                return outer;
            }

            float sourceAspect = sourceWidth / sourceHeight;
            float targetAspect = outer.width / outer.height;
            if (Mathf.Abs(sourceAspect - targetAspect) <= 0.001f)
            {
                return outer;
            }

            if (sourceAspect > targetAspect)
            {
                float height = outer.width / sourceAspect;
                return new Rect(
                    outer.x,
                    outer.y + ((outer.height - height) * 0.5f),
                    outer.width,
                    height);
            }

            float width = outer.height * sourceAspect;
            return new Rect(
                outer.x + ((outer.width - width) * 0.5f),
                outer.y,
                width,
                outer.height);
        }

        private static T FindCompProperties<T>(ThingDef thingDef)
            where T : CompProperties
        {
            if (thingDef == null || thingDef.comps == null)
            {
                return null;
            }

            for (int i = 0; i < thingDef.comps.Count; i++)
            {
                T props = thingDef.comps[i] as T;
                if (props != null)
                {
                    return props;
                }
            }

            return null;
        }

        private static Rot4 ResolvePreviewRotation(ShuttlePageDrawContext context)
        {
            return context != null ? context.PreviewRotation : Rot4.East;
        }

        private Vector2 GetFallbackAnchor(int index)
        {
            float[] xs = { 0.26f, 0.74f, 0.34f, 0.66f, 0.50f, 0.22f, 0.78f, 0.50f };
            float[] ys = { 0.34f, 0.34f, 0.50f, 0.50f, 0.24f, 0.64f, 0.64f, 0.72f };
            int safeIndex = Mathf.Abs(index) % xs.Length;
            return new Vector2(xs[safeIndex], ys[safeIndex]);
        }

        private void DrawWeaponHotspot(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            bool selected,
            V3DefensePageState state)
        {
            Color color = this.text.GetWeaponStatusColor(weapon != null ? weapon.StatusKey : null);
            bool hovered = Mouse.IsOver(rect);
            Color background = selected
                ? ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, hovered ? 0.09f : 0.055f)
                : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, hovered ? 0.26f : 0.16f);
            Color border = selected
                ? ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, 0.72f)
                : ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, hovered ? 0.16f : 0.07f);

            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                ShuttleUIStyle.ThinBorder);
            if (selected)
            {
                this.DrawSelectedWeaponTagAccent(rect);
            }

            Rect dotRect = new Rect(rect.x + 7f, rect.y + Mathf.Floor((rect.height - 5f) * 0.5f), 5f, 5f);
            Widgets.DrawBoxSolid(dotRect, color);

            bool drawTypeLine = false;
            Rect labelRect = new Rect(
                dotRect.xMax + 5f,
                drawTypeLine ? rect.y + 4f : rect.y + 2f,
                Mathf.Max(0f, rect.width - (dotRect.xMax - rect.x) - 10f),
                drawTypeLine ? 14f : rect.height - 4f);
            Color labelColor = selected
                ? ShuttleUIStyle.WithAlpha(Color.white, 0.90f)
                : ShuttleUIStyle.WithAlpha(Color.white, 0.64f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                weapon != null ? this.GetWeaponSlotLine(weapon) : "-",
                GameFont.Tiny,
                GameFont.Tiny,
                labelColor,
                weapon != null ? weapon.Tooltip : string.Empty,
                TextAnchor.MiddleLeft);
            if (drawTypeLine)
            {
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    new Rect(labelRect.x, rect.y + 18f, labelRect.width, 12f),
                    weapon != null ? weapon.WeaponTypeLabel : "-",
                    GameFont.Tiny,
                    GameFont.Tiny,
                    ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, selected ? 0.88f : 0.64f),
                    weapon != null ? weapon.Tooltip : string.Empty,
                    TextAnchor.MiddleLeft);
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect) && state != null && weapon != null)
            {
                state.SelectedWeaponId = weapon.Id;
            }

            this.text.AddTooltip(rect, weapon != null ? weapon.Tooltip : string.Empty);
        }

        private Rect GetTitleLegendRect(Rect rect)
        {
            float width = Mathf.Min(206f, Mathf.Max(146f, rect.width * 0.42f));
            return new Rect(rect.xMax - width - 10f, rect.y + 6f, width, 18f);
        }

        private void DrawDefenseLegend(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            float maxLabelWidth = Mathf.Max(24f, (rect.width - 50f) / 3f);
            string onlineLabel = this.text.Tr("CT_Shuttle_Defense_Online");
            string holdFireLabel = this.text.Tr("CT_Shuttle_Defense_HoldFire");
            string offlineLabel = this.text.Tr("CT_Shuttle_Defense_Offline");
            float onlineWidth = this.GetLegendItemWidth(onlineLabel, maxLabelWidth);
            float holdFireWidth = this.GetLegendItemWidth(holdFireLabel, maxLabelWidth);
            float offlineWidth = this.GetLegendItemWidth(offlineLabel, maxLabelWidth);
            float totalWidth = onlineWidth + holdFireWidth + offlineWidth + 18f;
            float x = rect.xMax - Mathf.Min(rect.width, totalWidth);
            x = this.DrawLegendItem(
                x,
                rect.y,
                V3DefenseText.GreenColor,
                onlineLabel,
                onlineWidth);
            x = this.DrawLegendItem(
                x + 9f,
                rect.y,
                V3DefenseText.YellowColor,
                holdFireLabel,
                holdFireWidth);
            this.DrawLegendItem(
                x + 9f,
                rect.y,
                V3DefenseText.RedColor,
                offlineLabel,
                offlineWidth);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private float GetLegendItemWidth(string label, float maxLabelWidth)
        {
            return 10f + Mathf.Min(maxLabelWidth, Mathf.Ceil(Text.CalcSize(label).x) + 2f);
        }

        private float DrawLegendItem(float x, float y, Color color, string label, float width)
        {
            Rect dotRect = new Rect(x, y + 6f, 5f, 5f);
            Widgets.DrawBoxSolid(dotRect, ShuttleUIStyle.WithAlpha(color, 0.76f));
            Rect labelRect = new Rect(dotRect.xMax + 4f, y, Mathf.Max(0f, width - 10f), 16f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.66f),
                label,
                TextAnchor.MiddleLeft);
            return labelRect.xMax;
        }

        private void DrawLightStatusChip(Rect rect, string label, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.WithAlpha(color, 0.04f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(color, 0.34f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    rect.x + 4f,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.width - 8f),
                    Mathf.Max(0f, rect.height - 2f)),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(color, 0.78f),
                tooltip,
                TextAnchor.MiddleCenter);
        }

        private void DrawTitleDivider(Rect rect)
        {
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 10f, rect.y + 30f, Mathf.Max(0f, rect.width - 20f), 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.20f));
        }

        private void DrawSelectedWeaponTagAccent(Rect rect)
        {
            Color accent = ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, 0.76f);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 1f, rect.y + 1f, 1f, Mathf.Max(0f, rect.height - 2f)),
                accent);
            Widgets.DrawBoxSolid(new Rect(rect.x + 1f, rect.y + 1f, 10f, 1f), accent);
            Widgets.DrawBoxSolid(new Rect(rect.x + 1f, rect.yMax - 2f, 10f, 1f), accent);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 11f, rect.y + 1f, 10f, 1f), accent);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 11f, rect.yMax - 2f, 10f, 1f), accent);
        }

        private void DrawConnector(Vector2 from, Vector2 to, bool selected)
        {
            Color color = selected
                ? ShuttleUIStyle.WithAlpha(V3DefenseText.BlueColor, 0.28f)
                : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.12f);
            float midX = (from.x + to.x) * 0.5f;
            float thickness = 1f;
            this.DrawThinLine(new Vector2(from.x, from.y), new Vector2(midX, from.y), color, thickness);
            this.DrawThinLine(new Vector2(midX, from.y), new Vector2(midX, to.y), color, thickness);
            this.DrawThinLine(new Vector2(midX, to.y), new Vector2(to.x, to.y), color, thickness);
        }

        private Vector2 GetRectEdgePointTowards(Rect rect, Vector2 target)
        {
            Vector2 center = rect.center;
            Vector2 delta = target - center;
            if (delta == Vector2.zero)
            {
                return center;
            }

            float scaleX = Mathf.Abs(delta.x) > 0.001f
                ? (rect.width * 0.5f) / Mathf.Abs(delta.x)
                : float.MaxValue;
            float scaleY = Mathf.Abs(delta.y) > 0.001f
                ? (rect.height * 0.5f) / Mathf.Abs(delta.y)
                : float.MaxValue;
            float scale = Mathf.Min(scaleX, scaleY);
            Vector2 point = center + (delta * Mathf.Clamp01(scale));
            return new Vector2(
                Mathf.Clamp(point.x, rect.x, rect.xMax),
                Mathf.Clamp(point.y, rect.y, rect.yMax));
        }

        private void DrawThinLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            if (Mathf.Abs(start.x - end.x) >= Mathf.Abs(start.y - end.y))
            {
                float x = Mathf.Min(start.x, end.x);
                float width = Mathf.Max(1f, Mathf.Abs(start.x - end.x));
                Widgets.DrawBoxSolid(new Rect(x, start.y, width, thickness), color);
                return;
            }

            float y = Mathf.Min(start.y, end.y);
            float height = Mathf.Max(1f, Mathf.Abs(start.y - end.y));
            Widgets.DrawBoxSolid(new Rect(start.x, y, thickness, height), color);
        }

        private static bool IsSelectedWeapon(
            V3DefenseWeaponEntryModel weapon,
            V3DefensePageReadModel defenseModel)
        {
            return weapon != null &&
                defenseModel != null &&
                defenseModel.SelectedWeapon != null &&
                weapon.Id == defenseModel.SelectedWeapon.Id;
        }
    }
}

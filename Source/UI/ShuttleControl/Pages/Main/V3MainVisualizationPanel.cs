using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainVisualizationPanel
    {
        private const float HeaderHeight = 34f;

        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;
        private readonly V3MainModuleSelection selection;

        internal V3MainVisualizationPanel(
            V3MainText text,
            V3MainPanelDrawer panel,
            V3MainModuleSelection selection)
        {
            this.text = text;
            this.panel = panel;
            this.selection = selection;
        }

        internal void Draw(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Main_ShuttleOverview"));
            Rect innerRect = this.panel.GetPanelInnerRect(rect, HeaderHeight);
            List<ShuttleControlSegmentSlotModel> segments =
                model != null && model.ControlModel != null
                    ? model.ControlModel.SegmentSlots
                    : null;
            if (segments == null || segments.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    innerRect,
                    this.text.Tr("CT_Shuttle_Main_NoSegmentSlots"));
                return;
            }

            this.DrawPaintPreview(innerRect, model, context);
            this.DrawConnectors(innerRect, segments, state);
            for (int i = 0; i < segments.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = segments[i];
                this.DrawNode(
                    this.GetNodeRect(innerRect, i),
                    segment,
                    state);
            }
        }

        private void DrawConnectors(
            Rect innerRect,
            List<ShuttleControlSegmentSlotModel> segments,
            V3MainPageState state)
        {
            if (segments.Count <= 1)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                Rect nodeRect = this.GetNodeRect(innerRect, i);
                Vector2 anchor = this.GetSegmentAnchor(innerRect, i);
                bool selected = segments[i] != null &&
                    segments[i].SlotID == state.SelectedSegmentSlotID;
                this.DrawConnector(
                    this.GetRectEdgePointTowards(nodeRect, anchor),
                    anchor,
                    selected);
            }
        }

        private void DrawNode(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            V3MainPageState state)
        {
            bool selected = segment != null && segment.SlotID == state.SelectedSegmentSlotID;
            bool installed = this.selection.IsSegmentInstalled(segment);
            bool disabled = !installed && segment != null && !segment.IsRequired;
            bool hovered = Mouse.IsOver(rect);
            this.panel.DrawSegmentNodeFrame(rect, selected, hovered, installed, disabled);
            if (V3MainAssemblyFocusFeedback.ShouldDrawSegmentFocus(state, segment))
            {
                V3MainFrameDrawer.DrawFocusBorder(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                this.selection.SelectSegment(state, segment);
                V3MainAssemblyFocusFeedback.FocusSegment(state, segment);
            }

            Text.Font = GameFont.Tiny;
            GUI.color = installed
                ? ShuttleUIStyle.WithAlpha(V3MainText.BlueColor, selected ? 1f : 0.92f)
                : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.92f);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 5f, rect.width - 42f, 18f),
                this.text.FitLabelText(
                    this.GetShortSegmentNodeLabel(segment),
                    rect.width - 42f));
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 8f, rect.y + 25f, Mathf.Max(0f, rect.width - 48f), 1f),
                ShuttleUIStyle.WithAlpha(V3MainText.BlueColor, installed ? 0.18f : 0.10f));
            this.DrawSegmentDescriptorLines(
                new Rect(rect.x + 8f, rect.y + 29f, rect.width - 42f, 36f),
                segment,
                installed
                    ? ShuttleUIStyle.MutedTextColor
                    : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.82f));
            V3MainCardActionRailDrawer.DrawStatusGlyph(
                new Rect(rect.xMax - 28f, rect.y + 8f, 22f, 22f),
                installed,
                installed && !this.selection.HasAnyDisabledInstalledModule(segment),
                segment != null && segment.IsRemovalInProgress,
                this.text.GetSegmentStatusLabel(segment, this.selection),
                selected || hovered);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.text.AddTooltip(rect, this.text.GetSegmentLabel(segment));
        }

        private Rect GetNodeRect(Rect rect, int index)
        {
            Vector2 pos = this.GetSegmentCardPos(index);
            float width = Mathf.Min(156f, Mathf.Max(128f, rect.width * 0.24f));
            float height = 68f;

            float x = rect.x + (rect.width * pos.x) - (width * 0.5f);
            float y = rect.y + (rect.height * pos.y) - (height * 0.5f);
            return new Rect(
                Mathf.Clamp(x, rect.x + 8f, rect.xMax - width - 8f),
                Mathf.Clamp(y, rect.y + 6f, rect.yMax - height - 6f),
                width,
                height);
        }

        private void DrawConnector(Vector2 start, Vector2 end, bool selected)
        {
            Color color = selected ? V3MainText.BlueColor : new Color(0.45f, 0.52f, 0.58f, 0.45f);
            float thickness = selected ? 2f : 1.5f;
            float midX = (start.x + end.x) * 0.5f;
            this.DrawLineSegment(new Vector2(start.x, start.y), new Vector2(midX, start.y), color, thickness);
            this.DrawLineSegment(new Vector2(midX, start.y), new Vector2(midX, end.y), color, thickness);
            this.DrawLineSegment(new Vector2(midX, end.y), new Vector2(end.x, end.y), color, thickness);
        }

        private void DrawLineSegment(Vector2 start, Vector2 end, Color color, float thickness)
        {
            if (Mathf.Abs(start.x - end.x) >= Mathf.Abs(start.y - end.y))
            {
                float x = Mathf.Min(start.x, end.x);
                Widgets.DrawBoxSolid(
                    new Rect(x, start.y, Mathf.Max(1f, Mathf.Abs(start.x - end.x)), thickness),
                    color);
                return;
            }

            float y = Mathf.Min(start.y, end.y);
            Widgets.DrawBoxSolid(
                new Rect(start.x, y, thickness, Mathf.Max(1f, Mathf.Abs(start.y - end.y))),
                color);
        }

        private Vector2 GetSegmentCardPos(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector2(0.12f, 0.18f);
                case 1:
                    return new Vector2(0.50f, 0.12f);
                case 2:
                    return new Vector2(0.88f, 0.18f);
                case 3:
                    return new Vector2(0.12f, 0.84f);
                case 4:
                    return new Vector2(0.50f, 0.84f);
                case 5:
                    return new Vector2(0.88f, 0.84f);
                default:
                    int column = Mathf.Max(0, index) % 3;
                    int row = (Mathf.Max(0, index) / 3) % 2;
                    return new Vector2(0.12f + (0.38f * column), row == 0 ? 0.18f : 0.84f);
            }
        }

        private Vector2 GetSegmentAnchor(Rect rect, int index)
        {
            Vector2 anchor;
            switch (index)
            {
                case 0:
                    anchor = new Vector2(0.42f, 0.43f);
                    break;
                case 1:
                    anchor = new Vector2(0.50f, 0.42f);
                    break;
                case 2:
                    anchor = new Vector2(0.58f, 0.44f);
                    break;
                case 3:
                    anchor = new Vector2(0.43f, 0.56f);
                    break;
                case 4:
                    anchor = new Vector2(0.50f, 0.59f);
                    break;
                case 5:
                    anchor = new Vector2(0.57f, 0.56f);
                    break;
                default:
                    anchor = new Vector2(0.50f, 0.52f);
                    break;
            }

            return new Vector2(
                rect.x + (rect.width * anchor.x),
                rect.y + (rect.height * anchor.y));
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

        private void DrawPaintPreview(
            Rect rect,
            V3MainPageModel model,
            ShuttlePageDrawContext context)
        {
            if (rect.width <= 0f ||
                rect.height <= 0f ||
                model == null ||
                model.ControlModel == null ||
                context == null ||
                context.Services == null ||
                context.Services.PaintBrowserPreviewProvider == null)
            {
                return;
            }

            Texture preview;
            int weaponStationMask =
                ShuttlePaintPreviewWeaponStationMask.FromControlModel(model.ControlModel);
            if (!context.Services.PaintBrowserPreviewProvider.TryGetPreview(
                    model.ControlModel.PaintScheme,
                    weaponStationMask,
                    context.PreviewRotation,
                    out preview) ||
                preview == null)
            {
                return;
            }

            Rect previewRect = GetPaintPreviewRect(rect);
            ShuttlePaintPreviewDrawer.Draw(previewRect, preview);
        }

        private static Rect GetPaintPreviewRect(Rect rect)
        {
            return new Rect(
                rect.x + 34f,
                rect.y + 8f,
                Mathf.Max(0f, rect.width - 68f),
                Mathf.Max(0f, rect.height - 68f));
        }

        private void DrawSegmentDescriptorLines(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            Color color)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = ShuttleUIStyle.WithAlpha(color, 0.92f);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y, rect.width, 18f),
                this.text.FitLabelText(
                    this.GetSegmentRequirementLabel(segment),
                    rect.width));
            GUI.color = ShuttleUIStyle.WithAlpha(color, 0.78f);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y + 18f, rect.width, 18f),
                this.text.FitLabelText(
                    this.GetSegmentRemovalLabel(segment),
                    rect.width));
        }

        private string GetShortSegmentNodeLabel(ShuttleControlSegmentSlotModel segment)
        {
            string label = this.text.GetSegmentLabel(segment);
            if (!string.IsNullOrEmpty(label) && label.Length <= 14)
            {
                return label;
            }

            return this.text.GetSegmentSlotLabel(segment);
        }

        private string GetSegmentRequirementLabel(ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null)
            {
                return "-";
            }

            return segment.IsRequired
                ? this.text.Tr("CT_Shuttle_Main_Required")
                : this.text.Tr("CT_Shuttle_Main_Optional");
        }

        private string GetSegmentRemovalLabel(ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null)
            {
                return "-";
            }

            return this.IsSegmentRemovableDescriptor(segment)
                ? this.text.Tr("CT_Shuttle_Main_Removable")
                : this.text.Tr("CT_Shuttle_Main_Locked");
        }

        private bool IsSegmentRemovableDescriptor(ShuttleControlSegmentSlotModel segment)
        {
            return segment != null &&
                !segment.IsRequired &&
                !segment.IsFixed &&
                !segment.IsLocked;
        }
    }
}

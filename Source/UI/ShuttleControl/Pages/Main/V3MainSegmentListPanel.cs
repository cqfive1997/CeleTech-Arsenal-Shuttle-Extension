using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainSegmentListPanel
    {
        private const float HeaderHeight = 34f;
        private const float RowPitch = 108f;
        private const float CardHeight = 102f;

        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;
        private readonly V3MainSegmentCardDrawer cardDrawer;
        private readonly V3MainModuleSelection selection;

        internal V3MainSegmentListPanel(
            V3MainText text,
            V3MainPanelDrawer panel,
            V3MainSegmentCardDrawer cardDrawer,
            V3MainModuleSelection selection)
        {
            this.text = text;
            this.panel = panel;
            this.cardDrawer = cardDrawer;
            this.selection = selection;
        }

        internal void Draw(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Main_SegmentBrief"));
            Rect innerRect = this.panel.GetPanelInnerRect(rect, HeaderHeight);
            List<ShuttleControlSegmentSlotModel> slots =
                model != null && model.ControlModel != null
                    ? model.ControlModel.SegmentSlots
                    : null;
            if (slots == null || slots.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    innerRect,
                    this.text.Tr("CT_Shuttle_Main_NoSegmentSlots"));
                return;
            }

            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            bool optionalTargetRegistered = false;
            Rect viewRect = new Rect(
                0f,
                0f,
                innerRect.width - 16f,
                slots.Count * RowPitch);
            Widgets.BeginScrollView(innerRect, ref state.SegmentScroll, viewRect);
            float y = 0f;
            for (int i = 0; i < slots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = slots[i];
                Rect rowRect = new Rect(0f, y, viewRect.width, CardHeight);
                optionalTargetRegistered = this.RegisterSegmentTutorialTargets(
                    tutorialTargets,
                    innerRect,
                    rowRect,
                    state.SegmentScroll,
                    segment,
                    optionalTargetRegistered);
                this.cardDrawer.Draw(rowRect, segment, model, state, context);
                y += RowPitch;
            }

            Widgets.EndScrollView();
        }

        private bool RegisterSegmentTutorialTargets(
            IShuttleTutorialTargetService tutorialTargets,
            Rect scrollRect,
            Rect rowRect,
            Vector2 scroll,
            ShuttleControlSegmentSlotModel segment,
            bool optionalTargetRegistered)
        {
            if (tutorialTargets == null || segment == null)
            {
                return optionalTargetRegistered;
            }

            Rect visibleRow = tutorialTargets.GetScrolledVisibleRect(
                scrollRect,
                rowRect,
                scroll);
            if (!IsValid(visibleRow))
            {
                return optionalTargetRegistered;
            }

            string targetId = this.ResolveSegmentTutorialTargetId(
                segment,
                optionalTargetRegistered);
            if (!string.IsNullOrEmpty(targetId))
            {
                tutorialTargets.Register(targetId, visibleRow);
            }

            if (targetId == ShuttleTutorialTargetIds.MainRequiredSegmentCockpit)
            {
                Rect controlsRect = new Rect(
                    rowRect.xMax - 52f,
                    rowRect.y + 8f,
                    44f,
                    Mathf.Max(0f, rowRect.height - 16f));
                tutorialTargets.Register(
                    ShuttleTutorialTargetIds.MainRequiredSegmentCockpitControls,
                    tutorialTargets.GetScrolledVisibleRect(scrollRect, controlsRect, scroll));
            }

            return optionalTargetRegistered ||
                targetId == ShuttleTutorialTargetIds.MainOptionalSegments;
        }

        private string ResolveSegmentTutorialTargetId(
            ShuttleControlSegmentSlotModel segment,
            bool optionalTargetRegistered)
        {
            if (segment == null)
            {
                return null;
            }

            ShuttleSegmentType type = ShuttleSegmentTypeCatalog.ParseSegmentType(segment.SegmentTypeID);
            if (type == ShuttleSegmentType.Unknown)
            {
                type = ShuttleSegmentTypeCatalog.ParseSlotType(segment.SlotTypeID);
            }

            if (segment.IsRequired)
            {
                if (type == ShuttleSegmentType.Cockpit)
                {
                    return ShuttleTutorialTargetIds.MainRequiredSegmentCockpit;
                }

                if (type == ShuttleSegmentType.Power)
                {
                    return ShuttleTutorialTargetIds.MainRequiredSegmentPower;
                }

                if (type == ShuttleSegmentType.Cargo)
                {
                    return ShuttleTutorialTargetIds.MainRequiredSegmentCargo;
                }
            }

            return !optionalTargetRegistered && !segment.IsRequired
                ? ShuttleTutorialTargetIds.MainOptionalSegments
                : null;
        }

        private static bool IsValid(Rect rect)
        {
            return rect.width > 1f && rect.height > 1f;
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.TutorialTargets
                : null;
        }
    }
}

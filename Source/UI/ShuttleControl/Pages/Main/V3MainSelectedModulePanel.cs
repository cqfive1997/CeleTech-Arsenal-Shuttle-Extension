using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainSelectedModulePanel
    {
        private const float HeaderHeight = 44f;
        private const float RowPitch = 78f;
        private const float RowHeight = 72f;
        private const float ConstructionGap = 8f;

        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;
        private readonly V3MainModuleCardDrawer cardDrawer;
        private readonly V3MainConstructionPanel constructionPanel;
        private readonly V3MainModuleSelection selection;

        internal V3MainSelectedModulePanel(
            V3MainText text,
            V3MainPanelDrawer panel,
            V3MainModuleCardDrawer cardDrawer,
            V3MainConstructionPanel constructionPanel,
            V3MainModuleSelection selection)
        {
            this.text = text;
            this.panel = panel;
            this.cardDrawer = cardDrawer;
            this.constructionPanel = constructionPanel;
            this.selection = selection;
        }

        internal void Draw(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context)
        {
            ShuttleControlSegmentSlotModel segment =
                this.selection.FindSelectedSegment(
                    state,
                    model != null ? model.ControlModel : null);
            this.DrawHeader(rect, segment);
            Rect innerRect = this.panel.GetPanelInnerRect(rect, HeaderHeight);
            if (segment == null)
            {
                this.panel.DrawEmptyPanelMessage(
                    innerRect,
                    this.text.Tr("CT_Shuttle_Main_NoSelectedSegment"));
                return;
            }

            float viewWidth = innerRect.width - 16f;
            float contentHeight = this.CalculateContentHeight(segment, model, viewWidth);
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, contentHeight);
            Widgets.BeginScrollView(innerRect, ref state.SelectedModuleScroll, viewRect);
            float y = 0f;
            this.selection.EnsureSelectedModule(state, segment);
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (this.constructionPanel.HasActiveConstruction(model))
            {
                float constructionHeight = this.constructionPanel.GetCardHeight(model, viewRect.width);
                Rect constructionRect = new Rect(0f, y, viewRect.width, constructionHeight);
                this.constructionPanel.Draw(constructionRect, model, context);
                y += constructionHeight + ConstructionGap;
            }

            this.DrawModuleRows(
                viewRect,
                innerRect,
                y,
                segment,
                model,
                state,
                context,
                tutorialTargets);
            Widgets.EndScrollView();
        }

        private void DrawHeader(Rect rect, ShuttleControlSegmentSlotModel segment)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 5f, 150f, 24f),
                this.text.Tr("CT_Shuttle_UI_ModuleReplace_Title"));
            Text.Font = GameFont.Tiny;
            GUI.color = V3MainText.BlueColor;
            Text.Anchor = TextAnchor.UpperRight;
            string currentSegmentLabel = this.text.Tr(
                "CT_Shuttle_UI_ModuleReplace_CurrentSegment",
                this.text.GetSegmentLabel(segment));
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 154f, rect.y + 8f, rect.width - 164f, 20f),
                currentSegmentLabel);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 8f, rect.y + 29f, rect.width - 16f, 1f),
                ShuttleUIStyle.SubtleBorderColor);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawModuleRows(
            Rect viewRect,
            Rect scrollRect,
            float y,
            ShuttleControlSegmentSlotModel segment,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            IShuttleTutorialTargetService tutorialTargets)
        {
            if (segment.ModuleSlots == null || segment.ModuleSlots.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    new Rect(0f, y, viewRect.width, 60f),
                    this.text.Tr("CT_Shuttle_Main_NoModuleSlots"));
                return;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[i];
                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                this.RegisterModuleSlotTutorialTarget(
                    tutorialTargets,
                    scrollRect,
                    rowRect,
                    state.SelectedModuleScroll,
                    moduleSlot);
                this.cardDrawer.Draw(
                    rowRect,
                    segment,
                    moduleSlot,
                    model,
                    state,
                    context,
                    false);
                y += RowPitch;
            }
        }

        private void RegisterModuleSlotTutorialTarget(
            IShuttleTutorialTargetService tutorialTargets,
            Rect scrollRect,
            Rect rowRect,
            Vector2 scroll,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (tutorialTargets == null || moduleSlot == null)
            {
                return;
            }

            string targetId = ResolveModuleSlotTutorialTargetId(moduleSlot);
            if (string.IsNullOrEmpty(targetId))
            {
                return;
            }

            Rect visibleRow = tutorialTargets.GetScrolledVisibleRect(
                scrollRect,
                rowRect,
                scroll);
            if (visibleRow.width <= 1f || visibleRow.height <= 1f)
            {
                return;
            }

            tutorialTargets.Register(targetId, visibleRow);
        }

        private static string ResolveModuleSlotTutorialTargetId(
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (moduleSlot == null)
            {
                return null;
            }

            string rawSlotType = moduleSlot.SlotTypeID;
            if (IsColdStorageSlot(moduleSlot))
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotColdStorage;
            }

            ShuttleModuleType type = ShuttleModuleTypeCatalog.ParseSlotType(rawSlotType);
            if (type == ShuttleModuleType.Cockpit)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotCockpit;
            }

            if (type == ShuttleModuleType.Navigation)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotControl;
            }

            if (type == ShuttleModuleType.Reactor)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotReactor;
            }

            if (type == ShuttleModuleType.Battery)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotBattery;
            }

            if (type == ShuttleModuleType.PowerRegulator)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotPowerRegulator;
            }

            if (type == ShuttleModuleType.Cargo)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotCargo;
            }

            if (type == ShuttleModuleType.Weapon)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotWeapon;
            }

            if (type == ShuttleModuleType.FireControl)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotFireControl;
            }

            if (type == ShuttleModuleType.AmmoLoader)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotAmmoLoader;
            }

            if (type == ShuttleModuleType.Shield)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotShield;
            }

            if (type == ShuttleModuleType.Scanner)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotScanner;
            }

            if (type == ShuttleModuleType.Production)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotProduction;
            }

            if (type == ShuttleModuleType.Habitat)
            {
                return ShuttleTutorialTargetIds.MainContextModuleSlotHabitat;
            }

            return null;
        }

        private static bool IsColdStorageSlot(
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (moduleSlot == null)
            {
                return false;
            }

            return ContainsColdStorageToken(moduleSlot.SlotTypeID) ||
                ContainsColdStorageToken(moduleSlot.SlotID) ||
                ContainsColdStorageToken(moduleSlot.InstalledModuleTypeID) ||
                ContainsColdStorageToken(moduleSlot.InstalledModuleDefName);
        }

        private static bool ContainsColdStorageToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string text = value.ToLowerInvariant();
            return text.Contains("cold") ||
                text.Contains("cool") ||
                text.Contains("refriger");
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.TutorialTargets
                : null;
        }

        private float CalculateContentHeight(
            ShuttleControlSegmentSlotModel segment,
            V3MainPageModel model,
            float viewWidth)
        {
            int rows = segment != null && segment.ModuleSlots != null
                ? segment.ModuleSlots.Count
                : 0;
            float height = (rows * RowPitch) + 12f;

            if (this.constructionPanel.HasActiveConstruction(model))
            {
                height += this.constructionPanel.GetCardHeight(model, viewWidth) + ConstructionGap;
            }

            return Mathf.Max(height, 80f);
        }
    }
}

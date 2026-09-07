using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainSegmentCardDrawer
    {
        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;
        private readonly V3MainModuleSelection selection;
        private readonly V3MainIconKeyResolver iconKeys = new V3MainIconKeyResolver();

        internal V3MainSegmentCardDrawer(
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
            ShuttleControlSegmentSlotModel segment,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context)
        {
            bool selected = segment != null && segment.SlotID == state.SelectedSegmentSlotID;
            bool installed = this.selection.IsSegmentInstalled(segment);
            string actionMenuKey = BuildSegmentActionKey(segment);
            Rect expandRect = V3MainCardActionRailDrawer.GetExpandRect(rect, rect.y + 11f);
            bool menuOpen = V3MainCardActionRailDrawer.IsMenuOpen(state, actionMenuKey);
            bool hovered = Mouse.IsOver(rect);
            Color accent = this.GetAccentColor(segment, installed);
            this.panel.DrawSegmentCardFrame(rect, selected, hovered, menuOpen);
            if (V3MainAssemblyFocusFeedback.ShouldDrawSegmentFocus(state, segment))
            {
                V3MainFrameDrawer.DrawFocusBorder(rect);
            }

            if (Widgets.ButtonInvisible(
                V3MainCardActionRailDrawer.GetSelectRect(rect, expandRect, menuOpen)))
            {
                this.selection.SelectSegment(state, segment);
                V3MainAssemblyFocusFeedback.FocusSegment(state, segment);
                state.OpenMainActionMenuKey = null;
            }

            this.DrawContent(rect, segment, selected, installed, accent, context);
            this.DrawActions(
                rect,
                segment,
                model,
                state,
                context,
                installed,
                actionMenuKey,
                expandRect,
                menuOpen);
        }

        private void DrawContent(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            bool selected,
            bool installed,
            Color accent,
            ShuttlePageDrawContext context)
        {
            const float cardPadding = 10f;
            const float columnGap = 10f;
            const float actionRailWidth = 34f;
            Rect innerRect = new Rect(
                rect.x + cardPadding,
                rect.y + cardPadding,
                Mathf.Max(0f, rect.width - (cardPadding * 2f)),
                Mathf.Max(0f, rect.height - (cardPadding * 2f)));
            Rect actionRailRect = new Rect(
                innerRect.xMax - actionRailWidth,
                innerRect.y,
                actionRailWidth,
                innerRect.height);
            Rect mainRect = new Rect(
                innerRect.x,
                innerRect.y,
                Mathf.Max(0f, innerRect.width - actionRailRect.width - columnGap),
                innerRect.height);
            float visualColumnWidth = Mathf.Floor(mainRect.width * 0.48f);
            Rect visualRect = new Rect(mainRect.x, mainRect.y, visualColumnWidth, mainRect.height);
            Rect infoRect = new Rect(
                visualRect.xMax + columnGap,
                mainRect.y,
                Mathf.Max(0f, mainRect.xMax - visualRect.xMax - columnGap),
                mainRect.height);

            float iconSize = Mathf.Min(
                visualRect.width,
                Mathf.Max(58f, visualRect.height - 8f));
            iconSize = Mathf.Min(iconSize, Mathf.Min(76f, visualRect.height));
            Rect iconRect = new Rect(
                visualRect.x + ((visualRect.width - iconSize) * 0.5f),
                visualRect.y + Mathf.Floor((visualRect.height - iconSize) * 0.42f),
                iconSize,
                iconSize);
            Texture2D icon = this.GetIcon(context, this.iconKeys.GetSegmentSpecificIconKey(segment));
            if (icon == null)
            {
                icon = this.GetIcon(context, this.iconKeys.GetSegmentTypeIconKey(segment));
            }

            this.panel.DrawFramelessIcon(
                iconRect,
                icon,
                this.GetSegmentGlyph(segment),
                accent,
                !installed && segment != null && !segment.IsRequired);

            Rect titleRect = new Rect(infoRect.x, infoRect.y + 3f, infoRect.width, 22f);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                titleRect,
                this.text.FitLabelText(this.text.GetSegmentLabel(segment), titleRect.width));

            this.DrawSegmentDescriptorLines(
                new Rect(infoRect.x, titleRect.yMax + 3f, infoRect.width, 36f),
                segment,
                ShuttleUIStyle.MutedTextColor);

            if (segment != null && segment.IsRemovalInProgress)
            {
                Rect progressRect = new Rect(
                    infoRect.x,
                    infoRect.yMax - 16f,
                    Mathf.Max(60f, rect.xMax - 104f - infoRect.x - 8f),
                    14f);
                this.panel.DrawProgressBar(
                    progressRect,
                    segment.RemovalProgress01,
                    this.text.FormatProgress(segment.RemovalProgress01),
                    V3MainText.YellowColor);
            }
            else
            {
                Rect dotsRect = new Rect(
                    infoRect.x,
                    infoRect.yMax - 18f,
                    infoRect.width,
                    16f);
                V3MainSlotDotsDrawer.Draw(dotsRect, segment != null ? segment.ModuleSlots : null);
            }

            this.text.AddTooltip(rect, this.BuildTooltip(segment, installed));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
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

        private void DrawActions(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            bool installed,
            string actionMenuKey,
            Rect expandRect,
            bool menuOpen)
        {
            float buttonWidth = 76f;
            float buttonHeight = 22f;
            float x = rect.xMax - buttonWidth - 10f;
            float y = rect.y + 12f;
            IShuttleMainSegmentInstallUIActions segmentInstallActions =
                GetSegmentInstallActions(context);
            IShuttleMainRemovalUIActions removalActions = GetRemovalActions(context);
            IShuttleMainRemovalWorkerUIActions removalWorkerActions =
                GetRemovalWorkerActions(context);
            IShuttleMainSegmentModulesEnablementUIActions enablementActions =
                GetSegmentEnablementActions(context);
            if (segmentInstallActions == null &&
                removalActions == null &&
                removalWorkerActions == null &&
                enablementActions == null)
            {
                return;
            }

            if (segment != null && segment.IsRemovalInProgress)
            {
                if (!menuOpen)
                {
                    this.DrawSegmentActionStatusIndicator(segment, installed, expandRect);
                }

                if (removalActions == null && removalWorkerActions == null)
                {
                    return;
                }

                this.DrawRemovalButtons(
                    rect,
                    segment,
                    removalActions,
                    removalWorkerActions,
                    x,
                    y,
                    buttonWidth,
                    buttonHeight);
                return;
            }

            if (installed)
            {
                bool enable = this.selection.HasAnyDisabledInstalledModule(segment);
                bool hasActiveConstructionOrder = HasActiveConstructionOrder(model);
                bool canToggle = enablementActions != null &&
                    enablementActions.CanSetSegmentModulesEnabled(segment, enable);
                bool canReplace = segmentInstallActions != null &&
                    segmentInstallActions.CanReplaceSegment(segment);
                bool canRemove = removalActions != null &&
                    removalActions.CanRemoveSegment(segment, hasActiveConstructionOrder);
                V3MainCardAction action = V3MainCardActionRailDrawer.Draw(
                    rect,
                    state,
                    actionMenuKey,
                    !enable,
                    canReplace,
                    canToggle,
                    canRemove,
                    segmentInstallActions != null
                        ? segmentInstallActions.GetSegmentReplaceTooltip(segment)
                        : null,
                    enablementActions != null
                        ? enablementActions.GetSegmentModulesEnablementTooltip(segment, enable)
                        : null,
                    removalActions != null
                        ? removalActions.GetSegmentRemoveTooltip(segment, hasActiveConstructionOrder)
                        : null,
                    expandRect);
                if (!menuOpen)
                {
                    this.DrawSegmentActionStatusIndicator(segment, installed, expandRect);
                }

                if (action == V3MainCardAction.Manage)
                {
                    V3MainAssemblyFocusFeedback.FocusSegment(state, segment);
                    segmentInstallActions.OpenReplaceSegment(segment);
                }
                else if (action == V3MainCardAction.Enable || action == V3MainCardAction.Disable)
                {
                    V3MainAssemblyFocusFeedback.FocusSegment(state, segment);
                    enablementActions.SetSegmentModulesEnabled(segment, action == V3MainCardAction.Enable);
                }
                else if (action == V3MainCardAction.Remove)
                {
                    V3MainAssemblyFocusFeedback.FocusSegment(state, segment);
                    removalActions.RemoveSegment(segment, hasActiveConstructionOrder);
                }

                return;
            }

            bool canInstall = segmentInstallActions != null &&
                segmentInstallActions.CanInstallSegment(segment);
            V3MainCardAction installAction = V3MainCardActionRailDrawer.Draw(
                rect,
                state,
                actionMenuKey,
                false,
                canInstall,
                false,
                false,
                segmentInstallActions != null
                    ? segmentInstallActions.GetSegmentInstallTooltip(segment)
                    : null,
                this.text.GetSegmentStatusLabel(segment, this.selection),
                this.text.GetSegmentStatusLabel(segment, this.selection),
                expandRect);
            if (!menuOpen)
            {
                this.DrawSegmentActionStatusIndicator(segment, installed, expandRect);
            }

            if (installAction == V3MainCardAction.Manage)
            {
                V3MainAssemblyFocusFeedback.FocusSegment(state, segment);
                segmentInstallActions.OpenInstallSegment(segment);
            }
        }

        private void DrawSegmentActionStatusIndicator(
            ShuttleControlSegmentSlotModel segment,
            bool installed,
            Rect expandRect)
        {
            const float statusIconSize = 30f;
            Rect statusRect = new Rect(
                expandRect.x + ((expandRect.width - statusIconSize) * 0.5f),
                expandRect.yMax + 7f,
                statusIconSize,
                statusIconSize);
            bool enabled = installed && !this.selection.HasAnyDisabledInstalledModule(segment);
            V3MainCardActionRailDrawer.DrawStatusIndicator(
                statusRect,
                installed,
                enabled,
                segment != null && segment.IsRemovalInProgress,
                this.text.GetSegmentStatusLabel(segment, this.selection));
        }

        private void DrawRemovalButtons(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            IShuttleMainRemovalUIActions removalActions,
            IShuttleMainRemovalWorkerUIActions workerActions,
            float x,
            float y,
            float buttonWidth,
            float buttonHeight)
        {
            if (workerActions != null &&
                workerActions.CanAssignSegmentRemovalWorker(segment))
            {
                this.panel.DrawButton(
                    new Rect(x, y, buttonWidth, buttonHeight),
                    this.text.Tr("CT_Shuttle_ModuleRemoval_AssignWorkerShort"),
                    segment.RemovalTooltip,
                    delegate { workerActions.AssignSegmentRemovalWorker(segment); },
                    ShuttleUIButtonKind.Primary);
                y += buttonHeight + 6f;
            }

            if (removalActions != null &&
                removalActions.CanCancelSegmentRemoval(segment))
            {
                this.panel.DrawButton(
                    new Rect(x, y, buttonWidth, buttonHeight),
                    this.text.Tr("CT_Shuttle_ModuleRemoval_Cancel"),
                    segment.RemovalTooltip,
                    delegate { removalActions.CancelSegmentRemoval(segment); },
                    ShuttleUIButtonKind.Danger);
            }
        }

        private static IShuttleMainSegmentInstallUIActions GetSegmentInstallActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.SegmentInstallActions
                : null;
        }

        private static IShuttleMainRemovalUIActions GetRemovalActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.RemovalActions
                : null;
        }

        private static IShuttleMainRemovalWorkerUIActions GetRemovalWorkerActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.RemovalWorkerActions
                : null;
        }

        private static IShuttleMainSegmentModulesEnablementUIActions GetSegmentEnablementActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.SegmentModulesEnablementActions
                : null;
        }

        private static bool HasActiveConstructionOrder(V3MainPageModel model)
        {
            return model != null &&
                model.ControlModel != null &&
                model.ControlModel.AssemblyConstruction != null &&
                model.ControlModel.AssemblyConstruction.HasActiveOrder;
        }

        private static string BuildSegmentActionKey(ShuttleControlSegmentSlotModel segment)
        {
            return "segment:" + (segment != null ? segment.SlotID : "null");
        }

        private Color GetAccentColor(ShuttleControlSegmentSlotModel segment, bool installed)
        {
            if (segment != null && segment.IsRemovalInProgress)
            {
                return V3MainText.YellowColor;
            }

            if (!installed)
            {
                return segment != null && segment.IsRequired
                    ? V3MainText.YellowColor
                    : ShuttleUIStyle.MutedTextColor;
            }

            return this.selection.HasAnyDisabledInstalledModule(segment)
                ? V3MainText.YellowColor
                : V3MainText.GreenColor;
        }

        private string GetSegmentGlyph(ShuttleControlSegmentSlotModel segment)
        {
            string label = this.text.GetSegmentLabel(segment);
            return this.iconKeys.GetSegmentFallbackGlyph(segment, label);
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

        private Texture2D GetIcon(ShuttlePageDrawContext context, string key)
        {
            return context != null && context.Services != null && context.Services.Icons != null
                ? context.Services.Icons.GetIcon(key)
                : null;
        }

        private string BuildTooltip(ShuttleControlSegmentSlotModel segment, bool installed)
        {
            string description =
                ShuttleAssemblyDisplayTextResolver.ResolveSegmentDescription(segment);
            return this.text.GetSegmentLabel(segment) + "\n" +
                this.text.GetSegmentStatusLabel(segment, this.selection) +
                (IsDashOrEmpty(description) ? string.Empty : "\n\n" + description);
        }

        private static bool IsDashOrEmpty(string value)
        {
            return string.IsNullOrEmpty(value) || value == "-";
        }
    }
}

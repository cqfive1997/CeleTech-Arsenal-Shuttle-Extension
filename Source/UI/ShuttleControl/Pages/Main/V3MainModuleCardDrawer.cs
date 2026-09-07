using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainModuleCardDrawer
    {
        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;
        private readonly V3MainModuleSelection selection;
        private readonly V3MainIconKeyResolver iconKeys = new V3MainIconKeyResolver();

        internal V3MainModuleCardDrawer(
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
            ShuttleControlModuleSlotModel moduleSlot,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            bool compact)
        {
            bool installed = this.selection.IsModuleInstalled(moduleSlot);
            bool selected = moduleSlot != null && moduleSlot.SlotID == state.SelectedModuleSlotID &&
                segment != null && segment.SlotID == state.SelectedSegmentSlotID;
            string actionMenuKey = BuildModuleActionKey(segment, moduleSlot, compact);
            Rect expandRect = V3MainCardActionRailDrawer.GetExpandRect(
                rect,
                rect.y + Mathf.Floor((rect.height - 30f) * 0.5f));
            bool menuOpen = V3MainCardActionRailDrawer.IsMenuOpen(state, actionMenuKey);
            Color accent = this.text.GetInstalledStateColor(
                installed,
                moduleSlot != null && moduleSlot.InstalledModuleEnabled,
                moduleSlot != null && moduleSlot.IsRemovalInProgress);
            this.panel.DrawModuleRowFrame(
                rect,
                installed,
                moduleSlot != null && moduleSlot.InstalledModuleEnabled,
                selected,
                moduleSlot != null && moduleSlot.IsRemovalInProgress);
            if (V3MainAssemblyFocusFeedback.ShouldDrawModuleFocus(state, moduleSlot))
            {
                V3MainFrameDrawer.DrawFocusBorder(rect);
            }

            Rect selectRect = compact
                ? V3MainCardActionRailDrawer.GetSelectRect(rect, expandRect, menuOpen)
                : GetSelectedModuleSelectRect(rect, moduleSlot, installed);
            if (Widgets.ButtonInvisible(selectRect))
            {
                this.selection.SelectModule(state, segment, moduleSlot);
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                state.OpenMainActionMenuKey = null;
            }

            this.DrawLabels(rect, segment, moduleSlot, installed, accent, context, compact);
            this.DrawActions(
                rect,
                segment,
                moduleSlot,
                model,
                state,
                context,
                installed,
                compact,
                actionMenuKey,
                expandRect,
                menuOpen);
        }

        private static Rect GetSelectedModuleSelectRect(
            Rect rect,
            ShuttleControlModuleSlotModel moduleSlot,
            bool installed)
        {
            const float actionRightInset = 8f;
            const float actionGap = 6f;
            float actionWidth = installed
                ? ((moduleSlot != null && moduleSlot.IsRemovalInProgress) ? 72f : 100f)
                : 30f;
            float selectRight = rect.xMax - actionRightInset - actionWidth - actionGap;
            return new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, selectRight - rect.x),
                rect.height);
        }

        private void DrawLabels(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool installed,
            Color accent,
            ShuttlePageDrawContext context,
            bool compact)
        {
            if (compact)
            {
                this.DrawGlobalModuleLabels(rect, segment, moduleSlot, installed, accent, context);
                return;
            }

            this.DrawSelectedModuleLabels(rect, segment, moduleSlot, installed, accent, context);
        }

        private void DrawGlobalModuleLabels(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool installed,
            Color accent,
            ShuttlePageDrawContext context)
        {
            Rect iconRect = new Rect(rect.x + 7f, rect.y + 9f, 34f, 34f);
            Texture2D icon = this.ResolveModuleIcon(context, moduleSlot);
            this.panel.DrawFramelessIcon(
                iconRect,
                icon,
                this.iconKeys.GetModuleFallbackGlyph(moduleSlot, installed),
                accent,
                !installed);

            const float railGap = 6f;
            const float railButtonSize = 30f;
            const float installedIndicatorWidth = 70f;
            float railWidth =
                installedIndicatorWidth + railGap + railButtonSize + railGap + railButtonSize;
            Rect installedIndicatorRect = new Rect(
                rect.xMax - railWidth - 8f,
                rect.y + Mathf.Floor((rect.height - 18f) * 0.5f),
                installedIndicatorWidth,
                18f);
            float contentWidth =
                Mathf.Max(96f, installedIndicatorRect.x - iconRect.xMax - 14f);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 6f, contentWidth, 18f),
                this.text.FitLabelText(
                    this.text.GetModuleSlotLabel(segment, moduleSlot),
                    contentWidth));
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 27f, contentWidth, 18f),
                this.text.FitLabelText(
                    this.text.Tr("CT_Shuttle_Main_Module") + ": " +
                    (installed ? this.text.GetModuleLabel(moduleSlot) : "-"),
                    contentWidth));
            this.DrawGlobalInstallStatusIndicator(installedIndicatorRect, installed);
            this.text.AddTooltip(rect, this.BuildTooltip(segment, moduleSlot));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawSelectedModuleLabels(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool installed,
            Color accent,
            ShuttlePageDrawContext context)
        {
            const float cardPadding = 10f;
            const float actionRightInset = 8f;
            const float actionButtonSize = 30f;
            const float actionButtonGap = 5f;
            const float iconTextGap = 10f;
            const float actionContentGap = 6f;

            bool activeOperation = installed &&
                moduleSlot != null &&
                moduleSlot.IsRemovalInProgress;
            float selectedTop = rect.y + cardPadding;
            float selectedHeight = rect.height - (cardPadding * 2f);
            float actionColumnWidth = activeOperation
                ? 72f
                : (installed
                    ? (actionButtonSize * 3f) + (actionButtonGap * 2f)
                    : actionButtonSize);
            Rect actionColumnRect = new Rect(
                rect.xMax - actionColumnWidth - actionRightInset,
                selectedTop,
                actionColumnWidth,
                selectedHeight);
            float iconSize = Mathf.Min(58f, Mathf.Max(40f, rect.height - 14f));
            Rect iconRect = new Rect(
                rect.x + cardPadding,
                rect.y + Mathf.Floor((rect.height - iconSize) * 0.5f),
                iconSize,
                iconSize);
            Rect infoRect = new Rect(
                iconRect.xMax + iconTextGap,
                selectedTop,
                Mathf.Max(
                    0f,
                    actionColumnRect.x - iconRect.xMax - iconTextGap - actionContentGap),
                selectedHeight);
            Texture2D icon = this.ResolveModuleIcon(context, moduleSlot);
            this.panel.DrawFramelessIcon(
                iconRect,
                icon,
                this.iconKeys.GetModuleFallbackGlyph(moduleSlot, installed),
                accent,
                !installed);

            Text.Font = GameFont.Tiny;
            if (activeOperation)
            {
                Rect slotLabelRect = new Rect(infoRect.x, infoRect.y, infoRect.width, 18f);
                Rect moduleLabelRect = new Rect(infoRect.x, slotLabelRect.yMax + 3f, infoRect.width, 18f);
                Rect progressRect = new Rect(
                    infoRect.x,
                    moduleLabelRect.yMax + 2f,
                    infoRect.width,
                    14f);
                GUI.color = Color.white;
                ShuttleUILayout.SafeLabel(
                    slotLabelRect,
                    this.text.FitLabelText(
                        this.text.GetModuleSlotLabel(segment, moduleSlot),
                        slotLabelRect.width));
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    moduleLabelRect,
                    this.text.FitLabelText(
                        this.text.GetModuleLabel(moduleSlot),
                        moduleLabelRect.width));
                this.panel.DrawProgressBar(
                    progressRect,
                    moduleSlot.RemovalProgress01,
                    this.text.FormatProgress(moduleSlot.RemovalProgress01),
                    V3MainText.YellowColor);
            }
            else
            {
                float replacementInfoHeight = 18f + (21f * 2f);
                float replacementInfoTop =
                    iconRect.y + Mathf.Floor((iconRect.height - replacementInfoHeight) * 0.5f);
                GUI.color = Color.white;
                ShuttleUILayout.SafeLabel(
                    new Rect(infoRect.x, replacementInfoTop, infoRect.width, 18f),
                    this.text.FitLabelText(
                        this.text.GetModuleSlotLabel(segment, moduleSlot),
                        infoRect.width));
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    new Rect(infoRect.x, replacementInfoTop + 21f, infoRect.width, 18f),
                    this.text.FitLabelText(
                        this.text.GetModuleStatusLabel(moduleSlot, this.selection) +
                        this.GetModuleSlotTags(moduleSlot),
                        infoRect.width));
                GUI.color = installed ? Color.white : ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    new Rect(infoRect.x, replacementInfoTop + 42f, infoRect.width, 18f),
                    this.text.FitLabelText(
                        installed ? this.text.GetModuleLabel(moduleSlot) : "-",
                        infoRect.width));
            }

            this.text.AddTooltip(
                new Rect(
                    rect.x,
                    rect.y,
                    Mathf.Max(0f, actionColumnRect.x - rect.x - 6f),
                    rect.height),
                this.BuildTooltip(segment, moduleSlot));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawGlobalInstallStatusIndicator(Rect rect, bool installed)
        {
            Color tint = installed
                ? ShuttleUIStyle.BlueStatusColor
                : ShuttleUIStyle.MutedTextColor;
            Rect dotRect = new Rect(
                rect.x,
                rect.y + Mathf.Floor((rect.height - 7f) * 0.5f),
                7f,
                7f);
            if (installed)
            {
                Widgets.DrawBoxSolid(dotRect, ShuttleUIStyle.WithAlpha(tint, 0.88f));
            }
            else
            {
                ShuttleUILayout.DrawRectBorder(
                    dotRect,
                    ShuttleUIStyle.WithAlpha(tint, 0.62f),
                    1f);
            }

            GUI.color = installed
                ? ShuttleUIStyle.WithAlpha(tint, 0.94f)
                : ShuttleUIStyle.WithAlpha(tint, 0.78f);
            Text.Font = GameFont.Tiny;
            ShuttleUILayout.SafeLabel(
                new Rect(dotRect.xMax + 5f, rect.y, rect.width - dotRect.width - 5f, rect.height),
                this.text.FitLabelText(
                    installed
                        ? this.text.Tr("CT_Shuttle_Main_Installed")
                        : this.text.Tr("CT_Shuttle_Main_Empty"),
                    rect.width - dotRect.width - 5f));
            GUI.color = Color.white;
        }

        private string GetModuleSlotTags(ShuttleControlModuleSlotModel moduleSlot)
        {
            if (moduleSlot == null)
            {
                return string.Empty;
            }

            string tags = string.Empty;
            if (moduleSlot.IsRequired)
            {
                tags = this.AppendTag(tags, this.text.Tr("CT_Shuttle_Main_Required"));
            }

            if (moduleSlot.IsLocked)
            {
                tags = this.AppendTag(tags, this.text.Tr("CT_Shuttle_UI_ModuleReplace_Status_Locked"));
            }

            return string.IsNullOrEmpty(tags) ? string.Empty : " / " + tags;
        }

        private string AppendTag(string tags, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return tags;
            }

            return string.IsNullOrEmpty(tags)
                ? tag
                : tags + " / " + tag;
        }

        private Texture2D ResolveModuleIcon(
            ShuttlePageDrawContext context,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            Texture2D icon = this.GetIcon(context, this.iconKeys.GetModuleSpecificIconKey(moduleSlot));
            if (icon == null)
            {
                icon = this.GetIcon(context, this.iconKeys.GetModuleTypeIconKey(moduleSlot));
            }

            return icon;
        }

        private Rect GetModuleReplaceStatusMarkerRect(Rect rect)
        {
            return new Rect(
                rect.xMax - 11f - 8f,
                rect.yMax - 9f - 8f,
                8f,
                8f);
        }

        private void DrawModuleReplaceStatusMarker(
            Rect rect,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (!this.selection.IsModuleInstalled(moduleSlot))
            {
                return;
            }

            bool running = moduleSlot != null && moduleSlot.InstalledModuleEnabled;
            Color color = running ? V3MainText.GreenColor : V3MainText.RedColor;
            if (running)
            {
                Widgets.DrawBoxSolid(rect, ShuttleUIStyle.WithAlpha(color, 0.88f));
            }
            else
            {
                ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.WithAlpha(color, 0.88f), 1f);
            }

            this.text.AddTooltip(rect, this.text.GetModuleStatusLabel(moduleSlot, this.selection));
        }

        private void DrawSelectedModuleActions(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            bool installed,
            IShuttleMainModuleInstallUIActions moduleInstallActions,
            IShuttleMainModuleReplaceUIActions moduleReplaceActions,
            IShuttleMainRemovalUIActions removalActions,
            IShuttleMainModuleEnablementUIActions moduleEnablementActions)
        {
            const float actionButtonSize = 30f;
            const float actionButtonGap = 5f;
            const float actionRightInset = 8f;
            float top = rect.y + 10f;

            if (!installed)
            {
                Rect installRect = new Rect(
                    rect.xMax - actionButtonSize - actionRightInset,
                    top,
                    actionButtonSize,
                    actionButtonSize);
                bool canInstall = moduleInstallActions != null &&
                    moduleInstallActions.CanInstallModule(moduleSlot);
                if (V3MainCardActionRailDrawer.DrawManageButton(
                        installRect,
                        canInstall,
                        moduleInstallActions != null
                            ? moduleInstallActions.GetModuleInstallTooltip(segment, moduleSlot)
                            : null,
                        false))
                {
                    V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                    moduleInstallActions.OpenInstallModule(segment, moduleSlot);
                }

                return;
            }

            ExternalModuleUIReadModel externalModel = this.FindExternalRuntime(model, moduleSlot);
            bool canManageExternal = externalModel != null &&
                context != null &&
                context.MainPageContext != null &&
                context.MainPageContext.OpenExternalRuntime != null;
            bool canReplace = !canManageExternal &&
                moduleReplaceActions != null &&
                moduleReplaceActions.CanReplaceModule(moduleSlot);
            bool canManage = canManageExternal || canReplace;
            string manageTooltip = canManageExternal
                ? this.text.ValueOrDash(externalModel.StatusText)
                : moduleReplaceActions != null
                    ? moduleReplaceActions.GetModuleReplaceTooltip(segment, moduleSlot)
                    : null;
            bool enable = moduleSlot != null && !moduleSlot.InstalledModuleEnabled;
            bool canToggle = moduleEnablementActions != null &&
                moduleEnablementActions.CanSetModuleEnabled(segment, moduleSlot, enable);
            bool hasActiveConstructionOrder = HasActiveConstructionOrder(model);
            bool canRemove = removalActions != null &&
                removalActions.CanRemoveModule(moduleSlot, hasActiveConstructionOrder);
            Rect manageRect = new Rect(
                rect.xMax - ((actionButtonSize * 3f) + (actionButtonGap * 2f)) - actionRightInset,
                top,
                actionButtonSize,
                actionButtonSize);
            Rect powerRect = new Rect(
                manageRect.xMax + actionButtonGap,
                top,
                actionButtonSize,
                actionButtonSize);
            Rect removeRect = new Rect(
                powerRect.xMax + actionButtonGap,
                top,
                actionButtonSize,
                actionButtonSize);

            if (V3MainCardActionRailDrawer.DrawManageButton(
                    manageRect,
                    canManage,
                    manageTooltip,
                    false))
            {
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                if (canManageExternal)
                {
                    context.MainPageContext.OpenExternalRuntime(
                        externalModel.ModuleInstanceID,
                        externalModel.RuntimeSystemKey);
                }
                else
                {
                    moduleReplaceActions.OpenReplaceModule(segment, moduleSlot);
                }
            }

            if (V3MainCardActionRailDrawer.DrawToggleButton(
                    powerRect,
                    moduleSlot != null && moduleSlot.InstalledModuleEnabled,
                    canToggle,
                    moduleEnablementActions != null
                        ? moduleEnablementActions.GetModuleEnablementTooltip(segment, moduleSlot, enable)
                        : null))
            {
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                moduleEnablementActions.SetModuleEnabled(segment, moduleSlot, enable);
            }

            if (V3MainCardActionRailDrawer.DrawRemoveButton(
                    removeRect,
                    canRemove,
                    removalActions != null
                        ? removalActions.GetModuleRemoveTooltip(moduleSlot, hasActiveConstructionOrder)
                        : null))
            {
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                removalActions.RemoveModule(segment, moduleSlot, hasActiveConstructionOrder);
            }

            this.DrawModuleReplaceStatusMarker(
                this.GetModuleReplaceStatusMarkerRect(rect),
                moduleSlot);
        }

        private void DrawActions(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            bool installed,
            bool compact,
            string actionMenuKey,
            Rect expandRect,
            bool menuOpen)
        {
            float buttonWidth = compact ? 62f : 72f;
            float buttonHeight = 22f;
            float x = rect.xMax - buttonWidth - 10f;
            float y = rect.y + 8f;
            IShuttleMainModuleInstallUIActions moduleInstallActions =
                GetModuleInstallActions(context);
            IShuttleMainModuleReplaceUIActions moduleReplaceActions =
                GetModuleReplaceActions(context);
            IShuttleMainRemovalUIActions removalActions = GetRemovalActions(context);
            IShuttleMainRemovalWorkerUIActions removalWorkerActions =
                GetRemovalWorkerActions(context);
            IShuttleMainModuleEnablementUIActions moduleEnablementActions =
                GetModuleEnablementActions(context);
            if (moduleInstallActions == null &&
                moduleReplaceActions == null &&
                removalActions == null &&
                removalWorkerActions == null &&
                moduleEnablementActions == null)
            {
                return;
            }

            if (moduleSlot != null && moduleSlot.IsRemovalInProgress)
            {
                if (compact && !menuOpen)
                {
                    this.DrawModuleActionStatusIndicator(moduleSlot, installed, expandRect);
                }

                if (removalActions == null && removalWorkerActions == null)
                {
                    return;
                }

                this.DrawRemovalButtons(
                    rect,
                    moduleSlot,
                    removalActions,
                    removalWorkerActions,
                    compact ? x : rect.xMax - 80f,
                    compact ? y : rect.y + 10f,
                    compact ? buttonWidth : 72f,
                    compact ? buttonHeight : 24f);
                return;
            }

            if (!compact)
            {
                this.DrawSelectedModuleActions(
                    rect,
                    segment,
                    moduleSlot,
                    model,
                    state,
                    context,
                    installed,
                    moduleInstallActions,
                    moduleReplaceActions,
                    removalActions,
                    moduleEnablementActions);
                return;
            }

            if (!installed)
            {
                bool canInstall = moduleInstallActions != null &&
                    moduleInstallActions.CanInstallModule(moduleSlot);
                V3MainCardAction installAction = V3MainCardActionRailDrawer.Draw(
                    rect,
                    state,
                    actionMenuKey,
                    false,
                    canInstall,
                    false,
                    false,
                    moduleInstallActions != null
                        ? moduleInstallActions.GetModuleInstallTooltip(segment, moduleSlot)
                        : null,
                    this.text.GetModuleStatusLabel(moduleSlot, this.selection),
                    this.text.GetModuleStatusLabel(moduleSlot, this.selection),
                    expandRect);
                if (!menuOpen)
                {
                    this.DrawModuleActionStatusIndicator(moduleSlot, installed, expandRect);
                }

                if (installAction == V3MainCardAction.Manage)
                {
                    V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                    moduleInstallActions.OpenInstallModule(segment, moduleSlot);
                }

                return;
            }

            this.DrawInstalledButtons(
                rect,
                segment,
                moduleSlot,
                model,
                state,
                context,
                moduleReplaceActions,
                removalActions,
                moduleEnablementActions,
                actionMenuKey,
                expandRect,
                menuOpen);
        }

        private void DrawInstalledButtons(
            Rect rect,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            IShuttleMainModuleReplaceUIActions moduleReplaceActions,
            IShuttleMainRemovalUIActions removalActions,
            IShuttleMainModuleEnablementUIActions moduleEnablementActions,
            string actionMenuKey,
            Rect expandRect,
            bool menuOpen)
        {
            bool enable = moduleSlot != null && !moduleSlot.InstalledModuleEnabled;
            ExternalModuleUIReadModel externalModel = this.FindExternalRuntime(model, moduleSlot);
            bool canManageExternal = externalModel != null &&
                context != null &&
                context.MainPageContext != null &&
                context.MainPageContext.OpenExternalRuntime != null;
            bool canReplace = !canManageExternal &&
                moduleReplaceActions != null &&
                moduleReplaceActions.CanReplaceModule(moduleSlot);
            bool canManage = canManageExternal || canReplace;
            string manageTooltip = canManageExternal
                ? this.text.ValueOrDash(externalModel.StatusText)
                : moduleReplaceActions != null
                    ? moduleReplaceActions.GetModuleReplaceTooltip(segment, moduleSlot)
                    : null;
            bool canToggle = moduleEnablementActions != null &&
                moduleEnablementActions.CanSetModuleEnabled(segment, moduleSlot, enable);
            bool hasActiveConstructionOrder = HasActiveConstructionOrder(model);
            bool canRemove = removalActions != null &&
                removalActions.CanRemoveModule(moduleSlot, hasActiveConstructionOrder);

            V3MainCardAction action = V3MainCardActionRailDrawer.Draw(
                rect,
                state,
                actionMenuKey,
                moduleSlot != null && moduleSlot.InstalledModuleEnabled,
                canManage,
                canToggle,
                canRemove,
                manageTooltip,
                moduleEnablementActions != null
                    ? moduleEnablementActions.GetModuleEnablementTooltip(segment, moduleSlot, enable)
                    : null,
                removalActions != null
                    ? removalActions.GetModuleRemoveTooltip(moduleSlot, hasActiveConstructionOrder)
                    : null,
                expandRect);
            if (!menuOpen)
            {
                this.DrawModuleActionStatusIndicator(moduleSlot, true, expandRect);
            }

            if (action == V3MainCardAction.Manage)
            {
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                if (canManageExternal)
                {
                    context.MainPageContext.OpenExternalRuntime(
                        externalModel.ModuleInstanceID,
                        externalModel.RuntimeSystemKey);
                    return;
                }

                moduleReplaceActions.OpenReplaceModule(segment, moduleSlot);
            }
            else if (action == V3MainCardAction.Enable || action == V3MainCardAction.Disable)
            {
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                moduleEnablementActions.SetModuleEnabled(
                    segment,
                    moduleSlot,
                    action == V3MainCardAction.Enable);
            }
            else if (action == V3MainCardAction.Remove)
            {
                V3MainAssemblyFocusFeedback.FocusModule(state, moduleSlot);
                removalActions.RemoveModule(segment, moduleSlot, hasActiveConstructionOrder);
            }
        }

        private void DrawModuleActionStatusIndicator(
            ShuttleControlModuleSlotModel moduleSlot,
            bool installed,
            Rect expandRect)
        {
            const float railGap = 6f;
            const float statusIconSize = 30f;
            Rect statusRect = new Rect(
                expandRect.x - statusIconSize - railGap,
                expandRect.y,
                statusIconSize,
                statusIconSize);
            V3MainCardActionRailDrawer.DrawStatusIndicator(
                statusRect,
                installed,
                moduleSlot != null && moduleSlot.InstalledModuleEnabled,
                moduleSlot != null && moduleSlot.IsRemovalInProgress,
                this.text.GetModuleStatusLabel(moduleSlot, this.selection));
        }

        private void DrawRemovalButtons(
            Rect rect,
            ShuttleControlModuleSlotModel moduleSlot,
            IShuttleMainRemovalUIActions removalActions,
            IShuttleMainRemovalWorkerUIActions workerActions,
            float x,
            float y,
            float buttonWidth,
            float buttonHeight)
        {
            if (workerActions != null &&
                workerActions.CanAssignModuleRemovalWorker(moduleSlot))
            {
                this.panel.DrawButton(
                    new Rect(x, y, buttonWidth, buttonHeight),
                    this.text.Tr("CT_Shuttle_ModuleRemoval_AssignWorkerShort"),
                    moduleSlot.RemovalTooltip,
                    delegate { workerActions.AssignModuleRemovalWorker(moduleSlot); },
                    ShuttleUIButtonKind.Primary);
                y += buttonHeight + 5f;
            }

            if (removalActions != null &&
                removalActions.CanCancelModuleRemoval(moduleSlot))
            {
                this.panel.DrawButton(
                    new Rect(x, y, buttonWidth, buttonHeight),
                    this.text.Tr("CT_Shuttle_ModuleRemoval_Cancel"),
                    moduleSlot.RemovalTooltip,
                    delegate { removalActions.CancelModuleRemoval(moduleSlot); },
                    ShuttleUIButtonKind.Danger);
            }
        }

        private static IShuttleMainModuleInstallUIActions GetModuleInstallActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.ModuleInstallActions
                : null;
        }

        private static IShuttleMainModuleReplaceUIActions GetModuleReplaceActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.ModuleReplaceActions
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

        private static IShuttleMainModuleEnablementUIActions GetModuleEnablementActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.ModuleEnablementActions
                : null;
        }

        private static bool HasActiveConstructionOrder(V3MainPageModel model)
        {
            return model != null &&
                model.ControlModel != null &&
                model.ControlModel.AssemblyConstruction != null &&
                model.ControlModel.AssemblyConstruction.HasActiveOrder;
        }

        private static string BuildModuleActionKey(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool compact)
        {
            return (compact ? "global-module:" : "selected-module:") +
                (segment != null ? segment.SlotID : "null") +
                ":" +
                (moduleSlot != null ? moduleSlot.SlotID : "null");
        }

        private Texture2D GetIcon(ShuttlePageDrawContext context, string key)
        {
            return context != null && context.Services != null && context.Services.Icons != null
                ? context.Services.Icons.GetIcon(key)
                : null;
        }

        private ExternalModuleUIReadModel FindExternalRuntime(
            V3MainPageModel model,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (model == null || model.ExternalModuleModels == null || moduleSlot == null)
            {
                return null;
            }

            for (int i = 0; i < model.ExternalModuleModels.Count; i++)
            {
                ExternalModuleUIReadModel externalModel = model.ExternalModuleModels[i];
                if (externalModel != null &&
                    externalModel.ModuleInstanceID == moduleSlot.InstalledModuleInstanceID &&
                    externalModel.HasExternalPanel)
                {
                    return externalModel;
                }
            }

            return null;
        }

        private string GetModuleDetail(ShuttleControlModuleSlotModel moduleSlot)
        {
            if (moduleSlot == null)
            {
                return "-";
            }

            if (!string.IsNullOrEmpty(moduleSlot.InstalledModuleTypeID))
            {
                return moduleSlot.InstalledModuleTypeID;
            }

            return this.text.ValueOrDash(moduleSlot.SlotTypeID);
        }

        private string BuildTooltip(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            string description =
                ShuttleAssemblyDisplayTextResolver.ResolveModuleDescription(moduleSlot);
            return this.text.GetModuleSlotLabel(segment, moduleSlot) + "\n" +
                this.text.GetModuleStatusLabel(moduleSlot, this.selection) +
                (IsDashOrEmpty(description) ? string.Empty : "\n\n" + description);
        }

        private static bool IsDashOrEmpty(string value)
        {
            return string.IsNullOrEmpty(value) || value == "-";
        }
    }
}

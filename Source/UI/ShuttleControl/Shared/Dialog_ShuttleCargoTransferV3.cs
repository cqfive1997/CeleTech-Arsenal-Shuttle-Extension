using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoTransferV3 : Window
    {
        private const float ButtonWidth = 118f;
        private const float FooterPadding = 12f;
        private const float SourceWidth = 236f;

        private readonly IShuttleCargoStackTransferUIActions actions;
        private readonly Action onSuccess;
        private readonly V3CargoTransferSelectionModel selectionModel;
        private readonly V3CargoTransferTargetRowDrawer targetRowDrawer =
            new V3CargoTransferTargetRowDrawer();

        private Vector2 targetScroll;
        private string countBuffer;
        private bool focusedInput;

        internal Dialog_ShuttleCargoTransferV3(
            ShuttleCargoStackActionTarget stack,
            ShuttleCargoPageActionContext pageContext,
            IShuttleCargoStackTransferUIActions actions,
            Action onSuccess)
        {
            this.actions = actions;
            this.onSuccess = onSuccess;
            this.selectionModel = new V3CargoTransferSelectionModel(stack, pageContext);
            this.countBuffer = this.selectionModel.Count.ToString();
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(620f, 460f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.SyncCountFromBuffer();
            ShuttleUILayout.DrawPanelBackground(inRect);
            float countHeight = this.GetCountControlsHeight();

            Rect headerRect = new Rect(
                inRect.x,
                inRect.y,
                inRect.width,
                V3CargoTransferDialogStyle.HeaderHeight);
            Rect footerRect = new Rect(
                inRect.x,
                inRect.yMax - V3CargoTransferDialogStyle.FooterHeight,
                inRect.width,
                V3CargoTransferDialogStyle.FooterHeight);
            Rect countRect = new Rect(
                inRect.x,
                footerRect.y - V3CargoTransferDialogStyle.Gap -
                    countHeight,
                inRect.width,
                countHeight);
            Rect bodyRect = new Rect(
                inRect.x,
                headerRect.yMax + V3CargoTransferDialogStyle.Gap,
                inRect.width,
                Mathf.Max(0f, countRect.y - headerRect.yMax - V3CargoTransferDialogStyle.Gap));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawCountControls(countRect);
            this.DrawFooter(footerRect);
            this.HandleSubmitHotkey();
        }

        private void DrawHeader(Rect rect)
        {
            Rect titleRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 28f);
            Rect directionRect = new Rect(rect.x + 4f, titleRect.yMax + 4f, rect.width - 8f, 18f);
            Rect massRect = new Rect(rect.x + 4f, directionRect.yMax + 2f, rect.width - 8f, 16f);

            Text.Font = GameFont.Medium;
            GUI.color = this.selectionModel.IsToLoadedCargo()
                ? V3CargoTransferDialogStyle.YellowColor
                : V3CargoTransferDialogStyle.ColdColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.GetTitle(this.selectionModel),
                    titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                directionRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.GetDirectionText(this.selectionModel),
                    directionRect.width));
            ShuttleUILayout.SafeLabel(
                massRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.GetSelectedMassText(this.selectionModel),
                    massRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect)
        {
            Rect sourceRect = new Rect(rect.x, rect.y, SourceWidth, rect.height);
            Rect targetRect = new Rect(
                sourceRect.xMax + V3CargoTransferDialogStyle.Gap,
                rect.y,
                rect.width - SourceWidth - V3CargoTransferDialogStyle.Gap,
                rect.height);

            this.DrawSourceCard(sourceRect);
            if (this.selectionModel.IsToLoadedCargo())
            {
                this.DrawNormalTargetCard(targetRect);
            }
            else
            {
                this.DrawColdTargetList(targetRect);
            }
        }

        private void DrawSourceCard(Rect rect)
        {
            V3CargoTransferDialogStyle.DrawCard(rect);
            Rect inner = rect.ContractedBy(10f);
            Rect iconRect = new Rect(inner.x, inner.y, 46f, 46f);
            Rect labelRect = new Rect(iconRect.xMax + 10f, inner.y + 2f, inner.width - 56f, 22f);
            Rect massRect = new Rect(labelRect.x, labelRect.yMax + 4f, labelRect.width, 18f);
            Rect sourceRect = new Rect(inner.x, iconRect.yMax + 12f, inner.width, 18f);

            V3CargoTransferDialogStyle.DrawThingIcon(
                iconRect,
                this.selectionModel.Stack != null ? this.selectionModel.Stack.DisplayThing : null);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                labelRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.GetStackLabel(this.selectionModel.Stack),
                    labelRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                massRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.GetStackMassText(this.selectionModel.Stack),
                    massRect.width));
            ShuttleUILayout.SafeLabel(
                sourceRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.Tr("CT_Shuttle_Cargo_Source") + ": " +
                    V3CargoTransferText.GetSourceText(this.selectionModel.Stack),
                    sourceRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawNormalTargetCard(Rect rect)
        {
            V3CargoTransferDialogStyle.DrawCard(rect);
            Rect inner = rect.ContractedBy(10f);
            Rect titleRect = new Rect(inner.x, inner.y + 3f, inner.width, 24f);
            Rect statusRect = new Rect(inner.x, titleRect.yMax + 8f, inner.width, 18f);
            Rect capacityRect = new Rect(inner.x, statusRect.yMax + 8f, inner.width, 18f);
            string blocker = this.selectionModel.GetSelectedBlocker();

            Text.Font = GameFont.Small;
            GUI.color = V3CargoTransferDialogStyle.YellowColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.Tr("CT_Shuttle_Cargo_SourceLoaded"),
                    titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = string.IsNullOrEmpty(blocker)
                ? ShuttleUIStyle.MutedTextColor
                : V3CargoTransferDialogStyle.YellowColor;
            ShuttleUILayout.SafeLabel(
                statusRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    string.IsNullOrEmpty(blocker)
                        ? V3CargoTransferText.GetDestinationStatusText(this.selectionModel)
                        : blocker,
                    statusRect.width));
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                capacityRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.GetCapacityText(this.selectionModel),
                    capacityRect.width));
            Text.Font = GameFont.Small;
        }

        private void DrawColdTargetList(Rect rect)
        {
            V3CargoTransferDialogStyle.DrawCard(rect);
            Rect inner = rect.ContractedBy(8f);
            int count = this.selectionModel.DestinationBayCount();
            if (count == 0)
            {
                this.DrawEmptyTarget(inner);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                inner.width - 16f,
                Mathf.Max(inner.height, count * V3CargoTransferDialogStyle.RowStride));
            Widgets.BeginScrollView(inner, ref this.targetScroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            this.GetVisibleRowRange(
                inner.height,
                count,
                out firstIndex,
                out lastIndexExclusive);
            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                Rect rowRect = new Rect(
                    0f,
                    i * V3CargoTransferDialogStyle.RowStride,
                    viewRect.width,
                    V3CargoTransferDialogStyle.RowHeight);
                if (this.targetRowDrawer.Draw(rowRect, this.selectionModel, i))
                {
                    this.selectionModel.SelectDestination(i);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawEmptyTarget(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = V3CargoTransferDialogStyle.YellowColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 10f, rect.width - 16f, 36f),
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing"),
                    rect.width - 16f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawCountControls(Rect rect)
        {
            V3CargoTransferDialogStyle.DrawCard(rect);
            Rect inner = rect.ContractedBy(8f);
            int max = this.selectionModel.MaxCount();
            int count = this.selectionModel.Count;
            bool showSlider = max > 1;
            Rect inputRect;

            if (showSlider)
            {
                Rect sliderRect = new Rect(inner.x, inner.y + 2f, inner.width, 22f);
                float selected = Widgets.HorizontalSlider(sliderRect, count, 1f, max);
                this.SetCount(Mathf.Clamp(Mathf.RoundToInt(selected), 1, max));
                inputRect = new Rect(inner.x, sliderRect.yMax + 6f, inner.width, 30f);
            }
            else
            {
                inputRect = new Rect(
                    inner.x,
                    inner.y + Mathf.Max(0f, (inner.height - 30f) * 0.5f),
                    inner.width,
                    30f);
            }

            float labelWidth = 76f;
            float allWidth = 70f;
            Rect labelRect = new Rect(inputRect.x, inputRect.y + 6f, labelWidth, 18f);
            Rect allRect = new Rect(inputRect.xMax - allWidth, inputRect.y, allWidth, inputRect.height);
            Rect fieldRect = new Rect(
                labelRect.xMax + 6f,
                inputRect.y,
                allRect.x - labelRect.xMax - 12f,
                inputRect.height);

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                labelRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    V3CargoTransferText.Tr("CT_Shuttle_Cargo_SetCount"),
                    labelRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            GUI.SetNextControlName(V3CargoTransferDialogStyle.CountControlName);
            this.countBuffer = this.NormalizeCountBuffer(Widgets.TextField(
                fieldRect,
                this.countBuffer ?? string.Empty));
            TooltipHandler.TipRegion(fieldRect, V3CargoTransferText.GetCountPreviewText(this.selectionModel));
            if (!this.focusedInput)
            {
                GUI.FocusControl(V3CargoTransferDialogStyle.CountControlName);
                this.focusedInput = true;
            }

            if (V3CargoTransferDialogStyle.DrawButton(
                allRect,
                V3CargoTransferText.Tr("CT_Shuttle_Cargo_Action_All"),
                max > 0,
                V3CargoTransferDialogStyle.AccentColor,
                V3CargoTransferText.GetStackMassText(this.selectionModel.Stack)))
            {
                this.SetCount(max);
                GUI.FocusControl(V3CargoTransferDialogStyle.CountControlName);
            }
        }

        private float GetCountControlsHeight()
        {
            return this.selectionModel.MaxCount() > 1
                ? V3CargoTransferDialogStyle.CountHeight
                : V3CargoTransferDialogStyle.CompactCountHeight;
        }

        private void DrawFooter(Rect rect)
        {
            Rect inner = new Rect(
                rect.x + FooterPadding,
                rect.y,
                Mathf.Max(0f, rect.width - (FooterPadding * 2f)),
                rect.height);
            Rect confirmRect = new Rect(
                inner.xMax - ButtonWidth,
                inner.y + 6f,
                ButtonWidth,
                V3CargoTransferDialogStyle.ButtonHeight);
            Rect cancelRect = new Rect(
                confirmRect.x - V3CargoTransferDialogStyle.Gap - ButtonWidth,
                confirmRect.y,
                ButtonWidth,
                confirmRect.height);
            ShuttleCargoTransferActionTarget transferTarget =
                this.selectionModel.CreateActionTarget();
            string blocker = this.selectionModel.GetSelectedBlocker();
            bool canTransfer = string.IsNullOrEmpty(blocker) &&
                this.actions != null &&
                this.actions.CanTransfer(transferTarget);
            string tooltip = string.IsNullOrEmpty(blocker)
                ? this.GetActionTooltip(transferTarget)
                : blocker;

            if (V3CargoTransferDialogStyle.DrawButton(
                cancelRect,
                V3CargoTransferText.Tr("CT_Shuttle_UI_Cancel"),
                true,
                ShuttleUIStyle.BorderColor,
                null))
            {
                this.Close(false);
            }

            if (V3CargoTransferDialogStyle.DrawButton(
                confirmRect,
                V3CargoTransferText.GetTitle(this.selectionModel),
                canTransfer,
                this.selectionModel.IsToLoadedCargo()
                    ? V3CargoTransferDialogStyle.YellowColor
                    : V3CargoTransferDialogStyle.ColdColor,
                tooltip))
            {
                this.TryTransfer(transferTarget);
            }
        }

        private void TryTransfer(ShuttleCargoTransferActionTarget transferTarget)
        {
            string blocker = this.selectionModel.GetSelectedBlocker();
            if (!string.IsNullOrEmpty(blocker))
            {
                ShuttleUICommandFeedback.ShowReject(blocker, false);
                return;
            }

            if (this.actions == null ||
                transferTarget == null ||
                !this.actions.CanTransfer(transferTarget))
            {
                ShuttleUICommandFeedback.ShowReject(
                    this.GetActionTooltip(transferTarget),
                    false);
                return;
            }

            if (this.actions.Transfer(
                    transferTarget,
                    this.onSuccess))
            {
                this.Close(false);
            }
        }

        private string GetActionTooltip(ShuttleCargoTransferActionTarget transferTarget)
        {
            return this.actions != null
                ? this.actions.GetTransferTooltip(transferTarget)
                : V3CargoTransferText.Tr("CT_Shuttle_Command_ExecutorUnavailable");
        }

        private void HandleSubmitHotkey()
        {
            Event evt = Event.current;
            if (evt == null ||
                evt.type != EventType.KeyDown ||
                evt.keyCode != KeyCode.Return ||
                GUI.GetNameOfFocusedControl() != V3CargoTransferDialogStyle.CountControlName)
            {
                return;
            }

            this.TryTransfer(this.selectionModel.CreateActionTarget());
            evt.Use();
        }

        private void SyncCountFromBuffer()
        {
            int parsed;
            if (int.TryParse(this.countBuffer ?? string.Empty, out parsed))
            {
                this.selectionModel.SetCount(parsed);
            }
        }

        private void SetCount(int count)
        {
            this.selectionModel.SetCount(count);
            this.countBuffer = this.selectionModel.Count > 0
                ? this.selectionModel.Count.ToString()
                : string.Empty;
        }

        private string NormalizeCountBuffer(string value)
        {
            string digits = this.FilterDigits(value);
            if (string.IsNullOrEmpty(digits))
            {
                return string.Empty;
            }

            int parsed;
            if (!int.TryParse(digits, out parsed))
            {
                parsed = this.selectionModel.MaxCount();
            }

            this.selectionModel.SetCount(parsed);
            return this.selectionModel.Count > 0
                ? this.selectionModel.Count.ToString()
                : string.Empty;
        }

        private string FilterDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c >= '0' && c <= '9')
                {
                    buffer[count] = c;
                    count++;
                }
            }

            return count == 0 ? string.Empty : new string(buffer, 0, count);
        }

        private void GetVisibleRowRange(
            float viewportHeight,
            int rowCount,
            out int firstIndex,
            out int lastIndexExclusive)
        {
            if (rowCount <= 0)
            {
                firstIndex = 0;
                lastIndexExclusive = 0;
                return;
            }

            firstIndex = Mathf.Clamp(
                Mathf.FloorToInt(this.targetScroll.y / V3CargoTransferDialogStyle.RowStride),
                0,
                rowCount - 1);
            int visibleRows = Mathf.CeilToInt(
                viewportHeight / V3CargoTransferDialogStyle.RowStride) + 2;
            lastIndexExclusive = Mathf.Clamp(firstIndex + visibleRows, firstIndex, rowCount);
        }
    }
}

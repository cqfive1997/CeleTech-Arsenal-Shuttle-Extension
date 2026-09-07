using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoBayContentsV3 : Window
    {
        private const float Gap = 10f;
        private const float HeaderHeight = 76f;
        private const float ActionBarHeight = 58f;
        private const float ButtonHeight = 32f;
        private const float ButtonGap = 8f;

        private static readonly Color CardColor =
            new Color(0.060f, 0.075f, 0.092f, 0.94f);
        private static readonly Color AccentColor =
            new Color(0.24f, 0.62f, 0.78f, 0.84f);
        private static readonly Color YellowColor =
            new Color(0.95f, 0.72f, 0.28f, 1f);
        private static readonly Color RedColor =
            new Color(0.90f, 0.32f, 0.26f, 1f);

        private readonly ShuttleCargoBayActionTarget bay;
        private readonly ShuttleCargoPageActionContext pageContext;
        private readonly IShuttleCargoStackTransferUIActions stackActions;
        private readonly IShuttleCargoStackUnloadUIActions stackUnloadActions;
        private readonly IShuttleCargoBayUnloadUIActions bayUnloadActions;
        private readonly V3CargoBayContentsRowDrawer rowDrawer =
            new V3CargoBayContentsRowDrawer();
        private Vector2 scrollPosition;
        private int selectedIndex = -1;

        internal Dialog_ShuttleCargoBayContentsV3(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoPageActionContext pageContext,
            IShuttleCargoStackTransferUIActions stackActions,
            IShuttleCargoStackUnloadUIActions stackUnloadActions,
            IShuttleCargoBayUnloadUIActions bayUnloadActions)
        {
            this.bay = bay;
            this.pageContext = pageContext;
            this.stackActions = stackActions;
            this.stackUnloadActions = stackUnloadActions;
            this.bayUnloadActions = bayUnloadActions;
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(720f, 560f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.ClampSelectedIndex();
            ShuttleUILayout.DrawPanelBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight);
            Rect actionRect = new Rect(inRect.x, inRect.yMax - ActionBarHeight, inRect.width, ActionBarHeight);
            Rect listRect = new Rect(
                inRect.x,
                headerRect.yMax + Gap,
                inRect.width,
                Mathf.Max(0f, actionRect.y - headerRect.yMax - Gap));

            this.DrawHeader(headerRect);
            this.DrawItemList(listRect);
            this.DrawActionBar(actionRect);
        }

        private void DrawHeader(Rect rect)
        {
            float contentX = rect.x + 4f;
            Rect titleRect = new Rect(contentX, rect.y + 2f, rect.width - 8f, 26f);
            Rect statusRect = new Rect(contentX, titleRect.yMax + 3f, rect.width - 8f, 18f);
            Rect filterRect = new Rect(contentX, statusRect.yMax + 2f, rect.width - 8f, 18f);

            Text.Font = GameFont.Medium;
            GUI.color = AccentColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                this.FitLabelText(this.GetBayLabel(), titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                statusRect,
                this.FitLabelText(this.GetHeaderStatusText(), statusRect.width));
            ShuttleUILayout.SafeLabel(
                filterRect,
                this.FitLabelText(this.GetHeaderFilterText(), filterRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawItemList(Rect rect)
        {
            List<ShuttleCargoStackActionTarget> items = this.GetItems();
            int count = items != null ? items.Count : 0;
            if (count == 0)
            {
                this.DrawEmpty(rect);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                rect.width - 16f,
                Mathf.Max(rect.height, count * V3CargoBayContentsRowDrawer.RowStride));
            Widgets.BeginScrollView(rect, ref this.scrollPosition, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            GetVisibleRowRange(
                this.scrollPosition,
                rect.height,
                count,
                V3CargoBayContentsRowDrawer.RowStride,
                out firstIndex,
                out lastIndexExclusive);
            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                Rect rowRect = new Rect(
                    0f,
                    i * V3CargoBayContentsRowDrawer.RowStride,
                    viewRect.width,
                    V3CargoBayContentsRowDrawer.RowHeight);
                if (this.rowDrawer.Draw(rowRect, items[i], i == this.selectedIndex))
                {
                    this.selectedIndex = i;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawActionBar(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, CardColor);
            float buttonY = rect.y + ((rect.height - ButtonHeight) * 0.5f);
            float closeWidth = 84f;
            float bayUnloadWidth = 112f;
            float transferWidth = 54f;
            float qtyWidth = 104f;
            float allWidth = 104f;
            Rect closeRect = new Rect(rect.xMax - 10f - closeWidth, buttonY, closeWidth, ButtonHeight);
            Rect bayUnloadRect = new Rect(closeRect.x - ButtonGap - bayUnloadWidth, buttonY, bayUnloadWidth, ButtonHeight);
            Rect transferRect = new Rect(bayUnloadRect.x - ButtonGap - transferWidth, buttonY, transferWidth, ButtonHeight);
            Rect qtyRect = new Rect(transferRect.x - ButtonGap - qtyWidth, buttonY, qtyWidth, ButtonHeight);
            Rect allRect = new Rect(qtyRect.x - ButtonGap - allWidth, buttonY, allWidth, ButtonHeight);

            ShuttleCargoStackActionTarget selected = this.GetSelectedStack();
            if (selected == null)
            {
                this.DrawActionBarEmpty(rect, bayUnloadRect.x);
                this.DrawUnloadBayButton(bayUnloadRect);
                this.DrawCloseButton(closeRect);
                return;
            }

            Rect labelRect = new Rect(
                rect.x + 10f,
                rect.y + 7f,
                Mathf.Max(32f, allRect.x - rect.x - 20f),
                20f);
            Rect countRect = new Rect(labelRect.x, rect.y + 30f, labelRect.width, 16f);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                labelRect,
                this.FitLabelText(this.GetStackLabel(selected), labelRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                countRect,
                this.FitLabelText(this.GetStackMassText(selected), countRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            this.DrawUnloadAllButton(allRect, selected);
            this.DrawQuantityButton(qtyRect, selected);
            this.DrawTransferButton(transferRect, selected);
            this.DrawUnloadBayButton(bayUnloadRect);
            this.DrawCloseButton(closeRect);
        }

        private void DrawActionBarEmpty(Rect rect, float reservedX)
        {
            Rect labelRect = new Rect(
                rect.x + 10f,
                rect.y + 19f,
                Mathf.Max(32f, reservedX - rect.x - 20f),
                18f);

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                labelRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_SelectStackPrompt"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawCloseButton(Rect rect)
        {
            if (this.DrawButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleUIStyle.BorderColor,
                null))
            {
                this.Close(false);
            }
        }

        private void DrawUnloadAllButton(Rect rect, ShuttleCargoStackActionTarget stack)
        {
            bool canUnload = this.stackUnloadActions != null &&
                this.stackUnloadActions.CanUnloadStack(stack);
            if (this.DrawButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Action_UnloadAll"),
                canUnload,
                RedColor,
                this.GetUnloadTooltip(stack)))
            {
                if (this.stackUnloadActions != null &&
                    this.stackUnloadActions.UnloadStack(stack))
                {
                    this.Close(false);
                }
            }
        }

        private void DrawUnloadBayButton(Rect rect)
        {
            bool canUnload = this.bayUnloadActions != null &&
                this.bayUnloadActions.CanUnloadBay(this.bay);
            string tooltip = this.bayUnloadActions != null
                ? this.bayUnloadActions.GetUnloadBayTooltip(this.bay)
                : ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable");
            if (this.DrawButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Action_UnloadBay"),
                canUnload,
                RedColor,
                tooltip))
            {
                if (this.bayUnloadActions != null)
                {
                    this.bayUnloadActions.ConfirmUnloadBay(
                        this.bay,
                        delegate
                        {
                            this.Close(false);
                        });
                }
            }
        }

        private void DrawQuantityButton(Rect rect, ShuttleCargoStackActionTarget stack)
        {
            bool canUnload = this.stackUnloadActions != null &&
                this.stackUnloadActions.CanUnloadStack(stack, 1);
            string tooltip = canUnload && this.stackUnloadActions != null
                ? this.stackUnloadActions.GetQuantityEjectTooltip()
                : this.GetUnloadTooltip(stack);
            if (this.DrawButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Action_EjectByCount"),
                canUnload,
                YellowColor,
                tooltip))
            {
                if (this.stackUnloadActions != null)
                {
                    this.stackUnloadActions.OpenQuantityUnload(stack);
                }
            }
        }

        private void DrawTransferButton(Rect rect, ShuttleCargoStackActionTarget stack)
        {
            bool canTransfer = this.stackActions != null &&
                this.stackActions.CanTransfer(stack, this.pageContext);
            string label = stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_Action_NormalShort")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_Action_ColdShort");
            if (this.DrawButton(
                rect,
                label,
                canTransfer,
                stack != null &&
                    stack.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo
                        ? YellowColor
                        : AccentColor,
                this.GetTransferTooltip(stack)))
            {
                if (this.stackActions != null)
                {
                    this.stackActions.Transfer(
                        stack,
                        this.pageContext,
                        delegate
                        {
                            this.Close(false);
                        });
                }
            }
        }

        private bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            bool clicked;
            ShuttleV3DialogLayout.DrawAccentButton(
                rect,
                label,
                enabled,
                accent,
                tooltip,
                out clicked);
            if (!clicked)
            {
                return false;
            }

            if (!enabled)
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable")
                        : tooltip,
                    false);
                return false;
            }

            return true;
        }

        private void DrawEmpty(Rect rect)
        {
            Rect emptyRect = new Rect(rect.x, rect.y, rect.width, 42f);
            ShuttleV3DialogLayout.DrawCardBackground(emptyRect, false, false, CardColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(emptyRect.x + 8f, emptyRect.y + 12f, emptyRect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Cargo_NoItems"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private string GetHeaderStatusText()
        {
            string status = this.bay != null && !string.IsNullOrEmpty(this.bay.StatusText)
                ? this.bay.StatusText
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown");
            string mass = ShuttleUIMetricFormatter.FormatKgPair(
                this.bay != null ? Mathf.Max(0f, this.bay.UsedMassKg) : 0f,
                this.bay != null ? Mathf.Max(0f, this.bay.CapacityKg) : 0f);
            string stackCount = ShuttleUIText.Tr(
                "CT_Shuttle_Cargo_StackCountFormat",
                this.GetItemsCount());
            return status + " / " + stackCount + " / " + mass;
        }

        private string GetHeaderFilterText()
        {
            if (this.bay != null && !string.IsNullOrEmpty(this.bay.FilterSummary))
            {
                return this.bay.FilterSummary;
            }

            return ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterUnavailable");
        }

        private string GetBayLabel()
        {
            return this.bay != null && !string.IsNullOrEmpty(this.bay.Label)
                ? this.bay.Label
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Bay");
        }

        private List<ShuttleCargoStackActionTarget> GetItems()
        {
            return this.bay != null ? this.bay.Items : null;
        }

        private int GetItemsCount()
        {
            List<ShuttleCargoStackActionTarget> items = this.GetItems();
            return items != null ? items.Count : 0;
        }

        private ShuttleCargoStackActionTarget GetSelectedStack()
        {
            List<ShuttleCargoStackActionTarget> items = this.GetItems();
            if (items == null || this.selectedIndex < 0 || this.selectedIndex >= items.Count)
            {
                return null;
            }

            return items[this.selectedIndex];
        }

        private void ClampSelectedIndex()
        {
            List<ShuttleCargoStackActionTarget> items = this.GetItems();
            int count = items != null ? items.Count : 0;
            if (count <= 0)
            {
                this.selectedIndex = -1;
                return;
            }

            if (this.selectedIndex < 0)
            {
                this.selectedIndex = 0;
                return;
            }

            if (this.selectedIndex >= count)
            {
                this.selectedIndex = count - 1;
            }
        }

        private string GetStackLabel(ShuttleCargoStackActionTarget stack)
        {
            return stack != null && !string.IsNullOrEmpty(stack.Label)
                ? stack.Label
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
        }

        private string GetStackMassText(ShuttleCargoStackActionTarget stack)
        {
            int count = stack != null ? stack.StackCount : 0;
            string massText = stack != null
                ? Mathf.Max(0f, stack.MassKg).ToString("0.#")
                : "0";
            return ShuttleUIText.Tr("CT_Shuttle_Cargo_StackMassFormat", count, massText);
        }

        private string GetUnloadTooltip(ShuttleCargoStackActionTarget stack)
        {
            return this.stackUnloadActions != null
                ? this.stackUnloadActions.GetUnloadTooltip(stack)
                : ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable");
        }

        private string GetTransferTooltip(ShuttleCargoStackActionTarget stack)
        {
            return this.stackActions != null
                ? this.stackActions.GetTransferTooltip(stack, this.pageContext)
                : ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable");
        }

        private static void GetVisibleRowRange(
            Vector2 scroll,
            float viewportHeight,
            int rowCount,
            float rowStride,
            out int firstIndex,
            out int lastIndexExclusive)
        {
            if (rowCount <= 0 || rowStride <= 0f)
            {
                firstIndex = 0;
                lastIndexExclusive = 0;
                return;
            }

            firstIndex = Mathf.Clamp(Mathf.FloorToInt(scroll.y / rowStride), 0, rowCount - 1);
            int visibleRows = Mathf.CeilToInt(viewportHeight / rowStride) + 2;
            lastIndexExclusive = Mathf.Clamp(firstIndex + visibleRows, firstIndex, rowCount);
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(
                text,
                width,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown"),
                8f);
        }
    }
}

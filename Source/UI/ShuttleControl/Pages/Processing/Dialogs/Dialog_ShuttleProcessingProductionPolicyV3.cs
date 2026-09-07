using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing.Dialogs
{
    internal sealed class Dialog_ShuttleProcessingProductionPolicyV3 : Window
    {
        private const float HeaderHeight = 54f;
        private const float FooterHeight = 40f;
        private const float Gap = 10f;

        private readonly IShuttleProcessingProductionPolicyActions processingActions;
        private readonly ShuttleProcessingOrderActionTarget order;

        private AutoWorkTableProductionMode originalMode;
        private int originalRepeatCount;
        private int originalTargetCount;
        private AutoWorkTableProductionMode workingMode;
        private int workingRepeatCount;
        private int workingTargetCount;
        private string repeatCountBuffer;
        private string targetCountBuffer;
        private bool applyingOnClose;

        internal Dialog_ShuttleProcessingProductionPolicyV3(
            IShuttleProcessingProductionPolicyActions processingActions,
            ShuttleProcessingOrderActionTarget order)
        {
            this.processingActions = processingActions;
            this.order = order;
            this.originalMode = ParseMode(order != null ? order.ProductionModeKey : null);
            this.originalRepeatCount = Mathf.Max(
                1,
                order != null && order.RequestedCount > 0
                    ? order.RequestedCount
                    : 1);
            this.originalTargetCount = Mathf.Max(
                1,
                order != null && order.TargetCount > 0
                    ? order.TargetCount
                    : 1);
            this.ResetWorkingCopy();
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(640f, 600f);
            }
        }

        public override void PreClose()
        {
            if (!this.applyingOnClose && this.HasUnsavedChanges())
            {
                this.applyingOnClose = true;
                this.TrySave(true);
                this.applyingOnClose = false;
            }

            base.PreClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 10f, inRect.width - 24f, HeaderHeight);
            Rect footerRect = new Rect(inRect.x + 12f, inRect.yMax - FooterHeight - 8f, inRect.width - 24f, FooterHeight);
            Rect bodyRect = new Rect(
                inRect.x + 12f,
                headerRect.yMax + Gap,
                inRect.width - 24f,
                footerRect.y - headerRect.yMax - (Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawFooter(footerRect);
        }

        private void DrawHeader(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleV3DialogStyle.HeaderColor);
            ShuttleV3DialogLayout.DrawRectBorder(rect, ShuttleV3DialogStyle.BorderColor, 2f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.86f, 0.94f, 1f, 1f);
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 26f),
                this.Tr("CT_Shuttle_AutoWorkTable_ConfigPolicy"));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 33f, rect.width - 24f, 18f),
                this.Tr("CT_Shuttle_AutoWorkTable_CloseSavesPolicy"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect)
        {
            Rect infoRect = new Rect(rect.x, rect.y, rect.width, 96f);
            Rect modeRect = new Rect(rect.x, infoRect.yMax + Gap, rect.width, 160f);
            Rect countsRect = new Rect(rect.x, modeRect.yMax + Gap, rect.width, rect.yMax - modeRect.yMax - Gap);

            this.DrawInfo(infoRect);
            this.DrawModeOptions(modeRect);
            this.DrawCounts(countsRect);
        }

        private void DrawInfo(Rect rect)
        {
            this.DrawSection(rect, this.Tr("CT_Shuttle_AutoWorkTable_CurrentRecipe"));
            Rect inner = this.Inner(rect);
            float y = inner.y;
            this.DrawInfoRow(ref y, inner, this.Tr("CT_Shuttle_AutoWorkTable_CurrentRecipe"), this.ValueOrDash(this.order != null ? this.order.Label : null));
            this.DrawInfoRow(ref y, inner, this.Tr("CT_Shuttle_AutoWorkTable_PolicyStatus"), this.ValueOrDash(this.order != null ? this.order.ProductionModeKey : null));
        }

        private void DrawModeOptions(Rect rect)
        {
            this.DrawSection(rect, this.Tr("CT_Shuttle_AutoWorkTable_ProductionMode"));
            Rect inner = this.Inner(rect);
            float rowHeight = 28f;
            this.DrawModeButton(new Rect(inner.x, inner.y, inner.width, rowHeight), AutoWorkTableProductionMode.OneShot);
            this.DrawModeButton(new Rect(inner.x, inner.y + rowHeight + 5f, inner.width, rowHeight), AutoWorkTableProductionMode.RepeatCount);
            this.DrawModeButton(new Rect(inner.x, inner.y + ((rowHeight + 5f) * 2f), inner.width, rowHeight), AutoWorkTableProductionMode.RepeatForever);
            this.DrawModeButton(new Rect(inner.x, inner.y + ((rowHeight + 5f) * 3f), inner.width, rowHeight), AutoWorkTableProductionMode.DoUntilStock);
        }

        private void DrawModeButton(Rect rect, AutoWorkTableProductionMode mode)
        {
            bool selected = this.workingMode == mode;
            bool supported = this.IsModeSupported(mode);
            Color color = !supported
                ? ShuttleV3DialogStyle.MutedTextColor
                : selected ? ShuttleV3DialogStyle.BlueStatusColor : ShuttleV3DialogStyle.MutedTextColor;
            if (ShuttleV3DialogLayout.DrawAccentActionRowButton(
                rect,
                this.LabelForMode(mode),
                supported,
                color,
                !supported ? this.GetModeUnsupportedTooltip(mode) : null))
            {
                this.workingMode = mode;
            }
        }

        private void DrawCounts(Rect rect)
        {
            this.DrawSection(rect, this.Tr("CT_Shuttle_AutoWorkTable_CountSettings"));
            Rect inner = this.Inner(rect);
            float y = inner.y;
            this.DrawNumberField(
                ref y,
                inner,
                this.Tr("CT_Shuttle_AutoWorkTable_RepeatCount"),
                ref this.repeatCountBuffer,
                this.workingMode == AutoWorkTableProductionMode.RepeatCount,
                true);
            this.DrawNumberField(
                ref y,
                inner,
                this.Tr("CT_Shuttle_AutoWorkTable_TargetCount"),
                ref this.targetCountBuffer,
                this.workingMode == AutoWorkTableProductionMode.DoUntilStock,
                false);

            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            Text.Font = GameFont.Tiny;
            string note = this.Tr("CT_Shuttle_AutoWorkTable_PrimaryProductOnly");
            if (!this.WorkbenchSupportsDoUntilStock())
            {
                note += "\n" + this.GetDoUntilStockUnsupportedReason() +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_DynamicProductsUseRepeatForever");
            }

            ShuttleV3DialogLayout.SafeLabel(
                new Rect(inner.x, y + 4f, inner.width, inner.yMax - y - 4f),
                note);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawFooter(Rect rect)
        {
            float buttonWidth = 112f;
            Rect closeRect = new Rect(rect.xMax - buttonWidth, rect.y + 5f, buttonWidth, 30f);
            Rect saveRect = new Rect(closeRect.x - buttonWidth - 8f, rect.y + 5f, buttonWidth, 30f);
            Rect resetRect = new Rect(saveRect.x - buttonWidth - 8f, rect.y + 5f, buttonWidth, 30f);

            bool canSave = this.CanSave();
            if (ShuttleV3DialogLayout.DrawDialogButton(
                resetRect,
                this.Tr("CT_Shuttle_AutoWorkTable_ResetPolicy"),
                this.HasUnsavedChanges(),
                ShuttleV3DialogButtonKind.Danger,
                null))
            {
                this.ResetWorkingCopy();
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                saveRect,
                this.Tr("CT_Shuttle_AutoWorkTable_SavePolicy"),
                canSave,
                ShuttleV3DialogButtonKind.Primary,
                !canSave ? this.GetSaveDisabledTooltip() : null))
            {
                this.TrySave(true);
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                closeRect,
                this.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.Close();
            }
        }

        private void DrawSection(Rect rect, string title)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false);
            ShuttleV3DialogLayout.DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, 30f), title);
        }

        private Rect Inner(Rect rect)
        {
            return new Rect(rect.x + 10f, rect.y + 36f, rect.width - 20f, rect.height - 44f);
        }

        private void DrawInfoRow(ref float y, Rect rect, string label, string value)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x, y, 140f, 18f), label);
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 145f, y, rect.width - 145f, 18f), this.FitLabelText(value, rect.width - 145f));
            Text.Font = GameFont.Small;
            y += 20f;
        }

        private void DrawNumberField(
            ref float y,
            Rect rect,
            string label,
            ref string buffer,
            bool enabled,
            bool isRepeatCount)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = enabled ? Color.white : ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x, y + 4f, 150f, 20f), label);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = enabled;
            string next = Widgets.TextField(new Rect(rect.x + 156f, y, 96f, 26f), buffer ?? string.Empty);
            GUI.enabled = oldEnabled;
            if (enabled)
            {
                buffer = next;
                int parsed;
                if (int.TryParse(buffer, out parsed))
                {
                    if (isRepeatCount)
                    {
                        this.workingRepeatCount = Mathf.Max(1, parsed);
                    }
                    else
                    {
                        this.workingTargetCount = Mathf.Max(1, parsed);
                    }
                }
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 34f;
        }

        private bool CanSave()
        {
            if (this.processingActions == null ||
                this.order == null ||
                !this.HasUnsavedChanges())
            {
                return false;
            }

            int repeat;
            int target;
            if (!this.TryParseCounts(out repeat, out target))
            {
                return false;
            }

            return this.processingActions.CanSaveProductionPolicy(
                this.order,
                this.workingMode,
                repeat,
                target);
        }

        private bool TrySave(bool showFailure)
        {
            int repeat;
            int target;
            if (!this.TryParseCounts(out repeat, out target))
            {
                if (showFailure)
                {
                    Messages.Message(this.Tr("CT_Shuttle_AutoWorkTable_InvalidPolicy"), MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            if (this.processingActions == null ||
                !this.processingActions.SaveProductionPolicy(this.order, this.workingMode, repeat, target))
            {
                return false;
            }

            this.originalMode = this.workingMode;
            this.originalRepeatCount = repeat;
            this.originalTargetCount = target;
            this.ResetWorkingCopy();
            return true;
        }

        private bool TryParseCounts(out int repeat, out int target)
        {
            repeat = Mathf.Max(1, this.workingRepeatCount);
            target = Mathf.Max(1, this.workingTargetCount);

            if (this.workingMode == AutoWorkTableProductionMode.RepeatCount)
            {
                if (!int.TryParse(this.repeatCountBuffer, out repeat))
                {
                    return false;
                }
            }

            if (this.workingMode == AutoWorkTableProductionMode.DoUntilStock)
            {
                if (!int.TryParse(this.targetCountBuffer, out target))
                {
                    return false;
                }
            }

            repeat = Mathf.Max(1, repeat);
            target = Mathf.Max(1, target);
            return repeat >= 1 && target >= 1;
        }

        private bool HasUnsavedChanges()
        {
            int repeat;
            int target;
            if (!this.TryParseCounts(out repeat, out target))
            {
                return true;
            }

            return this.workingMode != this.originalMode ||
                repeat != this.originalRepeatCount ||
                target != this.originalTargetCount;
        }

        private string GetSaveDisabledTooltip()
        {
            if (this.processingActions == null || this.order == null)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_InvalidPolicy");
            }

            int repeat;
            int target;
            if (!this.TryParseCounts(out repeat, out target))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_InvalidPolicy");
            }

            if (!this.HasUnsavedChanges())
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_NoUnsavedChanges");
            }

            return this.processingActions.GetProductionPolicyTooltip(this.order);
        }

        private bool IsModeSupported(AutoWorkTableProductionMode mode)
        {
            return mode != AutoWorkTableProductionMode.DoUntilStock ||
                this.WorkbenchSupportsDoUntilStock();
        }

        private bool WorkbenchSupportsDoUntilStock()
        {
            return this.order == null || this.order.SupportsDoUntilStock;
        }

        private string GetModeUnsupportedTooltip(AutoWorkTableProductionMode mode)
        {
            if (mode == AutoWorkTableProductionMode.DoUntilStock)
            {
                return this.GetDoUntilStockUnsupportedReason() + "\n" +
                    this.Tr("CT_Shuttle_AutoWorkTable_DynamicProductsUseRepeatForever");
            }

            return this.Tr("CT_Shuttle_AutoWorkTable_InvalidPolicy");
        }

        private string GetDoUntilStockUnsupportedReason()
        {
            if (this.processingActions != null)
            {
                return this.processingActions.GetDoUntilStockUnsupportedReason(this.order);
            }

            return this.Tr("CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts");
        }

        private void ResetWorkingCopy()
        {
            this.workingMode = this.originalMode;
            this.workingRepeatCount = Mathf.Max(1, this.originalRepeatCount);
            this.workingTargetCount = Mathf.Max(1, this.originalTargetCount);
            this.repeatCountBuffer = this.workingRepeatCount.ToString();
            this.targetCountBuffer = this.workingTargetCount.ToString();
        }

        private string LabelForMode(AutoWorkTableProductionMode mode)
        {
            return V3ProcessingDisplayTextResolver.ResolveProductionModeLabel(
                mode.ToString());
        }

        private static AutoWorkTableProductionMode ParseMode(string key)
        {
            if (key == "RepeatCount")
            {
                return AutoWorkTableProductionMode.RepeatCount;
            }

            if (key == "RepeatForever")
            {
                return AutoWorkTableProductionMode.RepeatForever;
            }

            if (key == "DoUntilStock")
            {
                return AutoWorkTableProductionMode.DoUntilStock;
            }

            return AutoWorkTableProductionMode.OneShot;
        }

        private string Tr(string key)
        {
            return V3ProcessingDisplayTextResolver.Tr(key);
        }

        private string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoQuantityUnloadV3 : Window
    {
        private const float Gap = 10f;
        private const float ButtonWidth = 96f;
        private const float ButtonHeight = 30f;
        private const string CountControlName = "CT_Shuttle_UnloadQuantityV3_Count";

        private static readonly Color CardColor =
            new Color(0.060f, 0.075f, 0.092f, 0.94f);
        private static readonly Color AccentColor =
            new Color(0.95f, 0.72f, 0.28f, 1f);

        private readonly ShuttleCargoStackActionTarget target;
        private readonly IShuttleCargoStackUnloadUIActions actions;
        private string countBuffer;
        private bool focusedInput;

        internal Dialog_ShuttleCargoQuantityUnloadV3(
            ShuttleCargoStackActionTarget target,
            IShuttleCargoStackUnloadUIActions actions)
        {
            this.target = target;
            this.actions = actions;
            this.countBuffer = this.GetInitialCountBuffer();
            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(430f, 260f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleUILayout.DrawPanelBackground(inRect);

            Rect titleRect = new Rect(inRect.x + 10f, inRect.y + 8f, inRect.width - 20f, 28f);
            Rect summaryRect = new Rect(inRect.x + 10f, titleRect.yMax + 4f, inRect.width - 20f, 44f);
            Rect sliderRect = new Rect(inRect.x + 10f, summaryRect.yMax + Gap, inRect.width - 20f, 30f);
            Rect inputRect = new Rect(inRect.x + 10f, sliderRect.yMax + Gap, inRect.width - 20f, 30f);
            Rect buttonRect = new Rect(inRect.x + 10f, inRect.yMax - ButtonHeight - 8f, inRect.width - 20f, ButtonHeight);

            this.DrawTitle(titleRect);
            this.DrawSummary(summaryRect);
            this.DrawSlider(sliderRect);
            this.DrawInput(inputRect);
            this.DrawButtons(buttonRect);
            this.HandleSubmitHotkey();
        }

        private void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = AccentColor;
            ShuttleUILayout.SafeLabel(
                rect,
                this.FitLabelText(this.Tr("CT_Shuttle_Cargo_UnloadQuantityTitle"), rect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawSummary(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, CardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            Rect labelRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f);
            Rect countRect = new Rect(rect.x + 8f, rect.y + 25f, rect.width - 16f, 16f);

            string stackLabel = this.GetStackLabel();
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                labelRect,
                this.FitLabelText(stackLabel, labelRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                countRect,
                this.FitLabelText(this.GetStackCountText(), countRect.width));
            TooltipHandler.TipRegion(rect, stackLabel + "\n" + this.GetStackCountText());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawSlider(Rect rect)
        {
            int max = this.GetMaxStackCount();
            int count = this.GetSubmittedCount();
            Rect labelRect = new Rect(rect.x, rect.y + 3f, 86f, 20f);
            Rect sliderRect = new Rect(labelRect.xMax + 8f, rect.y + 1f, rect.width - labelRect.width - 8f, 24f);

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                labelRect,
                this.FitLabelText(this.Tr("CT_Shuttle_Cargo_SetCount"), labelRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (max <= 1)
            {
                Widgets.DrawBoxSolid(sliderRect, CardColor);
                ShuttleUILayout.DrawRectBorder(
                    sliderRect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                TooltipHandler.TipRegion(sliderRect, this.GetCountPreviewText());
                return;
            }

            float selected = Widgets.HorizontalSlider(sliderRect, count, 1f, max);
            this.countBuffer = Mathf.Clamp(Mathf.RoundToInt(selected), 1, max).ToString();
            TooltipHandler.TipRegion(sliderRect, this.GetCountPreviewText());
        }

        private void DrawInput(Rect rect)
        {
            float labelWidth = 88f;
            float allWidth = 64f;
            Rect labelRect = new Rect(rect.x, rect.y + 6f, labelWidth, 18f);
            Rect allRect = new Rect(rect.xMax - allWidth, rect.y, allWidth, rect.height);
            Rect fieldRect = new Rect(labelRect.xMax + 6f, rect.y, allRect.x - labelRect.xMax - 12f, rect.height);

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                labelRect,
                this.FitLabelText(this.Tr("CT_Shuttle_Cargo_SetCount"), labelRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            GUI.SetNextControlName(CountControlName);
            string next = Widgets.TextField(fieldRect, this.countBuffer ?? string.Empty);
            this.countBuffer = this.NormalizeCountBuffer(next);
            TooltipHandler.TipRegion(fieldRect, this.GetCountPreviewText());
            if (!this.focusedInput)
            {
                GUI.FocusControl(CountControlName);
                this.focusedInput = true;
            }

            if (this.DrawButton(
                allRect,
                this.Tr("CT_Shuttle_Cargo_Action_All"),
                this.GetMaxStackCount() > 0,
                AccentColor,
                this.GetStackCountText()))
            {
                this.countBuffer = this.GetMaxStackCount().ToString();
                GUI.FocusControl(CountControlName);
            }
        }

        private void DrawButtons(Rect rect)
        {
            Rect unloadRect = new Rect(rect.xMax - ButtonWidth, rect.y, ButtonWidth, rect.height);
            Rect cancelRect = new Rect(unloadRect.x - Gap - ButtonWidth, rect.y, ButtonWidth, rect.height);
            int count = this.GetSubmittedCount();
            bool canUnload = this.CanUnloadStack(count);

            if (this.DrawButton(
                cancelRect,
                this.Tr("CT_Shuttle_UI_Cancel"),
                true,
                ShuttleUIStyle.BorderColor,
                null))
            {
                this.Close(false);
            }

            if (this.DrawButton(
                unloadRect,
                this.Tr("CT_Shuttle_Cargo_UnloadQuantityConfirm"),
                canUnload,
                AccentColor,
                this.GetUnloadTooltip()))
            {
                this.TryUnload();
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
            bool activated = ShuttleV3DialogLayout.DrawAccentButton(
                rect,
                this.FitLabelText(label, rect.width - 12f),
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
                        ? this.Tr("CT_Shuttle_Cargo_Action_UnloadQuantityUnavailable")
                        : tooltip,
                    false);
                return false;
            }

            return activated;
        }

        private void HandleSubmitHotkey()
        {
            Event evt = Event.current;
            if (evt == null ||
                evt.type != EventType.KeyDown ||
                evt.keyCode != KeyCode.Return)
            {
                return;
            }

            if (GUI.GetNameOfFocusedControl() != CountControlName)
            {
                return;
            }

            this.TryUnload();
            evt.Use();
        }

        private void TryUnload()
        {
            int count = this.GetSubmittedCount();
            if (!this.CanUnloadStack(count))
            {
                Messages.Message(
                    this.Tr("CT_Shuttle_Cargo_Action_UnloadQuantityUnavailable"),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            if (this.actions != null && this.actions.UnloadStack(this.target, count))
            {
                this.Close(false);
            }
        }

        private bool CanUnloadStack(int count)
        {
            return this.actions != null &&
                this.actions.CanUnloadStack(this.target, count);
        }

        private string GetUnloadTooltip()
        {
            return this.actions != null
                ? this.actions.GetUnloadTooltip(this.target)
                : this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
        }

        private string GetInitialCountBuffer()
        {
            int max = this.GetMaxStackCount();
            return max > 0 ? "1" : string.Empty;
        }

        private string NormalizeCountBuffer(string value)
        {
            string digits = this.FilterDigits(value);
            if (string.IsNullOrEmpty(digits))
            {
                return string.Empty;
            }

            int max = this.GetMaxStackCount();
            if (max <= 0)
            {
                return string.Empty;
            }

            int parsed;
            if (!int.TryParse(digits, out parsed))
            {
                return max.ToString();
            }

            return Mathf.Clamp(parsed, 1, max).ToString();
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

        private int GetSubmittedCount()
        {
            int max = this.GetMaxStackCount();
            if (max <= 0)
            {
                return 0;
            }

            int parsed;
            if (!int.TryParse(this.countBuffer ?? string.Empty, out parsed))
            {
                parsed = 1;
            }

            return Mathf.Clamp(parsed, 1, max);
        }

        private int GetMaxStackCount()
        {
            return this.target != null ? Mathf.Max(0, this.target.StackCount) : 0;
        }

        private string GetStackLabel()
        {
            return this.target != null && !string.IsNullOrEmpty(this.target.Label)
                ? this.target.Label
                : this.Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
        }

        private string GetStackCountText()
        {
            return this.Tr("CT_Shuttle_Cargo_StackCountFormat", this.GetMaxStackCount());
        }

        private string GetCountPreviewText()
        {
            return this.Tr(
                "CT_Shuttle_Cargo_CountFormat",
                this.GetSubmittedCount(),
                this.GetMaxStackCount());
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width, "-", 8f);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        private string Tr(string key, object arg0)
        {
            return ShuttleUIText.Tr(key, arg0);
        }

        private string Tr(string key, object arg0, object arg1)
        {
            return ShuttleUIText.Tr(key, arg0, arg1);
        }
    }
}

using System.Globalization;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal static class ShuttleOtherSettingsControlDrawer
    {
        private const float StepperGap = 6f;

        internal static bool DrawMultiplierCard(
            Rect rect,
            string labelKey,
            string tooltipKey,
            float defaultValue,
            float minimumValue,
            float maximumValue,
            float step,
            ref string valueBuffer,
            out float parsedValue)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleV3DialogStyle.SettingsInfoRowColor);
            DrawCompactLabel(rect, labelKey.Translate().ToString(), 218f);

            Rect minusRect = new Rect(rect.xMax - 184f, rect.y + 17f, 32f, 30f);
            Rect valueRect = new Rect(minusRect.xMax + StepperGap, rect.y + 17f, 96f, 30f);
            Rect plusRect = new Rect(valueRect.xMax + StepperGap, rect.y + 17f, 32f, 30f);

            float current;
            bool validBeforeEdit = TryParseMultiplier(
                valueBuffer,
                minimumValue,
                maximumValue,
                out current);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                minusRect,
                "-",
                true,
                ShuttleV3DialogButtonKind.Normal,
                "CT_Shuttle_OtherSettings_MultiplierStep".Translate().ToString()))
            {
                current = validBeforeEdit
                    ? current
                    : defaultValue;
                valueBuffer = Mathf.Max(
                    minimumValue,
                    current - step).ToString("0.0", CultureInfo.InvariantCulture);
            }

            valueBuffer = Widgets.TextField(valueRect, valueBuffer ?? string.Empty);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                plusRect,
                "+",
                true,
                ShuttleV3DialogButtonKind.Normal,
                "CT_Shuttle_OtherSettings_MultiplierStep".Translate().ToString()))
            {
                current = TryParseMultiplier(
                    valueBuffer,
                    minimumValue,
                    maximumValue,
                    out current)
                    ? current
                    : defaultValue;
                valueBuffer = Mathf.Min(
                    maximumValue,
                    current + step).ToString("0.0", CultureInfo.InvariantCulture);
            }

            bool valid = TryParseMultiplier(
                valueBuffer,
                minimumValue,
                maximumValue,
                out parsedValue);
            ShuttleV3DialogLayout.DrawRectBorder(
                valueRect,
                valid
                    ? ShuttleV3DialogStyle.SubtleBorderColor
                    : ShuttleV3DialogStyle.RedStatusColor,
                1f);
            TooltipHandler.TipRegion(
                rect,
                tooltipKey.Translate().ToString());
            return valid;
        }

        internal static void DrawToggleCard(
            Rect rect,
            string labelKey,
            string descriptionKey,
            ref bool value)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleV3DialogStyle.SettingsInfoRowColor);
            Rect checkboxRect = new Rect(rect.x + 12f, rect.y + 15f, 24f, 24f);
            Widgets.Checkbox(checkboxRect.position, ref value, 20f);
            DrawCompactLabel(
                new Rect(rect.x + 32f, rect.y, rect.width - 32f, rect.height),
                labelKey.Translate().ToString(),
                24f);
            TooltipHandler.TipRegion(rect, descriptionKey.Translate().ToString());
        }

        internal static bool DrawRatioCard(
            Rect rect,
            string labelKey,
            string tooltipKey,
            bool enabled,
            float defaultRatio,
            float minimumRatio,
            float maximumRatio,
            float stepRatio,
            ref string ratioBuffer,
            out float parsedRatio)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleV3DialogStyle.SettingsInfoRowColor);
            DrawCompactLabel(rect, labelKey.Translate().ToString(), 238f, enabled);

            Rect plusRect = new Rect(rect.xMax - 44f, rect.y + 17f, 32f, 30f);
            Rect valueRect = new Rect(
                plusRect.x - StepperGap - 96f,
                rect.y + 17f,
                96f,
                30f);
            Rect minusRect = new Rect(
                valueRect.x - StepperGap - 32f,
                rect.y + 17f,
                32f,
                30f);
            float currentRatio;
            bool validBeforeEdit = TryParseMultiplier(
                ratioBuffer,
                minimumRatio,
                maximumRatio,
                out currentRatio);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && enabled;
            try
            {
                if (ShuttleV3DialogLayout.DrawDialogButton(
                    minusRect,
                    "-",
                    enabled,
                    ShuttleV3DialogButtonKind.Normal,
                    "CT_Shuttle_OtherSettings_RefrigeratedRatioStep".Translate().ToString()))
                {
                    currentRatio = validBeforeEdit ? currentRatio : defaultRatio;
                    ratioBuffer = Mathf.Max(
                        minimumRatio,
                        currentRatio - stepRatio).ToString("0.00", CultureInfo.InvariantCulture);
                }

                ratioBuffer = Widgets.TextField(valueRect, ratioBuffer ?? string.Empty);
                if (ShuttleV3DialogLayout.DrawDialogButton(
                    plusRect,
                    "+",
                    enabled,
                    ShuttleV3DialogButtonKind.Normal,
                    "CT_Shuttle_OtherSettings_RefrigeratedRatioStep".Translate().ToString()))
                {
                    currentRatio = TryParseMultiplier(
                        ratioBuffer,
                        minimumRatio,
                        maximumRatio,
                        out currentRatio)
                        ? currentRatio
                        : defaultRatio;
                    ratioBuffer = Mathf.Min(
                        maximumRatio,
                        currentRatio + stepRatio).ToString("0.00", CultureInfo.InvariantCulture);
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }

            float parsedValue;
            bool valid = TryParseMultiplier(
                ratioBuffer,
                minimumRatio,
                maximumRatio,
                out parsedValue);
            parsedRatio = valid
                ? parsedValue
                : defaultRatio;
            ShuttleV3DialogLayout.DrawRectBorder(
                valueRect,
                valid || !enabled
                    ? ShuttleV3DialogStyle.SubtleBorderColor
                    : ShuttleV3DialogStyle.RedStatusColor,
                1f);
            TooltipHandler.TipRegion(rect, tooltipKey.Translate().ToString());
            return valid || !enabled;
        }

        private static void DrawCompactLabel(
            Rect rect,
            string label,
            float rightReserve)
        {
            DrawCompactLabel(rect, label, rightReserve, true);
        }

        private static void DrawCompactLabel(
            Rect rect,
            string label,
            float rightReserve,
            bool enabled)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = enabled ? Color.white : ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(
                        rect.x + 12f,
                        rect.y + 4f,
                        Mathf.Max(0f, rect.width - rightReserve - 12f),
                        Mathf.Max(0f, rect.height - 8f)),
                    label);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
            }
        }

        private static bool TryParseMultiplier(
            string value,
            float minimumValue,
            float maximumValue,
            out float parsed)
        {
            bool success = float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out parsed) ||
                float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsed);
            return success &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed) &&
                parsed >= minimumValue &&
                parsed <= maximumValue;
        }
    }
}

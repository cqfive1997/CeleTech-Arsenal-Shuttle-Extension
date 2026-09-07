using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialInstructionBoxDrawer
    {
        private const float InnerPadding = 14f;
        private const float TitleHeight = 30f;
        private const float TextGap = 8f;
        private const float ButtonGap = 12f;
        private const float ButtonWidth = 104f;
        private const float ButtonHeight = 28f;
        private const float MinHeight = 178f;

        internal float CalculateHeight(ShuttleTutorialStep step, Rect windowRect, float boxWidth)
        {
            float width = boxWidth - (InnerPadding * 2f);
            float bodyHeight = this.CalculateTextHeight(
                step != null ? ShuttleUIText.Tr(step.BodyKey) : string.Empty,
                width,
                GameFont.Small);
            float warningHeight = 0f;
            if (step != null && !string.IsNullOrEmpty(step.WarningKey))
            {
                warningHeight = TextGap + this.CalculateTextHeight(
                    ShuttleUIText.Tr(step.WarningKey),
                    width,
                    GameFont.Small);
            }

            float height =
                (InnerPadding * 2f) +
                TitleHeight +
                TextGap +
                bodyHeight +
                warningHeight +
                ButtonGap +
                ButtonHeight;
            float maxHeight = Mathf.Max(MinHeight, windowRect.height - 24f);
            return Mathf.Min(Mathf.Max(MinHeight, height), maxHeight);
        }

        internal ShuttleTutorialInstructionButtonRects Draw(
            Rect rect,
            ShuttleTutorialStep step,
            bool canAdvance)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.1f, 0.12f, 0.96f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.BorderColor,
                ShuttleUIStyle.ThickBorder);

            Rect inner = rect.ContractedBy(InnerPadding);
            this.DrawInstructionText(inner, step);
            return this.DrawInstructionButtons(inner, canAdvance);
        }

        private void DrawInstructionText(Rect inner, ShuttleTutorialStep step)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            bool oldWordWrap = Text.WordWrap;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;

            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, TitleHeight),
                step != null ? ShuttleUIText.Tr(step.TitleKey) : string.Empty);

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.82f, 0.88f, 0.92f, 1f);
            float y = inner.y + TitleHeight + TextGap;
            string bodyText = step != null ? ShuttleUIText.Tr(step.BodyKey) : string.Empty;
            float bodyHeight = this.CalculateTextHeight(
                bodyText,
                inner.width,
                GameFont.Small);
            Widgets.Label(new Rect(inner.x, y, inner.width, bodyHeight), bodyText);
            y += bodyHeight;

            if (step != null && !string.IsNullOrEmpty(step.WarningKey))
            {
                y += TextGap;
                GUI.color = new Color(0.95f, 0.67f, 0.24f, 1f);
                string warningText = ShuttleUIText.Tr(step.WarningKey);
                float warningHeight = this.CalculateTextHeight(
                    warningText,
                    inner.width,
                    GameFont.Small);
                Widgets.Label(new Rect(inner.x, y, inner.width, warningHeight), warningText);
            }

            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWordWrap;
            Text.Font = oldFont;
        }

        private ShuttleTutorialInstructionButtonRects DrawInstructionButtons(
            Rect inner,
            bool canAdvance)
        {
            Rect skipButtonRect =
                new Rect(inner.x, inner.yMax - ButtonHeight, ButtonWidth, ButtonHeight);
            Rect nextButtonRect =
                new Rect(inner.xMax - ButtonWidth, inner.yMax - ButtonHeight, ButtonWidth, ButtonHeight);

            this.DrawButton(
                skipButtonRect,
                ShuttleUIText.Tr("CT_Shuttle_Tutorial_SkipPage"),
                false,
                true);
            this.DrawButton(
                nextButtonRect,
                ShuttleUIText.Tr("CT_Shuttle_Tutorial_Next"),
                true,
                canAdvance);
            return new ShuttleTutorialInstructionButtonRects(
                nextButtonRect,
                skipButtonRect);
        }

        private void DrawButton(Rect rect, string label, bool primary, bool enabled)
        {
            Color fill = enabled
                ? (primary
                    ? new Color(0.18f, 0.48f, 0.78f, 1f)
                    : new Color(0.2f, 0.23f, 0.26f, 1f))
                : new Color(0.13f, 0.15f, 0.17f, 1f);
            if (enabled && Mouse.IsOver(rect))
            {
                fill = primary
                    ? new Color(0.24f, 0.58f, 0.9f, 1f)
                    : new Color(0.27f, 0.31f, 0.35f, 1f);
            }

            Widgets.DrawBoxSolid(rect, fill);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.BorderColor,
                ShuttleUIStyle.ThinBorder);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = enabled ? Color.white : ShuttleUIStyle.MutedTextColor;
            Widgets.Label(rect, label ?? string.Empty);
            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }

        private float CalculateTextHeight(string text, float width, GameFont font)
        {
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;
            Text.Font = font;
            Text.WordWrap = true;
            float height = Mathf.Ceil(Text.CalcHeight(text ?? string.Empty, width));
            Text.WordWrap = oldWordWrap;
            Text.Font = oldFont;
            return Mathf.Max(18f, height);
        }
    }
}

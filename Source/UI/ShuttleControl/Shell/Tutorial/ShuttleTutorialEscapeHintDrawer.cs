using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialEscapeHintDrawer
    {
        private const float HintWidth = 160f;
        private const float HintHeight = 26f;
        private const float WindowMargin = 12f;
        private const float LogoGap = 4f;

        internal static void Draw(
            Rect windowRect,
            ShuttleControlTutorialTargetService registry,
            bool hasInstructionRect,
            Rect instructionRect)
        {
            Rect hintRect = GetHintRect(
                windowRect,
                registry,
                hasInstructionRect,
                instructionRect);
            Widgets.DrawBoxSolid(hintRect, new Color(0.04f, 0.05f, 0.06f, 0.82f));
            ShuttleUILayout.DrawRectBorder(
                hintRect,
                new Color(0.38f, 0.72f, 0.96f, 0.82f),
                ShuttleUIStyle.ThinBorder);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.88f, 0.95f, 1f, 1f);
            Widgets.Label(
                hintRect.ContractedBy(5f),
                ShuttleUIText.Tr("CT_Shuttle_Tutorial_EscapeHint"));
            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }

        private static Rect GetHintRect(
            Rect windowRect,
            ShuttleControlTutorialTargetService registry,
            bool hasInstructionRect,
            Rect instructionRect)
        {
            Rect logoRect;
            Rect hintRect = registry != null &&
                registry.TryGet(ShuttleTutorialTargetIds.HeaderLogo, out logoRect)
                    ? new Rect(logoRect.x, logoRect.yMax + LogoGap, HintWidth, HintHeight)
                    : new Rect(windowRect.x + WindowMargin, windowRect.y + 10f, HintWidth, HintHeight);
            return FindNonOverlappingRect(
                hintRect,
                windowRect,
                hasInstructionRect,
                instructionRect);
        }

        private static Rect FindNonOverlappingRect(
            Rect preferredRect,
            Rect windowRect,
            bool hasInstructionRect,
            Rect instructionRect)
        {
            Rect rect = ClampToWindow(preferredRect, windowRect);
            if (!OverlapsInstruction(rect, hasInstructionRect, instructionRect))
            {
                return rect;
            }

            Rect candidate = new Rect(
                windowRect.xMax - WindowMargin - HintWidth,
                windowRect.y + WindowMargin,
                HintWidth,
                HintHeight);
            rect = ClampToWindow(candidate, windowRect);
            if (!OverlapsInstruction(rect, hasInstructionRect, instructionRect))
            {
                return rect;
            }

            candidate = new Rect(
                windowRect.x + WindowMargin,
                windowRect.y + WindowMargin,
                HintWidth,
                HintHeight);
            rect = ClampToWindow(candidate, windowRect);
            if (!OverlapsInstruction(rect, hasInstructionRect, instructionRect))
            {
                return rect;
            }

            candidate = new Rect(
                windowRect.center.x - (HintWidth * 0.5f),
                windowRect.y + WindowMargin,
                HintWidth,
                HintHeight);
            rect = ClampToWindow(candidate, windowRect);
            if (!OverlapsInstruction(rect, hasInstructionRect, instructionRect))
            {
                return rect;
            }

            candidate = new Rect(
                windowRect.xMax - WindowMargin - HintWidth,
                windowRect.yMax - WindowMargin - HintHeight,
                HintWidth,
                HintHeight);
            return ClampToWindow(candidate, windowRect);
        }

        private static bool OverlapsInstruction(
            Rect rect,
            bool hasInstructionRect,
            Rect instructionRect)
        {
            return hasInstructionRect &&
                instructionRect.width > 0f &&
                instructionRect.height > 0f &&
                rect.Overlaps(instructionRect);
        }

        private static Rect ClampToWindow(Rect rect, Rect windowRect)
        {
            float x = ClampCoordinate(
                rect.x,
                windowRect.x + WindowMargin,
                windowRect.xMax - WindowMargin - rect.width);
            float y = ClampCoordinate(
                rect.y,
                windowRect.y + WindowMargin,
                windowRect.yMax - WindowMargin - rect.height);
            return new Rect(x, y, rect.width, rect.height);
        }

        private static float ClampCoordinate(float value, float min, float max)
        {
            return max < min ? min : Mathf.Clamp(value, min, max);
        }
    }
}

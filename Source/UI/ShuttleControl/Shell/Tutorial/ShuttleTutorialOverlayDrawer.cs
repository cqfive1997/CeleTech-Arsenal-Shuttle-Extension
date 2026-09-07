using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialOverlayDrawer
    {
        private readonly ShuttleTutorialInstructionBoxDrawer instructionBoxDrawer =
            new ShuttleTutorialInstructionBoxDrawer();

        private ShuttleTutorialInstructionButtonRects lastButtonRects;

        internal void HandleInputBeforePageDraw(
            ShuttleTutorialSession session,
            ShuttleControlPageId currentPage)
        {
            if (session == null ||
                !session.IsActive ||
                session.CurrentPage != currentPage)
            {
                return;
            }

            Event current = Event.current;
            if (current == null)
            {
                return;
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                session.SkipCurrentPage();
                this.ResetButtonRects();
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                if (this.lastButtonRects.HasRects &&
                    this.lastButtonRects.NextButtonRect.Contains(current.mousePosition))
                {
                    if (session.CanAdvanceCurrentStep)
                    {
                        session.Advance();
                    }

                    this.ResetButtonRects();
                    current.Use();
                    return;
                }

                if (this.lastButtonRects.HasRects &&
                    this.lastButtonRects.SkipButtonRect.Contains(current.mousePosition))
                {
                    session.SkipCurrentPage();
                    this.ResetButtonRects();
                    current.Use();
                    return;
                }
            }

            if (current.isMouse || current.isKey)
            {
                current.Use();
            }
        }

        internal void Draw(
            Rect windowRect,
            ShuttleTutorialSession session,
            ShuttleControlTutorialTargetService registry)
        {
            if (session == null || registry == null || !session.IsActive)
            {
                this.ResetButtonRects();
                return;
            }

            ShuttleTutorialStep step = session.CurrentStep;
            if (step == null)
            {
                this.ResetButtonRects();
                ShuttleTutorialEscapeHintDrawer.Draw(windowRect, registry, false, Rect.zero);
                return;
            }

            if (!step.HasTarget)
            {
                this.DrawIntroStep(
                    windowRect,
                    step,
                    registry,
                    session.CanAdvanceCurrentStep);
                return;
            }

            Rect targetRect;
            if (!ShuttleTutorialStepTargetResolver.TryResolve(step, registry, out targetRect))
            {
                this.ResetButtonRects();
                ShuttleTutorialEscapeHintDrawer.Draw(windowRect, registry, false, Rect.zero);
                return;
            }

            Rect paddedTarget = ShuttleTutorialOverlayLayout.GetPaddedTarget(
                targetRect,
                windowRect,
                step.HighlightPadding);
            if (!ShuttleTutorialRectUtility.IsValid(paddedTarget))
            {
                this.ResetButtonRects();
                ShuttleTutorialEscapeHintDrawer.Draw(windowRect, registry, false, Rect.zero);
                return;
            }

            ShuttleTutorialMaskDrawer.DrawMask(windowRect, paddedTarget);
            ShuttleTutorialMaskDrawer.DrawHighlight(paddedTarget);
            float boxHeight = this.instructionBoxDrawer.CalculateHeight(
                step,
                windowRect,
                ShuttleTutorialOverlayLayout.BoxWidth);
            Rect boxRect = ShuttleTutorialOverlayLayout.CalculateBoxRect(
                windowRect,
                paddedTarget,
                step.Placement,
                boxHeight);
            this.lastButtonRects = this.instructionBoxDrawer.Draw(
                boxRect,
                step,
                session.CanAdvanceCurrentStep);
            ShuttleTutorialEscapeHintDrawer.Draw(windowRect, registry, true, boxRect);
        }

        private void DrawIntroStep(
            Rect windowRect,
            ShuttleTutorialStep step,
            ShuttleControlTutorialTargetService registry,
            bool canAdvance)
        {
            float boxHeight = this.instructionBoxDrawer.CalculateHeight(
                step,
                windowRect,
                ShuttleTutorialOverlayLayout.BoxWidth);
            Rect boxRect = ShuttleTutorialOverlayLayout.CalculateIntroBoxRect(
                windowRect,
                step.Placement,
                boxHeight);
            ShuttleTutorialMaskDrawer.DrawFullMask(windowRect);
            this.lastButtonRects = this.instructionBoxDrawer.Draw(
                boxRect,
                step,
                canAdvance);
            ShuttleTutorialEscapeHintDrawer.Draw(windowRect, registry, true, boxRect);
        }

        private void ResetButtonRects()
        {
            this.lastButtonRects = default(ShuttleTutorialInstructionButtonRects);
        }
    }
}

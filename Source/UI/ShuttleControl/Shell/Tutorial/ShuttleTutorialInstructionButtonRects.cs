using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal struct ShuttleTutorialInstructionButtonRects
    {
        internal readonly Rect NextButtonRect;
        internal readonly Rect SkipButtonRect;
        internal readonly bool HasRects;

        internal ShuttleTutorialInstructionButtonRects(
            Rect nextButtonRect,
            Rect skipButtonRect)
        {
            this.NextButtonRect = nextButtonRect;
            this.SkipButtonRect = skipButtonRect;
            this.HasRects = true;
        }
    }
}

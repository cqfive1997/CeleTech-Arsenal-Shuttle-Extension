using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialStepTargetResolver
    {
        internal static bool TryResolve(
            ShuttleTutorialStep step,
            ShuttleControlTutorialTargetService registry,
            out Rect rect)
        {
            if (step == null || registry == null || !step.HasTarget)
            {
                rect = Rect.zero;
                return false;
            }

            if (registry.TryGet(step.TargetId, out rect))
            {
                return true;
            }

            if (step.HasAlternateTarget &&
                registry.TryGet(step.AlternateTargetId, out rect))
            {
                return true;
            }

            rect = Rect.zero;
            return false;
        }
    }
}

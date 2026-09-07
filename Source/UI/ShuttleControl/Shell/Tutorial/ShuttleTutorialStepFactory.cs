using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialStepFactory
    {
        internal static void Add(
            List<ShuttleTutorialStep> steps,
            string targetId,
            string titleKey,
            string bodyKey,
            ShuttleTutorialPlacement placement,
            float highlightPadding)
        {
            Add(
                steps,
                targetId,
                titleKey,
                bodyKey,
                placement,
                highlightPadding,
                null,
                ShuttleTutorialPreAction.None,
                0f,
                false,
                null);
        }

        internal static void Add(
            List<ShuttleTutorialStep> steps,
            string targetId,
            string titleKey,
            string bodyKey,
            ShuttleTutorialPlacement placement,
            float highlightPadding,
            string alternateTargetId,
            ShuttleTutorialPreAction preAction,
            float minHoldSeconds)
        {
            Add(
                steps,
                targetId,
                titleKey,
                bodyKey,
                placement,
                highlightPadding,
                alternateTargetId,
                preAction,
                minHoldSeconds,
                false,
                null);
        }

        internal static void Add(
            List<ShuttleTutorialStep> steps,
            string targetId,
            string titleKey,
            string bodyKey,
            ShuttleTutorialPlacement placement,
            float highlightPadding,
            string alternateTargetId,
            ShuttleTutorialPreAction preAction,
            float minHoldSeconds,
            bool skipIfTargetMissing)
        {
            Add(
                steps,
                targetId,
                titleKey,
                bodyKey,
                placement,
                highlightPadding,
                alternateTargetId,
                preAction,
                minHoldSeconds,
                skipIfTargetMissing,
                null);
        }

        internal static void Add(
            List<ShuttleTutorialStep> steps,
            string targetId,
            string titleKey,
            string bodyKey,
            ShuttleTutorialPlacement placement,
            float highlightPadding,
            string alternateTargetId,
            ShuttleTutorialPreAction preAction,
            float minHoldSeconds,
            bool skipIfTargetMissing,
            string warningKey)
        {
            if (steps == null)
            {
                return;
            }

            steps.Add(new ShuttleTutorialStep(
                targetId,
                titleKey,
                bodyKey,
                placement,
                highlightPadding,
                alternateTargetId,
                preAction,
                minHoldSeconds,
                skipIfTargetMissing,
                warningKey));
        }
    }
}

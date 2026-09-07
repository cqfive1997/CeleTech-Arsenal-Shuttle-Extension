namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialStep
    {
        internal const float DefaultHighlightPadding = 8f;

        internal string TargetId;
        internal string TitleKey;
        internal string BodyKey;
        internal string WarningKey;
        internal ShuttleTutorialPlacement Placement;
        internal float HighlightPadding;
        internal string AlternateTargetId;
        internal ShuttleTutorialPreAction PreAction;
        internal float MinHoldSeconds;
        internal bool SkipIfTargetMissing;

        internal bool HasTarget
        {
            get { return !string.IsNullOrEmpty(this.TargetId); }
        }

        internal bool HasAlternateTarget
        {
            get { return !string.IsNullOrEmpty(this.AlternateTargetId); }
        }

        internal ShuttleTutorialStep(
            string targetId,
            string titleKey,
            string bodyKey,
            ShuttleTutorialPlacement placement)
            : this(
                targetId,
                titleKey,
                bodyKey,
                placement,
                DefaultHighlightPadding,
                null,
                ShuttleTutorialPreAction.None,
                0f,
                false,
                null)
        {
        }

        internal ShuttleTutorialStep(
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
            this.TargetId = targetId;
            this.TitleKey = titleKey;
            this.BodyKey = bodyKey;
            this.WarningKey = warningKey;
            this.Placement = placement;
            this.HighlightPadding = highlightPadding;
            this.AlternateTargetId = alternateTargetId;
            this.PreAction = preAction;
            this.MinHoldSeconds = minHoldSeconds;
            this.SkipIfTargetMissing = skipIfTargetMissing;
        }
    }
}

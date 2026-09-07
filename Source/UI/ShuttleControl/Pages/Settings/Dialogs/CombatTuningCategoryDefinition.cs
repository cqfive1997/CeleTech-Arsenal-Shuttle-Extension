namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal sealed class CombatTuningCategoryDefinition
    {
        internal readonly ShuttleCombatTuningCategory Category;
        internal readonly string LabelKey;
        internal readonly string HelpKey;
        internal readonly string SecondaryHelpKey;

        internal CombatTuningCategoryDefinition(
            ShuttleCombatTuningCategory category,
            string labelKey,
            string helpKey,
            string secondaryHelpKey)
        {
            this.Category = category;
            this.LabelKey = labelKey;
            this.HelpKey = helpKey;
            this.SecondaryHelpKey = secondaryHelpKey;
        }
    }
}

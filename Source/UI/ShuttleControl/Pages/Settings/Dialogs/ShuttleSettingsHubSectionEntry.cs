namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal enum ShuttleSettingsHubCategory
    {
        Gameplay,
        Performance,
        Combat,
        Paint,
        ShuttleDefense,
        PrisonSupply
    }

    internal sealed class ShuttleSettingsHubSectionEntry
    {
        internal ShuttleSettingsHubSectionEntry(
            ShuttleSettingsHubCategory category,
            string labelKey,
            string descriptionKey,
            IShuttleSettingsHubSection section)
        {
            this.Category = category;
            this.LabelKey = labelKey;
            this.DescriptionKey = descriptionKey;
            this.Section = section;
        }

        internal ShuttleSettingsHubCategory Category { get; private set; }

        internal string LabelKey { get; private set; }

        internal string DescriptionKey { get; private set; }

        internal IShuttleSettingsHubSection Section { get; private set; }
    }
}

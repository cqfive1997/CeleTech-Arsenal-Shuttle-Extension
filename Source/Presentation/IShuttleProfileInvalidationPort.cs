namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal interface IShuttleProfileInvalidationPort
    {
        void MarkCombatTuningSettingsChanged();

        void MarkOtherSettingsChanged();
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialDefinitionProvider :
        IShuttleTutorialDefinitionProvider
    {
        private readonly ShuttleMainTutorialDefinitionBuilder mainBuilder =
            new ShuttleMainTutorialDefinitionBuilder();
        private readonly ShuttleSettingsTutorialDefinitionBuilder settingsBuilder =
            new ShuttleSettingsTutorialDefinitionBuilder();
        private readonly ShuttleCrewTutorialDefinitionBuilder crewBuilder =
            new ShuttleCrewTutorialDefinitionBuilder();
        private readonly ShuttleLoadingTutorialDefinitionBuilder loadingBuilder =
            new ShuttleLoadingTutorialDefinitionBuilder();
        private readonly ShuttleDefenseTutorialDefinitionBuilder defenseBuilder =
            new ShuttleDefenseTutorialDefinitionBuilder();
        private readonly ShuttleMedicalTutorialDefinitionBuilder medicalBuilder =
            new ShuttleMedicalTutorialDefinitionBuilder();
        private readonly ShuttlePrisonCellTutorialDefinitionBuilder prisonCellBuilder =
            new ShuttlePrisonCellTutorialDefinitionBuilder();
        private readonly ShuttleProcessingTutorialDefinitionBuilder processingBuilder =
            new ShuttleProcessingTutorialDefinitionBuilder();
        private readonly ShuttleContextTutorialDefinitionBuilder contextBuilder =
            new ShuttleContextTutorialDefinitionBuilder();

        public ShuttleTutorialDefinition GetDefinition(ShuttleControlPageId page)
        {
            if (page == ShuttleControlPageId.Main)
            {
                return this.mainBuilder.Build();
            }

            if (page == ShuttleControlPageId.Settings)
            {
                return this.settingsBuilder.Build();
            }

            if (page == ShuttleControlPageId.Crew)
            {
                return this.crewBuilder.Build();
            }

            if (page == ShuttleControlPageId.Cargo)
            {
                return this.loadingBuilder.Build();
            }

            if (page == ShuttleControlPageId.Defense)
            {
                return this.defenseBuilder.Build();
            }

            if (page == ShuttleControlPageId.Medical)
            {
                return this.medicalBuilder.Build();
            }

            if (page == ShuttleControlPageId.PrisonCell)
            {
                return this.prisonCellBuilder.Build();
            }

            if (page == ShuttleControlPageId.Processing)
            {
                return this.processingBuilder.Build();
            }

            return null;
        }

        public ShuttleTutorialDefinition GetContextualDefinition(
            ShuttleControlPageId page,
            ShuttleTutorialEvent tutorialEvent)
        {
            return this.contextBuilder.Build(page, tutorialEvent);
        }
    }
}

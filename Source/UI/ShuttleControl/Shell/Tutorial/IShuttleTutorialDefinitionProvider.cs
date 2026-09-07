namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal interface IShuttleTutorialDefinitionProvider
    {
        ShuttleTutorialDefinition GetDefinition(ShuttleControlPageId page);

        ShuttleTutorialDefinition GetContextualDefinition(
            ShuttleControlPageId page,
            ShuttleTutorialEvent tutorialEvent);
    }
}

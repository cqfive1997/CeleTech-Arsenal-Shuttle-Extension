namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialEmptyDefinitionProvider :
        IShuttleTutorialDefinitionProvider
    {
        public ShuttleTutorialDefinition GetDefinition(ShuttleControlPageId page)
        {
            return null;
        }

        public ShuttleTutorialDefinition GetContextualDefinition(
            ShuttleControlPageId page,
            ShuttleTutorialEvent tutorialEvent)
        {
            return null;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal interface IShuttleControlUIReadModelSource
    {
        ShuttleControlReadModel GetControlModelForUI(ShuttleControlPageId page);
    }

    internal interface IShuttleControlUIReadModelInvalidationPort
    {
        void MarkDirty();
    }

    internal interface IShuttlePageReadModelContextBinder
    {
        void FillContext(
            ShuttlePageDrawContext context,
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel);
    }
}

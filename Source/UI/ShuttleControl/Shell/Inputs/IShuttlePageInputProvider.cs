namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs
{
    internal interface IShuttlePageInputProvider
    {
        void FillInputs(
            ShuttlePageDrawContext context,
            ShuttlePageInputBuildContext inputs);
    }
}

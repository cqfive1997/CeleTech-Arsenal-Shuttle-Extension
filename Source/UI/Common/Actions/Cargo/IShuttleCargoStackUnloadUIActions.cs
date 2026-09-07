namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal interface IShuttleCargoStackUnloadUIActions
    {
        bool CanUnloadStack(ShuttleCargoStackActionTarget target);

        bool CanUnloadStack(ShuttleCargoStackActionTarget target, int count);

        bool UnloadStack(ShuttleCargoStackActionTarget target);

        bool UnloadStack(ShuttleCargoStackActionTarget target, int count);

        string GetUnloadTooltip(ShuttleCargoStackActionTarget target);

        string GetQuantityEjectTooltip();

        void OpenQuantityUnload(ShuttleCargoStackActionTarget target);
    }
}

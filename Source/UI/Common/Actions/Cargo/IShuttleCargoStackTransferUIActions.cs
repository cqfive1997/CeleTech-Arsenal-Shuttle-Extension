namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal interface IShuttleCargoStackTransferUIActions
    {
        bool CanTransfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext);

        void Transfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext);

        void Transfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext,
            System.Action onSuccess);

        bool CanTransfer(ShuttleCargoTransferActionTarget transferTarget);

        bool Transfer(
            ShuttleCargoTransferActionTarget transferTarget,
            System.Action onSuccess);

        string GetTransferTooltip(ShuttleCargoTransferActionTarget transferTarget);

        string GetTransferTooltip(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext);
    }
}

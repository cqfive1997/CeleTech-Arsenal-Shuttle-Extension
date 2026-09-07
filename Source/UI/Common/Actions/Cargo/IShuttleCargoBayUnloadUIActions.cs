using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal interface IShuttleCargoBayUnloadUIActions
    {
        bool CanUnloadBay(ShuttleCargoBayActionTarget bay);

        string GetUnloadBayTooltip(ShuttleCargoBayActionTarget bay);

        void ConfirmUnloadBay(
            ShuttleCargoBayActionTarget bay,
            Action onSuccess);
    }
}

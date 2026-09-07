using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    /// <summary>
    /// Callback holder for V3 shell actions. Individual modal launchers bind page
    /// commands without owning modal lifecycle state.
    /// </summary>
    internal sealed class ShuttleUIModalService
    {
        internal Action<IShuttleCargoLoadUIActions> OpenLoadCargoAction;
        internal Action OpenCargoUnloadAction;
        internal Action OpenPageMenuAction;
        internal Action OpenLaunchAction;
        internal Action<string, string> OpenExternalRuntimeAction;
        internal IShuttleCargoLoadUIActions CargoLoadActions;

        internal void OpenLoadCargo()
        {
            if (this.OpenLoadCargoAction != null)
            {
                this.OpenLoadCargoAction(this.CargoLoadActions);
            }
        }

        internal void OpenCargoUnload()
        {
            if (this.OpenCargoUnloadAction != null)
            {
                this.OpenCargoUnloadAction();
            }
        }

        internal void OpenPageMenu()
        {
            if (this.OpenPageMenuAction != null)
            {
                this.OpenPageMenuAction();
            }
        }

        internal void OpenLaunch()
        {
            if (this.OpenLaunchAction != null)
            {
                this.OpenLaunchAction();
            }
        }

        internal void OpenExternalRuntime(string moduleInstanceID, string runtimeSystemKey)
        {
            if (this.OpenExternalRuntimeAction != null)
            {
                this.OpenExternalRuntimeAction(moduleInstanceID, runtimeSystemKey);
            }
        }
    }
}

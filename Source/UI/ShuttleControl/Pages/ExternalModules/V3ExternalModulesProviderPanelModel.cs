using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesProviderPanelModel
    {
        internal ExternalModulePanelRegistration Registration;
        internal ShuttleExternalModulePanelContext Context;
        internal List<ShuttleExternalPanelCommandContribution> Commands =
            new List<ShuttleExternalPanelCommandContribution>();
        internal float PanelHeight;
        internal string Message;
    }
}

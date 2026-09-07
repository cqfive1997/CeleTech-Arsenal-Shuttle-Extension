using CeleTech.ShuttleExtension.ModularShuttle.API.UI;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal sealed class ExternalModulePanelRegistration
    {
        internal ExternalModulePanelRegistration(
            string ownerPackageId,
            string localPanelKey,
            string fullPanelKey,
            string runtimeSystemKey,
            IShuttleExternalModulePanelProvider provider)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalPanelKey = localPanelKey;
            this.FullPanelKey = fullPanelKey;
            this.RuntimeSystemKey = runtimeSystemKey;
            this.Provider = provider;
        }

        internal string OwnerPackageId { get; private set; }

        internal string LocalPanelKey { get; private set; }

        internal string FullPanelKey { get; private set; }

        internal string RuntimeSystemKey { get; private set; }

        internal IShuttleExternalModulePanelProvider Provider { get; private set; }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands.External
{
    internal sealed class ExternalCommandRegistration
    {
        internal ExternalCommandRegistration(
            string ownerPackageId,
            string localCommandKey,
            string fullCommandKey,
            IShuttleExternalCommandHandler handler)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalCommandKey = localCommandKey;
            this.FullCommandKey = fullCommandKey;
            this.Handler = handler;
        }

        internal string OwnerPackageId { get; private set; }

        internal string LocalCommandKey { get; private set; }

        internal string FullCommandKey { get; private set; }

        internal IShuttleExternalCommandHandler Handler { get; private set; }
    }
}

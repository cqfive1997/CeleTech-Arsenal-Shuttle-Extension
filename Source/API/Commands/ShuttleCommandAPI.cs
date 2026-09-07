using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands.External;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Commands
{
    /// <summary>
    /// Public facade for registering third-party external command handlers.
    /// Command handlers are invoked only through the main mod command boundary.
    /// </summary>
    public static class ShuttleCommandAPI
    {
        /// <summary>
        /// Current external command SDK version exposed to third-party mods.
        /// </summary>
        public static Version APIVersion
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        /// <summary>
        /// Registers a command handler under ownerPackageId/localCommandKey.
        /// Commands are later routed through ShuttleExternalCommand and the core dispatcher.
        /// </summary>
        public static bool RegisterCommandHandler(
            string ownerPackageId,
            string localCommandKey,
            IShuttleExternalCommandHandler handler)
        {
            try
            {
                return ExternalShuttleCommandRegistry.Register(
                    ownerPackageId,
                    localCommandKey,
                    handler);
            }
            catch (Exception exception)
            {
                Log.Warning(ExternalShuttleCommandRegistry.LogPrefix +
                    "registration failed unexpectedly. Exception: " + exception);
                return false;
            }
        }
    }
}

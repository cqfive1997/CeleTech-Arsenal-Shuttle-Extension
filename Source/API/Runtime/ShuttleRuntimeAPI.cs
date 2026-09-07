using System;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Public facade for the runtime-system third-party shuttle SDK.
    /// Registered systems run only when module XML binds their full runtime key.
    /// </summary>
    public static class ShuttleRuntimeAPI
    {
        private static readonly Version APIVersionValue = new Version(1, 0, 0);

        /// <summary>
        /// Current external runtime SDK version exposed to third-party mods.
        /// </summary>
        public static Version APIVersion
        {
            get
            {
                return APIVersionValue;
            }
        }

        /// <summary>
        /// Registers an external runtime system under ownerPackageId/localRuntimeKey.
        /// Registration only stores the system; module XML decides where it applies.
        /// </summary>
        public static bool RegisterRuntimeSystem(
            string ownerPackageId,
            string localRuntimeKey,
            IShuttleExternalModuleRuntimeSystem system,
            string runtimeLabelKey,
            string runtimeDescriptionKey)
        {
            try
            {
                return ExternalShuttleRuntimeRegistry.Register(
                    ownerPackageId,
                    localRuntimeKey,
                    system,
                    runtimeLabelKey,
                    runtimeDescriptionKey);
            }
            catch (Exception exception)
            {
                Verse.Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "registration failed unexpectedly: " + exception);
                return false;
            }
        }
    }
}

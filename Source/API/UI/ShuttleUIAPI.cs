using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Public facade for registering third-party UI panel providers.
    /// Providers are hosted only inside core-owned UI regions.
    /// </summary>
    public static class ShuttleUIAPI
    {
        private const string LogPrefix = "[CeleTech ShuttleExtension] UI ";

        /// <summary>
        /// Current external UI SDK version exposed to third-party mods.
        /// </summary>
        public static Version APIVersion
        {
            get
            {
                return new Version(1, 1, 0);
            }
        }

        /// <summary>
        /// Registers a reusable UI icon under ownerPackageId/localIconKey. texPath is a
        /// RimWorld texture path under Textures, without the file extension.
        /// </summary>
        public static bool RegisterIcon(
            string ownerPackageId,
            string localIconKey,
            string texPath)
        {
            try
            {
                return ExternalShuttleIconRegistry.RegisterIcon(
                    ownerPackageId,
                    localIconKey,
                    texPath);
            }
            catch (Exception exception)
            {
                Log.Warning(LogPrefix + "icon registration failed with an unexpected exception. " +
                    "OwnerPackageId=" + ownerPackageId + ", LocalIconKey=" + localIconKey +
                    ". Exception: " + exception);
                return false;
            }
        }

        /// <summary>
        /// Registers the icon used for a module def on module cards, install candidates, and
        /// the External Modules page. moduleDefName must match the module's Def name.
        /// </summary>
        public static bool RegisterModuleIcon(
            string ownerPackageId,
            string moduleDefName,
            string texPath)
        {
            try
            {
                return ExternalShuttleIconRegistry.RegisterModuleIcon(
                    ownerPackageId,
                    moduleDefName,
                    texPath);
            }
            catch (Exception exception)
            {
                Log.Warning(LogPrefix + "module icon registration failed with an unexpected exception. " +
                    "OwnerPackageId=" + ownerPackageId + ", ModuleDefName=" + moduleDefName +
                    ". Exception: " + exception);
                return false;
            }
        }

        /// <summary>
        /// Registers a module panel provider under ownerPackageId/localPanelKey.
        /// Providers are hosted by the core UI and may only use controlled command paths.
        /// </summary>
        public static bool RegisterModulePanelProvider(
            string ownerPackageId,
            string localPanelKey,
            IShuttleExternalModulePanelProvider provider)
        {
            try
            {
                return ExternalModulePanelRegistry.Register(
                    ownerPackageId,
                    localPanelKey,
                    provider);
            }
            catch (Exception exception)
            {
                Log.Warning(LogPrefix + "panel provider registration failed with an unexpected exception. " +
                    "OwnerPackageId=" + ownerPackageId + ", LocalPanelKey=" + localPanelKey +
                    ". Exception: " + exception);
                return false;
            }
        }
    }
}

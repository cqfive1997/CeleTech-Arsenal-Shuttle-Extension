using System;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Profile
{
    /// <summary>
    /// Public facade for the profile-only third-party shuttle SDK.
    /// This API accepts static profile contributors only; it does not expose runtime systems,
    /// commands, UI zones, tick hooks, launch lifecycle hooks, or save data.
    /// </summary>
    public static class ShuttleProfileAPI
    {
        private static readonly Version APIVersionValue = new Version(1, 0, 0);

        /// <summary>
        /// Current profile-only SDK version exposed to third-party mods.
        /// </summary>
        public static Version APIVersion
        {
            get
            {
                return APIVersionValue;
            }
        }

        /// <summary>
        /// Registers a static profile contributor under ownerPackageId/localContributorKey.
        /// Invalid input and duplicate keys are logged and return false instead of throwing.
        /// </summary>
        public static bool RegisterProfileContributor(
            string ownerPackageId,
            string localContributorKey,
            IShuttleProfileContributor contributor)
        {
            if (string.IsNullOrWhiteSpace(ownerPackageId))
            {
                Log.Warning(ShuttleProfileContributorResolver.LogPrefix +
                    "registration rejected: ownerPackageId is empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(localContributorKey))
            {
                Log.Warning(ShuttleProfileContributorResolver.LogPrefix +
                    "registration rejected: localContributorKey is empty for owner '" +
                    ownerPackageId + "'.");
                return false;
            }

            if (contributor == null)
            {
                Log.Warning(ShuttleProfileContributorResolver.LogPrefix +
                    "registration rejected: contributor is null for owner '" +
                    ownerPackageId + "' and local key '" + localContributorKey + "'.");
                return false;
            }

            string fullKey = ownerPackageId.Trim() + "/" + localContributorKey.Trim();
            return ShuttleProfileContributorResolver.RegisterExternalContributor(fullKey, contributor);
        }
    }
}

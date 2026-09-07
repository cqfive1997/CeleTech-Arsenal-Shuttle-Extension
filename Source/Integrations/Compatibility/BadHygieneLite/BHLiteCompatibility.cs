using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    internal static class BHLiteCompatibility
    {
        private static CT_Shuttle_BHLiteIntegrationDef integration;
        private static bool? liteLoaded;
        private static bool? fullDbhLoaded;
        private static bool loggedFullPriority;

        public static void Configure(CT_Shuttle_BHLiteIntegrationDef integrationDef)
        {
            integration = integrationDef;
            liteLoaded = null;
            fullDbhLoaded = null;
            loggedFullPriority = false;
        }

        public static bool IsLiteLoaded
        {
            get
            {
                if (!liteLoaded.HasValue)
                {
                    liteLoaded = AnyPackageActive(integration != null ? integration.packageIds : null);
                }

                return liteLoaded.Value;
            }
        }

        public static bool IsFullDbhLoaded
        {
            get
            {
                if (!fullDbhLoaded.HasValue)
                {
                    fullDbhLoaded = AnyPackageActive(integration != null ? integration.fullDbhPackageIds : null);
                }

                return fullDbhLoaded.Value;
            }
        }

        public static bool FullDbhHasPriority
        {
            get { return IsLiteLoaded && IsFullDbhLoaded; }
        }

        public static string ResolveAvailabilityReason()
        {
            if (FullDbhHasPriority)
            {
                LogFullPriorityOnce();
                return "Bad Hygiene Lite hidden because full Dubs Bad Hygiene is active.";
            }

            if (!IsLiteLoaded)
            {
                return "Bad Hygiene Lite is not active.";
            }

            return "Bad Hygiene Lite is active.";
        }

        public static void LogFullPriorityOnce()
        {
            if (loggedFullPriority)
            {
                return;
            }

            loggedFullPriority = true;
            AdditionalModuleLog.Dev("Bad Hygiene Lite hidden because full Dubs Bad Hygiene is active.");
        }

        private static bool AnyPackageActive(List<string> packageIds)
        {
            if (packageIds == null)
            {
                return false;
            }

            for (int i = 0; i < packageIds.Count; i++)
            {
                string packageId = packageIds[i];
                if (!string.IsNullOrWhiteSpace(packageId) &&
                    ModsConfig.IsActive(packageId.Trim()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

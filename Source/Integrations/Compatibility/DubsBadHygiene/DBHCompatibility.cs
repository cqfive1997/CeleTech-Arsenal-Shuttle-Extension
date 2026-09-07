using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal static class DBHCompatibility
    {
        private static bool? loaded;
        private static CT_Shuttle_DBHIntegrationDef integration;

        public static void Configure(CT_Shuttle_DBHIntegrationDef integrationDef)
        {
            integration = integrationDef;
            loaded = null;
        }

        public static bool IsLoaded
        {
            get
            {
                if (!loaded.HasValue)
                {
                    loaded = ResolveLoaded();
                    if (!loaded.Value)
                    {
                        AdditionalModuleLog.WarningOnce(
                            "dbh-missing",
                            "Dubs Bad Hygiene is not active. DBH shuttle modules will stay in diagnostic mode.");
                    }
                }

                return loaded.Value;
            }
        }

        public static void ResetCacheForTests()
        {
            loaded = null;
        }

        public static string DescribeLoadedState()
        {
            return (IsLoaded ? "CT_Shuttle_Addon_Status_Loaded" : "CT_Shuttle_Addon_Status_Missing")
                .Translate()
                .ToString();
        }

        public static void RecordReflectionFailure(Exception exception)
        {
            AdditionalModuleLog.ErrorOnce(
                "dbh-reflection-failure",
                "Dubs Bad Hygiene bridge failed: " + (exception != null ? exception.Message : "unknown error"));
        }

        private static bool ResolveLoaded()
        {
            if (integration != null &&
                integration.packageIds != null &&
                integration.packageIds.Count > 0)
            {
                for (int i = 0; i < integration.packageIds.Count; i++)
                {
                    string packageId = integration.packageIds[i];
                    if (!string.IsNullOrWhiteSpace(packageId) &&
                        ModsConfig.IsActive(packageId.Trim()))
                    {
                        return true;
                    }
                }

                return false;
            }

            return DefExists<NeedDef>(integration != null ? integration.hygieneNeedDefName : null) ||
                DefExists<NeedDef>(integration != null ? integration.bladderNeedDefName : null) ||
                DefExists<NeedDef>(integration != null ? integration.thirstNeedDefName : null);
        }

        private static bool DefExists<T>(string defName)
            where T : Def
        {
            return !string.IsNullOrWhiteSpace(defName) &&
                DefDatabase<T>.GetNamedSilentFail(defName.Trim()) != null;
        }
    }
}

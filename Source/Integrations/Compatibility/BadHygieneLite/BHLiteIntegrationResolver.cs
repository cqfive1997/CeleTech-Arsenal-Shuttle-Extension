using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    internal static class BHLiteIntegrationResolver
    {
        private const string DefaultIntegrationDefName = "CT_Shuttle_BHLiteIntegration";

        public static CT_Shuttle_BHLiteIntegrationDef ResolveDefaultIntegration()
        {
            return DefDatabase<CT_Shuttle_BHLiteIntegrationDef>.GetNamedSilentFail(DefaultIntegrationDefName);
        }

        public static CT_Shuttle_BHLiteIntegrationDef ResolveIntegration(string defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return ResolveDefaultIntegration();
            }

            return DefDatabase<CT_Shuttle_BHLiteIntegrationDef>.GetNamedSilentFail(defName.Trim());
        }

        public static bool TryGetLocalKeyForOwner(string fullKey, string ownerPackageId, out string localKey)
        {
            localKey = null;
            if (string.IsNullOrWhiteSpace(fullKey) || string.IsNullOrWhiteSpace(ownerPackageId))
            {
                return false;
            }

            string trimmedFullKey = fullKey.Trim();
            string ownerPrefix = ownerPackageId.Trim() + "/";
            if (!trimmedFullKey.StartsWith(ownerPrefix, System.StringComparison.Ordinal))
            {
                return false;
            }

            string parsedLocalKey = trimmedFullKey.Substring(ownerPrefix.Length).Trim();
            if (parsedLocalKey.Length == 0 || parsedLocalKey.IndexOf('/') >= 0)
            {
                return false;
            }

            localKey = parsedLocalKey;
            return true;
        }

        public static bool ModuleHasLiteHygieneExtension(string moduleDefName, CT_Shuttle_BHLiteIntegrationDef integration)
        {
            if (string.IsNullOrWhiteSpace(moduleDefName) || integration == null)
            {
                return false;
            }

            ShuttleModuleBaseDef moduleDef = DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(moduleDefName.Trim());
            if (moduleDef == null)
            {
                return false;
            }

            BHLiteHygieneModuleExtension extension = moduleDef.GetModExtension<BHLiteHygieneModuleExtension>();
            if (extension == null || ResolveIntegration(extension.integrationDefName) != integration)
            {
                return false;
            }

            ShuttleRuntimeSystemDefExtension runtimeExtension = moduleDef.GetModExtension<ShuttleRuntimeSystemDefExtension>();
            return runtimeExtension != null && RuntimeExtensionContains(runtimeExtension, integration.runtimeSystemKey);
        }

        private static bool RuntimeExtensionContains(ShuttleRuntimeSystemDefExtension extension, string runtimeSystemKey)
        {
            if (extension == null || string.IsNullOrWhiteSpace(runtimeSystemKey))
            {
                return false;
            }

            string expectedKey = runtimeSystemKey.Trim();
            System.Collections.Generic.List<string> keys = extension.GetRuntimeSystemKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == expectedKey)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

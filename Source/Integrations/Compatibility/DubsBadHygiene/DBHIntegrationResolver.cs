using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal static class DBHIntegrationResolver
    {
        private const string DefaultIntegrationDefName = "CT_Shuttle_DBHIntegration";
        private const string HygieneSuiteModuleDefName = "CT_Shuttle_DBH_HygieneSuiteModule";
        private const string LivingSegmentTypeId = "living";
        private const string HabitatModuleTypeId = "habitat";

        public static CT_Shuttle_DBHIntegrationDef ResolveDefaultIntegration()
        {
            return DefDatabase<CT_Shuttle_DBHIntegrationDef>.GetNamedSilentFail(DefaultIntegrationDefName);
        }

        public static CT_Shuttle_DBHIntegrationDef ResolveIntegration(string defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return ResolveDefaultIntegration();
            }

            return DefDatabase<CT_Shuttle_DBHIntegrationDef>.GetNamedSilentFail(defName.Trim());
        }

        public static string BuildFullKey(string ownerPackageId, string localKey)
        {
            if (string.IsNullOrWhiteSpace(ownerPackageId) || string.IsNullOrWhiteSpace(localKey))
            {
                return null;
            }

            return ownerPackageId.Trim() + "/" + localKey.Trim();
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

        public static bool ModuleHasHygieneSuiteExtension(string moduleDefName, CT_Shuttle_DBHIntegrationDef integration)
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

            DBHHygieneSuiteModuleExtension extension = moduleDef.GetModExtension<DBHHygieneSuiteModuleExtension>();
            if (extension == null)
            {
                return false;
            }

            CT_Shuttle_DBHIntegrationDef extensionIntegration = ResolveIntegration(extension.integrationDefName);
            if (extensionIntegration != integration)
            {
                return false;
            }

            ShuttleRuntimeSystemDefExtension runtimeExtension = moduleDef.GetModExtension<ShuttleRuntimeSystemDefExtension>();
            if (runtimeExtension == null || !RuntimeExtensionContains(runtimeExtension, integration.runtimeSystemKey))
            {
                AdditionalModuleLog.WarningOnce(
                    "dbh-runtime-key-drift|" + moduleDef.defName,
                    "DBH module '" + moduleDef.defName + "' has a hygiene module extension but does not bind runtimeSystemKey '" + integration.runtimeSystemKey + "'.");
                return false;
            }

            return true;
        }

        public static DBHBindingDiagnostic ValidateHygieneSuiteModuleBinding(CT_Shuttle_DBHIntegrationDef integration)
        {
            DBHBindingDiagnostic diagnostic = BuildHygieneSuiteBindingDiagnostic(integration);
            if (diagnostic != null && !diagnostic.IsValid)
            {
                AdditionalModuleLog.WarningOnce(
                    "dbh-hygiene-suite-binding-diagnostic",
                    diagnostic.DevDetails);
            }

            return diagnostic;
        }

        public static DBHBindingDiagnostic BuildHygieneSuiteBindingDiagnostic(CT_Shuttle_DBHIntegrationDef integration)
        {
            DBHBindingDiagnostic diagnostic = new DBHBindingDiagnostic();
            diagnostic.ModuleDefName = HygieneSuiteModuleDefName;
            diagnostic.ExpectedRuntimeSystemKey = integration != null ? integration.runtimeSystemKey : null;

            if (integration == null)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_IntegrationMissing");
                diagnostic.DevDetails = "DBH integration def is missing; runtime binding cannot be validated.";
                return diagnostic;
            }

            ShuttleModuleBaseDef moduleDef = DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(HygieneSuiteModuleDefName);
            if (moduleDef == null)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_ModuleMissing");
                diagnostic.DevDetails = "Could not resolve module def '" + HygieneSuiteModuleDefName + "'.";
                return diagnostic;
            }

            diagnostic.ModuleDefFound = true;
            diagnostic.ModuleTypeId = moduleDef.moduleTypeID;
            diagnostic.LivingInstallAllowed = ContainsString(moduleDef.installableSegmentTypes, LivingSegmentTypeId);
            diagnostic.HabitatSlotAllowed = ContainsString(moduleDef.installableModuleSlotTypes, HabitatModuleTypeId);
            diagnostic.HabitatModuleType = moduleDef.moduleTypeID == HabitatModuleTypeId;

            DBHHygieneSuiteModuleExtension hygieneExtension = moduleDef.GetModExtension<DBHHygieneSuiteModuleExtension>();
            if (hygieneExtension == null)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_ExtensionMissing");
                diagnostic.DevDetails = "Module '" + HygieneSuiteModuleDefName + "' is missing DBHHygieneSuiteModuleExtension.";
                return diagnostic;
            }

            diagnostic.HygieneExtensionFound = true;
            diagnostic.ExtensionIntegrationDefName = hygieneExtension.integrationDefName;
            diagnostic.IntegrationMatches = ResolveIntegration(hygieneExtension.integrationDefName) == integration;
            if (!diagnostic.IntegrationMatches)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_DifferentIntegration");
                diagnostic.DevDetails = "Module '" + HygieneSuiteModuleDefName + "' extension integrationDefName is '" +
                    (hygieneExtension.integrationDefName ?? "-") + "', expected '" + integration.defName + "'.";
                return diagnostic;
            }

            ShuttleRuntimeSystemDefExtension runtimeExtension = moduleDef.GetModExtension<ShuttleRuntimeSystemDefExtension>();
            if (runtimeExtension == null)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_RuntimeBindingMissing");
                diagnostic.DevDetails = "Module '" + HygieneSuiteModuleDefName + "' is missing ShuttleRuntimeSystemDefExtension.";
                return diagnostic;
            }

            diagnostic.RuntimeExtensionFound = true;
            diagnostic.ModuleRuntimeSystemKeys = string.Join(", ", runtimeExtension.GetRuntimeSystemKeys().ToArray());
            diagnostic.RuntimeKeyMatches = RuntimeExtensionContains(runtimeExtension, integration.runtimeSystemKey);
            if (!diagnostic.RuntimeKeyMatches)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_RuntimeKeyMismatch");
                diagnostic.DevDetails = "Module '" + HygieneSuiteModuleDefName + "' runtime keys are [" +
                    diagnostic.ModuleRuntimeSystemKeys + "], expected '" + integration.runtimeSystemKey + "'.";
                return diagnostic;
            }

            if (!diagnostic.HabitatModuleType || !diagnostic.LivingInstallAllowed || !diagnostic.HabitatSlotAllowed)
            {
                diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_InstallTargetMismatch");
                diagnostic.DevDetails = "Module '" + HygieneSuiteModuleDefName + "' moduleTypeID='" +
                    (moduleDef.moduleTypeID ?? "-") + "', living segment allowed=" +
                    diagnostic.LivingInstallAllowed + ", habitat slot allowed=" + diagnostic.HabitatSlotAllowed + ".";
                return diagnostic;
            }

            diagnostic.IsValid = true;
            diagnostic.Summary = Tr("CT_Shuttle_Addon_DBH_Binding_Valid");
            diagnostic.DevDetails = "Module '" + HygieneSuiteModuleDefName + "' binds runtimeSystemKey '" +
                integration.runtimeSystemKey + "' and targets moduleTypeID 'habitat' in segment type 'living'.";
            return diagnostic;
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

        private static bool ContainsString(List<string> values, string expected)
        {
            if (values == null || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == expected)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }
    }

    internal sealed class DBHBindingDiagnostic
    {
        public bool IsValid;
        public bool ModuleDefFound;
        public bool HygieneExtensionFound;
        public bool RuntimeExtensionFound;
        public bool RuntimeKeyMatches;
        public bool IntegrationMatches;
        public bool LivingInstallAllowed;
        public bool HabitatSlotAllowed;
        public bool HabitatModuleType;
        public string ModuleDefName;
        public string ModuleTypeId;
        public string ExtensionIntegrationDefName;
        public string ExpectedRuntimeSystemKey;
        public string ModuleRuntimeSystemKeys;
        public string Summary;
        public string DevDetails;
    }
}

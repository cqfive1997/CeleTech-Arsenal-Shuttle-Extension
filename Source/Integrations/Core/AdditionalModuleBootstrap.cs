using CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene;
using CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule
{
    [StaticConstructorOnStartup]
    internal static class AdditionalModuleBootstrap
    {
        private const string HygieneIconPath = "Icon/Module/Support/Bad Hygiene";
        private const string DbhHygieneModuleDefName = "CT_Shuttle_DBH_HygieneSuiteModule";
        private const string BHLiteHygieneModuleDefName = "CT_Shuttle_BHLite_HygieneModule";

        static AdditionalModuleBootstrap()
        {
            string ownerPackageId = AdditionalModuleIdentity.ResolvePackageId();
            RegisterDubsBadHygiene(ownerPackageId);
            RegisterBadHygieneLite(ownerPackageId);
        }

        private static void RegisterDubsBadHygiene(string ownerPackageId)
        {
            CT_Shuttle_DBHIntegrationDef integration = DBHIntegrationResolver.ResolveDefaultIntegration();
            if (integration == null)
            {
                AdditionalModuleLog.ErrorOnce(
                    "dbh-integration-def-missing",
                    "Could not find CT_Shuttle_DBHIntegrationDef 'CT_Shuttle_DBHIntegration'. DBH integration was not registered.");
                return;
            }

            string parsedRuntimeRegistrationKey;
            if (!DBHIntegrationResolver.TryGetLocalKeyForOwner(
                integration.runtimeSystemKey,
                ownerPackageId,
                out parsedRuntimeRegistrationKey))
            {
                AdditionalModuleLog.ErrorOnce(
                    "dbh-runtime-key-missing",
                    "DBH integration def '" + integration.defName + "' has invalid runtimeSystemKey for owner '" + ownerPackageId + "'. Runtime was not registered.");
                return;
            }

            string parsedPanelRegistrationKey;
            if (!DBHIntegrationResolver.TryGetLocalKeyForOwner(
                integration.panelSystemKey,
                ownerPackageId,
                out parsedPanelRegistrationKey))
            {
                AdditionalModuleLog.ErrorOnce(
                    "dbh-panel-key-missing",
                    "DBH integration def '" + integration.defName + "' has invalid panelSystemKey for owner '" + ownerPackageId + "'. UI provider was not registered.");
                return;
            }

            DBHCompatibility.Configure(integration);
            DBHIntegrationResolver.ValidateHygieneSuiteModuleBinding(integration);
            ShuttleUIAPI.RegisterModuleIcon(
                ownerPackageId,
                DbhHygieneModuleDefName,
                HygieneIconPath);

            ShuttleRuntimeAPI.RegisterRuntimeSystem(
                ownerPackageId,
                parsedRuntimeRegistrationKey,
                DBHModuleRuntimeFactory.CreateHygieneSuiteRuntime(integration),
                integration.runtimeLabelKey,
                integration.runtimeDescriptionKey);

            ShuttleUIAPI.RegisterModulePanelProvider(
                ownerPackageId,
                parsedPanelRegistrationKey,
                new DBHModuleUIDrawer(integration.runtimeSystemKey.Trim()));

            ShuttleCommandAPI.RegisterCommandHandler(
                ownerPackageId,
                DBHPipeCheckCommandHandler.LocalCommandKey,
                new DBHPipeCheckCommandHandler());

            ShuttleCommandAPI.RegisterCommandHandler(
                ownerPackageId,
                DBHTankCapacityCommandHandler.LocalCommandKey,
                new DBHTankCapacityCommandHandler());

            AdditionalModuleLog.Dev("Registered Dubs Bad Hygiene shuttle integration runtime, UI provider, and commands.");
        }

        private static void RegisterBadHygieneLite(string ownerPackageId)
        {
            CT_Shuttle_BHLiteIntegrationDef integration = BHLiteIntegrationResolver.ResolveDefaultIntegration();
            if (integration == null)
            {
                AdditionalModuleLog.WarningOnce(
                    "bhlite-integration-def-missing",
                    "Could not find CT_Shuttle_BHLiteIntegrationDef 'CT_Shuttle_BHLiteIntegration'. Bad Hygiene Lite integration was not registered.");
                return;
            }

            string parsedRuntimeRegistrationKey;
            if (!BHLiteIntegrationResolver.TryGetLocalKeyForOwner(
                integration.runtimeSystemKey,
                ownerPackageId,
                out parsedRuntimeRegistrationKey))
            {
                AdditionalModuleLog.ErrorOnce(
                    "bhlite-runtime-key-missing",
                    "Bad Hygiene Lite integration def '" + integration.defName + "' has invalid runtimeSystemKey for owner '" + ownerPackageId + "'. Runtime was not registered.");
                return;
            }

            string parsedPanelRegistrationKey;
            if (!BHLiteIntegrationResolver.TryGetLocalKeyForOwner(
                integration.panelSystemKey,
                ownerPackageId,
                out parsedPanelRegistrationKey))
            {
                AdditionalModuleLog.ErrorOnce(
                    "bhlite-panel-key-missing",
                    "Bad Hygiene Lite integration def '" + integration.defName + "' has invalid panelSystemKey for owner '" + ownerPackageId + "'. UI provider was not registered.");
                return;
            }

            BHLiteCompatibility.Configure(integration);
            ShuttleUIAPI.RegisterModuleIcon(
                ownerPackageId,
                BHLiteHygieneModuleDefName,
                HygieneIconPath);

            ShuttleRuntimeAPI.RegisterRuntimeSystem(
                ownerPackageId,
                parsedRuntimeRegistrationKey,
                BHLiteModuleRuntimeFactory.CreateHygieneRuntime(integration),
                integration.runtimeLabelKey,
                integration.runtimeDescriptionKey);

            ShuttleUIAPI.RegisterModulePanelProvider(
                ownerPackageId,
                parsedPanelRegistrationKey,
                new BHLiteModuleUIDrawer(integration.runtimeSystemKey.Trim()));

            AdditionalModuleLog.Dev("Registered Bad Hygiene Lite diagnostic runtime and UI provider.");
        }
    }
}

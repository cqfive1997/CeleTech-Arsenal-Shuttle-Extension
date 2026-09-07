namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal static class DBHModuleRuntimeFactory
    {
        public static DBHModuleRuntime CreateHygieneSuiteRuntime(CT_Shuttle_DBHIntegrationDef integration)
        {
            return new DBHModuleRuntime(integration, new DBHReflectionBridge(integration));
        }
    }
}

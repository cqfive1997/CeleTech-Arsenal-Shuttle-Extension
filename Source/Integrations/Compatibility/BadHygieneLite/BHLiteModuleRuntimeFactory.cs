namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    internal static class BHLiteModuleRuntimeFactory
    {
        public static BHLiteModuleRuntime CreateHygieneRuntime(CT_Shuttle_BHLiteIntegrationDef integration)
        {
            return new BHLiteModuleRuntime(integration, new BHLiteReflectionBridge(integration));
        }
    }
}

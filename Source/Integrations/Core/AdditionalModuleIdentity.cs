namespace CeleTech.ShuttleExtension.AdditionalModule
{
    internal static class AdditionalModuleIdentity
    {
        public const string FallbackPackageId = "celetech.shuttle.extension.integrations";

        public static string ResolvePackageId()
        {
            return FallbackPackageId;
        }
    }
}

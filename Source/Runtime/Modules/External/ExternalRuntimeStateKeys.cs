namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeStateKeys
    {
        internal const string ReservedPrefix = "__ct.";
        internal const string RuntimeEnabled = "__ct.runtime.enabled";

        internal static bool IsReservedKey(string key)
        {
            return !string.IsNullOrEmpty(key) &&
                key.StartsWith(ReservedPrefix, System.StringComparison.Ordinal);
        }
    }
}

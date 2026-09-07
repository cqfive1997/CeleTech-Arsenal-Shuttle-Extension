namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeStateUtility
    {
        internal static bool IsRuntimeEnabled(ExternalModuleRuntimeState state)
        {
            if (state == null)
            {
                return true;
            }

            string rawValue;
            if (!state.TryGetRaw(ExternalRuntimeStateKeys.RuntimeEnabled, out rawValue))
            {
                return true;
            }

            bool enabled;
            return !bool.TryParse(rawValue, out enabled) || enabled;
        }

        internal static void SetRuntimeEnabled(
            ExternalModuleRuntimeState state,
            bool enabled)
        {
            if (state == null)
            {
                return;
            }

            state.SetSystemRaw(
                ExternalRuntimeStateKeys.RuntimeEnabled,
                enabled.ToString().ToLowerInvariant());
        }
    }
}

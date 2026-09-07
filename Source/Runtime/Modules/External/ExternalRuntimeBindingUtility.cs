using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeBindingUtility
    {
        internal static List<string> GetRuntimeSystemKeys(ShuttleModuleBaseDef moduleDef)
        {
            if (moduleDef == null)
            {
                return new List<string>();
            }

            ShuttleRuntimeSystemDefExtension extension =
                moduleDef.GetModExtension<ShuttleRuntimeSystemDefExtension>();
            return extension != null
                ? extension.GetRuntimeSystemKeys()
                : new List<string>();
        }

        internal static bool ModuleDefHasRuntimeKey(
            ShuttleModuleBaseDef moduleDef,
            string fullRuntimeKey)
        {
            string normalizedKey = NormalizeKey(fullRuntimeKey);
            if (moduleDef == null || normalizedKey == null)
            {
                return false;
            }

            List<string> runtimeKeys = GetRuntimeSystemKeys(moduleDef);
            for (int i = 0; i < runtimeKeys.Count; i++)
            {
                if (runtimeKeys[i] == normalizedKey)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool ModuleHasRuntimeKey(
            ShuttleModule module,
            string fullRuntimeKey)
        {
            if (module == null)
            {
                return false;
            }

            return ModuleDefHasRuntimeKey(module.ModuleDef, fullRuntimeKey);
        }

        internal static List<string> GetRuntimeSystemKeys(
            ShuttleLaunchModuleRecord moduleRecord)
        {
            return GetRuntimeSystemKeys(ResolveModuleDef(moduleRecord));
        }

        internal static bool LaunchModuleRecordHasRuntimeKey(
            ShuttleLaunchModuleRecord moduleRecord,
            string fullRuntimeKey)
        {
            string normalizedKey = NormalizeKey(fullRuntimeKey);
            if (moduleRecord == null || normalizedKey == null)
            {
                return false;
            }

            List<string> runtimeKeys = GetRuntimeSystemKeys(moduleRecord);
            for (int i = 0; i < runtimeKeys.Count; i++)
            {
                if (runtimeKeys[i] == normalizedKey)
                {
                    return true;
                }
            }

            return false;
        }

        private static ShuttleModuleBaseDef ResolveModuleDef(ShuttleLaunchModuleRecord moduleRecord)
        {
            if (moduleRecord == null)
            {
                return null;
            }

            if (moduleRecord.ModuleDef != null)
            {
                return moduleRecord.ModuleDef;
            }

            return !string.IsNullOrEmpty(moduleRecord.ModuleDefName)
                ? DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(moduleRecord.ModuleDefName)
                : null;
        }

        private static string NormalizeKey(string key)
        {
            return ShuttleRuntimeSystemKeyUtility.Normalize(key);
        }
    }
}

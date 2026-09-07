using System.Collections.Generic;
using System.Collections.ObjectModel;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeInfoFactory
    {
        internal static ShuttleExternalModuleInfo CreateModuleInfo(ShuttleModule module)
        {
            return CreateModuleInfo(
                module,
                module != null ? module.ModuleDef : null);
        }

        internal static ShuttleExternalModuleInfo CreateModuleInfo(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef)
        {
            Dictionary<string, string> instanceConfig = module != null
                ? module.CopyInstanceConfig()
                : new Dictionary<string, string>();

            return new ShuttleExternalModuleInfo(
                module != null ? module.ModuleInstanceID : null,
                module != null ? module.moduleDefName : (moduleDef != null ? moduleDef.defName : null),
                moduleDef != null ? moduleDef.moduleTypeID : null,
                GetDefLabel(moduleDef),
                moduleDef != null ? moduleDef.mass : 0f,
                moduleDef != null ? moduleDef.idlePowerDrawWatts : 0f,
                new ReadOnlyDictionary<string, string>(instanceConfig));
        }

        internal static ShuttleExternalModuleInfo CreateModuleInfo(
            ShuttleModuleRuntimeContext context)
        {
            ShuttleModuleBaseDef moduleDef = context != null ? context.ModuleDef : null;
            Dictionary<string, string> instanceConfig = context != null && context.ModuleInstanceConfig != null
                ? CopyDictionary(context.ModuleInstanceConfig)
                : new Dictionary<string, string>();

            return new ShuttleExternalModuleInfo(
                context != null ? context.ModuleInstanceID : null,
                context != null ? context.ModuleDefName : (moduleDef != null ? moduleDef.defName : null),
                moduleDef != null ? moduleDef.moduleTypeID : null,
                GetDefLabel(moduleDef),
                moduleDef != null ? moduleDef.mass : 0f,
                moduleDef != null ? moduleDef.idlePowerDrawWatts : 0f,
                new ReadOnlyDictionary<string, string>(instanceConfig));
        }

        internal static ShuttleExternalLaunchModuleInfo CreateLaunchModuleInfo(ShuttleModule module)
        {
            ShuttleModuleBaseDef moduleDef = module != null ? module.ModuleDef : null;
            Dictionary<string, string> instanceConfig = module != null
                ? module.CopyInstanceConfig()
                : new Dictionary<string, string>();

            return new ShuttleExternalLaunchModuleInfo(
                module != null ? module.ModuleInstanceID : null,
                module != null ? module.moduleDefName : (moduleDef != null ? moduleDef.defName : null),
                moduleDef != null ? moduleDef.moduleTypeID : null,
                GetDefLabel(moduleDef),
                moduleDef != null ? moduleDef.mass : 0f,
                moduleDef != null ? moduleDef.idlePowerDrawWatts : 0f,
                new ReadOnlyDictionary<string, string>(instanceConfig));
        }

        internal static ShuttleExternalLaunchModuleInfo CreateLaunchModuleInfo(
            ShuttleLaunchModuleRecord moduleRecord)
        {
            ShuttleModuleBaseDef moduleDef = ResolveModuleDef(moduleRecord);
            string moduleDefName = moduleRecord != null
                ? moduleRecord.ModuleDefName
                : moduleDef != null ? moduleDef.defName : null;
            if (string.IsNullOrEmpty(moduleDefName) && moduleDef != null)
            {
                moduleDefName = moduleDef.defName;
            }

            // ShuttleLaunchModuleRecord currently snapshots module identity and defs, but not
            // per-instance config. Keep the public DTO detached and fall back to an empty config
            // until the launch snapshot grows that field.
            Dictionary<string, string> instanceConfig = new Dictionary<string, string>();

            return new ShuttleExternalLaunchModuleInfo(
                moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                moduleDefName,
                moduleDef != null ? moduleDef.moduleTypeID : null,
                GetDefLabel(moduleDef, moduleDefName),
                moduleDef != null ? moduleDef.mass : 0f,
                moduleDef != null ? moduleDef.idlePowerDrawWatts : 0f,
                new ReadOnlyDictionary<string, string>(instanceConfig));
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

        private static string GetDefLabel(Def def)
        {
            return GetDefLabel(def, null);
        }

        private static string GetDefLabel(Def def, string fallbackDefName)
        {
            if (def == null)
            {
                return !string.IsNullOrEmpty(fallbackDefName) ? fallbackDefName : string.Empty;
            }

            string label = def.LabelCap.ToString();
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }

            return !string.IsNullOrEmpty(def.defName)
                ? def.defName
                : (!string.IsNullOrEmpty(fallbackDefName) ? fallbackDefName : string.Empty);
        }

        private static Dictionary<string, string> CopyDictionary(IReadOnlyDictionary<string, string> source)
        {
            Dictionary<string, string> copy = new Dictionary<string, string>();
            if (source == null)
            {
                return copy;
            }

            foreach (KeyValuePair<string, string> pair in source)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }
    }
}

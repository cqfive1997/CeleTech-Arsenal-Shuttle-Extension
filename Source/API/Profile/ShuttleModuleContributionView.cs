using System.Collections.Generic;
using System.Collections.ObjectModel;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Profile
{
    /// <summary>
    /// Read-only module facts exposed to third-party profile contributors.
    /// This view is detached from the live AssemblyState module instance.
    /// </summary>
    public sealed class ShuttleModuleContributionView
    {
        public ShuttleModuleContributionView(
            string moduleInstanceId,
            string moduleDefName,
            string moduleTypeId,
            string label,
            float mass,
            float idlePowerDrawWatts,
            IReadOnlyDictionary<string, string> instanceConfig)
        {
            this.ModuleInstanceId = moduleInstanceId;
            this.ModuleDefName = moduleDefName;
            this.ModuleTypeId = moduleTypeId;
            this.Label = label;
            this.Mass = mass;
            this.IdlePowerDrawWatts = idlePowerDrawWatts;
            this.InstanceConfig = instanceConfig ?? EmptyConfig();
        }

        public string ModuleInstanceId { get; private set; }

        public string ModuleDefName { get; private set; }

        public string ModuleTypeId { get; private set; }

        public string Label { get; private set; }

        public float Mass { get; private set; }

        public float IdlePowerDrawWatts { get; private set; }

        public IReadOnlyDictionary<string, string> InstanceConfig { get; private set; }

        internal static ShuttleModuleContributionView From(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef)
        {
            Dictionary<string, string> instanceConfig = module != null
                ? module.CopyInstanceConfig()
                : new Dictionary<string, string>();

            return new ShuttleModuleContributionView(
                module != null ? module.ModuleInstanceID : null,
                module != null ? module.moduleDefName : (moduleDef != null ? moduleDef.defName : null),
                moduleDef != null ? moduleDef.moduleTypeID : null,
                GetDefLabel(moduleDef),
                moduleDef != null ? moduleDef.mass : 0f,
                moduleDef != null ? moduleDef.idlePowerDrawWatts : 0f,
                new ReadOnlyDictionary<string, string>(instanceConfig));
        }

        private static IReadOnlyDictionary<string, string> EmptyConfig()
        {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }

        private static string GetDefLabel(Verse.Def def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            string label = def.LabelCap.ToString();
            return !string.IsNullOrEmpty(label) ? label : def.defName;
        }
    }
}

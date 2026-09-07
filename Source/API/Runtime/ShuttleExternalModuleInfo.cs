using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Detached read-only module facts for external runtime systems.
    /// Runtime R1 does not connect this DTO to live shuttle state yet.
    /// </summary>
    public sealed class ShuttleExternalModuleInfo
    {
        public ShuttleExternalModuleInfo(
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

        private static IReadOnlyDictionary<string, string> EmptyConfig()
        {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Detached read-only launch-time module facts for external runtime systems.
    /// Internal launch records are not exposed through this DTO.
    /// </summary>
    public sealed class ShuttleExternalLaunchModuleInfo
    {
        public ShuttleExternalLaunchModuleInfo(
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

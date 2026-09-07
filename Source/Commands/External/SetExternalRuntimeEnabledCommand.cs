using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands.External
{
    public sealed class SetExternalRuntimeEnabledCommand : IShuttleCommand
    {
        public const string CommandKey = "ct.external-runtime.set-enabled";

        public SetExternalRuntimeEnabledCommand(
            string moduleInstanceID,
            string runtimeSystemKey,
            bool enabled)
        {
            this.ModuleInstanceID = NormalizeKey(moduleInstanceID);
            this.RuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get
            {
                return CommandKey;
            }
        }

        public string ModuleInstanceID { get; private set; }

        public string RuntimeSystemKey { get; private set; }

        public bool Enabled { get; private set; }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

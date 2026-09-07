using System.Collections.Generic;
using System.Collections.ObjectModel;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Commands
{
    /// <summary>
    /// Core-routed command request targeting one external runtime on one module instance.
    /// Arguments are copied string-string data; no object payloads or delegates are allowed.
    /// </summary>
    public sealed class ShuttleExternalCommand : IShuttleCommand
    {
        public ShuttleExternalCommand(
            string commandKey,
            string runtimeSystemKey,
            string moduleInstanceID,
            IReadOnlyDictionary<string, string> arguments)
        {
            this.CommandKey = NormalizeKey(commandKey);
            this.RuntimeSystemKey = NormalizeKey(runtimeSystemKey);
            this.ModuleInstanceID = NormalizeKey(moduleInstanceID);
            this.Arguments = new ReadOnlyDictionary<string, string>(CopyArguments(arguments));
        }

        public string CommandID
        {
            get
            {
                return this.CommandKey;
            }
        }

        public string CommandKey { get; private set; }

        public string RuntimeSystemKey { get; private set; }

        public string ModuleInstanceID { get; private set; }

        public IReadOnlyDictionary<string, string> Arguments { get; private set; }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static Dictionary<string, string> CopyArguments(
            IReadOnlyDictionary<string, string> arguments)
        {
            Dictionary<string, string> copy = new Dictionary<string, string>();
            if (arguments == null)
            {
                return copy;
            }

            foreach (KeyValuePair<string, string> pair in arguments)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                // Preserve null values so handlers can distinguish "present with null"
                // from a missing key. Keys are trimmed but not lower-cased.
                copy[pair.Key.Trim()] = pair.Value;
            }

            return copy;
        }
    }
}

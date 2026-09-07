using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Host-rendered panel command declaration. It contains display text, a registered
    /// full command key, string arguments, enabled state, and ordering metadata only.
    /// </summary>
    public sealed class ShuttleExternalPanelCommandContribution
    {
        public ShuttleExternalPanelCommandContribution(
            string localKey,
            string label,
            string tooltip,
            string commandKey,
            IReadOnlyDictionary<string, string> arguments,
            bool enabled,
            string disabledReason,
            int order)
        {
            this.LocalKey = NormalizeKey(localKey);
            this.CommandKey = NormalizeKey(commandKey);
            this.Label = string.IsNullOrWhiteSpace(label) ? this.CommandKey : label;
            this.Tooltip = tooltip;
            this.Arguments = new ReadOnlyDictionary<string, string>(CopyArguments(arguments));
            this.Enabled = enabled;
            this.DisabledReason = disabledReason;
            this.Order = order;
        }

        public string LocalKey { get; private set; }

        public string Label { get; private set; }

        public string Tooltip { get; private set; }

        public string CommandKey { get; private set; }

        public IReadOnlyDictionary<string, string> Arguments { get; private set; }

        public bool Enabled { get; private set; }

        public string DisabledReason { get; private set; }

        public int Order { get; private set; }

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

                copy[pair.Key.Trim()] = pair.Value;
            }

            return copy;
        }
    }
}

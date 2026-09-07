using System.Globalization;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalRuntimeStateReader : IShuttleExternalRuntimeStateReader
    {
        private readonly ExternalModuleRuntimeState state;

        internal ExternalRuntimeStateReader(ExternalModuleRuntimeState state)
        {
            this.state = state;
        }

        public int SchemaVersion
        {
            get
            {
                return this.state != null ? this.state.SchemaVersion : 0;
            }
        }

        public bool TryGetString(string key, out string value)
        {
            value = null;
            return this.state != null && this.state.TryGetRaw(key, out value);
        }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            string raw;
            if (!this.TryGetString(key, out raw))
            {
                return false;
            }

            return int.TryParse(
                raw,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            string raw;
            if (!this.TryGetString(key, out raw))
            {
                return false;
            }

            return float.TryParse(
                raw,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            string raw;
            if (!this.TryGetString(key, out raw))
            {
                return false;
            }

            return bool.TryParse(raw, out value);
        }

        public bool ContainsKey(string key)
        {
            return this.state != null && this.state.ContainsKey(key);
        }
    }
}

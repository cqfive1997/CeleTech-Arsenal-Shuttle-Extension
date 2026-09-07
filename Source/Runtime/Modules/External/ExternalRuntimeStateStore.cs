using System.Globalization;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalRuntimeStateStore : IShuttleExternalRuntimeStateTryStoreWithReasons
    {
        private readonly ExternalModuleRuntimeState state;

        internal ExternalRuntimeStateStore(ExternalModuleRuntimeState state)
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

        public void SetString(string key, string value)
        {
            this.TrySetStringWithWarning(key, value);
        }

        public bool TrySetString(string key, string value)
        {
            string failureReason;
            return this.TrySetString(key, value, out failureReason);
        }

        public bool TrySetString(string key, string value, out string failureReason)
        {
            failureReason = null;
            if (this.state == null)
            {
                failureReason = "state store is unavailable";
                return false;
            }

            return this.state.TrySetRaw(key, value, out failureReason);
        }

        public void SetInt(string key, int value)
        {
            this.SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public bool TrySetInt(string key, int value)
        {
            string failureReason;
            return this.TrySetInt(key, value, out failureReason);
        }

        public bool TrySetInt(string key, int value, out string failureReason)
        {
            return this.TrySetString(
                key,
                value.ToString(CultureInfo.InvariantCulture),
                out failureReason);
        }

        public void SetFloat(string key, float value)
        {
            this.SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        }

        public bool TrySetFloat(string key, float value)
        {
            string failureReason;
            return this.TrySetFloat(key, value, out failureReason);
        }

        public bool TrySetFloat(string key, float value, out string failureReason)
        {
            return this.TrySetString(
                key,
                value.ToString("R", CultureInfo.InvariantCulture),
                out failureReason);
        }

        public void SetBool(string key, bool value)
        {
            this.SetString(key, value ? "true" : "false");
        }

        public bool TrySetBool(string key, bool value)
        {
            string failureReason;
            return this.TrySetBool(key, value, out failureReason);
        }

        public bool TrySetBool(string key, bool value, out string failureReason)
        {
            return this.TrySetString(key, value ? "true" : "false", out failureReason);
        }

        public void Remove(string key)
        {
            this.TryRemoveWithWarning(key);
        }

        public bool TryRemove(string key)
        {
            string failureReason;
            return this.TryRemove(key, out failureReason);
        }

        public bool TryRemove(string key, out string failureReason)
        {
            failureReason = null;
            if (this.state == null)
            {
                failureReason = "state store is unavailable";
                return false;
            }

            return this.state.TryRemove(key, out failureReason);
        }

        public void Clear()
        {
            this.TryClearUserValuesWithWarning();
        }

        public bool TryClearUserValues()
        {
            string failureReason;
            return this.TryClearUserValues(out failureReason);
        }

        public bool TryClearUserValues(out string failureReason)
        {
            failureReason = null;
            if (this.state == null)
            {
                failureReason = "state store is unavailable";
                return false;
            }

            this.state.ClearUserValues();
            return true;
        }

        private void TrySetStringWithWarning(string key, string value)
        {
            if (this.state == null)
            {
                return;
            }

            this.state.SetRaw(key, value);
        }

        private void TryRemoveWithWarning(string key)
        {
            if (this.state == null)
            {
                return;
            }

            this.state.Remove(key);
        }

        private void TryClearUserValuesWithWarning()
        {
            if (this.state == null)
            {
                return;
            }

            this.state.ClearUserValues();
        }
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Null object used when a context cannot expose real external state.
    /// Reads fail safely and writes are ignored.
    /// </summary>
    public sealed class NullShuttleExternalRuntimeStateStore :
        IShuttleExternalRuntimeStateTryStoreWithReasons
    {
        private static readonly NullShuttleExternalRuntimeStateStore instance =
            new NullShuttleExternalRuntimeStateStore();

        private NullShuttleExternalRuntimeStateStore()
        {
        }

        public static NullShuttleExternalRuntimeStateStore Instance
        {
            get
            {
                return instance;
            }
        }

        public int SchemaVersion
        {
            get
            {
                return 0;
            }
        }

        public bool TryGetString(string key, out string value)
        {
            value = null;
            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            return false;
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            return false;
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            return false;
        }

        public bool ContainsKey(string key)
        {
            return false;
        }

        public void SetString(string key, string value)
        {
        }

        public bool TrySetString(string key, string value)
        {
            string failureReason;
            return this.TrySetString(key, value, out failureReason);
        }

        public bool TrySetString(string key, string value, out string failureReason)
        {
            failureReason = "state store is unavailable";
            return false;
        }

        public void SetInt(string key, int value)
        {
        }

        public bool TrySetInt(string key, int value)
        {
            string failureReason;
            return this.TrySetInt(key, value, out failureReason);
        }

        public bool TrySetInt(string key, int value, out string failureReason)
        {
            failureReason = "state store is unavailable";
            return false;
        }

        public void SetFloat(string key, float value)
        {
        }

        public bool TrySetFloat(string key, float value)
        {
            string failureReason;
            return this.TrySetFloat(key, value, out failureReason);
        }

        public bool TrySetFloat(string key, float value, out string failureReason)
        {
            failureReason = "state store is unavailable";
            return false;
        }

        public void SetBool(string key, bool value)
        {
        }

        public bool TrySetBool(string key, bool value)
        {
            string failureReason;
            return this.TrySetBool(key, value, out failureReason);
        }

        public bool TrySetBool(string key, bool value, out string failureReason)
        {
            failureReason = "state store is unavailable";
            return false;
        }

        public void Remove(string key)
        {
        }

        public bool TryRemove(string key)
        {
            string failureReason;
            return this.TryRemove(key, out failureReason);
        }

        public bool TryRemove(string key, out string failureReason)
        {
            failureReason = "state store is unavailable";
            return false;
        }

        public void Clear()
        {
        }

        public bool TryClearUserValues()
        {
            string failureReason;
            return this.TryClearUserValues(out failureReason);
        }

        public bool TryClearUserValues(out string failureReason)
        {
            failureReason = "state store is unavailable";
            return false;
        }
    }
}

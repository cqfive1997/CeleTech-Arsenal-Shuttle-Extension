namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Writable key-value store for map-side runtime hooks and external command handlers.
    /// Values are persisted by the main mod as strings in the external state envelope.
    /// </summary>
    public interface IShuttleExternalRuntimeStateStore : IShuttleExternalRuntimeStateReader
    {
        /// <summary>
        /// Stores a user value. Keys reserved by the main mod, including keys starting with
        /// "__ct.", are rejected by the host implementation.
        /// </summary>
        void SetString(string key, string value);

        void SetInt(string key, int value);

        void SetFloat(string key, float value);

        void SetBool(string key, bool value);

        void Remove(string key);

        void Clear();
    }

    /// <summary>
    /// Optional result-returning state store surface for callers that want to handle
    /// rejected writes without relying on log output. Implementations reject reserved
    /// main-mod keys such as "__ct.*".
    /// </summary>
    public interface IShuttleExternalRuntimeStateTryStore : IShuttleExternalRuntimeStateStore
    {
        bool TrySetString(string key, string value);

        bool TrySetInt(string key, int value);

        bool TrySetFloat(string key, float value);

        bool TrySetBool(string key, bool value);

        bool TryRemove(string key);

        bool TryClearUserValues();
    }

    /// <summary>
    /// Optional result-returning state store surface for callers that need a concise
    /// rejection reason. This extends the bool-only try surface without breaking existing
    /// third-party implementations of IShuttleExternalRuntimeStateTryStore.
    /// </summary>
    public interface IShuttleExternalRuntimeStateTryStoreWithReasons :
        IShuttleExternalRuntimeStateTryStore
    {
        bool TrySetString(string key, string value, out string failureReason);

        bool TrySetInt(string key, int value, out string failureReason);

        bool TrySetFloat(string key, float value, out string failureReason);

        bool TrySetBool(string key, bool value, out string failureReason);

        bool TryRemove(string key, out string failureReason);

        bool TryClearUserValues(out string failureReason);
    }
}

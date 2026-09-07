namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Read-only key-value view over a main-mod-owned external runtime state envelope.
    /// Implementations parse primitive values without exposing the backing dictionary.
    /// </summary>
    public interface IShuttleExternalRuntimeStateReader
    {
        int SchemaVersion { get; }

        bool TryGetString(string key, out string value);

        bool TryGetInt(string key, out int value);

        bool TryGetFloat(string key, out float value);

        bool TryGetBool(string key, out bool value);

        bool ContainsKey(string key);
    }
}

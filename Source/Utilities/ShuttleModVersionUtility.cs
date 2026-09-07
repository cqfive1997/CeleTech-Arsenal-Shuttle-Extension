using System;
using System.Reflection;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal static class ShuttleModVersionUtility
    {
        internal static string ResolveCurrentModVersion()
        {
            string metadataVersion = TryGetModMetadataVersion();
            if (!string.IsNullOrEmpty(metadataVersion))
            {
                return metadataVersion;
            }

            try
            {
                Assembly assembly = typeof(ShuttleModVersionUtility).Assembly;
                AssemblyInformationalVersionAttribute attribute =
                    Attribute.GetCustomAttribute(
                        assembly,
                        typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                if (attribute != null)
                {
                    return NormalizeVersion(attribute.InformationalVersion);
                }
            }
            catch
            {
            }

            return null;
        }

        internal static string ResolveCurrentModVersionOrUnknown()
        {
            string version = ResolveCurrentModVersion();
            return !string.IsNullOrEmpty(version)
                ? version
                : "CT_Shuttle_Settings_Unknown".Translate().ToString();
        }

        internal static bool IsKnownVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            string trimmed = version.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            return !string.Equals(trimmed, "Unknown", StringComparison.OrdinalIgnoreCase) &&
                trimmed != "CT_Shuttle_Settings_Unknown".Translate().ToString();
        }

        private static string TryGetModMetadataVersion()
        {
            try
            {
                if (LoadedModManager.RunningModsListForReading == null)
                {
                    return null;
                }

                for (int i = 0; i < LoadedModManager.RunningModsListForReading.Count; i++)
                {
                    ModContentPack mod = LoadedModManager.RunningModsListForReading[i];
                    if (mod == null ||
                        !string.Equals(mod.PackageId, ShuttleModConstants.PackageId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ModMetaData metaData = mod.ModMetaData;
                    string version = GetStringMember(metaData, "ModVersion") ??
                        GetStringMember(metaData, "modVersion") ??
                        GetStringMember(metaData, "Version") ??
                        GetStringMember(metaData, "version");
                    return NormalizeVersion(version);
                }
            }
            catch
            {
            }

            return null;
        }

        private static string GetStringMember(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                PropertyInfo property = instance.GetType().GetProperty(memberName, flags);
                if (property != null)
                {
                    object value = property.GetValue(instance, null);
                    return value != null ? value.ToString() : null;
                }

                FieldInfo field = instance.GetType().GetField(memberName, flags);
                if (field != null)
                {
                    object value = field.GetValue(instance);
                    return value != null ? value.ToString() : null;
                }
            }
            catch
            {
            }

            return null;
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return null;
            }

            string trimmed = version.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}

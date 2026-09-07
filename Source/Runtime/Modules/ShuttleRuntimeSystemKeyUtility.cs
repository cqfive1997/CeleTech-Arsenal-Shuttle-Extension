using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal static class ShuttleRuntimeSystemKeyUtility
    {
        internal const string PackageId = "celetech.shuttle";

        internal const string Scanner = PackageId + "/scanner";
        internal const string Shield = PackageId + "/shield";
        internal const string SurfaceShield = PackageId + "/surface-shield";
        internal const string Weapon = PackageId + "/weapon";
        internal const string Habitat = PackageId + "/habitat";
        internal const string AutoWorkTable = PackageId + "/auto-worktable";
        internal const string RefrigeratedCargo = PackageId + "/refrigerated-cargo";

        internal static string Normalize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            string trimmed = key.Trim();
            string firstParty = NormalizeFirstPartyLegacyKey(trimmed);
            return firstParty ?? trimmed;
        }

        internal static bool IsNamespaced(string key)
        {
            return !string.IsNullOrEmpty(key) && key.IndexOf('/') >= 0;
        }

        internal static string LocalPartOrSelf(string key)
        {
            string normalized = Normalize(key);
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            int separatorIndex = normalized.LastIndexOf('/');
            return separatorIndex >= 0 && separatorIndex < normalized.Length - 1
                ? normalized.Substring(separatorIndex + 1)
                : normalized;
        }

        private static string NormalizeFirstPartyLegacyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (string.Equals(key, Scanner, StringComparison.Ordinal) ||
                string.Equals(key, Shield, StringComparison.Ordinal) ||
                string.Equals(key, SurfaceShield, StringComparison.Ordinal) ||
                string.Equals(key, Weapon, StringComparison.Ordinal) ||
                string.Equals(key, Habitat, StringComparison.Ordinal) ||
                string.Equals(key, AutoWorkTable, StringComparison.Ordinal) ||
                string.Equals(key, RefrigeratedCargo, StringComparison.Ordinal))
            {
                return key;
            }

            if (string.Equals(key, "celetech.scanner", StringComparison.Ordinal))
            {
                return Scanner;
            }

            if (string.Equals(key, "celetech.shield", StringComparison.Ordinal))
            {
                return Shield;
            }

            if (string.Equals(key, "celetech.surface_shield", StringComparison.Ordinal) ||
                string.Equals(key, "celetech.surface-shield", StringComparison.Ordinal))
            {
                return SurfaceShield;
            }

            if (string.Equals(key, "weapon", StringComparison.Ordinal))
            {
                return Weapon;
            }

            if (string.Equals(key, "celetech.habitat", StringComparison.Ordinal) ||
                string.Equals(key, "habitat", StringComparison.Ordinal))
            {
                return Habitat;
            }

            if (string.Equals(key, "auto-worktable", StringComparison.Ordinal))
            {
                return AutoWorkTable;
            }

            if (string.Equals(key, "celetech.refrigerated-cargo", StringComparison.Ordinal) ||
                string.Equals(key, "refrigerated-cargo", StringComparison.Ordinal))
            {
                return RefrigeratedCargo;
            }

            return null;
        }
    }
}

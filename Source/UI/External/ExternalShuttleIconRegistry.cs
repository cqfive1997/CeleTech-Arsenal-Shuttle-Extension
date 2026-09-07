using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal static class ExternalShuttleIconRegistry
    {
        private const string LogPrefix = "[CeleTech ShuttleExtension] UI ";

        private static readonly Dictionary<string, string> texPaths =
            new Dictionary<string, string>(System.StringComparer.Ordinal);

        private static readonly HashSet<string> nonNamespacedWarnings =
            new HashSet<string>(System.StringComparer.Ordinal);

        private static int revision;

        internal static int Revision
        {
            get
            {
                return revision;
            }
        }

        internal static bool RegisterIcon(
            string ownerPackageId,
            string localIconKey,
            string texPath)
        {
            string owner = NormalizeKey(ownerPackageId);
            string local = NormalizeKey(localIconKey);
            string path = NormalizeTexturePath(texPath);
            if (owner == null)
            {
                Log.Warning(LogPrefix + "icon registration failed: ownerPackageId is empty.");
                return false;
            }

            if (local == null)
            {
                Log.Warning(LogPrefix + "icon registration failed for owner " + owner +
                    ": localIconKey is empty.");
                return false;
            }

            if (path == null)
            {
                Log.Warning(LogPrefix + "icon registration failed for " + owner + "/" + local +
                    ": texPath is empty or invalid.");
                return false;
            }

            string fullKey = owner + "/" + local;
            WarnIfKeyIsNotNamespaced(fullKey);
            return RegisterResolvedPath(fullKey, path, "icon");
        }

        internal static bool RegisterModuleIcon(
            string ownerPackageId,
            string moduleDefName,
            string texPath)
        {
            string owner = NormalizeKey(ownerPackageId);
            string moduleKey = NormalizeKey(moduleDefName);
            string path = NormalizeTexturePath(texPath);
            if (owner == null)
            {
                Log.Warning(LogPrefix + "module icon registration failed: ownerPackageId is empty.");
                return false;
            }

            if (moduleKey == null)
            {
                Log.Warning(LogPrefix + "module icon registration failed for owner " + owner +
                    ": moduleDefName is empty.");
                return false;
            }

            if (path == null)
            {
                Log.Warning(LogPrefix + "module icon registration failed for " + moduleKey +
                    ": texPath is empty or invalid.");
                return false;
            }

            return RegisterResolvedPath(moduleKey, path, "module icon");
        }

        internal static bool TryResolveTexPath(string iconKey, out string texPath)
        {
            texPath = null;
            string key = NormalizeKey(iconKey);
            return key != null && texPaths.TryGetValue(key, out texPath);
        }

        private static bool RegisterResolvedPath(string key, string texPath, string kind)
        {
            if (texPaths.ContainsKey(key))
            {
                Log.Warning(LogPrefix + kind + " duplicate key ignored: " + key +
                    ". First registration wins.");
                return false;
            }

            texPaths.Add(key, texPath);
            unchecked
            {
                revision++;
            }

            return true;
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return key.Trim();
        }

        private static string NormalizeTexturePath(string texPath)
        {
            if (string.IsNullOrWhiteSpace(texPath))
            {
                return null;
            }

            string path = texPath.Trim().Replace('\\', '/');
            while (path.StartsWith("Textures/", System.StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("Textures/".Length);
            }

            if (path.StartsWith("/", System.StringComparison.Ordinal) ||
                path.Contains(":") ||
                path.Contains(".."))
            {
                return null;
            }

            if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".dds", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase))
            {
                int dotIndex = path.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    path = path.Substring(0, dotIndex);
                }
            }

            return string.IsNullOrWhiteSpace(path) ? null : path;
        }

        private static void WarnIfKeyIsNotNamespaced(string key)
        {
            if (!Prefs.DevMode ||
                string.IsNullOrEmpty(key) ||
                ShuttleRuntimeSystemKeyUtility.IsNamespaced(key))
            {
                return;
            }

            if (nonNamespacedWarnings.Add(key))
            {
                Log.WarningOnce(
                    LogPrefix + "icon key is not namespaced. Recommended format is package.id/local-key. Key: " +
                    key,
                    StableStringHash(LogPrefix + "|non-namespaced-icon|" + key));
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 37;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }
    }
}

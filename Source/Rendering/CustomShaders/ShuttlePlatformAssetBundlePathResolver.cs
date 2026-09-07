using System;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders
{
    internal enum ShuttleAssetBundlePlatform
    {
        Unknown = 0,
        Windows = 1,
        MacOS = 2,
        Linux = 3
    }

    internal struct ShuttleAssetBundlePathSelection
    {
        internal ShuttleAssetBundlePathSelection(
            bool found,
            string selectedRelativePath,
            string[] checkedRelativePaths)
        {
            this.Found = found;
            this.SelectedRelativePath = selectedRelativePath;
            this.CheckedRelativePaths = checkedRelativePaths ?? new string[0];
        }

        internal bool Found { get; private set; }

        internal string SelectedRelativePath { get; private set; }

        internal string[] CheckedRelativePaths { get; private set; }
    }

    internal static class ShuttlePlatformAssetBundlePathResolver
    {
        internal static string[] GetCandidateRelativePaths(
            ShuttleAssetBundlePlatform platform,
            IEnumerable<string> legacyRelativePaths)
        {
            if (platform == ShuttleAssetBundlePlatform.Unknown || legacyRelativePaths == null)
            {
                return new string[0];
            }

            List<string> legacy = new List<string>();
            foreach (string relativePath in legacyRelativePaths)
            {
                string normalized = NormalizeRelativePath(relativePath);
                if (!string.IsNullOrEmpty(normalized))
                {
                    legacy.Add(normalized);
                }
            }

            List<string> candidates = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string platformDirectory = GetPlatformDirectory(platform);
            for (int i = 0; i < legacy.Count; i++)
            {
                string platformPath = InsertPlatformDirectory(
                    legacy[i],
                    platformDirectory);
                AddUnique(candidates, seen, platformPath);
            }

            if (platform == ShuttleAssetBundlePlatform.Windows)
            {
                for (int i = 0; i < legacy.Count; i++)
                {
                    AddUnique(candidates, seen, legacy[i]);
                }
            }

            return candidates.ToArray();
        }

        internal static ShuttleAssetBundlePathSelection SelectFirstExisting(
            ShuttleAssetBundlePlatform platform,
            IEnumerable<string> legacyRelativePaths,
            Func<string, bool> relativePathExists)
        {
            string[] candidates = GetCandidateRelativePaths(platform, legacyRelativePaths);
            if (relativePathExists == null)
            {
                return new ShuttleAssetBundlePathSelection(false, null, candidates);
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (relativePathExists(candidates[i]))
                {
                    return new ShuttleAssetBundlePathSelection(true, candidates[i], candidates);
                }
            }

            return new ShuttleAssetBundlePathSelection(false, null, candidates);
        }

        internal static string GetPlatformDirectory(ShuttleAssetBundlePlatform platform)
        {
            switch (platform)
            {
                case ShuttleAssetBundlePlatform.Windows:
                    return "windows";
                case ShuttleAssetBundlePlatform.MacOS:
                    return "macos";
                case ShuttleAssetBundlePlatform.Linux:
                    return "linux";
                default:
                    return null;
            }
        }

        private static string NormalizeRelativePath(string relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath) ?
                null :
                relativePath.Trim().Replace('\\', '/').TrimStart('/');
        }

        private static string InsertPlatformDirectory(
            string legacyRelativePath,
            string platformDirectory)
        {
            if (string.IsNullOrEmpty(legacyRelativePath) ||
                string.IsNullOrEmpty(platformDirectory))
            {
                return null;
            }

            int separatorIndex = legacyRelativePath.LastIndexOf('/');
            if (separatorIndex < 0)
            {
                return platformDirectory + "/" + legacyRelativePath;
            }

            return legacyRelativePath.Substring(0, separatorIndex + 1) +
                platformDirectory +
                "/" +
                legacyRelativePath.Substring(separatorIndex + 1);
        }

        private static void AddUnique(
            List<string> candidates,
            HashSet<string> seen,
            string candidate)
        {
            if (!string.IsNullOrEmpty(candidate) && seen.Add(candidate))
            {
                candidates.Add(candidate);
            }
        }
    }
}

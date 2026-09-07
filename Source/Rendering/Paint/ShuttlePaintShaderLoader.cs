using System;
using System.Collections.Generic;
using System.IO;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    [StaticConstructorOnStartup]
    internal static class ShuttlePaintShaderLoader
    {
        internal const string PaintTemplateMaterialAssetName = "CT_ShuttlePaintOverlay";

        private const string LogCategory = "ShuttlePaintOverlay";
        private const int RetryIntervalTicks = 2500;

        private static bool loadAttempted;
        private static int nextRetryTick;
        private static AssetBundle assetBundle;
        private static Material paintTemplateMaterial;
        private static string modRootPath;
        private static string selectedBundleRelativePath;

        static ShuttlePaintShaderLoader()
        {
        }

        internal static string SelectedBundleRelativePath
        {
            get { return selectedBundleRelativePath; }
        }

        internal static bool TryLoadTemplateMaterial(out Material templateMaterial)
        {
            templateMaterial = paintTemplateMaterial;
            if (templateMaterial != null)
            {
                return true;
            }

            int ticks = GetCurrentTick();
            if (loadAttempted && ticks < nextRetryTick)
            {
                return false;
            }

            loadAttempted = true;
            nextRetryTick = ticks + RetryIntervalTicks;

            if (assetBundle != null)
            {
                paintTemplateMaterial = TryLoadTemplateMaterialFromBundle();
                templateMaterial = paintTemplateMaterial;
                return templateMaterial != null;
            }

            string resolvedBundlePath = ResolveBundlePath();
            if (string.IsNullOrEmpty(resolvedBundlePath))
            {
                return false;
            }

            try
            {
                assetBundle = AssetBundle.LoadFromFile(resolvedBundlePath);
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-load-failed|" + resolvedBundlePath,
                    "Paint AssetBundle LoadFromFile failed. path=" +
                    resolvedBundlePath +
                    ", reason=" +
                    exception.Message);
                return false;
            }

            if (assetBundle == null)
            {
                WarnOnce(
                    "bundle-load-null|" + resolvedBundlePath,
                    "Paint AssetBundle LoadFromFile returned null. path=" + resolvedBundlePath);
                return false;
            }

            paintTemplateMaterial = TryLoadTemplateMaterialFromBundle();
            templateMaterial = paintTemplateMaterial;
            return templateMaterial != null;
        }

        private static string ResolveBundlePath()
        {
            string root = ResolveModRootPath();
            if (string.IsNullOrEmpty(root))
            {
                return null;
            }

            try
            {
                string fullRoot = Path.GetFullPath(root);
                List<string> resolvedCandidates = new List<string>();
                List<string> relativeCandidates =
                    new List<string>(GetCandidateBundleRelativePaths());
                foreach (string relativePath in relativeCandidates)
                {
                    string fullResolved = TryResolveCandidateBundlePath(fullRoot, relativePath);
                    if (string.IsNullOrEmpty(fullResolved))
                    {
                        continue;
                    }

                    resolvedCandidates.Add(fullResolved);
                    if (File.Exists(fullResolved))
                    {
                        selectedBundleRelativePath = relativePath;
                        ShuttleCustomShaderDiagnostics.LogBundleSelection(
                            "paint",
                            relativePath,
                            true,
                            "candidate-found");
                        return fullResolved;
                    }
                }

                string firstCandidate = relativeCandidates.Count > 0 ?
                    relativeCandidates[0] :
                    null;
                ShuttleCustomShaderDiagnostics.LogBundleSelection(
                    "paint",
                    firstCandidate,
                    false,
                    "file-not-found");
                WarnOnce(
                    "bundle-missing|shuttlepaint",
                    "Paint AssetBundle is missing. checkedPaths=" +
                    JoinOrNone(resolvedCandidates.ToArray()));
                return null;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-path-resolve-failed|shuttlepaint",
                    "Paint AssetBundle candidate paths could not be resolved. reason=" +
                    exception.Message);
                return null;
            }
        }

        private static string TryResolveCandidateBundlePath(string fullRoot, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            string normalizedRelativePath = relativePath.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalizedRelativePath))
            {
                WarnOnce(
                    "bundle-path-rooted|" + normalizedRelativePath,
                    "Paint AssetBundle candidate path is absolute and was rejected. relativePath=" +
                    normalizedRelativePath);
                return null;
            }

            if (normalizedRelativePath.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                WarnOnce(
                    "bundle-path-parent|" + normalizedRelativePath,
                    "Paint AssetBundle candidate path contains parent-directory traversal and was rejected. relativePath=" +
                    normalizedRelativePath);
                return null;
            }

            try
            {
                string fullResolved = Path.GetFullPath(Path.Combine(fullRoot, normalizedRelativePath));
                if (!IsInsideDirectory(fullRoot, fullResolved))
                {
                    WarnOnce(
                        "bundle-outside-root|" + fullResolved,
                        "Paint AssetBundle path resolved outside the mod root and was rejected. path=" +
                        fullResolved);
                    return null;
                }

                return fullResolved;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-candidate-resolve-failed|" + normalizedRelativePath,
                    "Paint AssetBundle candidate path could not be resolved. relativePath=" +
                    normalizedRelativePath +
                    ", reason=" +
                    exception.Message);
                return null;
            }
        }

        private static IEnumerable<string> GetCandidateBundleRelativePaths()
        {
            string[] candidates = ShuttlePlatformAssetBundlePathResolver.GetCandidateRelativePaths(
                ShuttlePlatformAssetBundleEnvironment.CurrentPlatform,
                new[]
                {
                    "1.6/AssetBundles/shuttlepaint",
                    "AssetBundles/shuttlepaint",
                    "Assets/AssetBundles/shuttlepaint"
                });
            for (int i = 0; i < candidates.Length; i++)
            {
                yield return candidates[i];
            }
        }

        private static Material TryLoadTemplateMaterialFromBundle()
        {
            if (assetBundle == null)
            {
                return null;
            }

            Material material = null;
            try
            {
                material = assetBundle.LoadAsset<Material>(PaintTemplateMaterialAssetName);
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "template-load-failed|" + PaintTemplateMaterialAssetName,
                    "Paint template material LoadAsset failed. assetName=" +
                    PaintTemplateMaterialAssetName +
                    ", reason=" +
                    exception.Message);
            }

            if (material != null)
            {
                return material;
            }

            string[] assetNames = GetBundleAssetNames();
            material = TryLoadTemplateMaterialByAssetName(assetNames);
            if (material != null)
            {
                return material;
            }

            WarnOnce(
                "template-missing|" + PaintTemplateMaterialAssetName,
                "Paint template material is missing from AssetBundle. assetName=" +
                PaintTemplateMaterialAssetName +
                ", bundleAssets=" +
                JoinOrNone(assetNames));
            return null;
        }

        private static Material TryLoadTemplateMaterialByAssetName(string[] assetNames)
        {
            if (assetNames == null)
            {
                return null;
            }

            for (int i = 0; i < assetNames.Length; i++)
            {
                string assetName = assetNames[i];
                if (string.IsNullOrEmpty(assetName))
                {
                    continue;
                }

                string normalized = assetName.Replace('\\', '/');
                if (!normalized.EndsWith(
                        "/ct_shuttlepaintoverlay.mat",
                        StringComparison.OrdinalIgnoreCase) &&
                    !normalized.EndsWith(
                        "ct_shuttlepaintoverlay.mat",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    return assetBundle.LoadAsset<Material>(assetName);
                }
                catch (Exception exception)
                {
                    WarnOnce(
                        "template-fallback-load-failed|" + assetName,
                        "Paint template material fallback LoadAsset failed. assetName=" +
                        assetName +
                        ", reason=" +
                        exception.Message);
                }
            }

            return null;
        }

        private static string[] GetBundleAssetNames()
        {
            if (assetBundle == null)
            {
                return new string[0];
            }

            try
            {
                return assetBundle.GetAllAssetNames();
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-asset-names-failed",
                    "Paint AssetBundle GetAllAssetNames failed. reason=" + exception.Message);
                return new string[0];
            }
        }

        private static string ResolveModRootPath()
        {
            if (!string.IsNullOrEmpty(modRootPath))
            {
                return modRootPath;
            }

            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            if (mods != null)
            {
                for (int i = 0; i < mods.Count; i++)
                {
                    ModContentPack mod = mods[i];
                    if (mod == null ||
                        !string.Equals(
                            mod.PackageId,
                            ShuttleModConstants.PackageId,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(mod.RootDir))
                    {
                        continue;
                    }

                    try
                    {
                        modRootPath = Path.GetFullPath(mod.RootDir);
                        return modRootPath;
                    }
                    catch (Exception exception)
                    {
                        WarnOnce(
                            "mod-root-normalize-failed|" + mod.RootDir,
                            "Paint AssetBundle mod root could not be normalized. packageId=" +
                            ShuttleModConstants.PackageId +
                            ", root=" +
                            mod.RootDir +
                            ", reason=" +
                            exception.Message);
                        return null;
                    }
                }
            }

            WarnOnce(
                "mod-root-unresolved|" + ShuttleModConstants.PackageId,
                "Paint AssetBundle mod root could not be resolved. packageId=" +
                ShuttleModConstants.PackageId);
            return null;
        }

        private static bool IsInsideDirectory(string directoryPath, string candidatePath)
        {
            if (string.IsNullOrEmpty(directoryPath) || string.IsNullOrEmpty(candidatePath))
            {
                return false;
            }

            string normalizedDirectory = Path.GetFullPath(directoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string normalizedCandidate = Path.GetFullPath(candidatePath);
            return normalizedCandidate.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetCurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        private static string JoinOrNone(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return "<none>";
            }

            return string.Join(", ", values);
        }

        private static void WarnOnce(string key, string message)
        {
            ShuttleLog.WarnOnce(LogCategory, key, message);
        }
    }
}

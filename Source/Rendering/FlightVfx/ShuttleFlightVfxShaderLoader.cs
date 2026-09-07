using System;
using System.Collections.Generic;
using System.IO;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    [StaticConstructorOnStartup]
    internal static class ShuttleFlightVfxShaderLoader
    {
        internal const string BundleName = "shuttleflame";
        internal const string GlowTemplateMaterialAssetName = "CT_ShuttleFlameGlow";
        internal const string CoreTemplateMaterialAssetName = "CT_ShuttleFlameCore";

        private const string LogCategory = "ShuttleFlightVfxFlame";
        private const int RetryIntervalTicks = 2500;

        private static bool loadAttempted;
        private static int nextRetryTick;
        private static AssetBundle assetBundle;
        private static Material glowTemplateMaterial;
        private static Material coreTemplateMaterial;
        private static string modRootPath;
        private static string selectedBundleRelativePath;

        static ShuttleFlightVfxShaderLoader()
        {
        }

        internal static string SelectedBundleRelativePath
        {
            get { return selectedBundleRelativePath; }
        }

        internal static bool TryLoadTemplateMaterials(
            out Material glowTemplate,
            out Material coreTemplate)
        {
            glowTemplate = glowTemplateMaterial;
            coreTemplate = coreTemplateMaterial;
            if (glowTemplate != null && coreTemplate != null)
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

            if (!EnsureBundleLoaded())
            {
                return false;
            }

            glowTemplateMaterial = TryLoadTemplateMaterial(
                GlowTemplateMaterialAssetName,
                "ct_shuttleflameglow.mat");
            coreTemplateMaterial = TryLoadTemplateMaterial(
                CoreTemplateMaterialAssetName,
                "ct_shuttleflamecore.mat");

            glowTemplateMaterial = ValidateTemplateOrNull(
                glowTemplateMaterial,
                GlowTemplateMaterialAssetName);
            coreTemplateMaterial = ValidateTemplateOrNull(
                coreTemplateMaterial,
                CoreTemplateMaterialAssetName);

            glowTemplate = glowTemplateMaterial;
            coreTemplate = coreTemplateMaterial;
            return glowTemplate != null && coreTemplate != null;
        }

        private static bool EnsureBundleLoaded()
        {
            if (assetBundle != null)
            {
                return true;
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
                    "Flame AssetBundle LoadFromFile failed. path=" +
                    resolvedBundlePath +
                    ", reason=" +
                    exception.Message);
                return false;
            }

            if (assetBundle == null)
            {
                WarnOnce(
                    "bundle-load-null|" + resolvedBundlePath,
                    "Flame AssetBundle LoadFromFile returned null. path=" + resolvedBundlePath);
                return false;
            }

            return true;
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
                            "flame",
                            relativePath,
                            true,
                            "candidate-found");
                        return fullResolved;
                    }
                }

                ShuttleCustomShaderDiagnostics.LogBundleSelection(
                    "flame",
                    relativeCandidates.Count > 0 ? relativeCandidates[0] : null,
                    false,
                    relativeCandidates.Count > 0 ? "file-not-found" : "unsupported-platform");
                WarnOnce(
                    "bundle-missing|" + BundleName,
                    "Flame AssetBundle is missing. checkedPaths=" +
                    JoinOrNone(resolvedCandidates.ToArray()));
                return null;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-path-resolve-failed|" + BundleName,
                    "Flame AssetBundle candidate paths could not be resolved. reason=" +
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
                    "Flame AssetBundle candidate path is absolute and was rejected. relativePath=" +
                    normalizedRelativePath);
                return null;
            }

            if (normalizedRelativePath.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                WarnOnce(
                    "bundle-path-parent|" + normalizedRelativePath,
                    "Flame AssetBundle candidate path contains parent-directory traversal and was rejected. relativePath=" +
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
                        "Flame AssetBundle path resolved outside the mod root and was rejected. path=" +
                        fullResolved);
                    return null;
                }

                return fullResolved;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-candidate-resolve-failed|" + normalizedRelativePath,
                    "Flame AssetBundle candidate path could not be resolved. relativePath=" +
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
                    "1.6/AssetBundles/" + BundleName,
                    "AssetBundles/" + BundleName,
                    "Assets/AssetBundles/" + BundleName
                });
            for (int i = 0; i < candidates.Length; i++)
            {
                yield return candidates[i];
            }
        }

        private static Material ValidateTemplateOrNull(
            Material material,
            string materialAssetName)
        {
            CustomShaderMaterialValidationResult validation =
                UnityCustomShaderMaterialValidator.Validate(
                    material,
                    ShuttleCustomShaderMaterialContracts.Flame);
            ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                "flame",
                selectedBundleRelativePath,
                materialAssetName,
                material,
                validation,
                validation.IsValid ? "custom" : "fallback");
            return validation.IsValid ? material : null;
        }

        private static Material TryLoadTemplateMaterial(string assetName, string fallbackFileName)
        {
            if (assetBundle == null)
            {
                return null;
            }

            Material material = null;
            try
            {
                material = assetBundle.LoadAsset<Material>(assetName);
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "template-load-failed|" + assetName,
                    "Flame template material LoadAsset failed. assetName=" +
                    assetName +
                    ", reason=" +
                    exception.Message);
            }

            if (material != null)
            {
                return material;
            }

            string[] assetNames = GetBundleAssetNames();
            material = TryLoadTemplateMaterialByAssetName(assetNames, fallbackFileName);
            if (material != null)
            {
                return material;
            }

            WarnOnce(
                "template-missing|" + assetName,
                "Flame template material is missing from AssetBundle. assetName=" +
                assetName +
                ", bundleAssets=" +
                JoinOrNone(assetNames));
            return null;
        }

        private static Material TryLoadTemplateMaterialByAssetName(
            string[] assetNames,
            string fallbackFileName)
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
                        "/" + fallbackFileName,
                        StringComparison.OrdinalIgnoreCase) &&
                    !normalized.EndsWith(
                        fallbackFileName,
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
                        "Flame template material fallback LoadAsset failed. assetName=" +
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
                    "Flame AssetBundle GetAllAssetNames failed. reason=" + exception.Message);
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
                            "Flame AssetBundle mod root could not be normalized. packageId=" +
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
                "Flame AssetBundle mod root could not be resolved. packageId=" +
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

using System;
using System.Collections.Generic;
using System.IO;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    [StaticConstructorOnStartup]
    internal static class ShuttleFlightVfxVaporBundleLoader
    {
        private const string LogCategory = "ShuttleFlightVfxVapor";
        private const int RetryIntervalTicks = 2500;

        private static bool loadAttempted;
        private static int nextRetryTick;
        private static AssetBundle assetBundle;
        private static Material templateMaterial;
        private static string modRootPath;
        private static string lastResolvedBundlePath;
        private static string selectedBundleRelativePath;

        static ShuttleFlightVfxVaporBundleLoader()
        {
        }

        internal static string SelectedBundleRelativePath
        {
            get { return selectedBundleRelativePath; }
        }

        internal static bool TryLoadTemplateMaterial(
            out Material vaporTemplate,
            out ShuttleFlightVfxVaporAvailability status)
        {
            vaporTemplate = templateMaterial;
            if (vaporTemplate != null)
            {
                status = ShuttleFlightVfxVaporAvailability.Available;
                return true;
            }

            int ticks = GetCurrentTick();
            if (loadAttempted && ticks < nextRetryTick)
            {
                status = assetBundle == null ?
                    ShuttleFlightVfxVaporAvailability.MissingBundle :
                    ShuttleFlightVfxVaporAvailability.MissingMaterial;
                return false;
            }

            loadAttempted = true;
            nextRetryTick = ticks + RetryIntervalTicks;

            if (!EnsureBundleLoaded())
            {
                status = ShuttleFlightVfxVaporAvailability.MissingBundle;
                return false;
            }

            Material loadedMaterial = TryLoadTemplateMaterial(
                ShuttleFlightVfxVaporBundleContract.MaterialName,
                ShuttleFlightVfxVaporBundleContract.MaterialFileName);
            CustomShaderMaterialValidationResult validation =
                UnityCustomShaderMaterialValidator.Validate(
                    loadedMaterial,
                    ShuttleCustomShaderMaterialContracts.Vapor);
            ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                "vapor",
                selectedBundleRelativePath,
                ShuttleFlightVfxVaporBundleContract.MaterialName,
                loadedMaterial,
                validation,
                validation.IsValid ? "custom" : "skip");
            templateMaterial = validation.IsValid ? loadedMaterial : null;

            vaporTemplate = templateMaterial;
            status = vaporTemplate != null ?
                ShuttleFlightVfxVaporAvailability.Available :
                ShuttleFlightVfxVaporAvailability.MissingMaterial;
            return vaporTemplate != null;
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
                    "Vapor AssetBundle LoadFromFile failed. availability=" +
                    ShuttleFlightVfxVaporAvailability.MissingBundle +
                    " bundlePath=" +
                    resolvedBundlePath +
                    " missingReason=load-exception reason=" +
                    exception.Message);
                return false;
            }

            if (assetBundle == null)
            {
                WarnOnce(
                    "bundle-load-null|" + resolvedBundlePath,
                    "Vapor AssetBundle LoadFromFile returned null. availability=" +
                    ShuttleFlightVfxVaporAvailability.MissingBundle +
                    " bundlePath=" +
                    resolvedBundlePath +
                    " missingReason=load-returned-null");
                return false;
            }

            lastResolvedBundlePath = resolvedBundlePath;
            LogDiagnosticOnce(
                "bundle-load-success|" + resolvedBundlePath,
                "Vapor AssetBundle load success. availability=" +
                ShuttleFlightVfxVaporAvailability.Available +
                " bundleName=" +
                ShuttleFlightVfxVaporBundleContract.BundleName +
                " bundlePath=" +
                resolvedBundlePath +
                " missingReason=none");
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
                            "vapor",
                            relativePath,
                            true,
                            "candidate-found");
                        return fullResolved;
                    }
                }

                ShuttleCustomShaderDiagnostics.LogBundleSelection(
                    "vapor",
                    relativeCandidates.Count > 0 ? relativeCandidates[0] : null,
                    false,
                    relativeCandidates.Count > 0 ? "file-not-found" : "unsupported-platform");
                WarnOnce(
                    "bundle-missing|" + ShuttleFlightVfxVaporBundleContract.BundleName,
                    "Vapor AssetBundle is missing. availability=" +
                    ShuttleFlightVfxVaporAvailability.MissingBundle +
                    " bundleName=" +
                    ShuttleFlightVfxVaporBundleContract.BundleName +
                    " bundlePath=<none>" +
                    " missingReason=file-not-found checkedPaths=" +
                    JoinOrNone(resolvedCandidates.ToArray()));
                return null;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-path-resolve-failed|" + ShuttleFlightVfxVaporBundleContract.BundleName,
                    "Vapor AssetBundle candidate paths could not be resolved. availability=" +
                    ShuttleFlightVfxVaporAvailability.MissingBundle +
                    " bundleName=" +
                    ShuttleFlightVfxVaporBundleContract.BundleName +
                    " bundlePath=<unresolved>" +
                    " missingReason=path-resolve-failed reason=" +
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
                    "Vapor AssetBundle candidate path is absolute and was rejected. relativePath=" +
                    normalizedRelativePath);
                return null;
            }

            if (normalizedRelativePath.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                WarnOnce(
                    "bundle-path-parent|" + normalizedRelativePath,
                    "Vapor AssetBundle candidate path contains parent-directory traversal and was rejected. relativePath=" +
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
                        "Vapor AssetBundle path resolved outside the mod root and was rejected. path=" +
                        fullResolved);
                    return null;
                }

                return fullResolved;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "bundle-candidate-resolve-failed|" + normalizedRelativePath,
                    "Vapor AssetBundle candidate path could not be resolved. relativePath=" +
                    normalizedRelativePath +
                    ", reason=" +
                    exception.Message);
                return null;
            }
        }

        private static IEnumerable<string> GetCandidateBundleRelativePaths()
        {
            string bundleName = ShuttleFlightVfxVaporBundleContract.BundleName;
            string[] candidates = ShuttlePlatformAssetBundlePathResolver.GetCandidateRelativePaths(
                ShuttlePlatformAssetBundleEnvironment.CurrentPlatform,
                new[]
                {
                    "1.6/AssetBundles/" + bundleName,
                    "AssetBundles/" + bundleName,
                    "Assets/AssetBundles/" + bundleName
                });
            for (int i = 0; i < candidates.Length; i++)
            {
                yield return candidates[i];
            }
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
                    "Vapor template material LoadAsset failed. availability=" +
                    ShuttleFlightVfxVaporAvailability.MissingMaterial +
                    " bundlePath=" +
                    (lastResolvedBundlePath ?? "<unknown>") +
                    " requestedAssetName=" +
                    assetName +
                    " missingReason=material-load-exception reason=" +
                    exception.Message);
            }

            if (material != null)
            {
                LogTemplateLoadedOnce("direct|" + assetName, material, "direct", assetName);
                return material;
            }

            string[] assetNames = GetBundleAssetNames();
            string loadedAssetName;
            material = TryLoadTemplateMaterialByAssetName(assetNames, fallbackFileName, out loadedAssetName);
            if (material != null)
            {
                LogTemplateLoadedOnce("fallback|" + loadedAssetName, material, "fallback", loadedAssetName);
                return material;
            }

            WarnOnce(
                "template-missing|" + assetName,
                "Vapor template material is missing from AssetBundle. availability=" +
                ShuttleFlightVfxVaporAvailability.MissingMaterial +
                " bundlePath=" +
                (lastResolvedBundlePath ?? "<unknown>") +
                " requestedAssetName=" +
                assetName +
                " missingReason=material-not-found bundleAssets=" +
                JoinOrNone(assetNames));
            return null;
        }

        private static Material TryLoadTemplateMaterialByAssetName(
            string[] assetNames,
            string fallbackFileName,
            out string loadedAssetName)
        {
            loadedAssetName = null;
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
                    loadedAssetName = assetName;
                    return assetBundle.LoadAsset<Material>(assetName);
                }
                catch (Exception exception)
                {
                    WarnOnce(
                        "template-fallback-load-failed|" + assetName,
                        "Vapor template material fallback LoadAsset failed. assetName=" +
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
                    "Vapor AssetBundle GetAllAssetNames failed. reason=" + exception.Message);
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
                            "Vapor AssetBundle mod root could not be normalized. packageId=" +
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
                "Vapor AssetBundle mod root could not be resolved. packageId=" +
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

        private static void LogDiagnosticOnce(string key, string message)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogAvailability)
            {
                return;
            }

            ShuttleLog.WarnOnce(LogCategory, "diagnostic|" + key, message);
        }

        private static void LogTemplateLoadedOnce(
            string key,
            Material material,
            string loadPath,
            string loadedAssetName)
        {
            LogDiagnosticOnce(
                "template-load-success|" + key,
                "Vapor template material load success. availability=" +
                ShuttleFlightVfxVaporAvailability.Available +
                " bundlePath=" +
                (lastResolvedBundlePath ?? "<unknown>") +
                " requestedAssetName=" +
                ShuttleFlightVfxVaporBundleContract.MaterialName +
                " loadedAssetName=" +
                (loadedAssetName ?? "<unknown>") +
                " loadedBy=" +
                loadPath +
                " templateMaterial=" +
                MaterialName(material) +
                " templateShader=" +
                ShaderName(material) +
                " missingReason=none");
        }

        private static string MaterialName(Material material)
        {
            if (material == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(material.name) ? "<unnamed>" : material.name;
        }

        private static string ShaderName(Material material)
        {
            if (material == null || material.shader == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(material.shader.name) ? "<unnamed>" : material.shader.name;
        }
    }
}

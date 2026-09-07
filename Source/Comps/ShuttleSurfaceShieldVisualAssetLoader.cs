using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Small cached loader for optional Surface Shield visual materials.
    /// Missing bundles/materials are cached as failures so PostDraw does not repeatedly touch disk.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldVisualAssetLoader
    {
        private const string LogCategory = "SurfaceShieldVisual";
        private const string MissingAssetBundleTypeKey = "MissingAssetBundleType";
        private const string MissingAssetBundleLoadFromFileMethodKey = "MissingAssetBundleLoadFromFileMethod";
        private const string AssetBundleLoadFromFileInvokeFailedKey = "AssetBundleLoadFromFileInvokeFailed";
        private const string MissingAssetBundleLoadAssetMethodKey = "MissingAssetBundleLoadAssetMethod";
        private const string AssetBundleLoadAssetInvokeFailedKey = "AssetBundleLoadAssetInvokeFailed";
        private const string MissingAssetBundleLoadFromMemoryMethodKey = "MissingAssetBundleLoadFromMemoryMethod";
        private const string AssetBundleLoadFromMemoryReturnedNullKey = "AssetBundleLoadFromMemoryReturnedNull";
        private const string AssetBundleLoadFromMemoryInvokeFailedKey = "AssetBundleLoadFromMemoryInvokeFailed";
        private const string MissingAssetBundleUnloadMethodKey = "MissingAssetBundleUnloadMethod";
        private const string AssetBundleUnloadInvokeFailedKey = "AssetBundleUnloadInvokeFailed";

        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private readonly Dictionary<string, object> bundleCache = new Dictionary<string, object>();
        private readonly HashSet<string> failedKeys = new HashSet<string>();
        private readonly HashSet<string> loggedLoadedMaterialKeys = new HashSet<string>();
        private string modRootPath;
        private string selectedBundleRelativePath;

        internal Material TryGetMaterial(
            string bundlePath,
            string materialAssetName,
            out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrWhiteSpace(materialAssetName))
            {
                failureReason = "Surface shield visual material is not configured.";
                return null;
            }

            string key = (bundlePath ?? "<null>") + "|" + materialAssetName;
            Material cachedMaterial;
            if (this.materialCache.TryGetValue(key, out cachedMaterial))
            {
                return cachedMaterial;
            }

            if (this.failedKeys.Contains(key))
            {
                failureReason = "Surface shield visual material previously failed to load. fromFailedKeys=true, failedKeyCount=" +
                    this.failedKeys.Count;
                return null;
            }

            try
            {
                string resolveFailureReason;
                string resolvedBundlePath = this.ResolveBundlePath(bundlePath, out resolveFailureReason);
                if (string.IsNullOrEmpty(resolvedBundlePath))
                {
                    failureReason = this.MarkFailed(
                        key,
                        resolveFailureReason ?? "Surface shield bundle path could not be resolved.");
                    return null;
                }

                if (!File.Exists(resolvedBundlePath))
                {
                    this.LogBundleMissingOnce(bundlePath, resolvedBundlePath);
                    failureReason = this.MarkFailed(
                        key,
                        "Surface shield bundle file is missing: " + resolvedBundlePath);
                    return null;
                }

                string loadMethod;
                object bundle = this.TryGetBundle(resolvedBundlePath, key, out loadMethod, out failureReason);
                if (bundle == null)
                {
                    return null;
                }

                Material material = this.TryLoadMaterialFromBundle(
                    bundle,
                    materialAssetName,
                    key,
                    out failureReason);
                if (material == null)
                {
                    return null;
                }

                CustomShaderMaterialValidationResult validation =
                    UnityCustomShaderMaterialValidator.Validate(
                        material,
                        ShuttleCustomShaderMaterialContracts.SurfaceShield);
                if (!validation.IsValid)
                {
                    ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                        "surface-shield",
                        this.selectedBundleRelativePath,
                        materialAssetName,
                        material,
                        validation,
                        "fallback");
                    failureReason = this.MarkFailed(
                        key,
                        "Surface shield custom material validation failed: " +
                        validation.ReasonCode +
                        (string.IsNullOrEmpty(validation.Detail) ?
                            string.Empty :
                            " (" + validation.Detail + ")"));
                    return null;
                }

                this.materialCache[key] = material;
                ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                    "surface-shield",
                    this.selectedBundleRelativePath,
                    materialAssetName,
                    material,
                    validation,
                    "custom");
                this.LogMaterialLoadedOnce(key, bundlePath, materialAssetName, loadMethod, material);
                return material;
            }
            catch (Exception exception)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    key,
                    "Surface shield AssetBundle load failed unexpectedly: " + exception.Message);
                return null;
            }
        }

        internal string ClearAllCachesForDev()
        {
            int materialCount = this.materialCache.Count;
            int bundleCount = this.bundleCache.Count;
            int failedCount = this.failedKeys.Count;
            int unloadedCount = 0;
            int unloadFailureCount = 0;

            foreach (KeyValuePair<string, object> entry in this.bundleCache)
            {
                if (this.TryUnloadBundle(entry.Value, false))
                {
                    unloadedCount++;
                }
                else
                {
                    unloadFailureCount++;
                }
            }

            this.materialCache.Clear();
            this.bundleCache.Clear();
            this.failedKeys.Clear();
            this.loggedLoadedMaterialKeys.Clear();

            return
                "materialCacheBefore=" + materialCount +
                ", bundleCacheBefore=" + bundleCount +
                ", failedKeysBefore=" + failedCount +
                ", bundlesUnloadFalse=" + unloadedCount +
                ", unloadFailures=" + unloadFailureCount;
        }

        internal string ResolveBundlePathForDebug(string bundlePath)
        {
            string failureReason;
            return this.ResolveBundlePath(bundlePath, out failureReason);
        }

        private object TryGetBundle(
            string resolvedBundlePath,
            string materialKey,
            out string loadMethod,
            out string failureReason)
        {
            loadMethod = null;
            failureReason = null;

            object cachedBundle;
            if (this.bundleCache.TryGetValue(resolvedBundlePath, out cachedBundle))
            {
                loadMethod = "cache";
                return cachedBundle;
            }

            Type assetBundleType = Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule");
            if (assetBundleType == null)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    materialKey,
                    "UnityEngine.AssetBundle type is unavailable in the current compile/runtime references.",
                    MissingAssetBundleTypeKey);
                return null;
            }

            MethodInfo loadFromFile = assetBundleType.GetMethod(
                "LoadFromFile",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            if (loadFromFile == null)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    materialKey,
                    "AssetBundle.LoadFromFile(string) is unavailable.",
                    MissingAssetBundleLoadFromFileMethodKey);
                return null;
            }

            object bundle;
            try
            {
                bundle = loadFromFile.Invoke(null, new object[] { resolvedBundlePath });
            }
            catch (Exception exception)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    materialKey,
                    "AssetBundle.LoadFromFile(string) threw: " + exception.Message,
                    AssetBundleLoadFromFileInvokeFailedKey);
                return null;
            }

            if (bundle == null)
            {
                bundle = this.TryLoadBundleFromMemory(assetBundleType, resolvedBundlePath, materialKey, out failureReason);
                if (bundle == null)
                {
                    failureReason = this.MarkFailedWithAssetBundleWarning(
                        materialKey,
                        "Surface shield AssetBundle LoadFromFile returned null and LoadFromMemory failed. path=" +
                        resolvedBundlePath +
                        ", memoryFailure=" +
                        (failureReason ?? "<none>"));
                    return null;
                }

                loadMethod = "LoadFromMemory";
            }
            else
            {
                loadMethod = "LoadFromFile";
            }

            this.bundleCache[resolvedBundlePath] = bundle;
            return bundle;
        }

        private Material TryLoadMaterialFromBundle(
            object bundle,
            string materialAssetName,
            string materialKey,
            out string failureReason)
        {
            failureReason = null;
            if (bundle == null)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    materialKey,
                    "Surface shield AssetBundle instance is unavailable.");
                return null;
            }

            MethodInfo loadAsset = bundle.GetType().GetMethod(
                "LoadAsset",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Type) },
                null);
            if (loadAsset == null)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    materialKey,
                    "AssetBundle.LoadAsset(string, Type) is unavailable.",
                    MissingAssetBundleLoadAssetMethodKey);
                return null;
            }

            object loaded;
            try
            {
                loaded = loadAsset.Invoke(bundle, new object[] { materialAssetName, typeof(Material) });
            }
            catch (Exception exception)
            {
                failureReason = this.MarkFailedWithAssetBundleWarning(
                    materialKey,
                    "AssetBundle.LoadAsset(string, Type) threw: " + exception.Message,
                    AssetBundleLoadAssetInvokeFailedKey);
                return null;
            }

            Material material = loaded as Material;
            if (material == null)
            {
                failureReason = this.MarkFailed(
                    materialKey,
                    "Surface shield material asset not found: " + materialAssetName);
                return null;
            }

            return material;
        }

        private object TryLoadBundleFromMemory(
            Type assetBundleType,
            string resolvedBundlePath,
            string materialKey,
            out string failureReason)
        {
            failureReason = null;
            MethodInfo loadFromMemory = assetBundleType.GetMethod(
                "LoadFromMemory",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(byte[]) },
                null);
            if (loadFromMemory == null)
            {
                failureReason = "AssetBundle.LoadFromMemory(byte[]) is unavailable.";
                this.LogAssetBundleReflectionWarningOnce(
                    MissingAssetBundleLoadFromMemoryMethodKey,
                    failureReason);
                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(resolvedBundlePath);
                object bundle = loadFromMemory.Invoke(null, new object[] { bytes });
                if (bundle == null)
                {
                    failureReason = "AssetBundle.LoadFromMemory(byte[]) returned null. failedKeyCount=" +
                        this.failedKeys.Count;
                    this.LogAssetBundleReflectionWarningOnce(
                        AssetBundleLoadFromMemoryReturnedNullKey,
                        failureReason);
                }

                return bundle;
            }
            catch (Exception exception)
            {
                failureReason = "AssetBundle.LoadFromMemory(byte[]) threw: " + exception.Message +
                    ", failedKeyCount=" +
                    this.failedKeys.Count +
                    ", materialKey=" +
                    (materialKey ?? "<null>");
                this.LogAssetBundleReflectionWarningOnce(
                    AssetBundleLoadFromMemoryInvokeFailedKey,
                    failureReason);
                return null;
            }
        }

        private bool TryUnloadBundle(object bundle, bool unloadAllLoadedObjects)
        {
            if (bundle == null)
            {
                return false;
            }

            try
            {
                MethodInfo unload = bundle.GetType().GetMethod(
                    "Unload",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(bool) },
                    null);
                if (unload == null)
                {
                    this.LogAssetBundleReflectionWarningOnce(
                        MissingAssetBundleUnloadMethodKey,
                        "AssetBundle.Unload(bool) is unavailable. Cached bundle cleanup will be skipped.");
                    return false;
                }

                unload.Invoke(bundle, new object[] { unloadAllLoadedObjects });
                return true;
            }
            catch (Exception exception)
            {
                this.LogAssetBundleReflectionWarningOnce(
                    AssetBundleUnloadInvokeFailedKey,
                    "AssetBundle.Unload(bool) threw: " + exception.Message +
                    ". Cached bundle cleanup will be skipped.");
                return false;
            }
        }

        private string MarkFailed(string materialKey, string reason)
        {
            this.failedKeys.Add(materialKey);
            return (reason ?? "unknown failure") +
                ", fromFailedKeys=false, failedKeyCount=" +
                this.failedKeys.Count;
        }

        private string MarkFailedWithAssetBundleWarning(string materialKey, string reason, string warningKey = null)
        {
            this.LogAssetBundleReflectionWarningOnce(
                warningKey ?? "assetbundle-load-failure|" + (materialKey ?? "<null>") + "|" + (reason ?? "<null>"),
                reason);
            return this.MarkFailed(materialKey, reason);
        }

        private void LogAssetBundleReflectionWarningOnce(string warningKey, string reason)
        {
            ShuttleLog.WarnOnce(
                LogCategory,
                warningKey,
                "Surface shield AssetBundle reflection/load failure. " +
                (reason ?? "unknown failure") +
                " Fallback material/visual behavior will be used.");
        }

        private string ResolveBundlePath(string bundlePath, out string failureReason)
        {
            failureReason = null;
            this.selectedBundleRelativePath = null;
            if (string.IsNullOrWhiteSpace(bundlePath))
            {
                failureReason = "Surface shield bundle path is empty.";
                this.LogInvalidBundlePathOnce(bundlePath, failureReason);
                return null;
            }

            string normalizedBundlePath = bundlePath.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalizedBundlePath))
            {
                failureReason = "Surface shield bundle path is absolute and was rejected: " + normalizedBundlePath;
                this.LogInvalidBundlePathOnce(normalizedBundlePath, failureReason);
                return null;
            }

            if (normalizedBundlePath.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                failureReason = "Surface shield bundle path contains parent-directory traversal and was rejected: " +
                    normalizedBundlePath;
                this.LogInvalidBundlePathOnce(normalizedBundlePath, failureReason);
                return null;
            }

            if (!normalizedBundlePath.StartsWith(
                ShuttleModConstants.SurfaceShieldBundlePathPrefix,
                StringComparison.Ordinal))
            {
                failureReason = "Surface shield bundle path must be under " +
                    ShuttleModConstants.SurfaceShieldBundlePathPrefix +
                    " and was rejected: " +
                    normalizedBundlePath;
                this.LogInvalidBundlePathOnce(normalizedBundlePath, failureReason);
                return null;
            }

            string root = this.ResolveModRootPath();
            if (string.IsNullOrEmpty(root))
            {
                failureReason = "Surface shield bundle path could not be resolved because the mod root is unavailable.";
                return null;
            }

            try
            {
                string fullRoot = Path.GetFullPath(root);
                string[] candidates = ShuttlePlatformAssetBundlePathResolver.GetCandidateRelativePaths(
                    ShuttlePlatformAssetBundleEnvironment.CurrentPlatform,
                    new[] { normalizedBundlePath });
                if (candidates.Length == 0)
                {
                    failureReason = "Surface shield custom AssetBundles are disabled for runtime platform " +
                        Application.platform + ".";
                    ShuttleCustomShaderDiagnostics.LogBundleSelection(
                        "surface-shield",
                        null,
                        false,
                        "unsupported-platform");
                    return null;
                }

                for (int i = 0; i < candidates.Length; i++)
                {
                    string candidate = candidates[i];
                    string fullResolved = Path.GetFullPath(Path.Combine(fullRoot, candidate));
                    if (!this.IsInsideDirectory(fullRoot, fullResolved))
                    {
                        failureReason = "Surface shield bundle path resolved outside the mod root and was rejected: " +
                            candidate;
                        this.LogInvalidBundlePathOnce(candidate, failureReason);
                        return null;
                    }

                    if (!File.Exists(fullResolved))
                    {
                        continue;
                    }

                    this.selectedBundleRelativePath = candidate;
                    ShuttleCustomShaderDiagnostics.LogBundleSelection(
                        "surface-shield",
                        candidate,
                        true,
                        "candidate-found");
                    return fullResolved;
                }

                failureReason = "No compatible Surface Shield AssetBundle exists for runtime platform " +
                    Application.platform + ". checkedPaths=" + string.Join(", ", candidates);
                ShuttleCustomShaderDiagnostics.LogBundleSelection(
                    "surface-shield",
                    candidates[0],
                    false,
                    "file-not-found");
                return null;
            }
            catch (Exception exception)
            {
                failureReason = "Surface shield bundle path could not be normalized and was rejected: " +
                    normalizedBundlePath +
                    ", reason=" +
                    exception.Message;
                this.LogInvalidBundlePathOnce(normalizedBundlePath, failureReason);
                return null;
            }
        }

        private string ResolveModRootPath()
        {
            if (!string.IsNullOrEmpty(this.modRootPath))
            {
                return this.modRootPath;
            }

            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            if (mods != null)
            {
                for (int i = 0; i < mods.Count; i++)
                {
                    ModContentPack mod = mods[i];
                    if (mod != null &&
                        string.Equals(mod.PackageId, ShuttleModConstants.PackageId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(mod.RootDir))
                        {
                            continue;
                        }

                        try
                        {
                            this.modRootPath = Path.GetFullPath(mod.RootDir);
                            return this.modRootPath;
                        }
                        catch (Exception exception)
                        {
                            ShuttleLog.WarnOnce(
                                LogCategory,
                                "mod-root-normalize-failed|" + mod.RootDir,
                                "Surface shield mod root could not be normalized. packageId=" +
                                ShuttleModConstants.PackageId +
                                ", root=" +
                                mod.RootDir +
                                ", reason=" +
                                exception.Message);
                            return null;
                        }
                    }
                }
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "mod-root-unresolved|" + ShuttleModConstants.PackageId,
                "Surface shield mod root could not be resolved. packageId=" +
                ShuttleModConstants.PackageId);
            return null;
        }

        private bool IsInsideDirectory(string directoryPath, string candidatePath)
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

        private void LogInvalidBundlePathOnce(string bundlePath, string reason)
        {
            ShuttleLog.WarnOnce(
                LogCategory,
                "invalid-bundle-path|" + (bundlePath ?? "<null>"),
                "Invalid surface shield AssetBundle path rejected. " + (reason ?? "unknown reason"));
        }

        private void LogBundleMissingOnce(string bundlePath, string resolvedBundlePath)
        {
            ShuttleLog.WarnOnce(
                LogCategory,
                "bundle-missing|" + (resolvedBundlePath ?? bundlePath ?? "<null>"),
                "Surface shield AssetBundle file is missing. configuredPath=" +
                (bundlePath ?? "<null>") +
                ", resolvedPath=" +
                (resolvedBundlePath ?? "<null>"));
        }

        private void LogMaterialLoadedOnce(
            string key,
            string bundlePath,
            string materialAssetName,
            string loadMethod,
            Material material)
        {
            if (!Prefs.DevMode || this.loggedLoadedMaterialKeys.Contains(key))
            {
                return;
            }

            this.loggedLoadedMaterialKeys.Add(key);
            ShuttleLog.Debug(
                LogCategory,
                "Surface shield material loaded: bundle=" +
                (bundlePath ?? "<null>") +
                ", material=" +
                (materialAssetName ?? "<null>") +
                ", loadMethod=" +
                (loadMethod ?? "<unknown>") +
                ", materialName=" +
                (material != null ? material.name : "<null>") +
                ", shader=" +
                (material != null && material.shader != null ? material.shader.name : "<null>"));
        }
    }
}

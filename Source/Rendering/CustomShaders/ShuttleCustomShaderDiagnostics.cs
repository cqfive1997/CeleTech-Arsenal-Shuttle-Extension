using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders
{
    internal static class ShuttleCustomShaderDiagnostics
    {
        private const string LogCategory = "CustomShaderCompatibility";

        internal static void LogBundleSelection(
            string visualPath,
            string selectedRelativePath,
            bool succeeded,
            string reasonCode)
        {
            string key = "bundle|" +
                (visualPath ?? "unknown") + "|" +
                (selectedRelativePath ?? "none") + "|" +
                (reasonCode ?? "none");
            string message = BuildEnvironmentPrefix(visualPath) +
                " selectedBundle=" + (selectedRelativePath ?? "<none>") +
                " bundleLoad=" + (succeeded ? "success" : "failure") +
                " validation=" + (reasonCode ?? "none") +
                " rendering=" + (succeeded ? "custom-candidate" : "fallback");

            if (succeeded)
            {
                ShuttleLog.Debug(LogCategory, message);
            }
            else
            {
                ShuttleLog.WarnOnce(LogCategory, key, message);
            }
        }

        internal static void LogMaterialValidation(
            string visualPath,
            string bundleRelativePath,
            string materialAssetName,
            Material material,
            CustomShaderMaterialValidationResult result,
            string renderingOutcome)
        {
            string shaderName = material != null && material.shader != null ?
                material.shader.name :
                "<null>";
            bool shaderSupported = material != null &&
                material.shader != null &&
                material.shader.isSupported;
            string key = "material|" +
                (visualPath ?? "unknown") + "|" +
                (materialAssetName ?? "none") + "|" +
                (result.ReasonCode ?? "none") + "|" +
                (result.Detail ?? "none");
            string message = BuildEnvironmentPrefix(visualPath) +
                " selectedBundle=" + (bundleRelativePath ?? "<unknown>") +
                " material=" + (materialAssetName ?? "<unknown>") +
                " shader=" + shaderName +
                " shaderSupported=" + shaderSupported +
                " validation=" + (result.ReasonCode ?? "none") +
                (string.IsNullOrEmpty(result.Detail) ? string.Empty : " detail=" + result.Detail) +
                " rendering=" + (renderingOutcome ?? (result.IsValid ? "custom" : "fallback"));

            if (result.IsValid)
            {
                ShuttleLog.Debug(LogCategory, message);
            }
            else
            {
                ShuttleLog.WarnOnce(LogCategory, key, message);
            }
        }

        private static string BuildEnvironmentPrefix(string visualPath)
        {
            return "visualPath=" + (visualPath ?? "unknown") +
                " platform=" + Application.platform +
                " graphicsApi=" + SystemInfo.graphicsDeviceType +
                " graphicsDevice=" + Sanitize(SystemInfo.graphicsDeviceName) +
                " shaderLevel=" + SystemInfo.graphicsShaderLevel +
                " unityVersion=" + Application.unityVersion;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "<unknown>";
            }

            return value.Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}

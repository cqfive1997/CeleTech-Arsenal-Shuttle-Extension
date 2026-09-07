using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation.External
{
    /// <summary>
    /// Central display-name boundary for third-party module/runtime metadata.
    /// Logic keys remain raw IDs; ordinary UI consumes only these player-facing labels.
    /// </summary>
    internal static class ExternalRuntimeMetadataResolver
    {
        internal const string MissingValueText = "-";

        internal static string ResolveModuleLabel(ShuttleModuleBaseDef moduleDef, string fallback)
        {
            if (moduleDef != null)
            {
                string label = moduleDef.LabelCap.ToString();
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }

                if (!string.IsNullOrEmpty(moduleDef.defName))
                {
                    return moduleDef.defName;
                }
            }

            return !string.IsNullOrEmpty(fallback) ? fallback : MissingValueText;
        }

        internal static string ResolveRuntimeLabel(ExternalRuntimeRegistration registration)
        {
            if (registration == null)
            {
                return MissingValueText;
            }

            string translated = TranslateIfAvailable(registration.RuntimeLabelKey);
            if (!string.IsNullOrEmpty(translated))
            {
                return translated;
            }

            if (!string.IsNullOrEmpty(registration.RuntimeLabel))
            {
                return registration.RuntimeLabel;
            }

            return !string.IsNullOrEmpty(registration.LocalRuntimeKey)
                ? registration.LocalRuntimeKey
                : GetRuntimeLocalPart(registration.FullRuntimeKey);
        }

        internal static string ResolveRuntimeLabel(string runtimeKey)
        {
            ExternalRuntimeRegistration registration;
            if (ExternalShuttleRuntimeRegistry.TryResolve(runtimeKey, out registration) &&
                registration != null)
            {
                return ResolveRuntimeLabel(registration);
            }

            return GetRuntimeLocalPart(runtimeKey);
        }

        internal static string ResolveRuntimeDescription(ExternalRuntimeRegistration registration)
        {
            if (registration == null)
            {
                return string.Empty;
            }

            string translated = TranslateIfAvailable(registration.RuntimeDescriptionKey);
            if (!string.IsNullOrEmpty(translated))
            {
                return translated;
            }

            return !string.IsNullOrEmpty(registration.RuntimeDescription)
                ? registration.RuntimeDescription
                : string.Empty;
        }

        internal static string GetRuntimeLocalPart(string runtimeKey)
        {
            string normalizedRuntimeKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeKey);
            if (string.IsNullOrEmpty(normalizedRuntimeKey))
            {
                return MissingValueText;
            }

            int slash = normalizedRuntimeKey.LastIndexOf('/');
            return slash >= 0 && slash + 1 < normalizedRuntimeKey.Length
                ? normalizedRuntimeKey.Substring(slash + 1)
                : normalizedRuntimeKey;
        }

        private static string TranslateIfAvailable(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            string normalized = key.Trim();
            return Translator.CanTranslate(normalized)
                ? normalized.Translate().ToString()
                : null;
        }
    }
}

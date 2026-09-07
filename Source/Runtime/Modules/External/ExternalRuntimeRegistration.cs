using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalRuntimeRegistration
    {
        internal ExternalRuntimeRegistration(
            string ownerPackageId,
            string localRuntimeKey,
            string fullRuntimeKey,
            IShuttleExternalModuleRuntimeSystem system,
            string runtimeLabelKey,
            string runtimeDescriptionKey)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalRuntimeKey = localRuntimeKey;
            this.FullRuntimeKey = fullRuntimeKey;
            this.System = system;
            this.RuntimeLabelKey = runtimeLabelKey;
            this.RuntimeDescriptionKey = runtimeDescriptionKey;
        }

        internal string OwnerPackageId { get; private set; }

        internal string LocalRuntimeKey { get; private set; }

        internal string FullRuntimeKey { get; private set; }

        internal string RuntimeLabelKey { get; private set; }

        internal string RuntimeDescriptionKey { get; private set; }

        internal string RuntimeLabel
        {
            get
            {
                return TranslateOrFallback(this.RuntimeLabelKey, this.LocalRuntimeKey);
            }
        }

        internal string RuntimeDescription
        {
            get
            {
                return TranslateOrFallback(this.RuntimeDescriptionKey, string.Empty);
            }
        }

        internal IShuttleExternalModuleRuntimeSystem System { get; private set; }

        private static string TranslateOrFallback(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            string normalizedKey = key.Trim();
            if (!Translator.CanTranslate(normalizedKey))
            {
                return fallback;
            }

            return normalizedKey.Translate().ToString();
        }
    }
}

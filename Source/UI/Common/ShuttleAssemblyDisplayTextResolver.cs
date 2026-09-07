using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleAssemblyDisplayTextResolver
    {
        internal static string ResolveSegmentDisplayName(
            ShuttleControlSegmentSlotModel segment)
        {
            return ResolveSafeDisplayText(
                ShuttleControlDisplayNameResolver.ResolveSegmentLabel(segment));
        }

        internal static string ResolveSegmentDescription(
            ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null)
            {
                return "-";
            }

            string installedDefDescription =
                ResolveSegmentDefDescription(segment.InstalledSegmentDefName);
            if (!string.IsNullOrEmpty(installedDefDescription))
            {
                return installedDefDescription;
            }

            string defaultDefDescription =
                ResolveSegmentDefDescription(segment.DefaultSegmentDefName);
            if (!string.IsNullOrEmpty(defaultDefDescription))
            {
                return defaultDefDescription;
            }

            if (!string.IsNullOrEmpty(segment.InstalledSegmentDescription))
            {
                return segment.InstalledSegmentDescription;
            }

            if (!string.IsNullOrEmpty(segment.DefaultSegmentDescription))
            {
                return segment.DefaultSegmentDescription;
            }

            if (!string.IsNullOrEmpty(segment.Description))
            {
                return segment.Description;
            }

            return ResolveTranslatedText(segment.DescriptionKey);
        }

        internal static string ResolveModuleDisplayName(
            ShuttleControlModuleSlotModel module)
        {
            return ResolveSafeDisplayText(
                ShuttleControlDisplayNameResolver.ResolveModuleLabel(module));
        }

        internal static string ResolveModuleDisplayName(
            string moduleDefName,
            string moduleLabel,
            string fallback)
        {
            string defLabel = ResolveModuleDefDisplayNameByName(moduleDefName);
            if (!string.IsNullOrEmpty(defLabel))
            {
                return defLabel;
            }

            if (!string.IsNullOrEmpty(moduleLabel))
            {
                return moduleLabel;
            }

            return ResolveSafeDisplayText(fallback);
        }

        internal static string ResolveCargoRegionDisplayName(
            int regionIndex,
            string configuredLabel,
            bool hasCustomLabel)
        {
            if (hasCustomLabel && !string.IsNullOrEmpty(configuredLabel))
            {
                return configuredLabel;
            }

            return "CT_Shuttle_UI_CargoBayLabel".Translate(regionIndex + 1).ToString();
        }

        internal static string ResolveRefrigeratedCargoDisplayName(
            string moduleDefName,
            string configuredOrFallbackLabel,
            bool hasCustomLabel,
            string fallback)
        {
            if (hasCustomLabel && !string.IsNullOrEmpty(configuredOrFallbackLabel))
            {
                return configuredOrFallbackLabel;
            }

            return ResolveModuleDisplayName(
                moduleDefName,
                configuredOrFallbackLabel,
                fallback);
        }

        internal static string ResolveModuleDescription(
            ShuttleControlModuleSlotModel module)
        {
            if (module == null)
            {
                return "-";
            }

            string defDescription =
                ResolveModuleDefDescriptionByName(module.InstalledModuleDefName);
            if (!string.IsNullOrEmpty(defDescription))
            {
                return defDescription;
            }

            if (!string.IsNullOrEmpty(module.InstalledModuleDescription))
            {
                return module.InstalledModuleDescription;
            }

            if (!string.IsNullOrEmpty(module.Description))
            {
                return module.Description;
            }

            return ResolveTranslatedText(module.DescriptionKey);
        }

        internal static string ResolveSegmentSlotDisplayName(
            ShuttleControlSegmentSlotModel segment,
            bool shortLabel)
        {
            return ResolveSafeDisplayText(
                ShuttleControlDisplayNameResolver.ResolveSegmentSlotLabel(
                    segment,
                    shortLabel));
        }

        internal static string ResolveModuleSlotDisplayName(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel module,
            bool shortLabel)
        {
            return ResolveSafeDisplayText(
                ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(
                    segment,
                    module,
                    shortLabel));
        }

        internal static string ResolveStatusText(string statusText)
        {
            return ResolveSafeDisplayText(statusText);
        }

        internal static string ResolveFallbackLabel(string value)
        {
            return ResolveSafeDisplayText(value);
        }

        internal static string ResolveSafeDisplayText(string value)
        {
            return !string.IsNullOrEmpty(value) ? value : "-";
        }

        internal static string ResolveDefDisplayName(Def def)
        {
            if (def == null)
            {
                return "-";
            }

            return ResolveSafeDisplayText(
                ShuttleLocalizedDefTextLookup.GetLabel(def));
        }

        internal static string ResolveDefDisplayName(Def def, string fallback)
        {
            if (def != null)
            {
                string label = ResolveDefDisplayName(def);
                if (!string.IsNullOrEmpty(label) &&
                    label != "-" &&
                    label != def.defName)
                {
                    return label;
                }
            }

            return ResolveSafeDisplayText(fallback);
        }

        internal static string ResolveDefDescription(Def def)
        {
            return def != null
                ? ResolveSafeDisplayText(
                    ShuttleLocalizedDefTextLookup.GetDescription(def))
                : "-";
        }

        internal static string ResolveModuleDefDisplayName(
            ShuttleModuleBaseDef moduleDef)
        {
            string translated = TranslateIfAvailable(
                GetModuleNameTranslateKey(moduleDef));
            return !string.IsNullOrEmpty(translated)
                ? translated
                : ResolveDefDisplayName(moduleDef);
        }

        internal static string ResolveModuleDefDisplayName(
            string moduleDefName,
            string fallback)
        {
            string defLabel = ResolveModuleDefDisplayNameByName(moduleDefName);
            return !string.IsNullOrEmpty(defLabel)
                ? defLabel
                : ResolveSafeDisplayText(fallback);
        }

        internal static string ResolveModuleDefDescription(
            ShuttleModuleBaseDef moduleDef)
        {
            string translated = TranslateIfAvailable(
                GetModuleDescriptionTranslateKey(moduleDef));
            return !string.IsNullOrEmpty(translated)
                ? translated
                : ResolveDefDescription(moduleDef);
        }

        private static string ResolveTranslatedText(string key)
        {
            string translated = TranslateIfAvailable(key);
            return !string.IsNullOrEmpty(translated) ? translated : "-";
        }

        private static string ResolveSegmentDefDescription(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            ShuttleSegmentBaseDef segmentDef =
                DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(defName);
            return ResolveDefDescriptionOrNull(segmentDef);
        }

        private static string ResolveModuleDefDescriptionByName(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            ShuttleModuleBaseDef moduleDef =
                DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(defName);
            return ResolveDefDescriptionOrNull(moduleDef);
        }

        private static string ResolveModuleDefDisplayNameByName(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            ShuttleModuleBaseDef moduleDef =
                DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(defName);
            if (moduleDef == null)
            {
                return null;
            }

            string label = ResolveModuleDefDisplayName(moduleDef);
            return !string.IsNullOrEmpty(label) &&
                label != "-" &&
                label != moduleDef.defName
                ? label
                : null;
        }

        private static string ResolveDefDescriptionOrNull(Def def)
        {
            if (def == null)
            {
                return null;
            }

            string description = ShuttleLocalizedDefTextLookup.GetDescription(def);
            return !string.IsNullOrEmpty(description) && description != "-"
                ? description
                : null;
        }

        private static string GetModuleNameTranslateKey(
            ShuttleModuleBaseDef moduleDef)
        {
            if (moduleDef == null)
            {
                return null;
            }

            switch (moduleDef.defName)
            {
                case "CT_Shuttle_AutoWorkTableModule":
                    return "CT_Shuttle_Module_AutoWorkTable_Name";
                case "CT_Shuttle_AutoKitchen_Basic":
                    return "CT_Shuttle_Module_AutoKitchen_Name";
                case "CT_Shuttle_CargoLatchModule":
                    return "CT_Shuttle_Module_CargoLocker_Name";
                case "CT_Shuttle_Module_CargoLogisticsCore":
                    return "CT_Shuttle_Module_CargoLogisticsCore_Name";
                case "CT_Shuttle_RefrigeratedCargoModule":
                    return "CT_Shuttle_Module_RefrigeratedCargo_Name";
                case "CT_Shuttle_HullPlatingModule":
                    return "CT_Shuttle_Module_HullPlating_Name";
                case "CT_Shuttle_Module_AmmoLoader_I":
                    return "CT_Shuttle_Module_AmmoLoader_Name";
                case "CT_Shuttle_FireControlRadar_Basic":
                    return "CT_Shuttle_Module_FireControlRadar_Name";
                case "CT_Shuttle_Module_6mmPointDefense":
                    return "CT_Shuttle_Module_6mmPointDefense_Name";
                case "CT_Shuttle_Module_40mmCIWS":
                    return "CT_Shuttle_Module_40mmCIWS_Name";
                case "CT_Shuttle_BasicShieldModule":
                    return "CT_Shuttle_Module_BasicShield_Name";
                case "CT_Shuttle_SurfaceShield_Mk1":
                    return "CT_Shuttle_Module_SurfaceShieldMk1_Name";
                case "CT_Shuttle_SurfaceShield_I":
                    return "CT_Shuttle_Module_SurfaceShieldI_Name";
                case "CT_Shuttle_SurfaceShield_II":
                    return "CT_Shuttle_Module_SurfaceShieldII_Name";
                case "CT_Shuttle_SurfaceShield_III":
                    return "CT_Shuttle_Module_SurfaceShieldIII_Name";
                case "CT_Shuttle_SurfaceShield_IV":
                    return "CT_Shuttle_Module_SurfaceShieldIV_Name";
                case "CT_Shuttle_PrisonCell_Basic":
                    return "CT_Shuttle_Module_PrisonCell_Name";
                default:
                    return null;
            }
        }

        private static string GetModuleDescriptionTranslateKey(
            ShuttleModuleBaseDef moduleDef)
        {
            if (moduleDef == null)
            {
                return null;
            }

            switch (moduleDef.defName)
            {
                case "CT_Shuttle_AutoWorkTableModule":
                    return "CT_Shuttle_Module_AutoWorkTable_Desc";
                case "CT_Shuttle_AutoKitchen_Basic":
                    return "CT_Shuttle_Module_AutoKitchen_Desc";
                case "CT_Shuttle_CargoLatchModule":
                    return "CT_Shuttle_Module_CargoLocker_Desc";
                case "CT_Shuttle_Module_CargoLogisticsCore":
                    return "CT_Shuttle_Module_CargoLogisticsCore_Desc";
                case "CT_Shuttle_RefrigeratedCargoModule":
                    return "CT_Shuttle_Module_RefrigeratedCargo_Desc";
                default:
                    return null;
            }
        }

        private static string TranslateIfAvailable(string translateKey)
        {
            if (string.IsNullOrEmpty(translateKey) ||
                !Translator.CanTranslate(translateKey))
            {
                return null;
            }

            string translated = translateKey.Translate().ToString();
            return string.IsNullOrEmpty(translated) || translated == translateKey
                ? null
                : translated;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal static class V3MainInstallCandidateText
    {
        internal static string GetDefLabel(Def def)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(def);
        }

        internal static string GetDefDescription(Def def)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveDefDescription(def);
        }

        internal static string ResolveModuleDisplayLabel(ShuttleModuleBaseDef moduleDef)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveModuleDefDisplayName(moduleDef);
        }

        internal static string ResolveModuleDisplayDescription(ShuttleModuleBaseDef moduleDef)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveModuleDefDescription(moduleDef);
        }

        internal static string BuildSegmentConstructionCostSummary(
            ShuttleSegmentBaseDef segmentDef)
        {
            return ShuttleConstructionCostUtility.BuildCostSummary(
                ShuttleConstructionCostUtility.GetSegmentConstructionCost(segmentDef));
        }

        internal static string BuildModuleConstructionCostSummary(
            ShuttleModuleBaseDef moduleDef)
        {
            string summary = ShuttleConstructionCostUtility.BuildCostSummary(
                ShuttleConstructionCostUtility.GetModuleConstructionCost(moduleDef));
            ShuttleHullPlatingModuleDef hullDef = moduleDef as ShuttleHullPlatingModuleDef;
            if (hullDef == null || hullDef.stuffCostCount <= 0)
            {
                return summary;
            }

            return ShuttleUIText.Tr(
                "CT_Shuttle_UI_InstallCandidate_MaterialsWithDeferredStuff",
                summary,
                hullDef.stuffCostCount);
        }

        internal static string GetHullPlatingStuffOptionLabel(ThingDef stuffDef)
        {
            string stuffLabel = stuffDef != null ? stuffDef.LabelCap.ToString() : "-";
            return ShuttleUIText.Tr("CT_Shuttle_Hull_StuffedArmorLabel", stuffLabel);
        }

        internal static string GetCandidateMaterialsText(
            V3MainInstallCandidateModel candidate)
        {
            return candidate != null && !string.IsNullOrEmpty(candidate.ConstructionCostSummary)
                ? ShuttleUIText.Tr(
                    "CT_Shuttle_UI_InstallCandidate_Materials",
                    candidate.ConstructionCostSummary)
                : null;
        }

        internal static string BuildTooltip(V3MainInstallCandidateModel candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            string text = ValueOrDash(candidate.DisplayLabel) + "\n" +
                ValueOrDash(candidate.DisplayDescription) + "\n" +
                GetStatusLabel(candidate);
            string costText = GetCandidateMaterialsText(candidate);
            if (!string.IsNullOrEmpty(costText))
            {
                text += "\n" + costText;
            }

            if (!string.IsNullOrEmpty(candidate.DisabledReason))
            {
                text += "\n" + candidate.DisabledReason;
            }

            return text;
        }

        internal static string GetStatusLabel(V3MainInstallCandidateModel candidate)
        {
            return candidate != null && !string.IsNullOrEmpty(candidate.StatusTranslateKey)
                ? ShuttleUIText.Tr(candidate.StatusTranslateKey)
                : "-";
        }

        internal static string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }
    }
}

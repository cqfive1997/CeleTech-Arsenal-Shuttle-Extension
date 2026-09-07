using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueDetailTextBuilder
    {
        internal static string BuildDetail(
            ShuttleControlReadModel model,
            string code,
            ShuttleIssueCategory category,
            string fallbackTitle,
            string fallbackMessage)
        {
            string lowerCode = !string.IsNullOrEmpty(code)
                ? code.ToLowerInvariant()
                : string.Empty;
            if (lowerCode == ShuttleAssemblyIntegrityReport.BlockingPlayerIssueCode)
            {
                return "CT_Shuttle_Issue_AssemblyTopologyCorruptedDesc".Translate().ToString();
            }

            if (lowerCode == "required-segment-slot-empty")
            {
                return "CT_Shuttle_Issue_Detail_RequiredSegmentSlotEmpty".Translate().ToString();
            }

            if (lowerCode == "required-module-slot-empty" ||
                lowerCode == "required-module-slot-missing" ||
                lowerCode.Contains("required-module"))
            {
                return "CT_Shuttle_Issue_Detail_RequiredModuleSlotEmpty".Translate().ToString();
            }

            if (category == ShuttleIssueCategory.Cargo &&
                (lowerCode.Contains("over-capacity") ||
                    lowerCode.Contains("overloaded") ||
                    lowerCode.Contains("overload") ||
                    lowerCode.Contains("mass")))
            {
                return "CT_Shuttle_Issue_CargoOverloaded_Detail".Translate().ToString();
            }

            if (!string.IsNullOrEmpty(fallbackTitle) && fallbackTitle != "-")
            {
                return fallbackTitle;
            }

            return !string.IsNullOrEmpty(fallbackMessage) ? fallbackMessage : null;
        }
    }
}

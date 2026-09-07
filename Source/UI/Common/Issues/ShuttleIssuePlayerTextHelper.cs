using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssuePlayerTextHelper
    {
        internal static string BuildDetail(
            ShuttleControlReadModel model,
            string code,
            ShuttleIssueCategory category,
            string fallbackTitle,
            string fallbackMessage)
        {
            return ShuttleIssueDetailTextBuilder.BuildDetail(
                model,
                code,
                category,
                fallbackTitle,
                fallbackMessage);
        }

        internal static List<ShuttleIssueEvidenceLine> BuildEvidence(
            ShuttleControlReadModel model,
            string code,
            ShuttleIssueCategory category,
            string referenceID)
        {
            return ShuttleIssueEvidenceBuilder.BuildEvidence(
                model,
                code,
                category,
                referenceID);
        }

        internal static ShuttleIssueEvidenceLine BuildEvidenceLine(
            string labelKey,
            string value)
        {
            return ShuttleIssueEvidenceBuilder.BuildEvidenceLine(labelKey, value);
        }

        internal static bool IsModuleSlotReference(string code, string referenceID)
        {
            return ShuttleIssueSlotReferenceResolver.IsModuleSlotReference(
                code,
                referenceID);
        }

        internal static void SplitModuleReference(
            string referenceID,
            ref string segmentID,
            ref string moduleSlotID)
        {
            ShuttleIssueSlotReferenceResolver.SplitModuleReference(
                referenceID,
                ref segmentID,
                ref moduleSlotID);
        }
    }
}

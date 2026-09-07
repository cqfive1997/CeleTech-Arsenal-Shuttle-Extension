using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueEvidenceBuilder
    {
        internal static List<ShuttleIssueEvidenceLine> BuildEvidence(
            ShuttleControlReadModel model,
            string code,
            ShuttleIssueCategory category,
            string referenceID)
        {
            List<ShuttleIssueEvidenceLine> evidence = new List<ShuttleIssueEvidenceLine>();
            if (category != ShuttleIssueCategory.Assembly ||
                string.IsNullOrEmpty(referenceID))
            {
                return evidence;
            }

            bool moduleSlot = ShuttleIssueSlotReferenceResolver.IsModuleSlotReference(
                code,
                referenceID);
            evidence.Add(BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_Location",
                moduleSlot
                    ? ShuttleIssueSlotReferenceResolver.ResolveModuleSlotDisplayName(
                        model,
                        referenceID)
                    : ShuttleIssueSlotReferenceResolver.ResolveSegmentSlotDisplayName(
                        model,
                        referenceID)));
            evidence.Add(BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_Type",
                (moduleSlot
                    ? "CT_Shuttle_Issue_Evidence_ModuleSlot"
                    : "CT_Shuttle_Issue_Evidence_SegmentSlot").Translate().ToString()));
            return evidence;
        }

        internal static ShuttleIssueEvidenceLine BuildEvidenceLine(
            string labelKey,
            string value)
        {
            return new ShuttleIssueEvidenceLine
            {
                Label = labelKey.Translate().ToString(),
                Value = ShuttleAssemblyDisplayTextResolver.ResolveFallbackLabel(value)
            };
        }
    }
}

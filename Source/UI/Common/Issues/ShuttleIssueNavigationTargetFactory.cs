using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueNavigationTargetFactory
    {
        internal static ShuttleIssueNavigationTarget BuildNavigationTarget(
            ShuttleControlReadModel model,
            ShuttleIssueCategory category,
            string code,
            string referenceID)
        {
            if (category == ShuttleIssueCategory.Assembly)
            {
                if (ShuttleIssuePlayerTextHelper.IsModuleSlotReference(code, referenceID))
                {
                    return BuildModuleSlotTarget(model, referenceID);
                }

                ShuttleControlSegmentSlotModel segment;
                if (ShuttleIssueSlotReferenceResolver.TryResolveSegmentSlot(
                    model,
                    referenceID,
                    out segment) &&
                    segment != null)
                {
                    return new ShuttleIssueNavigationTarget
                    {
                        Kind = ShuttleIssueNavigationTargetKind.SegmentSlot,
                        SegmentSlotID = segment.SlotID
                    };
                }
            }

            return BuildNavigationTarget(category, code, referenceID);
        }

        internal static ShuttleIssueNavigationTarget BuildNavigationTarget(
            ShuttleIssueCategory category,
            string code,
            string referenceID)
        {
            if (category == ShuttleIssueCategory.Assembly)
            {
                if (ShuttleIssuePlayerTextHelper.IsModuleSlotReference(code, referenceID))
                {
                    return BuildModuleSlotTarget(referenceID);
                }

                return new ShuttleIssueNavigationTarget
                {
                    Kind = ShuttleIssueNavigationTargetKind.SegmentSlot,
                    SegmentSlotID = referenceID
                };
            }

            if (category == ShuttleIssueCategory.Cargo ||
                category == ShuttleIssueCategory.Loading ||
                category == ShuttleIssueCategory.Crew)
            {
                return new ShuttleIssueNavigationTarget
                {
                    Kind = ShuttleIssueNavigationTargetKind.CargoBay,
                    CargoBayKey = referenceID
                };
            }

            return new ShuttleIssueNavigationTarget
            {
                Kind = ShuttleIssueNavigationTargetKind.None
            };
        }

        internal static ShuttleIssueNavigationTarget BuildModuleSlotTarget(
            string referenceID)
        {
            string segmentID = null;
            string moduleSlotID = referenceID;
            ShuttleIssuePlayerTextHelper.SplitModuleReference(
                referenceID,
                ref segmentID,
                ref moduleSlotID);
            return new ShuttleIssueNavigationTarget
            {
                Kind = ShuttleIssueNavigationTargetKind.ModuleSlot,
                SegmentSlotID = segmentID,
                ModuleSlotID = moduleSlotID
            };
        }

        internal static ShuttleIssueNavigationTarget BuildModuleSlotTarget(
            ShuttleControlReadModel model,
            string referenceID)
        {
            ShuttleControlSegmentSlotModel segment;
            ShuttleControlModuleSlotModel moduleSlot;
            if (ShuttleIssueSlotReferenceResolver.TryResolveModuleSlot(
                model,
                referenceID,
                out segment,
                out moduleSlot) &&
                segment != null &&
                moduleSlot != null)
            {
                return new ShuttleIssueNavigationTarget
                {
                    Kind = ShuttleIssueNavigationTargetKind.ModuleSlot,
                    SegmentSlotID = segment.SlotID,
                    ModuleSlotID = moduleSlot.SlotID
                };
            }

            return BuildModuleSlotTarget(referenceID);
        }
    }
}

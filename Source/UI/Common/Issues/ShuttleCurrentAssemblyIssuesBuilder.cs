using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentAssemblyIssuesBuilder
    {
        internal void AddIssues(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || issues == null)
            {
                return;
            }

            this.AddAssemblyConstructionIssue(model, issues);
            this.AddDisabledRequiredModuleIssues(model, issues);
        }

        private void AddAssemblyConstructionIssue(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> issues)
        {
            ShuttleAssemblyConstructionReadModel construction =
                model != null ? model.AssemblyConstruction : null;
            if (construction == null ||
                !construction.HasActiveOrder ||
                !construction.AwaitingMaterials)
            {
                return;
            }

            string orderID = !string.IsNullOrEmpty(construction.OrderID)
                ? construction.OrderID
                : "active";
            ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                "assembly:awaiting-materials:" + orderID,
                ShuttleIssueSeverity.Warning,
                ShuttleIssueCategory.Assembly,
                "CT_Shuttle_Issue_AssemblyAwaitingMaterials_Title",
                "CT_Shuttle_Issue_AssemblyAwaitingMaterials_Detail",
                ShuttleIssueActionKind.OpenAssembly,
                50);
            if (!string.IsNullOrEmpty(construction.TargetLabel))
            {
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Target",
                    construction.TargetLabel));
            }

            if (!string.IsNullOrEmpty(construction.MaterialSummary))
            {
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Materials",
                    construction.MaterialSummary));
            }

            ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
        }

        private void AddDisabledRequiredModuleIssues(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || model.SegmentSlots == null || issues == null)
            {
                return;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                    if (module == null ||
                        !module.IsRequired ||
                        string.IsNullOrEmpty(module.InstalledModuleInstanceID) ||
                        module.InstalledModuleEnabled)
                    {
                        continue;
                    }

                    ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                        "assembly:required-module-disabled:" + module.SlotID,
                        ShuttleIssueSeverity.Warning,
                        ShuttleIssueCategory.Assembly,
                        "CT_Shuttle_Issue_RequiredModuleDisabled_Title",
                        "CT_Shuttle_Issue_RequiredModuleDisabled_Detail",
                        ShuttleIssueActionKind.OpenAssembly,
                        55);
                    issue.NavigationTarget = new ShuttleIssueNavigationTarget
                    {
                        Kind = ShuttleIssueNavigationTargetKind.ModuleSlot,
                        SegmentSlotID = segment.SlotID,
                        ModuleSlotID = module.SlotID
                    };
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Location",
                        ShuttleAssemblyDisplayTextResolver.ResolveModuleSlotDisplayName(
                            segment,
                            module,
                            false)));
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Module",
                        ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(module)));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                }
            }
        }
    }
}

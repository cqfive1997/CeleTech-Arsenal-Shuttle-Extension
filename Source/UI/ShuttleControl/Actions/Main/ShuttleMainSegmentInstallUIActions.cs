using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainSegmentInstallUIActions :
        IShuttleMainSegmentInstallUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Func<ShuttleAssemblyConstructionReadModel> getConstructionModel;
        private readonly Func<int> getResearchFingerprint;
        private readonly V3MainSegmentInstallCandidateBuilder candidateBuilder =
            new V3MainSegmentInstallCandidateBuilder();

        internal ShuttleMainSegmentInstallUIActions(
            IShuttleCommandExecutor commandExecutor,
            Func<ShuttleAssemblyConstructionReadModel> getConstructionModel,
            Func<int> getResearchFingerprint)
        {
            this.commandExecutor = commandExecutor;
            this.getConstructionModel = getConstructionModel;
            this.getResearchFingerprint = getResearchFingerprint;
        }

        public bool CanInstallSegment(ShuttleControlSegmentSlotModel segment)
        {
            return ShuttleMainInstallPolicy.CanRequestSegmentInstall(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment);
        }

        public void OpenInstallSegment(ShuttleControlSegmentSlotModel segment)
        {
            if (!this.CanInstallSegment(segment))
            {
                this.ShowReject(this.GetSegmentInstallTooltip(segment));
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleMainSegmentInstallV3(
                delegate
                {
                    return this.BuildCandidates(segment);
                },
                this.GetInstallCandidateFingerprint,
                delegate(V3MainInstallCandidateModel candidate)
                {
                    this.InstallSelectedCandidate(segment, candidate);
                }));
        }

        public string GetSegmentInstallTooltip(ShuttleControlSegmentSlotModel segment)
        {
            bool canInstall = this.CanInstallSegment(segment);
            return ShuttleMainInstallText.GetSegmentInstallTooltip(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                canInstall);
        }

        public bool CanReplaceSegment(ShuttleControlSegmentSlotModel segment)
        {
            return ShuttleMainInstallPolicy.CanRequestSegmentReplace(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment);
        }

        public void OpenReplaceSegment(ShuttleControlSegmentSlotModel segment)
        {
            if (!this.CanReplaceSegment(segment))
            {
                this.ShowReject(this.GetSegmentReplaceTooltip(segment));
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleMainSegmentInstallV3(
                delegate
                {
                    return this.BuildReplacementCandidates(segment);
                },
                this.GetInstallCandidateFingerprint,
                delegate(V3MainInstallCandidateModel candidate)
                {
                    this.ReplaceSelectedCandidate(segment, candidate);
                },
                true));
        }

        public string GetSegmentReplaceTooltip(ShuttleControlSegmentSlotModel segment)
        {
            bool canReplace = this.CanReplaceSegment(segment);
            return ShuttleMainInstallText.GetSegmentReplaceTooltip(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                canReplace);
        }

        private List<V3MainInstallCandidateModel> BuildCandidates(
            ShuttleControlSegmentSlotModel segment)
        {
            return this.candidateBuilder.BuildCandidates(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment);
        }

        private List<V3MainInstallCandidateModel> BuildReplacementCandidates(
            ShuttleControlSegmentSlotModel segment)
        {
            return this.candidateBuilder.BuildCandidates(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                true);
        }

        private void InstallSelectedCandidate(
            ShuttleControlSegmentSlotModel segment,
            V3MainInstallCandidateModel candidate)
        {
            if (segment == null || candidate == null || string.IsNullOrEmpty(candidate.Id))
            {
                return;
            }

            ShuttleSegmentBaseDef segmentDef =
                DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(candidate.Id);
            if (!ShuttleMainInstallPolicy.CanInstallSegmentDefIntoSlot(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                segmentDef))
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_UI_CannotInstallSegment"));
                return;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new BeginSegmentConstructionCommand(segment.SlotID, segmentDef.defName));
            this.ShowResult(result);
        }

        private void ReplaceSelectedCandidate(
            ShuttleControlSegmentSlotModel segment,
            V3MainInstallCandidateModel candidate)
        {
            if (segment == null || candidate == null || string.IsNullOrEmpty(candidate.Id))
            {
                return;
            }

            ShuttleSegmentBaseDef segmentDef =
                DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(candidate.Id);
            if (!ShuttleMainInstallPolicy.CanReplaceSegmentDefIntoSlot(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                segmentDef))
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_UI_CannotReplaceSegment"));
                return;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new BeginSegmentReplacementConstructionCommand(segment.SlotID, segmentDef.defName));
            this.ShowResult(result);
        }

        private bool HasActiveConstructionOrder()
        {
            ShuttleAssemblyConstructionReadModel model =
                this.getConstructionModel != null ? this.getConstructionModel() : null;
            return model != null && model.HasActiveOrder;
        }

        private int GetInstallCandidateFingerprint()
        {
            unchecked
            {
                int fingerprint = this.getResearchFingerprint != null
                    ? this.getResearchFingerprint()
                    : 0;
                ShuttleAssemblyConstructionReadModel model =
                    this.getConstructionModel != null ? this.getConstructionModel() : null;
                fingerprint = (fingerprint * 31) +
                    (model != null && model.HasActiveOrder ? 1 : 0);
                if (model != null && !string.IsNullOrEmpty(model.OrderID))
                {
                    fingerprint = (fingerprint * 31) + model.OrderID.GetHashCode();
                }

                return fingerprint;
            }
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result, MessageTypeDefOf.PositiveEvent);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message, false);
        }
    }
}

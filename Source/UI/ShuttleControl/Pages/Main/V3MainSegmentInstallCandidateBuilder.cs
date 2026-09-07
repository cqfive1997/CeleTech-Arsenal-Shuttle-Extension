using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainSegmentInstallCandidateBuilder
    {
        internal List<V3MainInstallCandidateModel> BuildCandidates(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment)
        {
            return this.BuildCandidates(
                commandAvailable,
                hasActiveConstructionOrder,
                segment,
                false);
        }

        internal List<V3MainInstallCandidateModel> BuildCandidates(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            bool replacing)
        {
            List<V3MainInstallCandidateModel> candidates =
                new List<V3MainInstallCandidateModel>();
            List<ShuttleSegmentBaseDef> defs =
                DefDatabase<ShuttleSegmentBaseDef>.AllDefsListForReading;
            for (int i = 0; defs != null && i < defs.Count; i++)
            {
                ShuttleSegmentBaseDef segmentDef = defs[i];
                if (segmentDef == null || !segmentDef.playerInstallable)
                {
                    continue;
                }

                V3MainInstallCandidateModel candidate =
                    this.CreateCandidate(segmentDef);
                bool compatible = this.IsCompatible(
                    commandAvailable,
                    hasActiveConstructionOrder,
                    segment,
                    segmentDef,
                    replacing);
                bool unlocked = ShuttleResearchGateUtility.IsUnlocked(segmentDef);
                bool requestAllowed = replacing
                    ? ShuttleMainInstallPolicy.CanRequestSegmentReplace(
                        commandAvailable,
                        hasActiveConstructionOrder,
                        segment)
                    : ShuttleMainInstallPolicy.CanRequestSegmentInstall(
                        commandAvailable,
                        hasActiveConstructionOrder,
                        segment);

                candidate.IsCompatible = compatible;
                candidate.IsUnlocked = unlocked;
                if (!compatible)
                {
                    candidate.Status = V3MainInstallCandidateStatus.Incompatible;
                    candidate.StatusTranslateKey =
                        "CT_Shuttle_UI_SegmentInstall_Status_Incompatible";
                }
                else if (!unlocked)
                {
                    candidate.Status = V3MainInstallCandidateStatus.Locked;
                    candidate.StatusTranslateKey =
                        "CT_Shuttle_UI_SegmentInstall_Status_Locked";
                    candidate.DisabledReason =
                        ShuttleResearchGateUtility.GetUILockedReason(segmentDef);
                }
                else
                {
                    candidate.Status = V3MainInstallCandidateStatus.Available;
                    candidate.StatusTranslateKey =
                        "CT_Shuttle_UI_SegmentInstall_Status_Available";
                }

                candidate.CanInstall = requestAllowed &&
                    compatible &&
                    unlocked;
                candidates.Add(candidate);
            }

            candidates.Sort(CompareCandidates);
            return candidates;
        }

        private bool IsCompatible(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef,
            bool replacing)
        {
            if (replacing)
            {
                return ShuttleMainInstallPolicy.CanReplaceSegmentDefIntoSlot(
                    commandAvailable,
                    hasActiveConstructionOrder,
                    segment,
                    segmentDef,
                    true);
            }

            return ShuttleMainInstallPolicy.CanInstallSegmentDefIntoSlot(
                commandAvailable,
                hasActiveConstructionOrder,
                segment,
                segmentDef,
                true);
        }

        private V3MainInstallCandidateModel CreateCandidate(
            ShuttleSegmentBaseDef segmentDef)
        {
            V3MainInstallCandidateModel candidate =
                new V3MainInstallCandidateModel();
            candidate.Id = segmentDef != null ? segmentDef.defName : null;
            candidate.DisplayLabel = V3MainInstallCandidateText.GetDefLabel(segmentDef);
            candidate.DisplayDescription =
                V3MainInstallCandidateText.GetDefDescription(segmentDef);
            candidate.ConstructionCostSummary =
                V3MainInstallCandidateText.BuildSegmentConstructionCostSummary(
                    segmentDef);
            return candidate;
        }

        private static int CompareCandidates(
            V3MainInstallCandidateModel left,
            V3MainInstallCandidateModel right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int statusCompare = left.Status.CompareTo(right.Status);
            return statusCompare != 0
                ? statusCompare
                : string.Compare(
                    left.DisplayLabel,
                    right.DisplayLabel,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}

using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainModuleInstallCandidateBuilder
    {
        private readonly IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityPort;

        internal V3MainModuleInstallCandidateBuilder(
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityPort)
        {
            this.moduleInstallEligibilityPort = moduleInstallEligibilityPort;
        }

        internal List<V3MainInstallCandidateModel> BuildCandidates(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool replacing)
        {
            List<V3MainInstallCandidateModel> candidates =
                new List<V3MainInstallCandidateModel>();
            List<ShuttleModuleBaseDef> defs =
                DefDatabase<ShuttleModuleBaseDef>.AllDefsListForReading;
            for (int i = 0; defs != null && i < defs.Count; i++)
            {
                ShuttleModuleBaseDef moduleDef = defs[i];
                if (moduleDef == null || !moduleDef.playerInstallable)
                {
                    continue;
                }

                if (this.IsSameInstalledModule(moduleDef, moduleSlot, replacing))
                {
                    continue;
                }

                string failureReason;
                if (!ShuttleMainInstallPolicy.CanInstallModuleDefIntoSlot(
                    this.moduleInstallEligibilityPort,
                    segment,
                    moduleSlot,
                    moduleDef,
                    replacing,
                    out failureReason))
                {
                    continue;
                }

                bool requestAllowed = replacing
                    ? ShuttleMainInstallPolicy.CanRequestModuleReplace(
                        commandAvailable,
                        hasActiveConstructionOrder,
                        moduleSlot)
                    : ShuttleMainInstallPolicy.CanRequestModuleInstall(
                        commandAvailable,
                        hasActiveConstructionOrder,
                        moduleSlot);
                if (!requestAllowed)
                {
                    continue;
                }

                V3MainInstallCandidateModel candidate =
                    this.CreateCandidate(moduleDef);
                candidate.IsUnlocked = true;
                candidate.IsCompatible = true;
                candidate.CanInstall = true;
                candidate.Status = V3MainInstallCandidateStatus.Available;
                candidate.StatusTranslateKey =
                    "CT_Shuttle_UI_ModuleReplace_Status_Available";
                candidates.Add(candidate);
            }

            candidates.Sort(CompareCandidates);
            return candidates;
        }

        private bool IsSameInstalledModule(
            ShuttleModuleBaseDef moduleDef,
            ShuttleControlModuleSlotModel moduleSlot,
            bool replacing)
        {
            return replacing &&
                moduleDef != null &&
                moduleSlot != null &&
                !string.IsNullOrEmpty(moduleSlot.InstalledModuleDefName) &&
                moduleDef.defName == moduleSlot.InstalledModuleDefName;
        }

        private V3MainInstallCandidateModel CreateCandidate(
            ShuttleModuleBaseDef moduleDef)
        {
            V3MainInstallCandidateModel candidate =
                new V3MainInstallCandidateModel();
            candidate.Id = moduleDef != null ? moduleDef.defName : null;
            candidate.DisplayLabel =
                V3MainInstallCandidateText.ResolveModuleDisplayLabel(moduleDef);
            candidate.DisplayDescription =
                V3MainInstallCandidateText.ResolveModuleDisplayDescription(moduleDef);
            candidate.ConstructionCostSummary =
                V3MainInstallCandidateText.BuildModuleConstructionCostSummary(
                    moduleDef);
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

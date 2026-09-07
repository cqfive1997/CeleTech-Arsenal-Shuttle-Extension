using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleControlChecklistReadModelBuilder
    {
        internal void AddIssues(ShuttleControlReadModel model, ShuttleProfile profile)
        {
            if (model == null || profile == null || profile.Issues == null)
            {
                return;
            }

            for (int i = 0; i < profile.Issues.Count; i++)
            {
                ProfileBuildIssue issue = profile.Issues[i];
                if (issue == null)
                {
                    continue;
                }

                ShuttleControlIssueModel issueModel = new ShuttleControlIssueModel();
                issueModel.Code = issue.Code;
                issueModel.Message = issue.Message;
                issueModel.Severity = issue.Severity.ToString();
                issueModel.Scope = issue.Scope.ToString();
                issueModel.ReferenceID = issue.ReferenceID;
                model.Issues.Add(issueModel);
            }
        }

        internal void AddAssemblyReadinessIssues(
            ShuttleControlReadModel model,
            IReadOnlyList<ProfileBuildIssue> readinessIssues)
        {
            if (model == null || readinessIssues == null)
            {
                return;
            }

            for (int i = 0; i < readinessIssues.Count; i++)
            {
                ProfileBuildIssue issue = readinessIssues[i];
                if (issue == null ||
                    issue.Code != ShuttleAssemblyIntegrityReport.BlockingPlayerIssueCode ||
                    this.HasIssue(model, issue.Code, issue.ReferenceID))
                {
                    continue;
                }

                ShuttleControlIssueModel issueModel = new ShuttleControlIssueModel();
                issueModel.Code = issue.Code;
                issueModel.Message = issue.Message;
                issueModel.Severity = issue.Severity.ToString();
                issueModel.Scope = issue.Scope.ToString();
                issueModel.ReferenceID = issue.ReferenceID;
                model.Issues.Add(issueModel);
            }
        }

        internal void AddLaunchChecklist(
            ShuttleControlReadModel model,
            IReadOnlyList<ProfileBuildIssue> readinessIssues)
        {
            if (model == null)
            {
                return;
            }

            if (model.LaunchChecklist == null)
            {
                model.LaunchChecklist = new List<ShuttleLaunchChecklistItemModel>();
            }

            if (readinessIssues == null || readinessIssues.Count == 0)
            {
                return;
            }

            for (int i = 0; i < readinessIssues.Count; i++)
            {
                ProfileBuildIssue issue = readinessIssues[i];
                if (issue == null)
                {
                    continue;
                }

                ShuttleLaunchChecklistItemModel item = new ShuttleLaunchChecklistItemModel();
                item.Code = issue.Code;
                item.Message = this.BuildLaunchChecklistPlayerMessage(model, issue);
                item.Severity = issue.Severity.ToString();
                item.Category = this.GetLaunchChecklistCategory(issue);
                item.ReferenceID = issue.ReferenceID;
                item.BlocksLaunch = issue.Severity == ProfileBuildIssueSeverity.Error;
                item.IsRiskOnly = issue.Severity == ProfileBuildIssueSeverity.Warning;
                item.DisplayName = this.ResolveReferencePlayerLabel(model, issue.ReferenceID);
                item.DisplayNameKey = this.ResolveReferenceDisplayNameKey(model, issue.ReferenceID);
                this.ApplyLaunchChecklistMessageMetadata(item, issue);
                item.Progress01 = -1f;
                item.SortPriority = this.GetLaunchChecklistSortPriority(item);
                item.Tooltip = Prefs.DevMode
                    ? this.BuildDiagnosticTooltip(
                        issue.Code,
                        issue.Severity.ToString(),
                        issue.Scope.ToString(),
                        issue.ReferenceID)
                    : null;
                model.LaunchChecklist.Add(item);
            }

            model.LaunchChecklist.Sort(CompareLaunchChecklistItems);
        }

        private bool HasIssue(
            ShuttleControlReadModel model,
            string code,
            string referenceID)
        {
            if (model == null || model.Issues == null)
            {
                return false;
            }

            for (int i = 0; i < model.Issues.Count; i++)
            {
                ShuttleControlIssueModel existing = model.Issues[i];
                if (existing != null &&
                    existing.Code == code &&
                    existing.ReferenceID == referenceID)
                {
                    return true;
                }
            }

            return false;
        }

        internal void AddDeveloperDiagnostics(
            ShuttleControlReadModel model,
            ShuttleProfile profile,
            IReadOnlyList<ShuttleDeveloperDiagnosticModel> runtimeDiagnostics)
        {
            if (model == null)
            {
                return;
            }

            if (model.DeveloperDiagnostics == null)
            {
                model.DeveloperDiagnostics = new List<ShuttleDeveloperDiagnosticModel>();
            }

            int tick = ShuttleTickUtility.TicksGameOrMinusOne();
            if (profile != null && profile.Issues != null)
            {
                for (int i = 0; i < profile.Issues.Count; i++)
                {
                    if (model.DeveloperDiagnostics.Count >= ShuttleControlReadModel.DeveloperDiagnosticsMax)
                    {
                        break;
                    }

                    ProfileBuildIssue issue = profile.Issues[i];
                    if (issue == null)
                    {
                        continue;
                    }

                    ShuttleDeveloperDiagnosticModel diagnostic = new ShuttleDeveloperDiagnosticModel();
                    diagnostic.Code = issue.Code;
                    diagnostic.Message = issue.Message;
                    diagnostic.Kind = "Issue";
                    diagnostic.Severity = issue.Severity.ToString();
                    diagnostic.Scope = issue.Scope.ToString();
                    diagnostic.ReferenceID = issue.ReferenceID;
                    diagnostic.Tick = tick;
                    diagnostic.Tooltip = this.BuildDiagnosticTooltip(
                        issue.Code,
                        issue.Severity.ToString(),
                        issue.Scope.ToString(),
                        issue.ReferenceID);
                    model.DeveloperDiagnostics.Add(diagnostic);
                }
            }

            if (runtimeDiagnostics != null)
            {
                for (int i = 0; i < runtimeDiagnostics.Count; i++)
                {
                    if (model.DeveloperDiagnostics.Count >= ShuttleControlReadModel.DeveloperDiagnosticsMax)
                    {
                        break;
                    }

                    ShuttleDeveloperDiagnosticModel diagnostic = runtimeDiagnostics[i];
                    if (diagnostic != null)
                    {
                        model.DeveloperDiagnostics.Add(diagnostic);
                    }
                }
            }
        }

        private string BuildLaunchChecklistPlayerMessage(
            ShuttleControlReadModel model,
            ProfileBuildIssue issue)
        {
            if (issue == null)
            {
                return "-";
            }

            string code = issue.Code ?? string.Empty;
            if (code == "required-segment-slot-empty")
            {
                return "CT_Shuttle_Issue_RequiredSegmentSlotEmpty"
                    .Translate(ShuttleControlDisplayNameResolver.ResolveSegmentSlotLabel(model, issue.ReferenceID))
                    .ToString();
            }

            if (code == "required-module-slot-empty" ||
                code == "required-module-slot-missing")
            {
                return "CT_Shuttle_Issue_RequiredModuleSlotEmpty"
                    .Translate(ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(model, issue.ReferenceID))
                    .ToString();
            }

            if (code == "optional-module-slot-missing")
            {
                return "CT_Shuttle_InfoPanel_OptionalModuleSlotMissing_Player"
                    .Translate(this.ResolveModuleSlotPlayerLabel(model, issue.ReferenceID))
                    .ToString();
            }

            return this.SanitizePlayerIssueMessage(model, issue.Message, issue.ReferenceID);
        }

        private void ApplyLaunchChecklistMessageMetadata(
            ShuttleLaunchChecklistItemModel item,
            ProfileBuildIssue issue)
        {
            if (item == null || issue == null)
            {
                return;
            }

            if (issue.Code == "required-segment-slot-empty")
            {
                item.MessageKey = "CT_Shuttle_Issue_RequiredSegmentSlotEmpty";
            }
            else if (issue.Code == "required-module-slot-empty" ||
                issue.Code == "required-module-slot-missing")
            {
                item.MessageKey = "CT_Shuttle_Issue_RequiredModuleSlotEmpty";
            }

            if (!string.IsNullOrEmpty(item.DisplayName))
            {
                item.MessageArgs = new List<string>();
                item.MessageArgs.Add(item.DisplayName);
            }
        }

        private string SanitizePlayerIssueMessage(
            ShuttleControlReadModel model,
            string rawMessage,
            string referenceID)
        {
            string message = string.IsNullOrEmpty(rawMessage) ? "-" : rawMessage;
            List<PlayerTextReplacement> replacements = this.BuildPlayerTextReplacements(model);
            replacements.Sort(delegate(PlayerTextReplacement left, PlayerTextReplacement right)
            {
                int leftLength = left.RawID != null ? left.RawID.Length : 0;
                int rightLength = right.RawID != null ? right.RawID.Length : 0;
                return rightLength.CompareTo(leftLength);
            });

            for (int i = 0; i < replacements.Count; i++)
            {
                PlayerTextReplacement replacement = replacements[i];
                if (replacement == null ||
                    string.IsNullOrEmpty(replacement.RawID) ||
                    string.IsNullOrEmpty(replacement.DisplayName))
                {
                    continue;
                }

                message = message.Replace(replacement.RawID, replacement.DisplayName);
                message = message.Replace(replacement.RawID.ToLowerInvariant(), replacement.DisplayName);
            }

            if (!string.IsNullOrEmpty(referenceID) && message == referenceID)
            {
                string display = this.ResolveReferencePlayerLabel(model, referenceID);
                if (!string.IsNullOrEmpty(display))
                {
                    message = display;
                }
            }

            return message;
        }

        private List<PlayerTextReplacement> BuildPlayerTextReplacements(ShuttleControlReadModel model)
        {
            List<PlayerTextReplacement> replacements = new List<PlayerTextReplacement>();
            if (model == null || model.SegmentSlots == null)
            {
                return replacements;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                if (segment == null)
                {
                    continue;
                }

                string segmentLabel = this.ResolveSegmentSlotPlayerLabel(model, segment.SlotID);
                this.AddPlayerTextReplacement(replacements, segment.SlotID, segmentLabel);
                this.AddPlayerTextReplacement(replacements, segment.InstalledSegmentInstanceID, segmentLabel);
                this.AddPlayerTextReplacement(replacements, segment.InstalledSegmentDefName, segmentLabel);

                if (segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                    if (module == null)
                    {
                        continue;
                    }

                    string moduleSlotLabel = this.ResolveModuleSlotPlayerLabel(segment, module, null);
                    string moduleLabel = ShuttleControlDisplayNameResolver.ResolveModuleLabel(module);
                    if (string.IsNullOrEmpty(moduleLabel))
                    {
                        moduleLabel = moduleSlotLabel;
                    }
                    this.AddPlayerTextReplacement(replacements, module.SlotID, moduleSlotLabel);
                    this.AddPlayerTextReplacement(replacements, module.InstalledModuleInstanceID, moduleLabel);
                    this.AddPlayerTextReplacement(replacements, module.InstalledModuleDefName, moduleLabel);
                }
            }

            return replacements;
        }

        private void AddPlayerTextReplacement(
            List<PlayerTextReplacement> replacements,
            string rawID,
            string displayName)
        {
            if (string.IsNullOrEmpty(rawID) ||
                string.IsNullOrEmpty(displayName) ||
                rawID == displayName)
            {
                return;
            }

            replacements.Add(new PlayerTextReplacement(rawID, displayName));
        }

        private string GetLaunchChecklistCategory(ProfileBuildIssue issue)
        {
            if (issue == null)
            {
                return "Runtime";
            }

            string code = issue.Code ?? string.Empty;
            if (code.Contains("cockpit"))
            {
                return "Crew";
            }

            if (code.Contains("cargo") || code.Contains("transporter"))
            {
                return "Cargo";
            }

            if (code.Contains("medical"))
            {
                return "Medical";
            }

            if (code.Contains("mech"))
            {
                return "Mech";
            }

            if (code.Contains("prison"))
            {
                return "Prison";
            }

            if (code.Contains("power") || code.Contains("energy") || code.Contains("charge"))
            {
                return "Power";
            }

            return issue.Scope.ToString();
        }

        private string ResolveReferencePlayerLabel(
            ShuttleControlReadModel model,
            string referenceID)
        {
            if (string.IsNullOrEmpty(referenceID))
            {
                return null;
            }

            if (model != null && model.FindSegmentSlot(referenceID) != null)
            {
                return ShuttleControlDisplayNameResolver.ResolveSegmentSlotLabel(model, referenceID);
            }

            string moduleLabel = ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(model, referenceID);
            if (!string.IsNullOrEmpty(moduleLabel))
            {
                return moduleLabel;
            }

            return ShuttleControlDisplayNameResolver.ResolveSegmentSlotLabel(model, referenceID);
        }

        private string ResolveReferenceDisplayNameKey(
            ShuttleControlReadModel model,
            string referenceID)
        {
            if (model == null || model.SegmentSlots == null || string.IsNullOrEmpty(referenceID))
            {
                return null;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                if (segment == null)
                {
                    continue;
                }

                if (referenceID == segment.SlotID ||
                    referenceID == segment.InstalledSegmentInstanceID ||
                    referenceID == segment.InstalledSegmentDefName)
                {
                    return segment.DisplayLabelKey;
                }

                if (segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                    if (module != null && referenceID == module.SlotID)
                    {
                        return module.DisplayLabelKey;
                    }
                }
            }

            return null;
        }

        private string ResolveSegmentSlotPlayerLabel(
            ShuttleControlReadModel model,
            string referenceID)
        {
            return ShuttleControlDisplayNameResolver.ResolveSegmentSlotLabel(model, referenceID);
        }

        private string ResolveModuleSlotPlayerLabel(
            ShuttleControlReadModel model,
            string referenceID)
        {
            return ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(model, referenceID);
        }

        private string ResolveModuleSlotPlayerLabel(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel module,
            string fallbackSlotID)
        {
            return module != null
                ? ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(segment, module)
                : ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(null, fallbackSlotID);
        }

        private int GetLaunchChecklistSortPriority(ShuttleLaunchChecklistItemModel item)
        {
            if (item == null)
            {
                return 10000;
            }

            int basePriority = item.BlocksLaunch ? 0 : item.IsRiskOnly ? 1000 : 2000;
            if (item.Category == "Crew")
            {
                return basePriority + 10;
            }

            if (item.Category == "Power")
            {
                return basePriority + 20;
            }

            if (item.Category == "Cargo")
            {
                return basePriority + 30;
            }

            if (item.Category == "Medical" || item.Category == "Prison" || item.Category == "Mech")
            {
                return basePriority + 40;
            }

            return basePriority + 100;
        }

        private string BuildDiagnosticTooltip(
            string code,
            string severity,
            string scope,
            string referenceID)
        {
            string tooltip = string.Empty;
            if (!string.IsNullOrEmpty(code))
            {
                tooltip += "Code: " + code;
            }

            if (!string.IsNullOrEmpty(severity))
            {
                tooltip += (tooltip.Length > 0 ? "\n" : string.Empty) + "Severity: " + severity;
            }

            if (!string.IsNullOrEmpty(scope))
            {
                tooltip += (tooltip.Length > 0 ? "\n" : string.Empty) + "Scope: " + scope;
            }

            if (!string.IsNullOrEmpty(referenceID))
            {
                tooltip += (tooltip.Length > 0 ? "\n" : string.Empty) + "Reference: " + referenceID;
            }

            return tooltip.Length > 0 ? tooltip : null;
        }

        private static int CompareLaunchChecklistItems(
            ShuttleLaunchChecklistItemModel left,
            ShuttleLaunchChecklistItemModel right)
        {
            int leftPriority = left != null ? left.SortPriority : 10000;
            int rightPriority = right != null ? right.SortPriority : 10000;
            int priorityCompare = leftPriority.CompareTo(rightPriority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            string leftCode = left != null ? left.Code : null;
            string rightCode = right != null ? right.Code : null;
            return string.CompareOrdinal(leftCode, rightCode);
        }

        private sealed class PlayerTextReplacement
        {
            internal readonly string RawID;
            internal readonly string DisplayName;

            internal PlayerTextReplacement(string rawID, string displayName)
            {
                this.RawID = rawID;
                this.DisplayName = displayName;
            }
        }
    }
}

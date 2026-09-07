using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleAssemblyTopologyIndexBuilder
    {
        internal ShuttleAssemblyTopologyIndex Build(
            ShuttleAssemblyState assemblyState,
            List<ProfileBuildIssue> issues,
            ShuttleReadinessIssueFactory issueFactory)
        {
            ShuttleAssemblyTopologyIndex topology = new ShuttleAssemblyTopologyIndex();
            if (assemblyState == null)
            {
                return topology;
            }

            this.IndexSegmentSlots(assemblyState, topology, issues, issueFactory);
            this.IndexSegments(assemblyState, topology, issues, issueFactory);
            this.IndexModules(assemblyState, topology, issues, issueFactory);
            return topology;
        }

        private void IndexSegmentSlots(
            ShuttleAssemblyState assemblyState,
            ShuttleAssemblyTopologyIndex topology,
            List<ProfileBuildIssue> issues,
            ShuttleReadinessIssueFactory issueFactory)
        {
            if (assemblyState.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < assemblyState.SegmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = assemblyState.SegmentSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(slot.SlotID))
                {
                    issueFactory.AddIssue(
                        issues,
                        "segment-slot-id-missing",
                        "CT_Shuttle_Issue_SegmentSlotIDMissing".Translate().ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        null);
                }
                else
                {
                    this.AddIDReference(topology.SegmentSlotIDReferences, slot.SlotID, slot.SlotID);
                }

                if (!string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
                {
                    this.AddIDReference(
                        topology.SegmentSlotReferences,
                        slot.InstalledSegmentInstanceID,
                        !string.IsNullOrEmpty(slot.SlotID) ? slot.SlotID : "unknown-segment-slot");
                }
            }

            issueFactory.ValidateDuplicateIDReferences(
                topology.SegmentSlotIDReferences,
                "segment-slot-id-duplicate",
                "CT_Shuttle_Issue_DuplicateSegmentSlotID",
                null,
                ProfileBuildIssueScope.Segment,
                issues);
        }

        private void IndexSegments(
            ShuttleAssemblyState assemblyState,
            ShuttleAssemblyTopologyIndex topology,
            List<ProfileBuildIssue> issues,
            ShuttleReadinessIssueFactory issueFactory)
        {
            if (assemblyState.Segments == null)
            {
                return;
            }

            for (int i = 0; i < assemblyState.Segments.Count; i++)
            {
                ShuttleSegment segment = assemblyState.Segments[i];
                if (segment == null)
                {
                    continue;
                }

                string segmentID = segment.SegmentInstanceID;
                if (string.IsNullOrEmpty(segmentID))
                {
                    issueFactory.AddIssue(
                        issues,
                        "segment-instance-id-missing",
                        "CT_Shuttle_Issue_SegmentInstanceIDMissing".Translate().ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        null);
                }
                else
                {
                    this.AddIDReference(topology.SegmentIDReferences, segmentID, segmentID);
                    if (!topology.FirstSegmentByID.ContainsKey(segmentID))
                    {
                        topology.FirstSegmentByID.Add(segmentID, segment);
                    }
                }

                this.IndexModuleSlots(segment, topology, issues, issueFactory);
            }

            issueFactory.ValidateDuplicateIDReferences(
                topology.SegmentIDReferences,
                "segment-instance-id-duplicate",
                "CT_Shuttle_Issue_DuplicateSegmentInstanceID",
                null,
                ProfileBuildIssueScope.Segment,
                issues);
        }

        private void IndexModuleSlots(
            ShuttleSegment segment,
            ShuttleAssemblyTopologyIndex topology,
            List<ProfileBuildIssue> issues,
            ShuttleReadinessIssueFactory issueFactory)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return;
            }

            string segmentID = !string.IsNullOrEmpty(segment.SegmentInstanceID)
                ? segment.SegmentInstanceID
                : "unknown-segment";
            Dictionary<string, List<string>> slotIDs = new Dictionary<string, List<string>>();

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot == null)
                {
                    continue;
                }

                string slotReference = segmentID + "/" + (!string.IsNullOrEmpty(slot.SlotID) ? slot.SlotID : "unknown-module-slot");
                if (string.IsNullOrEmpty(slot.SlotID))
                {
                    issueFactory.AddIssue(
                        issues,
                        "module-slot-id-missing",
                        "CT_Shuttle_Issue_ModuleSlotIDMissing".Translate(segmentID).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        segment.SegmentInstanceID);
                }
                else
                {
                    this.AddIDReference(slotIDs, slot.SlotID, slotReference);
                }

                if (!string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                {
                    this.AddIDReference(
                        topology.ModuleSlotReferences,
                        slot.InstalledModuleInstanceID,
                        slotReference);
                }
            }

            issueFactory.ValidateDuplicateIDReferences(
                slotIDs,
                "module-slot-id-duplicate",
                "CT_Shuttle_Issue_DuplicateModuleSlotID",
                segmentID,
                ProfileBuildIssueScope.Module,
                issues);
        }

        private void IndexModules(
            ShuttleAssemblyState assemblyState,
            ShuttleAssemblyTopologyIndex topology,
            List<ProfileBuildIssue> issues,
            ShuttleReadinessIssueFactory issueFactory)
        {
            if (assemblyState.Modules == null)
            {
                return;
            }

            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                if (module == null)
                {
                    continue;
                }

                string moduleID = module.ModuleInstanceID;
                if (string.IsNullOrEmpty(moduleID))
                {
                    issueFactory.AddIssue(
                        issues,
                        "module-instance-id-missing",
                        "CT_Shuttle_Issue_ModuleInstanceIDMissing".Translate().ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        null);
                }
                else
                {
                    this.AddIDReference(topology.ModuleIDReferences, moduleID, moduleID);
                    if (!topology.FirstModuleByID.ContainsKey(moduleID))
                    {
                        topology.FirstModuleByID.Add(moduleID, module);
                    }
                }
            }

            issueFactory.ValidateDuplicateIDReferences(
                topology.ModuleIDReferences,
                "module-instance-id-duplicate",
                "CT_Shuttle_Issue_DuplicateModuleInstanceID",
                null,
                ProfileBuildIssueScope.Module,
                issues);
        }

        private void AddIDReference(Dictionary<string, List<string>> references, string id, string reference)
        {
            if (references == null || string.IsNullOrEmpty(id))
            {
                return;
            }

            List<string> existingReferences;
            if (!references.TryGetValue(id, out existingReferences))
            {
                existingReferences = new List<string>();
                references.Add(id, existingReferences);
            }

            existingReferences.Add(!string.IsNullOrEmpty(reference) ? reference : "unknown-reference");
        }
    }
}

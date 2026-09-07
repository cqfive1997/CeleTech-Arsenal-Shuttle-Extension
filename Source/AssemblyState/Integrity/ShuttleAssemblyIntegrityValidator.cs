using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleAssemblyIntegrityValidator
    {
        internal ShuttleAssemblyIntegrityReport Validate(ShuttleAssemblyState state)
        {
            List<ShuttleAssemblyIntegrityIssue> issues =
                new List<ShuttleAssemblyIntegrityIssue>();
            if (state == null)
            {
                this.AddIssue(
                    issues,
                    "assembly-integrity-state-missing",
                    "assembly",
                    "AssemblyState is null.");
                return new ShuttleAssemblyIntegrityReport(issues);
            }

            Dictionary<string, ShuttleSegment> firstSegmentByID =
                new Dictionary<string, ShuttleSegment>();
            Dictionary<string, int> segmentCountsByID =
                new Dictionary<string, int>();
            Dictionary<int, ShuttleSegmentSlot> segmentSlotsByIndex =
                new Dictionary<int, ShuttleSegmentSlot>();
            Dictionary<string, int> segmentReferencesByID =
                new Dictionary<string, int>();

            Dictionary<string, ShuttleModule> firstModuleByID =
                new Dictionary<string, ShuttleModule>();
            Dictionary<string, int> moduleCountsByID =
                new Dictionary<string, int>();
            Dictionary<string, int> moduleReferencesByID =
                new Dictionary<string, int>();
            Dictionary<string, ModuleSlotContext> moduleSlotsByParentKey =
                new Dictionary<string, ModuleSlotContext>();
            Dictionary<string, ShuttleModule> firstModuleByParentKey =
                new Dictionary<string, ShuttleModule>();
            Dictionary<string, int> moduleParentCounts =
                new Dictionary<string, int>();

            this.IndexSegments(state, issues, firstSegmentByID, segmentCountsByID);
            this.IndexModules(
                state,
                issues,
                firstModuleByID,
                moduleCountsByID,
                firstModuleByParentKey,
                moduleParentCounts);
            this.ValidateSegmentSlots(
                state,
                issues,
                firstSegmentByID,
                segmentSlotsByIndex,
                segmentReferencesByID);
            this.ValidateModuleSlots(
                state,
                issues,
                firstModuleByID,
                moduleSlotsByParentKey,
                moduleReferencesByID);
            this.ValidateSegments(
                state,
                issues,
                segmentReferencesByID,
                segmentSlotsByIndex);
            this.ValidateModules(
                state,
                issues,
                moduleReferencesByID,
                moduleSlotsByParentKey,
                firstModuleByParentKey,
                moduleParentCounts);

            return new ShuttleAssemblyIntegrityReport(issues);
        }

        private void IndexSegments(
            ShuttleAssemblyState state,
            List<ShuttleAssemblyIntegrityIssue> issues,
            Dictionary<string, ShuttleSegment> firstSegmentByID,
            Dictionary<string, int> segmentCountsByID)
        {
            if (state.Segments == null)
            {
                return;
            }

            for (int i = 0; i < state.Segments.Count; i++)
            {
                ShuttleSegment segment = state.Segments[i];
                if (segment == null)
                {
                    continue;
                }

                string segmentID = segment.SegmentInstanceID;
                if (string.IsNullOrEmpty(segmentID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-id-missing",
                        "segments[" + i + "]",
                        "Segment object has no SegmentInstanceID.");
                    continue;
                }

                if (!ShuttleAssemblyInstanceIDRules.IsValidSegmentInstanceID(segmentID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-id-invalid",
                        segmentID,
                        "SegmentInstanceID must use a positive safe segment-N value.");
                    continue;
                }

                this.Increment(segmentCountsByID, segmentID);
                if (!firstSegmentByID.ContainsKey(segmentID))
                {
                    firstSegmentByID.Add(segmentID, segment);
                    continue;
                }

                this.AddIssue(
                    issues,
                    "assembly-integrity-segment-id-duplicate",
                    segmentID,
                    "Duplicate SegmentInstanceID found at segments[" + i + "].");
            }
        }

        private void IndexModules(
            ShuttleAssemblyState state,
            List<ShuttleAssemblyIntegrityIssue> issues,
            Dictionary<string, ShuttleModule> firstModuleByID,
            Dictionary<string, int> moduleCountsByID,
            Dictionary<string, ShuttleModule> firstModuleByParentKey,
            Dictionary<string, int> moduleParentCounts)
        {
            if (state.Modules == null)
            {
                return;
            }

            for (int i = 0; i < state.Modules.Count; i++)
            {
                ShuttleModule module = state.Modules[i];
                if (module == null)
                {
                    continue;
                }

                string moduleID = module.ModuleInstanceID;
                if (string.IsNullOrEmpty(moduleID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-id-missing",
                        "modules[" + i + "]",
                        "Module object has no ModuleInstanceID.");
                }
                else if (!ShuttleAssemblyInstanceIDRules.IsValidModuleInstanceID(moduleID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-id-invalid",
                        moduleID,
                        "ModuleInstanceID must use a positive safe module-N value.");
                }
                else
                {
                    this.Increment(moduleCountsByID, moduleID);
                    if (!firstModuleByID.ContainsKey(moduleID))
                    {
                        firstModuleByID.Add(moduleID, module);
                    }
                    else
                    {
                        this.AddIssue(
                            issues,
                            "assembly-integrity-module-id-duplicate",
                            moduleID,
                            "Duplicate ModuleInstanceID found at modules[" + i + "].");
                    }
                }

                if (!string.IsNullOrEmpty(module.ParentSegmentInstanceID) &&
                    !ShuttleAssemblyInstanceIDRules.IsValidSegmentInstanceID(module.ParentSegmentInstanceID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-parent-segment-id-invalid",
                        !string.IsNullOrEmpty(moduleID) ? moduleID : "modules[" + i + "]",
                        "Module parent segment instance ID must use a positive safe segment-N value.");
                }

                string parentKey = this.GetModuleSlotKey(
                    module.ParentSegmentInstanceID,
                    module.ParentSlotID);
                if (parentKey == null)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-parent-missing",
                        !string.IsNullOrEmpty(moduleID) ? moduleID : "modules[" + i + "]",
                        "Module object has no complete parent segment/slot reference.");
                    continue;
                }

                this.Increment(moduleParentCounts, parentKey);
                if (!firstModuleByParentKey.ContainsKey(parentKey))
                {
                    firstModuleByParentKey.Add(parentKey, module);
                }
                else
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-parent-duplicate",
                        parentKey,
                        "Multiple module objects reference the same parent segment/slot.");
                }
            }
        }

        private void ValidateSegmentSlots(
            ShuttleAssemblyState state,
            List<ShuttleAssemblyIntegrityIssue> issues,
            Dictionary<string, ShuttleSegment> firstSegmentByID,
            Dictionary<int, ShuttleSegmentSlot> segmentSlotsByIndex,
            Dictionary<string, int> segmentReferencesByID)
        {
            if (state.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < state.SegmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = state.SegmentSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (!segmentSlotsByIndex.ContainsKey(slot.SlotIndex))
                {
                    segmentSlotsByIndex.Add(slot.SlotIndex, slot);
                }

                if (string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
                {
                    continue;
                }

                if (!ShuttleAssemblyInstanceIDRules.IsValidSegmentInstanceID(slot.InstalledSegmentInstanceID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-reference-id-invalid",
                        slot.SlotID,
                        "Segment slot references invalid segment instance ID '" +
                        slot.InstalledSegmentInstanceID + "'.");
                    continue;
                }

                this.Increment(segmentReferencesByID, slot.InstalledSegmentInstanceID);
                if (!firstSegmentByID.ContainsKey(slot.InstalledSegmentInstanceID))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-reference-missing",
                        slot.SlotID,
                        "Segment slot points to missing segment instance '" +
                        slot.InstalledSegmentInstanceID + "'.");
                }
            }
        }

        private void ValidateModuleSlots(
            ShuttleAssemblyState state,
            List<ShuttleAssemblyIntegrityIssue> issues,
            Dictionary<string, ShuttleModule> firstModuleByID,
            Dictionary<string, ModuleSlotContext> moduleSlotsByParentKey,
            Dictionary<string, int> moduleReferencesByID)
        {
            if (state.Segments == null)
            {
                return;
            }

            for (int i = 0; i < state.Segments.Count; i++)
            {
                ShuttleSegment segment = state.Segments[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                string segmentID = segment.SegmentInstanceID;
                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleModuleSlot slot = segment.ModuleSlots[j];
                    if (slot == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(slot.ParentSegmentInstanceID) &&
                        !string.IsNullOrEmpty(segmentID) &&
                        slot.ParentSegmentInstanceID != segmentID)
                    {
                        this.AddIssue(
                            issues,
                            "assembly-integrity-module-slot-parent-mismatch",
                            this.GetModuleSlotReference(segmentID, slot.SlotID),
                            "Module slot parent segment '" + slot.ParentSegmentInstanceID +
                            "' does not match containing segment '" + segmentID + "'.");
                    }

                    if (!string.IsNullOrEmpty(slot.ParentSegmentInstanceID) &&
                        !ShuttleAssemblyInstanceIDRules.IsValidSegmentInstanceID(slot.ParentSegmentInstanceID))
                    {
                        this.AddIssue(
                            issues,
                            "assembly-integrity-module-slot-parent-segment-id-invalid",
                            this.GetModuleSlotReference(segmentID, slot.SlotID),
                            "Module slot parent segment instance ID must use a positive safe segment-N value.");
                    }

                    string parentKey = this.GetModuleSlotKey(segmentID, slot.SlotID);
                    if (parentKey != null && !moduleSlotsByParentKey.ContainsKey(parentKey))
                    {
                        moduleSlotsByParentKey.Add(
                            parentKey,
                            new ModuleSlotContext(segmentID, slot.SlotID, slot.InstalledModuleInstanceID));
                    }

                    if (string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                    {
                        continue;
                    }

                    if (!ShuttleAssemblyInstanceIDRules.IsValidModuleInstanceID(slot.InstalledModuleInstanceID))
                    {
                        this.AddIssue(
                            issues,
                            "assembly-integrity-module-reference-id-invalid",
                            this.GetModuleSlotReference(segmentID, slot.SlotID),
                            "Module slot references invalid module instance ID '" +
                            slot.InstalledModuleInstanceID + "'.");
                        continue;
                    }

                    this.Increment(moduleReferencesByID, slot.InstalledModuleInstanceID);
                    ShuttleModule installedModule;
                    if (!firstModuleByID.TryGetValue(slot.InstalledModuleInstanceID, out installedModule))
                    {
                        this.AddIssue(
                            issues,
                            "assembly-integrity-module-reference-missing",
                            this.GetModuleSlotReference(segmentID, slot.SlotID),
                            "Module slot points to missing module instance '" +
                            slot.InstalledModuleInstanceID + "'.");
                        continue;
                    }

                    if (!installedModule.IsInstalledIn(segmentID, slot.SlotID))
                    {
                        this.AddIssue(
                            issues,
                            "assembly-integrity-module-parent-mismatch",
                            slot.InstalledModuleInstanceID,
                            "Module parent segment/slot does not match the slot that contains it.");
                    }
                }
            }
        }

        private void ValidateSegments(
            ShuttleAssemblyState state,
            List<ShuttleAssemblyIntegrityIssue> issues,
            Dictionary<string, int> segmentReferencesByID,
            Dictionary<int, ShuttleSegmentSlot> segmentSlotsByIndex)
        {
            if (state.Segments == null)
            {
                return;
            }

            for (int i = 0; i < state.Segments.Count; i++)
            {
                ShuttleSegment segment = state.Segments[i];
                if (segment == null || string.IsNullOrEmpty(segment.SegmentInstanceID))
                {
                    continue;
                }

                int referenceCount = this.GetCount(segmentReferencesByID, segment.SegmentInstanceID);
                if (referenceCount == 0)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-orphan",
                        segment.SegmentInstanceID,
                        "Segment object is not referenced by any segment slot.");
                }
                else if (referenceCount > 1)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-multiple-slot-references",
                        segment.SegmentInstanceID,
                        "Segment object is referenced by multiple segment slots.");
                }

                ShuttleSegmentSlot slotByIndex;
                if (segmentSlotsByIndex.TryGetValue(segment.SegmentIndex, out slotByIndex) &&
                    !string.IsNullOrEmpty(slotByIndex.InstalledSegmentInstanceID) &&
                    slotByIndex.InstalledSegmentInstanceID != segment.SegmentInstanceID)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-segment-slot-id-mismatch",
                        segment.SegmentInstanceID,
                        "Segment index points at a slot installed with a different segment instance.");
                }
            }
        }

        private void ValidateModules(
            ShuttleAssemblyState state,
            List<ShuttleAssemblyIntegrityIssue> issues,
            Dictionary<string, int> moduleReferencesByID,
            Dictionary<string, ModuleSlotContext> moduleSlotsByParentKey,
            Dictionary<string, ShuttleModule> firstModuleByParentKey,
            Dictionary<string, int> moduleParentCounts)
        {
            if (state.Modules == null)
            {
                return;
            }

            for (int i = 0; i < state.Modules.Count; i++)
            {
                ShuttleModule module = state.Modules[i];
                if (module == null || string.IsNullOrEmpty(module.ModuleInstanceID))
                {
                    continue;
                }

                int referenceCount = this.GetCount(moduleReferencesByID, module.ModuleInstanceID);
                if (referenceCount == 0)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-orphan",
                        module.ModuleInstanceID,
                        "Module object is not referenced by any module slot.");
                }
                else if (referenceCount > 1)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-multiple-slot-references",
                        module.ModuleInstanceID,
                        "Module object is referenced by multiple module slots.");
                }

                string parentKey = this.GetModuleSlotKey(
                    module.ParentSegmentInstanceID,
                    module.ParentSlotID);
                if (parentKey == null)
                {
                    continue;
                }

                ModuleSlotContext parentSlot;
                if (!moduleSlotsByParentKey.TryGetValue(parentKey, out parentSlot))
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-parent-slot-missing",
                        module.ModuleInstanceID,
                        "Module parent segment/slot does not exist.");
                    continue;
                }

                if (parentSlot.InstalledModuleInstanceID != module.ModuleInstanceID)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-slot-id-mismatch",
                        module.ModuleInstanceID,
                        "Module parent slot is installed with a different module instance.");
                }

                ShuttleModule moduleAtParent;
                if (firstModuleByParentKey.TryGetValue(parentKey, out moduleAtParent) &&
                    moduleAtParent != null &&
                    moduleAtParent.ModuleInstanceID != module.ModuleInstanceID)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-parent-object-mismatch",
                        module.ModuleInstanceID,
                        "Another module object claims this module's parent slot.");
                }

                if (this.GetCount(moduleParentCounts, parentKey) > 1)
                {
                    this.AddIssue(
                        issues,
                        "assembly-integrity-module-parent-multiple-objects",
                        parentKey,
                        "Multiple module objects claim this parent segment/slot.");
                }
            }
        }

        private void AddIssue(
            List<ShuttleAssemblyIntegrityIssue> issues,
            string code,
            string referenceID,
            string developerDetail)
        {
            issues.Add(new ShuttleAssemblyIntegrityIssue(
                code,
                referenceID,
                developerDetail,
                true));
        }

        private void Increment(Dictionary<string, int> counts, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + 1;
        }

        private int GetCount(Dictionary<string, int> counts, string key)
        {
            if (counts == null || string.IsNullOrEmpty(key))
            {
                return 0;
            }

            int count;
            return counts.TryGetValue(key, out count) ? count : 0;
        }

        private string GetModuleSlotKey(string segmentInstanceID, string slotID)
        {
            if (string.IsNullOrEmpty(segmentInstanceID) || string.IsNullOrEmpty(slotID))
            {
                return null;
            }

            return segmentInstanceID + "::" + slotID;
        }

        private string GetModuleSlotReference(string segmentInstanceID, string slotID)
        {
            if (string.IsNullOrEmpty(segmentInstanceID))
            {
                return slotID;
            }

            if (string.IsNullOrEmpty(slotID))
            {
                return segmentInstanceID;
            }

            return segmentInstanceID + "::" + slotID;
        }

        private sealed class ModuleSlotContext
        {
            internal ModuleSlotContext(
                string segmentInstanceID,
                string slotID,
                string installedModuleInstanceID)
            {
                this.SegmentInstanceID = segmentInstanceID;
                this.SlotID = slotID;
                this.InstalledModuleInstanceID = installedModuleInstanceID;
            }

            internal string SegmentInstanceID;
            internal string SlotID;
            internal string InstalledModuleInstanceID;
        }
    }
}

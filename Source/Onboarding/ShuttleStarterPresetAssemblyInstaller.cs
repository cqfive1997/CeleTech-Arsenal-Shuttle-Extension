using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Onboarding
{
    internal sealed class ShuttleStarterPresetAssemblyInstaller
    {
        internal bool TryInstall(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            IShuttleCommandExecutor commandExecutor,
            out string failureReason)
        {
            failureReason = null;
            if (host == null || assemblyState == null || commandExecutor == null)
            {
                failureReason = "starter preset host, assembly, or command boundary is unavailable";
                return false;
            }

            assemblyState.EnsureInitialized();
            if ((assemblyState.Segments != null && assemblyState.Segments.Count > 0) ||
                (assemblyState.Modules != null && assemblyState.Modules.Count > 0))
            {
                // PostSpawnSetup(false) can also occur when an existing shuttle returns to a
                // map. Only a completely empty first-build topology is eligible; never refill
                // or overwrite an already assembled/customized shuttle on arrival.
                return true;
            }

            ShuttleStarterPresetDef preset =
                ShuttleStarterPresetBuildPolicy.ResolveBasicPreset();
            if (preset == null || preset.steps == null || preset.steps.Count == 0)
            {
                failureReason = "basic starter preset Def is missing or empty";
                return false;
            }

            if (!ShuttleStarterPresetBuildPolicy
                .ArePackageResearchPrerequisitesFinished())
            {
                failureReason = "basic starter preset research is incomplete";
                return false;
            }

            for (int i = 0; i < preset.steps.Count; i++)
            {
                ShuttleStarterPresetStepDef step = preset.steps[i];
                if (!this.TryInstallStep(
                    step,
                    assemblyState,
                    commandExecutor,
                    out failureReason))
                {
                    failureReason = "step " +
                        (step != null ? step.stepID : i.ToString()) +
                        " failed: " + failureReason;
                    return false;
                }
            }

            Messages.Message(
                "CT_Shuttle_StarterPreset_Installed".Translate(),
                host,
                MessageTypeDefOf.PositiveEvent,
                true);
            return true;
        }

        private bool TryInstallStep(
            ShuttleStarterPresetStepDef step,
            ShuttleAssemblyState assemblyState,
            IShuttleCommandExecutor commandExecutor,
            out string failureReason)
        {
            failureReason = null;
            if (step == null)
            {
                failureReason = "step is null";
                return false;
            }

            ShuttleSegmentSlot segmentSlot = this.FindSegmentSlot(
                assemblyState,
                step.segmentSlotTypeID);
            if (segmentSlot == null)
            {
                failureReason = "target segment slot is unavailable";
                return false;
            }

            if (step.kind == ShuttleStarterPresetStepKind.Segment)
            {
                return this.TryInstallSegment(
                    step,
                    segmentSlot,
                    assemblyState,
                    commandExecutor,
                    out failureReason);
            }

            return this.TryInstallModule(
                step,
                segmentSlot,
                assemblyState,
                commandExecutor,
                out failureReason);
        }

        private bool TryInstallSegment(
            ShuttleStarterPresetStepDef step,
            ShuttleSegmentSlot segmentSlot,
            ShuttleAssemblyState assemblyState,
            IShuttleCommandExecutor commandExecutor,
            out string failureReason)
        {
            failureReason = null;
            if (segmentSlot.IsOccupied)
            {
                ShuttleSegment installed = assemblyState.GetSegment(
                    segmentSlot.InstalledSegmentInstanceID);
                if (installed != null &&
                    installed.SegmentDef != null &&
                    installed.SegmentDef.defName == step.targetDefName)
                {
                    return true;
                }

                failureReason = "target segment slot contains a different segment";
                return false;
            }

            ShuttleCommandResult result = commandExecutor.Execute(
                new InstallSegmentCommand(
                    segmentSlot.SlotID,
                    step.targetDefName));
            if (result != null && result.Success)
            {
                return true;
            }

            failureReason = result != null && !string.IsNullOrEmpty(result.Message)
                ? result.Message
                : "segment install command failed";
            return false;
        }

        private bool TryInstallModule(
            ShuttleStarterPresetStepDef step,
            ShuttleSegmentSlot segmentSlot,
            ShuttleAssemblyState assemblyState,
            IShuttleCommandExecutor commandExecutor,
            out string failureReason)
        {
            failureReason = null;
            if (!segmentSlot.IsOccupied)
            {
                failureReason = "parent segment is not installed";
                return false;
            }

            ShuttleSegment segment = assemblyState.GetSegment(
                segmentSlot.InstalledSegmentInstanceID);
            ShuttleModuleSlot moduleSlot = this.FindModuleSlot(
                segment,
                step.moduleSlotTypeID);
            if (segment == null || moduleSlot == null)
            {
                failureReason = "target module slot is unavailable";
                return false;
            }

            if (moduleSlot.IsOccupied)
            {
                ShuttleModule installed = assemblyState.GetModule(
                    moduleSlot.InstalledModuleInstanceID);
                if (installed != null &&
                    installed.ModuleDef != null &&
                    installed.ModuleDef.defName == step.targetDefName)
                {
                    return true;
                }

                failureReason = "target module slot contains a different module";
                return false;
            }

            ShuttleCommandResult result = commandExecutor.Execute(
                new InstallModuleCommand(
                    segment.SegmentInstanceID,
                    moduleSlot.SlotID,
                    step.targetDefName));
            if (result != null && result.Success)
            {
                return true;
            }

            failureReason = result != null && !string.IsNullOrEmpty(result.Message)
                ? result.Message
                : "module install command failed";
            return false;
        }

        private ShuttleSegmentSlot FindSegmentSlot(
            ShuttleAssemblyState assemblyState,
            string slotTypeID)
        {
            ShuttleSegmentType targetType =
                ShuttleSegmentTypeCatalog.ParseSlotType(slotTypeID);
            for (int i = 0; assemblyState != null &&
                assemblyState.SegmentSlots != null &&
                i < assemblyState.SegmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = assemblyState.SegmentSlots[i];
                if (slot != null && slot.SlotType == targetType)
                {
                    return slot;
                }
            }

            return null;
        }

        private ShuttleModuleSlot FindModuleSlot(
            ShuttleSegment segment,
            string slotTypeID)
        {
            ShuttleModuleType targetType =
                ShuttleModuleTypeCatalog.ParseSlotType(slotTypeID);
            for (int i = 0; segment != null &&
                segment.ModuleSlots != null &&
                i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot != null && slot.SlotType == targetType)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}

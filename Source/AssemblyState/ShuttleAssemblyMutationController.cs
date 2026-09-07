using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Facade for legal assembly mutations. Callers keep one stable entry point while segment
    /// and module mutation rules live in smaller focused services.
    /// </summary>
    internal sealed class ShuttleAssemblyMutationController
    {
        private readonly ShuttleSegmentMutationService segmentMutations;
        private readonly ShuttleModuleMutationService moduleMutations;

        public ShuttleAssemblyMutationController(ShuttleAssemblyBootstrapper bootstrapper)
        {
            this.segmentMutations = new ShuttleSegmentMutationService(bootstrapper);
            this.moduleMutations = new ShuttleModuleMutationService();
        }

        public bool InstallSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef)
        {
            return this.segmentMutations.InstallSegment(state, segmentSlotID, segmentDef);
        }

        public bool RemoveSegment(ShuttleAssemblyState state, string segmentSlotID)
        {
            return this.segmentMutations.RemoveSegment(state, segmentSlotID);
        }

        public bool ReplaceSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef)
        {
            return this.segmentMutations.ReplaceSegment(state, segmentSlotID, segmentDef);
        }

        internal bool CanInstallSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            return this.segmentMutations.CanInstallSegment(
                state,
                segmentSlotID,
                segmentDef,
                out failureReason);
        }

        internal bool CanReplaceSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            return this.segmentMutations.CanReplaceSegment(
                state,
                segmentSlotID,
                segmentDef,
                out failureReason);
        }

        public bool InstallModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName = null)
        {
            return this.moduleMutations.InstallModule(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                selectedStuffDefName);
        }

        internal bool CanInstallModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            return this.moduleMutations.CanInstallModule(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out failureReason);
        }

        internal bool CanInstallModuleForUI(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            return this.moduleMutations.CanInstallModuleForUI(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out failureReason);
        }

        public bool RemoveModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID)
        {
            return this.moduleMutations.RemoveModule(state, segmentInstanceID, moduleSlotID);
        }

        public bool SetModuleEnabled(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            bool enabled)
        {
            return this.moduleMutations.SetModuleEnabled(
                state,
                segmentInstanceID,
                moduleSlotID,
                enabled);
        }

        public bool ReplaceModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName = null)
        {
            return this.moduleMutations.ReplaceModule(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                selectedStuffDefName);
        }
    }
}

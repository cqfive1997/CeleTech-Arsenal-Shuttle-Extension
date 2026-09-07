namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static module slot topology exposed by a segment def.
    /// This is assembly metadata only and should remain free of runtime values.
    /// Module slot types use the module-type catalog, not the segment-type catalog.
    /// It is template data only. Final runtime slot IDs are generated when a concrete segment
    /// instance materializes its module slots.
    /// Empty slotTypeID input falls back to optional; non-empty unknown input fails closed.
    /// </summary>
    public sealed class ShuttleModuleSlotDef
    {
        // Raw string persisted through XML authoring and used for display/debug text.
        public string slotTypeID;

        // Required module slots must stay occupied once filled; removal rejects them.
        public bool isRequired;

        // Locked module slots are not player-editable: install/remove/replace operations reject them.
        public bool isLocked;

        // Optional player-facing display metadata. These are translated by the
        // read-model/display resolver path and are not used for gameplay logic.
        public string labelKey;
        public string shortLabelKey;
        public string descriptionKey;

        private ShuttleModuleType slotType = ShuttleModuleType.Optional;

        public ShuttleModuleType SlotType
        {
            get
            {
                return this.slotType;
            }
        }

        public void EnsureInitialized(int slotIndex)
        {
            this.slotTypeID = ShuttleModuleTypeCatalog.NormalizeModuleSlotTypeID(this.slotTypeID);
            this.slotType = ShuttleModuleTypeCatalog.ParseSlotType(this.slotTypeID);
        }
    }
}

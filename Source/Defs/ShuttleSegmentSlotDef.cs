namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static shuttle-level segment slot topology.
    /// This defines which segment slots exist on the shuttle before any segment is installed.
    /// Segment slot types use the segment-type catalog and may use optional as a wildcard.
    /// It is template data only. Final runtime slot IDs are generated when the layout is
    /// materialized into ShuttleAssemblyState.
    /// Empty slotTypeID input falls back to optional; non-empty unknown input fails closed.
    /// </summary>
    public sealed class ShuttleSegmentSlotDef
    {
        // Raw string persisted through XML authoring and used for display/debug text.
        public string slotTypeID;

        // Required slots must remain occupied once materialized; they block removal.
        public bool isRequired;

        // Locked slots are not player-editable: install/remove operations reject them.
        public bool isLocked;

        // Fixed slots represent structural hull positions. Installed segments inherit this
        // flag and cannot be removed, but their own module slots may still define separate locks.
        public bool isFixed;

        // Optional segment def installed during first assembly bootstrap.
        public string defaultSegmentDefName;

        // Optional player-facing display metadata. These are translated by the
        // read-model/display resolver path and are not used for gameplay logic.
        public string labelKey;
        public string shortLabelKey;
        public string descriptionKey;

        private ShuttleSegmentType slotType = ShuttleSegmentType.Optional;

        public ShuttleSegmentType SlotType
        {
            get
            {
                return this.slotType;
            }
        }

        public void EnsureInitialized(int slotIndex)
        {
            this.slotTypeID = ShuttleSegmentTypeCatalog.NormalizeSegmentSlotTypeID(this.slotTypeID);
            this.slotType = ShuttleSegmentTypeCatalog.ParseSlotType(this.slotTypeID);
        }
    }
}

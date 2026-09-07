using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Read-only Medical Bay supply projection. Formal mutation is exposed only through
    /// the separate withdrawal handle below, not through UI or read models.
    /// </summary>
    internal interface IShuttleMedicalSupplySource
    {
        IReadOnlyList<ShuttleMedicineSupplyReadModel> ListLoadedMedicineForPatient(
            ThingWithComps shuttleHost,
            Pawn patient);
    }

    internal interface IShuttleMedicalSupplyWithdrawal
    {
        Medicine Medicine { get; }
        string MedicineDefName { get; }
        string SourceLabel { get; }
        int SourceThingID { get; }
        int TakenThingID { get; }
        int SourceStackCountBefore { get; }
        int SourceStackCountCurrent { get; }

        void CommitConsumed();

        bool RollbackOrDrop(
            Pawn doctor,
            ThingWithComps shuttleHost,
            out string notice);
    }

    public sealed class ShuttleMedicineSupplyReadModel
    {
        public ThingDef MedicineDef;
        public string MedicineDefName;
        public string Label;
        public int StackCount;
        public float Potency;
        public bool AllowedByPatientMedCare;
        public string SourceLabel;
        public int SourceThingID = -1;
    }
}

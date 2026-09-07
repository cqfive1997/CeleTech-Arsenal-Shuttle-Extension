using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    [Flags]
    internal enum ShuttlePawnPresenceKind
    {
        None = 0,
        Unknown = 1,
        CargoItem = 2,
        CargoLoaded = 4,
        CargoAssignedToLoad = 8,
        HabitatOccupant = 16,
        MedicalPatient = 32,
        MedicalAdmissionCandidate = 64,
        MedicalAdmissionCarrier = 128,
        MedicalActiveDoctor = 256,
        PrisonCellOccupant = 512,
        PrisonCellCandidate = 1024,
        PrisonCellCarrier = 2048,
        PrisonCellFeeder = 4096,
        PrisonCellDoctor = 8192,
        MechCharging = 16384,
        CockpitOccupant = 32768
    }
}

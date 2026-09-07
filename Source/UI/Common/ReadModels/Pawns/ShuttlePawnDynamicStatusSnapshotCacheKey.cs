using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnDynamicStatusSnapshotCacheKey
    {
        internal readonly int ProfileRevision;
        internal readonly int CargoSnapshotRevision;
        internal readonly ShuttlePawnPresenceKind IncludedKinds;
        internal readonly int CurrentGameTick;
        internal readonly int RecordCount;
        internal readonly int PresenceFingerprint;

        private ShuttlePawnDynamicStatusSnapshotCacheKey(
            int profileRevision,
            int cargoSnapshotRevision,
            ShuttlePawnPresenceKind includedKinds,
            int currentGameTick,
            int recordCount,
            int presenceFingerprint)
        {
            this.ProfileRevision = profileRevision;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
            this.IncludedKinds = includedKinds;
            this.CurrentGameTick = currentGameTick;
            this.RecordCount = recordCount;
            this.PresenceFingerprint = presenceFingerprint;
        }

        internal static ShuttlePawnDynamicStatusSnapshotCacheKey Create(
            ShuttlePawnPresenceSnapshot presenceSnapshot,
            ShuttlePawnPresenceKind includedKinds)
        {
            int recordCount = presenceSnapshot != null &&
                presenceSnapshot.Records != null
                    ? presenceSnapshot.Records.Count
                    : 0;

            return new ShuttlePawnDynamicStatusSnapshotCacheKey(
                presenceSnapshot != null ? presenceSnapshot.ProfileRevision : 0,
                presenceSnapshot != null ? presenceSnapshot.CargoSnapshotRevision : 0,
                includedKinds,
                ShuttleTickUtility.TicksGameOrZero(),
                recordCount,
                BuildPresenceFingerprint(presenceSnapshot));
        }

        internal bool Matches(ShuttlePawnDynamicStatusSnapshotCacheKey other)
        {
            return other != null &&
                this.ProfileRevision == other.ProfileRevision &&
                this.CargoSnapshotRevision == other.CargoSnapshotRevision &&
                this.IncludedKinds == other.IncludedKinds &&
                this.CurrentGameTick == other.CurrentGameTick &&
                this.RecordCount == other.RecordCount &&
                this.PresenceFingerprint == other.PresenceFingerprint;
        }

        private static int BuildPresenceFingerprint(
            ShuttlePawnPresenceSnapshot presenceSnapshot)
        {
            unchecked
            {
                int hash = 31;
                if (presenceSnapshot == null ||
                    presenceSnapshot.Records == null)
                {
                    return hash;
                }

                hash = AddHash(hash, presenceSnapshot.ProfileRevision);
                hash = AddHash(hash, presenceSnapshot.CargoSnapshotRevision);
                hash = AddHash(hash, presenceSnapshot.Records.Count);
                for (int i = 0; i < presenceSnapshot.Records.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddPresenceRecordHash(
                        hash,
                        presenceSnapshot.Records[i]);
                }

                return hash;
            }
        }

        private static int AddPresenceRecordHash(
            int hash,
            ShuttlePawnPresenceRecord record)
        {
            unchecked
            {
                if (record == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, record.PawnThingID);
                hash = AddStringHash(hash, record.StableKey);
                hash = AddThingHash(hash, record.DisplayThing);
                hash = AddStringHash(hash, record.Label);
                hash = AddHash(hash, (int)record.Kind);
                hash = AddStringHash(hash, record.SourceKey);
                hash = AddStringHash(hash, record.SourceLabel);
                hash = AddBoolHash(hash, record.IsHumanlike);
                hash = AddBoolHash(hash, record.IsColonist);
                hash = AddBoolHash(hash, record.IsPrisoner);
                hash = AddBoolHash(hash, record.IsSlave);
                hash = AddBoolHash(hash, record.IsAnimal);
                hash = AddBoolHash(hash, record.IsMech);
                hash = AddBoolHash(hash, record.IsLoadedCargo);
                hash = AddBoolHash(hash, record.IsAssignedToLoad);
                hash = AddBoolHash(hash, record.IsMedicalPatient);
                hash = AddBoolHash(hash, record.IsOccupant);
                hash = AddHash(hash, record.CargoRegionIndex);
                hash = AddHash(hash, record.TransporterIndex);
                hash = AddHash(hash, record.LoadedIndex);
                hash = AddHash(hash, record.QueueIndex);
                hash = AddHash(hash, record.SourceIndex);
                hash = AddSourceModelHash(hash, record.SourceModel);
                return hash;
            }
        }

        private static int AddSourceModelHash(int hash, object sourceModel)
        {
            unchecked
            {
                if (sourceModel == null)
                {
                    return AddHash(hash, 0);
                }

                ShuttleMedicalPatientReadModel medicalPatient =
                    sourceModel as ShuttleMedicalPatientReadModel;
                if (medicalPatient != null)
                {
                    return AddMedicalPatientHash(hash, medicalPatient);
                }

                ShuttleHabitatOccupantReadModel habitatOccupant =
                    sourceModel as ShuttleHabitatOccupantReadModel;
                if (habitatOccupant != null)
                {
                    return AddHabitatOccupantHash(hash, habitatOccupant);
                }

                ShuttleMechChargingPawnReadModel chargingPawn =
                    sourceModel as ShuttleMechChargingPawnReadModel;
                if (chargingPawn != null)
                {
                    return AddMechChargingPawnHash(hash, chargingPawn);
                }

                Type sourceType = sourceModel.GetType();
                hash = AddStringHash(
                    hash,
                    sourceType != null ? sourceType.FullName : null);
                return hash;
            }
        }

        private static int AddMedicalPatientHash(
            int hash,
            ShuttleMedicalPatientReadModel patient)
        {
            unchecked
            {
                hash = AddStringHash(hash, "medical-patient");
                hash = AddHash(hash, patient.PawnThingID);
                hash = AddStringHash(hash, patient.PawnLabel);
                hash = AddThingHash(hash, patient.DisplayThing);
                hash = AddBoolHash(hash, patient.IsDowned);
                hash = AddBoolHash(hash, patient.IsConscious);
                hash = AddBoolHash(hash, patient.IsConsciousKnown);
                hash = AddFloatHash(hash, patient.ConsciousnessPct);
                hash = AddFloatHash(hash, patient.PainPct);
                hash = AddBoolHash(hash, patient.IsBleeding);
                hash = AddHash(hash, patient.BleedingHediffCount);
                hash = AddFloatHash(hash, patient.BleedRateTotal);
                hash = AddFloatHash(hash, patient.FoodPct);
                hash = AddFloatHash(hash, patient.RestPct);
                hash = AddFloatHash(hash, patient.JoyPct);
                hash = AddFloatHash(hash, patient.MoodPct);
                hash = AddBoolHash(hash, patient.HasMechDurability);
                hash = AddFloatHash(hash, patient.DurabilityPct);
                hash = AddFloatHash(hash, patient.DamagePct);
                hash = AddBoolHash(hash, patient.HasMechEnergy);
                hash = AddFloatHash(hash, patient.EnergyPct);
                return hash;
            }
        }

        private static int AddHabitatOccupantHash(
            int hash,
            ShuttleHabitatOccupantReadModel occupant)
        {
            unchecked
            {
                hash = AddStringHash(hash, "habitat-occupant");
                hash = AddHash(hash, occupant.PawnThingID);
                hash = AddStringHash(hash, occupant.LabelShort);
                hash = AddStringHash(hash, occupant.LabelCap);
                hash = AddThingHash(hash, occupant.DisplayThing);
                hash = AddFloatHash(hash, occupant.FoodPct);
                hash = AddFloatHash(hash, occupant.RestPct);
                hash = AddFloatHash(hash, occupant.JoyPct);
                hash = AddFloatHash(hash, occupant.MoodPct);
                return hash;
            }
        }

        private static int AddMechChargingPawnHash(
            int hash,
            ShuttleMechChargingPawnReadModel chargingPawn)
        {
            unchecked
            {
                hash = AddStringHash(hash, "mech-charging");
                hash = AddHash(hash, chargingPawn.PawnThingID);
                hash = AddStringHash(hash, chargingPawn.PawnLabel);
                hash = AddThingHash(hash, chargingPawn.DisplayThing);
                hash = AddFloatHash(hash, chargingPawn.EnergyPct);
                return hash;
            }
        }

        private static int AddThingHash(int hash, Thing thing)
        {
            unchecked
            {
                if (thing == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, thing.thingIDNumber);
                hash = AddStringHash(hash, thing.def != null ? thing.def.defName : null);

                Pawn pawn = thing as Pawn;
                if (pawn == null)
                {
                    return hash;
                }

                hash = AddBoolHash(hash, pawn.Dead);
                hash = AddBoolHash(hash, pawn.Downed);
                hash = AddBoolHash(hash, pawn.RaceProps != null && pawn.RaceProps.Humanlike);
                hash = AddBoolHash(hash, pawn.RaceProps != null && pawn.RaceProps.Animal);
                hash = AddBoolHash(hash, pawn.RaceProps != null && pawn.RaceProps.IsMechanoid);
                return hash;
            }
        }

        private static int AddBoolHash(int hash, bool value)
        {
            return AddHash(hash, value ? 1 : 0);
        }

        private static int AddFloatHash(int hash, float value)
        {
            return AddHash(hash, StableFloatHash(value));
        }

        private static int AddHash(int hash, int value)
        {
            unchecked
            {
                return (hash * 397) ^ value;
            }
        }

        private static int AddStringHash(int hash, string value)
        {
            unchecked
            {
                return AddHash(hash, StableStringHash(value));
            }
        }

        private static int StableFloatHash(float value)
        {
            if (float.IsNaN(value))
            {
                return int.MinValue;
            }

            if (float.IsPositiveInfinity(value))
            {
                return int.MaxValue;
            }

            if (float.IsNegativeInfinity(value))
            {
                return int.MinValue + 1;
            }

            return value.GetHashCode();
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 37;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }
    }
}

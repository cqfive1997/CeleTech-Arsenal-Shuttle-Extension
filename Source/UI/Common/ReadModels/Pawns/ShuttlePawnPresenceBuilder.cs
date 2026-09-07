using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnPresenceBuilder
    {
        internal ShuttlePawnPresenceSnapshot Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int cargoSnapshotRevision)
        {
            List<ShuttlePawnPresenceRecord> records =
                new List<ShuttlePawnPresenceRecord>();
            controlModel = controlModel ?? new ShuttleControlReadModel();

            this.AddCargoRecords(records, cargoSnapshot);
            this.AddHabitatRecords(records, controlModel.Habitat);
            this.AddMedicalRecords(records, controlModel.MedicalBay);
            this.AddMechChargingRecords(records, controlModel.MechCharger);
            this.AddPrisonCellRecords(records, controlModel.PrisonCell);

            return new ShuttlePawnPresenceSnapshot(
                records,
                controlModel.ProfileRevision,
                cargoSnapshotRevision);
        }

        private void AddCargoRecords(
            List<ShuttlePawnPresenceRecord> records,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (records == null ||
                cargoSnapshot == null ||
                cargoSnapshot.Items == null)
            {
                return;
            }

            for (int i = 0; i < cargoSnapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[i];
                if (item == null || !item.IsPawn)
                {
                    continue;
                }

                ShuttlePawnPresenceKind kind =
                    ShuttlePawnPresenceKind.CargoItem;
                if (item.IsLoaded)
                {
                    Pawn loadedPawn = item.DisplayThing as Pawn;
                    kind |= ShuttleCockpitPresenceClassifier.IsCockpitOccupant(loadedPawn)
                        ? ShuttlePawnPresenceKind.CockpitOccupant
                        : ShuttlePawnPresenceKind.CargoLoaded;
                }

                if (item.IsAssignedToLoad)
                {
                    kind |= ShuttlePawnPresenceKind.CargoAssignedToLoad;
                }

                ShuttlePawnPresenceRecord record = this.CreateRecord(
                    item.ThingIDNumber,
                    item.DisplayThing,
                    item.Label,
                    kind,
                    "cargo:" + i.ToString(),
                    (kind & ShuttlePawnPresenceKind.CockpitOccupant) !=
                        ShuttlePawnPresenceKind.None
                        ? "Cockpit"
                        : "Cargo",
                    i,
                    item);
                record.CargoRegionIndex = item.CargoRegionIndex;
                record.TransporterIndex = item.TransporterIndex;
                record.LoadedIndex = item.LoadedIndex;
                record.QueueIndex = item.QueueIndex;
                record.IsLoadedCargo = item.IsLoaded;
                record.IsAssignedToLoad = item.IsAssignedToLoad;
                this.AddRecord(records, record);
            }
        }

        private void AddHabitatRecords(
            List<ShuttlePawnPresenceRecord> records,
            ShuttleHabitatReadModel habitat)
        {
            if (records == null || habitat == null || habitat.Occupants == null)
            {
                return;
            }

            for (int i = 0; i < habitat.Occupants.Count; i++)
            {
                ShuttleHabitatOccupantReadModel occupant = habitat.Occupants[i];
                if (occupant == null)
                {
                    continue;
                }

                ShuttlePawnPresenceRecord record = this.CreateRecord(
                    occupant.PawnThingID,
                    occupant.DisplayThing,
                    this.FirstNonEmpty(occupant.LabelCap, occupant.LabelShort),
                    ShuttlePawnPresenceKind.HabitatOccupant,
                    "habitat:" + i.ToString(),
                    "Habitat",
                    i,
                    occupant);
                record.IsOccupant = true;
                this.AddRecord(records, record);
            }
        }

        private void AddMedicalRecords(
            List<ShuttlePawnPresenceRecord> records,
            ShuttleMedicalBayReadModel medicalBay)
        {
            if (records == null || medicalBay == null)
            {
                return;
            }

            this.AddMedicalPatientRecords(records, medicalBay.Patients);
            this.AddMedicalActiveDoctorRecord(records, medicalBay);
            this.AddMedicalAdmissionRecords(records, medicalBay.AdmissionCandidates);
        }

        private void AddMedicalPatientRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttleMedicalPatientReadModel> patients)
        {
            if (records == null || patients == null)
            {
                return;
            }

            for (int i = 0; i < patients.Count; i++)
            {
                ShuttleMedicalPatientReadModel patient = patients[i];
                if (patient == null)
                {
                    continue;
                }

                ShuttlePawnPresenceRecord record = this.CreateRecord(
                    patient.PawnThingID,
                    patient.DisplayThing,
                    patient.PawnLabel,
                    ShuttlePawnPresenceKind.MedicalPatient,
                    "medical-patient:" + i.ToString(),
                    "Medical patient",
                    i,
                    patient);
                record.IsMedicalPatient = true;
                record.IsOccupant = true;
                this.AddRecord(records, record);
            }
        }

        private void AddMedicalActiveDoctorRecord(
            List<ShuttlePawnPresenceRecord> records,
            ShuttleMedicalBayReadModel medicalBay)
        {
            if (records == null ||
                medicalBay == null ||
                medicalBay.ActiveProcedureDoctorThingID <= 0)
            {
                return;
            }

            ShuttlePawnPresenceRecord record = this.CreateRecord(
                medicalBay.ActiveProcedureDoctorThingID,
                medicalBay.ActiveProcedureDoctorDisplayThing,
                medicalBay.ActiveProcedureDoctorLabel,
                ShuttlePawnPresenceKind.MedicalActiveDoctor,
                "medical-active-doctor:" +
                    medicalBay.ActiveProcedureDoctorThingID.ToString(),
                "Medical doctor",
                -1,
                medicalBay);
            this.AddRecord(records, record);
        }

        private void AddMedicalAdmissionRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttleMedicalAdmissionCandidateReadModel> candidates)
        {
            if (records == null || candidates == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                ShuttleMedicalAdmissionCandidateReadModel candidate =
                    candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                this.AddRecord(
                    records,
                    this.CreateRecord(
                        candidate.PawnThingID,
                        candidate.DisplayThing,
                        candidate.Label,
                        ShuttlePawnPresenceKind.MedicalAdmissionCandidate,
                        "medical-admission:" + this.GetCandidateKey(candidate, i),
                        "Medical admission candidate",
                        i,
                        candidate));
                this.AddMedicalAdmissionCarrierRecords(
                    records,
                    candidate,
                    i);
            }
        }

        private void AddMedicalAdmissionCarrierRecords(
            List<ShuttlePawnPresenceRecord> records,
            ShuttleMedicalAdmissionCandidateReadModel candidate,
            int candidateIndex)
        {
            if (records == null ||
                candidate == null ||
                candidate.CarryCandidates == null)
            {
                return;
            }

            for (int i = 0; i < candidate.CarryCandidates.Count; i++)
            {
                ShuttleMedicalAdmissionCarrierReadModel carrier =
                    candidate.CarryCandidates[i];
                if (carrier == null)
                {
                    continue;
                }

                this.AddRecord(
                    records,
                    this.CreateRecord(
                        carrier.PawnThingID,
                        carrier.DisplayThing,
                        carrier.Label,
                        ShuttlePawnPresenceKind.MedicalAdmissionCarrier,
                        "medical-carrier:" + candidateIndex.ToString() +
                            ":" + i.ToString(),
                        "Medical carrier",
                        i,
                        carrier));
            }
        }

        private void AddMechChargingRecords(
            List<ShuttlePawnPresenceRecord> records,
            ShuttleMechChargerReadModel mechCharger)
        {
            if (records == null ||
                mechCharger == null ||
                mechCharger.ChargingMechs == null)
            {
                return;
            }

            for (int i = 0; i < mechCharger.ChargingMechs.Count; i++)
            {
                ShuttleMechChargingPawnReadModel chargingMech =
                    mechCharger.ChargingMechs[i];
                if (chargingMech == null)
                {
                    continue;
                }

                ShuttlePawnPresenceRecord record = this.CreateRecord(
                    chargingMech.PawnThingID,
                    chargingMech.DisplayThing,
                    chargingMech.PawnLabel,
                    ShuttlePawnPresenceKind.MechCharging,
                    "mech-charging:" + i.ToString(),
                    "Mech charger",
                    i,
                    chargingMech);
                record.IsOccupant = true;
                this.AddRecord(records, record);
            }
        }

        private void AddPrisonCellRecords(
            List<ShuttlePawnPresenceRecord> records,
            ShuttlePrisonCellReadModel prisonCell)
        {
            if (records == null || prisonCell == null)
            {
                return;
            }

            this.AddHeldPrisonerRecords(records, prisonCell.Prisoners);
            this.AddPrisonCandidateRecords(records, prisonCell.Candidates);
            this.AddPrisonCarrierRecords(records, prisonCell.Carriers);
            this.AddPrisonFeederRecords(records, prisonCell.Feeders);
            this.AddPrisonDoctorRecords(records, prisonCell.Doctors);
        }

        private void AddHeldPrisonerRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttleHeldPrisonerReadModel> prisoners)
        {
            if (records == null || prisoners == null)
            {
                return;
            }

            for (int i = 0; i < prisoners.Count; i++)
            {
                ShuttleHeldPrisonerReadModel prisoner = prisoners[i];
                if (prisoner == null)
                {
                    continue;
                }

                ShuttlePawnPresenceRecord record = this.CreateRecord(
                    prisoner.ThingIDNumber,
                    prisoner.DisplayThing,
                    prisoner.Label,
                    ShuttlePawnPresenceKind.PrisonCellOccupant,
                    "prisoner:" + i.ToString(),
                    "Prison cell occupant",
                    i,
                    prisoner);
                record.IsPrisoner = true;
                record.IsOccupant = true;
                this.AddRecord(records, record);
            }
        }

        private void AddPrisonCandidateRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttlePrisonerCandidateReadModel> candidates)
        {
            if (records == null || candidates == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                ShuttlePrisonerCandidateReadModel candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                this.AddRecord(
                    records,
                    this.CreateRecord(
                        candidate.ThingIDNumber,
                        candidate.DisplayThing,
                        candidate.Label,
                        ShuttlePawnPresenceKind.PrisonCellCandidate,
                        "prison-candidate:" + i.ToString(),
                        "Prison cell candidate",
                        i,
                        candidate));
            }
        }

        private void AddPrisonCarrierRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> carriers)
        {
            if (records == null || carriers == null)
            {
                return;
            }

            for (int i = 0; i < carriers.Count; i++)
            {
                ShuttlePrisonerCarrierReadModel carrier = carriers[i];
                if (carrier == null)
                {
                    continue;
                }

                this.AddRecord(
                    records,
                    this.CreateRecord(
                        carrier.ThingIDNumber,
                        carrier.DisplayThing,
                        carrier.Label,
                        ShuttlePawnPresenceKind.PrisonCellCarrier,
                        "prison-carrier:" + i.ToString(),
                        "Prison cell carrier",
                        i,
                        carrier));
            }
        }

        private void AddPrisonFeederRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttlePrisonerFeederReadModel> feeders)
        {
            if (records == null || feeders == null)
            {
                return;
            }

            for (int i = 0; i < feeders.Count; i++)
            {
                ShuttlePrisonerFeederReadModel feeder = feeders[i];
                if (feeder == null)
                {
                    continue;
                }

                this.AddRecord(
                    records,
                    this.CreateRecord(
                        feeder.ThingIDNumber,
                        feeder.DisplayThing,
                        feeder.Label,
                        ShuttlePawnPresenceKind.PrisonCellFeeder,
                        "prison-feeder:" + i.ToString(),
                        "Prison cell feeder",
                        i,
                        feeder));
            }
        }

        private void AddPrisonDoctorRecords(
            List<ShuttlePawnPresenceRecord> records,
            IReadOnlyList<ShuttlePrisonerDoctorReadModel> doctors)
        {
            if (records == null || doctors == null)
            {
                return;
            }

            for (int i = 0; i < doctors.Count; i++)
            {
                ShuttlePrisonerDoctorReadModel doctor = doctors[i];
                if (doctor == null)
                {
                    continue;
                }

                this.AddRecord(
                    records,
                    this.CreateRecord(
                        doctor.ThingIDNumber,
                        doctor.DisplayThing,
                        doctor.Label,
                        ShuttlePawnPresenceKind.PrisonCellDoctor,
                        "prison-doctor:" + i.ToString(),
                        "Prison cell doctor",
                        i,
                        doctor));
            }
        }

        private ShuttlePawnPresenceRecord CreateRecord(
            int pawnThingID,
            Thing displayThing,
            string label,
            ShuttlePawnPresenceKind kind,
            string sourceKey,
            string sourceLabel,
            int sourceIndex,
            object sourceModel)
        {
            ShuttlePawnPresenceRecord record =
                new ShuttlePawnPresenceRecord();
            record.PawnThingID = pawnThingID;
            record.DisplayThing = displayThing;
            record.Label = this.GetLabel(label, displayThing);
            record.Kind = kind == ShuttlePawnPresenceKind.None
                ? ShuttlePawnPresenceKind.Unknown
                : kind;
            record.SourceKey = sourceKey;
            record.SourceLabel = sourceLabel;
            record.SourceIndex = sourceIndex;
            record.SourceModel = sourceModel;
            record.StableKey = pawnThingID > 0
                ? pawnThingID.ToString()
                : sourceKey;
            this.ApplyPawnClassification(record);
            return record;
        }

        private void AddRecord(
            List<ShuttlePawnPresenceRecord> records,
            ShuttlePawnPresenceRecord record)
        {
            if (records == null || record == null)
            {
                return;
            }

            if (record.PawnThingID <= 0 &&
                record.DisplayThing == null &&
                string.IsNullOrEmpty(record.Label))
            {
                return;
            }

            records.Add(record);
        }

        private void ApplyPawnClassification(ShuttlePawnPresenceRecord record)
        {
            if (record == null)
            {
                return;
            }

            Pawn pawn = record.DisplayThing as Pawn;
            record.Pawn = pawn;
            if (pawn == null || pawn.RaceProps == null)
            {
                return;
            }

            record.IsHumanlike = pawn.RaceProps.Humanlike;
            record.IsAnimal = pawn.RaceProps.Animal;
            record.IsMech = pawn.RaceProps.IsMechanoid;
            record.IsColonist = pawn.IsColonist;
            record.IsPrisoner = pawn.IsPrisonerOfColony || pawn.IsPrisoner;
            record.IsSlave = pawn.IsSlaveOfColony || pawn.IsSlave;
        }

        private string GetLabel(string label, Thing displayThing)
        {
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }

            if (displayThing != null)
            {
                return displayThing.LabelCap.ToString();
            }

            return string.Empty;
        }

        private string FirstNonEmpty(string first, string second)
        {
            return !string.IsNullOrEmpty(first) ? first : second;
        }

        private string GetCandidateKey(
            ShuttleMedicalAdmissionCandidateReadModel candidate,
            int index)
        {
            if (candidate != null && !string.IsNullOrEmpty(candidate.Id))
            {
                return candidate.Id;
            }

            return index.ToString();
        }
    }
}

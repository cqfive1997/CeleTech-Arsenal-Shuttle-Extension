using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewReadModelBuilder
    {
        private readonly V3CrewCardModelBuilder cardBuilder;
        private readonly V3CrewGroupModelBuilder groupBuilder =
            new V3CrewGroupModelBuilder();

        internal V3CrewReadModelBuilder()
        {
            V3CrewClassificationFormatter classificationFormatter =
                new V3CrewClassificationFormatter();
            V3CrewNeedIssueModelBuilder needIssueBuilder =
                new V3CrewNeedIssueModelBuilder();
            V3CrewPawnHealthIssueBuilder healthIssueBuilder =
                new V3CrewPawnHealthIssueBuilder();
            V3CrewCardPresentationBuilder presentationBuilder =
                new V3CrewCardPresentationBuilder(
                    classificationFormatter,
                    needIssueBuilder,
                    healthIssueBuilder);
            this.cardBuilder = new V3CrewCardModelBuilder(
                classificationFormatter,
                presentationBuilder,
                new V3CrewMedicalIssueModelBuilder());
        }

        internal V3CrewPageData Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            return this.Build(controlModel, cargoSnapshot, null, null);
        }

        internal V3CrewPageData Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            return this.Build(
                controlModel,
                cargoSnapshot,
                pawnPresenceSnapshot,
                null);
        }

        internal V3CrewPageData Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            V3CrewPageData data = new V3CrewPageData();
            HashSet<int> seenPawnIds = new HashSet<int>();
            if (pawnPresenceSnapshot != null &&
                pawnPresenceSnapshot.Records != null &&
                pawnPresenceSnapshot.Records.Count > 0)
            {
                this.AddPresenceCards(
                    data,
                    pawnPresenceSnapshot,
                    pawnDynamicStatusSnapshot,
                    seenPawnIds);
            }
            else
            {
                this.AddCargoPawns(data, cargoSnapshot, seenPawnIds);
                this.AddHabitatOccupants(data, controlModel, seenPawnIds);
                this.AddMedicalPatients(data, controlModel, seenPawnIds);
                this.AddPrisonCellOccupants(data, controlModel, seenPawnIds);
                this.AddMechChargingOccupants(data, controlModel, seenPawnIds);
            }

            this.groupBuilder.ApplyTotals(data);
            return data;
        }

        private void AddPresenceCards(
            V3CrewPageData data,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot,
            HashSet<int> seenPawnIds)
        {
            if (data == null ||
                pawnPresenceSnapshot == null ||
                pawnPresenceSnapshot.Records == null)
            {
                return;
            }

            for (int i = 0; i < pawnPresenceSnapshot.Records.Count; i++)
            {
                ShuttlePawnPresenceRecord presence =
                    pawnPresenceSnapshot.Records[i];
                if (presence == null || presence.PawnThingID <= 0)
                {
                    continue;
                }

                if (seenPawnIds != null &&
                    seenPawnIds.Contains(presence.PawnThingID))
                {
                    continue;
                }

                ShuttlePawnDynamicStatusRecord dynamicStatus =
                    this.FindDynamicStatus(
                        pawnDynamicStatusSnapshot,
                        presence.PawnThingID);
                V3CrewCardModel card =
                    this.BuildPresenceCard(presence, dynamicStatus);
                if (card == null)
                {
                    continue;
                }

                if (seenPawnIds != null)
                {
                    seenPawnIds.Add(presence.PawnThingID);
                }

                if (presence.HasKind(ShuttlePawnPresenceKind.CockpitOccupant))
                {
                    data.CockpitPawnCount++;
                }

                this.groupBuilder.AddCardToGroup(data, card);
            }
        }

        private V3CrewCardModel BuildPresenceCard(
            ShuttlePawnPresenceRecord presence,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (presence == null)
            {
                return null;
            }

            if (presence.HasKind(ShuttlePawnPresenceKind.CockpitOccupant) ||
                presence.HasKind(ShuttlePawnPresenceKind.CargoLoaded))
            {
                return this.cardBuilder.BuildCargoPawnCard(
                    presence.SourceModel as ShuttleCargoItemSnapshot,
                    dynamicStatus);
            }

            if (presence.HasKind(ShuttlePawnPresenceKind.HabitatOccupant))
            {
                return this.cardBuilder.BuildHabitatOccupantCard(
                    presence.SourceModel as ShuttleHabitatOccupantReadModel,
                    dynamicStatus);
            }

            if (presence.HasKind(ShuttlePawnPresenceKind.MedicalPatient))
            {
                return this.cardBuilder.BuildMedicalPatientCard(
                    presence.SourceModel as ShuttleMedicalPatientReadModel,
                    dynamicStatus);
            }

            if (presence.HasKind(ShuttlePawnPresenceKind.PrisonCellOccupant))
            {
                return this.cardBuilder.BuildPrisonCellOccupantCard(
                    presence.SourceModel as ShuttleHeldPrisonerReadModel,
                    dynamicStatus);
            }

            if (presence.HasKind(ShuttlePawnPresenceKind.MechCharging))
            {
                return this.cardBuilder.BuildChargingMechCard(
                    presence.SourceModel as ShuttleMechChargingPawnReadModel,
                    dynamicStatus);
            }

            return null;
        }

        private ShuttlePawnDynamicStatusRecord FindDynamicStatus(
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot,
            int pawnThingID)
        {
            return pawnDynamicStatusSnapshot != null
                ? pawnDynamicStatusSnapshot.FindByPawnThingID(pawnThingID)
                : null;
        }

        private void AddCargoPawns(
            V3CrewPageData data,
            ShuttleCargoSnapshot cargoSnapshot,
            HashSet<int> seenPawnIds)
        {
            if (data == null || cargoSnapshot == null || cargoSnapshot.Items == null)
            {
                return;
            }

            for (int i = 0; i < cargoSnapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[i];
                if (item == null || !item.IsLoaded || !item.IsPawn)
                {
                    continue;
                }

                if (item.ThingIDNumber > 0 && seenPawnIds.Contains(item.ThingIDNumber))
                {
                    continue;
                }

                V3CrewCardModel card = this.cardBuilder.BuildCargoPawnCard(item);
                if (card == null)
                {
                    continue;
                }

                if (item.ThingIDNumber > 0)
                {
                    seenPawnIds.Add(item.ThingIDNumber);
                }

                Pawn pawn = item.DisplayThing as Pawn;
                if (ShuttleCockpitPresenceClassifier.IsCockpitOccupant(pawn))
                {
                    data.CockpitPawnCount++;
                }
                this.groupBuilder.AddCardToGroup(data, card);
            }
        }

        private void AddHabitatOccupants(
            V3CrewPageData data,
            ShuttleControlReadModel controlModel,
            HashSet<int> seenPawnIds)
        {
            ShuttleHabitatReadModel habitat = controlModel != null
                ? controlModel.Habitat
                : null;
            if (data == null || habitat == null || habitat.Occupants == null)
            {
                return;
            }

            for (int i = 0; i < habitat.Occupants.Count; i++)
            {
                ShuttleHabitatOccupantReadModel occupant = habitat.Occupants[i];
                if (occupant == null ||
                    occupant.PawnThingID <= 0 ||
                    seenPawnIds.Contains(occupant.PawnThingID))
                {
                    continue;
                }

                seenPawnIds.Add(occupant.PawnThingID);
                this.groupBuilder.AddCardToGroup(
                    data,
                    this.cardBuilder.BuildHabitatOccupantCard(occupant));
            }
        }

        private void AddMedicalPatients(
            V3CrewPageData data,
            ShuttleControlReadModel controlModel,
            HashSet<int> seenPawnIds)
        {
            ShuttleMedicalBayReadModel medicalBay = controlModel != null
                ? controlModel.MedicalBay
                : null;
            if (data == null || medicalBay == null || medicalBay.Patients == null)
            {
                return;
            }

            for (int i = 0; i < medicalBay.Patients.Count; i++)
            {
                ShuttleMedicalPatientReadModel patient = medicalBay.Patients[i];
                if (patient == null ||
                    patient.PawnThingID <= 0 ||
                    seenPawnIds.Contains(patient.PawnThingID))
                {
                    continue;
                }

                seenPawnIds.Add(patient.PawnThingID);
                this.groupBuilder.AddCardToGroup(
                    data,
                    this.cardBuilder.BuildMedicalPatientCard(patient));
            }
        }

        private void AddMechChargingOccupants(
            V3CrewPageData data,
            ShuttleControlReadModel controlModel,
            HashSet<int> seenPawnIds)
        {
            ShuttleMechChargerReadModel mechCharger = controlModel != null
                ? controlModel.MechCharger
                : null;
            if (data == null || mechCharger == null || mechCharger.ChargingMechs == null)
            {
                return;
            }

            for (int i = 0; i < mechCharger.ChargingMechs.Count; i++)
            {
                ShuttleMechChargingPawnReadModel chargingMech = mechCharger.ChargingMechs[i];
                if (chargingMech == null ||
                    chargingMech.PawnThingID <= 0 ||
                    seenPawnIds.Contains(chargingMech.PawnThingID))
                {
                    continue;
                }

                seenPawnIds.Add(chargingMech.PawnThingID);
                this.groupBuilder.AddCardToGroup(
                    data,
                    this.cardBuilder.BuildChargingMechCard(chargingMech));
            }
        }

        private void AddPrisonCellOccupants(
            V3CrewPageData data,
            ShuttleControlReadModel controlModel,
            HashSet<int> seenPawnIds)
        {
            ShuttlePrisonCellReadModel prisonCell = controlModel != null
                ? controlModel.PrisonCell
                : null;
            if (data == null || prisonCell == null || prisonCell.Prisoners == null)
            {
                return;
            }

            for (int i = 0; i < prisonCell.Prisoners.Count; i++)
            {
                ShuttleHeldPrisonerReadModel prisoner = prisonCell.Prisoners[i];
                if (prisoner == null ||
                    prisoner.ThingIDNumber <= 0 ||
                    seenPawnIds.Contains(prisoner.ThingIDNumber))
                {
                    continue;
                }

                V3CrewCardModel card =
                    this.cardBuilder.BuildPrisonCellOccupantCard(prisoner);
                if (card == null)
                {
                    continue;
                }

                seenPawnIds.Add(prisoner.ThingIDNumber);
                this.groupBuilder.AddCardToGroup(data, card);
            }
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewCardModelBuilder
    {
        private readonly V3CrewClassificationFormatter classificationFormatter;
        private readonly V3CrewCardPresentationBuilder presentationBuilder;
        private readonly V3CrewMedicalIssueModelBuilder medicalIssueBuilder;

        internal V3CrewCardModelBuilder(
            V3CrewClassificationFormatter classificationFormatter,
            V3CrewCardPresentationBuilder presentationBuilder,
            V3CrewMedicalIssueModelBuilder medicalIssueBuilder)
        {
            this.classificationFormatter = classificationFormatter;
            this.presentationBuilder = presentationBuilder;
            this.medicalIssueBuilder = medicalIssueBuilder;
        }

        internal V3CrewCardModel BuildCargoPawnCard(ShuttleCargoItemSnapshot item)
        {
            return this.BuildCargoPawnCard(item, null);
        }

        internal V3CrewCardModel BuildCargoPawnCard(
            ShuttleCargoItemSnapshot item,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            Pawn pawn = item != null ? item.DisplayThing as Pawn : null;
            V3CrewCardModel card = new V3CrewCardModel();
            card.DisplayThing = item != null ? item.DisplayThing : null;
            card.Label = item != null && !string.IsNullOrEmpty(item.Label) ? item.Label : "-";
            card.SourceKind =
                ShuttleCockpitPresenceClassifier.IsCockpitOccupant(pawn)
                    ? V3CrewCardSourceKind.Cockpit
                    : V3CrewCardSourceKind.CargoLoaded;
            card.TransporterIndex = item != null ? item.TransporterIndex : -1;
            card.LoadedIndex = item != null ? item.LoadedIndex : -1;
            card.ThingIDNumber = item != null ? item.ThingIDNumber : 0;
            card.DefName = item != null ? item.DefName : null;
            this.ApplyCargoActivity(card, pawn);
            this.ApplyPawnNeeds(card, pawn, dynamicStatus);
            this.classificationFormatter.ApplyPawnClassification(card, pawn);

            if (pawn != null && pawn.RaceProps != null && pawn.RaceProps.IsMechanoid)
            {
                card.Note = ShuttleUIText.Tr("CT_ShuttleCrew_EnergyUnavailable");
            }

            this.presentationBuilder.FinalizeCrewCard(card, pawn, dynamicStatus);
            return card;
        }

        internal V3CrewCardModel BuildHabitatOccupantCard(
            ShuttleHabitatOccupantReadModel occupant)
        {
            return this.BuildHabitatOccupantCard(occupant, null);
        }

        internal V3CrewCardModel BuildHabitatOccupantCard(
            ShuttleHabitatOccupantReadModel occupant,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (occupant == null)
            {
                return null;
            }

            Pawn pawn = occupant.DisplayThing as Pawn;
            V3CrewCardModel card = new V3CrewCardModel();
            card.SourceKind = V3CrewCardSourceKind.Habitat;
            card.Group = V3CrewGroupKind.Human;
            card.DisplayThing = occupant.DisplayThing;
            card.Label = !string.IsNullOrEmpty(occupant.LabelCap)
                ? occupant.LabelCap
                : occupant.LabelShort;
            card.ThingIDNumber = occupant.PawnThingID;
            card.FoodPct = occupant.FoodPct;
            card.RestPct = occupant.RestPct;
            card.JoyPct = occupant.JoyPct;
            card.MoodPct = occupant.MoodPct;
            card.EnergyPct = -1f;
            this.ApplyDynamicNeeds(card, dynamicStatus, false);
            card.FallbackIconText = "H";
            card.AppearanceTooltip = this.GetHolderAppearancePendingTooltip();
            this.classificationFormatter.ApplyCrewKindDisplay(
                card,
                "Human",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Human"),
                this.classificationFormatter.GetHumanNeutralCardColor(),
                V3CrewText.BlueColor);
            this.classificationFormatter.ApplyCrewActivityDisplay(
                card,
                this.classificationFormatter.GetHabitatActivityKey(occupant.Activity),
                this.classificationFormatter.GetHabitatActivityLabel(occupant.Activity),
                ShuttleUIStyle.MutedTextColor);
            if (pawn != null)
            {
                this.classificationFormatter.ApplyPawnClassification(card, pawn);
            }
            card.CompartmentLabel = ShuttleUIText.Tr("CT_ShuttleCrew_HabitatA");
            this.presentationBuilder.FinalizeCrewCard(card, pawn, dynamicStatus);
            return card;
        }

        internal V3CrewCardModel BuildMedicalPatientCard(
            ShuttleMedicalPatientReadModel patient)
        {
            return this.BuildMedicalPatientCard(patient, null);
        }

        internal V3CrewCardModel BuildMedicalPatientCard(
            ShuttleMedicalPatientReadModel patient,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (patient == null)
            {
                return null;
            }

            Pawn pawn = patient.DisplayThing as Pawn;
            V3CrewCardModel card = new V3CrewCardModel();
            card.SourceKind = V3CrewCardSourceKind.MedicalBay;
            card.Group = V3CrewGroupKind.Human;
            card.DisplayThing = patient.DisplayThing;
            card.Label = !string.IsNullOrEmpty(patient.PawnLabel) ? patient.PawnLabel : "-";
            card.ThingIDNumber = patient.PawnThingID;
            card.FoodPct = patient.FoodPct;
            card.RestPct = patient.RestPct;
            card.JoyPct = patient.JoyPct;
            card.MoodPct = patient.MoodPct;
            card.EnergyPct = patient.HasMechEnergy ? patient.EnergyPct : -1f;
            this.ApplyDynamicNeeds(card, dynamicStatus, true);
            card.FallbackIconText = "H";
            card.AppearanceTooltip = this.GetHolderAppearancePendingTooltip();
            this.classificationFormatter.ApplyCrewKindDisplay(
                card,
                "Patient",
                ShuttleUIText.Tr("CT_ShuttleCrew_Patient"),
                this.classificationFormatter.GetMedicalHumanCardColor(),
                V3CrewText.GreenColor);
            this.classificationFormatter.ApplyCrewActivityDisplay(
                card,
                "Recovering",
                ShuttleUIText.Tr("CT_ShuttleCrew_Recovering"),
                V3CrewText.GreenColor);
            if (pawn != null)
            {
                this.classificationFormatter.ApplyPawnClassification(card, pawn);
            }
            card.CompartmentLabel = ShuttleUIText.Tr("CT_ShuttleCrew_MedicalBay");
            this.medicalIssueBuilder.AddPatientIssues(card, patient);
            if (pawn == null)
            {
                this.medicalIssueBuilder.AddMedicalReadModelIssues(card.Issues, patient.Hediffs);
            }

            this.presentationBuilder.FinalizeCrewCard(card, pawn, dynamicStatus);
            return card;
        }

        internal V3CrewCardModel BuildChargingMechCard(
            ShuttleMechChargingPawnReadModel chargingMech)
        {
            return this.BuildChargingMechCard(chargingMech, null);
        }

        internal V3CrewCardModel BuildChargingMechCard(
            ShuttleMechChargingPawnReadModel chargingMech,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (chargingMech == null)
            {
                return null;
            }

            V3CrewCardModel card = new V3CrewCardModel();
            card.SourceKind = V3CrewCardSourceKind.MechCharger;
            card.Group = V3CrewGroupKind.Mech;
            card.DisplayThing = chargingMech.DisplayThing;
            card.Label = !string.IsNullOrEmpty(chargingMech.PawnLabel)
                ? chargingMech.PawnLabel
                : "-";
            card.ThingIDNumber = chargingMech.PawnThingID;
            card.EnergyPct = chargingMech.EnergyPct;
            this.ApplyDynamicNeeds(card, dynamicStatus, true);
            card.FallbackIconText = "M";
            card.AppearanceTooltip = this.GetHolderAppearancePendingTooltip();
            this.classificationFormatter.ApplyMechClassification(card);
            this.classificationFormatter.ApplyCrewActivityDisplay(
                card,
                "Charging",
                ShuttleUIText.Tr("CT_ShuttleCrew_Charging"),
                V3CrewText.BlueColor);
            card.Note = ShuttleUIText.Tr("CT_ShuttleCrew_ShuttleChargingBay");
            this.presentationBuilder.FinalizeCrewCard(
                card,
                chargingMech.DisplayThing as Pawn,
                dynamicStatus);
            return card;
        }

        internal V3CrewCardModel BuildPrisonCellOccupantCard(
            ShuttleHeldPrisonerReadModel prisoner)
        {
            return this.BuildPrisonCellOccupantCard(prisoner, null);
        }

        internal V3CrewCardModel BuildPrisonCellOccupantCard(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (prisoner == null)
            {
                return null;
            }

            Pawn pawn = prisoner.DisplayThing as Pawn;
            V3CrewCardModel card = new V3CrewCardModel();
            card.SourceKind = V3CrewCardSourceKind.PrisonCell;
            card.Group = V3CrewGroupKind.Human;
            card.DisplayThing = prisoner.DisplayThing;
            card.Label = !string.IsNullOrEmpty(prisoner.Label)
                ? prisoner.Label
                : "-";
            card.ThingIDNumber = prisoner.ThingIDNumber;
            card.FoodPct = prisoner.FoodLevelPercent;
            card.FallbackIconText = "P";
            card.AppearanceTooltip = this.GetHolderAppearancePendingTooltip();
            this.ApplyDynamicNeeds(card, dynamicStatus, false);
            if (pawn != null)
            {
                this.classificationFormatter.ApplyPawnClassification(card, pawn);
            }

            this.classificationFormatter.ApplyCrewKindDisplay(
                card,
                "Prisoner",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Prisoner"),
                new Color(0.215f, 0.095f, 0.095f, 0.94f),
                V3CrewText.RedColor);
            this.classificationFormatter.ApplyCrewActivityDisplay(
                card,
                "Detained",
                ShuttleUIText.Tr("CT_ShuttleCrew_Detained"),
                V3CrewText.RedColor);

            card.CompartmentLabel = ShuttleUIText.Tr("CT_ShuttleCrew_Brig");
            card.Note = this.BuildPrisonCellOccupantNote(prisoner);
            this.presentationBuilder.FinalizeCrewCard(card, pawn, dynamicStatus);
            return card;
        }

        private void ApplyCargoActivity(V3CrewCardModel card, Pawn pawn)
        {
            bool detained = pawn != null && pawn.IsPrisonerOfColony;
            bool inCockpit =
                ShuttleCockpitPresenceClassifier.IsCockpitOccupant(pawn);
            this.classificationFormatter.ApplyCrewActivityDisplay(
                card,
                detained ? "Detained" : (inCockpit ? "InCockpit" : "LoadedCargo"),
                detained
                    ? ShuttleUIText.Tr("CT_ShuttleCrew_Detained")
                    : (inCockpit
                        ? ShuttleUIText.Tr("CT_ShuttleCrew_InsideCockpit")
                        : ShuttleUIText.Tr("CT_ShuttleCrew_LoadedCargo")),
                detained ? V3CrewText.RedColor : ShuttleUIStyle.MutedTextColor);
            card.CompartmentLabel = inCockpit
                ? ShuttleUIText.Tr("CT_ShuttleCrew_Cockpit")
                : ShuttleUIText.Tr("CT_ShuttleCrew_LoadedCargo");
        }

        private string BuildPrisonCellOccupantNote(
            ShuttleHeldPrisonerReadModel prisoner)
        {
            if (prisoner == null)
            {
                return string.Empty;
            }

            string note = string.Empty;
            if (!string.IsNullOrEmpty(prisoner.HealthLabel))
            {
                note = ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Health") +
                    ": " + prisoner.HealthLabel;
            }

            if (!string.IsNullOrEmpty(prisoner.InteractionModeLabel))
            {
                string interaction =
                    ShuttleUIText.Tr("CT_Shuttle_PrisonCell_InteractionMode") +
                    ": " + prisoner.InteractionModeLabel;
                note = string.IsNullOrEmpty(note)
                    ? interaction
                    : note + " | " + interaction;
            }

            return note;
        }

        private void ApplyPawnNeeds(
            V3CrewCardModel card,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (card == null)
            {
                return;
            }

            card.FoodPct = this.GetFoodPct(pawn);
            card.RestPct = this.GetRestPct(pawn);
            card.JoyPct = this.GetJoyPct(pawn);
            card.MoodPct = this.GetMoodPct(pawn);
            card.EnergyPct = -1f;
            this.ApplyDynamicNeeds(card, dynamicStatus, false);
        }

        private void ApplyDynamicNeeds(
            V3CrewCardModel card,
            ShuttlePawnDynamicStatusRecord dynamicStatus,
            bool includeEnergy)
        {
            if (card == null || dynamicStatus == null)
            {
                return;
            }

            this.SetKnownPct(ref card.FoodPct, dynamicStatus.FoodPct);
            this.SetKnownPct(ref card.RestPct, dynamicStatus.RestPct);
            this.SetKnownPct(ref card.JoyPct, dynamicStatus.JoyPct);
            this.SetKnownPct(ref card.MoodPct, dynamicStatus.MoodPct);
            if (includeEnergy && dynamicStatus.HasMechEnergy)
            {
                this.SetKnownPct(ref card.EnergyPct, dynamicStatus.EnergyPct);
            }
        }

        private void SetKnownPct(ref float target, float value)
        {
            if (value >= 0f)
            {
                target = Mathf.Clamp01(value);
            }
        }

        private float GetFoodPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.food != null
                ? pawn.needs.food.CurLevelPercentage
                : -1f;
        }

        private float GetRestPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.rest != null
                ? pawn.needs.rest.CurLevelPercentage
                : -1f;
        }

        private float GetJoyPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.joy != null
                ? pawn.needs.joy.CurLevelPercentage
                : -1f;
        }

        private float GetMoodPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.mood != null
                ? pawn.needs.mood.CurLevelPercentage
                : -1f;
        }

        private string GetHolderAppearancePendingTooltip()
        {
            return ShuttleUIText.Tr("CT_ShuttleCrew_UnknownAppearance");
        }
    }
}

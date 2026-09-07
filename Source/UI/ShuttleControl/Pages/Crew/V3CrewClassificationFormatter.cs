using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewClassificationFormatter
    {
        private readonly V3CrewDefinitionStyleResolver styleResolver =
            new V3CrewDefinitionStyleResolver();

        internal void ApplyPawnClassification(V3CrewCardModel card, Pawn pawn)
        {
            if (card == null || pawn == null || pawn.RaceProps == null)
            {
                this.ApplyEntityClassification(card, "?", ShuttleUIText.Tr("CT_ShuttleCrew_UnknownAppearance"));
                if (card != null)
                {
                    card.Note = ShuttleUIText.Tr("CT_ShuttleCrew_PreciseTypeUnavailable");
                }

                return;
            }

            if (pawn.RaceProps.Humanlike)
            {
                card.Group = V3CrewGroupKind.Human;
                card.FallbackIconText = "H";
                card.AppearanceTooltip = ShuttleUIText.Tr("CT_ShuttleCrew_AppearanceUnavailable");
                this.ApplyCrewKindDisplay(
                    card,
                    this.GetHumanKindKey(pawn),
                    this.GetHumanCategoryLabel(pawn),
                    this.GetHumanCardColor(pawn),
                    this.GetHumanCategoryTextColor(pawn));
                return;
            }

            if (pawn.RaceProps.IsMechanoid)
            {
                this.ApplyMechClassification(card);
                return;
            }

            if (pawn.RaceProps.Animal)
            {
                this.ApplyAnimalClassification(card);
                return;
            }

            this.ApplyEntityClassification(
                card,
                "E",
                ShuttleUIText.Tr("CT_ShuttleCrew_AppearanceUnavailable"));
        }

        internal void ApplyMechClassification(V3CrewCardModel card)
        {
            if (card == null)
            {
                return;
            }

            card.Group = V3CrewGroupKind.Mech;
            card.FallbackIconText = "M";
            card.AppearanceTooltip = ShuttleUIText.Tr("CT_ShuttleCrew_AppearanceUnavailable");
            this.ApplyCrewKindDisplay(
                card,
                "Mechanoid",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Mechanoid"),
                this.GetMechCardColor(),
                V3CrewText.BlueColor);
        }

        internal void ApplyAnimalClassification(V3CrewCardModel card)
        {
            if (card == null)
            {
                return;
            }

            card.Group = V3CrewGroupKind.Animal;
            card.FallbackIconText = "A";
            card.AppearanceTooltip = ShuttleUIText.Tr("CT_ShuttleCrew_AppearanceUnavailable");
            this.ApplyCrewKindDisplay(
                card,
                "Animal",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Animal"),
                this.GetAnimalCardColor(),
                V3CrewText.GreenColor);
        }

        internal void ApplyEntityClassification(
            V3CrewCardModel card,
            string fallbackIconText,
            string appearanceTooltip)
        {
            if (card == null)
            {
                return;
            }

            card.Group = V3CrewGroupKind.Entity;
            card.FallbackIconText = string.IsNullOrEmpty(fallbackIconText)
                ? "E"
                : fallbackIconText;
            card.AppearanceTooltip = appearanceTooltip;
            this.ApplyCrewKindDisplay(
                card,
                "Entity",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Entity"),
                this.GetEntityCardColor(),
                V3CrewText.PurpleColor);
        }

        internal void ApplyCrewKindDisplay(
            V3CrewCardModel card,
            string kindKey,
            string fallbackLabel,
            Color fallbackCardColor,
            Color fallbackTextColor)
        {
            this.styleResolver.ApplyCrewKindDisplay(
                card,
                kindKey,
                fallbackLabel,
                fallbackCardColor,
                fallbackTextColor);
        }

        internal void ApplyCrewActivityDisplay(
            V3CrewCardModel card,
            string activityKey,
            string fallbackLabel,
            Color fallbackColor)
        {
            this.styleResolver.ApplyCrewActivityDisplay(
                card,
                activityKey,
                fallbackLabel,
                fallbackColor);
        }

        internal string GetHabitatActivityLabel(HabitatOccupantActivity activity)
        {
            if (activity == HabitatOccupantActivity.Sleeping)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Activity_Resting");
            }

            if (activity == HabitatOccupantActivity.Dining)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Activity_Dining");
            }

            if (activity == HabitatOccupantActivity.Joying)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Activity_Recreation");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Crew_Activity_Standby");
        }

        internal string GetHabitatActivityKey(HabitatOccupantActivity activity)
        {
            if (activity == HabitatOccupantActivity.Sleeping)
            {
                return "Resting";
            }

            if (activity == HabitatOccupantActivity.Dining)
            {
                return "Eating";
            }

            if (activity == HabitatOccupantActivity.Joying)
            {
                return "Recreation";
            }

            return "Standby";
        }

        internal string GetDisplayActivityLabel(V3CrewCardModel card)
        {
            if (card == null)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_AwaitingOrders");
            }

            if (card.ActivityKey == "InCockpit")
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Driving");
            }

            if (card.ActivityKey == "Detained")
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_DetainedActivity");
            }

            if (!string.IsNullOrEmpty(card.ActivityLabel))
            {
                return card.ActivityLabel;
            }

            return ShuttleUIText.Tr("CT_ShuttleCrew_AwaitingOrders");
        }

        internal string GetCompartmentLabel(V3CrewCardModel card)
        {
            if (card == null)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Unassigned");
            }

            if (card.SourceKind == V3CrewCardSourceKind.Cockpit)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Cockpit");
            }

            if (card.SourceKind == V3CrewCardSourceKind.CargoLoaded)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_LoadedCargo");
            }

            if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_HabitatA");
            }

            if (card.SourceKind == V3CrewCardSourceKind.MedicalBay)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_MedicalBay");
            }

            if (card.SourceKind == V3CrewCardSourceKind.PrisonCell)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Brig");
            }

            if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_ChargingBay");
            }

            return ShuttleUIText.Tr("CT_ShuttleCrew_Unassigned");
        }

        internal string GetHumanKindKey(Pawn pawn)
        {
            if (pawn != null && pawn.IsPrisonerOfColony)
            {
                return "Prisoner";
            }

            if (pawn != null && pawn.IsSlaveOfColony)
            {
                return "Slave";
            }

            if (pawn != null && pawn.IsColonist)
            {
                return "Colonist";
            }

            return "Human";
        }

        internal string GetHumanCategoryLabel(Pawn pawn)
        {
            if (pawn != null && pawn.IsPrisonerOfColony)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Prisoner");
            }

            if (pawn != null && pawn.IsSlaveOfColony)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Slave");
            }

            if (pawn != null && pawn.IsColonist)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Colonist");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Crew_Human");
        }

        internal Color GetHumanCardColor(Pawn pawn)
        {
            if (pawn != null && pawn.IsPrisonerOfColony)
            {
                return new Color(0.215f, 0.095f, 0.095f, 0.94f);
            }

            if (pawn != null && pawn.IsSlaveOfColony)
            {
                return new Color(0.235f, 0.185f, 0.095f, 0.94f);
            }

            return this.GetHumanNeutralCardColor();
        }

        internal Color GetHumanCategoryTextColor(Pawn pawn)
        {
            if (pawn != null && pawn.IsPrisonerOfColony)
            {
                return V3CrewText.RedColor;
            }

            if (pawn != null && pawn.IsSlaveOfColony)
            {
                return V3CrewText.YellowColor;
            }

            return V3CrewText.BlueColor;
        }

        internal Color GetHumanNeutralCardColor()
        {
            return new Color(0.080f, 0.125f, 0.172f, 0.94f);
        }

        internal Color GetMedicalHumanCardColor()
        {
            return new Color(0.085f, 0.145f, 0.160f, 0.94f);
        }

        private Color GetMechCardColor()
        {
            return new Color(0.080f, 0.132f, 0.150f, 0.94f);
        }

        private Color GetAnimalCardColor()
        {
            return new Color(0.112f, 0.142f, 0.105f, 0.94f);
        }

        private Color GetEntityCardColor()
        {
            return new Color(0.122f, 0.110f, 0.150f, 0.94f);
        }

    }
}

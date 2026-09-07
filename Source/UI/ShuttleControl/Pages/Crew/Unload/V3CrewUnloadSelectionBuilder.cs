using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewUnloadSelectionBuilder
    {
        internal List<V3CrewUnloadOption> BuildCrew(
            V3CrewPageData pageData,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            List<V3CrewUnloadOption> options = new List<V3CrewUnloadOption>();
            if (pageData == null)
            {
                return options;
            }

            this.AddCrewOptions(
                options,
                pageData.Humans,
                loadedCrewActions,
                habitatActions,
                mechChargerActions);
            this.AddCrewOptions(
                options,
                pageData.Mechs,
                loadedCrewActions,
                habitatActions,
                mechChargerActions);
            this.AddCrewOptions(
                options,
                pageData.Animals,
                loadedCrewActions,
                habitatActions,
                mechChargerActions);
            return options;
        }

        private void AddCrewOptions(
            List<V3CrewUnloadOption> options,
            List<V3CrewCardModel> cards,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            if (options == null || cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                V3CrewUnloadOption option =
                    this.BuildCrewOption(
                        cards[i],
                        loadedCrewActions,
                        habitatActions,
                        mechChargerActions);
                if (option != null)
                {
                    options.Add(option);
                }
            }
        }

        private V3CrewUnloadOption BuildCrewOption(
            V3CrewCardModel card,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            if (!this.IsCrewCandidate(card))
            {
                return null;
            }

            V3CrewUnloadOption option = new V3CrewUnloadOption();
            option.Card = card;
            option.StableKey = BuildStableKey(card);
            option.CanUnload = this.CanUnloadCrew(
                card,
                loadedCrewActions,
                habitatActions,
                mechChargerActions);
            option.DisabledReason =
                this.GetCrewDisabledReason(
                    card,
                    loadedCrewActions,
                    habitatActions,
                    mechChargerActions,
                    option.CanUnload);
            option.SelectedByDefault = option.CanUnload;
            return option;
        }

        private bool IsCrewCandidate(V3CrewCardModel card)
        {
            if (card == null)
            {
                return false;
            }

            if (card.Group == V3CrewGroupKind.Human)
            {
                return (IsLoadedCrewSource(card.SourceKind) ||
                    card.SourceKind == V3CrewCardSourceKind.Habitat) &&
                    IsOrdinaryColonist(card.DisplayThing as Pawn);
            }

            if (card.Group == V3CrewGroupKind.Mech)
            {
                return (IsLoadedCrewSource(card.SourceKind) ||
                    card.SourceKind == V3CrewCardSourceKind.MechCharger) &&
                    IsAvailablePawn(card.DisplayThing as Pawn);
            }

            return card.Group == V3CrewGroupKind.Animal &&
                IsLoadedCrewSource(card.SourceKind) &&
                IsAvailablePawn(card.DisplayThing as Pawn);
        }

        private static bool IsOrdinaryColonist(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike &&
                pawn.IsColonist &&
                !pawn.IsPrisonerOfColony &&
                !pawn.IsPrisoner &&
                !pawn.IsSlaveOfColony &&
                !pawn.IsSlave;
        }

        private static bool IsAvailablePawn(Pawn pawn)
        {
            return pawn != null && !pawn.Destroyed && !pawn.Dead;
        }

        private bool CanUnloadCrew(
            V3CrewCardModel card,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            if (card == null)
            {
                return false;
            }

            if (IsLoadedCrewSource(card.SourceKind))
            {
                return loadedCrewActions != null &&
                    loadedCrewActions.CanUnloadLoadedCrew(
                        card.TransporterIndex,
                        card.LoadedIndex,
                        card.ThingIDNumber,
                        card.DefName);
            }

            if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                return habitatActions != null &&
                    habitatActions.CanEjectHabitatOccupant(card.ThingIDNumber);
            }

            if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                return mechChargerActions != null &&
                    mechChargerActions.CanEjectChargingMech(card.ThingIDNumber);
            }

            return false;
        }

        private string GetCrewDisabledReason(
            V3CrewCardModel card,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions,
            bool canUnload)
        {
            if (canUnload)
            {
                return null;
            }

            if (card == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
            }

            if (IsLoadedCrewSource(card.SourceKind))
            {
                return loadedCrewActions != null
                    ? loadedCrewActions.GetLoadedCrewUnloadTooltip(
                        card.TransporterIndex,
                        card.LoadedIndex,
                        card.ThingIDNumber,
                        card.DefName)
                    : ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
            }

            if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                return habitatActions != null
                    ? habitatActions.GetHabitatOccupantEjectTooltip(card.ThingIDNumber)
                    : ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
            }

            if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                return mechChargerActions != null
                    ? mechChargerActions.GetChargingMechEjectTooltip(card.ThingIDNumber)
                    : ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
            }

            return ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        internal static bool IsLoadedCrewSource(V3CrewCardSourceKind sourceKind)
        {
            return sourceKind == V3CrewCardSourceKind.Cockpit ||
                sourceKind == V3CrewCardSourceKind.CargoLoaded;
        }

        internal static string BuildStableKey(V3CrewCardModel card)
        {
            if (card == null)
            {
                return null;
            }

            return card.SourceKind.ToString() + "|" +
                card.ThingIDNumber.ToString() + "|" +
                card.TransporterIndex.ToString() + "|" +
                card.LoadedIndex.ToString() + "|" +
                (card.DefName ?? string.Empty);
        }
    }
}

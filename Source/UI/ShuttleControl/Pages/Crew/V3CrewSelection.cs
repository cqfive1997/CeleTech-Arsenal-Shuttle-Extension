using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal static class V3CrewSelection
    {
        internal static void Select(V3CrewPageState state, V3CrewCardModel card)
        {
            if (state == null || card == null)
            {
                return;
            }

            state.SelectedCrewMemberThingID = card.ThingIDNumber;
            state.SelectedCrewMemberKey = BuildSelectionKey(card);
            state.DetailScroll = UnityEngine.Vector2.zero;
        }

        internal static void Clear(V3CrewPageState state)
        {
            if (state == null)
            {
                return;
            }

            state.SelectedCrewMemberThingID = -1;
            state.SelectedCrewMemberKey = null;
            state.DetailScroll = UnityEngine.Vector2.zero;
        }

        internal static bool IsSelected(V3CrewCardModel card, V3CrewPageState state)
        {
            if (card == null || state == null)
            {
                return false;
            }

            if (card.ThingIDNumber > 0)
            {
                return state.SelectedCrewMemberThingID == card.ThingIDNumber;
            }

            return !string.IsNullOrEmpty(card.DefName) &&
                card.DefName == state.SelectedCrewMemberKey;
        }

        internal static V3CrewCardModel FindSelectedCrewCard(
            V3CrewPageData pageData,
            V3CrewPageState state)
        {
            if (pageData == null || state == null)
            {
                return null;
            }

            V3CrewCardModel selected = FindSelectedCrewCardInList(pageData.Humans, state);
            if (selected != null)
            {
                return selected;
            }

            selected = FindSelectedCrewCardInList(pageData.Mechs, state);
            if (selected != null)
            {
                return selected;
            }

            selected = FindSelectedCrewCardInList(pageData.Animals, state);
            if (selected != null)
            {
                return selected;
            }

            return FindSelectedCrewCardInList(pageData.Entities, state);
        }

        internal static string BuildSelectionKey(V3CrewCardModel card)
        {
            if (card == null)
            {
                return null;
            }

            if (card.ThingIDNumber > 0)
            {
                return card.ThingIDNumber.ToString();
            }

            if (!string.IsNullOrEmpty(card.DefName))
            {
                return card.DefName;
            }

            return card.Label;
        }

        private static V3CrewCardModel FindSelectedCrewCardInList(
            List<V3CrewCardModel> cards,
            V3CrewPageState state)
        {
            if (cards == null || state == null)
            {
                return null;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                V3CrewCardModel card = cards[i];
                if (card == null)
                {
                    continue;
                }

                if (card.ThingIDNumber > 0 &&
                    card.ThingIDNumber == state.SelectedCrewMemberThingID)
                {
                    return card;
                }

                string key = BuildSelectionKey(card);
                if (!string.IsNullOrEmpty(key) && key == state.SelectedCrewMemberKey)
                {
                    return card;
                }
            }

            return null;
        }
    }
}

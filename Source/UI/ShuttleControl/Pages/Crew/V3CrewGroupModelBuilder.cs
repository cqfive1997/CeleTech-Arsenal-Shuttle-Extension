namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewGroupModelBuilder
    {
        internal void AddCardToGroup(V3CrewPageData data, V3CrewCardModel card)
        {
            if (data == null || card == null)
            {
                return;
            }

            if (card.Group == V3CrewGroupKind.Human)
            {
                data.Humans.Add(card);
            }
            else if (card.Group == V3CrewGroupKind.Mech)
            {
                data.Mechs.Add(card);
            }
            else if (card.Group == V3CrewGroupKind.Animal)
            {
                data.Animals.Add(card);
            }
            else
            {
                data.Entities.Add(card);
            }
        }

        internal void ApplyTotals(V3CrewPageData data)
        {
            if (data == null)
            {
                return;
            }

            data.TotalCount =
                data.Humans.Count +
                data.Mechs.Count +
                data.Animals.Count +
                data.Entities.Count;
        }
    }
}

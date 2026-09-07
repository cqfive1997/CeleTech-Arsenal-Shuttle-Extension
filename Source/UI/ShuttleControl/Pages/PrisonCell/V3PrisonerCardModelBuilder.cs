using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonerCardModelBuilder
    {
        internal IReadOnlyList<ShuttleHeldPrisonerReadModel> BuildPrisoners(
            IReadOnlyList<ShuttleHeldPrisonerReadModel> source)
        {
            List<ShuttleHeldPrisonerReadModel> prisoners =
                new List<ShuttleHeldPrisonerReadModel>();
            if (source == null)
            {
                return prisoners;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ShuttleHeldPrisonerReadModel prisoner = this.CopyPrisoner(source[i]);
                if (prisoner != null)
                {
                    prisoners.Add(prisoner);
                }
            }

            return prisoners;
        }

        private ShuttleHeldPrisonerReadModel CopyPrisoner(
            ShuttleHeldPrisonerReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleHeldPrisonerReadModel target =
                new ShuttleHeldPrisonerReadModel();
            target.ThingIDNumber = source.ThingIDNumber;
            target.Label = source.Label;
            target.FactionLabel = source.FactionLabel;
            target.HostFactionLabel = source.HostFactionLabel;
            target.InteractionModeLabel = source.InteractionModeLabel;
            target.FoodLevelPercent = source.FoodLevelPercent;
            target.FoodLabel = source.FoodLabel;
            target.HealthLabel = source.HealthLabel;
            target.IsDowned = source.IsDowned;
            target.IsDead = source.IsDead;
            target.CanEject = source.CanEject;
            target.NeedsFeeding = source.NeedsFeeding;
            target.CanFeed = source.CanFeed;
            target.FeedTooltip = source.FeedTooltip;
            target.NeedsTending = source.NeedsTending;
            target.CanTend = source.CanTend;
            target.TendTooltip = source.TendTooltip;
            target.TendStatusLabel = source.TendStatusLabel;
            target.DisplayThing = source.DisplayThing;
            return target;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat
{
    /// <summary>
    /// Narrow Habitat-facing food source contract. Callers can request real food Things
    /// without learning about transporters, loaded cargo containers, or pending load queues.
    /// </summary>
    public interface IShuttleHabitatFoodSource
    {
        /// <summary>
        /// Checks whether shuttle cargo can currently provide an edible food stack.
        /// This is a read-only probe and must not move cargo.
        /// </summary>
        bool CanProvideFood(
            ThingWithComps shuttleHost,
            Pawn eater,
            HabitatProfile habitat);

        /// <summary>
        /// Moves real shuttle cargo food into the provided destination and returns a transaction
        /// that can be committed by Habitat or rolled back to cargo if entry fails.
        /// </summary>
        bool TryBeginHabitatFoodWithdrawal(
            ThingWithComps shuttleHost,
            Pawn eater,
            HabitatProfile habitat,
            ThingOwner<Thing> destination,
            string reason,
            out HabitatFoodWithdrawal withdrawal);
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    /// <summary>
    /// Single policy for Odyssey destinations protected by signal jamming.
    /// Capability is read from the derived command profile, never from live state.
    /// </summary>
    internal static class ShuttleSignalJammerAccessPolicy
    {
        internal static bool CanReach(WorldObject destination, ShuttleProfile profile)
        {
            if (!ModsConfig.OdysseyActive ||
                destination == null ||
                !destination.RequiresSignalJammerToReach)
            {
                return true;
            }

            return profile != null &&
                profile.Command != null &&
                profile.Command.ProvidesSecureSignalLink;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleLaunchCrewRequirementUtility
    {
        internal static bool HasLaunchController(
            ShuttleCargoSnapshot cargoSnapshot,
            ThingWithComps host)
        {
            return HasLoadedColonistInCockpit(cargoSnapshot) ||
                HasLocalAwayMapColonistController(host);
        }

        internal static bool HasLaunchController(
            ShuttleCargoSnapshot cargoSnapshot,
            ThingWithComps host,
            ShuttleProfile profile)
        {
            return ProfileAllowsAutonomousLaunch(profile) ||
                HasLaunchController(cargoSnapshot, host);
        }

        internal static bool ProfileAllowsAutonomousLaunch(ShuttleProfile profile)
        {
            return profile != null &&
                profile.Command != null &&
                profile.Command.ProvidesAutonomousLaunchControl;
        }

        internal static bool HasLoadedColonistInCockpit(ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot == null || cargoSnapshot.Items == null)
            {
                return false;
            }

            for (int i = 0; i < cargoSnapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[i];
                if (item == null || !item.IsLoaded || !item.IsPawn)
                {
                    continue;
                }

                Pawn pawn = item.DisplayThing as Pawn;
                if (pawn != null && pawn.IsColonist && !pawn.Dead)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLocalAwayMapColonistController(ThingWithComps host)
        {
            Map map = host != null ? host.Map : null;
            if (map == null || map.IsPlayerHome || map.mapPawns == null)
            {
                return false;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null &&
                    !pawn.Dead &&
                    !pawn.Downed &&
                    !pawn.InMentalState &&
                    pawn.IsColonistPlayerControlled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

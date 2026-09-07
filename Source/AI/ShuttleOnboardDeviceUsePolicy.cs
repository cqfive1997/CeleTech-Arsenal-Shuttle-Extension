using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI
{
    /// <summary>
    /// Read-only policy for player-home use of onboard facilities. It does not admit,
    /// move, reserve, or service occupants.
    /// </summary>
    internal static class ShuttleOnboardDeviceUsePolicy
    {
        internal static bool AllowsColonistFacilityUse(
            Pawn pawn,
            Thing shuttleHost,
            out string failReason)
        {
            Map map = shuttleHost != null ? shuttleHost.Map : null;
            return AllowsColonistFacilityUse(pawn, map, out failReason);
        }

        internal static bool AllowsColonistFacilityUse(
            Pawn pawn,
            Map hostMap,
            out string failReason)
        {
            failReason = null;
            if (!IsPlayerColonist(pawn) || !IsPlayerHome(hostMap))
            {
                return true;
            }

            ShuttleOtherSettings other = GetOtherSettings();
            if (other == null || other.AllowColonistOnboardDeviceUseAtPlayerHome)
            {
                return true;
            }

            failReason = "CT_Shuttle_OnboardUse_ColonistsDisabledAtBase".Translate().ToString();
            return false;
        }

        internal static bool AllowsMechFacilityUse(
            Pawn mech,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            Map map = shuttleHost != null ? shuttleHost.Map : null;
            if (!IsPlayerMech(mech) || !IsPlayerHome(map))
            {
                return true;
            }

            ShuttleOtherSettings other = GetOtherSettings();
            if (other == null || other.AllowMechOnboardDeviceUseAtPlayerHome)
            {
                return true;
            }

            failReason = "CT_Shuttle_OnboardUse_MechsDisabledAtBase".Translate().ToString();
            return false;
        }

        private static ShuttleOtherSettings GetOtherSettings()
        {
            return CeleTechShuttleMod.Settings != null
                ? CeleTechShuttleMod.Settings.Other
                : null;
        }

        private static bool IsPlayerHome(Map map)
        {
            return map != null && map.IsPlayerHome;
        }

        private static bool IsPlayerColonist(Pawn pawn)
        {
            return pawn != null &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike &&
                pawn.IsColonist;
        }

        private static bool IsPlayerMech(Pawn pawn)
        {
            return pawn != null &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.RaceProps != null &&
                pawn.RaceProps.IsMechanoid;
        }
    }
}

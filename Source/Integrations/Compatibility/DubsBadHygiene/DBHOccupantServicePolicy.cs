using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    /// <summary>
    /// Defines the holder roles DBH may service. It never creates DBH needs and never
    /// mutates shuttle holders; the runtime remains responsible only for need servicing.
    /// </summary>
    internal static class DBHOccupantServicePolicy
    {
        internal const ShuttleExternalOccupantRole SupportedRoles =
            ShuttleExternalOccupantRole.Habitat |
            ShuttleExternalOccupantRole.MedicalPatient |
            ShuttleExternalOccupantRole.Prisoner;

        internal static bool CanService(
            ShuttleExternalOccupantInfo occupant,
            Pawn pawn,
            ShuttleExternalHostInfo hostInfo)
        {
            if (occupant == null ||
                (occupant.Role & SupportedRoles) == 0 ||
                !occupant.CanReceiveExternalService ||
                !occupant.IsHumanlike ||
                occupant.IsMechanoid ||
                pawn == null ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike ||
                pawn.Dead ||
                pawn.Destroyed)
            {
                return false;
            }

            return !occupant.IsColonist ||
                hostInfo == null ||
                hostInfo.PlayerColonistOnboardFacilityUseAllowed;
        }
    }
}

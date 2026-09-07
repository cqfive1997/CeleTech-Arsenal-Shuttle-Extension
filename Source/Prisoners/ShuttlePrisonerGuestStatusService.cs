using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Centralized boundary for vanilla guest/prisoner state changes. Holder comps,
    /// jobs, and UI must not call pawn.guest mutation APIs directly.
    /// </summary>
    internal sealed class ShuttlePrisonerGuestStatusService
    {
        internal bool EnsurePrisonerStatusForAdmission(
            Pawn prisoner,
            Faction hostFaction,
            out ShuttlePrisonerAdmissionContext context,
            out string failReason)
        {
            context = default(ShuttlePrisonerAdmissionContext);
            failReason = null;
            if (prisoner == null || prisoner.Destroyed || prisoner.Dead || prisoner.guest == null)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            Faction resolvedHost = hostFaction ?? Faction.OfPlayer;
            if (resolvedHost == null)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            bool wasPrisonerOnAdmission = prisoner.guest.IsPrisoner;
            if (wasPrisonerOnAdmission)
            {
                if (prisoner.guest.HostFaction == null)
                {
                    // Repair legacy/broken prisoner state while the pawn is still spawned.
                    // Carried admission only accepts an already prepared player prisoner.
                    prisoner.guest.SetGuestStatus(resolvedHost, GuestStatus.Prisoner);
                }
                else if (prisoner.guest.HostFaction != resolvedHost)
                {
                    failReason = "CT_Shuttle_PrisonCell_NotPlayerPrisoner".Translate().ToString();
                    return false;
                }

                context = ShuttlePrisonerAdmissionContext.FromPawn(
                    prisoner,
                    prisoner.guest.HostFaction ?? resolvedHost);
                return true;
            }

            if (prisoner.RaceProps == null ||
                !prisoner.RaceProps.Humanlike ||
                prisoner.RaceProps.IsMechanoid ||
                !prisoner.Downed ||
                !prisoner.HostileTo(resolvedHost))
            {
                failReason = "CT_Shuttle_PrisonCell_PrisonerNotDownedOrPrisoner".Translate().ToString();
                return false;
            }

            // P3 prepares hostile downed pawns before the carry job removes them
            // from the map. CapturedBy is the vanilla status transition; later
            // warden/bed/prison-break systems are intentionally not invoked here.
            prisoner.guest.CapturedBy(resolvedHost, null);
            context = new ShuttlePrisonerAdmissionContext(
                resolvedHost,
                prisoner.guest.ExclusiveInteractionMode,
                false,
                Find.TickManager != null ? Find.TickManager.TicksGame : -1);
            return true;
        }

        internal bool TryBuildPreparedAdmissionContext(
            Pawn prisoner,
            Faction hostFaction,
            out ShuttlePrisonerAdmissionContext context,
            out string failReason)
        {
            context = default(ShuttlePrisonerAdmissionContext);
            failReason = null;
            if (prisoner == null || prisoner.Destroyed || prisoner.Dead || prisoner.guest == null)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            Faction resolvedHost = hostFaction ?? Faction.OfPlayer;
            if (resolvedHost == null)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (!prisoner.guest.IsPrisoner || prisoner.guest.HostFaction == null)
            {
                failReason = "CT_Shuttle_PrisonCell_PrisonerNotPrepared".Translate().ToString();
                return false;
            }

            if (prisoner.guest.HostFaction != resolvedHost)
            {
                failReason = "CT_Shuttle_PrisonCell_NotPlayerPrisoner".Translate().ToString();
                return false;
            }

            context = ShuttlePrisonerAdmissionContext.FromPawn(prisoner, resolvedHost);
            return true;
        }
    }
}

using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal struct ShuttlePrisonerAdmissionContext
    {
        internal readonly Faction HostFaction;
        internal readonly PrisonerInteractionModeDef InteractionMode;
        internal readonly bool WasPrisonerOnAdmission;
        internal readonly int AdmissionTick;

        internal ShuttlePrisonerAdmissionContext(
            Faction hostFaction,
            PrisonerInteractionModeDef interactionMode,
            bool wasPrisonerOnAdmission,
            int admissionTick)
        {
            this.HostFaction = hostFaction;
            this.InteractionMode = interactionMode;
            this.WasPrisonerOnAdmission = wasPrisonerOnAdmission;
            this.AdmissionTick = admissionTick;
        }

        internal static ShuttlePrisonerAdmissionContext FromPawn(Pawn pawn, Faction hostFaction)
        {
            Pawn_GuestTracker guest = pawn != null ? pawn.guest : null;
            return new ShuttlePrisonerAdmissionContext(
                hostFaction,
                guest != null ? guest.ExclusiveInteractionMode : null,
                guest != null && guest.IsPrisoner,
                Find.TickManager != null ? Find.TickManager.TicksGame : -1);
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI
{
    /// <summary>
    /// Prevents one pawn from entering a competing module holder when that pawn is
    /// already assigned to this shuttle's passenger plan. Unrelated cargo and pawns
    /// do not block facility use.
    /// </summary>
    internal static class ShuttlePassengerFacilityAdmissionPolicy
    {
        internal static bool AllowsEntry(
            Pawn pawn,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (pawn == null || shuttleWithComps == null)
            {
                return true;
            }

            CompModularShuttleCore core =
                shuttleWithComps.TryGetComp<CompModularShuttleCore>();
            ShuttleController controller = core != null ? core.Controller : null;
            if (controller == null ||
                !controller.IsPassengerScheduledForBoarding(pawn))
            {
                return true;
            }

            failReason = "CT_Shuttle_OnboardUse_PawnBoardingScheduled"
                .Translate(pawn.LabelShortCap)
                .ToString();
            return false;
        }
    }
}

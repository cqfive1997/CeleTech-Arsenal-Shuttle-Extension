using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularShuttle : CompProperties_Shuttle
    {
        public CompProperties_ModularShuttle()
        {
            this.compClass = typeof(CompModularShuttle);
        }
    }

    /// <summary>
    /// Shuttle backend for transport-ship handoff. The modular shuttle owns its UI,
    /// so vanilla loading/autoload/enter-shuttle entries are quarantined here.
    /// </summary>
    public sealed class CompModularShuttle : CompShuttle
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (ShuttleCriticalDamagePolicy.ShouldRestrictPlayerInteractions(this.parent))
            {
                yield break;
            }

            foreach (FloatMenuOption option in ShuttleCockpitBoardingFloatMenuUtility.GetFloatMenuOptions(
                selPawn,
                this.parent))
            {
                yield return option;
            }

            foreach (FloatMenuOption option in ShuttleMedicalBayFloatMenuUtility.GetFloatMenuOptions(
                selPawn,
                this.parent))
            {
                yield return option;
            }
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            if (ShuttleCriticalDamagePolicy.ShouldRestrictPlayerInteractions(this.parent))
            {
                yield break;
            }

            foreach (FloatMenuOption option in ShuttleCockpitBoardingFloatMenuUtility.GetMultiSelectFloatMenuOptions(
                selPawns,
                this.parent))
            {
                yield return option;
            }
        }
    }
}

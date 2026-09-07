using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Weapon
{
    public sealed class WorkGiver_ReloadShuttleWeapon : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ShuttleWeaponReloadWorkUtility.AllShuttlesNeedingWeaponReload(
                pawn != null ? pawn.Map : null);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return ShuttleWeaponReloadWorkUtility.CanPawnReloadShuttleWeapon(pawn, t, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return ShuttleWeaponReloadWorkUtility.TryMakeReloadJob(pawn, t, forced);
        }
    }
}

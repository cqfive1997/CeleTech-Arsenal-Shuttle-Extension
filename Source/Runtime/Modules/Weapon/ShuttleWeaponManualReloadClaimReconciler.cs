using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Reconciles the durable Pawn reload claim with the actual current Pawn job. It owns no
    /// reload policy and only releases claims whose saved Pawn/Job identity is no longer live.
    /// </summary>
    internal sealed class ShuttleWeaponManualReloadClaimReconciler
    {
        private const string ReloadJobDefName = "CT_Shuttle_ReloadShuttleWeapon";

        internal void Reconcile(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponAmmoState ammoState)
        {
            if (ammoState == null || !ammoState.ManualReloadJobActive)
            {
                return;
            }

            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null && host.Spawned ? host.Map : null;
            int pawnThingID = ammoState.ManualReloadPawnThingID;
            int jobLoadID = ammoState.ManualReloadJobLoadID;
            if (map == null || pawnThingID < 0 || jobLoadID < 0)
            {
                ammoState.ClearManualReloadJobActive();
                return;
            }

            Pawn pawn = this.FindSpawnedPawn(map, pawnThingID);
            Job job = pawn != null ? pawn.CurJob : null;
            if (job == null ||
                job.def == null ||
                job.def.defName != ReloadJobDefName ||
                job.loadID != jobLoadID ||
                job.GetTarget(TargetIndex.A).Thing != host)
            {
                ammoState.ClearManualReloadJobActive();
            }
        }

        private Pawn FindSpawnedPawn(Map map, int pawnThingID)
        {
            IReadOnlyList<Pawn> pawns = map != null && map.mapPawns != null
                ? map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}

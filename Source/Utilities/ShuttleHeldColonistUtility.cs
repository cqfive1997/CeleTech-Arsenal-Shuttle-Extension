using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal static class ShuttleHeldColonistUtility
    {
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";

        private static ThingDef cachedShuttleDef;

        internal static bool AnyLivingFreeColonistHeldInOnMapShuttle()
        {
            if (Current.Game == null || Find.Maps == null)
            {
                return false;
            }

            ThingDef shuttleDef = GetShuttleDef();
            if (shuttleDef == null)
            {
                return false;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map.listerThings == null)
                {
                    continue;
                }

                List<Thing> shuttles = map.listerThings.ThingsOfDef(shuttleDef);
                if (shuttles == null)
                {
                    continue;
                }

                for (int j = 0; j < shuttles.Count; j++)
                {
                    Thing shuttle = shuttles[j];
                    if (!IsActiveOnMapShuttle(shuttle))
                    {
                        continue;
                    }

                    if (AnyLivingFreeColonistHeldBy(shuttle))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool AnyLivingFreeColonistHeldBy(Thing shuttle)
        {
            List<ShuttleHeldThingOwnerRecord> owners =
                ShuttleHeldThingScanner.CollectHeldThingOwners(shuttle);
            for (int i = 0; i < owners.Count; i++)
            {
                ThingOwner owner = owners[i] != null ? owners[i].Owner : null;
                if (owner == null)
                {
                    continue;
                }

                for (int j = 0; j < owner.Count; j++)
                {
                    Pawn pawn = owner[j] as Pawn;
                    if (IsLivingFreeColonist(pawn))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsActiveOnMapShuttle(Thing thing)
        {
            return thing is Building_ModularShuttle &&
                !thing.Destroyed &&
                thing.Spawned &&
                thing.Map != null;
        }

        private static bool IsLivingFreeColonist(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.IsColonist &&
                pawn.HostFaction == null;
        }

        private static ThingDef GetShuttleDef()
        {
            if (cachedShuttleDef == null)
            {
                cachedShuttleDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            }

            return cachedShuttleDef;
        }
    }
}

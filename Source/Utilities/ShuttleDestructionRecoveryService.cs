using System.Collections.Generic;
using System.Text;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    /// <summary>
    /// Final on-map recovery boundary used immediately before the shuttle host is
    /// irreversibly destroyed. Normal launch handoff never calls this service.
    /// </summary>
    internal static class ShuttleDestructionRecoveryService
    {
        internal static bool TryRecoverBeforeDestruction(
            ThingWithComps shuttle,
            Map map,
            out string failureReason)
        {
            failureReason = null;
            if (shuttle == null || shuttle.Destroyed || map == null || !shuttle.Spawned)
            {
                failureReason = "shuttle or destruction map is unavailable";
                return false;
            }

            List<ShuttleHeldThingOwnerRecord> owners =
                ShuttleHeldThingScanner.CollectHeldThingOwners(shuttle);
            for (int i = 0; i < owners.Count; i++)
            {
                DropRecoverableContents(owners[i], shuttle.Position, map);
            }

            return VerifyAllOwnersEmpty(owners, out failureReason);
        }

        private static void DropRecoverableContents(
            ShuttleHeldThingOwnerRecord ownerRecord,
            IntVec3 near,
            Map map)
        {
            ThingOwner owner = ownerRecord != null ? ownerRecord.Owner : null;
            if (owner == null || owner.Count == 0)
            {
                return;
            }

            List<Thing> snapshot = new List<Thing>();
            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (ShuttleHeldThingScanner.IsRecoverableHeldThing(thing))
                {
                    snapshot.Add(thing);
                }
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing thing = snapshot[i];
                if (thing == null || thing.Destroyed || !owner.Contains(thing))
                {
                    continue;
                }

                Thing resultingThing;
                owner.TryDrop(
                    thing,
                    near,
                    map,
                    ThingPlaceMode.Near,
                    out resultingThing,
                    null,
                    null);
            }
        }

        private static bool VerifyAllOwnersEmpty(
            List<ShuttleHeldThingOwnerRecord> owners,
            out string failureReason)
        {
            failureReason = null;
            if (owners == null)
            {
                return true;
            }

            StringBuilder unresolved = new StringBuilder();
            for (int i = 0; i < owners.Count; i++)
            {
                ShuttleHeldThingOwnerRecord ownerRecord = owners[i];
                ThingOwner owner = ownerRecord != null ? ownerRecord.Owner : null;
                if (owner == null || owner.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < owner.Count; j++)
                {
                    Thing thing = owner[j];
                    if (unresolved.Length > 0)
                    {
                        unresolved.Append("; ");
                    }

                    unresolved.Append(ownerRecord.Path ?? "unknown-holder");
                    unresolved.Append(" -> ");
                    unresolved.Append(ShuttleHeldThingScanner.DescribeThing(thing));
                }
            }

            if (unresolved.Length == 0)
            {
                return true;
            }

            failureReason = unresolved.ToString();
            return false;
        }
    }
}

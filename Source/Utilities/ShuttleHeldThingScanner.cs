using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal sealed class ShuttleHeldThingOwnerRecord
    {
        internal ShuttleHeldThingOwnerRecord(ThingOwner owner, string path)
        {
            this.Owner = owner;
            this.Path = path;
        }

        internal ThingOwner Owner { get; private set; }
        internal string Path { get; private set; }
    }

    internal static class ShuttleHeldThingScanner
    {
        internal static bool ContainsRecoverableHeldThings(Thing root, out string debugDump)
        {
            List<ShuttleHeldThingOwnerRecord> owners = CollectHeldThingOwners(root);
            List<string> recoverable = new List<string>();
            for (int i = 0; i < owners.Count; i++)
            {
                ShuttleHeldThingOwnerRecord ownerRecord = owners[i];
                ThingOwner owner = ownerRecord != null ? ownerRecord.Owner : null;
                if (owner == null)
                {
                    continue;
                }

                for (int j = 0; j < owner.Count; j++)
                {
                    Thing heldThing = owner[j];
                    if (!IsRecoverableHeldThing(heldThing))
                    {
                        continue;
                    }

                    recoverable.Add(
                        (ownerRecord.Path ?? "unknown") +
                        " -> " +
                        DescribeThing(heldThing));
                }
            }

            debugDump = recoverable.Count > 0
                ? string.Join("; ", recoverable.ToArray())
                : "no recoverable held Things";
            return recoverable.Count > 0;
        }

        internal static List<ShuttleHeldThingOwnerRecord> CollectHeldThingOwners(Thing root)
        {
            List<ShuttleHeldThingOwnerRecord> owners = new List<ShuttleHeldThingOwnerRecord>();
            CollectHeldThingOwners(
                root,
                "root:" + DescribeThing(root),
                owners,
                new HashSet<Thing>(),
                new HashSet<ThingOwner>());
            return owners;
        }

        internal static bool IsRecoverableHeldThing(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            if (thing is ActiveTransporter)
            {
                return false;
            }

            if (thing is Skyfaller)
            {
                return false;
            }

            return true;
        }

        internal static string DescribeThing(Thing thing)
        {
            if (thing == null)
            {
                return "null";
            }

            return thing.GetType().Name +
                "(def=" +
                (thing.def != null ? thing.def.defName : "null") +
                ", thingID=" +
                thing.thingIDNumber +
                ", label=" +
                thing.LabelShortCap +
                ")";
        }

        private static void CollectHeldThingOwners(
            Thing thing,
            string path,
            List<ShuttleHeldThingOwnerRecord> outOwners,
            HashSet<Thing> visitedThings,
            HashSet<ThingOwner> visitedOwners)
        {
            if (thing == null || thing.Destroyed || visitedThings.Contains(thing))
            {
                return;
            }

            visitedThings.Add(thing);

            ActiveTransporter activeTransporter = thing as ActiveTransporter;
            if (activeTransporter != null &&
                activeTransporter.Contents != null &&
                activeTransporter.Contents.innerContainer != null)
            {
                AddHeldOwnerRecord(
                    activeTransporter.Contents.innerContainer,
                    path + ".ActiveTransporterInfo.innerContainer",
                    outOwners,
                    visitedThings,
                    visitedOwners);
            }

            IThingHolder holder = thing as IThingHolder;
            if (holder != null)
            {
                CollectHolderOwners(
                    holder,
                    path + ".IThingHolder",
                    outOwners,
                    visitedThings,
                    visitedOwners);
            }

            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null || thingWithComps.AllComps == null)
            {
                return;
            }

            for (int i = 0; i < thingWithComps.AllComps.Count; i++)
            {
                CompTransporter compTransporter = thingWithComps.AllComps[i] as CompTransporter;
                if (compTransporter != null)
                {
                    AddHeldOwnerRecord(
                        compTransporter.GetDirectlyHeldThings(),
                        path + ".comp:CompTransporter.innerContainer",
                        outOwners,
                        visitedThings,
                        visitedOwners);
                }

                IThingHolder compHolder = thingWithComps.AllComps[i] as IThingHolder;
                if (compHolder == null)
                {
                    continue;
                }

                CollectHolderOwners(
                    compHolder,
                    path + ".comp:" + thingWithComps.AllComps[i].GetType().Name,
                    outOwners,
                    visitedThings,
                    visitedOwners);
            }
        }

        private static void CollectHolderOwners(
            IThingHolder holder,
            string path,
            List<ShuttleHeldThingOwnerRecord> outOwners,
            HashSet<Thing> visitedThings,
            HashSet<ThingOwner> visitedOwners)
        {
            if (holder == null)
            {
                return;
            }

            AddHeldOwnerRecord(
                holder.GetDirectlyHeldThings(),
                path + ".GetDirectlyHeldThings",
                outOwners,
                visitedThings,
                visitedOwners);

            List<IThingHolder> childHolders = new List<IThingHolder>();
            holder.GetChildHolders(childHolders);
            for (int i = 0; i < childHolders.Count; i++)
            {
                CollectHolderOwners(
                    childHolders[i],
                    path + ".child[" + i + "]",
                    outOwners,
                    visitedThings,
                    visitedOwners);
            }
        }

        private static void AddHeldOwnerRecord(
            ThingOwner owner,
            string path,
            List<ShuttleHeldThingOwnerRecord> outOwners,
            HashSet<Thing> visitedThings,
            HashSet<ThingOwner> visitedOwners)
        {
            if (owner == null || visitedOwners.Contains(owner))
            {
                return;
            }

            visitedOwners.Add(owner);
            outOwners.Add(new ShuttleHeldThingOwnerRecord(owner, path));
            for (int i = 0; i < owner.Count; i++)
            {
                Thing child = owner[i];
                if (child == null || child.Destroyed || IsRecoverableHeldThing(child))
                {
                    continue;
                }

                CollectHeldThingOwners(
                    child,
                    path + " -> " + DescribeThing(child),
                    outOwners,
                    visitedThings,
                    visitedOwners);
            }
        }
    }
}

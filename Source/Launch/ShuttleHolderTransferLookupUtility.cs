using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleHolderTransferLookupUtility
    {
        internal static ShuttleLaunchCargoHandoff FindShuttleHandoff(List<ShuttleLaunchCargoHandoff> handoffs)
        {
            if (handoffs == null)
            {
                return null;
            }

            for (int i = 0; i < handoffs.Count; i++)
            {
                ShuttleLaunchCargoHandoff handoff = handoffs[i];
                if (handoff != null && handoff.SourceIsShuttle)
                {
                    return handoff;
                }
            }

            return null;
        }

        internal static ThingOwner GetActiveTransporterContainer(ShuttleLaunchCargoHandoff handoff)
        {
            return handoff != null &&
                handoff.ActiveTransporter != null &&
                handoff.ActiveTransporter.Contents != null
                    ? handoff.ActiveTransporter.Contents.innerContainer
                    : null;
        }

        internal static ThingOwner<Thing> GetActiveTransporterThingContainer(ShuttleLaunchCargoHandoff handoff)
        {
            return GetActiveTransporterContainer(handoff) as ThingOwner<Thing>;
        }

        internal static ThingOwner GetShuttleCargoContainer(ThingWithComps shuttleHost)
        {
            CompTransporter transporter = shuttleHost != null ? shuttleHost.TryGetComp<CompTransporter>() : null;
            return transporter != null ? transporter.GetDirectlyHeldThings() : null;
        }

        internal static Thing FindThingInHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, int thingID)
        {
            if (handoffs == null)
            {
                return null;
            }

            for (int i = 0; i < handoffs.Count; i++)
            {
                Thing thing = FindThingInOwner(GetActiveTransporterContainer(handoffs[i]), thingID);
                if (thing != null)
                {
                    return thing;
                }
            }

            return null;
        }

        internal static Thing FindThingInOwner(ThingOwner owner, int thingID)
        {
            if (owner == null || thingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (thing != null && thing.thingIDNumber == thingID)
                {
                    return thing;
                }
            }

            return null;
        }

        internal static int CountMedicalBayPatientManifestThingsInHandoffs(
            ShuttleHolderLaunchManifest manifest,
            List<ShuttleLaunchCargoHandoff> handoffs)
        {
            if (manifest == null)
            {
                return 0;
            }

            int count = 0;
            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null || entry.ThingID <= 0)
                {
                    continue;
                }

                if (FindThingInHandoffs(handoffs, entry.ThingID) != null)
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountMedicalBayPatientManifestThingsInOwners(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (manifest == null)
            {
                return 0;
            }

            int count = 0;
            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null || entry.ThingID <= 0)
                {
                    continue;
                }

                if (FindThingInOwner(primarySource, entry.ThingID) != null ||
                    (!object.ReferenceEquals(primarySource, secondarySource) &&
                    FindThingInOwner(secondarySource, entry.ThingID) != null))
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountMechChargerManifestThingsInHandoffs(
            ShuttleHolderLaunchManifest manifest,
            List<ShuttleLaunchCargoHandoff> handoffs)
        {
            if (manifest == null)
            {
                return 0;
            }

            int count = 0;
            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry != null && entry.ThingID > 0 && FindThingInHandoffs(handoffs, entry.ThingID) != null)
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountMechChargerManifestThingsInOwners(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (manifest == null)
            {
                return 0;
            }

            int count = 0;
            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null || entry.ThingID <= 0)
                {
                    continue;
                }

                if (FindThingInOwner(primarySource, entry.ThingID) != null ||
                    (!object.ReferenceEquals(primarySource, secondarySource) &&
                    FindThingInOwner(secondarySource, entry.ThingID) != null))
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountPrisonCellPrisonerManifestThingsInHandoffs(
            ShuttleHolderLaunchManifest manifest,
            List<ShuttleLaunchCargoHandoff> handoffs)
        {
            if (manifest == null)
            {
                return 0;
            }

            int count = 0;
            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry != null && entry.ThingID > 0 && FindThingInHandoffs(handoffs, entry.ThingID) != null)
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountPrisonCellPrisonerManifestThingsInOwners(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (manifest == null)
            {
                return 0;
            }

            int count = 0;
            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null || entry.ThingID <= 0)
                {
                    continue;
                }

                if (FindThingInOwner(primarySource, entry.ThingID) != null ||
                    (!object.ReferenceEquals(primarySource, secondarySource) &&
                    FindThingInOwner(secondarySource, entry.ThingID) != null))
                {
                    count++;
                }
            }

            return count;
        }

        internal static bool OwnerContainsThing(ThingOwner owner, Thing thing)
        {
            if (owner == null || thing == null)
            {
                return false;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                if (owner[i] == thing)
                {
                    return true;
                }
            }

            return false;
        }

        internal static Thing FindSpawnedThingByID(Map map, int thingID)
        {
            if (map == null || thingID <= 0)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns != null
                ? map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn != null && pawn.thingIDNumber == thingID)
                    {
                        return pawn;
                    }
                }
            }

            List<Thing> things = map.listerThings != null ? map.listerThings.AllThings : null;
            if (things == null)
            {
                return null;
            }

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && thing.thingIDNumber == thingID)
                {
                    return thing;
                }
            }

            return null;
        }
    }
}

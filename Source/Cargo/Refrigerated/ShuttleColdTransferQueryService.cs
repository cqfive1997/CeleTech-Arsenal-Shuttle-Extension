using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ShuttleColdTransferQueryService
    {
        private readonly ThingWithComps host;
        private readonly IShuttleCargoBackend cargoBackend;

        internal ShuttleColdTransferQueryService(
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend)
        {
            this.host = host;
            this.cargoBackend = cargoBackend;
        }

        internal bool TryResolveLoadedCargo(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            out CompTransporter transporter,
            out ThingOwner contents,
            out Thing thing,
            out string failureReason)
        {
            transporter = null;
            contents = null;
            thing = null;
            failureReason = null;

            List<CompTransporter> transporters = this.ResolveTransporters();
            if (transporterIndex < 0 || transporterIndex >= transporters.Count)
            {
                failureReason = "Loaded cargo transporter index is no longer valid.";
                return false;
            }

            transporter = transporters[transporterIndex];
            contents = GetContents(transporter);
            if (contents == null)
            {
                failureReason = "Loaded cargo container is not available.";
                return false;
            }

            thing = this.FindThing(contents, loadedIndex, thingIDNumber, expectedDefName);
            if (thing == null)
            {
                failureReason = "Loaded cargo entry is no longer available.";
                return false;
            }

            return true;
        }

        internal bool TryResolveLoadedCargoByIdentity(
            CargoStackRef stackRef,
            out int loadedIndex,
            out Thing thing,
            out string failureReason)
        {
            loadedIndex = -1;
            thing = null;
            failureReason = null;
            if (stackRef == null ||
                stackRef.SourceKind != ShuttleCargoInventorySourceKind.RegularCargo ||
                stackRef.SourceIndex < 0 ||
                stackRef.ThingIDNumber <= 0 ||
                string.IsNullOrEmpty(stackRef.DefName))
            {
                failureReason = "Deposited ordinary cargo identity is invalid.";
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters();
            if (stackRef.SourceIndex >= transporters.Count)
            {
                failureReason = "Deposited cargo transporter is no longer available.";
                return false;
            }

            CompTransporter transporter = transporters[stackRef.SourceIndex];
            ThingOwner contents = GetContents(transporter);
            if (contents == null)
            {
                failureReason = "Deposited cargo holder is no longer available.";
                return false;
            }

            for (int i = 0; i < contents.Count; i++)
            {
                Thing candidate = contents[i];
                if (!ShuttleColdTransferMatcher.MatchesThing(
                        candidate,
                        stackRef.ThingIDNumber,
                        stackRef.DefName))
                {
                    continue;
                }

                loadedIndex = i;
                thing = candidate;
                return true;
            }

            failureReason = "Deposited cargo stack is no longer in ordinary cargo.";
            return false;
        }

        internal bool TryResolveDeliveredLoadedCargo(
            CompTransporter sourceTransporter,
            Thing deliveredThing,
            int deliveredCount,
            out ThingOwner contents,
            out Thing thing,
            out string failureReason)
        {
            contents = null;
            thing = null;
            failureReason = null;

            contents = GetContents(sourceTransporter);
            if (contents == null)
            {
                failureReason = "Loaded cargo container is not available for refrigerated routing.";
                return false;
            }

            if (deliveredThing != null && contents.Contains(deliveredThing))
            {
                thing = deliveredThing;
                return true;
            }

            thing = this.FindMatchingLoadedThing(
                contents,
                deliveredThing,
                deliveredCount);
            if (thing == null)
            {
                failureReason = "Newly loaded cargo could not be resolved in normal cargo for refrigerated routing.";
                return false;
            }

            return true;
        }

        internal bool TryResolveColdThing(
            RefrigeratedCargoRecord record,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            out Thing thing,
            out string failureReason)
        {
            thing = null;
            failureReason = null;

            if (record == null || record.Contents == null)
            {
                failureReason = "Refrigerated cargo holder is not available.";
                return false;
            }

            thing = this.FindThing(record.Contents, coldIndex, thingIDNumber, expectedDefName);
            if (thing == null)
            {
                failureReason = "Refrigerated cargo entry is no longer available.";
                return false;
            }

            return true;
        }

        internal Thing FindThing(
            ThingOwner contents,
            int index,
            int thingIDNumber,
            string expectedDefName)
        {
            if (contents == null)
            {
                return null;
            }

            if (thingIDNumber <= 0 || string.IsNullOrEmpty(expectedDefName))
            {
                return null;
            }

            if (index >= 0 && index < contents.Count)
            {
                Thing indexedThing = contents[index];
                if (ShuttleColdTransferMatcher.MatchesThing(
                    indexedThing,
                    thingIDNumber,
                    expectedDefName))
                {
                    return indexedThing;
                }
            }

            // Indices are transient display coordinates. A preceding exact unload can shift
            // later entries, so fall back only to the same guarded Thing identity.
            for (int i = 0; i < contents.Count; i++)
            {
                Thing candidate = contents[i];
                if (ShuttleColdTransferMatcher.MatchesThing(
                    candidate,
                    thingIDNumber,
                    expectedDefName))
                {
                    return candidate;
                }
            }

            return null;
        }

        internal Thing FindMatchingLoadedThing(
            ThingOwner contents,
            Thing deliveredThing,
            int minimumStackCount)
        {
            if (contents == null || deliveredThing == null || deliveredThing.def == null)
            {
                return null;
            }

            for (int i = 0; i < contents.Count; i++)
            {
                Thing candidate = contents[i];
                if (candidate == null ||
                    candidate.stackCount < minimumStackCount ||
                    !HasVanillaStackCompatibility(candidate, deliveredThing))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static bool HasVanillaStackCompatibility(
            Thing candidate,
            Thing deliveredThing)
        {
            if (candidate == null || deliveredThing == null)
            {
                return false;
            }

            if (!deliveredThing.Destroyed)
            {
                return candidate.CanStackWith(deliveredThing);
            }

            // ThingOwner reports INotifyHauledTo after insertion. A fully merged carried
            // stack is destroyed by then, so ordinary CanStackWith rejects it before the
            // destination ThingWithComps can run its Quality/Ingredients/etc. policies.
            // Reproduce only the vanilla Thing/ThingWithComps stack checks while ignoring
            // that expected source-destroyed flag. Custom Thing subclasses that override
            // CanStackWith fail closed because their extra semantics cannot be reproduced
            // safely after the source has been consumed.
            MethodInfo canStackWithMethod = candidate.GetType().GetMethod(
                nameof(Thing.CanStackWith),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Thing) },
                null);
            if (canStackWithMethod == null ||
                (canStackWithMethod.DeclaringType != typeof(Thing) &&
                    canStackWithMethod.DeclaringType != typeof(ThingWithComps)))
            {
                return false;
            }

            if (candidate.Destroyed ||
                candidate.def == null ||
                deliveredThing.def == null ||
                candidate.def.category != ThingCategory.Item ||
                candidate.IsRelic() ||
                deliveredThing.IsRelic() ||
                candidate.def != deliveredThing.def ||
                candidate.Stuff != deliveredThing.Stuff)
            {
                return false;
            }

            ThingWithComps candidateWithComps = candidate as ThingWithComps;
            if (candidateWithComps == null || candidateWithComps.AllComps == null)
            {
                return true;
            }

            for (int i = 0; i < candidateWithComps.AllComps.Count; i++)
            {
                ThingComp comp = candidateWithComps.AllComps[i];
                if (comp != null && !comp.AllowStackWith(deliveredThing))
                {
                    return false;
                }
            }

            return true;
        }

        internal float GetNormalCargoMassKg()
        {
            float massKg = 0f;
            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    massKg += transporter.MassUsage;
                }
            }

            return massKg;
        }

        internal float GetTotalColdCargoMassKg(CompShuttleRefrigeratedCargoRegistry registry)
        {
            float massKg = 0f;
            if (registry == null || registry.Records == null)
            {
                return massKg;
            }

            IReadOnlyList<RefrigeratedCargoRecord> records = registry.Records;
            for (int i = 0; i < records.Count; i++)
            {
                RefrigeratedCargoRecord record = records[i];
                if (record != null)
                {
                    massKg += record.StoredMassKg;
                }
            }

            return massKg;
        }

        internal List<CompTransporter> ResolveTransporters()
        {
            if (this.cargoBackend == null)
            {
                return new List<CompTransporter>();
            }

            List<CompTransporter> transporters = this.cargoBackend.ResolveTransportersForLaunch(this.host);
            return transporters ?? new List<CompTransporter>();
        }

        internal static ThingOwner GetContents(CompTransporter transporter)
        {
            return transporter != null ? transporter.GetDirectlyHeldThings() : null;
        }
    }
}

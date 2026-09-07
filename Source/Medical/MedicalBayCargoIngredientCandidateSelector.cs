using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Selects ordinary loaded-cargo ingredient identities for Medical Bay surgery.
    /// It owns no holder and performs no mutation.
    /// </summary>
    internal sealed class MedicalBayCargoIngredientCandidateSelector
    {
        internal bool TryResolveBroker(
            ThingWithComps shuttleHost,
            out IShuttleCargoResourceBroker cargoBroker)
        {
            return ShuttleCargoTransactionResolver.TryResolveBroker(
                shuttleHost,
                out cargoBroker);
        }

        internal int Count(
            IShuttleCargoResourceBroker cargoBroker,
            ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return 0;
            }

            return this.CountMatching(
                cargoBroker,
                delegate(ThingDef candidateDef)
                {
                    return candidateDef == thingDef;
                });
        }

        internal int Count(
            IShuttleCargoResourceBroker cargoBroker,
            IReadOnlyList<ThingDef> allowedThingDefs)
        {
            if (allowedThingDefs == null || allowedThingDefs.Count == 0)
            {
                return 0;
            }

            return this.CountMatching(
                cargoBroker,
                delegate(ThingDef candidateDef)
                {
                    return AllowsThingDef(allowedThingDefs, candidateDef);
                });
        }

        internal MedicalBayCargoIngredientCandidate FindBest(
            IShuttleCargoResourceBroker cargoBroker,
            IReadOnlyList<ThingDef> allowedThingDefs)
        {
            if (cargoBroker == null || allowedThingDefs == null)
            {
                return null;
            }

            ShuttleCargoInventorySnapshot snapshot = cargoBroker.GetInventorySnapshot();
            IReadOnlyList<CargoStackRef> stackRefs = snapshot != null
                ? snapshot.StackRefs
                : null;
            MedicalBayCargoIngredientCandidate best = null;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                ThingDef thingDef;
                if (!this.TryResolveRegularThingDef(stackRef, out thingDef) ||
                    !AllowsThingDef(allowedThingDefs, thingDef))
                {
                    continue;
                }

                MedicalBayCargoIngredientCandidate candidate =
                    new MedicalBayCargoIngredientCandidate(stackRef, thingDef);
                if (best == null || this.Compare(candidate, best) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private int CountMatching(
            IShuttleCargoResourceBroker cargoBroker,
            Predicate<ThingDef> matcher)
        {
            if (cargoBroker == null || matcher == null)
            {
                return 0;
            }

            int count = 0;
            ShuttleCargoInventorySnapshot snapshot = cargoBroker.GetInventorySnapshot();
            IReadOnlyList<CargoStackRef> stackRefs = snapshot != null
                ? snapshot.StackRefs
                : null;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                ThingDef thingDef;
                if (this.TryResolveRegularThingDef(stackRef, out thingDef) &&
                    matcher(thingDef))
                {
                    count += stackRef.Count;
                }
            }

            return count;
        }

        private bool TryResolveRegularThingDef(
            CargoStackRef stackRef,
            out ThingDef thingDef)
        {
            thingDef = null;
            if (stackRef == null ||
                stackRef.SourceKind != ShuttleCargoInventorySourceKind.RegularCargo ||
                stackRef.Count <= 0 ||
                string.IsNullOrEmpty(stackRef.DefName))
            {
                return false;
            }

            thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(stackRef.DefName);
            return thingDef != null;
        }

        private int Compare(
            MedicalBayCargoIngredientCandidate left,
            MedicalBayCargoIngredientCandidate right)
        {
            if (left == null || right == null)
            {
                return left == right ? 0 : (left == null ? 1 : -1);
            }

            int potencyCompare = this.GetMedicinePotency(right.ThingDef)
                .CompareTo(this.GetMedicinePotency(left.ThingDef));
            if (potencyCompare != 0)
            {
                return potencyCompare;
            }

            string leftLabel = left.ThingDef != null
                ? left.ThingDef.LabelCap.ToString()
                : string.Empty;
            string rightLabel = right.ThingDef != null
                ? right.ThingDef.LabelCap.ToString()
                : string.Empty;
            int labelCompare = string.Compare(
                leftLabel,
                rightLabel,
                StringComparison.OrdinalIgnoreCase);
            return labelCompare != 0
                ? labelCompare
                : left.StackRef.ThingIDNumber.CompareTo(
                    right.StackRef.ThingIDNumber);
        }

        private float GetMedicinePotency(ThingDef thingDef)
        {
            return thingDef != null &&
                thingDef.IsMedicine &&
                StatDefOf.MedicalPotency != null
                    ? thingDef.GetStatValueAbstract(StatDefOf.MedicalPotency)
                    : 0f;
        }

        private static bool AllowsThingDef(
            IReadOnlyList<ThingDef> allowedThingDefs,
            ThingDef thingDef)
        {
            if (thingDef == null || allowedThingDefs == null)
            {
                return false;
            }

            for (int i = 0; i < allowedThingDefs.Count; i++)
            {
                if (allowedThingDefs[i] == thingDef)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class MedicalBayCargoIngredientCandidate
    {
        internal MedicalBayCargoIngredientCandidate(
            CargoStackRef stackRef,
            ThingDef thingDef)
        {
            this.StackRef = stackRef;
            this.ThingDef = thingDef;
        }

        internal CargoStackRef StackRef { get; private set; }
        internal ThingDef ThingDef { get; private set; }
    }
}

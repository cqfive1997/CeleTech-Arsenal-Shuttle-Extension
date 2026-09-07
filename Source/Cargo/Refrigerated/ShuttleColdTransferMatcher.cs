using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal static class ShuttleColdTransferMatcher
    {
        internal static bool MatchesThing(
            Thing thing,
            int thingIDNumber,
            string expectedDefName)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            if (thingIDNumber <= 0 || thing.thingIDNumber != thingIDNumber)
            {
                return false;
            }

            if (string.IsNullOrEmpty(expectedDefName) ||
                thing.def == null ||
                thing.def.defName != expectedDefName)
            {
                return false;
            }

            return true;
        }

        internal static bool IsBasicAutoTransferCandidate(
            Thing thing,
            ThingOwner sourceOwner,
            ThingWithComps host)
        {
            if (thing == null ||
                thing.Destroyed ||
                thing.def == null ||
                thing.stackCount <= 0 ||
                sourceOwner == null ||
                !sourceOwner.Contains(thing))
            {
                return false;
            }

            if (thing == host || thing is Pawn)
            {
                return false;
            }

            return thing.def.category == ThingCategory.Item;
        }

        internal static bool PassesAutoTransferSelection(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter)
        {
            if (thing == null || thing.Destroyed || thing.def == null || thing.stackCount <= 0)
            {
                return false;
            }

            if (thing is Pawn)
            {
                return false;
            }

            if (thing.def.category != ThingCategory.Item)
            {
                return false;
            }

            if (!AllowsCorpseByModuleOrCustomFilter(
                thing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter))
            {
                return false;
            }

            bool hasExplicitAutoTransferFilter =
                hasCustomAutoTransferFilter || effectiveAutoTransferFilter != null;
            if (moduleDef != null &&
                !hasExplicitAutoTransferFilter &&
                moduleDef.autoTransferRottableItems &&
                thing.TryGetComp<CompRottable>() == null)
            {
                return false;
            }

            return effectiveAutoTransferFilter == null || effectiveAutoTransferFilter.Allows(thing);
        }

        internal static bool AllowsCorpseByModuleOrCustomFilter(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter)
        {
            if (!(thing is Corpse))
            {
                return true;
            }

            if (moduleDef != null && moduleDef.allowCorpses)
            {
                return true;
            }

            return hasCustomAutoTransferFilter &&
                effectiveAutoTransferFilter != null &&
                effectiveAutoTransferFilter.Allows(thing);
        }
    }
}

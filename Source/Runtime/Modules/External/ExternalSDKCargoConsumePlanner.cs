using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoConsumePlanner
    {
        private const float Epsilon = 0.0001f;

        internal bool TryBuildPlan(
            ShuttleController controller,
            ExternalSDKCargoValidatedConsumeRequest request,
            out ExternalSDKCargoConsumePlan plan,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            plan = null;
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (controller == null || request == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "planner input is unavailable";
                return false;
            }

            ShuttleCargoSnapshot cargoSnapshot = controller.BuildCargoSnapshot();
            if (cargoSnapshot == null ||
                !cargoSnapshot.HasTransporter ||
                cargoSnapshot.Items == null)
            {
                plan = this.CreateEmptyPlan(request);
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoUnavailable;
                message = "normal cargo transporters are unavailable";
                return false;
            }

            List<ExternalSDKCargoConsumeCandidate> candidates =
                new List<ExternalSDKCargoConsumeCandidate>();
            int matchedCount = 0;
            int affectedCount = 0;
            int excludedPawnCount = 0;
            int excludedCorpseCount = 0;
            int excludedBlockedCount = 0;
            float affectedMassKg = 0f;

            for (int itemIndex = 0;
                itemIndex < cargoSnapshot.Items.Count;
                itemIndex++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[itemIndex];
                if (item == null || !item.IsLoaded)
                {
                    continue;
                }

                Thing thing = item.DisplayThing;
                if (!this.MatchesRequestFilters(thing, request))
                {
                    continue;
                }

                int availableCount = item.StackCount > 0
                    ? item.StackCount
                    : this.GetStackCount(thing);
                if (item.IsPawn || thing is Pawn)
                {
                    excludedPawnCount += availableCount;
                    continue;
                }

                if (thing is Corpse)
                {
                    excludedCorpseCount += availableCount;
                    continue;
                }

                if (item.CargoRegionIndex < 0)
                {
                    excludedBlockedCount += availableCount;
                    continue;
                }

                matchedCount += availableCount;
                if (affectedCount >= request.Count)
                {
                    continue;
                }

                int takeCount = this.ClampTakeCountByRequestAndMass(
                    thing,
                    availableCount,
                    request,
                    affectedCount,
                    affectedMassKg);
                if (takeCount <= 0)
                {
                    continue;
                }

                float takeMass = CargoDisplayUtility.GetThingMass(thing, takeCount);
                CargoStackRef stackRef = new CargoStackRef(
                    item.ThingIDNumber,
                    item.DefName,
                    availableCount,
                    ShuttleCargoInventorySourceKind.RegularCargo,
                    item.TransporterIndex,
                    null);
                candidates.Add(new ExternalSDKCargoConsumeCandidate(
                    stackRef,
                    takeCount,
                    takeMass));
                affectedCount += takeCount;
                affectedMassKg += takeMass;
            }

            plan = new ExternalSDKCargoConsumePlan(
                request,
                candidates,
                matchedCount,
                affectedCount,
                affectedMassKg);

            if (!this.IsPlanSufficient(
                    request,
                    plan,
                    excludedPawnCount,
                    excludedCorpseCount,
                    excludedBlockedCount,
                    out failureReason,
                    out message))
            {
                return false;
            }

            return true;
        }

        private ExternalSDKCargoConsumePlan CreateEmptyPlan(
            ExternalSDKCargoValidatedConsumeRequest request)
        {
            return new ExternalSDKCargoConsumePlan(
                request,
                new List<ExternalSDKCargoConsumeCandidate>(),
                0,
                0,
                0f);
        }

        private bool IsPlanSufficient(
            ExternalSDKCargoValidatedConsumeRequest request,
            ExternalSDKCargoConsumePlan plan,
            int excludedPawnCount,
            int excludedCorpseCount,
            int excludedBlockedCount,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (plan == null || plan.AffectedCount <= 0)
            {
                return this.FailForExcludedOrInsufficient(
                    excludedPawnCount,
                    excludedCorpseCount,
                    excludedBlockedCount,
                    out failureReason,
                    out message);
            }

            if (request.RequireExactCount && plan.AffectedCount < request.Count)
            {
                if (request.MaxMassKg > 0f && plan.MatchedCount >= request.Count)
                {
                    failureReason = ShuttleExternalCargoTransactionFailureReason.InsufficientItems;
                    message = "matching cargo exceeds maxMassKg";
                    return false;
                }

                return this.FailForExcludedOrInsufficient(
                    excludedPawnCount,
                    excludedCorpseCount,
                    excludedBlockedCount,
                    out failureReason,
                    out message);
            }

            return true;
        }

        private bool FailForExcludedOrInsufficient(
            int excludedPawnCount,
            int excludedCorpseCount,
            int excludedBlockedCount,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            if (excludedPawnCount > 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.PawnCargoExcluded;
                message = "matching pawn cargo is excluded";
                return false;
            }

            if (excludedCorpseCount > 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.CorpseCargoExcluded;
                message = "matching corpse cargo is excluded";
                return false;
            }

            if (excludedBlockedCount > 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.BlockedCargoExcluded;
                message = "matching cargo is blocked by cargo region policy";
                return false;
            }

            failureReason = ShuttleExternalCargoTransactionFailureReason.InsufficientItems;
            message = "insufficient matching loaded normal cargo";
            return false;
        }

        private bool MatchesRequestFilters(
            Thing thing,
            ExternalSDKCargoValidatedConsumeRequest request)
        {
            if (thing == null ||
                thing.Destroyed ||
                thing.def == null ||
                request == null ||
                thing.stackCount <= 0 ||
                thing.def != request.ThingDef)
            {
                return false;
            }

            return this.MatchesString(
                    this.GetCategoryDefName(thing),
                    request.CategoryDefName) &&
                this.MatchesString(this.GetStuffDefName(thing), request.StuffDefName) &&
                this.MatchesString(this.GetQuality(thing), request.Quality);
        }

        private int ClampTakeCountByRequestAndMass(
            Thing thing,
            int availableCount,
            ExternalSDKCargoValidatedConsumeRequest request,
            int affectedCount,
            float affectedMassKg)
        {
            int remainingCount = request.Count - affectedCount;
            if (remainingCount <= 0 || availableCount <= 0)
            {
                return 0;
            }

            int takeCount = availableCount < remainingCount
                ? availableCount
                : remainingCount;

            if (request.MaxMassKg <= 0f)
            {
                return takeCount;
            }

            float remainingMassKg = request.MaxMassKg - affectedMassKg;
            if (remainingMassKg <= Epsilon)
            {
                return 0;
            }

            float unitMass = this.GetUnitMassKg(thing);
            if (unitMass <= Epsilon)
            {
                return takeCount;
            }

            int massLimitedCount =
                (int)Math.Floor((remainingMassKg + Epsilon) / unitMass);
            if (massLimitedCount <= 0)
            {
                return 0;
            }

            return takeCount < massLimitedCount ? takeCount : massLimitedCount;
        }

        private float GetUnitMassKg(Thing thing)
        {
            int stackCount = this.GetStackCount(thing);
            if (thing == null || stackCount <= 0)
            {
                return 0f;
            }

            float mass = CargoDisplayUtility.GetThingMass(thing, stackCount);
            return mass > 0f ? mass / stackCount : 0f;
        }

        private int GetStackCount(Thing thing)
        {
            return thing != null && thing.stackCount > 0 ? thing.stackCount : 0;
        }

        private bool MatchesString(string actual, string expected)
        {
            return string.IsNullOrEmpty(expected) ||
                string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private string GetStuffDefName(Thing thing)
        {
            return thing != null && thing.Stuff != null
                ? thing.Stuff.defName
                : null;
        }

        private string GetCategoryDefName(Thing thing)
        {
            if (thing != null &&
                thing.def != null &&
                thing.def.thingCategories != null &&
                thing.def.thingCategories.Count > 0 &&
                thing.def.thingCategories[0] != null)
            {
                return thing.def.thingCategories[0].defName;
            }

            return thing != null && thing.def != null
                ? thing.def.category.ToString()
                : null;
        }

        private string GetQuality(Thing thing)
        {
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null)
            {
                return null;
            }

            CompQuality quality = thingWithComps.GetComp<CompQuality>();
            return quality != null ? quality.Quality.ToString() : null;
        }
    }
}

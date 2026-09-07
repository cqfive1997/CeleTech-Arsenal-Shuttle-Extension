using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class ShuttleCargoResourcePreflightService
    {
        private const float MassEpsilon = 0.0001f;

        internal bool CanConsume(
            ThingDef thingDef,
            int count,
            bool isAvailable,
            ShuttleProfile profile)
        {
            return thingDef != null &&
                count > 0 &&
                isAvailable &&
                profile != null &&
                profile.CargoLogistics != null &&
                profile.CargoLogistics.SupportsItemConsumption;
        }

        internal bool CanConsumeBatch(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            bool isAvailable,
            ShuttleProfile profile,
            out string failureReason)
        {
            failureReason = null;
            if (requirements == null || requirements.Count == 0)
            {
                failureReason = "No cargo ingredient requirements were provided.";
                return false;
            }

            if (!isAvailable)
            {
                failureReason = "Cargo broker is not available.";
                return false;
            }

            if (profile == null ||
                profile.CargoLogistics == null ||
                !profile.CargoLogistics.SupportsItemConsumption)
            {
                failureReason = "Cargo logistics does not support item consumption.";
                return false;
            }

            return true;
        }

        internal bool CanTransfer(
            ThingDef thingDef,
            int count,
            ThingOwner<Thing> destination,
            bool isAvailable,
            ShuttleProfile profile)
        {
            return thingDef != null &&
                count > 0 &&
                destination != null &&
                isAvailable &&
                profile != null &&
                profile.CargoLogistics != null &&
                profile.CargoLogistics.SupportsItemTransfer;
        }

        internal bool CanDeposit(
            ThingOwner<Thing> source,
            bool isAvailable,
            ShuttleProfile profile,
            out string failureReason)
        {
            failureReason = null;
            if (source == null)
            {
                failureReason = "No pending product owner was provided.";
                return false;
            }

            if (!isAvailable)
            {
                failureReason = "Cargo broker is not available.";
                return false;
            }

            if (profile == null ||
                profile.CargoLogistics == null ||
                !profile.CargoLogistics.SupportsItemDeposit)
            {
                failureReason = "Cargo logistics does not support item deposit.";
                return false;
            }

            return true;
        }

        internal bool CanQueryAvailableThingDefs(
            ThingFilter ingredientFilter,
            bool isAvailable,
            ShuttleProfile profile)
        {
            return ingredientFilter != null &&
                isAvailable &&
                profile != null &&
                profile.CargoLogistics != null &&
                profile.CargoLogistics.SupportsItemConsumption;
        }

        internal bool ValidateDepositSource(
            ThingOwner<Thing> source,
            out string failureReason)
        {
            failureReason = null;
            if (source == null)
            {
                failureReason = "No pending product owner was provided.";
                return false;
            }

            for (int i = 0; i < source.Count; i++)
            {
                Thing thing = source[i];
                if (!ShuttleCargoResourceMatcher.MatchesAvailableThing(thing))
                {
                    failureReason = "Pending products contain an invalid thing.";
                    return false;
                }
            }

            return true;
        }

        internal bool HasMassCapacityForDeposit(
            ThingOwner<Thing> source,
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend,
            out string failureReason)
        {
            failureReason = null;
            float requiredMassKg = this.GetOwnerMassKg(source);
            if (requiredMassKg <= MassEpsilon)
            {
                return true;
            }

            float availableMassKg = cargoBackend != null
                ? cargoBackend.GetAvailableMass(host)
                : 0f;
            if (availableMassKg + MassEpsilon >= requiredMassKg)
            {
                return true;
            }

            failureReason = "Cargo mass capacity is insufficient for pending products (" +
                requiredMassKg.ToString("0.#") + "/" + availableMassKg.ToString("0.#") + " kg available).";
            return false;
        }

        private float GetOwnerMassKg(ThingOwner<Thing> source)
        {
            if (source == null)
            {
                return 0f;
            }

            float massKg = 0f;
            for (int i = 0; i < source.Count; i++)
            {
                Thing thing = source[i];
                if (thing != null)
                {
                    massKg += CargoDisplayUtility.GetThingMass(thing, thing.stackCount);
                }
            }

            return massKg;
        }
    }
}

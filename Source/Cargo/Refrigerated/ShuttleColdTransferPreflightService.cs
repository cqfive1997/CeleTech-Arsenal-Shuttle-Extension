using System;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ShuttleColdTransferPreflightService
    {
        private const float MassEpsilon = 0.0001f;

        internal bool TryValidateTransferIntoCold(
            ShuttleModule module,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend,
            ShuttleProfile profile,
            out string failureReason)
        {
            if (!this.TryValidateCommonCargoTransferAccess(host, cargoBackend, profile, out failureReason))
            {
                return false;
            }

            if (module == null || moduleDef == null)
            {
                failureReason = "Refrigerated cargo module is no longer installed.";
                return false;
            }

            if (record == null)
            {
                failureReason = "Refrigerated cargo holder is not available.";
                return false;
            }

            return true;
        }

        internal bool TryValidateLoadRoutingIntoCold(
            ShuttleModule module,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend,
            out string failureReason)
        {
            failureReason = null;
            if (host == null)
            {
                failureReason = "Shuttle host is not available.";
                return false;
            }

            if (cargoBackend == null)
            {
                failureReason = "Cargo backend is not available.";
                return false;
            }

            if (module == null || moduleDef == null)
            {
                failureReason = "Refrigerated cargo module is no longer installed.";
                return false;
            }

            if (record == null)
            {
                failureReason = "Refrigerated cargo holder is not available.";
                return false;
            }

            return true;
        }

        internal bool TryValidateTransferOutOfCold(
            ShuttleModule module,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend,
            ShuttleProfile profile,
            out string failureReason)
        {
            if (!this.TryValidateCommonCargoTransferAccess(host, cargoBackend, profile, out failureReason))
            {
                return false;
            }

            if (module == null || moduleDef == null)
            {
                failureReason = "Refrigerated cargo module is no longer installed.";
                return false;
            }

            return true;
        }

        internal bool TryValidateCommonCargoTransferAccess(
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend,
            ShuttleProfile profile,
            out string failureReason)
        {
            failureReason = null;
            if (host == null)
            {
                failureReason = "Shuttle host is not available.";
                return false;
            }

            if (cargoBackend == null)
            {
                failureReason = "Cargo backend is not available.";
                return false;
            }

            if (profile == null ||
                profile.Cargo == null ||
                profile.CargoLogistics == null ||
                !profile.CargoLogistics.HasCargoLogistics ||
                !profile.CargoLogistics.SupportsItemTransfer)
            {
                failureReason = "Cargo logistics does not support item transfer.";
                return false;
            }

            return true;
        }

        internal bool TryValidateMoveIntoCold(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            int requestedCount,
            out int moveCount,
            out string failureReason)
        {
            if (!this.TryValidateMoveCount(thing, moduleDef, requestedCount, out moveCount, out failureReason))
            {
                return false;
            }

            return this.TryValidateColdCargoThing(
                thing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter,
                out failureReason);
        }

        internal bool TryValidateLoadRoutedMoveIntoCold(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            int deliveredCount,
            out int moveCount,
            out string failureReason)
        {
            if (!this.TryValidateMoveCountWithinStack(
                thing,
                moduleDef,
                deliveredCount,
                out moveCount,
                out failureReason))
            {
                return false;
            }

            // Vanilla may merge a haul delivery into an existing transporter stack before
            // INotifyHauledTo reports the delivered delta. Extracting that exact delta is
            // delivery routing, not a player-requested partial-stack transfer, so the module's
            // manual allowPartialStackTransfer policy must not reject it.
            return this.TryValidateColdCargoThing(
                thing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter,
                out failureReason);
        }

        private bool TryValidateColdCargoThing(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            out string failureReason)
        {
            failureReason = null;
            if (thing is Pawn)
            {
                failureReason = "Pawns cannot be moved into refrigerated cargo.";
                return false;
            }

            if (thing.def.category != ThingCategory.Item)
            {
                failureReason = "Only item cargo can be moved into refrigerated cargo.";
                return false;
            }

            if (!ShuttleColdTransferMatcher.AllowsCorpseByModuleOrCustomFilter(
                thing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter))
            {
                failureReason = "This refrigerated cargo module does not allow corpses.";
                return false;
            }

            // The full auto-transfer filter remains scheduler/load-routing policy.
            // For manual transfer it only acts as the player's explicit corpse opt-in.
            return true;
        }

        internal bool TryValidateMoveOutOfCold(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            int requestedCount,
            out int moveCount,
            out string failureReason)
        {
            return this.TryValidateMoveCount(thing, moduleDef, requestedCount, out moveCount, out failureReason);
        }

        internal bool TryValidateMoveCount(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            int requestedCount,
            out int moveCount,
            out string failureReason)
        {
            if (!this.TryValidateMoveCountWithinStack(
                thing,
                moduleDef,
                requestedCount,
                out moveCount,
                out failureReason))
            {
                return false;
            }

            if (!moduleDef.allowPartialStackTransfer && requestedCount != thing.stackCount)
            {
                failureReason = "This refrigerated cargo module does not allow partial stack transfers.";
                return false;
            }

            moveCount = requestedCount;
            return true;
        }

        private bool TryValidateMoveCountWithinStack(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            int requestedCount,
            out int moveCount,
            out string failureReason)
        {
            moveCount = 0;
            failureReason = null;

            if (thing == null || thing.Destroyed || thing.def == null || thing.stackCount <= 0)
            {
                failureReason = "Selected cargo item is invalid.";
                return false;
            }

            if (moduleDef == null)
            {
                failureReason = "Refrigerated cargo module definition is not available.";
                return false;
            }

            if (requestedCount <= 0)
            {
                failureReason = "Transfer count must be positive.";
                return false;
            }

            if (requestedCount > thing.stackCount)
            {
                failureReason = "Transfer count exceeds the selected cargo stack.";
                return false;
            }

            moveCount = requestedCount;
            return true;
        }

        internal bool HasColdCapacity(
            RefrigeratedCargoRecord record,
            ShuttleProfile profile,
            CompShuttleRefrigeratedCargoRegistry registry,
            float moveMassKg,
            out string failureReason)
        {
            failureReason = null;
            if (record == null)
            {
                failureReason = "Refrigerated cargo holder is not available.";
                return false;
            }

            if (ShuttleRefrigeratedCargoCapacityPolicy.SharesOverallCapacity)
            {
                return true;
            }

            float capacityKg;
            float projectedUsedMassKg;
            if (ShuttleRefrigeratedCargoCapacityPolicy.CanAddColdMass(
                profile,
                registry,
                0f,
                moveMassKg,
                out capacityKg,
                out projectedUsedMassKg))
            {
                return true;
            }

            failureReason = "Refrigerated cargo capacity is insufficient (" +
                projectedUsedMassKg.ToString("0.#") + "/" +
                capacityKg.ToString("0.#") + " kg).";
            return false;
        }

        internal int GetMaxPartialCountForColdCapacity(
            Thing thing,
            RefrigeratedCargoRecord record,
            ShuttleProfile profile,
            CompShuttleRefrigeratedCargoRegistry registry)
        {
            if (thing == null || thing.stackCount <= 0 || record == null)
            {
                return 0;
            }

            if (ShuttleRefrigeratedCargoCapacityPolicy.SharesOverallCapacity)
            {
                return thing.stackCount;
            }

            float availableMassKg =
                ShuttleRefrigeratedCargoCapacityPolicy.GetAvailableColdMassKg(
                    profile,
                    registry,
                    0f);
            if (availableMassKg <= MassEpsilon)
            {
                return 0;
            }

            float fullMassKg = CargoDisplayUtility.GetThingMass(thing, thing.stackCount);
            if (fullMassKg <= MassEpsilon)
            {
                return thing.stackCount;
            }

            float perItemMassKg = fullMassKg / thing.stackCount;
            if (perItemMassKg <= MassEpsilon)
            {
                return thing.stackCount;
            }

            int maxCount = (int)Math.Floor((availableMassKg + MassEpsilon) / perItemMassKg);
            if (maxCount < 0)
            {
                return 0;
            }

            return maxCount > thing.stackCount ? thing.stackCount : maxCount;
        }
    }
}

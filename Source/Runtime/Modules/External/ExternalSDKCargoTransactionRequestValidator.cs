using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionRequestValidator
    {
        private const int MaxStringLength = 128;
        private const int MaxCount = 10000;
        private const float MaxMassKg = 1000000f;

        internal bool TryValidateConsumeRequest(
            ShuttleExternalCargoConsumeRequest request,
            out ExternalSDKCargoValidatedConsumeRequest validated,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            validated = null;
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (request == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "consume request is null";
                return false;
            }

            ShuttleExternalCargoTransactionSource source;
            if (!this.TryValidateSource(request.Source, out source, out failureReason, out message))
            {
                return false;
            }

            string itemDefName;
            if (!this.TryNormalizeRequiredString(
                    request.ItemDefName,
                    "itemDefName",
                    out itemDefName,
                    out message))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidDef;
                return false;
            }

            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(itemDefName);
            if (thingDef == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidDef;
                message = "itemDefName is not a known ThingDef: " + itemDefName;
                return false;
            }

            string categoryDefName;
            string stuffDefName;
            string quality;
            if (!this.TryNormalizeOptionalString(
                    request.CategoryDefName,
                    "categoryDefName",
                    out categoryDefName,
                    out message) ||
                !this.TryNormalizeOptionalString(
                    request.StuffDefName,
                    "stuffDefName",
                    out stuffDefName,
                    out message) ||
                !this.TryNormalizeOptionalString(
                    request.Quality,
                    "quality",
                    out quality,
                    out message))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                return false;
            }

            if (!string.IsNullOrEmpty(stuffDefName) &&
                DefDatabase<ThingDef>.GetNamedSilentFail(stuffDefName) == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidDef;
                message = "stuffDefName is not a known ThingDef: " + stuffDefName;
                return false;
            }

            if (request.Count <= 0 || request.Count > MaxCount)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidQuantity;
                message = "count must be between 1 and " + MaxCount;
                return false;
            }

            if (float.IsNaN(request.MaxMassKg) ||
                float.IsInfinity(request.MaxMassKg) ||
                request.MaxMassKg < 0f ||
                request.MaxMassKg > MaxMassKg)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidQuantity;
                message = "maxMassKg must be zero or a finite value no greater than " + MaxMassKg;
                return false;
            }

            if (!request.RequireExactCount && !request.AllowPartial)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "request must require exact count or explicitly allow partial consume";
                return false;
            }

            validated = new ExternalSDKCargoValidatedConsumeRequest(
                source,
                thingDef,
                itemDefName,
                categoryDefName,
                stuffDefName,
                quality,
                request.Count,
                request.MaxMassKg,
                request.AllowPartial,
                request.RequireExactCount);
            return true;
        }

        private bool TryValidateSource(
            ShuttleExternalCargoTransactionSource source,
            out ShuttleExternalCargoTransactionSource normalized,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            normalized = null;
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (source == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidSource;
                message = "transaction source is required";
                return false;
            }

            string ownerPackageId;
            string localRequesterKey;
            string moduleInstanceId;
            string reasonKey;
            if (!this.TryNormalizeRequiredString(
                    source.OwnerPackageId,
                    "ownerPackageId",
                    out ownerPackageId,
                    out message) ||
                !this.TryNormalizeRequiredString(
                    source.LocalRequesterKey,
                    "localRequesterKey",
                    out localRequesterKey,
                    out message) ||
                !this.TryNormalizeOptionalString(
                    source.ModuleInstanceId,
                    "moduleInstanceId",
                    out moduleInstanceId,
                    out message) ||
                !this.TryNormalizeOptionalString(
                    source.ReasonKey,
                    "reasonKey",
                    out reasonKey,
                    out message))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidSource;
                return false;
            }

            normalized = new ShuttleExternalCargoTransactionSource(
                ownerPackageId,
                localRequesterKey,
                moduleInstanceId,
                reasonKey);
            return true;
        }

        private bool TryNormalizeRequiredString(
            string value,
            string fieldName,
            out string normalized,
            out string message)
        {
            normalized = null;
            message = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                message = fieldName + " is required";
                return false;
            }

            return this.TryNormalizeOptionalString(value, fieldName, out normalized, out message) &&
                !string.IsNullOrEmpty(normalized);
        }

        private bool TryNormalizeOptionalString(
            string value,
            string fieldName,
            out string normalized,
            out string message)
        {
            normalized = null;
            message = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            normalized = value.Trim();
            if (normalized.Length > MaxStringLength)
            {
                message = fieldName + " exceeds " + MaxStringLength + " characters";
                return false;
            }

            return true;
        }
    }

    internal sealed class ExternalSDKCargoValidatedConsumeRequest
    {
        internal ExternalSDKCargoValidatedConsumeRequest(
            ShuttleExternalCargoTransactionSource source,
            ThingDef thingDef,
            string itemDefName,
            string categoryDefName,
            string stuffDefName,
            string quality,
            int count,
            float maxMassKg,
            bool allowPartial,
            bool requireExactCount)
        {
            this.Source = source;
            this.ThingDef = thingDef;
            this.ItemDefName = itemDefName;
            this.CategoryDefName = categoryDefName;
            this.StuffDefName = stuffDefName;
            this.Quality = quality;
            this.Count = count;
            this.MaxMassKg = maxMassKg;
            this.AllowPartial = allowPartial;
            this.RequireExactCount = requireExactCount;
        }

        internal ShuttleExternalCargoTransactionSource Source { get; private set; }
        internal ThingDef ThingDef { get; private set; }
        internal string ItemDefName { get; private set; }
        internal string CategoryDefName { get; private set; }
        internal string StuffDefName { get; private set; }
        internal string Quality { get; private set; }
        internal int Count { get; private set; }
        internal float MaxMassKg { get; private set; }
        internal bool AllowPartial { get; private set; }
        internal bool RequireExactCount { get; private set; }
    }
}

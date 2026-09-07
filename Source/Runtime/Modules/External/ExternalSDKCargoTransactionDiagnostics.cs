using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionDiagnostics
    {
        private const int MaxMessageLength = 512;
        private const int MaxSourceFieldLength = 128;

        internal ShuttleExternalCargoTransactionResult BuildFailureFromRequest(
            ShuttleExternalCargoConsumeRequest request,
            ShuttleExternalCargoTransactionKind kind,
            bool dryRun,
            bool available,
            ShuttleExternalCargoTransactionFailureReason failureReason,
            string message)
        {
            return new ShuttleExternalCargoTransactionResult(
                available,
                false,
                dryRun,
                kind,
                failureReason,
                this.Clamp(message),
                request != null ? request.Count : 0,
                0,
                0,
                0,
                0f,
                this.ClampSourceField(request != null ? request.ItemDefName : null),
                this.SanitizeSource(request != null ? request.Source : null),
                ShuttleTickUtility.TicksGameOrMinusOne(),
                this.Clamp(message));
        }

        internal ShuttleExternalCargoTransactionResult BuildFailure(
            ExternalSDKCargoValidatedConsumeRequest request,
            ExternalSDKCargoConsumePlan plan,
            ShuttleExternalCargoTransactionKind kind,
            bool dryRun,
            bool available,
            ShuttleExternalCargoTransactionFailureReason failureReason,
            string message)
        {
            return new ShuttleExternalCargoTransactionResult(
                available,
                false,
                dryRun,
                kind,
                failureReason,
                this.Clamp(message),
                request != null ? request.Count : 0,
                plan != null ? plan.MatchedCount : 0,
                plan != null ? plan.AffectedCount : 0,
                plan != null ? plan.AffectedStackCount : 0,
                plan != null ? plan.AffectedMassKg : 0f,
                request != null ? request.ItemDefName : null,
                request != null ? request.Source : null,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                this.Clamp(message));
        }

        internal ShuttleExternalCargoTransactionResult BuildSuccess(
            ExternalSDKCargoValidatedConsumeRequest request,
            ExternalSDKCargoConsumePlan plan,
            ShuttleExternalCargoTransactionKind kind,
            bool dryRun,
            int affectedCount,
            int affectedStackCount,
            float affectedMassKg,
            string message)
        {
            return new ShuttleExternalCargoTransactionResult(
                true,
                true,
                dryRun,
                kind,
                ShuttleExternalCargoTransactionFailureReason.None,
                this.Clamp(message),
                request != null ? request.Count : 0,
                plan != null ? plan.MatchedCount : 0,
                affectedCount,
                affectedStackCount,
                affectedMassKg,
                request != null ? request.ItemDefName : null,
                request != null ? request.Source : null,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                this.Clamp(message));
        }

        internal void LogInternalFailure(
            ExternalSDKCargoValidatedConsumeRequest request,
            ShuttleExternalCargoTransactionKind kind,
            string message)
        {
            Log.Error("[CeleTech Shuttle] External SDK cargo transaction internal failure. kind=" +
                kind +
                " owner=" +
                this.SourceOwner(request) +
                " requester=" +
                this.SourceRequester(request) +
                " itemDef=" +
                (request != null ? request.ItemDefName : "null") +
                " count=" +
                (request != null ? request.Count : 0) +
                " message=" +
                this.Clamp(message));
        }

        private string SourceOwner(ExternalSDKCargoValidatedConsumeRequest request)
        {
            return request != null &&
                request.Source != null &&
                !string.IsNullOrEmpty(request.Source.OwnerPackageId)
                ? request.Source.OwnerPackageId
                : "unknown";
        }

        private string SourceRequester(ExternalSDKCargoValidatedConsumeRequest request)
        {
            return request != null &&
                request.Source != null &&
                !string.IsNullOrEmpty(request.Source.LocalRequesterKey)
                ? request.Source.LocalRequesterKey
                : "unknown";
        }

        private ShuttleExternalCargoTransactionSource SanitizeSource(
            ShuttleExternalCargoTransactionSource source)
        {
            if (source == null)
            {
                return null;
            }

            return new ShuttleExternalCargoTransactionSource(
                this.ClampSourceField(source.OwnerPackageId),
                this.ClampSourceField(source.LocalRequesterKey),
                this.ClampSourceField(source.ModuleInstanceId),
                this.ClampSourceField(source.ReasonKey));
        }

        private string ClampSourceField(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Trim();
            return normalized.Length <= MaxSourceFieldLength
                ? normalized
                : normalized.Substring(0, MaxSourceFieldLength);
        }

        private string Clamp(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= MaxMessageLength
                ? value
                : value.Substring(0, MaxMessageLength);
        }
    }
}

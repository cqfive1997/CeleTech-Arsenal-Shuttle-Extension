using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Map-side context for external runtime systems.
    /// It exposes writable external state and narrow runtime services, not controller,
    /// assembly state, runtime state, or live module objects.
    /// </summary>
    public sealed class ShuttleExternalRuntimeContext
    {
        public ShuttleExternalRuntimeContext(
            string ownerPackageId,
            string runtimeSystemKey,
            ShuttleExternalModuleInfo module,
            int ticksGame,
            IShuttleExternalRuntimeStateStore state,
            bool internalBusPowered,
            Func<float, bool> storedEnergyConsumer,
            IShuttleExternalOccupantReadPort occupantReadPort,
            IShuttleExternalHostReadPort hostReadPort)
            : this(
                ownerPackageId,
                runtimeSystemKey,
                module,
                ticksGame,
                state,
                internalBusPowered,
                storedEnergyConsumer,
                occupantReadPort,
                hostReadPort,
                null)
        {
        }

        public ShuttleExternalRuntimeContext(
            string ownerPackageId,
            string runtimeSystemKey,
            ShuttleExternalModuleInfo module,
            int ticksGame,
            IShuttleExternalRuntimeStateStore state,
            bool internalBusPowered,
            Func<float, bool> storedEnergyConsumer,
            IShuttleExternalOccupantReadPort occupantReadPort,
            IShuttleExternalHostReadPort hostReadPort,
            IShuttleExternalLaunchOccupantHandoffProvider launchOccupantHandoffProvider)
        {
            this.OwnerPackageId = ownerPackageId;
            this.RuntimeSystemKey = runtimeSystemKey;
            this.Module = module;
            this.TicksGame = ticksGame;
            this.State = state ?? NullShuttleExternalRuntimeStateStore.Instance;
            this.InternalBusPowered = internalBusPowered;
            this.storedEnergyConsumer = storedEnergyConsumer;
            this.occupantReadPort = occupantReadPort;
            this.hostReadPort = hostReadPort;
            this.launchOccupantHandoffProvider = launchOccupantHandoffProvider;
        }

        private static readonly IReadOnlyList<ShuttleExternalOccupantInfo> EmptyOccupants =
            Array.Empty<ShuttleExternalOccupantInfo>();

        private readonly Func<float, bool> storedEnergyConsumer;
        private readonly IShuttleExternalOccupantReadPort occupantReadPort;
        private readonly IShuttleExternalHostReadPort hostReadPort;
        private readonly IShuttleExternalLaunchOccupantHandoffProvider launchOccupantHandoffProvider;

        public string OwnerPackageId { get; private set; }

        public string RuntimeSystemKey { get; private set; }

        public ShuttleExternalModuleInfo Module { get; private set; }

        public int TicksGame { get; private set; }

        public IShuttleExternalRuntimeStateStore State { get; private set; }

        public bool InternalBusPowered { get; private set; }

        public bool SupportsOccupantRead
        {
            get
            {
                return this.occupantReadPort != null;
            }
        }

        public bool SupportsHostRead
        {
            get
            {
                return this.hostReadPort != null;
            }
        }

        public bool SupportsLaunchOccupantHandoff
        {
            get
            {
                return this.launchOccupantHandoffProvider != null;
            }
        }

        public IReadOnlyList<ShuttleExternalOccupantInfo> GetOccupants(
            ShuttleExternalOccupantQuery query)
        {
            if (this.occupantReadPort == null)
            {
                return EmptyOccupants;
            }

            try
            {
                IReadOnlyList<ShuttleExternalOccupantInfo> occupants =
                    this.occupantReadPort.GetOccupants(query);
                return occupants ?? EmptyOccupants;
            }
            catch (Exception exception)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] External runtime occupant read failed for runtime key '" +
                    (this.RuntimeSystemKey ?? "null") + "'. Returning an empty occupant list. Exception: " +
                    exception,
                    this.MakeWarningHash("external-occupant-read-failed"));
                return EmptyOccupants;
            }
        }

        public bool TryGetHostInfo(out ShuttleExternalHostInfo hostInfo)
        {
            hostInfo = null;
            if (this.hostReadPort == null)
            {
                return false;
            }

            try
            {
                return this.hostReadPort.TryGetHostInfo(out hostInfo) && hostInfo != null;
            }
            catch (Exception exception)
            {
                hostInfo = null;
                Log.WarningOnce(
                    "[CeleTech Shuttle] External runtime host read failed for runtime key '" +
                    (this.RuntimeSystemKey ?? "null") + "'. Returning false. Exception: " +
                    exception,
                    this.MakeWarningHash("external-host-read-failed"));
                return false;
            }
        }

        public bool TryQuoteLaunchOccupantHandoff(
            Pawn pawn,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            return this.TryQuoteLaunchOccupantHandoff(pawn, null, out result);
        }

        public bool TryQuoteLaunchOccupantHandoff(
            Pawn pawn,
            string reasonKey,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            return this.TryRunLaunchOccupantHandoff(pawn, reasonKey, true, out result);
        }

        public bool TryHandoffLaunchOccupant(
            Pawn pawn,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            return this.TryHandoffLaunchOccupant(pawn, null, out result);
        }

        public bool TryHandoffLaunchOccupant(
            Pawn pawn,
            string reasonKey,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            return this.TryRunLaunchOccupantHandoff(pawn, reasonKey, false, out result);
        }

        public bool TryConsumeStoredEnergyWd(float amountWd)
        {
            if (float.IsNaN(amountWd) || float.IsInfinity(amountWd))
            {
                return false;
            }

            if (amountWd <= 0f)
            {
                return true;
            }

            if (this.storedEnergyConsumer == null)
            {
                return false;
            }

            try
            {
                return this.storedEnergyConsumer(amountWd);
            }
            catch
            {
                return false;
            }
        }

        private bool TryRunLaunchOccupantHandoff(
            Pawn pawn,
            string reasonKey,
            bool dryRun,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            result = null;
            ShuttleExternalLaunchOccupantHandoffRequest request =
                new ShuttleExternalLaunchOccupantHandoffRequest(
                    this.BuildLaunchOccupantHandoffSource(reasonKey),
                    pawn);

            if (this.launchOccupantHandoffProvider == null)
            {
                result = this.BuildLaunchOccupantHandoffFailure(
                    request,
                    dryRun,
                    ShuttleExternalLaunchOccupantHandoffFailureReason.Unavailable,
                    "launch occupant handoff is unavailable");
                return false;
            }

            try
            {
                return dryRun
                    ? this.launchOccupantHandoffProvider.TryQuoteHandoff(request, out result)
                    : this.launchOccupantHandoffProvider.TryHandoff(request, out result);
            }
            catch (Exception exception)
            {
                result = this.BuildLaunchOccupantHandoffFailure(
                    request,
                    dryRun,
                    ShuttleExternalLaunchOccupantHandoffFailureReason.InternalError,
                    "launch occupant handoff threw: " + exception.GetType().Name);
                Log.WarningOnce(
                    "[CeleTech Shuttle] External runtime launch occupant handoff failed for runtime key '" +
                    (this.RuntimeSystemKey ?? "null") + "'. Exception: " +
                    exception,
                    this.MakeWarningHash("external-launch-occupant-handoff-failed"));
                return false;
            }
        }

        private ShuttleExternalLaunchOccupantHandoffSource BuildLaunchOccupantHandoffSource(
            string reasonKey)
        {
            return new ShuttleExternalLaunchOccupantHandoffSource(
                string.IsNullOrWhiteSpace(this.OwnerPackageId)
                    ? "unknown"
                    : this.OwnerPackageId,
                string.IsNullOrWhiteSpace(this.RuntimeSystemKey)
                    ? "runtime"
                    : this.RuntimeSystemKey,
                this.Module != null ? this.Module.ModuleInstanceId : null,
                string.IsNullOrWhiteSpace(reasonKey) ? null : reasonKey.Trim());
        }

        private ShuttleExternalLaunchOccupantHandoffResult BuildLaunchOccupantHandoffFailure(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            bool dryRun,
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            string message)
        {
            Pawn pawn = request != null ? request.Pawn : null;
            return new ShuttleExternalLaunchOccupantHandoffResult(
                false,
                false,
                dryRun,
                dryRun
                    ? ShuttleExternalLaunchOccupantHandoffKind.Quote
                    : ShuttleExternalLaunchOccupantHandoffKind.Handoff,
                failureReason,
                message,
                pawn != null ? pawn.thingIDNumber : -1,
                pawn != null ? pawn.ThingID : null,
                pawn != null ? pawn.LabelShort : null,
                0f,
                request != null ? request.Source : null,
                this.TicksGame,
                message);
        }

        private int MakeWarningHash(string key)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (key != null ? key.GetHashCode() : 0);
                hash = (hash * 31) + (this.RuntimeSystemKey != null ? this.RuntimeSystemKey.GetHashCode() : 0);
                hash = (hash * 31) + (this.Module != null && this.Module.ModuleInstanceId != null
                    ? this.Module.ModuleInstanceId.GetHashCode()
                    : 0);
                return hash;
            }
        }
    }
}

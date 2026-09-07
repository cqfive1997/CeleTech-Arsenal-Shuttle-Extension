using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponBackendSupport
    {
        NotApplicable,
        Supported,
        Rejected
    }

    /// <summary>
    /// Read-only input used while selecting a runtime backend. Probes must not create guns,
    /// bind verbs, or mutate module runtime state.
    /// </summary>
    internal sealed class ShuttleWeaponBackendProbeContext
    {
        internal ShuttleWeaponBackendProbeContext(
            ShuttleWeaponModuleDef weaponDef,
            Thing existingGun)
            : this(weaponDef, existingGun, null, false)
        {
        }

        internal ShuttleWeaponBackendProbeContext(
            ShuttleWeaponModuleDef weaponDef,
            Thing existingGun,
            string magazineAuthorityBackendId)
            : this(
                weaponDef,
                existingGun,
                magazineAuthorityBackendId,
                false)
        {
        }

        internal ShuttleWeaponBackendProbeContext(
            ShuttleWeaponModuleDef weaponDef,
            Thing existingGun,
            string magazineAuthorityBackendId,
            bool authorityTransferReady)
        {
            this.WeaponDef = weaponDef;
            this.ExistingGun = existingGun;
            this.MagazineAuthorityBackendId = magazineAuthorityBackendId;
            this.AuthorityTransferReady = authorityTransferReady;
        }

        internal ShuttleWeaponModuleDef WeaponDef { get; private set; }

        internal Thing ExistingGun { get; private set; }

        internal string MagazineAuthorityBackendId { get; private set; }

        internal bool AuthorityTransferReady { get; private set; }
    }

    /// <summary>
    /// Structured backend selection result. ReasonCode is diagnostic data, not translated UI
    /// text, and probe evaluation has no side effects.
    /// </summary>
    internal sealed class ShuttleWeaponCompatibilityReport
    {
        private ShuttleWeaponCompatibilityReport(
            ShuttleWeaponBackendSupport support,
            string backendId,
            string reasonCode)
        {
            this.Support = support;
            this.BackendId = backendId;
            this.ReasonCode = reasonCode;
        }

        internal ShuttleWeaponBackendSupport Support { get; private set; }

        internal string BackendId { get; private set; }

        internal string ReasonCode { get; private set; }

        internal bool IsSupported
        {
            get { return this.Support == ShuttleWeaponBackendSupport.Supported; }
        }

        internal bool BlocksLowerPriorityFallback
        {
            get { return this.Support == ShuttleWeaponBackendSupport.Rejected; }
        }

        internal static ShuttleWeaponCompatibilityReport Supported(
            string backendId,
            string reasonCode)
        {
            return new ShuttleWeaponCompatibilityReport(
                ShuttleWeaponBackendSupport.Supported,
                backendId,
                reasonCode);
        }

        internal static ShuttleWeaponCompatibilityReport NotApplicable(
            string backendId,
            string reasonCode)
        {
            return new ShuttleWeaponCompatibilityReport(
                ShuttleWeaponBackendSupport.NotApplicable,
                backendId,
                reasonCode);
        }

        internal static ShuttleWeaponCompatibilityReport Rejected(
            string backendId,
            string reasonCode)
        {
            return new ShuttleWeaponCompatibilityReport(
                ShuttleWeaponBackendSupport.Rejected,
                backendId,
                reasonCode);
        }
    }

    internal interface IShuttleWeaponLifecycleDriver
    {
        bool EnsureReady(ShuttleModuleRuntimeContext context);
    }

    internal interface IShuttleWeaponTickDriver
    {
        void Tick(ShuttleModuleRuntimeContext context);
    }

    /// <summary>
    /// Narrow host seam used by the backend-neutral cycle pipeline. It may create/rebind only the
    /// selected backend's supported hidden gun and may advance only that gun's owned Verb tracker.
    /// </summary>
    internal interface IShuttleWeaponCycleHost
    {
        bool EnsureReady(ShuttleModuleRuntimeContext context);

        Verb GetAttackVerb(ShuttleModuleRuntimeContext context);

        void TickVerbs(ShuttleModuleRuntimeContext context);
    }

    /// <summary>
    /// Owns the final target check, authoritative magazine reservation and selected-backend cast
    /// entry for one accepted cycle. Failed cast start must restore the reservation exactly once.
    /// </summary>
    internal interface IShuttleWeaponBurstStarter
    {
        void BeginBurst(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb);
    }

    internal interface IShuttleWeaponPowerDemandDriver
    {
        void CollectPowerDemand(ShuttleModuleRuntimeContext context);
    }

    internal interface IShuttleWeaponForcedTargetEvaluator
    {
        ShuttleWeaponEngagementResult Evaluate(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target);
    }

    /// <summary>
    /// Starts one cast only after the owning cycle coordinator has reserved authoritative
    /// ammunition. Target selection, magazine mutation and rollback remain outside this facet.
    /// </summary>
    internal interface IShuttleWeaponFireDriver
    {
        bool IsBusy(ShuttleModuleRuntimeContext context);

        bool TryStartReservedCast(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target,
            out string failureReason);
    }

    /// <summary>
    /// Exposes stable logical channel IDs. Implementations must not persist or publish raw Verb
    /// indices because definition order is not a durable identity contract.
    /// </summary>
    internal interface IShuttleWeaponChannelDriver
    {
        int GetChannelCount(ShuttleModuleRuntimeContext context);

        string GetChannelId(ShuttleModuleRuntimeContext context, int channelIndex);

        string GetSelectedChannelId(ShuttleModuleRuntimeContext context);

        bool TrySelectChannel(
            ShuttleModuleRuntimeContext context,
            string channelId,
            out string failureReason);
    }

    internal interface IShuttleWeaponAmmoReadDriver
    {
        bool TryBuildSnapshot(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponAmmoSnapshot snapshot);
    }

    internal interface IShuttleWeaponAmmoCommandDriver
    {
        bool TrySelectAmmo(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            string ammoDefName,
            out string failureReason);

        bool TryRequestReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            ShuttleWeaponReloadRequestKind requestKind,
            out string failureReason);

        bool TryCancelReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            out string failureReason);

        bool TrySetAutoReloadEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason);

        bool TrySetManualReloadAllowed(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason);

        bool TrySetLogisticsAutoFeedEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason);
    }

    internal interface IShuttleWeaponManualReloadDriver
    {
        bool TryGetPlan(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason);

        bool TryClaim(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason);

        bool TryComplete(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            out string failureReason);
    }

    internal interface IShuttleWeaponRemovalRefundDriver
    {
        bool TryCollect(
            ShuttleModuleRuntimeContext context,
            ShuttleModuleRemovalRefundCollector collector,
            out string failureReason);
    }

    internal interface IShuttleWeaponBackendFactory
    {
        string BackendId { get; }

        int Priority { get; }

        ShuttleWeaponCompatibilityReport Probe(ShuttleWeaponBackendProbeContext context);

        ShuttleWeaponBackendBinding CreateBinding(ShuttleWeaponBackendProbeContext context);
    }
}

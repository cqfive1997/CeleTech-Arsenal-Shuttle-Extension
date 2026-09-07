using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Runtime backend facade for installed shuttle weapons. Detailed tick,
    /// targeting, fire-control, verb, burst, and profiler behavior lives in
    /// focused helpers so the runtime-system boundary stays small.
    /// </summary>
    internal sealed class ShuttleWeaponRuntimeSystem :
        ShuttleModuleRuntimeSystemBase,
        IShuttleModuleRemovalRefundContributor
    {
        public static readonly ShuttleWeaponRuntimeSystem Instance = new ShuttleWeaponRuntimeSystem();

        internal const string WeaponRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Weapon;

        private readonly ShuttleWeaponBackendRegistry backendRegistry;

        private ShuttleWeaponRuntimeSystem()
        {
            this.backendRegistry = ShuttleWeaponBackendRegistry.Shared;
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return WeaponRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                return 1;
            }
        }

        public override bool ParticipatesInPowerDemand
        {
            get
            {
                return true;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            return module != null && module.ModuleDef is ShuttleWeaponModuleDef;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new ShuttleWeaponRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding != null)
            {
                binding.Lifecycle.EnsureReady(context);
            }
        }

        public override void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding != null)
            {
                binding.PowerDemand.CollectPowerDemand(context);
            }
        }

        public override void Tick(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponFacadeTickDiagnostics diagnostics =
                ShuttleWeaponFacadeTickDiagnostics.Create(context);
            diagnostics.Begin();
            try
            {
                ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
                diagnostics.CompleteBindingAndStartDriver();
                if (binding != null)
                {
                    binding.TickDriver.Tick(context);
                }
            }
            finally
            {
                diagnostics.Finish();
            }
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
            this.Reconcile(context);
        }

        public override void OnArrived(ShuttleModuleRuntimeContext context)
        {
            this.Reconcile(context);
        }

        bool IShuttleModuleRemovalRefundContributor.TryCollectRemovalRefunds(
            ShuttleModuleRuntimeContext context,
            ShuttleModuleRemovalRefundCollector collector,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_RemovalRefundUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            return binding.RemovalRefund.TryCollect(context, collector, out failureReason);
        }

        internal bool TryBuildAmmoSnapshot(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponAmmoSnapshot snapshot)
        {
            snapshot = null;
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            return binding != null &&
                binding.AmmoReader.TryBuildSnapshot(context, out snapshot);
        }

        internal bool TrySelectAmmo(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            string ammoDefName,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(weaponDef, weaponState);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return binding.AmmoCommands.TrySelectAmmo(
                moduleInstanceID,
                weaponDef,
                weaponState,
                ammoDefName,
                out failureReason);
        }

        internal bool TryRequestReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            ShuttleWeaponReloadRequestKind requestKind,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(weaponDef, weaponState);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return binding.AmmoCommands.TryRequestReload(
                moduleInstanceID,
                weaponDef,
                weaponState,
                requestKind,
                out failureReason);
        }

        internal bool TryCancelReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(weaponDef, weaponState);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return binding.AmmoCommands.TryCancelReload(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
        }

        internal bool TrySetAutoReloadEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(weaponDef, weaponState);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return binding.AmmoCommands.TrySetAutoReloadEnabled(
                moduleInstanceID,
                weaponDef,
                weaponState,
                enabled,
                out failureReason);
        }

        internal bool TrySetManualReloadAllowed(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(weaponDef, weaponState);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return binding.AmmoCommands.TrySetManualReloadAllowed(
                moduleInstanceID,
                weaponDef,
                weaponState,
                enabled,
                out failureReason);
        }

        internal bool TrySetLogisticsAutoFeedEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(weaponDef, weaponState);
            if (binding == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return binding.AmmoCommands.TrySetLogisticsAutoFeedEnabled(
                moduleInstanceID,
                weaponDef,
                weaponState,
                enabled,
                out failureReason);
        }

        internal bool TryGetManualReloadPlan(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            plan = null;
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding == null || !binding.Lifecycle.EnsureReady(context))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob"
                    .Translate()
                    .ToString();
                return false;
            }

            return binding.ManualReload.TryGetPlan(
                context,
                out plan,
                out failureReason);
        }

        internal bool TryClaimManualReload(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            plan = null;
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding == null || !binding.Lifecycle.EnsureReady(context))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob"
                    .Translate()
                    .ToString();
                return false;
            }

            return binding.ManualReload.TryClaim(
                context,
                pawn,
                jobLoadID,
                requiredAmmoThingDef,
                out plan,
                out failureReason);
        }

        internal bool TryCompleteManualReload(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            out string failureReason)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding == null || !binding.Lifecycle.EnsureReady(context))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob"
                    .Translate()
                    .ToString();
                return false;
            }

            return binding.ManualReload.TryComplete(
                context,
                pawn,
                jobLoadID,
                out failureReason);
        }

        internal ShuttleWeaponEngagementResult EvaluateForcedTarget(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target)
        {
            ShuttleWeaponBackendBinding binding = this.GetOrResolveBinding(context);
            if (binding == null)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.BackendUnavailable);
            }

            if (!binding.Lifecycle.EnsureReady(context))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            return binding.ForcedTargetEvaluator.Evaluate(context, target);
        }

        private ShuttleWeaponBackendBinding GetOrResolveBinding(
            ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            return this.GetOrResolveBinding(weaponDef, state);
        }

        private ShuttleWeaponBackendBinding GetOrResolveBinding(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            if (weaponDef == null || state == null)
            {
                return null;
            }

            ShuttleWeaponBackendBinding cached;
            if (state.TryGetBackendBindingForRuntimeOnly(weaponDef, out cached))
            {
                return cached;
            }

            ShuttleWeaponBackendBinding resolved;
            ShuttleWeaponCompatibilityReport report;
            ShuttleWeaponBackendProbeContext probeContext =
                new ShuttleWeaponBackendProbeContext(
                    weaponDef,
                    state.GunForRuntimeOnly,
                    state.MagazineAuthorityBackendIdForRuntimeOnly,
                    state.IsMagazineAuthorityTransferIdleForRuntimeOnly());
            if (!this.backendRegistry.TryResolve(probeContext, out resolved, out report))
            {
                state.ClearBackendBindingForRuntimeOnly();
                return null;
            }

            string magazineAuthority = state.MagazineAuthorityBackendIdForRuntimeOnly;
            if (magazineAuthority != ShuttleWeaponRuntimeState.CoreMagazineAuthorityId &&
                resolved.BackendId != magazineAuthority)
            {
                state.ClearBackendBindingForRuntimeOnly();
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Weapon magazine authority '" +
                    magazineAuthority + "' cannot fall back to backend '" +
                    resolved.BackendId + "'. The weapon was stopped to preserve its magazine.",
                    AuthorityMismatchWarningKey(weaponDef.defName, magazineAuthority));
                return null;
            }

            state.SetBackendBindingForRuntimeOnly(weaponDef, resolved);
            return resolved;
        }

        private static int AuthorityMismatchWarningKey(
            string weaponDefName,
            string authorityBackendId)
        {
            unchecked
            {
                int hash = 1786049137;
                string text = (weaponDefName ?? string.Empty) + "|" +
                    (authorityBackendId ?? string.Empty);
                for (int i = 0; i < text.Length; i++)
                {
                    hash = (hash * 397) ^ text[i];
                }

                return hash;
            }
        }
    }
}

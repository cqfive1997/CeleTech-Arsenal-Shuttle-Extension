using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Owns the one-time idle-boundary authority transfer and CE host rebinding. It does not tick,
    /// select targets, consume cargo or decide reload policy.
    /// </summary>
    internal sealed class CeWeaponBackendLifecycleDriver : IShuttleWeaponLifecycleDriver
    {
        private const string LegacyBackendId = "legacy";

        private readonly CeWeaponMagazineAuthorityTransfer authorityTransfer =
            new CeWeaponMagazineAuthorityTransfer();
        private readonly CoreMagazineDriver sourceMagazine =
            new CoreMagazineDriver(new ShuttleWeaponAmmoDefinitionCatalog());
        private readonly CeWeaponCycleDriver cycleDriver;
        private readonly ConditionalWeakTable<ShuttleWeaponModuleDef, CachedAmmoExtension>
            ammoExtensionCache =
                new ConditionalWeakTable<ShuttleWeaponModuleDef, CachedAmmoExtension>();

        internal CeWeaponBackendLifecycleDriver(CeWeaponCycleDriver cycleDriver)
        {
            this.cycleDriver = cycleDriver;
        }

        public bool EnsureReady(ShuttleModuleRuntimeContext context)
        {
            string failureReason;
            bool ready = this.TryEnsureReady(context, out failureReason);
            if (!ready && !string.IsNullOrEmpty(failureReason))
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle][CE Backend] Weapon lifecycle stopped: " +
                    failureReason + ".",
                    FailureKey(context, failureReason));
            }

            return ready;
        }

        internal bool TryEnsureReady(
            ShuttleModuleRuntimeContext context,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (context == null || context.Host == null || !context.Host.Spawned ||
                weaponDef == null || state == null || this.cycleDriver == null)
            {
                failureReason = "ce-lifecycle-context-missing";
                return false;
            }

            ShuttleWeaponModuleAmmoExtension ammoExtension =
                this.GetAmmoExtension(weaponDef);
            if (ammoExtension != null)
            {
                // Loader-versus-Pawn is executor selection, not two independent player policies.
                state.AmmoForRuntimeOnly.SetManualReloadAllowed(
                    ammoExtension.allowManualReload);
                if (state.AmmoForRuntimeOnly.LogisticsAutoFeedEnabled)
                {
                    state.AmmoForRuntimeOnly.SetLogisticsAutoFeedEnabled(false);
                }
            }

            string authority = state.MagazineAuthorityBackendIdForRuntimeOnly;
            if (authority == LegacyBackendId)
            {
                if (!state.IsMagazineAuthorityTransferIdleForRuntimeOnly())
                {
                    failureReason = "ce-authority-transfer-not-idle";
                    return false;
                }

                if (this.sourceMagazine.GetOrCreate(
                        context.ModuleInstanceID,
                        weaponDef,
                        state) == null)
                {
                    failureReason = "ce-authority-source-magazine-missing";
                    return false;
                }

                CeWeaponPreparedMagazineTransfer prepared;
                if (!this.authorityTransfer.TryPrepare(
                        weaponDef,
                        state,
                        out prepared,
                        out failureReason) ||
                    !this.authorityTransfer.TryCommit(
                        state,
                        prepared,
                        out failureReason))
                {
                    return false;
                }

                authority = state.MagazineAuthorityBackendIdForRuntimeOnly;
            }

            if (authority != CeWeaponBackendFactory.Id)
            {
                failureReason = "ce-magazine-authority-mismatch";
                return false;
            }

            Thing gun = state.GunForRuntimeOnly;
            CeWeaponMuzzleVerb existingVerb = CeWeaponRuntimeGunAccess.GetMuzzleVerb(gun);
            global::CombatExtended.CompAmmoUser existingMagazine =
                CeWeaponRuntimeGunAccess.GetMagazine(gun);
            CeWeaponTurretOwnerToken existingToken = existingMagazine != null
                ? existingMagazine.turret as CeWeaponTurretOwnerToken
                : null;
            if (state.AreVerbsBoundForRuntimeOnly(context.Host, gun) &&
                existingVerb != null &&
                ReferenceEquals(existingVerb.caster, context.Host) &&
                existingToken != null &&
                existingToken.OwnerAdapter != null &&
                existingToken.OwnerAdapter.ContextMatches(context.Host))
            {
                return true;
            }

            CeWeaponMuzzleVerb verb;
            CeWeaponAmmoOwnerAdapter owner;
            int shotsPerBurst;
            if (!this.cycleDriver.TryBind(
                    context,
                    null,
                    out verb,
                    out owner,
                    out shotsPerBurst,
                    out failureReason))
            {
                return false;
            }

            if (verb == null || owner == null || shotsPerBurst <= 1 ||
                !owner.ContextMatches(context.Host))
            {
                failureReason = "ce-runtime-binding-postcondition-failed";
                return false;
            }

            return true;
        }

        private ShuttleWeaponModuleAmmoExtension GetAmmoExtension(
            ShuttleWeaponModuleDef weaponDef)
        {
            if (weaponDef == null)
            {
                return null;
            }

            CachedAmmoExtension cached =
                this.ammoExtensionCache.GetOrCreateValue(weaponDef);
            if (!cached.Resolved)
            {
                cached.Extension =
                    weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>();
                cached.Resolved = true;
            }

            return cached.Extension;
        }

        private sealed class CachedAmmoExtension
        {
            public CachedAmmoExtension()
            {
            }

            internal bool Resolved;
            internal ShuttleWeaponModuleAmmoExtension Extension;
        }

        private static int FailureKey(
            ShuttleModuleRuntimeContext context,
            string failureReason)
        {
            unchecked
            {
                string moduleId = context != null
                    ? context.ModuleInstanceID
                    : string.Empty;
                string value = moduleId + "|" + (failureReason ?? string.Empty);
                int hash = 1847061299;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }
    }
}

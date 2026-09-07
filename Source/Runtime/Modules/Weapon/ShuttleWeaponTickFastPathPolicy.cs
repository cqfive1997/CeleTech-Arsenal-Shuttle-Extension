using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Evaluates cooldown and automatic-reload early-return conditions without mutating state.
    /// </summary>
    internal sealed class ShuttleWeaponTickFastPathPolicy
    {
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;

        internal ShuttleWeaponTickFastPathPolicy(
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine)
        {
            this.ammoDefinitions = ammoDefinitions;
            this.coreMagazine = coreMagazine;
        }

        internal bool CanUseCooldownFastPath(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (ammoState != null &&
                (ammoState.ReloadInProgress ||
                 ammoState.ManualReloadJobActive ||
                 this.DoesReloadRequestBlock(weaponDef, ammoState)))
            {
                return false;
            }

            return this.coreMagazine.CanFire(weaponDef, ammoState);
        }

        internal bool CanThrottleAutomaticReloadRetry(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            ShuttleWeaponAmmoState ammoState,
            int cooldownTicks)
        {
            if (context == null || weaponDef == null || state == null ||
                attackVerb == null || ammoState == null)
            {
                return false;
            }

            if (!ammoState.ReloadRequested ||
                ammoState.ReloadInProgress ||
                ammoState.ManualReloadJobActive ||
                ammoState.ReloadRequestKind == ShuttleWeaponReloadRequestKind.Manual)
            {
                return false;
            }

            ShuttleWeaponReloadRequestKind requestKind =
                this.GetEffectiveRequestKind(weaponDef, ammoState);
            if (requestKind != ShuttleWeaponReloadRequestKind.RequiredForFire &&
                requestKind != ShuttleWeaponReloadRequestKind.AutoTopOff)
            {
                return false;
            }

            if (this.coreMagazine.CanFire(weaponDef, ammoState) ||
                ammoState.CanAttemptAutomaticReloadCheck(context.TicksGame))
            {
                return false;
            }

            return attackVerb.state != VerbState.Bursting &&
                state.GetWarmupTicksForRuntimeOnly() <= 0 &&
                cooldownTicks <= 0;
        }

        internal WeaponCooldownFastPathMissReason GetCooldownMissReason(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (this.ammoDefinitions.HasAmmoSystem(weaponDef) && ammoState == null)
            {
                return WeaponCooldownFastPathMissReason.InvalidAmmoState;
            }

            if (ammoState != null && ammoState.ReloadInProgress)
            {
                return WeaponCooldownFastPathMissReason.ReloadInProgress;
            }

            if (ammoState != null && this.DoesReloadRequestBlock(weaponDef, ammoState))
            {
                return WeaponCooldownFastPathMissReason.ReloadRequested;
            }

            return !this.coreMagazine.CanFire(weaponDef, ammoState)
                ? WeaponCooldownFastPathMissReason.LoadedAmmoInsufficient
                : WeaponCooldownFastPathMissReason.Other;
        }

        internal bool DoesReloadRequestBlock(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (ammoState == null || !ammoState.ReloadRequested)
            {
                return false;
            }

            if (ammoState.ReloadRequestKind == ShuttleWeaponReloadRequestKind.Manual)
            {
                return true;
            }

            return !this.coreMagazine.CanFire(weaponDef, ammoState);
        }

        private ShuttleWeaponReloadRequestKind GetEffectiveRequestKind(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            return ShuttleWeaponReloadPolicy.ResolveEffectiveRequestKind(
                ammoState != null && ammoState.ReloadRequested,
                ammoState != null
                    ? ammoState.ReloadRequestKind
                    : ShuttleWeaponReloadRequestKind.None,
                this.coreMagazine.CanFire(weaponDef, ammoState));
        }
    }
}

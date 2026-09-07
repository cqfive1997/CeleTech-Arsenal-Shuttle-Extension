using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponFireControlPolicy
    {
        private readonly ShuttleWeaponBallisticTargetValidator ballisticTargetValidator;

        internal ShuttleWeaponFireControlPolicy(
            ShuttleWeaponBallisticTargetValidator ballisticTargetValidator)
        {
            this.ballisticTargetValidator = ballisticTargetValidator;
        }

        internal bool CanOperate(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            if (context == null || context.Host == null || !context.Host.Spawned)
            {
                return false;
            }

            if (weaponDef == null || state == null || !context.IsEnabled)
            {
                return false;
            }

            if (state.IsHoldFireForRuntimeOnly() || !context.InternalBusPowered)
            {
                return false;
            }

            if (state.FireControlMode == ShuttleWeaponFireControlMode.Offline)
            {
                return false;
            }

            Thing gun = state.GunForRuntimeOnly;
            if (gun == null || gun.Destroyed || attackVerb == null || !attackVerb.Available())
            {
                return false;
            }

            if (context.Host.Map == null)
            {
                return false;
            }

            ShuttleWeaponMuzzleSource source =
                this.ballisticTargetValidator.ResolveValidationSource(
                context,
                weaponDef,
                state,
                attackVerb,
                LocalTargetInfo.Invalid);
            if (attackVerb.ProjectileFliesOverhead() &&
                source.Cell.IsValid &&
                context.Host.Map.roofGrid.Roofed(source.Cell))
            {
                return false;
            }

            return true;
        }

        internal bool IsFireControlHardStopped(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponRuntimeState state)
        {
            if (context == null || state == null)
            {
                return true;
            }

            return state.IsHoldFireForRuntimeOnly() ||
                state.FireControlMode == ShuttleWeaponFireControlMode.Offline ||
                !context.InternalBusPowered;
        }

        internal bool CanUseAutomaticFireControl(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            if (context == null || weaponDef == null || state == null)
            {
                return false;
            }

            if (!state.AutoFireEnabled ||
                state.FireControlMode == ShuttleWeaponFireControlMode.Offline ||
                state.FireControlMode == ShuttleWeaponFireControlMode.ManualOnly)
            {
                return false;
            }

            if (!this.HasEffectiveFireControlLink(context, weaponDef, state))
            {
                return this.CanUseFallbackAutoDefense(context, weaponDef, state);
            }

            // TODO W3+: gate scanner/navigation requirements once stable capability profiles exist.
            switch (state.FireControlMode)
            {
                case ShuttleWeaponFireControlMode.AutoDefense:
                    return context.Profile.FireControl.SupportsAutoDefense;
                case ShuttleWeaponFireControlMode.PointDefense:
                    return context.Profile.FireControl.SupportsPointDefense;
                default:
                    return false;
            }
        }

        internal bool CanUseFallbackAutoDefense(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            return context != null &&
                weaponDef != null &&
                state != null &&
                weaponDef.canAutoFire &&
                weaponDef.canAutoFireWithoutFireControl &&
                state.FireControlMode == ShuttleWeaponFireControlMode.AutoDefense;
        }

        internal bool IsUsingFallbackAutoDefense(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            return !this.HasEffectiveFireControlLink(context, weaponDef, state) &&
                this.CanUseFallbackAutoDefense(context, weaponDef, state);
        }

        internal bool HasEffectiveFireControlLink(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            return context != null &&
                weaponDef != null &&
                state != null &&
                state.FireControlLinked &&
                weaponDef.canAutoFire &&
                context.Profile != null &&
                context.Profile.FireControl != null &&
                context.Profile.FireControl.HasFireControlRadar;
        }

        internal bool HasEffectiveFireControlAccuracyLink(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            return context != null &&
                weaponDef != null &&
                state != null &&
                state.FireControlLinked &&
                context.Profile != null &&
                context.Profile.FireControl != null &&
                context.Profile.FireControl.HasFireControlRadar;
        }
    }
}

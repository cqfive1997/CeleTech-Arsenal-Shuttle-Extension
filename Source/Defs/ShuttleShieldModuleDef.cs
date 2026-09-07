using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Common shield-module settings shared by any concrete shield backend.
    /// Live shield backend state must stay out of Defs and Profile.
    /// </summary>
    public class ShuttleShieldModuleDef : ShuttleModuleBaseDef
    {
        // Common settings layer shared by future shield backends. These are static tuning values;
        // the selected radius lives in ShuttleShieldRuntimeState.
        public bool supportsRadiusControl = true;
        public float minRadius;
        public float maxRadius;
        public float defaultRadius;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.ModuleType != ShuttleModuleType.Shield)
            {
                yield return this.defName + " must use moduleTypeID shield.";
            }

            if (!this.ContainsOnlySegmentType(ShuttleSegmentType.Weapon))
            {
                yield return this.defName + " must install only into weapon segment type.";
            }

            if (!this.ContainsOnlyModuleSlotType(ShuttleModuleType.Shield))
            {
                yield return this.defName + " must install only into shield module slot type.";
            }

            foreach (string error in this.ValidateRadiusValues())
            {
                yield return error;
            }
        }

        private IEnumerable<string> ValidateRadiusValues()
        {
            if (!this.IsFiniteFloat(this.minRadius))
            {
                yield return this.defName + " has non-finite minRadius.";
            }
            else if (this.minRadius < 0f)
            {
                yield return this.defName + " has negative minRadius.";
            }

            if (!this.IsFiniteFloat(this.maxRadius))
            {
                yield return this.defName + " has non-finite maxRadius.";
            }
            else if (this.maxRadius < 0f)
            {
                yield return this.defName + " has negative maxRadius.";
            }

            if (!this.IsFiniteFloat(this.defaultRadius))
            {
                yield return this.defName + " has non-finite defaultRadius.";
            }
            else if (this.defaultRadius < 0f)
            {
                yield return this.defName + " has negative defaultRadius.";
            }

            if (this.IsFiniteFloat(this.minRadius) &&
                this.IsFiniteFloat(this.maxRadius) &&
                this.maxRadius < this.minRadius)
            {
                yield return this.defName + " has maxRadius smaller than minRadius.";
            }

            if (this.IsFiniteFloat(this.minRadius) &&
                this.IsFiniteFloat(this.maxRadius) &&
                this.IsFiniteFloat(this.defaultRadius) &&
                (this.defaultRadius < this.minRadius || this.defaultRadius > this.maxRadius))
            {
                yield return this.defName + " has defaultRadius outside minRadius/maxRadius.";
            }
        }

        private bool ContainsOnlySegmentType(ShuttleSegmentType expectedType)
        {
            IReadOnlyList<ShuttleSegmentType> segmentTypes = this.InstallableSegmentTypeEnums;
            if (segmentTypes == null || segmentTypes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < segmentTypes.Count; i++)
            {
                if (segmentTypes[i] != expectedType)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ContainsOnlyModuleSlotType(ShuttleModuleType expectedType)
        {
            IReadOnlyList<ShuttleModuleType> slotTypes = this.InstallableModuleSlotTypeEnums;
            if (slotTypes == null || slotTypes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < slotTypes.Count; i++)
            {
                if (slotTypes[i] != expectedType)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Static tuning for the vanilla CompProjectileInterceptor shield backend.
    /// Current hit points and cooldown remain owned by the vanilla comp instance.
    /// </summary>
    public sealed class ShuttleVanillaInterceptorShieldModuleDef : ShuttleShieldModuleDef
    {
        // Backend tuning copied into the instance-local projectile-interceptor props at runtime.
        // Current HP, cooldown, stunner, and last intercept tick remain owned by the vanilla comp.
        public int shieldHitPoints;
        public int hitPointsPerRechargeInterval;
        public int rechargeIntervalTicks;
        public float rechargeEnergyPerHitPointWd;
        public bool interceptGroundProjectiles = true;
        public bool interceptAirProjectiles = true;
        public bool interceptNonHostileProjectiles;
        public int cooldownTicks;
        public EffecterDef interceptEffect;
        public Color color = Color.white;
        public SoundDef activeSound;
        public EffecterDef reactivateEffect;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.shieldHitPoints < 0)
            {
                yield return this.defName + " has negative shieldHitPoints.";
            }

            if (this.hitPointsPerRechargeInterval < 0)
            {
                yield return this.defName + " has negative hitPointsPerRechargeInterval.";
            }

            if (this.rechargeIntervalTicks < 0)
            {
                yield return this.defName + " has negative rechargeIntervalTicks.";
            }

            if (this.shieldHitPoints > 0 && this.hitPointsPerRechargeInterval <= 0)
            {
                yield return this.defName + " must have positive hitPointsPerRechargeInterval when shieldHitPoints is positive.";
            }

            if (this.shieldHitPoints > 0 && this.rechargeIntervalTicks <= 0)
            {
                yield return this.defName + " must have positive rechargeIntervalTicks when shieldHitPoints is positive.";
            }

            if (!this.IsFiniteFloat(this.rechargeEnergyPerHitPointWd))
            {
                yield return this.defName + " has non-finite rechargeEnergyPerHitPointWd.";
            }
            else if (this.rechargeEnergyPerHitPointWd < 0f)
            {
                yield return this.defName + " has negative rechargeEnergyPerHitPointWd.";
            }

            if (this.cooldownTicks < 0)
            {
                yield return this.defName + " has negative cooldownTicks.";
            }

            if (!this.interceptGroundProjectiles && !this.interceptAirProjectiles)
            {
                yield return this.defName + " must intercept ground or air projectiles.";
            }

            if (!this.IsFiniteColor(this.color))
            {
                yield return this.defName + " has non-finite color.";
            }
        }

        private bool IsFiniteColor(Color value)
        {
            return this.IsFiniteFloat(value.r) &&
                this.IsFiniteFloat(value.g) &&
                this.IsFiniteFloat(value.b) &&
                this.IsFiniteFloat(value.a);
        }
    }
}

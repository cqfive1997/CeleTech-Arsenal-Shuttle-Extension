using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static tuning for the custom shuttle-owned surface shield backend.
    /// Current shield HP, cooldown, recharge progress, and visual pulses must stay out of Defs.
    /// </summary>
    public sealed class ShuttleSurfaceShieldModuleDef : ShuttleShieldModuleDef
    {
        public int maxHitPoints;
        public float initialHitPointsPercent = 0f;

        public int rechargeHitPointsPerInterval;
        public int rechargeIntervalTicks;
        public float rechargeEnergyPerHitPointWd;
        public float damageToShieldMultiplier = 1f;

        public bool absorbsProjectileDamage = true;
        public bool absorbsExplosionDamage = true;
        public bool absorbsMeleeDamage = true;
        public bool absorbsFireDamage = true;
        public bool absorbsEMP = true;

        public float empShieldDamageMultiplier = 2f;

        public string surfaceShaderBundlePath;
        public string surfaceMaterialAssetName;
        public string surfaceMaskTexturePath;
        public string surfaceNoiseTexturePath;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.maxHitPoints < 0)
            {
                yield return this.defName + " has negative maxHitPoints.";
            }

            if (!this.IsFiniteFloat(this.initialHitPointsPercent))
            {
                yield return this.defName + " has non-finite initialHitPointsPercent.";
            }
            else if (this.initialHitPointsPercent < 0f || this.initialHitPointsPercent > 1f)
            {
                yield return this.defName + " has initialHitPointsPercent outside 0..1.";
            }

            if (this.rechargeHitPointsPerInterval < 0)
            {
                yield return this.defName + " has negative rechargeHitPointsPerInterval.";
            }

            if (this.rechargeIntervalTicks < 0)
            {
                yield return this.defName + " has negative rechargeIntervalTicks.";
            }

            if (this.maxHitPoints > 0 &&
                this.rechargeHitPointsPerInterval > 0 &&
                this.rechargeIntervalTicks <= 0)
            {
                yield return this.defName + " must have positive rechargeIntervalTicks when recharge is enabled.";
            }

            if (!this.IsFiniteFloat(this.rechargeEnergyPerHitPointWd))
            {
                yield return this.defName + " has non-finite rechargeEnergyPerHitPointWd.";
            }
            else if (this.rechargeEnergyPerHitPointWd < 0f)
            {
                yield return this.defName + " has negative rechargeEnergyPerHitPointWd.";
            }

            if (!this.IsFiniteFloat(this.damageToShieldMultiplier))
            {
                yield return this.defName + " has non-finite damageToShieldMultiplier.";
            }
            else if (this.damageToShieldMultiplier < 0f)
            {
                yield return this.defName + " has negative damageToShieldMultiplier.";
            }

            if (!this.IsFiniteFloat(this.empShieldDamageMultiplier))
            {
                yield return this.defName + " has non-finite empShieldDamageMultiplier.";
            }
            else if (this.empShieldDamageMultiplier < 0f)
            {
                yield return this.defName + " has negative empShieldDamageMultiplier.";
            }
        }
    }
}

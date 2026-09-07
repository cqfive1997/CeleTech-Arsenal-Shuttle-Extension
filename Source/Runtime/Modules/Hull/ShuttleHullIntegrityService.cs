using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Damage;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullIntegrityService
    {
        public void ApplyProfile(ShuttleProfile profile, ShuttleRuntimeState runtimeState)
        {
            int maxHitPoints = 0;
            if (profile != null && profile.Hull != null)
            {
                maxHitPoints = profile.Hull.MaxHitPoints;
            }

            if (runtimeState != null)
            {
                runtimeState.Hull.ReconcileMaxHitPoints(maxHitPoints);
            }
        }

        public bool TryApplyIncomingDamage(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ref DamageInfo dinfo,
            int ticksGame,
            out ShuttleHullDamageResult result)
        {
            result = ShuttleHullDamageResult.NotHandled();
            if (profile == null ||
                profile.Hull == null ||
                runtimeState == null ||
                profile.Hull.MaxHitPoints <= 0)
            {
                return false;
            }

            runtimeState.Hull.ReconcileMaxHitPoints(profile.Hull.MaxHitPoints);

            float incomingDamage = dinfo.Amount;
            if (!this.IsFiniteFloat(incomingDamage) || incomingDamage <= 0f)
            {
                return false;
            }

            ShuttleDamageClassification classification =
                DamageClassificationUtility.Classify(dinfo);
            float multiplier = this.GetDamageMultiplier(profile.Hull, classification);
            float flatReduction = this.SanitizeNonNegativeFinite(profile.Hull.FlatDamageReduction);

            float hullBefore = runtimeState.Hull.CurrentHitPoints;
            if (hullBefore <= 0f)
            {
                return false;
            }

            float armorPoolDamage = this.CalculateArmorPoolDamage(
                incomingDamage,
                multiplier,
                flatReduction);
            if (armorPoolDamage <= 0f)
            {
                result = ShuttleHullDamageResult.CreateFullyAbsorbed(
                    incomingDamage,
                    armorPoolDamage,
                    0f,
                    0f,
                    hullBefore,
                    hullBefore);
                return true;
            }

            float absorbedByHull = runtimeState.Hull.ApplyDamage(armorPoolDamage, ticksGame);
            float hullAfter = runtimeState.Hull.CurrentHitPoints;
            float remainingDamage = this.CalculateRemainingDamage(
                incomingDamage,
                armorPoolDamage,
                absorbedByHull);
            if (remainingDamage <= 0f)
            {
                result = ShuttleHullDamageResult.CreateFullyAbsorbed(
                    incomingDamage,
                    armorPoolDamage,
                    absorbedByHull,
                    0f,
                    hullBefore,
                    hullAfter);
                return true;
            }

            dinfo.SetAmount(remainingDamage);
            result = ShuttleHullDamageResult.CreatePartiallyAbsorbed(
                incomingDamage,
                armorPoolDamage,
                absorbedByHull,
                remainingDamage,
                hullBefore,
                hullAfter);
            return true;
        }

        private float GetDamageMultiplier(
            HullProfile hull,
            ShuttleDamageClassification classification)
        {
            if (hull == null)
            {
                return 1f;
            }

            if (classification.HullCategory == ShuttleDamageCategory.EMP)
            {
                return this.SanitizePositiveFinite(hull.EmpDamageMultiplier);
            }

            if (classification.HullCategory == ShuttleDamageCategory.Explosion)
            {
                return this.SanitizePositiveFinite(hull.ExplosionDamageMultiplier);
            }

            if (classification.HullCategory == ShuttleDamageCategory.Sharp)
            {
                return this.SanitizePositiveFinite(hull.SharpDamageMultiplier);
            }

            if (classification.HullCategory == ShuttleDamageCategory.Blunt)
            {
                return this.SanitizePositiveFinite(hull.BluntDamageMultiplier);
            }

            if (classification.HullCategory == ShuttleDamageCategory.Heat)
            {
                return this.SanitizePositiveFinite(hull.HeatDamageMultiplier);
            }

            return 1f;
        }

        private float CalculateArmorPoolDamage(
            float incomingDamage,
            float multiplier,
            float flatReduction)
        {
            incomingDamage = this.SanitizeNonNegativeFinite(incomingDamage);
            multiplier = this.SanitizePositiveFinite(multiplier);
            flatReduction = this.SanitizeNonNegativeFinite(flatReduction);

            float armorPoolDamage = (incomingDamage * multiplier) - flatReduction;
            if (!this.IsFiniteFloat(armorPoolDamage) || armorPoolDamage < 0f)
            {
                return 0f;
            }

            return armorPoolDamage;
        }

        private float CalculateRemainingDamage(
            float incomingDamage,
            float armorPoolDamage,
            float absorbedByHull)
        {
            incomingDamage = this.SanitizeNonNegativeFinite(incomingDamage);
            armorPoolDamage = this.SanitizeNonNegativeFinite(armorPoolDamage);
            absorbedByHull = this.SanitizeNonNegativeFinite(absorbedByHull);
            if (incomingDamage <= 0f || armorPoolDamage <= 0f)
            {
                return 0f;
            }

            float overflowArmorDamage = armorPoolDamage - absorbedByHull;
            if (!this.IsFiniteFloat(overflowArmorDamage) || overflowArmorDamage <= 0f)
            {
                return 0f;
            }

            float remaining = incomingDamage * (overflowArmorDamage / armorPoolDamage);
            if (!this.IsFiniteFloat(remaining) || remaining < 0f)
            {
                return 0f;
            }

            return remaining > incomingDamage ? incomingDamage : remaining;
        }

        private float SanitizePositiveFinite(float value)
        {
            if (!this.IsFiniteFloat(value) || value <= 0f)
            {
                return 1f;
            }

            return value;
        }

        private float SanitizeNonNegativeFinite(float value)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

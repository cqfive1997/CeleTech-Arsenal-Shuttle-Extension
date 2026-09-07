using System;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Damage
{
    public static class DamageClassificationUtility
    {
        private const string ArmorCategoryBlunt = "Blunt";
        private const string ArmorCategoryHeat = "Heat";

        public static ShuttleDamageClassification Classify(DamageInfo dinfo)
        {
            DamageDef damageDef = dinfo.Def;
            bool hasDamageAmount = IsFinitePositive(dinfo.Amount);
            if (damageDef == null)
            {
                return new ShuttleDamageClassification(
                    ShuttleDamageCategory.Unknown,
                    ShuttleDamageCategory.Unknown,
                    false,
                    false,
                    false,
                    false,
                    false,
                    hasDamageAmount);
            }

            string defName = damageDef.defName ?? string.Empty;
            bool isEmp = IsEMP(damageDef, defName);
            bool isExplosive = damageDef.isExplosive;
            bool isHeat = IsHeat(damageDef, defName);
            bool isSharp = damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
            bool isBlunt = IsArmorCategory(damageDef, ArmorCategoryBlunt);
            bool isProjectile = IsProjectile(damageDef);

            ShuttleDamageCategory hullCategory = ResolveHullCategory(
                isEmp,
                isExplosive,
                isSharp,
                isBlunt,
                isHeat);
            ShuttleDamageCategory surfaceCategory = ResolveSurfaceCategory(
                isEmp,
                isHeat,
                isExplosive,
                isProjectile);
            bool isDirect =
                hullCategory == ShuttleDamageCategory.Direct &&
                surfaceCategory == ShuttleDamageCategory.Direct;

            return new ShuttleDamageClassification(
                hullCategory,
                surfaceCategory,
                isProjectile,
                isExplosive,
                isEmp,
                isHeat,
                isDirect,
                hasDamageAmount);
        }

        public static string BuildDevDebugLine(DamageInfo dinfo)
        {
            return BuildDevDebugLine(dinfo, Classify(dinfo));
        }

        public static string BuildDevDebugLine(
            DamageInfo dinfo,
            ShuttleDamageClassification classification)
        {
            DamageDef damageDef = dinfo.Def;
            string defName = damageDef != null && !string.IsNullOrEmpty(damageDef.defName)
                ? damageDef.defName
                : "<null>";
            string armorCategory =
                damageDef != null &&
                damageDef.armorCategory != null &&
                !string.IsNullOrEmpty(damageDef.armorCategory.defName)
                    ? damageDef.armorCategory.defName
                    : "<none>";

            return "[DEV] DamageClassify: Def=" + defName +
                " Hull=" + classification.HullCategory.ToString() +
                " Surface=" + classification.SurfaceCategory.ToString() +
                " Projectile=" + classification.IsProjectile.ToString() +
                " Explosive=" + classification.IsExplosive.ToString() +
                " EMP=" + classification.IsEMP.ToString() +
                " Heat=" + classification.IsHeat.ToString() +
                " Direct=" + classification.IsDirect.ToString() +
                " HasDamageAmount=" + classification.HasDamageAmount.ToString() +
                " ArmorCategory=" + armorCategory;
        }

        private static ShuttleDamageCategory ResolveHullCategory(
            bool isEmp,
            bool isExplosive,
            bool isSharp,
            bool isBlunt,
            bool isHeat)
        {
            if (isEmp)
            {
                return ShuttleDamageCategory.EMP;
            }

            if (isExplosive)
            {
                return ShuttleDamageCategory.Explosion;
            }

            if (isSharp)
            {
                return ShuttleDamageCategory.Sharp;
            }

            if (isBlunt)
            {
                return ShuttleDamageCategory.Blunt;
            }

            if (isHeat)
            {
                return ShuttleDamageCategory.Heat;
            }

            return ShuttleDamageCategory.Direct;
        }

        private static ShuttleDamageCategory ResolveSurfaceCategory(
            bool isEmp,
            bool isHeat,
            bool isExplosive,
            bool isProjectile)
        {
            if (isEmp)
            {
                return ShuttleDamageCategory.EMP;
            }

            if (isHeat)
            {
                return ShuttleDamageCategory.Heat;
            }

            if (isExplosive)
            {
                return ShuttleDamageCategory.Explosion;
            }

            if (isProjectile)
            {
                return ShuttleDamageCategory.Projectile;
            }

            return ShuttleDamageCategory.Direct;
        }

        private static bool IsEMP(DamageDef damageDef, string defName)
        {
            return damageDef == DamageDefOf.EMP ||
                IndexOfOrdinalIgnoreCase(defName, "EMP") >= 0;
        }

        private static bool IsHeat(DamageDef damageDef, string defName)
        {
            return damageDef == DamageDefOf.Flame ||
                damageDef == DamageDefOf.Burn ||
                IsArmorCategory(damageDef, ArmorCategoryHeat) ||
                IndexOfOrdinalIgnoreCase(defName, "Flame") >= 0 ||
                IndexOfOrdinalIgnoreCase(defName, "Burn") >= 0 ||
                IndexOfOrdinalIgnoreCase(defName, "Fire") >= 0;
        }

        private static bool IsProjectile(DamageDef damageDef)
        {
            return damageDef != null && damageDef.isRanged;
        }

        private static bool IsArmorCategory(DamageDef damageDef, string defName)
        {
            return damageDef != null &&
                damageDef.armorCategory != null &&
                damageDef.armorCategory.defName == defName;
        }

        private static int IndexOfOrdinalIgnoreCase(string value, string pattern)
        {
            return string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern)
                ? -1
                : value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}

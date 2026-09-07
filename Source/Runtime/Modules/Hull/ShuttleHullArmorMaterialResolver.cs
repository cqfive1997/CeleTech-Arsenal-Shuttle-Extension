using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullArmorMaterialResolver
    {
        internal ShuttleHullArmorMaterialProfile Resolve(
            ShuttleHullPlatingModuleDef hullDef,
            ThingDef stuffDef)
        {
            if (hullDef == null)
            {
                return null;
            }

            if (!ShuttleHullArmorStuffUtility.IsValidHullArmorStuff(stuffDef))
            {
                return this.BuildBaseProfile(hullDef, null);
            }

            float hpFactor = this.ClampFinite(
                this.GetStuffStatFactor(stuffDef, "MaxHitPoints", 1f),
                0.25f,
                4f,
                1f);
            float hpOffset = this.ClampFinite(
                this.GetStuffStatOffset(stuffDef, "MaxHitPoints", 0f),
                -100000f,
                100000f,
                0f);
            int hullHitPointsBonus = Mathf.Max(
                0,
                Mathf.RoundToInt((hullDef.hullHitPointsBonus * hpFactor) + hpOffset));

            float sharpArmorPower = this.GetStuffStatValue(stuffDef, "StuffPower_Armor_Sharp", 0f);
            float bluntArmorPower = this.GetStuffStatValue(stuffDef, "StuffPower_Armor_Blunt", 0f);
            float heatArmorPower = this.GetStuffStatValue(stuffDef, "StuffPower_Armor_Heat", 0f);

            float sharpDamageMultiplier = this.ResolveDamageMultiplier(
                hullDef.sharpDamageMultiplier,
                sharpArmorPower);
            float bluntDamageMultiplier = this.ResolveDamageMultiplier(
                hullDef.bluntDamageMultiplier,
                bluntArmorPower);
            float heatDamageMultiplier = this.ResolveDamageMultiplier(
                hullDef.heatDamageMultiplier,
                heatArmorPower);
            float explosionDamageMultiplier = bluntArmorPower > 0f
                ? this.ResolveDamageMultiplier(hullDef.explosionDamageMultiplier, bluntArmorPower)
                : this.ClampFinite(hullDef.explosionDamageMultiplier, 0.25f, 2f, 1f);

            // TODO H6+: If a conductivity/electronics resistance stat is introduced, map it here.
            float empDamageMultiplier = this.ClampFinite(hullDef.empDamageMultiplier, 0.25f, 2f, 1f);

            // TODO H6+: Add a dedicated, tuned material flat-reduction curve after playtesting.
            float flatDamageReduction = this.ClampFinite(
                hullDef.flatDamageReduction,
                0f,
                10000f,
                0f);

            float massFactor = this.ClampFinite(
                this.GetStuffStatFactor(stuffDef, "Mass", 1f),
                0.25f,
                5f,
                1f);

            return new ShuttleHullArmorMaterialProfile(
                stuffDef,
                hullHitPointsBonus,
                sharpDamageMultiplier,
                bluntDamageMultiplier,
                heatDamageMultiplier,
                explosionDamageMultiplier,
                empDamageMultiplier,
                flatDamageReduction,
                massFactor);
        }

        private ShuttleHullArmorMaterialProfile BuildBaseProfile(
            ShuttleHullPlatingModuleDef hullDef,
            ThingDef stuffDef)
        {
            return new ShuttleHullArmorMaterialProfile(
                stuffDef,
                Mathf.Max(0, hullDef.hullHitPointsBonus),
                this.ClampFinite(hullDef.sharpDamageMultiplier, 0.25f, 2f, 1f),
                this.ClampFinite(hullDef.bluntDamageMultiplier, 0.25f, 2f, 1f),
                this.ClampFinite(hullDef.heatDamageMultiplier, 0.25f, 2f, 1f),
                this.ClampFinite(hullDef.explosionDamageMultiplier, 0.25f, 2f, 1f),
                this.ClampFinite(hullDef.empDamageMultiplier, 0.25f, 2f, 1f),
                this.ClampFinite(hullDef.flatDamageReduction, 0f, 10000f, 0f),
                1f);
        }

        private float ResolveDamageMultiplier(float baseMultiplier, float armorPower)
        {
            float sanitizedArmorPower = this.ClampFinite(armorPower, 0f, 100f, 0f);
            if (sanitizedArmorPower <= 0f)
            {
                return this.ClampFinite(baseMultiplier, 0.25f, 2f, 1f);
            }

            float multiplierFactor = 1f / Mathf.Max(0.25f, 1f + sanitizedArmorPower);
            return this.ClampFinite(baseMultiplier * multiplierFactor, 0.25f, 2f, 1f);
        }

        private float GetStuffStatValue(ThingDef stuffDef, string statDefName, float fallback)
        {
            StatDef statDef = this.GetStatDef(statDefName);
            if (stuffDef == null || statDef == null)
            {
                return fallback;
            }

            float value = stuffDef.GetStatValueAbstract(statDef, null);
            return this.IsFinite(value) ? value : fallback;
        }

        private float GetStuffStatFactor(ThingDef stuffDef, string statDefName, float fallback)
        {
            StatDef statDef = this.GetStatDef(statDefName);
            if (stuffDef == null || stuffDef.stuffProps == null || statDef == null)
            {
                return fallback;
            }

            return this.GetStatModifierValue(stuffDef.stuffProps.statFactors, statDef, fallback);
        }

        private float GetStuffStatOffset(ThingDef stuffDef, string statDefName, float fallback)
        {
            StatDef statDef = this.GetStatDef(statDefName);
            if (stuffDef == null || stuffDef.stuffProps == null || statDef == null)
            {
                return fallback;
            }

            return this.GetStatModifierValue(stuffDef.stuffProps.statOffsets, statDef, fallback);
        }

        private float GetStatModifierValue(
            List<StatModifier> modifiers,
            StatDef statDef,
            float fallback)
        {
            if (modifiers == null || statDef == null)
            {
                return fallback;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.stat == statDef)
                {
                    return this.IsFinite(modifier.value) ? modifier.value : fallback;
                }
            }

            return fallback;
        }

        private StatDef GetStatDef(string defName)
        {
            return string.IsNullOrEmpty(defName)
                ? null
                : DefDatabase<StatDef>.GetNamedSilentFail(defName);
        }

        private float ClampFinite(float value, float min, float max, float fallback)
        {
            if (!this.IsFinite(value))
            {
                return fallback;
            }

            return Mathf.Clamp(value, min, max);
        }

        private bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

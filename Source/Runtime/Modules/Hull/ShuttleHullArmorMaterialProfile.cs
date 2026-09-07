using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullArmorMaterialProfile
    {
        internal ShuttleHullArmorMaterialProfile(
            ThingDef stuffDef,
            int hullHitPointsBonus,
            float sharpDamageMultiplier,
            float bluntDamageMultiplier,
            float heatDamageMultiplier,
            float explosionDamageMultiplier,
            float empDamageMultiplier,
            float flatDamageReduction,
            float massFactor)
        {
            this.StuffDef = stuffDef;
            this.StuffDefName = stuffDef != null ? stuffDef.defName : null;
            this.StuffLabel = stuffDef != null ? stuffDef.LabelCap.ToString() : null;
            this.HullHitPointsBonus = hullHitPointsBonus;
            this.SharpDamageMultiplier = sharpDamageMultiplier;
            this.BluntDamageMultiplier = bluntDamageMultiplier;
            this.HeatDamageMultiplier = heatDamageMultiplier;
            this.ExplosionDamageMultiplier = explosionDamageMultiplier;
            this.EmpDamageMultiplier = empDamageMultiplier;
            this.FlatDamageReduction = flatDamageReduction;
            this.MassFactor = massFactor;
        }

        internal ThingDef StuffDef { get; private set; }
        internal string StuffDefName { get; private set; }
        internal string StuffLabel { get; private set; }
        internal int HullHitPointsBonus { get; private set; }
        internal float SharpDamageMultiplier { get; private set; }
        internal float BluntDamageMultiplier { get; private set; }
        internal float HeatDamageMultiplier { get; private set; }
        internal float ExplosionDamageMultiplier { get; private set; }
        internal float EmpDamageMultiplier { get; private set; }
        internal float FlatDamageReduction { get; private set; }
        internal float MassFactor { get; private set; }
    }
}

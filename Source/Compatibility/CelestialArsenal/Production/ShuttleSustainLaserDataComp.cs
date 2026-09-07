using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Authored visual and damage data for the shuttle-owned sustained particle lance.
    /// </summary>
    public sealed class CompProperties_ShuttleSustainLaserData : CompProperties
    {
        public ThingDef LaserLine_MoteDef;
        public ThingDef LaserLine_MoteDef_Core;
        public FleckDef ImpactFleck;
        public float ImpactFleckScale = 1f;
        public float ImpactFleckChancePerDamagePulse = 0.4f;
        public SoundDef SoundDef;
        public float Color_Red = 255f;
        public float Color_Green = 255f;
        public float Color_Blue = 255f;
        public float Color_Alpha = 1f;
        public DamageDef DamageDef;
        public float DamageNum;
        public float DamageArmorPenetration;
        public float DefaultRetargetRadius = 20f;
        public int DefaultRetargetTransitionShots = 6;

        public CompProperties_ShuttleSustainLaserData()
        {
            this.compClass = typeof(CompShuttleSustainLaserData);
        }
    }

    /// <summary>
    /// Read-only runtime access to the hidden gun's authored particle-lance data.
    /// </summary>
    public sealed class CompShuttleSustainLaserData : ThingComp
    {
        public CompProperties_ShuttleSustainLaserData Props
        {
            get { return this.props as CompProperties_ShuttleSustainLaserData; }
        }

        public float QualityNum
        {
            get { return 1f; }
        }
    }
}

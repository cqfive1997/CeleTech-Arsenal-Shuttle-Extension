using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static behavior for one outbound point-defense projectile. Runtime target ownership and
    /// random rolls remain on the projectile instance rather than in Def or profile state.
    /// </summary>
    public sealed class ShuttlePointDefenseProjectileExtension : DefModExtension
    {
        public float interceptionChance;
        public float interceptionRadius = 1f;
        public FleckDef impactFleck;
        public float impactFleckScale = 1f;
        public float homingAcceleration;
    }
}

using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Applies the single direct-damage payload authored for HPJ-L01.
    /// </summary>
    internal sealed class CelestialSustainLaserDamageApplier
    {
        internal bool Apply(
            Thing caster,
            Thing equipmentSource,
            LocalTargetInfo currentTarget,
            Thing target,
            IntVec3 sourceCell,
            CompShuttleSustainLaserData data)
        {
            if (caster == null || equipmentSource == null || target == null || data == null)
            {
                return false;
            }

            CompProperties_ShuttleSustainLaserData props = data.Props;
            if (props == null)
            {
                return false;
            }

            float angle = (currentTarget.Cell - sourceCell).AngleFlat;
            BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(
                caster,
                target,
                currentTarget.Thing,
                equipmentSource.def,
                null,
                null);
            return this.ApplyOne(
                caster,
                equipmentSource,
                currentTarget,
                target,
                props.DamageDef,
                props.DamageNum * data.QualityNum,
                props.DamageArmorPenetration,
                angle,
                log);
        }

        private bool ApplyOne(
            Thing caster,
            Thing equipmentSource,
            LocalTargetInfo currentTarget,
            Thing target,
            DamageDef damageDef,
            float damage,
            float armorPenetration,
            float angle,
            BattleLogEntry_RangedImpact log)
        {
            if (damageDef == null || damage <= 0f)
            {
                return false;
            }

            DamageInfo info = new DamageInfo(
                damageDef,
                damage,
                armorPenetration,
                angle,
                caster,
                null,
                equipmentSource.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                currentTarget.Thing);
            target.TakeDamage(info).AssociateWithLog(log);
            return true;
        }
    }
}

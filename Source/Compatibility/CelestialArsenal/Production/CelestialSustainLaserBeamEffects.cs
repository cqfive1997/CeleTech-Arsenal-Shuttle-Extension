using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Owns only the transient HPJ-L01 beam motes, flecks and Sustainer.
    /// </summary>
    internal sealed class CelestialSustainLaserBeamEffects
    {
        private MoteDualAttached beam;
        private MoteDualAttached core;
        private Sustainer sustainer;

        internal bool IsActive { get; private set; }

        internal void Begin(
            Thing caster,
            IntVec3 sourceCell,
            Vector3 sourceDrawPos,
            Vector3 targetDrawPos,
            CompProperties_ShuttleSustainLaserData props)
        {
            this.End();
            Map map = caster != null ? caster.Map : null;
            if (map == null || props == null || !sourceCell.IsValid)
            {
                return;
            }

            this.IsActive = true;
            if (props.SoundDef != null)
            {
                this.sustainer = props.SoundDef.TrySpawnSustainer(
                    SoundInfo.InMap(caster, MaintenanceType.PerTick));
            }

            TargetInfo source = new TargetInfo(sourceCell, map);
            TargetInfo target = new TargetInfo(targetDrawPos.ToIntVec3(), map);
            if (props.LaserLine_MoteDef != null)
            {
                float oldLayerAlpha = Mathf.Clamp01(props.Color_Alpha) * 0.5f;
                float combinedLayerAlpha = 1f -
                    ((1f - oldLayerAlpha) * (1f - oldLayerAlpha));
                Color color = new Color(
                    props.Color_Red / 255f,
                    props.Color_Green / 255f,
                    props.Color_Blue / 255f,
                    combinedLayerAlpha);
                this.beam = MoteMaker.MakeInteractionOverlay(
                    props.LaserLine_MoteDef,
                    source,
                    target);
                this.beam.instanceColor = color;
            }

            if (props.LaserLine_MoteDef_Core != null)
            {
                Color color = new Color(1f, 1f, 1f, props.Color_Alpha);
                this.core = MoteMaker.MakeInteractionOverlay(
                    props.LaserLine_MoteDef_Core,
                    source,
                    target);
                this.core.instanceColor = color;
            }

            this.UpdateTargets(map, sourceCell, sourceDrawPos, targetDrawPos);
            this.MaintainMotes();
        }

        internal void Update(
            Thing caster,
            IntVec3 sourceCell,
            Vector3 sourceDrawPos,
            Vector3 targetDrawPos,
            bool burstActive)
        {
            Map map = caster != null ? caster.Map : null;
            if (!this.IsActive || map == null)
            {
                return;
            }

            this.UpdateTargets(map, sourceCell, sourceDrawPos, targetDrawPos);
            this.MaintainMotes();
            this.MaintainSustainer();

            if (!burstActive)
            {
                this.End();
            }
        }

        internal void NotifyDamagePulse(
            Thing caster,
            Vector3 targetDrawPos,
            CompProperties_ShuttleSustainLaserData props)
        {
            Map map = caster != null ? caster.Map : null;
            if (map == null || props == null || props.ImpactFleck == null ||
                !Rand.Chance(Mathf.Clamp01(props.ImpactFleckChancePerDamagePulse)))
            {
                return;
            }

            float scale = Mathf.Max(0f, props.ImpactFleckScale) * Rand.Range(0.6f, 1.1f);
            FleckMaker.Static(targetDrawPos, map, props.ImpactFleck, scale);
        }

        internal void End()
        {
            if (this.sustainer != null)
            {
                this.sustainer.End();
            }

            this.sustainer = null;
            this.beam = null;
            this.core = null;
            this.IsActive = false;
        }

        private void UpdateTargets(
            Map map,
            IntVec3 sourceCell,
            Vector3 sourceDrawPos,
            Vector3 targetDrawPos)
        {
            TargetInfo source = new TargetInfo(sourceCell, map);
            IntVec3 targetCell = targetDrawPos.ToIntVec3();
            TargetInfo target = new TargetInfo(targetCell, map);
            Vector3 sourceOffset = sourceDrawPos - sourceCell.ToVector3Shifted();
            Vector3 targetOffset = targetDrawPos - targetCell.ToVector3Shifted();

            if (this.beam != null)
            {
                this.beam.UpdateTargets(source, target, sourceOffset, targetOffset);
            }

            if (this.core != null)
            {
                this.core.UpdateTargets(source, target, sourceOffset, targetOffset);
            }
        }

        private void MaintainMotes()
        {
            if (this.beam != null)
            {
                this.beam.Maintain();
            }

            if (this.core != null)
            {
                this.core.Maintain();
            }
        }

        private void MaintainSustainer()
        {
            if (this.sustainer == null)
            {
                return;
            }

            if (this.sustainer.Ended)
            {
                this.sustainer = null;
                return;
            }

            this.sustainer.Maintain();
        }
    }
}

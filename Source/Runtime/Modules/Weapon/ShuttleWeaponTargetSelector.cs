using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Enumerates and scores automatic hostile Thing targets. Engagement legality is delegated
    /// to the shared evaluator; this class does not mutate state or start firing.
    /// </summary>
    internal sealed class ShuttleWeaponTargetSelector
    {
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponEngagementEvaluator engagementEvaluator;
        private readonly ShuttleWeaponTargetScorer targetScorer;
        private readonly ShuttleWeaponPointDefenseTargetSelector pointDefenseTargetSelector;
        private readonly ShuttleWeaponAutomaticTargetScanCache automaticTargetScanCache;
        private readonly ShuttleWeaponAttackTargetCandidateSource targetCandidateSource;

        internal ShuttleWeaponTargetSelector(
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponEngagementEvaluator engagementEvaluator,
            ShuttleWeaponTargetScorer targetScorer,
            ShuttleWeaponPointDefenseTargetSelector pointDefenseTargetSelector,
            ShuttleWeaponAutomaticTargetScanCache automaticTargetScanCache = null,
            ShuttleWeaponAttackTargetCandidateSource targetCandidateSource = null)
        {
            this.fireControlPolicy = fireControlPolicy;
            this.engagementEvaluator = engagementEvaluator;
            this.targetScorer = targetScorer;
            this.pointDefenseTargetSelector = pointDefenseTargetSelector;
            this.automaticTargetScanCache = automaticTargetScanCache;
            this.targetCandidateSource = targetCandidateSource ??
                ShuttleWeaponAttackTargetCandidateSource.Shared;
        }

        internal LocalTargetInfo FindNewTarget(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (map == null ||
                !this.fireControlPolicy.CanUseAutomaticFireControl(context, weaponDef, state) ||
                state.TargetPriority == ShuttleWeaponTargetPriority.ForcedTargetOnly)
            {
                return LocalTargetInfo.Invalid;
            }

            if (state.FireControlMode == ShuttleWeaponFireControlMode.PointDefense)
            {
                return this.pointDefenseTargetSelector != null
                    ? this.pointDefenseTargetSelector.FindNewTarget(
                        context,
                        weaponDef,
                        state,
                        attackVerb)
                    : LocalTargetInfo.Invalid;
            }

            bool profileScan = this.automaticTargetScanCache != null &&
                ShuttleWeaponRuntimeProfiler.Enabled;
            long cacheLookupStart = ShuttleWeaponRuntimeProfiler.StartSection(profileScan);
            LocalTargetInfo cachedTarget;
            if (this.automaticTargetScanCache != null &&
                this.automaticTargetScanCache.TryGet(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    out cachedTarget))
            {
                ShuttleWeaponRuntimeProfiler.Record(
                    profileScan,
                    ShuttleWeaponRuntimeProfiler.SectionAutomaticTargetCacheHit,
                    cacheLookupStart);
                return cachedTarget;
            }

            long scanStart = ShuttleWeaponRuntimeProfiler.StartSection(profileScan);
            Thing bestTarget = null;
            IReadOnlyList<Thing> candidates = this.targetCandidateSource.GetCandidates(
                context,
                attackVerb);
            for (int i = 0; candidates != null && i < candidates.Count; i++)
            {
                this.TryConsider(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    host,
                    candidates[i],
                    ref bestTarget);
            }

            LocalTargetInfo selectedTarget = bestTarget != null
                ? new LocalTargetInfo(bestTarget)
                : LocalTargetInfo.Invalid;
            ShuttleWeaponRuntimeProfiler.Record(
                profileScan,
                ShuttleWeaponRuntimeProfiler.SectionAutomaticTargetScan,
                scanStart);
            if (this.automaticTargetScanCache != null)
            {
                this.automaticTargetScanCache.Store(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    selectedTarget);
            }

            return selectedTarget;
        }

        private bool TryConsider(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            ThingWithComps host,
            Thing thing,
            ref Thing bestTarget)
        {
            if (thing == null || thing.Destroyed || thing == host)
            {
                return false;
            }

            LocalTargetInfo target = new LocalTargetInfo(thing);
            if (!this.engagementEvaluator.EvaluateAutomaticTarget(
                context,
                weaponDef,
                state,
                attackVerb,
                target).IsAllowed)
            {
                return false;
            }

            if (!this.targetScorer.IsBetterAutoTarget(
                thing,
                bestTarget,
                host,
                state.TargetPriority))
            {
                return false;
            }

            bestTarget = thing;
            return true;
        }
    }
}

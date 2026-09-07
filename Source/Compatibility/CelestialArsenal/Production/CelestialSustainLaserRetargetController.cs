using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Owns only Celestial's sustained-beam retarget interpolation and candidate search.
    /// </summary>
    internal sealed class CelestialSustainLaserRetargetController
    {
        private bool retargeting;
        private int transitionShotsTotal;
        private int transitionShotsDone;
        private Vector3 fromPosition;
        private Vector3 toPosition;

        internal Vector3 ResolveBeamEnd(
            LocalTargetInfo currentTarget,
            Vector3 fallbackPosition)
        {
            if (this.retargeting)
            {
                float progress = this.transitionShotsTotal <= 0
                    ? 1f
                    : (float)this.transitionShotsDone / this.transitionShotsTotal;
                return Vector3.Lerp(
                    this.fromPosition,
                    this.toPosition,
                    Mathf.Clamp01(progress));
            }

            return currentTarget.IsValid
                ? currentTarget.CenterVector3
                : fallbackPosition;
        }

        internal void RefreshMovingEndpoint(LocalTargetInfo currentTarget)
        {
            if (this.retargeting && currentTarget.HasThing &&
                currentTarget.Thing != null && !currentTarget.Thing.Destroyed)
            {
                this.toPosition = currentTarget.Thing.DrawPos;
            }
        }

        internal bool AdvanceTransitionShot()
        {
            if (!this.retargeting)
            {
                return false;
            }

            this.transitionShotsDone++;
            if (this.transitionShotsDone < this.transitionShotsTotal)
            {
                return true;
            }

            this.retargeting = false;
            return false;
        }

        internal bool TryBeginRetarget(
            Thing caster,
            LocalTargetInfo currentTarget,
            Vector3 currentBeamEnd,
            CompProperties_ShuttleSustainLaserData props,
            int burstShotsLeft,
            Func<LocalTargetInfo, bool> canTarget,
            out LocalTargetInfo nextTarget)
        {
            nextTarget = LocalTargetInfo.Invalid;
            if (caster == null || caster.Map == null || props == null ||
                burstShotsLeft <= 0 || !IsDeadOrInvalid(currentTarget, caster.Map))
            {
                return false;
            }

            Thing next = this.FindCandidate(
                caster,
                currentBeamEnd.ToIntVec3(),
                props.DefaultRetargetRadius,
                canTarget);
            if (next == null)
            {
                return false;
            }

            nextTarget = new LocalTargetInfo(next);
            this.retargeting = true;
            this.fromPosition = currentBeamEnd;
            this.toPosition = next.DrawPos;
            this.transitionShotsDone = 0;
            this.transitionShotsTotal = Mathf.Clamp(
                props.DefaultRetargetTransitionShots,
                1,
                Mathf.Max(1, burstShotsLeft));
            return true;
        }

        internal void Reset()
        {
            this.retargeting = false;
            this.transitionShotsDone = 0;
            this.transitionShotsTotal = 0;
            this.fromPosition = Vector3.zero;
            this.toPosition = Vector3.zero;
        }

        private Thing FindCandidate(
            Thing caster,
            IntVec3 root,
            float radius,
            Func<LocalTargetInfo, bool> canTarget)
        {
            return GenClosest.ClosestThingReachable(
                root,
                caster.Map,
                ThingRequest.ForGroup(ThingRequestGroup.AttackTarget),
                PathEndMode.Touch,
                this.ResolveTraverseParms(caster),
                radius,
                delegate(Thing thing)
                {
                    if (thing == null || thing == caster || thing.Destroyed ||
                        !thing.Spawned || thing.Map != caster.Map)
                    {
                        return false;
                    }

                    Pawn pawn = thing as Pawn;
                    if ((pawn != null && pawn.Dead) || !caster.HostileTo(thing))
                    {
                        return false;
                    }

                    return canTarget != null && canTarget(new LocalTargetInfo(thing));
                });
        }

        private TraverseParms ResolveTraverseParms(Thing caster)
        {
            Pawn pawn = caster as Pawn;
            return pawn != null
                ? TraverseParms.For(pawn, Danger.Deadly)
                : TraverseParms.For(TraverseMode.NoPassClosedDoors);
        }

        private static bool IsDeadOrInvalid(LocalTargetInfo target, Map map)
        {
            if (!target.IsValid)
            {
                return true;
            }

            Thing thing = target.Thing;
            if (thing == null)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            return thing.Destroyed || !thing.Spawned || thing.Map != map ||
                (pawn != null && pawn.Dead);
        }
    }
}

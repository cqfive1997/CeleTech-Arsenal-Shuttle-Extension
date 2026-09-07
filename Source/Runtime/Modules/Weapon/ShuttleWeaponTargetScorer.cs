using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponTargetScorer
    {
        internal bool IsBetterAutoTarget(
            Pawn candidate,
            Pawn currentBest,
            ThingWithComps host,
            ShuttleWeaponTargetPriority priority)
        {
            return this.IsBetterAutoTarget((Thing)candidate, currentBest, host, priority);
        }

        internal bool IsBetterAutoTarget(
            Thing candidate,
            Thing currentBest,
            ThingWithComps host,
            ShuttleWeaponTargetPriority priority)
        {
            if (candidate == null || host == null)
            {
                return false;
            }

            if (currentBest == null)
            {
                return true;
            }

            switch (priority)
            {
                case ShuttleWeaponTargetPriority.RaidersFirst:
                    return this.CompareBucketThenDistance(
                        this.IsHumanlikePawn(candidate),
                        this.IsHumanlikePawn(currentBest),
                        candidate,
                        currentBest,
                        host);
                case ShuttleWeaponTargetPriority.MechanoidsFirst:
                    return this.CompareBucketThenDistance(
                        this.IsMechanoidPawn(candidate),
                        this.IsMechanoidPawn(currentBest),
                        candidate,
                        currentBest,
                        host);
                case ShuttleWeaponTargetPriority.ManhuntersFirst:
                    return this.CompareBucketThenDistance(
                        this.IsManhunterPawn(candidate),
                        this.IsManhunterPawn(currentBest),
                        candidate,
                        currentBest,
                        host);
                case ShuttleWeaponTargetPriority.HighThreatFirst:
                    return this.CompareThreatThenDistance(candidate, currentBest, host);
                default:
                    return this.DistanceSquaredToHost(candidate, host) <
                        this.DistanceSquaredToHost(currentBest, host);
            }
        }

        internal bool CompareBucketThenDistance(
            bool candidateInPriorityBucket,
            bool bestInPriorityBucket,
            Thing candidate,
            Thing currentBest,
            ThingWithComps host)
        {
            if (candidateInPriorityBucket != bestInPriorityBucket)
            {
                return candidateInPriorityBucket;
            }

            return this.DistanceSquaredToHost(candidate, host) <
                this.DistanceSquaredToHost(currentBest, host);
        }

        internal bool CompareThreatThenDistance(
            Thing candidate,
            Thing currentBest,
            ThingWithComps host)
        {
            float candidateScore = this.GetHighThreatScore(candidate, host);
            float bestScore = this.GetHighThreatScore(currentBest, host);
            if (Math.Abs(candidateScore - bestScore) > 0.01f)
            {
                return candidateScore > bestScore;
            }

            return this.DistanceSquaredToHost(candidate, host) <
                this.DistanceSquaredToHost(currentBest, host);
        }

        internal float GetHighThreatScore(Thing thing, ThingWithComps host)
        {
            if (thing == null || host == null)
            {
                return 0f;
            }

            float score = 0f;
            Pawn pawn = thing as Pawn;
            if (this.IsMechanoidPawn(pawn))
            {
                score += 50f;
            }

            if (this.IsHumanlikePawn(pawn))
            {
                score += 20f;
            }

            if (this.HasRangedWeapon(pawn))
            {
                score += 30f;
            }

            if (thing is Building_Turret ||
                (pawn == null && thing is IAttackTarget))
            {
                score += 80f;
            }

            float distanceSquared = this.DistanceSquaredToHost(thing, host);
            score += Math.Max(0f, 100f - distanceSquared * 0.05f);
            return score;
        }

        internal float DistanceSquaredToHost(Thing thing, ThingWithComps host)
        {
            if (thing == null || host == null)
            {
                return float.MaxValue;
            }

            return (thing.Position - host.Position).LengthHorizontalSquared;
        }

        internal bool IsHumanlikePawn(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            return pawn != null &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike;
        }

        internal bool IsMechanoidPawn(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            return pawn != null &&
                pawn.RaceProps != null &&
                pawn.RaceProps.IsMechanoid;
        }

        internal bool IsManhunterPawn(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            return pawn != null &&
                pawn.InMentalState &&
                pawn.MentalStateDef != null &&
                !string.IsNullOrEmpty(pawn.MentalStateDef.defName) &&
                pawn.MentalStateDef.defName.IndexOf("Manhunter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal bool HasRangedWeapon(Pawn pawn)
        {
            ThingWithComps primary = pawn != null && pawn.equipment != null
                ? pawn.equipment.Primary
                : null;
            return primary != null &&
                primary.def != null &&
                primary.def.IsRangedWeapon;
        }
    }
}

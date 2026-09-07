using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Copies RimWorld's transient hostile-target list into one short-lived weak host snapshot.
    /// It is populated only when an authored weapon scan is already due; it never ticks itself.
    /// </summary>
    internal sealed class ShuttleWeaponAttackTargetCandidateSource
    {
        private const int CandidateFreshnessTicks = 15;

        private static readonly ShuttleWeaponAttackTargetCandidateSource SharedSource =
            new ShuttleWeaponAttackTargetCandidateSource();

        private readonly ConditionalWeakTable<ThingWithComps, HostEntry> entries =
            new ConditionalWeakTable<ThingWithComps, HostEntry>();

        internal static ShuttleWeaponAttackTargetCandidateSource Shared
        {
            get { return SharedSource; }
        }

        internal IReadOnlyList<Thing> GetCandidates(
            ShuttleModuleRuntimeContext context,
            Verb attackVerb)
        {
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (host == null || map == null)
            {
                return HostEntry.EmptyCandidates;
            }

            int ticksGame = context != null ? context.TicksGame : -1;
            int epoch = ticksGame >= 0
                ? ticksGame / CandidateFreshnessTicks
                : int.MinValue;
            int attackTargetStateHash = map.listerThings != null
                ? map.listerThings.StateHashOfGroup(ThingRequestGroup.AttackTarget)
                : 0;
            HostEntry entry = this.entries.GetOrCreateValue(host);
            if (!entry.Matches(map, host.Faction, epoch, attackTargetStateHash))
            {
                entry.Refresh(
                    host,
                    map,
                    attackVerb,
                    epoch,
                    attackTargetStateHash);
            }

            return entry.Candidates;
        }

        private sealed class HostEntry
        {
            internal static readonly IReadOnlyList<Thing> EmptyCandidates =
                new List<Thing>();

            private readonly List<Thing> candidates = new List<Thing>();
            private readonly AttackTargetSearcherAdapter searcher =
                new AttackTargetSearcherAdapter();
            private Map map;
            private Faction faction;
            private int epoch = int.MinValue;
            private int attackTargetStateHash;
            private bool initialized;

            internal IReadOnlyList<Thing> Candidates
            {
                get { return this.candidates; }
            }

            internal bool Matches(
                Map candidateMap,
                Faction candidateFaction,
                int candidateEpoch,
                int candidateStateHash)
            {
                return this.initialized &&
                    object.ReferenceEquals(this.map, candidateMap) &&
                    object.ReferenceEquals(this.faction, candidateFaction) &&
                    this.epoch == candidateEpoch &&
                    this.attackTargetStateHash == candidateStateHash;
            }

            internal void Refresh(
                ThingWithComps host,
                Map candidateMap,
                Verb attackVerb,
                int candidateEpoch,
                int candidateStateHash)
            {
                this.candidates.Clear();
                this.searcher.Bind(host, attackVerb);

                List<IAttackTarget> targets = candidateMap.attackTargetsCache != null
                    ? candidateMap.attackTargetsCache.GetPotentialTargetsFor(this.searcher)
                    : null;
                if (targets != null)
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        IAttackTarget target = targets[i];
                        Thing thing = target != null ? target.Thing : null;
                        if (thing != null)
                        {
                            this.candidates.Add(thing);
                        }
                    }
                }
                else
                {
                    this.AddListerFallback(candidateMap);
                }

                this.map = candidateMap;
                this.faction = host.Faction;
                this.epoch = candidateEpoch;
                this.attackTargetStateHash = candidateStateHash;
                this.initialized = true;
            }

            private void AddListerFallback(Map candidateMap)
            {
                IReadOnlyList<Thing> targets = candidateMap != null &&
                    candidateMap.listerThings != null
                        ? candidateMap.listerThings.ThingsInGroup(ThingRequestGroup.AttackTarget)
                        : null;
                for (int i = 0; targets != null && i < targets.Count; i++)
                {
                    Thing thing = targets[i];
                    if (thing != null)
                    {
                        this.candidates.Add(thing);
                    }
                }
            }
        }

        private sealed class AttackTargetSearcherAdapter : IAttackTargetSearcher
        {
            private ThingWithComps host;
            private Verb attackVerb;

            public Thing Thing { get { return this.host; } }

            public Verb CurrentEffectiveVerb { get { return this.attackVerb; } }

            public LocalTargetInfo LastAttackedTarget
            {
                get { return LocalTargetInfo.Invalid; }
            }

            public int LastAttackTargetTick { get { return -1; } }

            internal void Bind(ThingWithComps candidateHost, Verb candidateVerb)
            {
                this.host = candidateHost;
                this.attackVerb = candidateVerb;
            }
        }
    }
}

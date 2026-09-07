using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal enum ShuttleAssemblyWorkEffectKind
    {
        Install = 0,
        Remove = 1,
        Repair = 2
    }

    internal static class ShuttleAssemblyWorkEffectUtility
    {
        private const int EffecterStaleTicks = 180;
        private const int CleanupIntervalTicks = 240;

        private static readonly Dictionary<WorkEffectKey, ActiveWorkEffecter> ActiveEffecters =
            new Dictionary<WorkEffectKey, ActiveWorkEffecter>();

        private static readonly List<WorkEffectKey> ExpiredEffecterKeys = new List<WorkEffectKey>();

        private static int lastCleanupTick = -1;

        internal static void TryPlayWorkTickEffect(
            ThingWithComps host,
            Pawn worker,
            ShuttleAssemblyWorkEffectKind kind,
            int ticksGame,
            int intervalTicks = 60)
        {
            if (host == null || !host.Spawned || host.Map == null)
            {
                return;
            }

            int ticks = ticksGame >= 0
                ? ticksGame
                : (Find.TickManager != null ? Find.TickManager.TicksGame : -1);
            CleanupStaleEffecters(ticks);
            TryMaintainVanillaWorkEffecter(host, worker, kind, ticks);

            int interval = Mathf.Max(1, intervalTicks);
            if (ticks >= 0)
            {
                int seed = host.thingIDNumber;
                if (worker != null)
                {
                    seed ^= worker.thingIDNumber;
                }

                int offset = Mathf.Abs(seed % interval);
                if ((ticks + offset) % interval != 0)
                {
                    return;
                }
            }

            Vector3 drawPos = host.DrawPos;
            if (worker != null && worker.Spawned && worker.Map == host.Map)
            {
                drawPos = (drawPos + worker.DrawPos) * 0.5f;
            }

            drawPos += new Vector3(
                Rand.Range(-0.35f, 0.35f),
                0f,
                Rand.Range(-0.35f, 0.35f));

            FleckMaker.ThrowMicroSparks(drawPos, host.Map);
        }

        internal static void ClearAllEffecters()
        {
            foreach (KeyValuePair<WorkEffectKey, ActiveWorkEffecter> pair in ActiveEffecters)
            {
                ActiveWorkEffecter active = pair.Value;
                if (active != null && active.Effecter != null)
                {
                    active.Effecter.Cleanup();
                }
            }

            ActiveEffecters.Clear();
            ExpiredEffecterKeys.Clear();
            lastCleanupTick = -1;
        }

        private static void TryMaintainVanillaWorkEffecter(
            ThingWithComps host,
            Pawn worker,
            ShuttleAssemblyWorkEffectKind kind,
            int ticksGame)
        {
            EffecterDef effecterDef = ResolveEffecterDef(host, kind);
            if (effecterDef == null)
            {
                return;
            }

            WorkEffectKey key = new WorkEffectKey(
                host.thingIDNumber,
                worker != null ? worker.thingIDNumber : 0,
                kind);
            TargetInfo source = worker != null && worker.Spawned && worker.Map == host.Map
                ? new TargetInfo(worker)
                : new TargetInfo(host);
            TargetInfo target = new TargetInfo(host);

            ActiveWorkEffecter active;
            if (!ActiveEffecters.TryGetValue(key, out active) ||
                active == null ||
                active.Effecter == null ||
                active.EffecterDef != effecterDef)
            {
                if (active != null && active.Effecter != null)
                {
                    active.Effecter.Cleanup();
                }

                active = new ActiveWorkEffecter(effecterDef.Spawn(), effecterDef);
                ActiveEffecters[key] = active;
                active.Effecter.Trigger(source, target, -1);
            }

            active.LastTick = ticksGame;
            active.Effecter.EffectTick(source, target);
        }

        private static EffecterDef ResolveEffecterDef(
            ThingWithComps host,
            ShuttleAssemblyWorkEffectKind kind)
        {
            if (host == null || host.def == null)
            {
                return null;
            }

            if (kind == ShuttleAssemblyWorkEffectKind.Repair)
            {
                return host.def.repairEffect ??
                    DefDatabase<EffecterDef>.GetNamedSilentFail("Repair") ??
                    EffecterDefOf.ConstructMetal;
            }

            return host.def.constructEffect ?? EffecterDefOf.ConstructMetal;
        }

        private static void CleanupStaleEffecters(int ticksGame)
        {
            if (ticksGame < 0 ||
                lastCleanupTick >= 0 &&
                ticksGame - lastCleanupTick < CleanupIntervalTicks)
            {
                return;
            }

            lastCleanupTick = ticksGame;
            ExpiredEffecterKeys.Clear();
            foreach (KeyValuePair<WorkEffectKey, ActiveWorkEffecter> pair in ActiveEffecters)
            {
                ActiveWorkEffecter active = pair.Value;
                if (active == null ||
                    active.Effecter == null ||
                    active.LastTick < 0 ||
                    ticksGame - active.LastTick >= EffecterStaleTicks)
                {
                    ExpiredEffecterKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < ExpiredEffecterKeys.Count; i++)
            {
                WorkEffectKey key = ExpiredEffecterKeys[i];
                ActiveWorkEffecter active;
                if (ActiveEffecters.TryGetValue(key, out active) &&
                    active != null &&
                    active.Effecter != null)
                {
                    active.Effecter.Cleanup();
                }

                ActiveEffecters.Remove(key);
            }

            ExpiredEffecterKeys.Clear();
        }

        private sealed class ActiveWorkEffecter
        {
            internal readonly Effecter Effecter;
            internal readonly EffecterDef EffecterDef;
            internal int LastTick = -1;

            internal ActiveWorkEffecter(Effecter effecter, EffecterDef effecterDef)
            {
                this.Effecter = effecter;
                this.EffecterDef = effecterDef;
            }
        }

        private struct WorkEffectKey : IEquatable<WorkEffectKey>
        {
            private readonly int hostID;
            private readonly int workerID;
            private readonly ShuttleAssemblyWorkEffectKind kind;

            internal WorkEffectKey(int hostID, int workerID, ShuttleAssemblyWorkEffectKind kind)
            {
                this.hostID = hostID;
                this.workerID = workerID;
                this.kind = kind;
            }

            public bool Equals(WorkEffectKey other)
            {
                return this.hostID == other.hostID &&
                    this.workerID == other.workerID &&
                    this.kind == other.kind;
            }

            public override bool Equals(object obj)
            {
                return obj is WorkEffectKey && this.Equals((WorkEffectKey)obj);
            }

            public override int GetHashCode()
            {
                int hash = this.hostID;
                hash = Gen.HashCombineInt(hash, this.workerID);
                hash = Gen.HashCombineInt(hash, (int)this.kind);
                return hash;
            }
        }
    }
}

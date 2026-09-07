using System;
using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Reuses one equivalent automatic-target query through the current authored scan epoch. Host
    /// ownership is weak and cached candidates remain subject to the normal per-weapon validation.
    /// </summary>
    internal sealed class ShuttleWeaponAutomaticTargetScanCache
    {
        private readonly ConditionalWeakTable<ThingWithComps, HostEntry> entries =
            new ConditionalWeakTable<ThingWithComps, HostEntry>();

        internal bool TryGet(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            out LocalTargetInfo target)
        {
            target = LocalTargetInfo.Invalid;
            Query query;
            if (!TryBuildQuery(context, weaponDef, state, attackVerb, out query))
            {
                return false;
            }

            HostEntry entry;
            if (!this.entries.TryGetValue(context.Host, out entry) ||
                !entry.Matches(query))
            {
                return false;
            }

            target = entry.Target;
            return true;
        }

        internal void Store(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            Query query;
            if (!TryBuildQuery(context, weaponDef, state, attackVerb, out query))
            {
                return;
            }

            this.entries.GetOrCreateValue(context.Host).Set(query, target);
        }

        private static bool TryBuildQuery(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            out Query query)
        {
            query = default(Query);
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (host == null || map == null || weaponDef == null ||
                state == null || attackVerb == null || context.TicksGame < 0)
            {
                return false;
            }

            int interval = Math.Max(1, weaponDef.scanIntervalTicks);
            query.Map = map;
            query.HostPosition = host.Position;
            query.WeaponDef = weaponDef;
            query.VerbProperties = attackVerb.verbProps;
            query.VerbType = attackVerb.GetType();
            query.FireControlMode = state.FireControlMode;
            query.TargetPriority = state.TargetPriority;
            query.FireControlLinked = state.FireControlLinked;
            query.AutoFireEnabled = state.AutoFireEnabled;
            query.ProfileRevision = context.Profile != null
                ? context.Profile.Revision
                : -1;
            query.ScanInterval = interval;
            query.ScanEpoch = context.TicksGame / interval;
            return true;
        }

        private struct Query
        {
            internal Map Map;
            internal IntVec3 HostPosition;
            internal ShuttleWeaponModuleDef WeaponDef;
            internal VerbProperties VerbProperties;
            internal Type VerbType;
            internal ShuttleWeaponFireControlMode FireControlMode;
            internal ShuttleWeaponTargetPriority TargetPriority;
            internal bool FireControlLinked;
            internal bool AutoFireEnabled;
            internal int ProfileRevision;
            internal int ScanInterval;
            internal int ScanEpoch;
        }

        private sealed class HostEntry
        {
            private bool initialized;
            private Query query;

            internal LocalTargetInfo Target { get; private set; }

            internal bool Matches(Query candidate)
            {
                return this.initialized &&
                    object.ReferenceEquals(this.query.Map, candidate.Map) &&
                    this.query.HostPosition == candidate.HostPosition &&
                    object.ReferenceEquals(this.query.WeaponDef, candidate.WeaponDef) &&
                    object.ReferenceEquals(
                        this.query.VerbProperties,
                        candidate.VerbProperties) &&
                    this.query.VerbType == candidate.VerbType &&
                    this.query.FireControlMode == candidate.FireControlMode &&
                    this.query.TargetPriority == candidate.TargetPriority &&
                    this.query.FireControlLinked == candidate.FireControlLinked &&
                    this.query.AutoFireEnabled == candidate.AutoFireEnabled &&
                    this.query.ProfileRevision == candidate.ProfileRevision &&
                    this.query.ScanInterval == candidate.ScanInterval &&
                    this.query.ScanEpoch == candidate.ScanEpoch;
            }

            internal void Set(Query query, LocalTargetInfo target)
            {
                this.query = query;
                this.Target = target;
                this.initialized = true;
            }
        }
    }
}

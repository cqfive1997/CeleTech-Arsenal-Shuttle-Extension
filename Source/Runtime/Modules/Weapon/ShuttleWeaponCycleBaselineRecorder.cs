using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.Weapons;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Dev-only observation aid for the pre-Native weapon cadence gate. It owns no gameplay
    /// state and is inert unless detailed performance breakdown logging is enabled.
    /// </summary>
    internal static class ShuttleWeaponCycleBaselineRecorder
    {
        private static readonly ConditionalWeakTable<ShuttleWeaponRuntimeState, CycleSample> Samples =
            new ConditionalWeakTable<ShuttleWeaponRuntimeState, CycleSample>();

        internal static void RecordTargetAcquired(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            if (!IsEnabled(context, weaponDef, state))
            {
                return;
            }

            CycleSample sample = Samples.GetOrCreateValue(state);
            sample.ModuleInstanceID = context.ModuleInstanceID;
            sample.WeaponDefName = weaponDef.defName;
            sample.TargetAcquiredTick = context.TicksGame;
        }

        internal static void RecordBurstStart(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            bool started,
            int loadedBefore,
            int loadedAfter)
        {
            if (!IsEnabled(context, weaponDef, state))
            {
                return;
            }

            CycleSample sample = Samples.GetOrCreateValue(state);
            int startTick = context.TicksGame;
            int acquisitionDelay = DeltaOrUnknown(startTick, sample.TargetAcquiredTick);
            int readyDelay = DeltaOrUnknown(startTick, sample.LastCompletedTick);
            int expectedBurstCount = GetEffectiveBurstCount(attackVerb);
            int firstObservedShotTick = GetLastShuttleShotTick(attackVerb);

            sample.ModuleInstanceID = context.ModuleInstanceID;
            sample.WeaponDefName = weaponDef.defName;
            sample.BurstStartedTick = started ? startTick : -1;

            Log.Message(
                "[CeleTech Shuttle] WeaponBaseline start" +
                " module=" + Safe(sample.ModuleInstanceID) +
                " weapon=" + Safe(sample.WeaponDefName) +
                " tick=" + startTick +
                " started=" + started +
                " acquiredTick=" + sample.TargetAcquiredTick +
                " acquisitionToStartTicks=" + acquisitionDelay +
                " previousCompleteToStartTicks=" + readyDelay +
                " firstObservedShotTick=" + firstObservedShotTick +
                " expectedBurstCount=" + expectedBurstCount +
                " loaded=" + loadedBefore + "->" + loadedAfter);
        }

        internal static void RecordCastComplete(
            ShuttleWeaponRuntimeState state,
            ShuttleWeaponModuleDef weaponDef,
            Verb attackVerb)
        {
            if (!ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns ||
                state == null ||
                weaponDef == null)
            {
                return;
            }

            CycleSample sample = Samples.GetOrCreateValue(state);
            int completedTick = CurrentGameTick();
            int burstDuration = DeltaOrUnknown(completedTick, sample.BurstStartedTick);
            int loadedAmmo = state.AmmoForRuntimeOnly.LoadedAmmoCount;
            int lastObservedShotTick = GetLastShuttleShotTick(attackVerb);
            int observedShotCount = GetObservedShuttleShotCount(attackVerb);

            sample.WeaponDefName = weaponDef.defName;
            sample.LastCompletedTick = completedTick;

            Log.Message(
                "[CeleTech Shuttle] WeaponBaseline complete" +
                " module=" + Safe(sample.ModuleInstanceID) +
                " weapon=" + Safe(sample.WeaponDefName) +
                " tick=" + completedTick +
                " burstDurationTicks=" + burstDuration +
                " lastObservedShotTick=" + lastObservedShotTick +
                " observedShotCount=" + observedShotCount +
                " cooldownTicks=" + state.GetCooldownTicksForRuntimeOnly() +
                " loaded=" + loadedAmmo);
        }

        private static bool IsEnabled(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            return ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns &&
                context != null &&
                weaponDef != null &&
                state != null;
        }

        private static int CurrentGameTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
        }

        private static int GetLastShuttleShotTick(Verb attackVerb)
        {
            Verb_ShuttleMuzzleShoot muzzleVerb = attackVerb as Verb_ShuttleMuzzleShoot;
            return muzzleVerb != null ? muzzleVerb.LastShuttleShotTick : -1;
        }

        private static int GetEffectiveBurstCount(Verb attackVerb)
        {
            Verb_ShuttleMuzzleShoot muzzleVerb = attackVerb as Verb_ShuttleMuzzleShoot;
            if (muzzleVerb != null)
            {
                return muzzleVerb.EffectiveShotsPerBurstForDiagnostics;
            }

            return attackVerb != null ? attackVerb.BurstShotCount : 0;
        }

        private static int GetObservedShuttleShotCount(Verb attackVerb)
        {
            Verb_ShuttleMuzzleShoot muzzleVerb = attackVerb as Verb_ShuttleMuzzleShoot;
            return muzzleVerb != null ? muzzleVerb.ShotsFiredInCurrentBurst : -1;
        }

        private static int DeltaOrUnknown(int laterTick, int earlierTick)
        {
            return laterTick >= 0 && earlierTick >= 0
                ? laterTick - earlierTick
                : -1;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<unknown>" : value;
        }

        private sealed class CycleSample
        {
            public CycleSample()
            {
            }

            internal string ModuleInstanceID;
            internal string WeaponDefName;
            internal int TargetAcquiredTick = -1;
            internal int BurstStartedTick = -1;
            internal int LastCompletedTick = -1;
        }
    }
}

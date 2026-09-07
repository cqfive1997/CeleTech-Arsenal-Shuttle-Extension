using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal sealed class CelestialSustainLaserBurstStarter : IShuttleWeaponBurstStarter
    {
        private const int FailedStartCooldownTicks = 15;

        private readonly ShuttleWeaponTargetValidator targetValidator;
        private readonly CelestialSustainLaserFireDriver fireDriver;

        internal CelestialSustainLaserBurstStarter(
            ShuttleWeaponTargetValidator targetValidator,
            CelestialSustainLaserFireDriver fireDriver)
        {
            this.targetValidator = targetValidator;
            this.fireDriver = fireDriver;
        }

        public void BeginBurst(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            if (context == null || weaponDef == null || state == null ||
                attackVerb == null || this.targetValidator == null ||
                this.fireDriver == null)
            {
                return;
            }

            LocalTargetInfo target = state.GetCurrentTargetForRuntimeOnly();
            Verb_ShuttleCelestialSustainLaser laser =
                attackVerb as Verb_ShuttleCelestialSustainLaser;
            if (!target.IsValid || !context.InternalBusPowered ||
                laser == null || !attackVerb.Available() ||
                !this.targetValidator.IsValidTargetNow(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    target))
            {
                state.ResetCurrentTargetForRuntimeOnly();
                return;
            }

            string failureReason;
            bool started = this.fireDriver.TryStartReservedCast(
                context,
                target,
                out failureReason);
            ShuttleWeaponCycleBaselineRecorder.RecordBurstStart(
                context,
                weaponDef,
                state,
                attackVerb,
                started,
                0,
                0);
            if (started)
            {
                return;
            }

            state.ResetCurrentTargetForRuntimeOnly();
            state.SetCooldownTicksForRuntimeOnly(FailedStartCooldownTicks);
        }
    }
}

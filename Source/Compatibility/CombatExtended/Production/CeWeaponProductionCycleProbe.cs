using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Creates an isolated firing-cycle state around one transient CE gun. Installed ammo and
    /// runtime cycle state are observed but never replaced or advanced.
    /// </summary>
    internal sealed class CeWeaponProductionCycleProbe
    {
        private readonly CeWeaponMagazineAuthorityTransfer transfer =
            new CeWeaponMagazineAuthorityTransfer();

        internal CeWeaponProductionCycleProbeSession Create(
            ThingWithComps host,
            CompModularShuttleCore core,
            ShuttleModule module,
            LocalTargetInfo target)
        {
            ShuttleWeaponModuleDef weaponDef = module != null
                ? module.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState sourceState = core != null &&
                core.Controller != null && module != null
                ? core.Controller.TryGetWeaponRuntimeState(module)
                : null;
            CeWeaponProductionCycleProbeReport report =
                new CeWeaponProductionCycleProbeReport();
            report.ModuleInstanceID = module != null ? module.ModuleInstanceID : null;
            report.ModuleDefName = weaponDef != null ? weaponDef.defName : null;
            report.HostRotation = host != null ? host.Rotation.AsInt : -1;
            report.Target = target.ToString();

            ShuttleWeaponAmmoState sourceAmmo = sourceState != null
                ? sourceState.AmmoForRuntimeOnly
                : null;
            string sourceAmmoDefName = sourceAmmo != null
                ? sourceAmmo.SelectedAmmoDefName
                : null;
            report.SourceLoadedBefore = sourceAmmo != null
                ? sourceAmmo.LoadedAmmoCount
                : -1;
            if (host == null || core == null || core.Controller == null ||
                module == null || weaponDef == null || sourceState == null)
            {
                return Failed(report, sourceAmmo, sourceAmmoDefName,
                    "ce-production-cycle-source-context-missing");
            }

            CeWeaponPreparedMagazineTransfer prepared;
            string failureReason;
            if (!this.transfer.TryPrepare(
                    weaponDef,
                    sourceState,
                    out prepared,
                    out failureReason))
            {
                return Failed(report, sourceAmmo, sourceAmmoDefName, failureReason);
            }

            CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(
                prepared.PreparedGun);
            report.CeLoadedBefore = magazine != null ? magazine.CurMagCount : -1;
            if (magazine == null)
            {
                return Failed(report, sourceAmmo, sourceAmmoDefName,
                    "ce-production-cycle-magazine-missing",
                    prepared.PreparedGun);
            }

            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            ShuttleRuntimeState shuttleRuntimeState =
                core.Controller.GetLaunchRuntimeState();
            ShuttleWeaponRuntimeState cycleState = new ShuttleWeaponRuntimeState();
            cycleState.GunForRuntimeOnly = prepared.PreparedGun;
            cycleState.SetTargetPriority(
                ShuttleWeaponTargetPriority.ForcedTargetOnly);
            cycleState.SetForcedTargetForRuntimeOnly(target);

            CeWeaponProductionCycleProbePowerSink powerSink =
                new CeWeaponProductionCycleProbePowerSink();
            int ticksGame = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : -1;
            ShuttleModuleRuntimeContext context =
                new ShuttleRuntimeDispatchSupport().CreateRuntimeContext(
                    host,
                    assembly,
                    core.Controller.GetProfileForRead(),
                    shuttleRuntimeState,
                    module,
                    cycleState,
                    null,
                    powerSink,
                    null,
                    ticksGame);
            CeWeaponFiringComposition composition =
                new CeWeaponFiringComposition();
            CeWeaponProductionCycleProbeSession session =
                new CeWeaponProductionCycleProbeSession(
                    report,
                    sourceAmmo,
                    sourceAmmoDefName,
                    cycleState,
                    shuttleRuntimeState,
                    context,
                    composition.CycleDriver,
                    composition.PowerProjection,
                    powerSink,
                    prepared.PreparedGun,
                    magazine);

            CeWeaponMuzzleVerb verb;
            CeWeaponAmmoOwnerAdapter owner;
            int shotsPerBurst;
            if (!composition.CycleDriver.TryBind(
                    context,
                    session.NotifyCastComplete,
                    out verb,
                    out owner,
                    out shotsPerBurst,
                    out failureReason))
            {
                session.Attach(verb, owner);
                session.Fail(failureReason);
                return session;
            }

            session.Attach(verb, owner);
            report.OwnerReady = owner != null && owner.ContextMatches(host);
            report.ExpectedShots = shotsPerBurst;
            report.ExpectedWarmupTicks =
                composition.CyclePolicy.GetWarmupTicks(weaponDef, verb);
            report.ExpectedCooldownTicks =
                composition.CyclePolicy.GetCooldownTicks(weaponDef, verb);
            report.ExpectedPowerWatts =
                composition.CyclePolicy.GetFiringPowerDrawWatts(weaponDef);
            if (magazine.CurMagCount < shotsPerBurst)
            {
                session.Fail("ce-production-cycle-insufficient-ammo");
                return session;
            }

            ShuttleWeaponEngagementResult engagement =
                composition.CycleDriver.EvaluateForcedTarget(
                    context,
                    verb,
                    target);
            if (!engagement.IsAllowed)
            {
                session.Fail("ce-production-cycle-target-rejected:" +
                    engagement.ReasonCode);
                return session;
            }

            report.TargetAccepted = true;
            return session;
        }

        private static CeWeaponProductionCycleProbeSession Failed(
            CeWeaponProductionCycleProbeReport report,
            ShuttleWeaponAmmoState sourceAmmo,
            string sourceAmmoDefName,
            string failureReason,
            Thing preparedGun = null)
        {
            CeWeaponProductionCycleProbeSession session =
                new CeWeaponProductionCycleProbeSession(
                    report,
                    sourceAmmo,
                    sourceAmmoDefName,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    preparedGun,
                    null);
            session.Fail(failureReason);
            return session;
        }
    }
}

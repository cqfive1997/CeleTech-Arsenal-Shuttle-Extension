using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal static class CeWeaponProductionShotDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "CE production shot boundary: fire transient installed weapons...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginTargeting()
        {
            ThingWithComps host;
            CompModularShuttleCore core;
            if (!TryGetSelectedShuttle(out host, out core))
            {
                return;
            }

            Messages.Message(
                "Choose an empty target cell within 30 tiles. Each loaded authored weapon will fire one transient CE test shot.",
                MessageTypeDefOf.NeutralEvent,
                false);
            TargetingParameters parameters = TargetingParameters.ForAttackAny();
            parameters.canTargetLocations = true;
            parameters.canTargetPawns = true;
            parameters.canTargetBuildings = true;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return target.IsValid &&
                    host.Spawned &&
                    (!target.HasThing || target.Thing != host);
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target) { RunAll(host, core, target); },
                null,
                null,
                null,
                true);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE production burst boundary: fire transient installed weapons...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginBurstTargeting()
        {
            ThingWithComps host;
            CompModularShuttleCore core;
            if (!TryGetSelectedShuttle(out host, out core))
            {
                return;
            }

            Messages.Message(
                "Choose a clear target cell within 30 tiles. Each loaded authored weapon will fire one complete transient CE burst.",
                MessageTypeDefOf.NeutralEvent,
                false);
            TargetingParameters parameters = TargetingParameters.ForAttackAny();
            parameters.canTargetLocations = true;
            parameters.canTargetPawns = true;
            parameters.canTargetBuildings = true;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return target.IsValid &&
                    host.Spawned &&
                    (!target.HasThing || target.Thing != host);
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target) { StartBurstBatch(host, core, target); },
                null,
                null,
                null,
                true);
        }

        private static void RunAll(
            ThingWithComps host,
            CompModularShuttleCore core,
            LocalTargetInfo target)
        {
            core.Controller.GetProfileForRead();
            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            CeWeaponProductionShotProbe probe = new CeWeaponProductionShotProbe();
            StringBuilder output = new StringBuilder();
            output.Append("[CeleTech Shuttle][CE Production Shot Probe] begin target=")
                .Append(target.ToString())
                .AppendLine();

            int inspected = 0;
            int passed = 0;
            if (assembly != null && assembly.Modules != null)
            {
                for (int i = 0; i < assembly.Modules.Count; i++)
                {
                    ShuttleModule module = assembly.Modules[i];
                    ShuttleWeaponModuleDef weaponDef = module != null
                        ? module.ModuleDef as ShuttleWeaponModuleDef
                        : null;
                    if (module == null || !module.IsEnabled || weaponDef == null ||
                        CeWeaponCompatibilitySpec.ForWeapon(weaponDef.defName) == null)
                    {
                        continue;
                    }

                    inspected++;
                    CeWeaponProductionShotProbeReport report = probe.Run(
                        host,
                        module.ModuleInstanceID,
                        module.ParentSlotID,
                        weaponDef,
                        core.Controller.TryGetWeaponRuntimeState(module),
                        target);
                    if (report.Passed)
                    {
                        passed++;
                    }

                    AppendReport(output, report);
                }
            }

            bool allPassed = inspected > 0 && passed == inspected;
            output.Append("[CeleTech Shuttle][CE Production Shot Probe] complete")
                .Append(" inspected=").Append(inspected)
                .Append(" passed=").Append(passed)
                .Append(" allPassed=").Append(allPassed);
            Log.Message(output.ToString());
            Messages.Message(
                allPassed
                    ? "CE production shot boundary passed; installed magazines were not changed."
                    : "CE production shot boundary did not pass; see the log for the exact module and reason.",
                allPassed ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        private static void AppendReport(
            StringBuilder output,
            CeWeaponProductionShotProbeReport report)
        {
            output.Append("[CeleTech Shuttle][CE Production Shot Probe] module=")
                .Append(report.ModuleInstanceID)
                .Append(" moduleDef=").Append(report.ModuleDefName)
                .Append(" source=").Append(report.SourceLoadedBefore)
                .Append("->").Append(report.SourceLoadedAfter)
                .Append(" ce=").Append(report.CeLoadedBefore)
                .Append("->").Append(report.CeLoadedAfter)
                .Append(" callbacks=").Append(report.CompletionCallbacks)
                .Append(" ownerReady=").Append(report.OwnerReady)
                .Append(" castAccepted=").Append(report.CastAccepted)
                .Append(" sourceUnchanged=").Append(report.SourceUnchanged)
                .Append(" muzzleCell=").Append(report.MuzzleCell)
                .Append(" muzzleXZ=")
                .Append(report.MuzzleDrawPos.x).Append(',')
                .Append(report.MuzzleDrawPos.z)
                .Append(" originObserved=").Append(report.ProjectileOriginObserved)
                .Append(" originMatches=").Append(report.ProjectileOriginMatches)
                .Append(" passed=").Append(report.Passed)
                .Append(" failure=").Append(report.Failure ?? "none")
                .Append(" trace=").Append(report.FailureTrace ?? "none")
                .AppendLine();
        }

        private static void StartBurstBatch(
            ThingWithComps host,
            CompModularShuttleCore core,
            LocalTargetInfo target)
        {
            CeWeaponProductionBurstProbeGameComponent component =
                CeWeaponProductionBurstProbeGameComponent.CurrentComponent;
            if (component == null)
            {
                Messages.Message(
                    "The CE production burst probe component is unavailable.",
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            if (component.HasActiveBatch)
            {
                Messages.Message(
                    "A CE production burst probe is already active.",
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            core.Controller.GetProfileForRead();
            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            CeWeaponProductionBurstProbe probe = new CeWeaponProductionBurstProbe();
            List<CeWeaponProductionBurstProbeSession> sessions =
                new List<CeWeaponProductionBurstProbeSession>();
            if (assembly != null && assembly.Modules != null)
            {
                for (int i = 0; i < assembly.Modules.Count; i++)
                {
                    ShuttleModule module = assembly.Modules[i];
                    ShuttleWeaponModuleDef weaponDef = module != null
                        ? module.ModuleDef as ShuttleWeaponModuleDef
                        : null;
                    if (module == null || !module.IsEnabled || weaponDef == null ||
                        CeWeaponCompatibilitySpec.ForWeapon(weaponDef.defName) == null)
                    {
                        continue;
                    }

                    sessions.Add(probe.CreateAndStart(
                        host,
                        module.ModuleInstanceID,
                        module.ParentSlotID,
                        weaponDef,
                        core.Controller.TryGetWeaponRuntimeState(module),
                        target));
                }
            }

            string failureReason;
            if (component.TryStart(sessions, target, out failureReason))
            {
                return;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i] != null)
                {
                    sessions[i].Release();
                }
            }

            Messages.Message(
                failureReason ?? "The CE production burst probe could not start.",
                MessageTypeDefOf.RejectInput,
                false);
        }

        private static bool TryGetSelectedShuttle(
            out ThingWithComps host,
            out CompModularShuttleCore core)
        {
            host = Find.Selector.SingleSelectedThing as ThingWithComps;
            core = host != null ? host.GetComp<CompModularShuttleCore>() : null;
            if (core != null && core.Controller != null && host.Spawned)
            {
                return true;
            }

            Messages.Message(
                "Select one landed modular shuttle before running the CE production shot test.",
                MessageTypeDefOf.RejectInput,
                false);
            return false;
        }
    }
}

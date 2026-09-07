using System.Text;
using CombatExtended;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal static class CeWeaponBackendDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "CE production backend: report registration",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportRegistration()
        {
            StringBuilder report = new StringBuilder();
            report.Append("[CeleTech Shuttle][CE Backend] registration report")
                .Append(" registered=").Append(CeWeaponBackendBootstrap.RegistrationSucceeded)
                .Append(" backend=").Append(CeWeaponBackendBootstrap.Factory.BackendId)
                .Append(" productionAssembly=")
                .Append(typeof(CeWeaponBackendBootstrap).Assembly.GetName().Name)
                .Append(" ceVersion=").Append(typeof(AmmoDef).Assembly.GetName().Version)
                .AppendLine();

            AppendWeaponProbe(report, "CT_Shuttle_Module_6mmPointDefense");
            AppendWeaponProbe(report, "CT_Shuttle_Module_40mmCIWS");
            new CeWeaponBackendAuthorityReportWriter().AppendSelectedShuttle(report);
            Log.Message(report.ToString().TrimEnd());

            Messages.Message(
                "CE production backend registration report written to the log.",
                MessageTypeDefOf.NeutralEvent,
                false);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE installed magazine transfer: preflight selected shuttle",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PreflightSelectedShuttle()
        {
            CompModularShuttleCore core;
            if (!TryGetSelectedShuttleCore(out core))
            {
                return;
            }

            core.Controller.GetProfileForRead();
            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            CeWeaponMagazineMigrationPreflight preflight =
                new CeWeaponMagazineMigrationPreflight();
            StringBuilder output = new StringBuilder();
            output.AppendLine("[CeleTech Shuttle][CE Magazine Preflight] begin");

            int inspected = 0;
            int ready = 0;
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
                    CeWeaponMagazineMigrationReport report = preflight.Inspect(
                        module.ModuleInstanceID,
                        weaponDef,
                        core.Controller.TryGetWeaponRuntimeState(module));
                    if (report.Ready)
                    {
                        ready++;
                    }

                    AppendMigrationReport(output, report);
                }
            }

            output.Append("[CeleTech Shuttle][CE Magazine Preflight] complete")
                .Append(" inspected=").Append(inspected)
                .Append(" ready=").Append(ready)
                .Append(" allReady=").Append(inspected > 0 && ready == inspected);
            Log.Message(output.ToString());

            Messages.Message(
                inspected > 0 && ready == inspected
                    ? "CE magazine transfer preflight passed; details were written to the log."
                    : "CE magazine transfer preflight did not pass; see the log for the exact reason.",
                inspected > 0 && ready == inspected
                    ? MessageTypeDefOf.NeutralEvent
                    : MessageTypeDefOf.RejectInput,
                false);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE installed magazine transfer: commit/rollback selected shuttle",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ProbeCommitRollbackSelectedShuttle()
        {
            CompModularShuttleCore core;
            if (!TryGetSelectedShuttleCore(out core))
            {
                return;
            }

            core.Controller.GetProfileForRead();
            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            CeWeaponMagazineAuthorityTransferProbe probe =
                new CeWeaponMagazineAuthorityTransferProbe();
            StringBuilder output = new StringBuilder();
            output.AppendLine("[CeleTech Shuttle][CE Magazine Transaction Probe] begin");

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
                    CeWeaponMagazineAuthorityTransferProbeReport report = probe.Run(
                        module.ModuleInstanceID,
                        weaponDef,
                        core.Controller.TryGetWeaponRuntimeState(module));
                    if (report.Passed)
                    {
                        passed++;
                    }

                    AppendAuthorityTransferProbeReport(output, report);
                }
            }

            bool allPassed = inspected > 0 && passed == inspected;
            output.Append("[CeleTech Shuttle][CE Magazine Transaction Probe] complete")
                .Append(" inspected=").Append(inspected)
                .Append(" passed=").Append(passed)
                .Append(" allPassed=").Append(allPassed);
            Log.Message(output.ToString());
            Messages.Message(
                allPassed
                    ? "CE magazine commit/rollback probe passed; the installed state was restored."
                    : "CE magazine commit/rollback probe did not pass; pause and inspect the log before saving.",
                allPassed ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        private static void AppendWeaponProbe(StringBuilder report, string weaponDefName)
        {
            ShuttleWeaponModuleDef weaponDef =
                DefDatabase<ShuttleWeaponModuleDef>.GetNamedSilentFail(weaponDefName);
            ShuttleWeaponBackendProbeContext context =
                new ShuttleWeaponBackendProbeContext(
                    weaponDef,
                    null,
                    "legacy",
                    true);
            ShuttleWeaponCompatibilityReport ceReport =
                CeWeaponBackendBootstrap.Factory.Probe(context);

            ShuttleWeaponBackendBinding selectedBinding;
            ShuttleWeaponCompatibilityReport selectedReport;
            bool resolved = ShuttleWeaponBackendRegistry.Shared.TryResolve(
                context,
                out selectedBinding,
                out selectedReport);

            report.Append("[CeleTech Shuttle][CE Backend] weapon=")
                .Append(weaponDefName)
                .Append(" defResolved=").Append(weaponDef != null)
                .Append(" definitionContext=idle-legacy-source")
                .Append(" definitionProbe=").Append(FormatReport(ceReport))
                .Append(" candidateResolved=").Append(resolved)
                .Append(" candidate=")
                .Append(selectedBinding == null ? "null" : selectedBinding.BackendId)
                .Append(" candidateReport=").Append(FormatReport(selectedReport))
                .AppendLine();
        }

        private static string FormatReport(ShuttleWeaponCompatibilityReport report)
        {
            return report == null
                ? "null"
                : report.Support + ":" + report.ReasonCode;
        }

        private static void AppendMigrationReport(
            StringBuilder output,
            CeWeaponMagazineMigrationReport report)
        {
            output.Append("[CeleTech Shuttle][CE Magazine Preflight] module=")
                .Append(report.ModuleInstanceID)
                .Append(" moduleDef=").Append(report.ModuleDefName)
                .Append(" runtimeGun=").Append(report.RuntimeGunDefName)
                .Append(" ammo=").Append(report.AmmoDefName)
                .Append(" source=").Append(report.SourceLoadedCount)
                .Append('/').Append(report.SourceCapacity)
                .Append(" staged=").Append(report.StagedLoadedCount)
                .Append('/').Append(report.StagedCapacity)
                .Append(" sourceUnchanged=").Append(report.SourceUnchanged)
                .Append(" ready=").Append(report.Ready)
                .Append(" failure=").Append(report.Failure ?? "none")
                .AppendLine();
        }

        private static void AppendAuthorityTransferProbeReport(
            StringBuilder output,
            CeWeaponMagazineAuthorityTransferProbeReport report)
        {
            output.Append("[CeleTech Shuttle][CE Magazine Transaction Probe] module=")
                .Append(report.ModuleInstanceID)
                .Append(" moduleDef=").Append(report.ModuleDefName)
                .Append(" ammo=").Append(report.AmmoDefName)
                .Append(" loaded=").Append(report.SourceLoadedCount)
                .Append(" authorityBefore=").Append(report.AuthorityBefore)
                .Append(" authorityAfterCommit=").Append(report.AuthorityAfterCommit)
                .Append(" authorityAfterRollback=").Append(report.AuthorityAfterRollback)
                .Append(" commitAccepted=").Append(report.CommitAccepted)
                .Append(" rollbackAccepted=").Append(report.RollbackAccepted)
                .Append(" sourceRestored=").Append(report.SourceRestored)
                .Append(" passed=").Append(report.Passed)
                .Append(" failure=").Append(report.Failure ?? "none")
                .AppendLine();
        }

        private static bool TryGetSelectedShuttleCore(out CompModularShuttleCore core)
        {
            ThingWithComps host = Find.Selector.SingleSelectedThing as ThingWithComps;
            core = host != null ? host.GetComp<CompModularShuttleCore>() : null;
            if (core != null && core.Controller != null)
            {
                return true;
            }

            Messages.Message(
                "Select one landed modular shuttle before running the CE magazine test.",
                MessageTypeDefOf.RejectInput,
                false);
            return false;
        }
    }
}

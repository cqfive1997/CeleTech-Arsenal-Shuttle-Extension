using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal static class NativeVerbWeaponBackendDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "Native backend: report definition readiness",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportDefinitionReadiness()
        {
            NativeVerbWeaponCompatibilityEvaluator evaluator =
                new NativeVerbWeaponCompatibilityEvaluator();
            List<ShuttleWeaponModuleDef> defs =
                DefDatabase<ShuttleWeaponModuleDef>.AllDefsListForReading;
            StringBuilder output = new StringBuilder();
            output.AppendLine("[CeleTech Shuttle][Native Backend] definition readiness");

            int inspected = 0;
            int ready = 0;
            int nativeSelected = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                ShuttleWeaponModuleDef weaponDef = defs[i];
                if (weaponDef == null)
                {
                    continue;
                }

                inspected++;
                ShuttleWeaponCompatibilityReport report = evaluator.Evaluate(
                    new ShuttleWeaponBackendProbeContext(
                        weaponDef,
                        null,
                        ShuttleWeaponRuntimeState.CoreMagazineAuthorityId,
                        false));
                bool isReady = report != null && report.IsSupported;
                if (isReady)
                {
                    ready++;
                }

                ShuttleWeaponBackendProbeContext selectionContext =
                    new ShuttleWeaponBackendProbeContext(
                        weaponDef,
                        null,
                        ShuttleWeaponRuntimeState.CoreMagazineAuthorityId,
                        true);
                ShuttleWeaponBackendBinding selectedBinding;
                ShuttleWeaponCompatibilityReport selectedReport;
                bool resolved = ShuttleWeaponBackendRegistry.Shared.TryResolve(
                    selectionContext,
                    out selectedBinding,
                    out selectedReport);
                bool selectedNative = resolved && selectedBinding != null &&
                    selectedBinding.BackendId == NativeVerbWeaponBackendFactory.Id;
                if (selectedNative)
                {
                    nativeSelected++;
                }

                output.Append("weapon=").Append(weaponDef.defName)
                    .Append(" gun=")
                    .Append(weaponDef.weaponDef != null ? weaponDef.weaponDef.defName : "<none>")
                    .Append(" ready=").Append(isReady)
                    .Append(" support=")
                    .Append(report != null ? report.Support.ToString() : "<none>")
                    .Append(" reason=")
                    .Append(report != null ? report.ReasonCode : "<none>")
                    .Append(" candidateResolved=").Append(resolved)
                    .Append(" selected=")
                    .Append(selectedBinding != null ? selectedBinding.BackendId : "<none>")
                    .Append(" selectedReport=").Append(FormatReport(selectedReport))
                    .AppendLine();
            }

            output.Append("summary inspected=").Append(inspected)
                .Append(" ready=").Append(ready)
                .Append(" nativeSelected=").Append(nativeSelected)
                .Append(" productionSelection=")
                .Append(ready > 0 && nativeSelected == ready)
                .AppendLine();
            AppendSelectedShuttle(output);
            Log.Message(output.ToString());
            Messages.Message(
                "Native backend definition-readiness report written to the log.",
                MessageTypeDefOf.NeutralEvent,
                false);
        }

        private static void AppendSelectedShuttle(StringBuilder output)
        {
            ThingWithComps selected = Find.Selector.SingleSelectedThing as ThingWithComps;
            CompModularShuttleCore core = selected != null
                ? selected.GetComp<CompModularShuttleCore>()
                : null;
            if (core == null || core.Controller == null)
            {
                output.Append("selectedShuttle=false");
                return;
            }

            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            if (assembly == null || assembly.Modules == null)
            {
                output.Append("selectedShuttle=true modulesUnavailable=true");
                return;
            }

            output.Append("selectedShuttle=true").AppendLine();
            for (int i = 0; i < assembly.Modules.Count; i++)
            {
                ShuttleModule module = assembly.Modules[i];
                ShuttleWeaponModuleDef weaponDef = module != null
                    ? module.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                if (module == null || weaponDef == null)
                {
                    continue;
                }

                ShuttleWeaponRuntimeState state =
                    core.Controller.TryGetWeaponRuntimeState(module);
                ShuttleWeaponBackendBinding cached = null;
                if (state != null)
                {
                    state.TryGetBackendBindingForRuntimeOnly(weaponDef, out cached);
                }

                Thing gun = state != null ? state.GunForRuntimeOnly : null;
                CompEquippable equippable = gun != null
                    ? gun.TryGetComp<CompEquippable>()
                    : null;
                Verb verb = equippable != null ? equippable.PrimaryVerb : null;
                string authority = state != null
                    ? state.MagazineAuthorityBackendIdForRuntimeOnly
                    : "<none>";
                bool nativeInvariant = state != null &&
                    cached != null &&
                    cached.BackendId == NativeVerbWeaponBackendFactory.Id &&
                    cached.FireDriver != null &&
                    cached.ChannelDriver != null &&
                    authority == ShuttleWeaponRuntimeState.CoreMagazineAuthorityId &&
                    gun != null &&
                    gun.def == weaponDef.weaponDef;

                output.Append("module=").Append(module.ModuleInstanceID)
                    .Append(" moduleDef=").Append(weaponDef.defName)
                    .Append(" authority=").Append(authority)
                    .Append(" gun=")
                    .Append(gun != null && gun.def != null ? gun.def.defName : "<none>")
                    .Append(" loaded=")
                    .Append(state != null ? state.AmmoForRuntimeOnly.LoadedAmmoCount : 0)
                    .Append(" verbState=")
                    .Append(verb != null ? verb.state.ToString() : "<none>")
                    .Append(" verbType=")
                    .Append(verb != null ? verb.GetType().FullName : "<none>")
                    .Append(" warmup=")
                    .Append(state != null ? state.GetWarmupTicksForRuntimeOnly() : 0)
                    .Append(" cooldown=")
                    .Append(state != null ? state.GetCooldownTicksForRuntimeOnly() : 0)
                    .Append(" cached=")
                    .Append(cached != null ? cached.BackendId : "<none>")
                    .Append(" legacyRuntimeActive=")
                    .Append(cached != null &&
                        cached.BackendId == ShuttleWeaponRuntimeState.CoreMagazineAuthorityId)
                    .Append(" cutoverInvariant=").Append(nativeInvariant)
                    .AppendLine();
            }
        }

        private static string FormatReport(ShuttleWeaponCompatibilityReport report)
        {
            return report == null
                ? "<none>"
                : report.Support + ":" + report.ReasonCode;
        }
    }
}

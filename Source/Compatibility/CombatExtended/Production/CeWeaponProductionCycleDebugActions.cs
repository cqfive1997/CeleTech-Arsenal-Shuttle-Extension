using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal static class CeWeaponProductionCycleDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "CE production firing cycle: selected installed weapon...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ChooseWeapon()
        {
            ThingWithComps host = Find.Selector.SingleSelectedThing as ThingWithComps;
            CompModularShuttleCore core = host != null
                ? host.GetComp<CompModularShuttleCore>()
                : null;
            if (core == null || core.Controller == null || !host.Spawned)
            {
                Notify(
                    "Select one landed modular shuttle before running the CE firing-cycle test.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            CeWeaponProductionCycleProbeGameComponent component =
                CeWeaponProductionCycleProbeGameComponent.CurrentComponent;
            if (component == null || component.HasActiveSession)
            {
                Notify(
                    component == null
                        ? "The CE production firing-cycle component is unavailable."
                        : "A CE production firing-cycle probe is already active.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            List<FloatMenuOption> options = new List<FloatMenuOption>();
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

                    ShuttleModule selectedModule = module;
                    options.Add(new FloatMenuOption(
                        weaponDef.LabelCap + " / " + module.ModuleInstanceID,
                        delegate { BeginTargeting(host, core, selectedModule); }));
                }
            }

            if (options.Count == 0)
            {
                Notify(
                    "No compatible enabled 6mm or 40mm weapon is installed.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void BeginTargeting(
            ThingWithComps host,
            CompModularShuttleCore core,
            ShuttleModule module)
        {
            Notify(
                "Choose a hostile pawn or building in range. The probe will run target validation, warmup, one full burst, cooldown and firing-power projection.",
                MessageTypeDefOf.NeutralEvent);
            TargetingParameters parameters = TargetingParameters.ForAttackAny();
            parameters.canTargetLocations = false;
            parameters.canTargetPawns = true;
            parameters.canTargetBuildings = true;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return target.IsValid && target.HasThing &&
                    host.Spawned && target.Thing != host;
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    Start(host, core, module, target);
                },
                null,
                null,
                null,
                true);
        }

        private static void Start(
            ThingWithComps host,
            CompModularShuttleCore core,
            ShuttleModule module,
            LocalTargetInfo target)
        {
            CeWeaponProductionCycleProbeGameComponent component =
                CeWeaponProductionCycleProbeGameComponent.CurrentComponent;
            if (component == null)
            {
                Notify(
                    "The CE production firing-cycle component is unavailable.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            CeWeaponProductionCycleProbeSession session =
                new CeWeaponProductionCycleProbe().Create(
                    host,
                    core,
                    module,
                    target);
            string failureReason;
            if (!component.TryStart(session, out failureReason))
            {
                session.Release();
                Notify(
                    failureReason ?? "The CE production firing-cycle probe could not start.",
                    MessageTypeDefOf.RejectInput);
            }
        }

        private static void Notify(string text, MessageTypeDef type)
        {
            Messages.Message(text, type, false);
        }
    }
}

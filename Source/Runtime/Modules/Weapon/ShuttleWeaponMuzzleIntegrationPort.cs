using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponMuzzleIntegrationChoice
    {
        internal string ModuleInstanceID { get; set; }

        internal string ModuleLabel { get; set; }

        internal string ModuleDefName { get; set; }

        internal string WeaponDefName { get; set; }

        internal string WeaponLabel { get; set; }

        internal string ParentSlotID { get; set; }
    }

    internal static class ShuttleWeaponMuzzleIntegrationPort
    {
        private static readonly ShuttleWeaponMuzzleResolver Resolver =
            new ShuttleWeaponMuzzleResolver();

        internal static bool TryBuildChoices(
            Thing host,
            List<ShuttleWeaponMuzzleIntegrationChoice> output,
            out string failure)
        {
            failure = null;
            if (output == null)
            {
                failure = "output-unavailable";
                return false;
            }

            output.Clear();
            ShuttleController controller;
            ThingWithComps typedHost;
            if (!TryResolveController(host, out typedHost, out controller, out failure))
            {
                return false;
            }

            controller.GetProfileForRead();
            ShuttleAssemblyState assembly = controller.AssemblyState;
            if (assembly == null || assembly.Modules == null)
            {
                failure = "assembly-unavailable";
                return false;
            }

            IReadOnlyList<ShuttleModule> modules = assembly.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                ShuttleWeaponModuleDef weaponDef = module != null
                    ? module.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                if (module == null || !module.IsEnabled || weaponDef == null)
                {
                    continue;
                }

                output.Add(new ShuttleWeaponMuzzleIntegrationChoice
                {
                    ModuleInstanceID = module.ModuleInstanceID,
                    ModuleLabel = weaponDef.LabelCap,
                    ModuleDefName = weaponDef.defName,
                    WeaponDefName = weaponDef.weaponDef != null
                        ? weaponDef.weaponDef.defName
                        : null,
                    WeaponLabel = weaponDef.weaponDef != null
                        ? weaponDef.weaponDef.LabelCap
                        : null,
                    ParentSlotID = module.ParentSlotID
                });
            }

            if (output.Count == 0)
            {
                failure = "no-enabled-weapon";
                return false;
            }

            return true;
        }

        internal static bool TryResolve(
            Thing host,
            string moduleInstanceID,
            LocalTargetInfo target,
            out ShuttleWeaponMuzzleSource source,
            out ShuttleWeaponMuzzleIntegrationChoice resolvedChoice,
            out string failure)
        {
            source = default(ShuttleWeaponMuzzleSource);
            resolvedChoice = null;
            failure = null;

            ShuttleController controller;
            ThingWithComps typedHost;
            if (!TryResolveController(host, out typedHost, out controller, out failure))
            {
                return false;
            }

            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                failure = "module-identity-missing";
                return false;
            }

            if (!target.IsValid ||
                !target.Cell.IsValid ||
                !target.Cell.InBounds(typedHost.Map))
            {
                failure = "target-invalid";
                return false;
            }

            controller.GetProfileForRead();
            ShuttleAssemblyState assembly = controller.AssemblyState;
            ShuttleModule module = assembly != null
                ? assembly.GetModule(moduleInstanceID)
                : null;
            ShuttleWeaponModuleDef weaponDef = module != null
                ? module.ModuleDef as ShuttleWeaponModuleDef
                : null;
            if (module == null || !module.IsEnabled || weaponDef == null)
            {
                failure = "weapon-unavailable";
                return false;
            }

            ShuttleWeaponRuntimeState runtimeState =
                controller.TryGetWeaponRuntimeState(module);
            if (runtimeState == null)
            {
                failure = "runtime-state-unavailable";
                return false;
            }

            source = Resolver.Resolve(
                typedHost,
                module.ParentSlotID,
                weaponDef,
                runtimeState,
                target,
                false);
            if (!source.Cell.IsValid ||
                !source.Cell.InBounds(typedHost.Map))
            {
                failure = "resolver-source-invalid";
                return false;
            }

            resolvedChoice = new ShuttleWeaponMuzzleIntegrationChoice
            {
                ModuleInstanceID = module.ModuleInstanceID,
                ModuleLabel = weaponDef.LabelCap,
                ModuleDefName = weaponDef.defName,
                WeaponDefName = weaponDef.weaponDef != null
                    ? weaponDef.weaponDef.defName
                    : null,
                WeaponLabel = weaponDef.weaponDef != null
                    ? weaponDef.weaponDef.LabelCap
                    : null,
                ParentSlotID = module.ParentSlotID
            };
            return true;
        }

        private static bool TryResolveController(
            Thing host,
            out ThingWithComps typedHost,
            out ShuttleController controller,
            out string failure)
        {
            typedHost = host as ThingWithComps;
            controller = null;
            failure = null;
            if (typedHost == null || !typedHost.Spawned || typedHost.Map == null)
            {
                failure = "host-not-spawned";
                return false;
            }

            CompModularShuttleCore core =
                typedHost.GetComp<CompModularShuttleCore>();
            controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                failure = "controller-unavailable";
                return false;
            }

            return true;
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeResolvedMuzzleChoice
    {
        internal string ModuleInstanceID { get; set; }

        internal string MenuLabel { get; set; }
    }

    internal static class CeRuntimeProbeResolvedMuzzleAdapter
    {
        internal static bool TryBuildChoices(
            Thing host,
            List<CeRuntimeProbeResolvedMuzzleChoice> output,
            out string failure)
        {
            failure = null;
            if (output == null)
            {
                failure = "The CE integration choice output is unavailable.";
                return false;
            }

            output.Clear();
            List<ShuttleWeaponMuzzleIntegrationChoice> resolved =
                new List<ShuttleWeaponMuzzleIntegrationChoice>();
            if (!ShuttleWeaponMuzzleIntegrationPort.TryBuildChoices(
                host,
                resolved,
                out failure))
            {
                failure = BuildFailure(failure);
                return false;
            }

            for (int i = 0; i < resolved.Count; i++)
            {
                ShuttleWeaponMuzzleIntegrationChoice choice = resolved[i];
                if (choice == null || string.IsNullOrEmpty(choice.ModuleInstanceID))
                {
                    continue;
                }

                output.Add(new CeRuntimeProbeResolvedMuzzleChoice
                {
                    ModuleInstanceID = choice.ModuleInstanceID,
                    MenuLabel = BuildMenuLabel(
                        choice.ModuleLabel,
                        choice.ModuleDefName,
                        choice.ParentSlotID,
                        choice.WeaponLabel,
                        choice.WeaponDefName)
                });
            }

            if (output.Count == 0)
            {
                failure = "The main resolver returned no usable installed weapon choice.";
                return false;
            }

            return true;
        }

        internal static bool TryResolve(
            Thing host,
            CeRuntimeProbeResolvedMuzzleChoice choice,
            LocalTargetInfo target,
            out CeRuntimeProbeMuzzleSource source,
            out string failure)
        {
            source = null;
            failure = null;
            if (choice == null || string.IsNullOrEmpty(choice.ModuleInstanceID))
            {
                failure = "The selected installed weapon identity is unavailable.";
                return false;
            }

            ShuttleWeaponMuzzleSource resolvedSource;
            ShuttleWeaponMuzzleIntegrationChoice resolvedChoice;
            if (!ShuttleWeaponMuzzleIntegrationPort.TryResolve(
                host,
                choice.ModuleInstanceID,
                target,
                out resolvedSource,
                out resolvedChoice,
                out failure))
            {
                failure = BuildFailure(failure);
                return false;
            }

            CeRuntimeProbeMuzzleSource converted = new CeRuntimeProbeMuzzleSource(
                resolvedSource.Cell,
                resolvedSource.DrawPos,
                true,
                resolvedChoice.ModuleInstanceID,
                resolvedChoice.ModuleDefName,
                resolvedChoice.WeaponDefName,
                resolvedChoice.ParentSlotID,
                resolvedSource.UsesFallback,
                resolvedSource.MuzzleIndex);
            if (!converted.IsValidFor(host))
            {
                failure = "The existing resolver returned an invalid map-space source.";
                return false;
            }

            source = converted;
            return true;
        }

        private static string BuildMenuLabel(
            string moduleLabel,
            string moduleDefName,
            string parentSlotID,
            string weaponLabel,
            string weaponDefName)
        {
            string module = !string.IsNullOrEmpty(moduleLabel)
                ? moduleLabel
                : moduleDefName;
            string weapon = !string.IsNullOrEmpty(weaponLabel)
                ? weaponLabel
                : weaponDefName;
            return module + " / " + parentSlotID + " / " + weapon;
        }

        private static string BuildFailure(string failureCode)
        {
            return string.IsNullOrEmpty(failureCode)
                ? "The existing shuttle muzzle resolver rejected the request."
                : "The existing shuttle muzzle resolver rejected the request: " +
                    failureCode + ".";
        }
    }
}

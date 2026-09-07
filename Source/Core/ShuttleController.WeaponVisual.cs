using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal void BuildWeaponTurretVisualSnapshots(
            List<ShuttleWeaponTurretVisualSnapshot> output)
        {
            if (output == null)
            {
                return;
            }

            this.EnsureRuntimeState();

            if (this.assemblyState == null || this.runtimeState == null)
            {
                output.Clear();
                return;
            }

            this.assemblyState.EnsureInitialized();
            IReadOnlyList<ShuttleModule> modules = this.assemblyState.Modules;
            if (modules == null || modules.Count == 0)
            {
                output.Clear();
                return;
            }

            int outputIndex = 0;
            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module == null || !module.IsEnabled)
                {
                    continue;
                }

                ShuttleWeaponModuleDef weaponDef = module.ModuleDef as ShuttleWeaponModuleDef;
                if (weaponDef == null)
                {
                    continue;
                }

                ShuttleWeaponTurretVisualSnapshot snapshot =
                    this.GetOrCreateWeaponTurretVisualSnapshot(output, outputIndex);
                this.FillWeaponTurretVisualSnapshot(snapshot, module, weaponDef);
                outputIndex++;
            }

            while (output.Count > outputIndex)
            {
                output.RemoveAt(output.Count - 1);
            }
        }

        private ShuttleWeaponTurretVisualSnapshot GetOrCreateWeaponTurretVisualSnapshot(
            List<ShuttleWeaponTurretVisualSnapshot> output,
            int index)
        {
            if (index < output.Count)
            {
                ShuttleWeaponTurretVisualSnapshot existing = output[index];
                if (existing != null)
                {
                    return existing;
                }

                existing = new ShuttleWeaponTurretVisualSnapshot();
                output[index] = existing;
                return existing;
            }

            ShuttleWeaponTurretVisualSnapshot created =
                new ShuttleWeaponTurretVisualSnapshot();
            output.Add(created);
            return created;
        }

        private void FillWeaponTurretVisualSnapshot(
            ShuttleWeaponTurretVisualSnapshot snapshot,
            ShuttleModule module,
            ShuttleWeaponModuleDef weaponDef)
        {
            if (snapshot == null)
            {
                return;
            }

            ShuttleWeaponRuntimeState weaponState = this.TryGetWeaponRuntimeState(module);
            LocalTargetInfo forcedTarget = weaponState != null
                ? weaponState.GetForcedTargetForRuntimeOnly()
                : LocalTargetInfo.Invalid;
            LocalTargetInfo currentTarget = weaponState != null
                ? weaponState.GetCurrentTargetForRuntimeOnly()
                : LocalTargetInfo.Invalid;

            snapshot.ModuleInstanceID = module.ModuleInstanceID;
            snapshot.ModuleDefName = weaponDef.defName;
            snapshot.ParentSlotID = module.ParentSlotID;
            snapshot.ParentSlotIndex = ParseParentSlotIndex(module.ParentSlotID);
            snapshot.ForcedTarget = forcedTarget.IsValid
                ? forcedTarget
                : LocalTargetInfo.Invalid;
            snapshot.CurrentTarget = currentTarget.IsValid
                ? currentTarget
                : LocalTargetInfo.Invalid;
            snapshot.HoldFire = weaponState != null &&
                weaponState.GetHoldFireForRuntimeOnly();
            snapshot.HasWarmup = weaponState != null &&
                weaponState.HasWarmupTicksForRuntimeOnly();
            snapshot.HasCooldown = weaponState != null &&
                weaponState.HasCooldownTicksForRuntimeOnly();
            snapshot.HasActivePower = weaponState != null &&
                weaponState.HasActivePowerTicksForRuntimeOnly();
        }

        internal ShuttleWeaponRuntimeState TryGetWeaponRuntimeState(ShuttleModule module)
        {
            if (module == null ||
                this.runtimeState == null ||
                this.runtimeState.Modules == null)
            {
                return null;
            }

            IShuttleModuleRuntimeState payload;
            if (!this.runtimeState.Modules.TryGetState(
                    module.ModuleInstanceID,
                    ShuttleWeaponRuntimeSystem.WeaponRuntimeSystemKey,
                    out payload))
            {
                return null;
            }

            return payload as ShuttleWeaponRuntimeState;
        }

        private static int ParseParentSlotIndex(string parentSlotID)
        {
            const string Marker = ".slot.";
            if (string.IsNullOrEmpty(parentSlotID))
            {
                return -1;
            }

            int markerIndex = parentSlotID.LastIndexOf(Marker);
            if (markerIndex < 0)
            {
                return -1;
            }

            int index = markerIndex + Marker.Length;
            if (index >= parentSlotID.Length)
            {
                return -1;
            }

            int value = 0;
            for (; index < parentSlotID.Length; index++)
            {
                char c = parentSlotID[index];
                if (c < '0' || c > '9')
                {
                    return -1;
                }

                int digit = c - '0';
                if (value > (int.MaxValue - digit) / 10)
                {
                    return -1;
                }

                value = (value * 10) + digit;
            }

            return value;
        }
    }

    internal sealed class ShuttleWeaponTurretVisualSnapshot
    {
        internal string ModuleInstanceID;
        internal string ModuleDefName;
        internal string ParentSlotID;
        internal int ParentSlotIndex = -1;
        internal LocalTargetInfo ForcedTarget;
        internal LocalTargetInfo CurrentTarget;
        internal bool HoldFire;
        internal bool HasWarmup;
        internal bool HasCooldown;
        internal bool HasActivePower;

        internal LocalTargetInfo BestTarget
        {
            get
            {
                return this.ForcedTarget.IsValid
                    ? this.ForcedTarget
                    : this.CurrentTarget;
            }
        }
    }
}

using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Initializes the durable, backend-neutral ammo policy/source state. It does not reload,
    /// consume cargo, mutate a backend magazine, or select a runtime backend.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoStateInitializer
    {
        private readonly ShuttleWeaponAmmoDefinitionCatalog definitions;

        internal ShuttleWeaponAmmoStateInitializer(
            ShuttleWeaponAmmoDefinitionCatalog definitions)
        {
            this.definitions = definitions ?? new ShuttleWeaponAmmoDefinitionCatalog();
        }

        internal ShuttleWeaponAmmoState GetOrCreate(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState)
        {
            if (weaponState == null || !this.definitions.HasAmmoSystem(weaponDef))
            {
                return null;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.definitions.GetExtension(weaponDef);
            ShuttleWeaponAmmoState ammoState = weaponState.AmmoForRuntimeOnly;
            string defaultAmmoName = extension.ammoSet.defaultAmmo.defName;
            ammoState.Configure(
                moduleInstanceID,
                defaultAmmoName,
                this.GetEffectiveMagazineCapacity(extension.magazineCapacity),
                extension.autoReloadEnabledByDefault,
                false,
                extension.allowManualReload);
            // Executor selection is adaptive. Pawn reload is an authored fallback, not a separate
            // player policy switch. Logistics auto-feed was also a redundant source selector;
            // normalize both old persisted switches into the one automatic-reload policy.
            ammoState.SetManualReloadAllowed(extension.allowManualReload);
            if (ammoState.LogisticsAutoFeedEnabled)
            {
                ammoState.SetLogisticsAutoFeedEnabled(false);
            }
            if (this.definitions.FindAmmo(
                    weaponDef,
                    ammoState.SelectedAmmoDefName) == null)
            {
                ammoState.ChangeSelectedAmmoAndClearMagazine(defaultAmmoName);
            }

            return ammoState;
        }

        internal int GetEffectiveMagazineCapacity(int authoredCapacity)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                authoredCapacity > 0 ? authoredCapacity : 0,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponAmmoCapacityMultiplier,
                authoredCapacity > 0 ? 1 : 0,
                int.MaxValue);
        }

    }
}

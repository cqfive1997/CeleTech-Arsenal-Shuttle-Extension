using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Quotes loaded core-magazine ammunition before the owning weapon runtime payload is
    /// removed. It does not clear the magazine or create/place item stacks.
    /// </summary>
    internal sealed class ShuttleWeaponRemovalRefundContributor : IShuttleWeaponRemovalRefundDriver
    {
        private readonly CoreMagazineDriver coreMagazine;

        internal ShuttleWeaponRemovalRefundContributor(
            CoreMagazineDriver coreMagazine)
        {
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(
                new ShuttleWeaponAmmoDefinitionCatalog());
        }

        public bool TryCollect(
            ShuttleModuleRuntimeContext context,
            ShuttleModuleRemovalRefundCollector collector,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState weaponState = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            ShuttleWeaponAmmoState ammoState = weaponState != null
                ? weaponState.AmmoForRuntimeOnly
                : null;
            int loadedCount = ammoState != null ? ammoState.LoadedAmmoCount : 0;
            if (loadedCount <= 0)
            {
                return true;
            }

            ShuttleWeaponAmmoDef ammoDef =
                this.coreMagazine.GetSelectedAmmo(weaponDef, ammoState);
            if (ammoDef == null ||
                string.IsNullOrEmpty(ammoState.SelectedAmmoDefName) ||
                ammoDef.defName != ammoState.SelectedAmmoDefName ||
                ammoDef.AmmoThingDef == null ||
                collector == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_RemovalRefundUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            collector.Add(ammoDef.AmmoThingDef, loadedCount);
            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                Log.Message(
                    "[CeleTech Shuttle] WeaponRefundBaseline quote" +
                    " module=" + Safe(context.ModuleInstanceID) +
                    " weapon=" + Safe(weaponDef.defName) +
                    " ammo=" + Safe(ammoDef.AmmoThingDef.defName) +
                    " loaded=" + loadedCount);
            }

            return true;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<unknown>" : value;
        }
    }
}

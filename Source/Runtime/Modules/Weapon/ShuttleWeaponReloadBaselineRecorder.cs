using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Dev-only completion trace for the pre-Native reload behavior gate. It is inert during
    /// normal play and owns no reload policy or durable state.
    /// </summary>
    internal static class ShuttleWeaponReloadBaselineRecorder
    {
        internal static void RecordCompletion(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            ShuttleWeaponAmmoDef ammoDef,
            IShuttleWeaponAmmoSupply supply,
            ShuttleWeaponReloadRequestKind completedRequestKind,
            ShuttleWeaponReloadExecutorKind completedExecutorKind,
            int loadedBefore,
            int requested,
            int committed,
            bool fullReload)
        {
            if (!ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns ||
                context == null ||
                weaponDef == null ||
                ammoState == null)
            {
                return;
            }

            ThingDef ammoThingDef = ammoDef != null ? ammoDef.AmmoThingDef : null;
            int sourceRemaining = supply != null && ammoThingDef != null
                ? supply.CountAvailable(ammoThingDef)
                : -1;
            string ammoThingDefName = ammoThingDef != null
                ? ammoThingDef.defName
                : "<none>";

            Log.Message(
                "[CeleTech Shuttle] WeaponReloadBaseline complete" +
                " module=" + Safe(context.ModuleInstanceID) +
                " weapon=" + Safe(weaponDef.defName) +
                " tick=" + context.TicksGame +
                " request=" + completedRequestKind +
                " executor=" + completedExecutorKind +
                " source=" + (supply != null ? supply.GetType().Name : "<none>") +
                " ammo=" + ammoThingDefName +
                " loaded=" + loadedBefore + "->" + ammoState.LoadedAmmoCount +
                "/" + ammoState.MagazineCapacity +
                " requested=" + requested +
                " committed=" + committed +
                " sourceRemaining=" + sourceRemaining +
                " full=" + fullReload +
                " requestRetained=" + ammoState.ReloadRequested +
                " nextRequest=" + ammoState.ReloadRequestKind);
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<unknown>" : value;
        }
    }
}

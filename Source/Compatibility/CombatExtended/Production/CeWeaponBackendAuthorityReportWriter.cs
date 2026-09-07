using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Formats selected-shuttle backend and single-magazine authority evidence only.
    /// </summary>
    internal sealed class CeWeaponBackendAuthorityReportWriter
    {
        internal void AppendSelectedShuttle(StringBuilder report)
        {
            ThingWithComps host = Find.Selector.SingleSelectedThing as ThingWithComps;
            CompModularShuttleCore core = host != null
                ? host.GetComp<CompModularShuttleCore>()
                : null;
            ShuttleAssemblyState assembly = core != null && core.Controller != null
                ? core.Controller.AssemblyState
                : null;
            if (report == null)
            {
                return;
            }

            if (assembly == null || assembly.Modules == null)
            {
                report.AppendLine(
                    "[CeleTech Shuttle][CE Backend] selectedShuttle=false");
                return;
            }

            IShuttleCargoResourceBroker cargoBroker =
                core.Controller.GetCargoResourceBrokerForInternalTransactions();

            for (int i = 0; i < assembly.Modules.Count; i++)
            {
                ShuttleModule module = assembly.Modules[i];
                ShuttleWeaponModuleDef weaponDef = module != null
                    ? module.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                if (module == null || weaponDef == null ||
                    CeWeaponCompatibilitySpec.ForWeapon(weaponDef.defName) == null)
                {
                    continue;
                }

                this.AppendModule(report, core, cargoBroker, module, weaponDef);
            }
        }

        private void AppendModule(
            StringBuilder report,
            CompModularShuttleCore core,
            IShuttleCargoResourceBroker cargoBroker,
            ShuttleModule module,
            ShuttleWeaponModuleDef weaponDef)
        {
            ShuttleWeaponRuntimeState state =
                core.Controller.TryGetWeaponRuntimeState(module);
            Thing gun = state != null ? state.GunForRuntimeOnly : null;
            CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(gun);
            ShuttleWeaponAmmoState coreAmmo = state != null
                ? state.AmmoForRuntimeOnly
                : null;
            AmmoDef selectedAmmo = magazine != null
                ? magazine.SelectedAmmo ?? magazine.CurrentAmmo
                : null;
            int cargoAmmo = cargoBroker != null && selectedAmmo != null
                ? cargoBroker.CountStored(selectedAmmo)
                : 0;
            ShuttleWeaponBackendBinding selectedBinding;
            ShuttleWeaponCompatibilityReport selectedReport;
            ShuttleWeaponBackendProbeContext context =
                new ShuttleWeaponBackendProbeContext(
                    weaponDef,
                    gun,
                    state != null
                        ? state.MagazineAuthorityBackendIdForRuntimeOnly
                        : null,
                    state != null &&
                        state.IsMagazineAuthorityTransferIdleForRuntimeOnly());
            bool resolved = ShuttleWeaponBackendRegistry.Shared.TryResolve(
                context,
                out selectedBinding,
                out selectedReport);
            ShuttleModuleRuntimeContext runtimeContext = new ShuttleModuleRuntimeContext(
                core.parent as ThingWithComps,
                core.Controller.GetProfileForRead(),
                core.Controller.GetLaunchRuntimeState(),
                module,
                null,
                state,
                null,
                null,
                cargoBroker,
                Find.TickManager != null ? Find.TickManager.TicksGame : -1);
            ShuttleWeaponAmmoSnapshot readSnapshot;
            bool readResolved = ShuttleWeaponRuntimeSystem.Instance.TryBuildAmmoSnapshot(
                runtimeContext,
                out readSnapshot);
            ShuttleWeaponBackendBinding cachedBinding = null;
            bool cachedResolved = state != null &&
                state.TryGetBackendBindingForRuntimeOnly(weaponDef, out cachedBinding);
            bool legacyRuntimeActive =
                (selectedBinding != null && selectedBinding.BackendId == "legacy") ||
                (cachedResolved && cachedBinding != null && cachedBinding.BackendId == "legacy");
            bool cutoverInvariant = state != null &&
                state.MagazineAuthorityBackendIdForRuntimeOnly == CeWeaponBackendFactory.Id &&
                magazine != null &&
                coreAmmo != null &&
                coreAmmo.LoadedAmmoCount == 0 &&
                selectedBinding != null &&
                selectedBinding.BackendId == CeWeaponBackendFactory.Id &&
                (!cachedResolved ||
                    (cachedBinding != null &&
                     cachedBinding.BackendId == CeWeaponBackendFactory.Id)) &&
                readResolved &&
                readSnapshot != null &&
                readSnapshot.CeAmmoModeActive &&
                readSnapshot.LoadedAmmoCount == magazine.CurMagCount &&
                readSnapshot.MagazineCapacity == magazine.MagSize;

            report.Append("[CeleTech Shuttle][CE Backend] module=")
                .Append(module.ModuleInstanceID)
                .Append(" moduleDef=").Append(weaponDef.defName)
                .Append(" authority=").Append(state != null
                    ? state.MagazineAuthorityBackendIdForRuntimeOnly
                    : "<missing>")
                .Append(" gun=").Append(gun != null && gun.def != null
                    ? gun.def.defName
                    : "<missing>")
                .Append(" ceLoaded=").Append(magazine != null
                    ? magazine.CurMagCount.ToString()
                    : "<missing>")
                .Append('/').Append(magazine != null
                    ? magazine.MagSize.ToString()
                    : "<missing>")
                .Append(" coreAmmo=").Append(coreAmmo != null
                    ? coreAmmo.SelectedAmmoDefName ?? "<none>"
                    : "<missing>")
                .Append(':').Append(coreAmmo != null
                    ? coreAmmo.LoadedAmmoCount.ToString()
                    : "<missing>")
                .Append(" cargoAmmo=").Append(cargoAmmo)
                .Append(" autoReload=").Append(coreAmmo != null &&
                    coreAmmo.AutoReloadEnabled)
                .Append(" logisticsAutoFeed=").Append(coreAmmo != null &&
                    coreAmmo.LogisticsAutoFeedEnabled)
                .Append(" manualReloadAllowed=").Append(coreAmmo != null &&
                    coreAmmo.ManualReloadAllowed)
                .Append(" logisticsCore=").Append(readResolved && readSnapshot != null &&
                    readSnapshot.LogisticsCoreAvailable)
                .Append(" reloadRequested=").Append(coreAmmo != null && coreAmmo.ReloadRequested)
                .Append(':').Append(coreAmmo != null
                    ? coreAmmo.ReloadRequestKind.ToString()
                    : "<missing>")
                .Append(" reloadWork=").Append(coreAmmo != null
                    ? coreAmmo.ReloadWorkDone.ToString()
                    : "<missing>")
                .Append('/').Append(coreAmmo != null
                    ? coreAmmo.ReloadWorkTotal.ToString()
                    : "<missing>")
                .Append(" reloadExecutor=").Append(coreAmmo != null
                    ? coreAmmo.ReloadExecutorKind.ToString()
                    : "<missing>")
                .Append(" reloadBlocker=").Append(coreAmmo != null &&
                    !string.IsNullOrEmpty(coreAmmo.LastReloadBlockerReason)
                        ? coreAmmo.LastReloadBlockerReason
                        : "<none>")
                .Append(" readModel=").Append(readResolved && readSnapshot != null
                    ? readSnapshot.LoadedAmmoCount + "/" + readSnapshot.MagazineCapacity
                    : "<unresolved>")
                .Append(" readCe=").Append(readResolved && readSnapshot != null &&
                    readSnapshot.CeAmmoModeActive)
                .Append(" readCanReload=").Append(readResolved && readSnapshot != null &&
                    readSnapshot.CanReload)
                .Append(" readCanCancel=").Append(readResolved && readSnapshot != null &&
                    readSnapshot.CanCancelReload)
                .Append(" cached=").Append(cachedResolved && cachedBinding != null
                    ? cachedBinding.BackendId
                    : "<none>")
                .Append(" resolved=").Append(resolved)
                .Append(" selected=").Append(selectedBinding != null
                    ? selectedBinding.BackendId
                    : "<none>")
                .Append(" selectedReport=").Append(FormatReport(selectedReport))
                .Append(" legacyRuntimeActive=").Append(legacyRuntimeActive)
                .Append(" cutoverInvariant=").Append(cutoverInvariant)
                .AppendLine();
        }

        private static string FormatReport(ShuttleWeaponCompatibilityReport report)
        {
            return report == null
                ? "null"
                : report.Support + ":" + report.ReasonCode;
        }
    }
}

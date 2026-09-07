using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using Verse;

namespace MyCoolMod.ShuttleRuntimeSample
{
    [StaticConstructorOnStartup]
    public static class MyModShuttleRuntimeBootstrap
    {
        static MyModShuttleRuntimeBootstrap()
        {
            ShuttleRuntimeAPI.RegisterRuntimeSystem(
                "my.cool.mod",
                "alien-scanner-runtime",
                new AlienScannerRuntimeSystem(),
                "CT_Shuttle_Sample_AlienScanner_Runtime_Label",
                "CT_Shuttle_Sample_AlienScanner_Runtime_Description");

            ShuttleUIAPI.RegisterModulePanelProvider(
                "my.cool.mod",
                "alien-scanner-panel",
                new AlienScannerPanelProvider());

            ShuttleCommandAPI.RegisterCommandHandler(
                "my.cool.mod",
                "set-scan-mode",
                new SetScanModeCommandHandler());
        }
    }

    public sealed class AlienScannerRuntimeSystem : ShuttleExternalModuleRuntimeSystemBase
    {
        private const int ScanIntervalTicks = 250;
        private const float ScanEnergyCostWd = 50f;
        private const float ScannerPowerDemandWatts = 25f;

        public override int TickInterval
        {
            get
            {
                return 60;
            }
        }

        public override int StateSchemaVersion
        {
            get
            {
                return 2;
            }
        }

        public override bool AppliesTo(ShuttleExternalModuleInfo module)
        {
            return module != null && module.ModuleTypeId == "scanner";
        }

        public override bool AppliesTo(ShuttleExternalLaunchModuleInfo module)
        {
            return module != null && module.ModuleTypeId == "scanner";
        }

        public override void Initialize(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetInt("scanCount", 0);
            context.State.SetInt("nextScanTick", context.TicksGame + ScanIntervalTicks);
            context.State.SetBool("installed", true);
            context.State.SetBool("launchNotified", false);
        }

        public override void Migrate(
            ShuttleExternalRuntimeContext context,
            int loadedSchemaVersion)
        {
            if (context == null)
            {
                return;
            }

            if (loadedSchemaVersion < 2)
            {
                context.State.SetBool("migratedToV2", true);
                if (!context.State.ContainsKey("nextScanTick"))
                {
                    context.State.SetInt("nextScanTick", context.TicksGame + ScanIntervalTicks);
                }
            }
        }

        public override void Reconcile(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetString("moduleDefName", context.Module != null ? context.Module.ModuleDefName : null);
            context.State.SetBool("internalBusPowered", context.InternalBusPowered);
            context.State.SetBool("supportsOccupantRead", context.SupportsOccupantRead);
            context.State.SetBool("supportsHostRead", context.SupportsHostRead);

            ShuttleExternalHostInfo hostInfo;
            if (context.TryGetHostInfo(out hostInfo))
            {
                context.State.SetString("hostThingId", hostInfo.ThingId);
                context.State.SetString("hostMapUniqueId", hostInfo.MapUniqueId.ToString());
            }

            IReadOnlyList<ShuttleExternalOccupantInfo> serviceableOccupants =
                context.GetOccupants(new ShuttleExternalOccupantQuery
                {
                    HumanlikeOnly = true,
                    ServiceableOnly = true
                });
            context.State.SetInt("serviceableHumanlikeOccupants", serviceableOccupants.Count);

            if (!context.State.ContainsKey("nextScanTick"))
            {
                context.State.SetInt("nextScanTick", context.TicksGame + ScanIntervalTicks);
            }
        }

        public override void CollectPowerDemand(ShuttleExternalPowerDemandContext context)
        {
            if (context == null)
            {
                return;
            }

            context.AddInternalPowerDemandWatts(ScannerPowerDemandWatts);
        }

        public override void Tick(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            int nextScanTick;
            if (!context.State.TryGetInt("nextScanTick", out nextScanTick))
            {
                nextScanTick = context.TicksGame;
            }

            if (context.TicksGame < nextScanTick)
            {
                return;
            }

            if (!context.TryConsumeStoredEnergyWd(ScanEnergyCostWd))
            {
                context.State.SetInt("nextScanTick", context.TicksGame + ScanIntervalTicks);
                return;
            }

            int scanCount;
            if (!context.State.TryGetInt("scanCount", out scanCount))
            {
                scanCount = 0;
            }

            context.State.SetInt("scanCount", scanCount + 1);
            context.State.SetInt("nextScanTick", context.TicksGame + ScanIntervalTicks);
        }

        public override bool CanRemove(
            ShuttleExternalRuntimeContext context,
            out string reason)
        {
            reason = null;
            if (context == null)
            {
                return true;
            }

            int scanCount;
            if (!context.State.TryGetInt("scanCount", out scanCount) || scanCount <= 0)
            {
                reason = "Alien scanner has not completed an initial scan.";
                return false;
            }

            return true;
        }

        public override bool PreLaunchValidate(
            ShuttleExternalLaunchValidationContext context,
            out string reason)
        {
            reason = null;

            int scanCount;
            if (context == null ||
                !context.State.TryGetInt("scanCount", out scanCount) ||
                scanCount <= 0)
            {
                reason = "Alien scanner has not completed a scan.";
                return false;
            }

            return true;
        }

        public override void OnInstalled(ShuttleExternalRuntimeContext context)
        {
            if (context != null)
            {
                context.State.SetBool("installed", true);
            }
        }

        public override void OnRemoved(ShuttleExternalRuntimeContext context)
        {
            if (context != null)
            {
                context.State.SetBool("installed", false);
            }
        }

        public override void OnLaunchSucceeded(ShuttleExternalLaunchContext context)
        {
            // Launch contexts are read-only in Runtime R3b. This hook may inspect state,
            // but it must not write launchNotified or any other state value here.
            int scanCount;
            bool launchNotified;
            if (context != null)
            {
                context.State.TryGetInt("scanCount", out scanCount);
                context.State.TryGetBool("launchNotified", out launchNotified);
            }
        }

        public override void OnArrived(ShuttleExternalRuntimeContext context)
        {
            if (context != null)
            {
                context.State.SetBool("launchNotified", true);
            }
        }
    }
}

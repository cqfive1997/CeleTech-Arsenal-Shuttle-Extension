using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal sealed class DBHModuleRuntime : ShuttleExternalModuleRuntimeSystemBase
    {
        private const int MaxServicedPawnsPerService = 4;
        private const int TicksPerDay = 60000;
        private const int MaxSepticTreatmentCatchupTicks = TicksPerDay;

        private readonly CT_Shuttle_DBHIntegrationDef integration;
        private readonly DBHReflectionBridge bridge;
        private readonly DBHPipeBridge pipeBridge;

        public DBHModuleRuntime(CT_Shuttle_DBHIntegrationDef integration, DBHReflectionBridge bridge)
        {
            this.integration = integration;
            this.bridge = bridge;
            this.pipeBridge = new DBHPipeBridge(integration);
        }

        public override int TickInterval
        {
            get { return DBHCompatibility.IsLoaded ? 60 : 0; }
        }

        public override int StateSchemaVersion
        {
            get { return 11; }
        }

        public override bool AppliesTo(ShuttleExternalModuleInfo module)
        {
            return module != null &&
                DBHIntegrationResolver.ModuleHasHygieneSuiteExtension(module.ModuleDefName, integration);
        }

        public override bool AppliesTo(ShuttleExternalLaunchModuleInfo module)
        {
            return module != null &&
                DBHIntegrationResolver.ModuleHasHygieneSuiteExtension(module.ModuleDefName, integration);
        }

        public override void Initialize(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetBool("dbhLoaded", DBHCompatibility.IsLoaded);
            context.State.SetBool("bridgeResolved", false);
            context.State.SetBool("supportActive", false);
            context.State.SetBool("pipeConnected", false);
            context.State.SetBool("waterPipeConnected", false);
            context.State.SetBool("sewagePipeConnected", false);
            DBHLiquidUtility.InitializeTankState(context.State, integration);
            context.State.SetInt("servicedPawns", 0);
            context.State.SetInt("serviceActions", 0);
            context.State.SetInt("serviceableOccupants", 0);
            context.State.SetInt("showerUses", 0);
            context.State.SetInt("toiletUses", 0);
            context.State.SetFloat("dumpedSewageAmount", 0f);
            context.State.SetInt("lastPipeCheckTick", -1);
            context.State.SetInt("lastAutoPipeCheckTick", -1);
            context.State.SetInt("lastForcedPipeCheckTick", -1);
            context.State.SetInt("nextPipeCheckTick", context.TicksGame + PipeCheckIntervalTicks);
            context.State.SetFloat("pipeWaterAdded", 0f);
            context.State.SetFloat("pipeSewageDrained", 0f);
            context.State.SetFloat("lastPipeWaterAdded", 0f);
            context.State.SetInt("lastPipeWaterRefillTick", -1);
            context.State.SetFloat("lastPipeSewageDrained", 0f);
            context.State.SetInt("lastPipeSewageDrainTick", -1);
            context.State.SetBool("septicTreatmentEnabled", SepticTreatmentEnabled);
            context.State.SetBool("septicTreatmentActive", false);
            context.State.SetFloat("septicTreatmentRatePerDay", DefaultSepticTreatmentRatePerDay);
            context.State.SetFloat("septicTreatmentRateMultiplier", 1f);
            context.State.SetFloat("septicTreatmentEffectiveRatePerDay", DefaultSepticTreatmentRatePerDay);
            context.State.SetFloat("septicCleanWaterRecoveryRatio", SepticCleanWaterRecoveryRatio);
            context.State.SetFloat("sewageTreatedAmount", 0f);
            context.State.SetFloat("lastSewageTreated", 0f);
            context.State.SetFloat("septicWaterRecoveredAmount", 0f);
            context.State.SetFloat("lastSepticWaterRecovered", 0f);
            context.State.SetInt("lastSepticTreatmentTick", context.TicksGame);
            context.State.SetString("lastSepticTreatmentResult", Tr("CT_Shuttle_Addon_DBH_Result_Initialized"));
            context.State.SetString("septicTreatmentDiagnostic", null);
            context.State.SetInt("lastDumpTick", -1);
            context.State.SetInt("nextServiceTick", context.TicksGame + ServiceIntervalTicks);
            context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Result_Initialized"));
            context.State.SetString("blockedReason", null);
            context.State.SetString("pipeBridgeMode", Tr("CT_Shuttle_Addon_DBH_PipeMode_NotChecked"));
            context.State.SetString("pipeBridgeDiagnostic", null);
            context.State.SetBool("forcePipeCheckNow", false);
            context.State.SetBool("cargoWaterRefillAvailable", false);
            context.State.SetBool("cargoWaterRefillConfigured", false);
            context.State.SetBool("cargoWaterDefLoaded", false);
            context.State.SetBool("cargoWaterConsumeApiAvailable", false);
            context.State.SetString("cargoWaterRefillMode", Tr("CT_Shuttle_Addon_DBH_CargoWater_InternalTankOnly"));
            context.State.SetString("lastCargoWaterRefillResult", Tr("CT_Shuttle_Addon_DBH_CargoWater_DisabledResult"));
            context.State.SetInt("lastCargoWaterCheckTick", -1);
            context.State.SetInt("lastCargoWaterRefillTick", -1);
            context.State.SetFloat("cargoWaterAdded", 0f);
            context.State.SetInt("cargoWaterItemsConsumed", 0);
            context.State.SetString("missingNeedDefs", null);
            context.State.SetString("optionalMissingNeedDefs", null);
            context.State.SetString("dumpMethod", "none");
            context.State.SetString("bladderDirection", bridge != null ? bridge.BladderDirectionForDisplay : "unknown");
            context.State.SetString("lastServicedPawnLabels", null);
            UpdateRuntimeMetadataState(context);
        }

        public override void Migrate(ShuttleExternalRuntimeContext context, int loadedSchemaVersion)
        {
            if (context == null)
            {
                return;
            }

            DBHLiquidUtility.EnsureTankState(context.State, integration);
            EnsureSepticTreatmentState(context);
        }

        public override void Reconcile(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            bool dbhLoaded = DBHCompatibility.IsLoaded;
            context.State.SetBool("dbhLoaded", dbhLoaded);
            context.State.SetBool("internalBusPowered", context.InternalBusPowered);
            EnsureTankState(context);
            EnsureSepticTreatmentState(context);
            UpdateRuntimeMetadataState(context);

            if (!dbhLoaded)
            {
                context.State.SetBool("supportActive", false);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing"));
                return;
            }

            bool resolved = bridge != null && bridge.Resolve();
            context.State.SetBool("bridgeResolved", resolved);
            if (!resolved)
            {
                context.State.SetBool("supportActive", false);
                context.State.SetString("missingNeedDefs", bridge != null ? bridge.MissingNeedDefs : null);
                context.State.SetString("optionalMissingNeedDefs", bridge != null ? bridge.OptionalMissingNeedDefs : null);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_NeedDefsMissing"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Block_NeedDefsMissingWithNames", MissingNeedDefsForDisplay()));
                return;
            }

            context.State.SetString("missingNeedDefs", null);
            context.State.SetString("optionalMissingNeedDefs", bridge != null ? bridge.OptionalMissingNeedDefs : null);
        }

        private void UpdateRuntimeMetadataState(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetString("runtimeLabel", ResolveTranslation(integration != null ? integration.runtimeLabelKey : null, Tr("CT_Shuttle_Addon_DBH_Runtime_Label")));
            context.State.SetString("runtimeDescription", ResolveTranslation(integration != null ? integration.runtimeDescriptionKey : null, Tr("CT_Shuttle_Addon_DBH_Runtime_Description")));
            context.State.SetString("runtimeSystemKey", integration != null ? integration.runtimeSystemKey : null);

            DBHBindingDiagnostic diagnostic = DBHIntegrationResolver.BuildHygieneSuiteBindingDiagnostic(integration);
            context.State.SetString("runtimeBindingDiagnostic", diagnostic != null ? diagnostic.Summary : null);
            context.State.SetString("runtimeBindingDiagnosticDev", diagnostic != null ? diagnostic.DevDetails : null);
        }

        public override void CollectMassContribution(ShuttleExternalMassContributionContext context)
        {
            if (context == null || context.State == null)
            {
                return;
            }

            float cleanWaterLiters = DBHLiquidUtility.ReadCleanWaterLiters(context.State, 0f);
            float sewageLiters = DBHLiquidUtility.ReadSewageLiters(context.State, 0f);
            float massKg = DBHLiquidUtility.CalculateLiquidMassKg(cleanWaterLiters, sewageLiters, integration);
            context.AddMassKg(massKg, "CT_Shuttle_Addon_DBH_LiquidMass", Tr("CT_Shuttle_Addon_DBH_LiquidMass"));
        }

        public override void Tick(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            int nextServiceTick;
            bool forcePipeCheckNow;
            context.State.SetBool("dbhLoaded", DBHCompatibility.IsLoaded);
            EnsureTankState(context);
            EnsureSepticTreatmentState(context);
            ProcessSepticTreatmentIfDue(context);

            if (context.State.TryGetBool("forcePipeCheckNow", out forcePipeCheckNow) && forcePipeCheckNow)
            {
                TryRunPipeCheck(context, true);
                context.State.SetBool("forcePipeCheckNow", false);
                return;
            }

            RunAutoPipeCheckIfDue(context);

            if (!context.State.TryGetInt("nextServiceTick", out nextServiceTick))
            {
                nextServiceTick = context.TicksGame;
            }

            if (context.TicksGame < nextServiceTick)
            {
                return;
            }

            context.State.SetInt("nextServiceTick", context.TicksGame + ServiceIntervalTicks);

            if (!DBHCompatibility.IsLoaded)
            {
                context.State.SetBool("supportActive", false);
                context.State.SetInt("servicedPawns", 0);
                context.State.SetInt("serviceActions", 0);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing"));
                return;
            }

            if (!context.InternalBusPowered)
            {
                context.State.SetBool("supportActive", false);
                context.State.SetInt("servicedPawns", 0);
                context.State.SetInt("serviceActions", 0);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_InternalBusOffline"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Block_InternalBusOffline"));
                return;
            }

            if (bridge == null || !bridge.Resolve())
            {
                context.State.SetBool("supportActive", false);
                context.State.SetInt("servicedPawns", 0);
                context.State.SetInt("serviceActions", 0);
                context.State.SetString("missingNeedDefs", bridge != null ? bridge.MissingNeedDefs : null);
                context.State.SetString("optionalMissingNeedDefs", bridge != null ? bridge.OptionalMissingNeedDefs : null);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_BridgeNeedDefs"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Block_BridgeNeedDefsWithNames", MissingNeedDefsForDisplay()));
                return;
            }

            context.State.SetBool("bridgeResolved", true);
            context.State.SetString("missingNeedDefs", null);
            context.State.SetString("optionalMissingNeedDefs", bridge.OptionalMissingNeedDefs);
            context.State.SetString("blockedReason", null);

            float sewage = ReadSewageLiters(context);
            if (sewage >= ReadSewageCapacityLiters(context) && !TryDumpSewage(context))
            {
                context.State.SetBool("supportActive", false);
                context.State.SetInt("servicedPawns", 0);
                context.State.SetInt("serviceActions", 0);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_SepticFull"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Result_SepticFullCannotDump"));
                return;
            }

            if (!context.SupportsOccupantRead)
            {
                context.State.SetBool("supportActive", false);
                context.State.SetInt("servicedPawns", 0);
                context.State.SetInt("serviceActions", 0);
                context.State.SetInt("serviceableOccupants", 0);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_OccupantApi"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Block_OccupantApi"));
                return;
            }

            long occupantServiceStarted = DBHRuntimePerformanceMetrics.Start(context.TicksGame);
            try
            {
                ServiceOccupants(context);
            }
            finally
            {
                DBHRuntimePerformanceMetrics.Record(
                    DBHPerformanceSection.OccupantService,
                    occupantServiceStarted);
            }

            sewage = ReadSewageLiters(context);
            if (sewage >= ReadSewageCapacityLiters(context) && !TryDumpSewage(context))
            {
                context.State.SetBool("supportActive", false);
                context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_SepticFull"));
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Result_SepticFullCannotDump"));
            }
        }

        private void ServiceOccupants(ShuttleExternalRuntimeContext context)
        {
            ShuttleExternalHostInfo hostInfo;
            context.TryGetHostInfo(out hostInfo);
            IReadOnlyList<ShuttleExternalOccupantInfo> occupants = context.GetOccupants(
                new ShuttleExternalOccupantQuery
                {
                    RoleMask = DBHOccupantServicePolicy.SupportedRoles,
                    HumanlikeOnly = true,
                    ServiceableOnly = true
                });

            int serviceableCount = CountServiceableHumanlikeOccupants(occupants, hostInfo);
            context.State.SetInt("serviceableOccupants", serviceableCount);
            context.State.SetInt("servicedPawns", 0);
            context.State.SetInt("serviceActions", 0);
            context.State.SetString("lastServicedPawnLabels", null);

            int servicedPawnCount = 0;
            int serviceActions = 0;
            string lastServiceBlockReason = null;
            bool anyOccupantNeededService = false;
            bool prisonerMissingDbhNeed = false;
            bool patientMissingDbhNeed = false;
            List<string> servicedLabels = new List<string>();
            for (int i = 0; i < occupants.Count && servicedPawnCount < MaxServicedPawnsPerService; i++)
            {
                ShuttleExternalOccupantInfo occupant = occupants[i];
                Pawn pawn = occupant != null ? occupant.UnsafeLivePawn : null;
                if (!DBHOccupantServicePolicy.CanService(occupant, pawn, hostInfo))
                {
                    continue;
                }

                bool hasHygiene;
                bool hasBladder;
                if (bridge.TryGetNeedAvailability(pawn, out hasHygiene, out hasBladder) &&
                    (!hasHygiene || !hasBladder))
                {
                    prisonerMissingDbhNeed = prisonerMissingDbhNeed || occupant.IsPrisonCellPrisoner;
                    patientMissingDbhNeed = patientMissingDbhNeed || occupant.IsMedicalPatient;
                }

                bool servicedThisPawn = false;
                bool neededService;
                string blockReason;
                if (TryServiceShower(context, pawn, out neededService, out blockReason))
                {
                    serviceActions++;
                    servicedThisPawn = true;
                }
                anyOccupantNeededService = anyOccupantNeededService || neededService;
                if (!string.IsNullOrEmpty(blockReason))
                {
                    lastServiceBlockReason = blockReason;
                }

                if (TryServiceToilet(context, pawn, out neededService, out blockReason))
                {
                    serviceActions++;
                    servicedThisPawn = true;
                }
                anyOccupantNeededService = anyOccupantNeededService || neededService;
                if (!string.IsNullOrEmpty(blockReason))
                {
                    lastServiceBlockReason = blockReason;
                }

                if (servicedThisPawn)
                {
                    servicedPawnCount++;
                    AddPawnLabel(servicedLabels, pawn);
                }
            }

            context.State.SetInt("servicedPawns", servicedPawnCount);
            context.State.SetInt("serviceActions", serviceActions);
            string serviceBlockReason = serviceActions > 0
                ? null
                : ResolveServiceBlockReason(anyOccupantNeededService, lastServiceBlockReason);
            string noServiceResult = ResolveNoServiceResult(
                serviceableCount,
                anyOccupantNeededService,
                serviceBlockReason);
            if (serviceActions <= 0 &&
                !anyOccupantNeededService &&
                string.IsNullOrEmpty(serviceBlockReason))
            {
                if (prisonerMissingDbhNeed)
                {
                    noServiceResult = Tr("CT_Shuttle_Addon_DBH_Result_PrisonerNeedsMissing");
                }
                else if (patientMissingDbhNeed)
                {
                    noServiceResult = Tr("CT_Shuttle_Addon_DBH_Result_PatientNeedsMissing");
                }
            }

            context.State.SetBool("supportActive", serviceActions > 0 || string.IsNullOrEmpty(serviceBlockReason));
            context.State.SetString("lastServicedPawnLabels", servicedLabels.Count > 0 ? string.Join(", ", servicedLabels.ToArray()) : null);
            context.State.SetString("blockedReason", serviceBlockReason);
            context.State.SetString(
                "lastResult",
                serviceActions > 0
                    ? Tr("CT_Shuttle_Addon_DBH_Result_ServicePerformed", serviceActions.ToString(), servicedPawnCount.ToString())
                    : noServiceResult);
        }

        private bool TryServiceShower(
            ShuttleExternalRuntimeContext context,
            Pawn pawn,
            out bool neededService,
            out string blockReason)
        {
            neededService = false;
            blockReason = null;
            float hygiene;
            float bladder;
            float thirst;
            if (!bridge.TryReadNeeds(pawn, out hygiene, out bladder, out thirst) ||
                hygiene < 0f ||
                hygiene >= ShowerHygieneThreshold)
            {
                return false;
            }

            neededService = true;
            if (!CanSpendWaterAndStoreSewage(context, ShowerWaterCost, ShowerSewageOutput, out blockReason))
            {
                return false;
            }

            if (!bridge.TryRaiseHygiene(pawn, ShowerHygieneGain))
            {
                return false;
            }

            SpendWaterAndStoreSewage(context, ShowerWaterCost, ShowerSewageOutput);
            AddInt(context, "showerUses", 1);
            return true;
        }

        private bool TryServiceToilet(
            ShuttleExternalRuntimeContext context,
            Pawn pawn,
            out bool neededService,
            out string blockReason)
        {
            neededService = false;
            blockReason = null;
            float bladder;
            if (!bridge.TryReadBladder(pawn, out bladder) ||
                bladder < 0f ||
                bladder > BladderServiceThreshold)
            {
                return false;
            }

            neededService = true;
            if (!CanSpendWaterAndStoreSewage(context, ToiletWaterCost, ToiletSewageOutput, out blockReason))
            {
                return false;
            }

            if (!bridge.TryServiceBladder(pawn, BladderServiceTarget))
            {
                return false;
            }

            SpendWaterAndStoreSewage(context, ToiletWaterCost, ToiletSewageOutput);
            AddInt(context, "toiletUses", 1);
            return true;
        }

        private bool CanSpendWaterAndStoreSewage(
            ShuttleExternalRuntimeContext context,
            float waterCost,
            float sewageOutput,
            out string blockReason)
        {
            blockReason = null;
            float cleanWater = ReadCleanWaterLiters(context);
            if (cleanWater < waterCost)
            {
                blockReason = cleanWater <= 0f
                    ? Tr("CT_Shuttle_Addon_DBH_Block_CleanWaterEmpty")
                    : Tr("CT_Shuttle_Addon_DBH_Block_CleanWaterInsufficient");
                context.State.SetString("blockedReason", blockReason);
                return false;
            }

            return EnsureSewageCapacityForService(context, sewageOutput, out blockReason);
        }

        private bool EnsureSewageCapacityForService(
            ShuttleExternalRuntimeContext context,
            float sewageOutput,
            out string blockReason)
        {
            blockReason = null;
            if (sewageOutput <= 0f)
            {
                return true;
            }

            float sewage = ReadSewageLiters(context);
            if (sewage + sewageOutput <= ReadSewageCapacityLiters(context))
            {
                return true;
            }

            TryDumpSewage(context);
            sewage = ReadSewageLiters(context);
            if (sewage + sewageOutput <= ReadSewageCapacityLiters(context))
            {
                return true;
            }

            blockReason = Tr("CT_Shuttle_Addon_DBH_Block_SepticCapacityInsufficient");
            context.State.SetString("blockedReason", blockReason);
            return false;
        }

        private void SpendWaterAndStoreSewage(
            ShuttleExternalRuntimeContext context,
            float waterCost,
            float sewageOutput)
        {
            float cleanWater = ReadCleanWaterLiters(context);
            float sewage = ReadSewageLiters(context);
            DBHLiquidUtility.WriteTankState(
                context.State,
                integration,
                ReadCleanWaterCapacityLiters(context),
                ReadSewageCapacityLiters(context),
                cleanWater - waterCost,
                sewage + sewageOutput);
        }

        private void EnsureTankState(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            DBHLiquidUtility.EnsureTankState(context.State, integration);
        }

        private void EnsureSepticTreatmentState(ShuttleExternalRuntimeContext context)
        {
            if (context == null || context.State == null)
            {
                return;
            }

            context.State.SetBool("septicTreatmentEnabled", SepticTreatmentEnabled);
            float rate;
            if (!context.State.TryGetFloat("septicTreatmentRatePerDay", out rate))
            {
                rate = DefaultSepticTreatmentRatePerDay;
            }

            context.State.SetFloat("septicTreatmentRatePerDay", ClampSepticTreatmentRatePerDay(rate));
            context.State.SetFloat("septicCleanWaterRecoveryRatio", SepticCleanWaterRecoveryRatio);
            int lastTick;
            if (!context.State.TryGetInt("lastSepticTreatmentTick", out lastTick) ||
                lastTick < 0 ||
                lastTick > context.TicksGame)
            {
                context.State.SetInt("lastSepticTreatmentTick", context.TicksGame);
            }

            float value;
            if (!context.State.TryGetFloat("sewageTreatedAmount", out value))
            {
                context.State.SetFloat("sewageTreatedAmount", 0f);
            }

            if (!context.State.TryGetFloat("lastSewageTreated", out value))
            {
                context.State.SetFloat("lastSewageTreated", 0f);
            }

            if (!context.State.TryGetFloat("septicWaterRecoveredAmount", out value))
            {
                context.State.SetFloat("septicWaterRecoveredAmount", 0f);
            }

            if (!context.State.TryGetFloat("lastSepticWaterRecovered", out value))
            {
                context.State.SetFloat("lastSepticWaterRecovered", 0f);
            }
        }

        private void ProcessSepticTreatmentIfDue(ShuttleExternalRuntimeContext context)
        {
            if (context == null || context.State == null)
            {
                return;
            }

            float effectiveRatePerDay = ReadSepticTreatmentRatePerDay(context);
            context.State.SetFloat("septicTreatmentRateMultiplier", 1f);
            context.State.SetFloat("septicTreatmentEffectiveRatePerDay", effectiveRatePerDay);
            context.State.SetString("septicTreatmentDiagnostic", null);

            if (!SepticTreatmentEnabled)
            {
                ResetSepticTreatmentClock(context, Tr("CT_Shuttle_Addon_DBH_Treatment_Disabled"));
                return;
            }

            if (!DBHCompatibility.IsLoaded)
            {
                ResetSepticTreatmentClock(context, Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing"));
                return;
            }

            if (!context.InternalBusPowered)
            {
                ResetSepticTreatmentClock(context, Tr("CT_Shuttle_Addon_DBH_Block_InternalBusOffline"));
                return;
            }

            if (effectiveRatePerDay <= 0f)
            {
                ResetSepticTreatmentClock(context, Tr("CT_Shuttle_Addon_DBH_Treatment_RateZero"));
                return;
            }

            int lastTick;
            if (!context.State.TryGetInt("lastSepticTreatmentTick", out lastTick))
            {
                context.State.SetInt("lastSepticTreatmentTick", context.TicksGame);
                return;
            }

            int elapsedTicks = context.TicksGame - lastTick;
            if (elapsedTicks <= 0)
            {
                context.State.SetBool("septicTreatmentActive", true);
                return;
            }

            if (elapsedTicks > MaxSepticTreatmentCatchupTicks)
            {
                elapsedTicks = MaxSepticTreatmentCatchupTicks;
            }

            float sewage = ReadSewageLiters(context);
            if (sewage <= 0f)
            {
                context.State.SetBool("septicTreatmentActive", true);
                context.State.SetFloat("lastSewageTreated", 0f);
                context.State.SetFloat("lastSepticWaterRecovered", 0f);
                context.State.SetInt("lastSepticTreatmentTick", context.TicksGame);
                context.State.SetString("lastSepticTreatmentResult", Tr("CT_Shuttle_Addon_DBH_Treatment_NoSewage"));
                return;
            }

            float treated = effectiveRatePerDay * elapsedTicks / TicksPerDay;
            if (treated <= 0f)
            {
                return;
            }

            if (treated > sewage)
            {
                treated = sewage;
            }

            float cleanWater = ReadCleanWaterLiters(context);
            float cleanWaterCapacity = ReadCleanWaterCapacityLiters(context);
            float recoveredCleanWater = Min(
                treated * SepticCleanWaterRecoveryRatio,
                Max(0f, cleanWaterCapacity - cleanWater));

            DBHLiquidUtility.WriteTankState(
                context.State,
                integration,
                cleanWaterCapacity,
                ReadSewageCapacityLiters(context),
                cleanWater + recoveredCleanWater,
                sewage - treated);
            AddFloat(context, "sewageTreatedAmount", treated);
            AddFloat(context, "septicWaterRecoveredAmount", recoveredCleanWater);
            context.State.SetFloat("lastSewageTreated", treated);
            context.State.SetFloat("lastSepticWaterRecovered", recoveredCleanWater);
            context.State.SetInt("lastSepticTreatmentTick", context.TicksGame);
            context.State.SetBool("septicTreatmentActive", true);
            context.State.SetString(
                "lastSepticTreatmentResult",
                Tr(
                    "CT_Shuttle_Addon_DBH_Treatment_Treated",
                    treated.ToString("0.##"),
                    recoveredCleanWater.ToString("0.##")));
        }

        private void ResetSepticTreatmentClock(ShuttleExternalRuntimeContext context, string result)
        {
            if (context == null || context.State == null)
            {
                return;
            }

            context.State.SetBool("septicTreatmentActive", false);
            context.State.SetFloat("lastSewageTreated", 0f);
            context.State.SetFloat("lastSepticWaterRecovered", 0f);
            context.State.SetInt("lastSepticTreatmentTick", context.TicksGame);
            context.State.SetString("lastSepticTreatmentResult", result);
        }

        private void RunAutoPipeCheckIfDue(ShuttleExternalRuntimeContext context)
        {
            int nextPipeCheckTick;
            if (!context.State.TryGetInt("nextPipeCheckTick", out nextPipeCheckTick))
            {
                nextPipeCheckTick = context.TicksGame;
            }

            if (context.TicksGame < nextPipeCheckTick)
            {
                return;
            }

            TryRunPipeCheck(context, false);
        }

        private bool TryRunPipeCheck(ShuttleExternalRuntimeContext context, bool forced)
        {
            if (context == null)
            {
                return false;
            }

            if (!DBHCompatibility.IsLoaded)
            {
                RecordPipeCheckSkipped(
                    context,
                    forced,
                    Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing"),
                    Tr("CT_Shuttle_Addon_DBH_PipeMode_DbhMissing"));
                return false;
            }

            if (!context.InternalBusPowered)
            {
                RecordPipeCheckSkipped(
                    context,
                    forced,
                    Tr("CT_Shuttle_Addon_DBH_Block_InternalBusOffline"),
                    Tr("CT_Shuttle_Addon_DBH_PipeMode_InternalBusOffline"));
                return false;
            }

            if (bridge == null || !bridge.Resolve())
            {
                context.State.SetBool("bridgeResolved", false);
                context.State.SetString("missingNeedDefs", bridge != null ? bridge.MissingNeedDefs : null);
                context.State.SetString("optionalMissingNeedDefs", bridge != null ? bridge.OptionalMissingNeedDefs : null);
                RecordPipeCheckSkipped(
                    context,
                    forced,
                    Tr("CT_Shuttle_Addon_DBH_Block_BridgeNeedDefsWithNames", MissingNeedDefsForDisplay()),
                    Tr("CT_Shuttle_Addon_DBH_PipeMode_NeedDefsMissing"));
                return false;
            }

            context.State.SetBool("bridgeResolved", true);
            context.State.SetString("missingNeedDefs", null);
            context.State.SetString("optionalMissingNeedDefs", bridge.OptionalMissingNeedDefs);
            RunPipeCheck(context, forced);
            return true;
        }

        private void RecordPipeCheckSkipped(
            ShuttleExternalRuntimeContext context,
            bool forced,
            string reason,
            string bridgeMode)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetInt("lastPipeCheckTick", context.TicksGame);
            context.State.SetBool("pipeConnected", false);
            context.State.SetBool("waterPipeConnected", false);
            context.State.SetBool("sewagePipeConnected", false);
            if (forced)
            {
                context.State.SetInt("lastForcedPipeCheckTick", context.TicksGame);
            }
            else
            {
                context.State.SetInt("lastAutoPipeCheckTick", context.TicksGame);
                context.State.SetInt("nextPipeCheckTick", context.TicksGame + PipeCheckIntervalTicks);
            }

            context.State.SetString("lastPipeResult", reason);
            context.State.SetString("pipeBlockedReason", reason);
            context.State.SetString("pipeBridgeMode", bridgeMode);
            context.State.SetString("pipeBridgeDiagnostic", "skipped=" + reason);
        }

        private void RunPipeCheck(ShuttleExternalRuntimeContext context, bool forced)
        {
            float cleanWater = ReadCleanWaterLiters(context);
            float sewage = ReadSewageLiters(context);
            DBHPipeBridgeResult pipeResult;
            long pipeBridgeStarted = DBHRuntimePerformanceMetrics.Start(context.TicksGame);
            try
            {
                pipeResult = pipeBridge.CheckAndService(
                    context,
                    cleanWater,
                    ReadCleanWaterCapacityLiters(context),
                    sewage,
                    ReadSewageCapacityLiters(context));
            }
            finally
            {
                DBHRuntimePerformanceMetrics.Record(
                    DBHPerformanceSection.PipeBridge,
                    pipeBridgeStarted);
            }

            ApplyPipeResult(context, pipeResult);
            context.State.SetInt("lastPipeCheckTick", context.TicksGame);
            if (forced)
            {
                context.State.SetInt("lastForcedPipeCheckTick", context.TicksGame);
            }
            else
            {
                context.State.SetInt("lastAutoPipeCheckTick", context.TicksGame);
                context.State.SetInt("nextPipeCheckTick", context.TicksGame + PipeCheckIntervalTicks);
            }

            if (forced)
            {
                float afterCleanWater = ReadCleanWaterLiters(context);
                context.State.SetString(
                    "lastPipeResult",
                    Tr(
                        "CT_Shuttle_Addon_DBH_Result_ForcedPipeCheck",
                        cleanWater.ToString("0.#"),
                        afterCleanWater.ToString("0.#"),
                        pipeResult.WaterAdded.ToString("0.#"),
                        pipeResult.Result ?? string.Empty));
            }
        }

        private bool TryDumpSewage(ShuttleExternalRuntimeContext context)
        {
            float cleanWater = ReadCleanWaterLiters(context);
            float sewage = ReadSewageLiters(context);
            if (sewage <= 0f)
            {
                return true;
            }

            if (integration != null && integration.allowPipeSewageDrain)
            {
                DBHPipeBridgeResult pipeResult = pipeBridge.TryDrainSewage(
                    context,
                    cleanWater,
                    ReadCleanWaterCapacityLiters(context),
                    sewage,
                    ReadSewageCapacityLiters(context));
                ApplyPipeResult(context, pipeResult);
                sewage = ReadSewageLiters(context);
                if (pipeResult.SewageDrained > 0f)
                {
                    return true;
                }
            }

            if (integration != null && integration.allowMapDump && TryDumpSewageToMap(context, sewage))
            {
                return true;
            }

            if (integration != null && integration.allowWorldDump && CanUseWorldDump(context))
            {
                AddFloat(context, "dumpedSewageAmount", sewage);
                DBHLiquidUtility.SetSewageLiters(context.State, integration, 0f);
                context.State.SetInt("lastDumpTick", context.TicksGame);
                context.State.SetString("blockedReason", null);
                context.State.SetString("dumpMethod", "world");
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Result_DumpWorld"));
                return true;
            }

            context.State.SetString("blockedReason", Tr("CT_Shuttle_Addon_DBH_Block_SepticFull"));
            context.State.SetString("dumpMethod", "failed");
            return false;
        }

        private static bool CanUseWorldDump(ShuttleExternalRuntimeContext context)
        {
            if (context == null || !context.SupportsHostRead)
            {
                return true;
            }

            ShuttleExternalHostInfo hostInfo;
            return !context.TryGetHostInfo(out hostInfo) ||
                hostInfo == null ||
                !hostInfo.Spawned ||
                hostInfo.UnsafeLiveMap == null;
        }

        private bool TryDumpSewageToMap(ShuttleExternalRuntimeContext context, float sewage)
        {
            if (context == null || !context.SupportsHostRead)
            {
                return false;
            }

            ShuttleExternalHostInfo hostInfo;
            if (!context.TryGetHostInfo(out hostInfo) ||
                hostInfo == null ||
                !hostInfo.Spawned ||
                hostInfo.UnsafeLiveMap == null ||
                hostInfo.AdjacentExternalCells == null ||
                hostInfo.AdjacentExternalCells.Count == 0)
            {
                return false;
            }

            IntVec3 dumpCell;
            if (!TryFindDumpCell(hostInfo, out dumpCell))
            {
                return false;
            }

            ThingDef sewageDef = ResolveSewageDef();
            if (sewageDef != null)
            {
                TrySpawnSewageThing(sewageDef, dumpCell, hostInfo.UnsafeLiveMap, sewage);
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Result_DumpMap"));
            }
            else
            {
                context.State.SetString("lastResult", Tr("CT_Shuttle_Addon_DBH_Result_DumpMapAbstract"));
            }

            AddFloat(context, "dumpedSewageAmount", sewage);
            DBHLiquidUtility.SetSewageLiters(context.State, integration, 0f);
            context.State.SetInt("lastDumpTick", context.TicksGame);
            context.State.SetString("blockedReason", null);
            context.State.SetString("dumpMethod", "map");
            return true;
        }

        private void ApplyPipeResult(ShuttleExternalRuntimeContext context, DBHPipeBridgeResult pipeResult)
        {
            if (context == null || pipeResult == null)
            {
                return;
            }

            context.State.SetBool("pipeConnected", pipeResult.PipeConnected);
            context.State.SetBool("waterPipeConnected", pipeResult.WaterPipeConnected);
            context.State.SetBool("sewagePipeConnected", pipeResult.SewagePipeConnected);
            DBHLiquidUtility.WriteTankState(
                context.State,
                integration,
                ReadCleanWaterCapacityLiters(context),
                ReadSewageCapacityLiters(context),
                pipeResult.CleanWater,
                pipeResult.Sewage);
            context.State.SetString("pipeBridgeMode", pipeResult.BridgeMode);
            context.State.SetString("pipeBridgeDiagnostic", BuildPipeDiagnostic(pipeResult));

            if (!string.IsNullOrEmpty(pipeResult.Result))
            {
                context.State.SetString("lastPipeResult", pipeResult.Result);
            }

            if (!string.IsNullOrEmpty(pipeResult.BlockedReason))
            {
                context.State.SetString("pipeBlockedReason", pipeResult.BlockedReason);
            }
            else
            {
                context.State.SetString("pipeBlockedReason", null);
            }

            if (pipeResult.SewageDrained > 0f)
            {
                AddFloat(context, "dumpedSewageAmount", pipeResult.SewageDrained);
                AddFloat(context, "pipeSewageDrained", pipeResult.SewageDrained);
                context.State.SetFloat("lastPipeSewageDrained", pipeResult.SewageDrained);
                context.State.SetInt("lastPipeSewageDrainTick", context.TicksGame);
                context.State.SetString("dumpMethod", "pipe");
                context.State.SetInt("lastDumpTick", context.TicksGame);
            }

            if (pipeResult.WaterAdded > 0f)
            {
                AddFloat(context, "pipeWaterAdded", pipeResult.WaterAdded);
                context.State.SetFloat("lastPipeWaterAdded", pipeResult.WaterAdded);
                context.State.SetInt("lastPipeWaterRefillTick", context.TicksGame);
                ClearCleanWaterBlockReason(context);
            }
        }

        private static string BuildPipeDiagnostic(DBHPipeBridgeResult pipeResult)
        {
            if (pipeResult == null)
            {
                return null;
            }

            List<string> parts = new List<string>();
            parts.Add("occupied=" + pipeResult.CheckedOccupiedCells);
            parts.Add("adjacent=" + pipeResult.CheckedAdjacentCells);
            parts.Add("mapComp=" + pipeResult.FoundMapComp);
            parts.Add("pipeGrid=" + pipeResult.FoundPipeGrid);
            parts.Add("netByMapComp=" + pipeResult.FoundNetByMapComp);
            parts.Add("netByThingComp=" + pipeResult.FoundNetByThingComp);
            parts.Add("pipeNet=" + pipeResult.PipeNetFound);
            if (!string.IsNullOrEmpty(pipeResult.ResolveFailureReason))
            {
                parts.Add("resolve=" + pipeResult.ResolveFailureReason);
            }

            if (!string.IsNullOrEmpty(pipeResult.PipeNetNullReason))
            {
                parts.Add("pipeNetNull=" + pipeResult.PipeNetNullReason);
            }

            if (!string.IsNullOrEmpty(pipeResult.PipeCell))
            {
                parts.Add("cell=" + pipeResult.PipeCell);
            }

            if (!string.IsNullOrEmpty(pipeResult.PipeTypeName))
            {
                parts.Add("pipeType=" + pipeResult.PipeTypeName);
            }

            if (!string.IsNullOrEmpty(pipeResult.WaterPipeCell))
            {
                parts.Add("waterCell=" + pipeResult.WaterPipeCell);
                parts.Add("waterPipeId=" + pipeResult.WaterPipeId);
            }

            if (!string.IsNullOrEmpty(pipeResult.SewagePipeCell))
            {
                parts.Add("sewageCell=" + pipeResult.SewagePipeCell);
                parts.Add("sewagePipeId=" + pipeResult.SewagePipeId);
            }

            if (pipeResult.PipeId > 0)
            {
                parts.Add("pipeId=" + pipeResult.PipeId);
            }

            if (!string.IsNullOrEmpty(pipeResult.PipeThingDefName))
            {
                parts.Add("thing=" + pipeResult.PipeThingDefName);
            }

            if (!string.IsNullOrEmpty(pipeResult.PipeCompTypeName))
            {
                parts.Add("comp=" + pipeResult.PipeCompTypeName);
            }

            if (!string.IsNullOrEmpty(pipeResult.LastContaminationLevel))
            {
                parts.Add("contam=" + pipeResult.LastContaminationLevel);
            }

            return string.Join("; ", parts.ToArray());
        }

        private int ServiceIntervalTicks
        {
            get
            {
                return integration != null && integration.serviceIntervalTicks > 0
                    ? integration.serviceIntervalTicks
                    : 600;
            }
        }

        private int PipeCheckIntervalTicks
        {
            get
            {
                return integration != null && integration.pipeCheckIntervalTicks > 0
                    ? integration.pipeCheckIntervalTicks
                    : 600;
            }
        }

        private bool SepticTreatmentEnabled
        {
            get { return integration == null || integration.allowSepticTreatment; }
        }

        private float DefaultSepticTreatmentRatePerDay
        {
            get
            {
                return integration != null
                    ? ClampSepticTreatmentRatePerDay(integration.septicTreatmentRatePerDay)
                    : 1500f;
            }
        }

        private float SepticCleanWaterRecoveryRatio
        {
            get
            {
                return integration != null
                    ? Clamp(integration.septicCleanWaterRecoveryRatio, 0f, 1f)
                    : 0.6f;
            }
        }

        private float MaxSepticTreatmentRatePerDay
        {
            get
            {
                return integration != null && integration.maxSepticTreatmentRatePerDay > 0f
                    ? integration.maxSepticTreatmentRatePerDay
                    : 10000f;
            }
        }

        private float ReadSepticTreatmentRatePerDay(ShuttleExternalRuntimeContext context)
        {
            float value;
            if (context != null &&
                context.State != null &&
                context.State.TryGetFloat("septicTreatmentRatePerDay", out value))
            {
                return ClampSepticTreatmentRatePerDay(value);
            }

            return DefaultSepticTreatmentRatePerDay;
        }

        private float ClampSepticTreatmentRatePerDay(float value)
        {
            return Clamp(value, 0f, MaxSepticTreatmentRatePerDay);
        }

        private float ReadCleanWaterLiters(ShuttleExternalRuntimeContext context)
        {
            return DBHLiquidUtility.ReadCleanWaterLiters(
                context != null ? context.State : null,
                ReadCleanWaterCapacityLiters(context) * 0.5f);
        }

        private float ReadSewageLiters(ShuttleExternalRuntimeContext context)
        {
            return DBHLiquidUtility.ReadSewageLiters(context != null ? context.State : null, 0f);
        }

        private float ReadCleanWaterCapacityLiters(ShuttleExternalRuntimeContext context)
        {
            return DBHLiquidUtility.ReadCleanWaterCapacityLiters(
                context != null ? context.State : null,
                integration);
        }

        private float ReadSewageCapacityLiters(ShuttleExternalRuntimeContext context)
        {
            return DBHLiquidUtility.ReadSewageCapacityLiters(
                context != null ? context.State : null,
                integration);
        }

        private float ShowerHygieneThreshold
        {
            get { return integration != null ? Clamp(integration.showerHygieneThreshold, 0f, 1f) : 0.70f; }
        }

        private float ShowerHygieneGain
        {
            get { return integration != null && integration.showerHygieneGain > 0f ? integration.showerHygieneGain : 0.08f; }
        }

        private float ShowerWaterCost
        {
            get { return integration != null && integration.showerWaterCost > 0f ? integration.showerWaterCost : 20f; }
        }

        private float ShowerSewageOutput
        {
            get { return integration != null && integration.showerSewageOutput > 0f ? integration.showerSewageOutput : 20f; }
        }

        private float BladderServiceThreshold
        {
            get { return integration != null ? Clamp(integration.bladderServiceThreshold, 0f, 1f) : 0.35f; }
        }

        private float BladderServiceTarget
        {
            get { return integration != null ? Clamp(integration.bladderServiceTarget, 0f, 1f) : 0.70f; }
        }

        private float ToiletWaterCost
        {
            get { return integration != null && integration.toiletWaterCost > 0f ? integration.toiletWaterCost : 2f; }
        }

        private float ToiletSewageOutput
        {
            get { return integration != null && integration.toiletSewageOutput > 0f ? integration.toiletSewageOutput : 5f; }
        }

        private string MissingNeedDefsForDisplay()
        {
            return bridge != null && !string.IsNullOrEmpty(bridge.MissingNeedDefs)
                ? bridge.MissingNeedDefs
                : Tr("CT_Shuttle_Addon_DBH_Unknown");
        }

        private static string ResolveTranslation(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            return key.Trim().Translate().ToString();
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }

        private static string Tr(string key, params object[] args)
        {
            return string.Format(Tr(key), args);
        }

        private static string ResolveServiceBlockReason(
            bool anyOccupantNeededService,
            string resourceBlockReason)
        {
            if (!string.IsNullOrEmpty(resourceBlockReason))
            {
                return resourceBlockReason;
            }

            return anyOccupantNeededService ? Tr("CT_Shuttle_Addon_DBH_Block_ResourcesPreventService") : null;
        }

        private static string ResolveNoServiceResult(
            int serviceableCount,
            bool anyOccupantNeededService,
            string serviceBlockReason)
        {
            if (!string.IsNullOrEmpty(serviceBlockReason))
            {
                return serviceBlockReason;
            }

            if (serviceableCount <= 0)
            {
                return Tr("CT_Shuttle_Addon_DBH_Block_NoServiceableOccupants");
            }

            if (!anyOccupantNeededService)
            {
                return Tr("CT_Shuttle_Addon_DBH_Block_NoOccupantsNeedService");
            }

            return Tr("CT_Shuttle_Addon_DBH_Result_NoServicePerformed", serviceableCount.ToString());
        }

        private static void ClearCleanWaterBlockReason(ShuttleExternalRuntimeContext context)
        {
            if (context == null || context.State == null)
            {
                return;
            }

            string blockedReason;
            if (!context.State.TryGetString("blockedReason", out blockedReason) ||
                string.IsNullOrEmpty(blockedReason))
            {
                return;
            }

            if (blockedReason == Tr("CT_Shuttle_Addon_DBH_Block_CleanWaterEmpty") ||
                blockedReason == Tr("CT_Shuttle_Addon_DBH_Block_CleanWaterInsufficient"))
            {
                context.State.SetString("blockedReason", null);
            }
        }

        private static float ReadFloat(ShuttleExternalRuntimeContext context, string key, float fallback)
        {
            float value;
            return context != null && context.State.TryGetFloat(key, out value) ? value : fallback;
        }

        private static void AddFloat(ShuttleExternalRuntimeContext context, string key, float delta)
        {
            if (context == null)
            {
                return;
            }

            float value;
            context.State.TryGetFloat(key, out value);
            context.State.SetFloat(key, value + delta);
        }

        private static void AddInt(ShuttleExternalRuntimeContext context, string key, int delta)
        {
            if (context == null)
            {
                return;
            }

            int value;
            context.State.TryGetInt(key, out value);
            context.State.SetInt(key, value + delta);
        }

        private static int CountServiceableHumanlikeOccupants(
            IReadOnlyList<ShuttleExternalOccupantInfo> occupants,
            ShuttleExternalHostInfo hostInfo)
        {
            int count = 0;
            if (occupants == null)
            {
                return count;
            }

            for (int i = 0; i < occupants.Count; i++)
            {
                ShuttleExternalOccupantInfo occupant = occupants[i];
                if (DBHOccupantServicePolicy.CanService(
                    occupant,
                    occupant != null ? occupant.UnsafeLivePawn : null,
                    hostInfo))
                {
                    count++;
                }
            }

            return count;
        }

        private static void AddPawnLabel(List<string> labels, Pawn pawn)
        {
            if (labels == null || pawn == null)
            {
                return;
            }

            string label = pawn.LabelShortCap;
            if (!string.IsNullOrEmpty(label) && !labels.Contains(label))
            {
                labels.Add(label);
            }
        }

        private static bool TryFindDumpCell(ShuttleExternalHostInfo hostInfo, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Map map = hostInfo != null ? hostInfo.UnsafeLiveMap : null;
            if (hostInfo == null || map == null || hostInfo.AdjacentExternalCells == null)
            {
                return false;
            }

            HashSet<IntVec3> occupied = new HashSet<IntVec3>();
            if (hostInfo.OccupiedCells != null)
            {
                for (int i = 0; i < hostInfo.OccupiedCells.Count; i++)
                {
                    occupied.Add(hostInfo.OccupiedCells[i]);
                }
            }

            IntVec3 fallback = IntVec3.Invalid;
            for (int i = 0; i < hostInfo.AdjacentExternalCells.Count; i++)
            {
                IntVec3 candidate = hostInfo.AdjacentExternalCells[i];
                if (!candidate.InBounds(map) || occupied.Contains(candidate))
                {
                    continue;
                }

                bool walkable = candidate.Walkable(map);
                bool roofless = !candidate.Roofed(map);
                if (walkable && roofless)
                {
                    cell = candidate;
                    return true;
                }

                if (!fallback.IsValid && walkable)
                {
                    fallback = candidate;
                }
            }

            if (fallback.IsValid)
            {
                cell = fallback;
                return true;
            }

            return false;
        }

        private ThingDef ResolveSewageDef()
        {
            ThingDef def = ResolveThingDef(integration != null ? integration.sewageThingDefName : null);
            if (def != null)
            {
                return def;
            }

            return ResolveThingDef(integration != null ? integration.sewageFilthDefName : null);
        }

        private static ThingDef ResolveThingDef(string defName)
        {
            return !string.IsNullOrWhiteSpace(defName)
                ? DefDatabase<ThingDef>.GetNamedSilentFail(defName.Trim())
                : null;
        }

        private static void TrySpawnSewageThing(ThingDef def, IntVec3 cell, Map map, float amount)
        {
            if (def == null || map == null || !cell.IsValid)
            {
                return;
            }

            Thing thing = ThingMaker.MakeThing(def);
            if (thing == null)
            {
                return;
            }

            int stackCount = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(amount));
            thing.stackCount = UnityEngine.Mathf.Min(stackCount, thing.def.stackLimit);
            GenSpawn.Spawn(thing, cell, map);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static float Max(float left, float right)
        {
            return left > right ? left : right;
        }

        private static float Min(float left, float right)
        {
            return left < right ? left : right;
        }
    }
}

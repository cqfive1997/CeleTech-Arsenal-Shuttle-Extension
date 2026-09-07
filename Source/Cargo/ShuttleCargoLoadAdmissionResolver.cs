using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    public enum ShuttleCargoLoadAdmissionKind
    {
        None,
        RegularCargo,
        RefrigeratedAutoTransfer,
        Both
    }

    public sealed class ShuttleCargoLoadAdmissionResult
    {
        public int ThingIDNumber;
        public string DefName;
        public bool Accepted;
        public bool RegularAccepted;
        public bool RefrigeratedAccepted;
        public ShuttleCargoLoadAdmissionKind Kind;
        public string Reason;
        public string DestinationLabel;
        public string RefrigeratedModuleInstanceID;
        public string RefrigeratedModuleLabel;
        public float RefrigeratedAvailableMassKg;
    }

    /// <summary>
    /// Read/validation helper for load-cargo admission. It decides whether a thing has at
    /// least one legal destination: ordinary cargo region or load-time cold routing.
    /// It never moves cargo and is shared by load read models and BeginLoadCargo validation.
    /// </summary>
    internal sealed class ShuttleCargoLoadAdmissionResolver
    {
        private const float MassEpsilon = 0.0001f;

        private readonly ThingWithComps host;
        private readonly ShuttleProfile profile;
        private readonly ShuttleAssemblyState assemblyState;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleCargoRegionConfigState cargoRegionConfig;
        private readonly int activeRegionCount;
        private readonly Dictionary<int, ShuttleCargoLoadAdmissionResult> admissionCache =
            new Dictionary<int, ShuttleCargoLoadAdmissionResult>();
        private CompShuttleRefrigeratedCargoRegistry registry;
        private List<AutoTransferDestination> autoTransferDestinations;
        private bool autoTransferDestinationsBuilt;
        private bool? hasActiveRefrigeratedLaunchTransfer;
        private float? normalCargoUsedMassKg;

        internal ShuttleCargoLoadAdmissionResolver(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            this.host = host;
            this.profile = profile;
            this.assemblyState = assemblyState;
            this.runtimeState = runtimeState;
            this.cargoRegionConfig = assemblyState != null ? assemblyState.CargoRegionConfig : null;
            this.activeRegionCount = profile != null && profile.Cargo != null
                ? (int)profile.Cargo.CargoRegionCount
                : 0;
        }

        internal int ActiveRegionCount
        {
            get
            {
                return this.activeRegionCount;
            }
        }

        internal float RefrigeratedAutoTransferCapacityKg
        {
            get
            {
                return this.HasAnyRefrigeratedAutoTransferDestination
                    ? ShuttleRefrigeratedCargoCapacityPolicy
                        .GetRefrigeratedPoolCapacityKg(this.profile)
                    : 0f;
            }
        }

        internal float RefrigeratedAutoTransferUsedMassKg
        {
            get
            {
                return ShuttleRefrigeratedCargoCapacityPolicy.GetTotalColdMassKg(
                    this.GetRegistry());
            }
        }

        internal float RefrigeratedAutoTransferAvailableMassKg
        {
            get
            {
                return this.HasAnyRefrigeratedAutoTransferDestination
                    ? ShuttleRefrigeratedCargoCapacityPolicy.GetAvailableColdMassKg(
                        this.profile,
                        this.GetRegistry(),
                        this.GetNormalCargoUsedMassKg())
                    : 0f;
            }
        }

        internal bool RefrigeratedMassSharesOverallCapacity
        {
            get { return ShuttleRefrigeratedCargoCapacityPolicy.SharesOverallCapacity; }
        }

        internal bool HasAnyRefrigeratedAutoTransferDestination
        {
            get
            {
                bool found = false;
                this.ForEachAutoTransferRecord(delegate(ShuttleRefrigeratedCargoModuleDef moduleDef, RefrigeratedCargoRecord record, ShuttleRefrigeratedCargoAutoTransferConfig config)
                {
                    found = true;
                });
                return found;
            }
        }

        internal ShuttleCargoLoadAdmissionResult Resolve(Thing thing)
        {
            int thingIDNumber = thing != null ? thing.thingIDNumber : 0;
            ShuttleCargoLoadAdmissionResult cachedResult;
            if (thingIDNumber > 0 && this.admissionCache.TryGetValue(thingIDNumber, out cachedResult))
            {
                return cachedResult;
            }

            ShuttleCargoLoadAdmissionResult result = new ShuttleCargoLoadAdmissionResult();
            result.ThingIDNumber = thingIDNumber;
            result.DefName = thing != null && thing.def != null ? thing.def.defName : null;

            if (thing == null || thing.Destroyed || thing.def == null || thing.stackCount <= 0)
            {
                result.Reason = "Load candidate is no longer valid.";
                if (thingIDNumber > 0)
                {
                    this.admissionCache[thingIDNumber] = result;
                }

                return result;
            }

            result.RegularAccepted = this.RegularCargoAccepts(thing);
            string refrigeratedModuleInstanceID;
            string refrigeratedModuleLabel;
            result.RefrigeratedAccepted = this.TryResolvePreferredRefrigeratedDestination(
                thing,
                thing.stackCount,
                null,
                out refrigeratedModuleInstanceID,
                out refrigeratedModuleLabel,
                out result.RefrigeratedAvailableMassKg);
            result.RefrigeratedModuleInstanceID = refrigeratedModuleInstanceID;
            result.RefrigeratedModuleLabel = refrigeratedModuleLabel;
            result.Accepted = result.RegularAccepted || result.RefrigeratedAccepted;
            result.Kind = this.GetKind(result.RegularAccepted, result.RefrigeratedAccepted);
            result.DestinationLabel = this.GetDestinationLabel(result);
            result.Reason = this.GetReason(result);
            if (thingIDNumber > 0)
            {
                this.admissionCache[thingIDNumber] = result;
            }

            return result;
        }

        internal ShuttleCargoLoadAdmissionResult ResolveOrDefault(Thing thing)
        {
            ShuttleCargoLoadAdmissionResult result = this.Resolve(thing);
            if (result == null)
            {
                result = new ShuttleCargoLoadAdmissionResult();
            }

            return result;
        }

        private bool RegularCargoAccepts(Thing thing)
        {
            if (thing == null || this.cargoRegionConfig == null || this.activeRegionCount <= 0)
            {
                return false;
            }

            return this.cargoRegionConfig.AllowsAnyActiveRegion(thing, this.activeRegionCount);
        }

        internal bool TryResolvePreferredRefrigeratedDestination(
            Thing thing,
            int requestedCount,
            IDictionary<string, float> reservedMassByModule,
            out string moduleInstanceID,
            out string moduleLabel,
            out float availableMassKg)
        {
            moduleInstanceID = null;
            moduleLabel = null;
            availableMassKg = 0f;
            if (!this.IsBasicColdCandidate(thing) ||
                this.HasActiveRefrigeratedLaunchTransfer())
            {
                return false;
            }

            AutoTransferDestination bestDestination = null;
            float bestAvailableMassKg = 0f;
            bool bestHasSameDef = false;
            float requestedMass = CargoDisplayUtility.GetThingMass(
                thing,
                requestedCount > 0 && requestedCount < thing.stackCount ? requestedCount : thing.stackCount);
            float sharedAvailableMassKg =
                ShuttleRefrigeratedCargoCapacityPolicy.GetAvailableColdMassKg(
                    this.profile,
                    this.GetRegistry(),
                    this.GetNormalCargoUsedMassKg()) -
                GetTotalReservedMass(reservedMassByModule);
            this.ForEachAutoTransferRecord(delegate(ShuttleRefrigeratedCargoModuleDef moduleDef, RefrigeratedCargoRecord record, ShuttleRefrigeratedCargoAutoTransferConfig config)
            {
                if (!this.ModuleAcceptsThing(moduleDef, record, config, thing))
                {
                    return;
                }

                float available = sharedAvailableMassKg;

                if (available <= MassEpsilon)
                {
                    return;
                }

                if (requestedMass > available + MassEpsilon)
                {
                    return;
                }

                AutoTransferDestination destination = this.FindAutoTransferDestination(record.ModuleInstanceID);
                if (destination == null)
                {
                    return;
                }

                bool hasSameDef = this.RecordContainsSameDef(record, thing);
                if (bestDestination == null ||
                    available > bestAvailableMassKg + MassEpsilon ||
                    (System.Math.Abs(available - bestAvailableMassKg) <= MassEpsilon && hasSameDef && !bestHasSameDef) ||
                    (System.Math.Abs(available - bestAvailableMassKg) <= MassEpsilon &&
                        hasSameDef == bestHasSameDef &&
                        string.CompareOrdinal(record.ModuleInstanceID, bestDestination.Record.ModuleInstanceID) < 0))
                {
                    bestDestination = destination;
                    bestAvailableMassKg = available;
                    bestHasSameDef = hasSameDef;
                }
            });

            if (bestDestination == null)
            {
                return false;
            }

            moduleInstanceID = bestDestination.Record.ModuleInstanceID;
            moduleLabel = bestDestination.Label;
            availableMassKg = bestAvailableMassKg;
            return true;
        }

        private bool ModuleAcceptsThing(
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            ShuttleRefrigeratedCargoAutoTransferConfig config,
            Thing thing)
        {
            if (moduleDef == null || record == null || config == null || thing == null)
            {
                return false;
            }

            if (!config.AutoTransferEnabled)
            {
                return false;
            }

            if (!ShuttleColdTransferMatcher.AllowsCorpseByModuleOrCustomFilter(
                thing,
                moduleDef,
                config.AutoTransferFilter,
                config.HasCustomAutoTransferFilter))
            {
                return false;
            }

            bool hasExplicitFilter = config.HasCustomAutoTransferFilter || config.AutoTransferFilter != null;
            if (!hasExplicitFilter &&
                moduleDef.autoTransferRottableItems &&
                thing.TryGetComp<CompRottable>() == null)
            {
                return false;
            }

            return config.AutoTransferFilter == null || config.AutoTransferFilter.Allows(thing);
        }

        private bool IsBasicColdCandidate(Thing thing)
        {
            return thing != null &&
                !thing.Destroyed &&
                thing.def != null &&
                thing.stackCount > 0 &&
                !(thing is Pawn) &&
                thing.def.category == ThingCategory.Item;
        }

        private bool UnitMassFits(Thing thing, float availableMassKg)
        {
            if (thing == null || thing.stackCount <= 0)
            {
                return false;
            }

            float fullMass = CargoDisplayUtility.GetThingMass(thing, thing.stackCount);
            if (fullMass <= MassEpsilon)
            {
                return true;
            }

            float unitMass = fullMass / thing.stackCount;
            return unitMass <= availableMassKg + MassEpsilon;
        }

        private void ForEachAutoTransferRecord(AutoTransferRecordAction action)
        {
            if (action == null)
            {
                return;
            }

            this.EnsureAutoTransferDestinations();
            if (this.autoTransferDestinations == null)
            {
                return;
            }

            for (int i = 0; i < this.autoTransferDestinations.Count; i++)
            {
                AutoTransferDestination destination = this.autoTransferDestinations[i];
                if (destination == null)
                {
                    continue;
                }

                action(destination.ModuleDef, destination.Record, destination.Config);
            }
        }

        private void EnsureAutoTransferDestinations()
        {
            if (this.autoTransferDestinationsBuilt)
            {
                return;
            }

            this.autoTransferDestinationsBuilt = true;
            this.autoTransferDestinations = new List<AutoTransferDestination>();
            if (this.host == null ||
                this.assemblyState == null ||
                this.assemblyState.RefrigeratedCargoConfig == null)
            {
                return;
            }

            CompShuttleRefrigeratedCargoRegistry currentRegistry = this.GetRegistry();
            if (currentRegistry == null || currentRegistry.Records == null)
            {
                return;
            }

            currentRegistry.Reconcile(this.assemblyState, this.runtimeState);
            for (int i = 0; i < currentRegistry.Records.Count; i++)
            {
                RefrigeratedCargoRecord record = currentRegistry.Records[i];
                if (record == null || string.IsNullOrEmpty(record.ModuleInstanceID))
                {
                    continue;
                }

                ShuttleModule module = this.assemblyState.GetModule(record.ModuleInstanceID);
                ShuttleRefrigeratedCargoModuleDef moduleDef =
                    module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
                if (module == null || !module.IsEnabled || moduleDef == null)
                {
                    continue;
                }

                ShuttleRefrigeratedCargoAutoTransferConfig config =
                    this.assemblyState.RefrigeratedCargoConfig.BuildEffectiveAutoTransferConfig(
                        record.ModuleInstanceID,
                        moduleDef);
                if (config == null || !config.AutoTransferEnabled)
                {
                    continue;
                }

                this.autoTransferDestinations.Add(new AutoTransferDestination(
                    module,
                    moduleDef,
                    record,
                    config,
                    this.GetDestinationLabel(module, moduleDef)));
            }
        }

        private AutoTransferDestination FindAutoTransferDestination(string moduleInstanceID)
        {
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            this.EnsureAutoTransferDestinations();
            if (this.autoTransferDestinations == null)
            {
                return null;
            }

            for (int i = 0; i < this.autoTransferDestinations.Count; i++)
            {
                AutoTransferDestination destination = this.autoTransferDestinations[i];
                if (destination != null &&
                    destination.Record != null &&
                    destination.Record.ModuleInstanceID == moduleInstanceID)
                {
                    return destination;
                }
            }

            return null;
        }

        private bool RecordContainsSameDef(RefrigeratedCargoRecord record, Thing thing)
        {
            if (record == null || record.Contents == null || thing == null || thing.def == null)
            {
                return false;
            }

            for (int i = 0; i < record.Contents.Count; i++)
            {
                Thing candidate = record.Contents[i];
                if (candidate != null && candidate.def == thing.def)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetDestinationLabel(ShuttleModule module, ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            return moduleDef != null
                ? moduleDef.LabelCap.ToString()
                : "Cold storage";
        }

        private CompShuttleRefrigeratedCargoRegistry GetRegistry()
        {
            if (this.registry == null && this.host != null)
            {
                this.registry = this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>();
            }

            return this.registry;
        }

        private bool HasActiveRefrigeratedLaunchTransfer()
        {
            if (this.hasActiveRefrigeratedLaunchTransfer.HasValue)
            {
                return this.hasActiveRefrigeratedLaunchTransfer.Value;
            }

            CompShuttleHolderLaunchTransferState transferState = this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            bool active = transferState != null && transferState.HasRefrigeratedCargoLaunchTransfer;
            this.hasActiveRefrigeratedLaunchTransfer = active;
            return active;
        }

        private float GetNormalCargoUsedMassKg()
        {
            if (this.normalCargoUsedMassKg.HasValue)
            {
                return this.normalCargoUsedMassKg.Value;
            }

            VanillaTransporterAdapter adapter = new VanillaTransporterAdapter();
            this.normalCargoUsedMassKg = adapter.ExistingMassUsage(
                adapter.ResolveTransportersForLaunch(this.host));
            return this.normalCargoUsedMassKg.Value;
        }

        private static float GetTotalReservedMass(
            IDictionary<string, float> reservedMassByModule)
        {
            float totalMassKg = 0f;
            if (reservedMassByModule == null)
            {
                return totalMassKg;
            }

            foreach (KeyValuePair<string, float> pair in reservedMassByModule)
            {
                if (!float.IsNaN(pair.Value) &&
                    !float.IsInfinity(pair.Value) &&
                    pair.Value > 0f)
                {
                    totalMassKg += pair.Value;
                }
            }

            return totalMassKg;
        }

        private ShuttleCargoLoadAdmissionKind GetKind(bool regularAccepted, bool refrigeratedAccepted)
        {
            if (regularAccepted && refrigeratedAccepted)
            {
                return ShuttleCargoLoadAdmissionKind.Both;
            }

            if (regularAccepted)
            {
                return ShuttleCargoLoadAdmissionKind.RegularCargo;
            }

            return refrigeratedAccepted
                ? ShuttleCargoLoadAdmissionKind.RefrigeratedAutoTransfer
                : ShuttleCargoLoadAdmissionKind.None;
        }

        private string GetDestinationLabel(ShuttleCargoLoadAdmissionResult result)
        {
            if (result == null)
            {
                return "No valid cargo destination";
            }

            if (result.Kind == ShuttleCargoLoadAdmissionKind.RegularCargo)
            {
                return "Regular cargo";
            }

            if (result.Kind == ShuttleCargoLoadAdmissionKind.RefrigeratedAutoTransfer)
            {
                return !string.IsNullOrEmpty(result.RefrigeratedModuleLabel)
                    ? "Cold: " + result.RefrigeratedModuleLabel
                    : "Cold storage";
            }

            if (result.Kind == ShuttleCargoLoadAdmissionKind.Both)
            {
                return !string.IsNullOrEmpty(result.RefrigeratedModuleLabel)
                    ? "Cold preferred: " + result.RefrigeratedModuleLabel
                    : "Regular cargo / cold storage";
            }

            return "No valid cargo destination";
        }

        private string GetReason(ShuttleCargoLoadAdmissionResult result)
        {
            if (result == null || !result.Accepted)
            {
            return "Rejected by ordinary cargo filters and cold-routing filters.";
            }

            if (result.Kind == ShuttleCargoLoadAdmissionKind.RegularCargo)
            {
                return "Ordinary cargo filter allows this item.";
            }

            if (result.Kind == ShuttleCargoLoadAdmissionKind.RefrigeratedAutoTransfer)
            {
                return "Ordinary cargo filter rejects this item, but cold routing on load accepts it.";
            }

            return "Ordinary cargo and refrigerated loading both allow this item; refrigerated loading is preferred when capacity is available.";
        }

        private delegate void AutoTransferRecordAction(
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            ShuttleRefrigeratedCargoAutoTransferConfig config);

        private sealed class AutoTransferDestination
        {
            internal readonly ShuttleModule Module;
            internal readonly ShuttleRefrigeratedCargoModuleDef ModuleDef;
            internal readonly RefrigeratedCargoRecord Record;
            internal readonly ShuttleRefrigeratedCargoAutoTransferConfig Config;
            internal readonly string Label;

            internal AutoTransferDestination(
                ShuttleModule module,
                ShuttleRefrigeratedCargoModuleDef moduleDef,
                RefrigeratedCargoRecord record,
                ShuttleRefrigeratedCargoAutoTransferConfig config,
                string label)
            {
                this.Module = module;
                this.ModuleDef = moduleDef;
                this.Record = record;
                this.Config = config;
                this.Label = label;
            }
        }
    }
}

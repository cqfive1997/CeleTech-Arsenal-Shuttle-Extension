using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal enum ShuttleTransferRecoveryStatus
    {
        None,
        RecoveredToPreferredOwner,
        RecoveredToFallbackOwner,
        RecoveredToEmergencyOwner,
        DroppedToMap,
        FatalUnresolved
    }

    public sealed class CompProperties_ShuttleHolderLaunchTransferState : CompProperties
    {
        public CompProperties_ShuttleHolderLaunchTransferState()
        {
            this.compClass = typeof(CompShuttleHolderLaunchTransferState);
        }
    }

    /// <summary>
    /// Shuttle-owned save state for holder launch transfer manifests and legacy
    /// debug staging holders. Formal Habitat transfer uses the manifest as the
    /// durable contract across launch, world travel, save/load, and impact.
    /// </summary>
    public sealed class CompShuttleHolderLaunchTransferState : ThingComp, IThingHolder
    {
        private ThingOwner<Thing> devTestHeldThings;
        private ThingOwner<Thing> medicalBayPatientStagingThings;
        private ThingOwner<Thing> emergencyRecoveryThings;
        private ActiveTransporterInfo devHabitatLivingActiveTransporter;
        private ShuttleHolderLaunchManifest manifest;
        private RefrigeratedCargoLaunchManifest refrigeratedCargoLaunchManifest;
        private List<RefrigeratedLaunchStagingHolder> refrigeratedLaunchStagingHolders;
        private ThingOwnerHolderRoot medicalBayPatientStagingRoot;
        private ThingOwnerHolderRoot emergencyRecoveryRoot;
        private ThingOwnerHolderRoot devHabitatLivingStagingRoot;
        private bool devHabitatJoyRealLaunchSpikeEnabled;
        private bool devHabitatMixedRealLaunchSpikeEnabled;
        private bool devMedicalBayRealLaunchRollbackSpikeEnabled;
        private bool devMedicalBayRealLaunchRestoreSpikeEnabled;

        public CompShuttleHolderLaunchTransferState()
        {
            this.devTestHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.medicalBayPatientStagingThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.emergencyRecoveryThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.devHabitatLivingActiveTransporter = new ActiveTransporterInfo();
            this.manifest = new ShuttleHolderLaunchManifest();
            this.refrigeratedCargoLaunchManifest = new RefrigeratedCargoLaunchManifest();
            this.refrigeratedLaunchStagingHolders = new List<RefrigeratedLaunchStagingHolder>();
            this.EnsureHolderRoots();
        }

        internal bool HasDevTestHeldThings
        {
            get
            {
                this.EnsureInitialized();
                return this.devTestHeldThings.Count > 0;
            }
        }

        internal int DevTestHeldThingsCount
        {
            get
            {
                this.EnsureInitialized();
                return this.devTestHeldThings.Count;
            }
        }

        internal bool HasActiveManifest
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest.HasEntries;
            }
        }

        internal bool HasHabitatLivingManifestEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind).Count > 0 ||
                    this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind).Count > 0 ||
                    this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind).Count > 0;
            }
        }

        internal bool HasHabitatJoyManifestEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind).Count > 0;
            }
        }

        internal bool HasMedicalBayPatientManifestEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind).Count > 0;
            }
        }

        internal bool HasMechChargerManifestEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind).Count > 0;
            }
        }

        internal bool HasPrisonCellPrisonerManifestEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind).Count > 0;
            }
        }

        internal bool HasDevHabitatLivingStagingThings
        {
            get
            {
                ThingOwner container = this.DevHabitatLivingExportContainer;
                return container != null && container.Count > 0;
            }
        }

        internal bool HasMedicalBayPatientStagingThings
        {
            get
            {
                this.EnsureInitialized();
                return this.medicalBayPatientStagingThings.Count > 0;
            }
        }

        internal bool HasEmergencyRecoveryThings
        {
            get
            {
                this.EnsureInitialized();
                return this.emergencyRecoveryThings.Count > 0;
            }
        }

        internal bool HasAnyActiveOrRecoveryTransfer
        {
            get
            {
                this.EnsureInitialized();
                return this.HasActiveManifest ||
                    this.HasRefrigeratedCargoLaunchTransfer ||
                    this.HasMedicalBayPatientStagingThings ||
                    this.HasDevHabitatLivingStagingThings ||
                    this.HasEmergencyRecoveryThings;
            }
        }

        internal bool HasAnyRecoveryOrNonClearableTransfer
        {
            get
            {
                this.EnsureInitialized();
                return this.HasRefrigeratedCargoLaunchTransfer ||
                    this.HasMedicalBayPatientStagingThings ||
                    this.HasDevHabitatLivingStagingThings ||
                    this.HasEmergencyRecoveryThings ||
                    this.HasHabitatLivingManifestEntries ||
                    this.HasHabitatJoyManifestEntries ||
                    this.HasMedicalBayPatientManifestEntries ||
                    this.HasMechChargerManifestEntries ||
                    this.HasPrisonCellPrisonerManifestEntries;
            }
        }

        internal bool HasOnlyClearableDevTestManifestEntries(out string reason)
        {
            reason = null;
            this.EnsureInitialized();

            if (this.HasRefrigeratedCargoLaunchTransfer)
            {
                reason = "refrigerated launch transfer is active.";
                return false;
            }

            if (this.HasMedicalBayPatientStagingThings)
            {
                reason = "medical bay patient staging contains " +
                    this.medicalBayPatientStagingThings.Count +
                    " thing(s).";
                return false;
            }

            if (this.HasDevHabitatLivingStagingThings)
            {
                ThingOwner container = this.DevHabitatLivingExportContainer;
                reason = "dev Habitat living staging contains " +
                    (container != null ? container.Count : 0) +
                    " thing(s).";
                return false;
            }

            if (this.HasEmergencyRecoveryThings)
            {
                reason = "emergency recovery contains " +
                    this.emergencyRecoveryThings.Count +
                    " thing(s).";
                return false;
            }

            if (this.manifest == null ||
                this.manifest.Entries == null ||
                this.manifest.Entries.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < this.manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.manifest.Entries[i];
                if (entry == null)
                {
                    reason = "null holder transfer manifest entry at index=" + i + ".";
                    return false;
                }

                if (entry.HolderKind != ShuttleHolderLaunchManifestConstants.DevTestHolderKind)
                {
                    reason =
                        "non-DevTest holder transfer manifest entry at index=" +
                        i +
                        " holderKind=" +
                        (entry.HolderKind ?? "null") +
                        " thingID=" +
                        entry.ThingID +
                        " phase=" +
                        (entry.TransferPhase ?? "null") +
                        ".";
                    return false;
                }
            }

            return true;
        }

        internal int EmergencyRecoveryThingCount
        {
            get
            {
                this.EnsureInitialized();
                return this.emergencyRecoveryThings.Count;
            }
        }

        internal bool DevHabitatJoyRealLaunchSpikeEnabled
        {
            get
            {
                return Prefs.DevMode && this.devHabitatJoyRealLaunchSpikeEnabled;
            }
        }

        internal bool DevHabitatMixedRealLaunchSpikeEnabled
        {
            get
            {
                return Prefs.DevMode && this.devHabitatMixedRealLaunchSpikeEnabled;
            }
        }

        internal bool DevMedicalBayRealLaunchRollbackSpikeEnabled
        {
            get
            {
                return Prefs.DevMode && this.devMedicalBayRealLaunchRollbackSpikeEnabled;
            }
        }

        internal bool DevMedicalBayRealLaunchRestoreSpikeEnabled
        {
            get
            {
                return Prefs.DevMode && this.devMedicalBayRealLaunchRestoreSpikeEnabled;
            }
        }

        internal ThingOwner<Thing> DevTestHeldThings
        {
            get
            {
                this.EnsureInitialized();
                return this.devTestHeldThings;
            }
        }

        internal ThingOwner<Thing> MedicalBayPatientStagingThings
        {
            get
            {
                this.EnsureInitialized();
                return this.medicalBayPatientStagingThings;
            }
        }

        internal ThingOwner<Thing> EmergencyRecoveryThings
        {
            get
            {
                this.EnsureInitialized();
                return this.emergencyRecoveryThings;
            }
        }

        internal ThingOwner DevHabitatLivingExportContainer
        {
            get
            {
                this.EnsureInitialized();
                return this.devHabitatLivingActiveTransporter != null
                    ? this.devHabitatLivingActiveTransporter.innerContainer
                    : null;
            }
        }

        internal ShuttleHolderLaunchManifest Manifest
        {
            get
            {
                this.EnsureInitialized();
                return this.manifest;
            }
        }

        internal RefrigeratedCargoLaunchManifest RefrigeratedCargoLaunchManifest
        {
            get
            {
                this.EnsureInitialized();
                return this.refrigeratedCargoLaunchManifest;
            }
        }

        internal int RefrigeratedCargoLaunchManifestEntryCount
        {
            get
            {
                this.EnsureInitialized();
                return this.refrigeratedCargoLaunchManifest != null
                    ? this.refrigeratedCargoLaunchManifest.EntryCount
                    : 0;
            }
        }

        internal IReadOnlyList<RefrigeratedLaunchStagingHolder> RefrigeratedLaunchStagingHolders
        {
            get
            {
                this.EnsureInitialized();
                return this.refrigeratedLaunchStagingHolders;
            }
        }

        internal int RefrigeratedLaunchStagingHolderCount
        {
            get
            {
                this.EnsureInitialized();
                return this.refrigeratedLaunchStagingHolders != null
                    ? this.refrigeratedLaunchStagingHolders.Count
                    : 0;
            }
        }

        internal int RefrigeratedCargoLaunchTransferStackCount
        {
            get
            {
                this.EnsureInitialized();
                int stagedStackCount = 0;
                for (int i = 0; i < this.refrigeratedLaunchStagingHolders.Count; i++)
                {
                    RefrigeratedLaunchStagingHolder holder =
                        this.refrigeratedLaunchStagingHolders[i];
                    if (holder != null)
                    {
                        stagedStackCount += holder.StackCount;
                    }
                }

                int manifestEntryCount =
                    this.refrigeratedCargoLaunchManifest != null
                        ? this.refrigeratedCargoLaunchManifest.EntryCount
                        : 0;
                return System.Math.Max(stagedStackCount, manifestEntryCount);
            }
        }

        internal bool HasRefrigeratedCargoLaunchTransfer
        {
            get
            {
                this.EnsureInitialized();
                if (this.refrigeratedCargoLaunchManifest.HasEntries)
                {
                    return true;
                }

                for (int i = 0; i < this.refrigeratedLaunchStagingHolders.Count; i++)
                {
                    RefrigeratedLaunchStagingHolder holder = this.refrigeratedLaunchStagingHolders[i];
                    if (holder != null && holder.HasContents)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal bool HasActiveRefrigeratedLaunchTransferForModule(
            string moduleInstanceID,
            out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            this.EnsureInitialized();
            List<RefrigeratedCargoLaunchManifestEntry> entries =
                this.refrigeratedCargoLaunchManifest.FindEntriesByModule(moduleInstanceID);
            if (entries != null && entries.Count > 0)
            {
                RefrigeratedCargoLaunchManifestEntry entry = entries[0];
                reason = "active refrigerated launch manifest entry count=" +
                    entries.Count +
                    " phase=" +
                    (entry != null ? entry.TransferPhase ?? "null" : "null");
                return true;
            }

            RefrigeratedLaunchStagingHolder holder;
            if (this.TryGetRefrigeratedLaunchStagingHolder(moduleInstanceID, out holder) &&
                holder != null &&
                holder.HasContents)
            {
                reason = "refrigerated launch staging holder contains " +
                    holder.StackCount +
                    " stack(s)";
                return true;
            }

            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref this.devTestHeldThings, "devTestHeldThings", new object[] { this });
            Scribe_Deep.Look(ref this.medicalBayPatientStagingThings, "medicalBayPatientStagingThings", new object[] { this });
            Scribe_Deep.Look(ref this.emergencyRecoveryThings, "emergencyRecoveryThings", new object[] { this });
            Scribe_Deep.Look(ref this.devHabitatLivingActiveTransporter, "devHabitatLivingActiveTransporter");
            Scribe_Deep.Look(ref this.manifest, "holderLaunchManifest");
            Scribe_Deep.Look(ref this.refrigeratedCargoLaunchManifest, "refrigeratedCargoLaunchManifest");
            Scribe_Collections.Look(
                ref this.refrigeratedLaunchStagingHolders,
                "refrigeratedLaunchStagingHolders",
                LookMode.Deep,
                System.Array.Empty<object>());
            Scribe_Values.Look(ref this.devHabitatJoyRealLaunchSpikeEnabled, "devHabitatJoyRealLaunchSpikeEnabled", false);
            Scribe_Values.Look(ref this.devHabitatMixedRealLaunchSpikeEnabled, "devHabitatMixedRealLaunchSpikeEnabled", false);
            Scribe_Values.Look(ref this.devMedicalBayRealLaunchRollbackSpikeEnabled, "devMedicalBayRealLaunchRollbackSpikeEnabled", false);
            Scribe_Values.Look(ref this.devMedicalBayRealLaunchRestoreSpikeEnabled, "devMedicalBayRealLaunchRestoreSpikeEnabled", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.devTestHeldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
            this.EnsureHolderRoots();
            outChildren.Add(this.medicalBayPatientStagingRoot);
            outChildren.Add(this.emergencyRecoveryRoot);
            outChildren.Add(this.devHabitatLivingStagingRoot);

            this.EnsureInitialized();
            for (int i = 0; i < this.refrigeratedLaunchStagingHolders.Count; i++)
            {
                RefrigeratedLaunchStagingHolder holder = this.refrigeratedLaunchStagingHolders[i];
                if (holder != null)
                {
                    outChildren.Add(holder);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        internal string DumpManifestForDebug()
        {
            this.EnsureInitialized();
            return this.manifest.DumpForDebug();
        }

        private void DumpManifestToMessagesAndLog()
        {
            string dump = this.DumpManifestForDebug();
            Log.Message("[CeleTech Shuttle] Holder transfer manifest dump: " + dump);
            Messages.Message("[CeleTech Shuttle] Holder transfer manifest dumped to log.", MessageTypeDefOf.PositiveEvent, false);
        }

        internal string DescribeActiveOrRecoveryTransferBlocker()
        {
            this.EnsureInitialized();
            StringBuilder builder = new StringBuilder();
            builder.Append("activeManifest=");
            builder.Append(this.HasActiveManifest);
            builder.Append(" habitatLivingManifest=");
            builder.Append(this.HasHabitatLivingManifestEntries);
            builder.Append(" habitatJoyManifest=");
            builder.Append(this.HasHabitatJoyManifestEntries);
            builder.Append(" medicalBayPatientManifest=");
            builder.Append(this.HasMedicalBayPatientManifestEntries);
            builder.Append(" mechChargerManifest=");
            builder.Append(this.HasMechChargerManifestEntries);
            builder.Append(" refrigeratedTransfer=");
            builder.Append(this.HasRefrigeratedCargoLaunchTransfer);
            builder.Append(" medicalBayStaging=");
            builder.Append(this.HasMedicalBayPatientStagingThings);
            builder.Append(" devHabitatLivingStaging=");
            builder.Append(this.HasDevHabitatLivingStagingThings);
            builder.Append(" emergencyRecovery=");
            builder.Append(this.HasEmergencyRecoveryThings);
            builder.Append(" manifestEntries=");
            builder.Append(this.manifest != null && this.manifest.Entries != null
                ? this.manifest.Entries.Count
                : 0);
            builder.Append(" refrigeratedEntries=");
            builder.Append(this.refrigeratedCargoLaunchManifest != null &&
                this.refrigeratedCargoLaunchManifest.Entries != null
                    ? this.refrigeratedCargoLaunchManifest.Entries.Count
                    : 0);
            builder.Append(" refrigeratedStagingHolders=");
            builder.Append(this.refrigeratedLaunchStagingHolders != null
                ? this.refrigeratedLaunchStagingHolders.Count
                : 0);
            builder.Append(" emergencyRecoveryCount=");
            builder.Append(this.emergencyRecoveryThings != null
                ? this.emergencyRecoveryThings.Count
                : 0);
            return builder.ToString();
        }

        internal void MarkManifestFailed(string reason)
        {
            this.EnsureInitialized();
            for (int i = 0; i < this.manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.manifest.Entries[i];
                if (entry == null)
                {
                    continue;
                }

                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = reason;
            }
        }

        internal void ClearManifest()
        {
            this.EnsureInitialized();
            this.manifest.Clear();
        }

        internal RefrigeratedLaunchStagingHolder GetOrCreateRefrigeratedLaunchStagingHolder(
            string moduleInstanceID,
            string sourceModuleDefName,
            float sourceTemperatureC,
            float targetTemperatureC,
            bool coolingActive)
        {
            this.EnsureInitialized();
            RefrigeratedLaunchStagingHolder holder;
            if (this.TryGetRefrigeratedLaunchStagingHolder(moduleInstanceID, out holder))
            {
                holder.BindOrRefresh(
                    moduleInstanceID,
                    sourceModuleDefName,
                    sourceTemperatureC,
                    targetTemperatureC,
                    coolingActive);
                return holder;
            }

            holder = new RefrigeratedLaunchStagingHolder();
            holder.SetParentHolder(this);
            holder.BindOrRefresh(
                moduleInstanceID,
                sourceModuleDefName,
                sourceTemperatureC,
                targetTemperatureC,
                coolingActive);
            this.refrigeratedLaunchStagingHolders.Add(holder);
            return holder;
        }

        internal bool TryGetRefrigeratedLaunchStagingHolder(
            string moduleInstanceID,
            out RefrigeratedLaunchStagingHolder holder)
        {
            holder = null;
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.refrigeratedLaunchStagingHolders.Count; i++)
            {
                RefrigeratedLaunchStagingHolder candidate = this.refrigeratedLaunchStagingHolders[i];
                if (candidate != null && candidate.ModuleInstanceID == moduleInstanceID)
                {
                    holder = candidate;
                    return true;
                }
            }

            return false;
        }

        internal void RemoveEmptyRefrigeratedLaunchStagingHolders()
        {
            this.EnsureInitialized();
            for (int i = this.refrigeratedLaunchStagingHolders.Count - 1; i >= 0; i--)
            {
                RefrigeratedLaunchStagingHolder holder = this.refrigeratedLaunchStagingHolders[i];
                if (holder == null || !holder.HasContents)
                {
                    this.refrigeratedLaunchStagingHolders.RemoveAt(i);
                }
            }
        }

        internal void ClearRefrigeratedCargoLaunchTransferIfEmpty()
        {
            this.EnsureInitialized();
            this.RemoveEmptyRefrigeratedLaunchStagingHolders();
            if (this.refrigeratedLaunchStagingHolders.Count != 0)
            {
                return;
            }

            string failureReason;
            if (!this.IsRefrigeratedCargoLaunchManifestResolvedForClear(out failureReason))
            {
                Log.Warning(
                    "[CeleTech Shuttle] Refused to clear refrigerated cargo launch transfer while manifest entries are unresolved. " +
                    failureReason +
                    " " +
                    this.DescribeActiveOrRecoveryTransferBlocker());
                return;
            }

            this.refrigeratedCargoLaunchManifest.Clear();
        }

        internal bool TryRecoverTransferThing(
            Thing thing,
            string reason,
            out ShuttleTransferRecoveryStatus status,
            out string failureReason)
        {
            return this.TryRecoverTransferThing(
                thing,
                reason,
                null,
                null,
                null,
                null,
                out status,
                out failureReason);
        }

        internal bool TryRecoverTransferThing(
            Thing thing,
            string reason,
            ThingOwner preferredOwner,
            ThingOwner fallbackOwner,
            Map map,
            IntVec3? cell,
            out ShuttleTransferRecoveryStatus status,
            out string failureReason)
        {
            this.EnsureInitialized();
            status = ShuttleTransferRecoveryStatus.None;
            failureReason = null;

            if (thing == null)
            {
                status = ShuttleTransferRecoveryStatus.FatalUnresolved;
                failureReason = "Emergency transfer recovery failed: thing is null. reason=" +
                    (reason ?? "null");
                return false;
            }

            if (thing.Destroyed)
            {
                status = ShuttleTransferRecoveryStatus.FatalUnresolved;
                failureReason = "Emergency transfer recovery refused destroyed thing. " +
                    this.DescribeRecoveryThing(thing) +
                    " reason=" +
                    (reason ?? "null");
                return false;
            }

            if (preferredOwner != null && preferredOwner.TryAddOrTransfer(thing, false))
            {
                status = ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner;
                return true;
            }

            if (fallbackOwner != null &&
                !object.ReferenceEquals(preferredOwner, fallbackOwner) &&
                fallbackOwner.TryAddOrTransfer(thing, false))
            {
                status = ShuttleTransferRecoveryStatus.RecoveredToFallbackOwner;
                return true;
            }

            if (this.emergencyRecoveryThings.TryAddOrTransfer(thing, false))
            {
                status = ShuttleTransferRecoveryStatus.RecoveredToEmergencyOwner;
                Log.Warning("[CeleTech Shuttle] Transfer recovery moved thing to emergency holder. " +
                    this.DescribeRecoveryThing(thing) +
                    " reason=" +
                    (reason ?? "null"));
                return true;
            }

            Map targetMap = map ?? (this.parent != null ? this.parent.Map : null);
            IntVec3 targetCell = cell.HasValue
                ? cell.Value
                : (this.parent != null ? this.parent.Position : IntVec3.Invalid);
            if (targetMap != null &&
                targetCell.IsValid &&
                GenPlace.TryPlaceThing(thing, targetCell, targetMap, ThingPlaceMode.Near))
            {
                status = ShuttleTransferRecoveryStatus.DroppedToMap;
                Log.Warning("[CeleTech Shuttle] Transfer recovery dropped thing near shuttle after all holder recovery owners rejected it. " +
                    this.DescribeRecoveryThing(thing) +
                    " reason=" +
                    (reason ?? "null"));
                return true;
            }

            status = ShuttleTransferRecoveryStatus.FatalUnresolved;
            failureReason = "Emergency transfer recovery failed for " +
                this.DescribeRecoveryThing(thing) +
                ". preferredOwner=" +
                this.DescribeOwner(preferredOwner) +
                " fallbackOwner=" +
                this.DescribeOwner(fallbackOwner) +
                " emergencyCount=" +
                this.emergencyRecoveryThings.Count +
                " mapAvailable=" +
                (targetMap != null) +
                " cell=" +
                targetCell +
                " reason=" +
                (reason ?? "null");
            Log.Error("[CeleTech Shuttle] " + failureReason);
            return false;
        }

        internal bool ContainsEmergencyRecoveryThing(int thingID)
        {
            this.EnsureInitialized();
            if (thingID <= 0)
            {
                return false;
            }

            for (int i = 0; i < this.emergencyRecoveryThings.Count; i++)
            {
                Thing thing = this.emergencyRecoveryThings[i];
                if (thing != null && thing.thingIDNumber == thingID)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasActiveMedicalBayPatientLocalTransfer(out string reason)
        {
            this.EnsureInitialized();
            if (this.HasMedicalBayPatientManifestEntries)
            {
                List<ShuttleHolderLaunchManifestEntry> entries =
                    this.manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
                reason = "active MedicalBay patient manifest entry count=" + entries.Count;
                return true;
            }

            if (this.medicalBayPatientStagingThings.Count > 0)
            {
                reason = "MedicalBay patient staging contains " +
                    this.medicalBayPatientStagingThings.Count +
                    " pawn(s)";
                return true;
            }

            reason = null;
            return false;
        }

        internal void EnableDevHabitatJoyRealLaunchSpike()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            this.devHabitatJoyRealLaunchSpikeEnabled = true;
        }

        internal void ClearDevHabitatJoyRealLaunchSpike()
        {
            this.devHabitatJoyRealLaunchSpikeEnabled = false;
        }

        internal void EnableDevHabitatMixedRealLaunchSpike()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            this.devHabitatMixedRealLaunchSpikeEnabled = true;
        }

        internal void ClearDevHabitatMixedRealLaunchSpike()
        {
            this.devHabitatMixedRealLaunchSpikeEnabled = false;
        }

        internal void EnableDevMedicalBayRealLaunchRollbackSpike()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            this.devMedicalBayRealLaunchRollbackSpikeEnabled = true;
            this.devMedicalBayRealLaunchRestoreSpikeEnabled = false;
        }

        internal void ClearDevMedicalBayRealLaunchRollbackSpike()
        {
            this.devMedicalBayRealLaunchRollbackSpikeEnabled = false;
        }

        internal void EnableDevMedicalBayRealLaunchRestoreSpike()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            this.devMedicalBayRealLaunchRestoreSpikeEnabled = true;
            this.devMedicalBayRealLaunchRollbackSpikeEnabled = false;
        }

        internal void ClearDevMedicalBayRealLaunchRestoreSpike()
        {
            this.devMedicalBayRealLaunchRestoreSpikeEnabled = false;
        }

        internal bool TryAddDevTestThing(Thing thing, out string failureReason)
        {
            failureReason = null;
            this.EnsureInitialized();

            if (!this.IsEligibleDevTestThing(thing))
            {
                failureReason = "[CeleTech Shuttle] Dev holder transfer test accepts only spawned ordinary non-pawn item Things.";
                return false;
            }

            Map map = thing.Map;
            IntVec3 fallbackCell = this.parent != null ? this.parent.Position : thing.Position;
            if (thing.Spawned)
            {
                thing.DeSpawn(DestroyMode.Vanish);
            }

            if (this.devTestHeldThings.TryAddOrTransfer(thing, true))
            {
                return true;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string recoveryFailureReason;
            if (this.TryRecoverTransferThing(
                thing,
                "Dev holder transfer test add failed after map despawn.",
                this.devTestHeldThings,
                null,
                map,
                fallbackCell,
                out recoveryStatus,
                out recoveryFailureReason))
            {
                if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                {
                    return true;
                }

                failureReason = "[CeleTech Shuttle] Failed to add thing to Dev holder transfer test; recovered safely with status=" +
                    recoveryStatus +
                    ".";
                return false;
            }

            if (!thing.Spawned &&
                map != null &&
                fallbackCell.IsValid &&
                GenPlace.TryPlaceThing(thing, fallbackCell, map, ThingPlaceMode.Near))
            {
            failureReason = "[CeleTech Shuttle] Failed to add thing to Dev holder transfer test; restored near the shuttle.";
                return false;
            }

            failureReason = "[CeleTech Shuttle] Failed to add thing to Dev holder transfer test; emergency recovery also failed: " +
                (recoveryFailureReason ?? "null");
            Log.Error(failureReason);
            return false;
        }

        private bool IsEligibleDevTestThing(Thing thing)
        {
            return thing != null &&
                thing != this.parent &&
                !thing.Destroyed &&
                thing.Spawned &&
                thing.Map == (this.parent != null ? this.parent.Map : null) &&
                !(thing is Pawn) &&
                thing.def != null &&
                thing.def.category == ThingCategory.Item;
        }

        internal string DumpRefrigeratedCargoLaunchTransferForDebug()
        {
            this.EnsureInitialized();
            StringBuilder builder = new StringBuilder();
            builder.Append("activeTransfer=");
            builder.Append(this.HasRefrigeratedCargoLaunchTransfer);
            builder.Append(" manifestEntryCount=");
            builder.Append(this.refrigeratedCargoLaunchManifest != null &&
                this.refrigeratedCargoLaunchManifest.Entries != null
                    ? this.refrigeratedCargoLaunchManifest.Entries.Count
                    : 0);
            builder.Append(" stagingHolderCount=");
            builder.Append(this.refrigeratedLaunchStagingHolders != null
                ? this.refrigeratedLaunchStagingHolders.Count
                : 0);
            builder.Append(" emergencyRecoveryCount=");
            builder.Append(this.emergencyRecoveryThings != null
                ? this.emergencyRecoveryThings.Count
                : 0);
            builder.AppendLine();
            builder.Append(this.refrigeratedCargoLaunchManifest != null
                ? this.refrigeratedCargoLaunchManifest.DumpForDebug()
                : "refrigeratedManifest=null");

            if (this.refrigeratedLaunchStagingHolders == null ||
                this.refrigeratedLaunchStagingHolders.Count == 0)
            {
                builder.AppendLine();
                builder.Append("stagingHolders=none");
                builder.AppendLine();
                builder.Append("emergencyRecoveryThings=");
                builder.Append(this.BuildThingOwnerSummary(this.emergencyRecoveryThings));
                return builder.ToString();
            }

            builder.AppendLine();
            builder.Append("stagingHolders=");
            builder.Append(this.refrigeratedLaunchStagingHolders.Count);
            for (int i = 0; i < this.refrigeratedLaunchStagingHolders.Count; i++)
            {
                RefrigeratedLaunchStagingHolder holder = this.refrigeratedLaunchStagingHolders[i];
                builder.AppendLine();
                builder.Append("  [");
                builder.Append(i);
                builder.Append("] moduleInstanceID=");
                builder.Append(holder != null ? holder.ModuleInstanceID ?? "null" : "null");
                builder.Append(" sourceModuleDefName=");
                builder.Append(holder != null ? holder.SourceModuleDefName ?? "null" : "null");
                builder.Append(" coolingActive=");
                builder.Append(holder != null && holder.CoolingActive);
                builder.Append(" targetTemperatureC=");
                builder.Append(holder != null ? holder.TargetTemperatureC.ToString("0.###") : "0");
                builder.Append(" stackCount=");
                builder.Append(holder != null ? holder.StackCount : 0);
                builder.Append(" things=");
                builder.Append(this.BuildRefrigeratedStagingThingSummary(holder));
            }

            builder.AppendLine();
            builder.Append("emergencyRecoveryThings=");
            builder.Append(this.BuildThingOwnerSummary(this.emergencyRecoveryThings));
            return builder.ToString();
        }

        private string BuildRefrigeratedStagingThingSummary(RefrigeratedLaunchStagingHolder holder)
        {
            if (holder == null || holder.Contents == null || holder.Contents.Count == 0)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < holder.Contents.Count; i++)
            {
                Thing thing = holder.Contents[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                if (thing == null)
                {
                    builder.Append("null");
                    continue;
                }

                builder.Append(thing.thingIDNumber);
                builder.Append(":");
                builder.Append(thing.def != null ? thing.def.defName : "null");
                builder.Append("x");
                builder.Append(thing.stackCount);
            }

            return builder.ToString();
        }

        private string BuildThingOwnerSummary(ThingOwner owner)
        {
            if (owner == null || owner.Count == 0)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                if (thing == null)
                {
                    builder.Append("null");
                    continue;
                }

                builder.Append(thing.thingIDNumber);
                builder.Append(":");
                builder.Append(thing.def != null ? thing.def.defName : "null");
                builder.Append("x");
                builder.Append(thing.stackCount);
            }

            return builder.ToString();
        }

        private void ClearManifestForDev()
        {
            string clearReason;
            if (!this.HasOnlyClearableDevTestManifestEntries(out clearReason))
            {
                string blocker = this.DescribeActiveOrRecoveryTransferBlocker();
                Messages.Message(
                    "[CeleTech Shuttle] Refused to dev-clear holder transfer state because only DevTest manifests are clearable. " +
                    (clearReason ?? "No reason provided.") +
                    " " +
                    blocker,
                    MessageTypeDefOf.RejectInput,
                    false);
                Log.Warning("[CeleTech Shuttle] Dev clear manifest refused because only DevTest manifests are clearable. " +
                    (clearReason ?? "No reason provided.") +
                    " " +
                    blocker);
                return;
            }

            this.ClearManifest();
            Messages.Message(
                "[CeleTech Shuttle] Non-Habitat test manifest cleared only. No Things were moved, restored, recovered, or ejected.",
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private void ArmSetShuttleFailureInjectionForDev()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            LaunchCargoTransaction.ArmDevSetShuttleFailureInjectionForNextCommit();
            Messages.Message(
                "[CeleTech Shuttle] Dev launch cargo SetShuttle failure injection armed for the next shuttle-source commit.",
                MessageTypeDefOf.NeutralEvent,
                false);
            Log.Warning("[CeleTech Shuttle] Dev launch cargo SetShuttle failure injection armed for the next shuttle-source commit.");
        }

        private bool IsRefrigeratedCargoLaunchManifestResolvedForClear(out string failureReason)
        {
            failureReason = null;
            this.EnsureInitialized();
            if (this.refrigeratedCargoLaunchManifest == null ||
                this.refrigeratedCargoLaunchManifest.Entries == null ||
                this.refrigeratedCargoLaunchManifest.Entries.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < this.refrigeratedCargoLaunchManifest.Entries.Count; i++)
            {
                RefrigeratedCargoLaunchManifestEntry entry = this.refrigeratedCargoLaunchManifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "null refrigerated manifest entry at index=" + i;
                    return false;
                }

                string phase = entry.TransferPhase;
                if (phase == RefrigeratedCargoLaunchManifestConstants.PhaseRestored ||
                    phase == RefrigeratedCargoLaunchManifestConstants.PhaseRolledBack ||
                    phase == RefrigeratedCargoLaunchManifestConstants.PhaseNormalCargoFallback ||
                    phase == RefrigeratedCargoLaunchManifestConstants.PhaseImpactFallback)
                {
                    continue;
                }

                failureReason =
                    "unresolved refrigerated entry index=" +
                    i +
                    " moduleInstanceID=" +
                    (entry.ModuleInstanceID ?? "null") +
                    " thingID=" +
                    entry.ThingIDNumber +
                    " phase=" +
                    (phase ?? "null") +
                    " dump=" +
                    entry.DumpForDebug();
                return false;
            }

            return true;
        }

        private void EnsureHolderRoots()
        {
            if (this.medicalBayPatientStagingRoot == null)
            {
                this.medicalBayPatientStagingRoot = new ThingOwnerHolderRoot(
                    this,
                    () => this.MedicalBayPatientStagingThings,
                    "CompShuttleHolderLaunchTransferState.medicalBayPatientStagingThings");
            }

            if (this.emergencyRecoveryRoot == null)
            {
                this.emergencyRecoveryRoot = new ThingOwnerHolderRoot(
                    this,
                    () => this.EmergencyRecoveryThings,
                    "CompShuttleHolderLaunchTransferState.emergencyRecoveryThings");
            }

            if (this.devHabitatLivingStagingRoot == null)
            {
                this.devHabitatLivingStagingRoot = new ThingOwnerHolderRoot(
                    this,
                    () => this.DevHabitatLivingExportContainer,
                    "CompShuttleHolderLaunchTransferState.devHabitatLivingActiveTransporter.innerContainer");
            }
        }

        private void EnsureInitialized()
        {
            if (this.devTestHeldThings == null)
            {
                this.devTestHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.medicalBayPatientStagingThings == null)
            {
                this.medicalBayPatientStagingThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.emergencyRecoveryThings == null)
            {
                this.emergencyRecoveryThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.devHabitatLivingActiveTransporter == null ||
                this.devHabitatLivingActiveTransporter.innerContainer == null)
            {
                this.devHabitatLivingActiveTransporter = new ActiveTransporterInfo();
            }

            if (this.manifest == null)
            {
                this.manifest = new ShuttleHolderLaunchManifest();
            }

            this.manifest.EnsureInitialized();

            if (this.refrigeratedCargoLaunchManifest == null)
            {
                this.refrigeratedCargoLaunchManifest = new RefrigeratedCargoLaunchManifest();
            }

            this.refrigeratedCargoLaunchManifest.EnsureInitialized();

            if (this.refrigeratedLaunchStagingHolders == null)
            {
                this.refrigeratedLaunchStagingHolders = new List<RefrigeratedLaunchStagingHolder>();
            }

            for (int i = 0; i < this.refrigeratedLaunchStagingHolders.Count; i++)
            {
                RefrigeratedLaunchStagingHolder holder = this.refrigeratedLaunchStagingHolders[i];
                if (holder != null)
                {
                    holder.EnsureInitialized();
                    holder.SetParentHolder(this);
                }
            }

            this.EnsureHolderRoots();
        }

        private string DescribeRecoveryThing(Thing thing)
        {
            if (thing == null)
            {
                return "thing=null";
            }

            return "thingID=" +
                thing.thingIDNumber +
                " defName=" +
                (thing.def != null ? thing.def.defName : "null") +
                " stackCount=" +
                thing.stackCount +
                " pawn=" +
                (thing is Pawn);
        }

        private string DescribeOwner(ThingOwner owner)
        {
            if (owner == null)
            {
                return "null";
            }

            return owner.GetType().Name + " count=" + owner.Count;
        }
    }
}

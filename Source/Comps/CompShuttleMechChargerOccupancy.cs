using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttleMechChargerOccupancy : CompProperties
    {
        public CompProperties_ShuttleMechChargerOccupancy()
        {
            this.compClass = typeof(CompShuttleMechChargerOccupancy);
        }
    }

    /// <summary>
    /// Dedicated holder for mechs charging inside shuttle mech charger modules.
    /// It owns containment, charge records, save/load reconciliation, and safe ejection.
    /// </summary>
    public sealed partial class CompShuttleMechChargerOccupancy : ThingComp, IThingHolder
    {
        internal const float VanillaMechChargePerTick = 0.00083333335f;
        private const int ReservationCleanupIntervalTicks = 120;
        private const int RecordReconcileIntervalTicks = 250;
        private const int InvalidMechEjectIntervalTicks = 250;

        private ThingOwner<Thing> mechChargingHeldThings;
        private List<ShuttleMechChargingRecord> chargingRecords =
            new List<ShuttleMechChargingRecord>();
        private List<ShuttleMechChargingReservation> chargingReservations =
            new List<ShuttleMechChargingReservation>();
        private int nextReservationCleanupTick = -1;
        private int nextRecordReconcileTick = -1;
        private int nextInvalidMechEjectTick = -1;

        public CompShuttleMechChargerOccupancy()
        {
            this.mechChargingHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
        }

        internal bool HasChargingMechs
        {
            get
            {
                return this.ChargingMechCount > 0;
            }
        }

        internal int ChargingMechCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountValidChargingRecords();
            }
        }

        internal int FreeChargingSlots
        {
            get
            {
                int slots = this.GetMechChargeSlots();
                this.ClearStaleChargingReservations();
                int free = slots - this.ChargingMechCount - this.CountChargingReservationsExcluding(-1);
                return free > 0 ? free : 0;
            }
        }

        internal List<Pawn> HeldChargingMechs
        {
            get
            {
                return this.GetHeldChargingMechsForReading();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref this.mechChargingHeldThings, "mechChargingHeldThings", new object[] { this });
            Scribe_Collections.Look(
                ref this.chargingRecords,
                "mechChargingRecords",
                LookMode.Deep,
                System.Array.Empty<object>());
            Scribe_Collections.Look(
                ref this.chargingReservations,
                "mechChargingReservations",
                LookMode.Deep,
                System.Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.ResetTickIntervals();
                this.ReconcileChargingRecordsToHeldMechs();
                this.ClearStaleChargingReservations();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            this.EnsureInitialized();
            this.MaybeReconcileChargingRecords();
            this.MaybeCleanupReservations();
            this.TickChargingMechs();
            this.MaybeEjectInvalidMechs();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                return;
            }

            this.EnsureInitialized();
            this.ResetTickIntervals();
            this.ReconcileChargingRecordsToHeldMechs();
            this.ClearStaleChargingReservations();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap != null)
            {
                if (!this.TryEjectAllChargingMechs(previousMap, out string failureReason) && this.HasChargingMechs)
                {
                    Log.Error("[CeleTech Shuttle] Failed to eject all mech charger occupants while shuttle was destroyed on-map. " + failureReason);
                }

                return;
            }

            this.PreserveDestroyedHolderContents(mode);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.mechChargingHeldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        internal bool ContainsChargingMech(Pawn mech)
        {
            if (mech == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return this.IsContainedChargingMech(mech);
        }

        internal bool TryReserveCharging(Pawn actor, Pawn mech, out string failReason)
        {
            return MechChargerOccupancyAdmissionService.TryReserveCharging(
                this,
                actor,
                mech,
                out failReason);
        }

        internal void ReleaseChargingReservation(Pawn mech)
        {
            this.ReleaseChargingReservation(mech != null ? mech.thingIDNumber : -1);
        }

        internal void ReleaseChargingReservation(int mechThingID)
        {
            this.EnsureInitialized();
            if (mechThingID <= 0 || this.chargingReservations == null)
            {
                return;
            }

            for (int i = this.chargingReservations.Count - 1; i >= 0; i--)
            {
                ShuttleMechChargingReservation reservation = this.chargingReservations[i];
                if (reservation == null || reservation.MechThingID == mechThingID)
                {
                    this.chargingReservations.RemoveAt(i);
                }
            }
        }

        internal int CountPendingChargingReservations(Pawn ignoredMech)
        {
            this.EnsureInitialized();
            this.ClearStaleChargingReservations();
            return this.CountChargingReservationsExcluding(
                ignoredMech != null ? ignoredMech.thingIDNumber : -1);
        }

        internal bool HasChargingReservationForMechID(int mechThingID)
        {
            this.EnsureInitialized();
            this.ClearStaleChargingReservations();
            return this.FindChargingReservation(mechThingID) != null;
        }

        internal bool TryEnterForCharging(Pawn mech, out string failureReason)
        {
            return MechChargerOccupancyAdmissionService.TryEnterForCharging(
                this,
                mech,
                out failureReason);
        }

        internal bool TryEjectChargingMech(Pawn mech, out string failureReason)
        {
            return MechChargerOccupancyEjectionService.TryEjectChargingMech(
                this,
                mech,
                out failureReason);
        }

        internal bool TryEjectAllChargingMechs(Map map, out string failureReason)
        {
            return MechChargerOccupancyEjectionService.TryEjectAllChargingMechs(
                this,
                map,
                out failureReason);
        }

        internal bool IsChargingComplete(Pawn mech)
        {
            return ShuttleMechChargeNeedUtility.IsAtRechargeLimit(mech);
        }

        internal bool CanTransferChargingMechsForLaunch(out string failureReason)
        {
            return MechChargerLaunchTransferAdapter.CanTransferChargingMechsForLaunch(
                this,
                out failureReason);
        }

        internal bool TryExportChargingMechsForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> destination,
            out string failureReason)
        {
            return MechChargerLaunchTransferAdapter.TryExportChargingMechsForLaunch(
                this,
                manifest,
                destination,
                out failureReason);
        }

        internal bool TryRollbackChargingMechLaunchExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> source,
            out string notice)
        {
            return MechChargerLaunchTransferAdapter.TryRollbackChargingMechLaunchExport(
                this,
                manifest,
                source,
                out notice);
        }

        internal bool TryRestoreChargingMechsFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> primarySource,
            ThingOwner<Thing> secondarySource,
            Map map,
            IntVec3 fallbackCell,
            ThingOwner<Thing> emergencyRecoveryOwner,
            out string notice)
        {
            return MechChargerLaunchTransferAdapter.TryRestoreChargingMechsFromLaunchStaging(
                this,
                manifest,
                primarySource,
                secondarySource,
                map,
                fallbackCell,
                emergencyRecoveryOwner,
                out notice);
        }

        private bool TryEjectChargingMech(Pawn mech, Map map, out string failureReason)
        {
            return MechChargerOccupancyEjectionService.TryEjectChargingMech(
                this,
                mech,
                map,
                out failureReason);
        }

        private void TickChargingMechs()
        {
            MechChargerOccupancyTickService.TickChargingMechs(this);
        }

        private bool TryResolveRecordModule(ShuttleMechChargingRecord record)
        {
            if (record == null)
            {
                return false;
            }

            if (this.IsModuleAvailableForCharging(record.ModuleInstanceID, record.PawnThingID))
            {
                return true;
            }

            string moduleInstanceID;
            float chargeRateFactor;
            if (!this.TryFindAvailableChargerModule(
                record.PawnThingID,
                out moduleInstanceID,
                out chargeRateFactor))
            {
                return false;
            }

            record.SetModuleAssignment(moduleInstanceID, chargeRateFactor);
            return true;
        }

        private bool TryFindAvailableChargerModule(
            int ignoredMechThingID,
            out string moduleInstanceID,
            out float chargeRateFactor)
        {
            moduleInstanceID = null;
            chargeRateFactor = 1f;
            ShuttleController controller = this.GetController();
            ShuttleAssemblyState assemblyState = controller != null ? controller.AssemblyState : null;
            if (assemblyState == null || assemblyState.Modules == null)
            {
                return false;
            }

            float bestRate = -1f;
            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                ShuttleMechChargerModuleDef mechChargerDef = module != null
                    ? module.ModuleDef as ShuttleMechChargerModuleDef
                    : null;
                if (mechChargerDef == null ||
                    !module.IsEnabled ||
                    mechChargerDef.mechChargeSlots <= 0)
                {
                    continue;
                }

                int assignedCount = this.CountAssignedToModule(
                    module.ModuleInstanceID,
                    ignoredMechThingID);
                if (assignedCount >= mechChargerDef.mechChargeSlots)
                {
                    continue;
                }

                float rate = this.GetValidChargeRateFactor(mechChargerDef.chargeRateFactor);
                if (moduleInstanceID == null || rate > bestRate)
                {
                    moduleInstanceID = module.ModuleInstanceID;
                    chargeRateFactor = rate;
                    bestRate = rate;
                }
            }

            return !string.IsNullOrEmpty(moduleInstanceID);
        }

        private bool IsModuleAvailableForCharging(string moduleInstanceID, int ignoredMechThingID)
        {
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            ShuttleModule module = this.GetModule(moduleInstanceID);
            ShuttleMechChargerModuleDef mechChargerDef = module != null
                ? module.ModuleDef as ShuttleMechChargerModuleDef
                : null;
            if (module == null ||
                mechChargerDef == null ||
                !module.IsEnabled ||
                mechChargerDef.mechChargeSlots <= 0)
            {
                return false;
            }

            return this.CountAssignedToModule(moduleInstanceID, ignoredMechThingID) < mechChargerDef.mechChargeSlots;
        }

        private int CountAssignedToModule(string moduleInstanceID, int ignoredMechThingID)
        {
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return 0;
            }

            int count = 0;
            if (this.chargingRecords != null)
            {
                for (int i = 0; i < this.chargingRecords.Count; i++)
                {
                    ShuttleMechChargingRecord record = this.chargingRecords[i];
                    if (record != null &&
                        record.PawnThingID != ignoredMechThingID &&
                        record.ModuleInstanceID == moduleInstanceID)
                    {
                        count++;
                    }
                }
            }

            if (this.chargingReservations != null)
            {
                for (int i = 0; i < this.chargingReservations.Count; i++)
                {
                    ShuttleMechChargingReservation reservation = this.chargingReservations[i];
                    if (reservation != null &&
                        reservation.MechThingID != ignoredMechThingID &&
                        reservation.ModuleInstanceID == moduleInstanceID)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private ShuttleModule GetModule(string moduleInstanceID)
        {
            ShuttleController controller = this.GetController();
            ShuttleAssemblyState assemblyState = controller != null ? controller.AssemblyState : null;
            return assemblyState != null ? assemblyState.GetModule(moduleInstanceID) : null;
        }

        private ShuttleController GetController()
        {
            ThingWithComps host = this.parent as ThingWithComps;
            CompModularShuttleCore core = host != null
                ? host.TryGetComp<CompModularShuttleCore>()
                : null;
            return core != null ? core.Controller : null;
        }

        private bool TryGetMechChargerProfile(out MechChargerProfile mechCharger)
        {
            mechCharger = null;
            ShuttleController controller = this.GetController();
            ShuttleProfile profile = controller != null ? controller.GetProfileForRead() : null;
            mechCharger = profile != null ? profile.MechCharger : null;
            return mechCharger != null;
        }

        private int GetMechChargeSlots()
        {
            MechChargerProfile mechCharger;
            return this.TryGetMechChargerProfile(out mechCharger) && mechCharger.HasMechCharger
                ? mechCharger.MechChargeSlots
                : 0;
        }

        private float GetValidChargeRateFactor(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f
                ? value
                : 1f;
        }

        private void HandleInvalidChargingRecord(ShuttleMechChargingRecord record, Map map)
        {
            Pawn mech = record != null ? record.Pawn : null;
            if (mech != null && mech.Destroyed)
            {
                this.ReleaseChargingReservation(mech.thingIDNumber);
                this.mechChargingHeldThings.Remove(mech);
                this.chargingRecords.Remove(record);
                return;
            }

            if (!this.IsHeldPawn(mech))
            {
                this.ReleaseChargingReservation(record != null ? record.PawnThingID : -1);
                this.chargingRecords.Remove(record);
                return;
            }

            if (map == null)
            {
                this.LogMechPreservedWithoutMap(record, true);
                return;
            }

            this.TryEjectChargingMech(mech, map, out string unusedFailureReason);
        }

        private bool IsValidHeldChargingMech(Pawn mech)
        {
            return mech != null &&
                !mech.Destroyed &&
                !mech.Dead &&
                !mech.Spawned &&
                this.IsHeldPawn(mech) &&
                mech.RaceProps != null &&
                mech.RaceProps.IsMechanoid &&
                ShuttleMechChargeNeedUtility.HasChargeNeed(mech);
        }

        private void ReconcileChargingRecordsToHeldMechs()
        {
            this.EnsureInitialized();
            this.RemoveInvalidChargingRecords();

            for (int i = 0; i < this.mechChargingHeldThings.Count; i++)
            {
                Pawn pawn = this.mechChargingHeldThings[i] as Pawn;
                if (pawn == null || this.HasRecordFor(pawn))
                {
                    continue;
                }

                string moduleInstanceID;
                float chargeRateFactor;
                if (!this.TryFindAvailableChargerModule(
                    pawn.thingIDNumber,
                    out moduleInstanceID,
                    out chargeRateFactor))
                {
                    moduleInstanceID = string.Empty;
                    chargeRateFactor = 1f;
                }

                this.chargingRecords.Add(new ShuttleMechChargingRecord(
                    pawn,
                    moduleInstanceID,
                    chargeRateFactor,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1));
            }
        }

        private void RemoveInvalidChargingRecords()
        {
            HashSet<int> seenMechThingIDs = new HashSet<int>();
            for (int i = this.chargingRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMechChargingRecord record = this.chargingRecords[i];
                if (record == null)
                {
                    this.chargingRecords.RemoveAt(i);
                    continue;
                }

                record.Sanitize();
                if (record.PawnThingID > 0 && !seenMechThingIDs.Add(record.PawnThingID))
                {
                    this.ReleaseChargingReservation(record.PawnThingID);
                    this.chargingRecords.RemoveAt(i);
                    continue;
                }

                Pawn pawn = record.Pawn;
                if (pawn == null && record.PawnThingID > 0)
                {
                    pawn = this.FindRawHeldPawnByThingID(record.PawnThingID);
                    if (pawn != null)
                    {
                        record.SetPawn(pawn);
                    }
                }

                if (pawn == null || !this.IsHeldPawn(pawn))
                {
                    this.ReleaseChargingReservation(record.PawnThingID);
                    this.chargingRecords.RemoveAt(i);
                }
            }
        }

        private bool HasRecordFor(Pawn pawn)
        {
            if (pawn == null || this.chargingRecords == null)
            {
                return false;
            }

            for (int i = 0; i < this.chargingRecords.Count; i++)
            {
                ShuttleMechChargingRecord record = this.chargingRecords[i];
                if (record != null && record.Pawn == pawn && this.IsHeldPawn(pawn))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveChargingRecord(Pawn pawn)
        {
            int pawnThingID = pawn != null ? pawn.thingIDNumber : -1;
            for (int i = this.chargingRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMechChargingRecord record = this.chargingRecords[i];
                if (record == null ||
                    record.Pawn == pawn ||
                    (pawnThingID > 0 && record.PawnThingID == pawnThingID))
                {
                    this.chargingRecords.RemoveAt(i);
                }
            }
        }

        private bool IsContainedChargingMech(Pawn mech)
        {
            return mech != null && this.IsHeldPawn(mech) && this.HasRecordFor(mech);
        }

        private bool IsHeldPawn(Pawn pawn)
        {
            return pawn != null &&
                this.mechChargingHeldThings != null &&
                this.mechChargingHeldThings.Contains(pawn);
        }

        private int CountValidChargingRecords()
        {
            List<Pawn> countedPawns = new List<Pawn>();
            if (this.chargingRecords != null)
            {
                for (int i = 0; i < this.chargingRecords.Count; i++)
                {
                    ShuttleMechChargingRecord record = this.chargingRecords[i];
                    if (record != null && this.IsHeldPawn(record.Pawn) && !countedPawns.Contains(record.Pawn))
                    {
                        countedPawns.Add(record.Pawn);
                    }
                }
            }

            if (this.mechChargingHeldThings != null)
            {
                for (int i = 0; i < this.mechChargingHeldThings.Count; i++)
                {
                    Pawn pawn = this.mechChargingHeldThings[i] as Pawn;
                    if (pawn != null && !countedPawns.Contains(pawn))
                    {
                        countedPawns.Add(pawn);
                    }
                }
            }

            return countedPawns.Count;
        }

        private List<Pawn> GetHeldChargingMechsForReading()
        {
            this.EnsureInitialized();
            this.ReconcileChargingRecordsToHeldMechs();
            List<Pawn> mechs = new List<Pawn>();
            for (int i = 0; i < this.chargingRecords.Count; i++)
            {
                Pawn mech = this.chargingRecords[i] != null ? this.chargingRecords[i].Pawn : null;
                if (this.IsHeldPawn(mech) && !mechs.Contains(mech))
                {
                    mechs.Add(mech);
                }
            }

            for (int i = 0; i < this.mechChargingHeldThings.Count; i++)
            {
                Pawn mech = this.mechChargingHeldThings[i] as Pawn;
                if (mech != null && !mech.Destroyed && !mechs.Contains(mech))
                {
                    mechs.Add(mech);
                }
            }

            return mechs;
        }

        private ShuttleMechChargingReservation FindChargingReservation(int mechThingID)
        {
            if (mechThingID <= 0 || this.chargingReservations == null)
            {
                return null;
            }

            for (int i = 0; i < this.chargingReservations.Count; i++)
            {
                ShuttleMechChargingReservation reservation = this.chargingReservations[i];
                if (reservation != null && reservation.MechThingID == mechThingID)
                {
                    return reservation;
                }
            }

            return null;
        }

        private int CountChargingReservationsExcluding(int ignoredMechThingID)
        {
            if (this.chargingReservations == null || this.chargingReservations.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < this.chargingReservations.Count; i++)
            {
                ShuttleMechChargingReservation reservation = this.chargingReservations[i];
                if (reservation != null &&
                    reservation.MechThingID > 0 &&
                    reservation.MechThingID != ignoredMechThingID)
                {
                    count++;
                }
            }

            return count;
        }

        internal void ClearStaleChargingReservations()
        {
            this.ClearStaleChargingReservations(this.BuildSpawnedPawnLookup());
        }

        private void ClearStaleChargingReservations(Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (this.chargingReservations == null || this.chargingReservations.Count == 0)
            {
                return;
            }

            for (int i = this.chargingReservations.Count - 1; i >= 0; i--)
            {
                ShuttleMechChargingReservation reservation = this.chargingReservations[i];
                if (reservation == null || this.IsChargingReservationStale(reservation, spawnedPawnsByThingID))
                {
                    this.chargingReservations.RemoveAt(i);
                }
            }
        }

        private bool IsChargingReservationStale(
            ShuttleMechChargingReservation reservation,
            Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (reservation == null || reservation.MechThingID <= 0)
            {
                return true;
            }

            if (this.FindHeldMechByThingID(reservation.MechThingID) != null)
            {
                return true;
            }

            Pawn mech = null;
            if (spawnedPawnsByThingID != null)
            {
                spawnedPawnsByThingID.TryGetValue(reservation.MechThingID, out mech);
            }

            if (this.IsCurrentChargingJobForReservation(reservation, mech))
            {
                return false;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            return currentTick < 0 ||
                reservation.ReservationTick < 0 ||
                currentTick - reservation.ReservationTick > 250;
        }

        private bool IsCurrentChargingJobForReservation(
            ShuttleMechChargingReservation reservation,
            Pawn mech)
        {
            if (reservation == null || mech == null || mech.CurJob == null)
            {
                return false;
            }

            JobDef chargeJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MechChargerAdmissionValidator.ShuttleMechChargeJobDefName);
            return chargeJobDef != null &&
                mech.CurJob.def == chargeJobDef &&
                TargetReferences(mech.CurJob.GetTarget(TargetIndex.A), this.parent);
        }

        private Pawn FindHeldMechByThingID(int mechThingID)
        {
            if (mechThingID <= 0)
            {
                return null;
            }

            return this.FindRawHeldPawnByThingID(mechThingID);
        }

        private Pawn FindRawHeldPawnByThingID(int pawnThingID)
        {
            if (pawnThingID <= 0 || this.mechChargingHeldThings == null)
            {
                return null;
            }

            for (int i = 0; i < this.mechChargingHeldThings.Count; i++)
            {
                Pawn pawn = this.mechChargingHeldThings[i] as Pawn;
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    return pawn;
                }
            }

            return null;
        }

        private Dictionary<int, Pawn> BuildSpawnedPawnLookup()
        {
            Dictionary<int, Pawn> lookup = new Dictionary<int, Pawn>();
            Map map = this.parent != null ? this.parent.Map : null;
            IReadOnlyList<Pawn> pawns = map != null && map.mapPawns != null
                ? map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return lookup;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed && pawn.thingIDNumber > 0)
                {
                    lookup[pawn.thingIDNumber] = pawn;
                }
            }

            return lookup;
        }

        private void MaybeReconcileChargingRecords()
        {
            if (!this.ShouldRunInterval(ref this.nextRecordReconcileTick, RecordReconcileIntervalTicks))
            {
                return;
            }

            this.ReconcileChargingRecordsToHeldMechs();
        }

        private void MaybeCleanupReservations()
        {
            if (!this.ShouldRunInterval(ref this.nextReservationCleanupTick, ReservationCleanupIntervalTicks))
            {
                return;
            }

            this.ClearStaleChargingReservations(this.BuildSpawnedPawnLookup());
        }

        private void MaybeEjectInvalidMechs()
        {
            if (!this.ShouldRunInterval(ref this.nextInvalidMechEjectTick, InvalidMechEjectIntervalTicks))
            {
                return;
            }

            this.EjectInvalidMechsIfPossible();
        }

        private void EjectInvalidMechsIfPossible()
        {
            if (this.chargingRecords == null || this.chargingRecords.Count == 0)
            {
                return;
            }

            Map map = this.parent != null ? this.parent.Map : null;
            for (int i = this.chargingRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMechChargingRecord record = this.chargingRecords[i];
                Pawn mech = record != null ? record.Pawn : null;
                bool invalidMech = mech == null ||
                    mech.Dead ||
                    mech.Destroyed ||
                    !this.IsHeldPawn(mech) ||
                    !this.TryResolveRecordModule(record);
                if (!invalidMech)
                {
                    continue;
                }

                this.HandleInvalidChargingRecord(record, map);
            }
        }

        private bool ShouldRunInterval(ref int nextTick, int intervalTicks)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            if (ticksGame < 0)
            {
                return true;
            }

            if (nextTick < 0 ||
                ticksGame >= nextTick ||
                ticksGame < nextTick - intervalTicks * 4)
            {
                nextTick = ticksGame + intervalTicks;
                return true;
            }

            return false;
        }

        private IntVec3 GetEjectCell(Map map)
        {
            if (this.parent == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 cell = this.parent.InteractionCell;
            if (map != null && cell.IsValid && cell.InBounds(map))
            {
                return cell;
            }

            return this.parent.Position;
        }

        private CompShuttleHolderLaunchTransferState GetHolderTransferState()
        {
            ThingWithComps host = this.parent as ThingWithComps;
            return host != null
                ? host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }

        private void HandleFailedPawnHolderTransfer(
            Pawn pawn,
            PawnHolderTransferResult transferResult,
            string operation)
        {
            if (transferResult == null)
            {
                Log.Error("[CeleTech Shuttle] Mech charger pawn holder transfer returned null result. operation=" +
                    (operation ?? "null"));
                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.FatalOwnerless)
            {
                Log.Error("[CeleTech Shuttle] Mech charger pawn holder transfer fatal. operation=" +
                    (operation ?? "null") +
                    " context=" +
                    (transferResult.DebugContext ?? "null") +
                    " reason=" +
                    (transferResult.FailureReason ?? "null"));
                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.RecoveredToEmergencyOwner)
            {
                Log.Warning("[CeleTech Shuttle] Mech charger pawn holder transfer recovered mech to emergency owner; no charging record was created. operation=" +
                    (operation ?? "null") +
                    " context=" +
                    (transferResult.DebugContext ?? "null") +
                    " reason=" +
                    (transferResult.FailureReason ?? "null"));
                if (transferResult.WasSelected && this.parent != null)
                {
                    Find.Selector.Select(this.parent, false, false);
                }

                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.RecoveredToMap &&
                transferResult.WasSelected &&
                pawn != null &&
                pawn.Spawned)
            {
                Find.Selector.Select(pawn, false, false);
            }
        }

        private string GetPawnHolderTransferFailureReason(
            PawnHolderTransferResult transferResult,
            string defaultReason)
        {
            if (transferResult != null && !string.IsNullOrEmpty(transferResult.FailureReason))
            {
                return transferResult.FailureReason;
            }

            return defaultReason;
        }

        private bool TargetReferences(LocalTargetInfo target, Thing thing)
        {
            return thing != null && target.IsValid && target.Thing == thing;
        }

        private void ResetTickIntervals()
        {
            this.nextReservationCleanupTick = -1;
            this.nextRecordReconcileTick = -1;
            this.nextInvalidMechEjectTick = -1;
        }

        private void EnsureInitialized()
        {
            if (this.mechChargingHeldThings == null)
            {
                this.mechChargingHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.chargingRecords == null)
            {
                this.chargingRecords = new List<ShuttleMechChargingRecord>();
            }

            if (this.chargingReservations == null)
            {
                this.chargingReservations = new List<ShuttleMechChargingReservation>();
            }
        }

        private void LogMechPreservedWithoutMap(ShuttleMechChargingRecord record, bool mechChargerUnavailable)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(
                this.GetMechChargerLogKey(
                    "preserved-without-map",
                    (mechChargerUnavailable ? "charger-missing:" : "map-missing:") +
                        (record != null ? record.PawnThingID.ToString() : "null"))))
            {
                return;
            }

            Log.Warning(
                "[CeleTech Shuttle] Preserving mech charger occupant because no map is available for safe eject. " +
                "mechChargerUnavailable=" + mechChargerUnavailable +
                " mech=" + (record != null ? record.PawnLabel : "null"));
        }

        private string GetMechChargerLogKey(string category, string detail)
        {
            return "MechCharger:" +
                (this.parent != null ? this.parent.thingIDNumber : -1) +
                ":" +
                (category ?? "unknown") +
                ":" +
                (detail ?? "null");
        }

        private void PreserveDestroyedHolderContents(DestroyMode mode)
        {
            this.EnsureInitialized();
            if (this.HasChargingMechs)
            {
                Log.Error(
                    "[CeleTech Shuttle] Shuttle mech charger occupancy was destroyed without a map while mechs were inside. " +
                    "Preserving contained mechs instead of killing them. destroyMode=" + mode);
                return;
            }

            this.chargingRecords.Clear();
            this.chargingReservations.Clear();
        }
    }

    internal sealed class ShuttleMechChargingReservation : IExposable
    {
        internal int MechThingID;
        internal int ActorThingID;
        internal string ModuleInstanceID;
        internal float ChargeRateFactor = 1f;
        internal int ReservationTick;

        public ShuttleMechChargingReservation()
        {
        }

        internal ShuttleMechChargingReservation(
            int mechThingID,
            int actorThingID,
            string moduleInstanceID,
            float chargeRateFactor,
            int reservationTick)
        {
            this.MechThingID = mechThingID;
            this.ActorThingID = actorThingID;
            this.ModuleInstanceID = moduleInstanceID;
            this.ChargeRateFactor = chargeRateFactor;
            this.ReservationTick = reservationTick;
            this.Sanitize();
        }

        internal void SetModuleAssignment(string moduleInstanceID, float chargeRateFactor)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.ChargeRateFactor = chargeRateFactor;
            this.Sanitize();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.MechThingID, "mechThingID", -1);
            Scribe_Values.Look(ref this.ActorThingID, "actorThingID", -1);
            Scribe_Values.Look(ref this.ModuleInstanceID, "moduleInstanceID", string.Empty);
            Scribe_Values.Look(ref this.ChargeRateFactor, "chargeRateFactor", 1f);
            Scribe_Values.Look(ref this.ReservationTick, "reservationTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }

        private void Sanitize()
        {
            if (this.MechThingID < -1)
            {
                this.MechThingID = -1;
            }

            if (this.ActorThingID < -1)
            {
                this.ActorThingID = -1;
            }

            if (this.ModuleInstanceID == null)
            {
                this.ModuleInstanceID = string.Empty;
            }

            if (float.IsNaN(this.ChargeRateFactor) ||
                float.IsInfinity(this.ChargeRateFactor) ||
                this.ChargeRateFactor <= 0f)
            {
                this.ChargeRateFactor = 1f;
            }

            if (this.ReservationTick < -1)
            {
                this.ReservationTick = -1;
            }
        }
    }
}

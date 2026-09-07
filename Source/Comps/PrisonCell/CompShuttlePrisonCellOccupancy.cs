using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttlePrisonCellOccupancy : CompProperties
    {
        public CompProperties_ShuttlePrisonCellOccupancy()
        {
            this.compClass = typeof(CompShuttlePrisonCellOccupancy);
        }
    }

    /// <summary>
    /// Minimal Prison Cell holder foundation. It owns contained pawns and records only;
    /// prisoner status, warden work, feeding, tending, escape risk, UI, and launch
    /// transfer are intentionally handled by later services.
    /// </summary>
    public sealed partial class CompShuttlePrisonCellOccupancy : ThingComp, IThingHolder
    {
        private const string PrisonerHolderSaveLabel = "prisonCellHeldThings";
        private const string PrisonerRecordsSaveLabel = "shuttlePrisonerRecords";

        private ThingOwner<Thing> prisonCellHeldThings;
        private List<ShuttlePrisonerRecord> prisonerRecords =
            new List<ShuttlePrisonerRecord>();

        public CompShuttlePrisonCellOccupancy()
        {
            this.prisonCellHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
        }

        internal bool HasPrisoners
        {
            get { return this.PrisonerCount > 0; }
        }

        internal int PrisonerCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountHeldPrisoners();
            }
        }

        internal int FreePrisonerSlots
        {
            get
            {
                this.EnsureInitialized();
                int free = this.GetPrisonerSlots() - this.CountHeldPrisoners();
                return free > 0 ? free : 0;
            }
        }

        internal IReadOnlyList<Pawn> HeldPrisonersForReading
        {
            get { return this.GetHeldPrisonersForReading(); }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref this.prisonCellHeldThings, PrisonerHolderSaveLabel, new object[] { this });
            Scribe_Collections.Look(
                ref this.prisonerRecords,
                PrisonerRecordsSaveLabel,
                LookMode.Deep,
                System.Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.ReconcilePrisonerRecords();
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            this.EnsureInitialized();
            if (!this.HasPrisoners)
            {
                return;
            }

            string failureReason;
            if (!this.TryRecoverAllPrisonersDuringDestroy(previousMap, out failureReason) && this.HasPrisoners)
            {
                Log.Error("[CeleTech Shuttle] Prison Cell destroy recovery did not complete. parent=" +
                    this.parent +
                    " reason=" +
                    (failureReason ?? "unknown"));
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.prisonCellHeldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        internal bool ContainsPrisoner(Pawn pawn)
        {
            return this.IsHeldPawn(pawn);
        }

        internal Pawn FindHeldPrisonerByThingID(int thingIDNumber)
        {
            this.EnsureInitialized();
            return this.FindHeldPrisonerByThingIDNoReconcile(thingIDNumber);
        }

        internal void MarkPrisonerFedForInternalUse(
            int pawnThingIDNumber,
            int tick,
            float nutrition,
            string foodLabel)
        {
            this.EnsureInitialized();
            ShuttlePrisonerRecord record = this.FindPrisonerRecordByThingID(pawnThingIDNumber);
            if (record != null)
            {
                record.MarkFed(tick, nutrition, foodLabel);
            }
        }

        internal void MarkPrisonerTendedForInternalUse(
            int pawnThingIDNumber,
            int tick,
            string medicineLabel)
        {
            this.EnsureInitialized();
            ShuttlePrisonerRecord record = this.FindPrisonerRecordByThingID(pawnThingIDNumber);
            if (record != null)
            {
                record.MarkTended(tick, medicineLabel);
            }
        }

        internal ShuttlePrisonerRecord FindPrisonerRecordForInternalUse(int pawnThingIDNumber)
        {
            this.EnsureInitialized();
            return this.FindPrisonerRecordByThingID(pawnThingIDNumber);
        }

        /// <summary>
        /// Spawned-pawn admission only. P3 carry jobs must not call this at the
        /// destination with a carried pawn; carried admission needs a separate
        /// transfer from the carrier carryTracker/inner container into this holder.
        /// </summary>
        internal bool TryAdmitSpawnedPrisonerForInternalUse(
            Pawn prisoner,
            ShuttlePrisonerAdmissionContext context,
            out string failReason)
        {
            return PrisonCellOccupancyAdmissionService.TryAdmitSpawnedPrisonerForInternalUse(
                this,
                prisoner,
                context,
                out failReason);
        }

        /// <summary>
        /// Carried-pawn admission only. This is the P3 job-terminal path: the
        /// prisoner must already be in carrier.carryTracker, and guest/prisoner
        /// status must already have been prepared by ShuttlePrisonerGuestStatusService.
        /// </summary>
        internal bool TryAdmitCarriedPrisonerForInternalUse(
            Pawn carrier,
            Pawn prisoner,
            ShuttlePrisonerAdmissionContext context,
            out string failReason)
        {
            return PrisonCellOccupancyAdmissionService.TryAdmitCarriedPrisonerForInternalUse(
                this,
                carrier,
                prisoner,
                context,
                out failReason);
        }

        internal bool TryEjectPrisonerByThingID(
            int pawnThingIDNumber,
            out bool hadPrisoner,
            out string failReason)
        {
            return PrisonCellOccupancyEjectionService.TryEjectPrisonerByThingID(
                this,
                pawnThingIDNumber,
                out hadPrisoner,
                out failReason);
        }

        internal bool TryEjectPrisoner(Pawn prisoner, out string failReason)
        {
            return PrisonCellOccupancyEjectionService.TryEjectPrisoner(
                this,
                prisoner,
                out failReason);
        }

        internal bool TryEjectAllPrisoners(out string failReason)
        {
            return PrisonCellOccupancyEjectionService.TryEjectAllPrisoners(
                this,
                out failReason);
        }

        internal bool TryEjectAllPrisoners(Map map, out string failReason)
        {
            return PrisonCellOccupancyEjectionService.TryEjectAllPrisoners(
                this,
                map,
                out failReason);
        }

        internal bool CanTransferPrisonersForLaunch(out string failReason)
        {
            return PrisonCellLaunchTransferAdapter.CanTransferPrisonersForLaunch(
                this,
                out failReason);
        }

        internal bool TryExportPrisonersForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> destination,
            bool requireEmptyDestination,
            out string failReason)
        {
            return PrisonCellLaunchTransferAdapter.TryExportPrisonersForLaunch(
                this,
                manifest,
                destination,
                requireEmptyDestination,
                out failReason);
        }

        internal bool TryRollbackPrisonerLaunchExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> source,
            out string notice)
        {
            return PrisonCellLaunchTransferAdapter.TryRollbackPrisonerLaunchExport(
                this,
                manifest,
                source,
                out notice);
        }

        internal bool TryRestorePrisonersFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> primarySource,
            ThingOwner<Thing> secondarySource,
            out string notice)
        {
            return PrisonCellLaunchTransferAdapter.TryRestorePrisonersFromLaunchStaging(
                this,
                manifest,
                primarySource,
                secondarySource,
                out notice);
        }

        private bool TryRecoverAllPrisonersDuringDestroy(Map previousMap, out string failReason)
        {
            return PrisonCellOccupancyEjectionService.TryRecoverAllPrisonersDuringDestroy(
                this,
                previousMap,
                out failReason);
        }

        private void ReconcilePrisonerRecords()
        {
            PrisonCellOccupancyReconciler.ReconcilePrisonerRecords(this);
        }

        private List<Pawn> GetHeldPrisonersForReading()
        {
            this.EnsureInitialized();
            List<Pawn> prisoners = new List<Pawn>();
            for (int i = 0; i < this.prisonCellHeldThings.Count; i++)
            {
                Pawn pawn = this.prisonCellHeldThings[i] as Pawn;
                if (pawn != null && !pawn.Destroyed && !prisoners.Contains(pawn))
                {
                    prisoners.Add(pawn);
                }
            }

            return prisoners;
        }

        private int CountHeldPrisoners()
        {
            int count = 0;
            if (this.prisonCellHeldThings == null)
            {
                return 0;
            }

            for (int i = 0; i < this.prisonCellHeldThings.Count; i++)
            {
                Pawn pawn = this.prisonCellHeldThings[i] as Pawn;
                if (pawn != null && !pawn.Destroyed)
                {
                    count++;
                }
            }

            return count;
        }

        private Pawn FindHeldPrisonerByThingIDNoReconcile(int thingIDNumber)
        {
            if (thingIDNumber <= 0 || this.prisonCellHeldThings == null)
            {
                return null;
            }

            for (int i = 0; i < this.prisonCellHeldThings.Count; i++)
            {
                Pawn pawn = this.prisonCellHeldThings[i] as Pawn;
                if (pawn != null && !pawn.Destroyed && pawn.thingIDNumber == thingIDNumber)
                {
                    return pawn;
                }
            }

            return null;
        }

        private bool IsHeldPawn(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                this.prisonCellHeldThings != null &&
                this.prisonCellHeldThings.Contains(pawn);
        }

        private void RemovePrisonerRecord(Pawn prisoner)
        {
            if (prisoner == null)
            {
                return;
            }

            this.RemovePrisonerRecordByThingID(prisoner.thingIDNumber);
        }

        private void RemovePrisonerRecordByThingID(int thingIDNumber)
        {
            if (thingIDNumber <= 0 || this.prisonerRecords == null)
            {
                return;
            }

            for (int i = this.prisonerRecords.Count - 1; i >= 0; i--)
            {
                ShuttlePrisonerRecord record = this.prisonerRecords[i];
                if (record == null || record.MatchesThingID(thingIDNumber))
                {
                    this.prisonerRecords.RemoveAt(i);
                }
            }
        }

        private ShuttlePrisonerRecord FindPrisonerRecordByThingID(int thingIDNumber)
        {
            if (thingIDNumber <= 0 || this.prisonerRecords == null)
            {
                return null;
            }

            for (int i = 0; i < this.prisonerRecords.Count; i++)
            {
                ShuttlePrisonerRecord record = this.prisonerRecords[i];
                if (record != null && record.MatchesThingID(thingIDNumber))
                {
                    return record;
                }
            }

            return null;
        }

        private bool TryFindPrisonerRecord(
            Pawn pawn,
            out ShuttlePrisonerRecord record,
            out int recordIndex)
        {
            record = null;
            recordIndex = -1;
            if (pawn == null || this.prisonerRecords == null)
            {
                return false;
            }

            for (int i = 0; i < this.prisonerRecords.Count; i++)
            {
                ShuttlePrisonerRecord candidate = this.prisonerRecords[i];
                if (candidate != null && candidate.Matches(pawn))
                {
                    record = candidate;
                    recordIndex = i;
                    return true;
                }
            }

            return false;
        }

        private int CountPrisonerRecordsForThingID(int thingIDNumber)
        {
            if (thingIDNumber <= 0 || this.prisonerRecords == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < this.prisonerRecords.Count; i++)
            {
                ShuttlePrisonerRecord record = this.prisonerRecords[i];
                if (record != null && record.MatchesThingID(thingIDNumber))
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetPrisonCellProfile(out PrisonCellProfile prisonCell)
        {
            prisonCell = null;
            ThingWithComps host = this.parent as ThingWithComps;
            CompModularShuttleCore core = host != null
                ? host.TryGetComp<CompModularShuttleCore>()
                : null;
            ShuttleProfile profile = core != null && core.Controller != null
                ? core.Controller.GetProfileForRead()
                : null;
            prisonCell = profile != null ? profile.PrisonCell : null;
            return prisonCell != null;
        }

        private int GetPrisonerSlots()
        {
            PrisonCellProfile prisonCell;
            return this.TryGetPrisonCellProfile(out prisonCell) && prisonCell.HasPrisonCell
                ? prisonCell.PrisonerSlots
                : 0;
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

        private void EnsureInitialized()
        {
            if (this.prisonCellHeldThings == null)
            {
                this.prisonCellHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.prisonerRecords == null)
            {
                this.prisonerRecords = new List<ShuttlePrisonerRecord>();
            }
        }

    }
}

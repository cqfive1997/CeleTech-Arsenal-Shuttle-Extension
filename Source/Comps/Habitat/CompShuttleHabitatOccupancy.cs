using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttleHabitatOccupancy : CompProperties
    {
        public CompProperties_ShuttleHabitatOccupancy()
        {
            this.compClass = typeof(CompShuttleHabitatOccupancy);
        }
    }

    /// <summary>
    /// Dedicated holder for pawns and food currently inside Habitat/Recreation modules.
    /// This is not cargo storage: cargo withdrawal may feed dining, but holder truth
    /// stays here so launch transfer can snapshot/restore sleep, dining, and joy records.
    /// </summary>
    public sealed partial class CompShuttleHabitatOccupancy : ThingComp, IThingHolder
    {
        private const int ThoughtCleanupIntervalTicks = 250;
        private const float FullRestLevel = 0.99f;
        private const int MinimumDiningChewTicks = 60;

        private ThingOwner<Thing> habitatHeldThings;
        private List<ShuttleHabitatOccupantRecord> sleepingOccupants =
            new List<ShuttleHabitatOccupantRecord>();
        private List<ShuttleHabitatDiningOccupantRecord> diningOccupants =
            new List<ShuttleHabitatDiningOccupantRecord>();
        private List<ShuttleHabitatJoyOccupantRecord> joyOccupants =
            new List<ShuttleHabitatJoyOccupantRecord>();

        public CompShuttleHabitatOccupancy()
        {
            this.habitatHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
        }

        internal int SleepingOccupantCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountValidSleepingRecords();
            }
        }

        internal bool HasAnySleepingOccupants
        {
            get
            {
                return this.SleepingOccupantCount > 0;
            }
        }

        internal int DiningOccupantCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountValidDiningRecords();
            }
        }

        internal int JoyOccupantCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountValidJoyRecords();
            }
        }

        internal int TotalOccupantCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountHeldPawns();
            }
        }

        internal bool HasAnyOccupants
        {
            get
            {
                return this.TotalOccupantCount > 0;
            }
        }

        internal List<Pawn> OccupantsForReading
        {
            get
            {
                return this.GetOccupantsForReading();
            }
        }

        internal List<Pawn> SleepingOccupantsForReading
        {
            get
            {
                return this.GetSleepingOccupantsForReading();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref this.habitatHeldThings, "habitatHeldThings", new object[] { this });
            Scribe_Collections.Look(
                ref this.sleepingOccupants,
                "sleepingOccupants",
                LookMode.Deep,
                Array.Empty<object>());
            Scribe_Collections.Look(
                ref this.diningOccupants,
                "diningOccupants",
                LookMode.Deep,
                Array.Empty<object>());
            Scribe_Collections.Look(
                ref this.joyOccupants,
                "joyOccupants",
                LookMode.Deep,
                Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.ReconcileRecordsToContainedPawns();
                this.ReconcileDiningRecordsToContainedThings();
                this.ReconcileJoyRecordsToContainedPawns();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            this.EnsureInitialized();

            if (this.IsCompletelyIdleForTick())
            {
                return;
            }

            this.TickSleepingOccupants();
            this.TickDiningOccupants();
            this.TickJoyOccupants();
            this.TickHeldThings();
        }

        private bool IsCompletelyIdleForTick()
        {
            return HabitatOccupancyQueryService.IsCompletelyIdle(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap != null)
            {
                if (!this.TryEjectAllToMap(previousMap, false) && this.HasAnyOccupants)
                {
                    Log.Error("[CeleTech Shuttle] Failed to eject all Habitat occupants while shuttle was destroyed on-map. Occupants were preserved in holder.");
                }
                return;
            }

            this.PreserveDestroyedHolderContents(mode);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.habitatHeldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        internal bool TryEjectAllToMap(Map map, bool completedRest)
        {
            return HabitatEjectionService.TryEjectAllToMap(
                this.CreateEjectionAccess(),
                map,
                completedRest);
        }

        internal bool TryEjectOccupantByThingID(
            int pawnThingID,
            Map map,
            out bool hadOccupant,
            out string failureReason)
        {
            return HabitatEjectionService.TryEjectOccupantByThingID(
                this.CreateEjectionAccess(),
                pawnThingID,
                map,
                out hadOccupant,
                out failureReason);
        }

        private void TickHeldThings()
        {
            if (this.habitatHeldThings == null || this.habitatHeldThings.Count == 0)
            {
                return;
            }

            // ThingWithComps does not automatically tick IThingHolder comps. Held pawns
            // need their own DoTick so Need_Rest can convert TickResting into CurLevel.
            this.habitatHeldThings.DoTick();
        }

        private bool TryGetHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.parent, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsSleep &&
                habitat.SleepSlots > 0;
        }

        private IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.parent, map);
        }

        private void EnsureInitialized()
        {
            if (this.habitatHeldThings == null)
            {
                this.habitatHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.sleepingOccupants == null)
            {
                this.sleepingOccupants = new List<ShuttleHabitatOccupantRecord>();
            }

            if (this.diningOccupants == null)
            {
                this.diningOccupants = new List<ShuttleHabitatDiningOccupantRecord>();
            }

            if (this.joyOccupants == null)
            {
                this.joyOccupants = new List<ShuttleHabitatJoyOccupantRecord>();
            }
        }

        private void PreserveDestroyedHolderContents(DestroyMode mode)
        {
            this.EnsureInitialized();
            if (this.HasAnyOccupants)
            {
                Log.Error(
                    "[CeleTech Shuttle] Shuttle Habitat occupancy was destroyed without a map while occupants were inside. " +
                    "Preserving contained pawns instead of killing them. destroyMode=" + mode);
                return;
            }

            this.sleepingOccupants.Clear();
            this.diningOccupants.Clear();
            this.joyOccupants.Clear();
        }
    }

}

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttleMedicalProcedureOccupancy : CompProperties
    {
        public CompProperties_ShuttleMedicalProcedureOccupancy()
        {
            this.compClass = typeof(CompShuttleMedicalProcedureOccupancy);
        }
    }

    /// <summary>
    /// Dedicated holder for doctors who are inside the shuttle Medical Bay for an active
    /// procedure. Patients remain owned by CompShuttleMedicalBayOccupancy.
    /// </summary>
    public sealed class CompShuttleMedicalProcedureOccupancy : ThingComp, IThingHolder
    {
        private ThingOwner<Pawn> activeDoctors;

        public CompShuttleMedicalProcedureOccupancy()
        {
            this.activeDoctors = new ThingOwner<Pawn>(this, false, LookMode.Deep, false);
        }

        public bool HasActiveDoctors
        {
            get
            {
                this.EnsureInitialized();
                return this.activeDoctors.Count > 0;
            }
        }

        public IReadOnlyList<Pawn> ActiveDoctors
        {
            get
            {
                return this.GetActiveDoctorsForReading();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref this.activeDoctors, "activeDoctors", new object[] { this });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap == null)
            {
                if (this.HasActiveDoctors)
                {
                    Log.Warning("[CeleTech Shuttle] Shuttle medical procedure doctors remain held because the shuttle was destroyed without a map.");
                }

                return;
            }

            string reason;
            if (!this.TryExitAllDoctors(this.GetEjectCell(previousMap), out reason) && this.HasActiveDoctors)
            {
                Log.Error("[CeleTech Shuttle] Failed to eject all medical procedure doctors while shuttle was destroyed on-map. " + reason);
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.activeDoctors;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public bool TryEnterDoctor(Pawn doctor, out string reason)
        {
            reason = null;
            this.EnsureInitialized();
            if (doctor != null && this.activeDoctors.Contains(doctor))
            {
                return true;
            }

            if (!this.CanEnterDoctor(doctor, out reason))
            {
                return false;
            }

            Map map = doctor.Map;
            IntVec3 fallbackCell = this.GetEjectCell(map);
            bool wasSelected = false;
            bool added = false;
            try
            {
                wasSelected = doctor.DeSpawnOrDeselect(DestroyMode.Vanish);
                added = this.activeDoctors.TryAddOrTransfer(doctor, true);
                if (!added)
                {
                    reason = "CT_Shuttle_MedicalProcedure_DoctorEnterFailed".Translate().ToString();
                }
            }
            catch (System.Exception exception)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorEnterFailed".Translate().ToString();
                Log.Error("[CeleTech Shuttle] Medical procedure doctor enter failed: " + exception);
            }
            finally
            {
                if (!added)
                {
                    this.TryRecoverDoctorToMap(doctor, map, fallbackCell);
                }
            }

            if (!added)
            {
                return false;
            }

            this.StopDoctorJobs(doctor);
            if (wasSelected)
            {
                Find.Selector.Select(this.parent, false, false);
            }

            return true;
        }

        public bool TryExitDoctor(Pawn doctor, IntVec3 preferredCell, out string reason)
        {
            reason = null;
            this.EnsureInitialized();
            if (doctor == null || doctor.Destroyed)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            if (!this.activeDoctors.Contains(doctor))
            {
                return true;
            }

            Map map = this.parent != null ? this.parent.Map : null;
            if (map == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            IntVec3 cell = preferredCell.IsValid && preferredCell.InBounds(map)
                ? preferredCell
                : this.GetEjectCell(map);
            Pawn resultingPawn;
            if (!this.activeDoctors.TryDrop(
                doctor,
                cell,
                map,
                ThingPlaceMode.Near,
                out resultingPawn,
                null,
                null))
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        public bool TryExitAllDoctors(IntVec3 preferredCell, out string reason)
        {
            reason = null;
            this.EnsureInitialized();
            List<Pawn> doctors = this.GetActiveDoctorsForReading();
            bool allExited = true;
            for (int i = doctors.Count - 1; i >= 0; i--)
            {
                string doctorReason;
                if (!this.TryExitDoctor(doctors[i], preferredCell, out doctorReason))
                {
                    allExited = false;
                    reason = doctorReason;
                }
            }

            if (!allExited && string.IsNullOrEmpty(reason))
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
            }

            return allExited && !this.HasActiveDoctors;
        }

        public bool ContainsDoctor(int doctorThingID)
        {
            return this.GetDoctorByThingID(doctorThingID) != null;
        }

        public Pawn GetDoctorByThingID(int doctorThingID)
        {
            if (doctorThingID <= 0)
            {
                return null;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.activeDoctors.Count; i++)
            {
                Pawn doctor = this.activeDoctors[i];
                if (doctor != null && doctor.thingIDNumber == doctorThingID)
                {
                    return doctor;
                }
            }

            return null;
        }

        internal bool CanEnterDoctor(Pawn doctor, out string reason)
        {
            reason = null;
            if (doctor == null ||
                doctor.Destroyed ||
                doctor.Dead ||
                !doctor.Spawned ||
                doctor.Map == null ||
                doctor.Downed)
            {
                reason = "CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (doctor.Faction != Faction.OfPlayer ||
                !doctor.IsColonist ||
                doctor.RaceProps == null ||
                !doctor.RaceProps.Humanlike ||
                doctor.MentalState != null)
            {
                reason = "CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (this.parent == null ||
                !this.parent.Spawned ||
                this.parent.Map == null ||
                doctor.Map != this.parent.Map)
            {
                reason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            return true;
        }

        private List<Pawn> GetActiveDoctorsForReading()
        {
            this.EnsureInitialized();
            List<Pawn> doctors = new List<Pawn>();
            for (int i = 0; i < this.activeDoctors.Count; i++)
            {
                Pawn doctor = this.activeDoctors[i];
                if (doctor != null && !doctor.Destroyed && !doctors.Contains(doctor))
                {
                    doctors.Add(doctor);
                }
            }

            return doctors;
        }

        private void EnsureInitialized()
        {
            if (this.activeDoctors == null)
            {
                this.activeDoctors = new ThingOwner<Pawn>(this, false, LookMode.Deep, false);
            }
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

        private void StopDoctorJobs(Pawn doctor)
        {
            if (doctor == null || doctor.jobs == null || doctor.CurJob == null)
            {
                return;
            }

            try
            {
                doctor.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
            }
            catch (System.Exception exception)
            {
                Log.Warning("[CeleTech Shuttle] Could not stop medical procedure doctor job after holder entry: " + exception);
            }
        }

        private bool TryRecoverDoctorToMap(Pawn doctor, Map map, IntVec3 cell)
        {
            if (doctor == null || doctor.Destroyed || doctor.Spawned)
            {
                return doctor != null && !doctor.Destroyed;
            }

            if (map == null || !cell.IsValid)
            {
                return false;
            }

            try
            {
                return GenPlace.TryPlaceThing(doctor, cell, map, ThingPlaceMode.Near);
            }
            catch (System.Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Medical procedure doctor map recovery threw: " + exception);
                return false;
            }
        }
    }
}

using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal bool TryEjectPatient(Pawn patient, out string failureReason)
        {
            return MedicalBayOccupancyEjectionService.TryEjectPatient(
                this,
                patient,
                out failureReason);
        }

        internal bool TryEjectAllPatients(out string failureReason)
        {
            return MedicalBayOccupancyEjectionService.TryEjectAllPatients(
                this,
                out failureReason);
        }

        internal bool TryEjectAllPatients(Map map, out string failureReason)
        {
            return MedicalBayOccupancyEjectionService.TryEjectAllPatients(
                this,
                map,
                out failureReason);
        }

        internal void EjectAllPatientsSafely(Map map)
        {
            MedicalBayOccupancyEjectionService.EjectAllPatientsSafely(this, map);
        }

        private bool TryEjectPatient(Pawn patient, Map map, out string failureReason)
        {
            return MedicalBayOccupancyEjectionService.TryEjectPatient(
                this,
                patient,
                map,
                out failureReason);
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

        private void PreserveDestroyedHolderContents(DestroyMode mode)
        {
            MedicalBayOccupancyEjectionService.PreserveDestroyedHolderContents(this, mode);
        }
    }
}

using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal bool CanTransferPatientsForLaunch(out string failureReason)
        {
            return MedicalBayLaunchTransferAdapter.CanTransferPatientsForLaunch(
                this,
                out failureReason);
        }

        internal bool TryExportPatientsForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> destination,
            bool requireEmptyDestination,
            out string failureReason)
        {
            return MedicalBayLaunchTransferAdapter.TryExportPatientsForLaunch(
                this,
                manifest,
                destination,
                requireEmptyDestination,
                out failureReason);
        }

        internal bool TryRollbackPatientLaunchExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> source,
            out string notice)
        {
            return MedicalBayLaunchTransferAdapter.TryRollbackPatientLaunchExport(
                this,
                manifest,
                source,
                out notice);
        }

        internal bool TryRestorePatientsFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> source,
            out string notice)
        {
            return MedicalBayLaunchTransferAdapter.TryRestorePatientsFromLaunchStaging(
                this,
                manifest,
                source,
                out notice);
        }

        internal bool TryRestorePatientsFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> primarySource,
            ThingOwner<Thing> secondarySource,
            out string notice)
        {
            return MedicalBayLaunchTransferAdapter.TryRestorePatientsFromLaunchStaging(
                this,
                manifest,
                primarySource,
                secondarySource,
                out notice);
        }

        private CompShuttleHolderLaunchTransferState GetHolderTransferState()
        {
            ThingWithComps host = this.parent as ThingWithComps;
            return host != null
                ? host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }
    }
}

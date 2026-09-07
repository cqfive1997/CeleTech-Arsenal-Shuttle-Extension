using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static Medical Bay capability contribution. Patient containment, tending, and
    /// comfort ticks are live systems and stay outside the rebuildable profile.
    /// </summary>
    public sealed class MedicalBayProfileContributor : IShuttleProfileContributor
    {
        private static readonly MedicalBayProfileContributor instance = new MedicalBayProfileContributor();

        private MedicalBayProfileContributor()
        {
        }

        public static MedicalBayProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            ShuttleMedicalBayModuleDef medicalBayDef = context.ModuleDef as ShuttleMedicalBayModuleDef;
            if (medicalBayDef == null)
            {
                return;
            }

            IShuttleMedicalBayProfileContributionSink medicalBaySink =
                context.Contributions as IShuttleMedicalBayProfileContributionSink;
            if (medicalBaySink == null)
            {
                return;
            }

            medicalBaySink.AddMedicalBayModule(
                medicalBayDef.medicalPatientSlots,
                medicalBayDef.supportsMedevacPriority,
                medicalBayDef.supportsStabilization,
                medicalBayDef.supportsPassiveComfort,
                medicalBayDef.passiveJoyGainFactor,
                medicalBayDef.passiveJoyCapPct,
                medicalBayDef.comfortThoughtStageIndex);
        }
    }
}

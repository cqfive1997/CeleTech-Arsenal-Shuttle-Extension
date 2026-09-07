using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Non-persistent snapshot derived from the current assembly state.
    /// </summary>
    public sealed class ShuttleProfile
    {
        public ShuttleProfile(
            int revision,
            ProfileDirtyReason sourceReasons,
            LayoutProfile layout,
            MassProfile mass,
            CrewProfile crew,
            HullProfile hull,
            HabitatProfile habitat,
            MedicalBayProfile medicalBay,
            PrisonCellProfile prisonCell,
            MechChargerProfile mechCharger,
            CommandProfile command,
            PowerProfile power,
            ShieldProfile shield,
            FireControlProfile fireControl,
            WeaponProfile weapon,
            CargoProfile cargo,
            CargoLogisticsProfile cargoLogistics,
            FlightProfile flight,
            UILayoutProfile UILayout,
            ShuttleExternalProfileExtensionSnapshot externalExpressions,
            IReadOnlyList<ProfileBuildIssue> issues)
        {
            this.Revision = revision;
            this.SourceReasons = sourceReasons;
            this.Layout = layout;
            this.Mass = mass;
            this.Crew = crew;
            this.Hull = hull ?? HullProfile.Empty;
            this.Habitat = habitat ?? HabitatProfile.Empty;
            this.MedicalBay = medicalBay ?? MedicalBayProfile.Empty;
            this.PrisonCell = prisonCell ?? PrisonCellProfile.Empty;
            this.MechCharger = mechCharger ?? MechChargerProfile.Empty;
            this.Command = command;
            this.Power = power;
            this.Shield = shield;
            this.FireControl = fireControl ?? FireControlProfile.Empty;
            this.Weapon = weapon;
            this.Cargo = cargo;
            this.CargoLogistics = cargoLogistics;
            this.Flight = flight;
            this.UILayout = UILayout;
            this.ExternalExpressions =
                externalExpressions ?? ShuttleExternalProfileExtensionSnapshot.Empty();
            this.Issues = issues;
        }

        // Revision is controller-owned and increases whenever a new derived snapshot is produced.
        public int Revision { get; private set; }

        // SourceReasons captures why this snapshot had to be rebuilt.
        public ProfileDirtyReason SourceReasons { get; private set; }

        public LayoutProfile Layout { get; private set; }

        public MassProfile Mass { get; private set; }

        public CrewProfile Crew { get; private set; }

        public HullProfile Hull { get; private set; }

        public HabitatProfile Habitat { get; private set; }

        public MedicalBayProfile MedicalBay { get; private set; }

        public PrisonCellProfile PrisonCell { get; private set; }

        public MechChargerProfile MechCharger { get; private set; }

        public CommandProfile Command { get; private set; }

        public PowerProfile Power { get; private set; }

        public ShieldProfile Shield { get; private set; }

        public FireControlProfile FireControl { get; private set; }

        public WeaponProfile Weapon { get; private set; }

        public CargoProfile Cargo { get; private set; }

        public CargoLogisticsProfile CargoLogistics { get; private set; }

        public FlightProfile Flight { get; private set; }

        public UILayoutProfile UILayout { get; private set; }

        public ShuttleExternalProfileExtensionSnapshot ExternalExpressions { get; private set; }

        // Issues are validation-style warnings produced during profile construction.
        // TODO: Project these into the future UI/read-model layer. In the current rewrite they are
        // only stored on the profile snapshot for diagnostics.
        public IReadOnlyList<ProfileBuildIssue> Issues { get; private set; }
    }
}

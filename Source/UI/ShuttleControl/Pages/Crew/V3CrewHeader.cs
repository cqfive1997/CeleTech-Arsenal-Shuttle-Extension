using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewHeader
    {
        private readonly V3CrewText text;
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal V3CrewHeader(V3CrewText text)
        {
            this.text = text;
        }

        internal void Draw(Rect rect, V3CrewPageModel model, ShuttlePageDrawContext context)
        {
            if (model == null || context == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                model.ControlModel ?? new ShuttleControlReadModel();
            ShuttleCargoSnapshot cargoSnapshot =
                context.ReadModels != null && context.ReadModels.CargoSnapshot != null
                    ? context.ReadModels.CargoSnapshot
                    : new ShuttleCargoSnapshot();
            ShuttleWeaponBayReadModel weaponBayModel =
                context.ReadModels != null && context.ReadModels.WeaponBayModel != null
                    ? context.ReadModels.WeaponBayModel
                    : ShuttleWeaponBayReadModel.Empty;

            ShuttleControlHeaderDrawer.Draw(
                rect,
                this.identitySpecBuilder.GetTitle(controlModel),
                this.identitySpecBuilder.GetLogo(context),
                this.statusSpecBuilder.Build(
                    controlModel,
                    cargoSnapshot,
                    weaponBayModel,
                    ShuttlePageHeaderShieldMode.AggregateAnyMaxHitPoints),
                this.commandSpecBuilder.Build(context),
                new ShuttleHeaderRibbonSpec(
                    ShuttleHeaderProgressSpec.Empty,
                    this.BuildMetricSpecs(controlModel, model.CrewData)));
        }

        private IList<ShuttleHeaderMetricSpec> BuildMetricSpecs(
            ShuttleControlReadModel model,
            V3CrewPageData pageData)
        {
            List<ShuttleHeaderMetricSpec> metrics =
                new List<ShuttleHeaderMetricSpec>();
            CabinStatusChip[] chips =
            {
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_Cockpit"),
                    model != null && model.HasCockpit
                        ? this.GetCountText(pageData != null ? pageData.CockpitPawnCount : 0, this.text.Tr("CT_Shuttle_Crew_Unit_Pawn"))
                        : this.text.Tr("CT_Shuttle_Crew_NotInstalled"),
                    model != null && model.HasCockpit ? V3CrewText.GreenColor : ShuttleUIStyle.MutedTextColor,
                    this.text.Tr("CT_Shuttle_Crew_Tooltip_CockpitLoaded")),
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_Habitat"),
                    model != null && model.Habitat != null && model.Habitat.HasHabitat
                        ? this.GetCountText(model.Habitat.TotalOccupants, this.text.Tr("CT_Shuttle_Crew_Unit_Pawn"))
                        : this.text.Tr("CT_Shuttle_Crew_NotInstalled"),
                    model != null && model.Habitat != null && model.Habitat.HasHabitat ? V3CrewText.GreenColor : ShuttleUIStyle.MutedTextColor,
                    this.text.Tr("CT_Shuttle_Crew_Tooltip_HabitatOccupants")),
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_RecreationModule"),
                    model != null && model.Habitat != null && model.Habitat.SupportsJoy
                        ? this.GetCountText(model.Habitat.JoyOccupants, this.text.Tr("CT_Shuttle_Crew_Unit_Pawn"))
                        : this.text.Tr("CT_Shuttle_Crew_NotInstalled"),
                    model != null && model.Habitat != null && model.Habitat.SupportsJoy ? V3CrewText.GreenColor : ShuttleUIStyle.MutedTextColor,
                    this.text.Tr("CT_Shuttle_Crew_Tooltip_RecreationOccupants")),
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_MedicalBay"),
                    model != null && model.MedicalBay != null && model.MedicalBay.HasMedicalBay
                        ? this.GetCountText(model.MedicalBay.PatientCount, this.text.Tr("CT_Shuttle_Crew_Unit_Pawn"))
                        : this.text.Tr("CT_Shuttle_Crew_NotInstalled"),
                    model != null && model.MedicalBay != null && model.MedicalBay.HasMedicalBay ? V3CrewText.GreenColor : ShuttleUIStyle.MutedTextColor,
                    this.text.Tr("CT_Shuttle_Crew_Tooltip_MedicalPatients")),
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_ChargingBay"),
                    this.GetMechChargerChipValue(model),
                    this.GetMechChargerChipColor(model),
                    this.GetMechChargerChipTooltip(model)),
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_Brig"),
                    this.GetPrisonCellChipValue(model),
                    this.GetPrisonCellChipColor(model),
                    this.GetPrisonCellChipTooltip(model)),
                new CabinStatusChip(
                    this.text.Tr("CT_Shuttle_Crew_Status_TotalMembers"),
                    this.GetCountText(pageData != null ? pageData.TotalCount : 0, string.Empty),
                    V3CrewText.BlueColor,
                    this.text.Tr("CT_Shuttle_Crew_Tooltip_TotalMembers"))
            };

            for (int i = 0; i < chips.Length; i++)
            {
                CabinStatusChip chip = chips[i];
                metrics.Add(new ShuttleHeaderMetricSpec(
                    chip.Label,
                    chip.Value,
                    this.BuildChipTooltip(chip),
                    chip.ValueColor));
            }

            return metrics;
        }

        private string BuildChipTooltip(CabinStatusChip chip)
        {
            if (string.IsNullOrEmpty(chip.Tooltip))
            {
                return chip.Label + ": " + chip.Value;
            }

            return chip.Tooltip + "\n" + chip.Label + ": " + chip.Value;
        }

        private string GetMechChargerChipValue(ShuttleControlReadModel model)
        {
            if (model == null || model.MechCharger == null || !model.MechCharger.HasMechCharger)
            {
                return this.text.Tr("CT_Shuttle_Crew_NotInstalled");
            }

            return this.GetCountText(
                model.MechCharger.ChargingMechCount,
                this.text.Tr("CT_Shuttle_Crew_Unit_Mech")) +
                " / " +
                Mathf.Max(0, model.MechCharger.MechChargeSlots).ToString();
        }

        private Color GetMechChargerChipColor(ShuttleControlReadModel model)
        {
            if (model == null || model.MechCharger == null || !model.MechCharger.HasMechCharger)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (model.MechCharger.MechChargerPoweredKnown && !model.MechCharger.MechChargerPowered)
            {
                return V3CrewText.YellowColor;
            }

            return model.MechCharger.ChargingMechCount > 0
                ? V3CrewText.BlueColor
                : V3CrewText.GreenColor;
        }

        private string GetMechChargerChipTooltip(ShuttleControlReadModel model)
        {
            if (model == null || model.MechCharger == null || !model.MechCharger.HasMechCharger)
            {
                return this.text.Tr("CT_Shuttle_Crew_Tooltip_MechChargerMissing");
            }

            if (model.MechCharger.MechChargerPoweredKnown && !model.MechCharger.MechChargerPowered)
            {
                return this.text.Tr("CT_Shuttle_Crew_Tooltip_MechChargerUnpowered");
            }

            return this.text.Tr("CT_Shuttle_Crew_Tooltip_MechChargerCharging");
        }

        private string GetPrisonCellChipValue(ShuttleControlReadModel model)
        {
            ShuttlePrisonCellReadModel prisonCell = model != null
                ? model.PrisonCell
                : null;
            if (prisonCell == null || !prisonCell.HasPrisonCell)
            {
                return this.text.Tr("CT_Shuttle_Crew_NotInstalled");
            }

            return Mathf.Max(0, prisonCell.PrisonerCount).ToString() +
                " / " +
                Mathf.Max(0, prisonCell.PrisonerSlots).ToString();
        }

        private Color GetPrisonCellChipColor(ShuttleControlReadModel model)
        {
            ShuttlePrisonCellReadModel prisonCell = model != null
                ? model.PrisonCell
                : null;
            if (prisonCell == null || !prisonCell.HasPrisonCell)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            int prisonerCount = Mathf.Max(0, prisonCell.PrisonerCount);
            int prisonerSlots = Mathf.Max(0, prisonCell.PrisonerSlots);
            int freePrisonerSlots = Mathf.Max(0, prisonCell.FreePrisonerSlots);
            if (prisonerSlots > 0 && freePrisonerSlots == 0)
            {
                return V3CrewText.RedColor;
            }

            return prisonerCount > 0
                ? V3CrewText.BlueColor
                : V3CrewText.GreenColor;
        }

        private string GetPrisonCellChipTooltip(ShuttleControlReadModel model)
        {
            ShuttlePrisonCellReadModel prisonCell = model != null
                ? model.PrisonCell
                : null;
            if (prisonCell == null || !prisonCell.HasPrisonCell)
            {
                return this.text.Tr("CT_Shuttle_Crew_Tooltip_BrigMissing");
            }

            int prisonerCount = Mathf.Max(0, prisonCell.PrisonerCount);
            int prisonerSlots = Mathf.Max(0, prisonCell.PrisonerSlots);
            int freePrisonerSlots = Mathf.Max(0, prisonCell.FreePrisonerSlots);
            string stateTooltip;
            if (prisonerSlots > 0 && freePrisonerSlots == 0)
            {
                stateTooltip = this.text.Tr("CT_Shuttle_Crew_Tooltip_BrigFull");
            }
            else if (prisonerCount > 0)
            {
                stateTooltip = this.text.Tr("CT_Shuttle_Crew_Tooltip_BrigOccupied");
            }
            else
            {
                stateTooltip = this.text.Tr("CT_Shuttle_Crew_Tooltip_BrigEmpty");
            }

            return stateTooltip + "\n" +
                this.text.Tr("CT_Shuttle_PrisonCell_FreeSlots") + ": " +
                freePrisonerSlots.ToString();
        }

        private string GetCountText(int count, string unit)
        {
            return count.ToString() + unit;
        }

        private struct CabinStatusChip
        {
            internal readonly string Label;
            internal readonly string Value;
            internal readonly Color ValueColor;
            internal readonly string Tooltip;

            internal CabinStatusChip(string label, string value, Color valueColor, string tooltip)
            {
                this.Label = label;
                this.Value = value;
                this.ValueColor = valueColor;
                this.Tooltip = tooltip;
            }
        }
    }
}

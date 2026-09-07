using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainHeaderSpecBuilder
    {
        private readonly V3MainText text;
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal V3MainHeaderSpecBuilder(V3MainText text)
        {
            this.text = text ?? new V3MainText();
        }

        internal string GetTitle(V3MainPageModel model)
        {
            ShuttleControlReadModel controlModel = model != null ? model.ControlModel : null;
            if (controlModel != null && !string.IsNullOrEmpty(controlModel.ShuttleLabel))
            {
                return controlModel.ShuttleLabel;
            }

            return this.text.Tr("CT_Shuttle_UI_DefaultShuttleLabel");
        }

        internal Texture2D GetLogo(ShuttlePageDrawContext context)
        {
            Texture2D logo = this.GetIcon(context, "console_logo");
            return logo != null ? logo : this.GetIcon(context, "logo");
        }

        internal IList<ShuttleHeaderStatusSpec> BuildStatusSpecs(V3MainPageModel model)
        {
            ShuttleControlReadModel controlModel =
                model != null && model.ControlModel != null
                    ? model.ControlModel
                    : new ShuttleControlReadModel();
            ShuttleCargoSnapshot cargoSnapshot =
                model != null && model.CargoSnapshot != null
                    ? model.CargoSnapshot
                    : new ShuttleCargoSnapshot();
            ShuttleWeaponBayReadModel weaponBayModel =
                model != null && model.WeaponBayModel != null
                    ? model.WeaponBayModel
                    : ShuttleWeaponBayReadModel.Empty;

            float powerWatts = ShuttleUIMetricFormatter.GetDisplayPowerWatts(controlModel);
            float powerMax =
                ShuttleUIMetricFormatter.GetDisplayPowerMaxWatts(controlModel, powerWatts);
            float powerDemandWatts = controlModel.HasPowerRuntimeSnapshot
                ? Mathf.Max(0f, controlModel.ActiveInternalDemandWatts)
                : Mathf.Max(0f, controlModel.InternalIdleDemandWatts);
            float remainingPowerWatts = Mathf.Max(0f, powerWatts - powerDemandWatts);
            int shieldCurrent;
            int shieldMax;
            this.GetShieldHitPoints(weaponBayModel, out shieldCurrent, out shieldMax);
            float cargoCurrent =
                ShuttleUIMetricFormatter.GetCargoCurrentMassKg(cargoSnapshot);
            float cargoCapacity =
                ShuttleUIMetricFormatter.GetCargoCapacity(
                    controlModel,
                    cargoSnapshot,
                    true);
            float armor01 = controlModel.Hull != null
                ? Mathf.Clamp01(controlModel.Hull.IntegrityPct)
                : 0f;

            return new List<ShuttleHeaderStatusSpec>
            {
                new ShuttleHeaderStatusSpec(
                    this.text.Tr("CT_Shuttle_Header_Status_Power"),
                    ShuttleUIMetricFormatter.FormatWatts(remainingPowerWatts),
                    ShuttleUIMetricFormatter.FormatWattsPair(remainingPowerWatts, powerMax),
                    ShuttleUIStyle.HeaderPowerMeterColor,
                    Normalize01(remainingPowerWatts, powerMax)),
                new ShuttleHeaderStatusSpec(
                    this.text.Tr("CT_Shuttle_Header_Status_ShieldValue"),
                    shieldCurrent.ToString(),
                    shieldCurrent.ToString() + "/" + shieldMax.ToString(),
                    ShuttleUIStyle.HeaderShieldMeterColor,
                    Normalize01(shieldCurrent, shieldMax)),
                new ShuttleHeaderStatusSpec(
                    this.text.Tr("CT_Shuttle_Header_Status_Load"),
                    ShuttleUIMetricFormatter.FormatKgCompact(cargoCurrent),
                    ShuttleUIMetricFormatter.FormatKgPair(cargoCurrent, cargoCapacity),
                    V3MainText.YellowColor,
                    ShuttleUIMetricFormatter.GetRemainingCapacityRatio01(
                        cargoCurrent,
                        cargoCapacity)),
                new ShuttleHeaderStatusSpec(
                    this.text.Tr("CT_Shuttle_Header_Status_StoredPower"),
                    ShuttleUIMetricFormatter.FormatEnergy(controlModel.StoredEnergyWd),
                    ShuttleUIMetricFormatter.FormatEnergyPair(
                        controlModel.StoredEnergyWd,
                        controlModel.EnergyCapacityWd),
                    ShuttleUIStyle.HeaderPowerMeterColor,
                    Normalize01(controlModel.StoredEnergyWd, controlModel.EnergyCapacityWd)),
                new ShuttleHeaderStatusSpec(
                    this.text.Tr("CT_Shuttle_Hull_ArmorMetricLabel"),
                    ShuttleUIMetricFormatter.FormatPercent(armor01),
                    ShuttleUIMetricFormatter.FormatPercent(armor01),
                    ShuttleUIStyle.HeaderArmorMeterColor,
                    armor01)
            };
        }

        internal IList<ShuttleHeaderMetricSpec> BuildMetricSpecs(V3MainPageModel model)
        {
            ShuttleControlReadModel controlModel =
                model != null && model.ControlModel != null
                    ? model.ControlModel
                    : new ShuttleControlReadModel();
            ShuttleCargoSnapshot cargoSnapshot =
                model != null && model.CargoSnapshot != null
                    ? model.CargoSnapshot
                    : new ShuttleCargoSnapshot();

            int externalTotal;
            int externalEnabled;
            int externalWithPanels;
            this.CountExternalModules(
                model,
                out externalTotal,
                out externalEnabled,
                out externalWithPanels);

            string massValue = ShuttleUIMetricFormatter.FormatKgPair(
                Mathf.Max(0f, controlModel.TotalMass),
                Mathf.Max(0f, controlModel.MassCapacity));
            string launchTooltip = this.text.Tr("CT_Shuttle_Main_Range") + ": " +
                controlModel.HardRangeCapTiles.ToString() + " " +
                this.text.Tr("CT_Shuttle_Main_Tiles");
            if (cargoSnapshot.TotalPlannedMassKg > 0f)
            {
                launchTooltip = launchTooltip + "\n" + this.text.Tr("CT_Shuttle_Main_Cargo") +
                    ": " + ShuttleUIMetricFormatter.FormatKg(cargoSnapshot.TotalPlannedMassKg);
            }

            return new List<ShuttleHeaderMetricSpec>
            {
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_Profile"),
                    this.text.Tr("CT_Shuttle_Main_ProfileRevisionShort", controlModel.ProfileRevision),
                    this.text.Tr("CT_Shuttle_Main_Profile"),
                    controlModel.IsProfileDirty ? V3MainText.YellowColor : V3MainText.GreenColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_UI_Assembly"),
                    this.text.FormatCountPair(
                        controlModel.InstalledSegmentCount,
                        controlModel.SegmentSlotCount),
                    this.text.Tr("CT_Shuttle_UI_Assembly"),
                    V3MainText.BlueColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_FunctionModules"),
                    this.text.FormatCountPair(
                        controlModel.InstalledModuleCount,
                        controlModel.ModuleSlotCount),
                    this.text.Tr("CT_Shuttle_Main_FunctionModules"),
                    V3MainText.AccentColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_Mass"),
                    massValue,
                    massValue,
                    V3MainText.YellowColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_LaunchReady"),
                    this.text.FormatLaunchReady(controlModel.LaunchReadyProgress),
                    launchTooltip,
                    controlModel.LaunchReadyProgress >= 1f
                        ? V3MainText.GreenColor
                        : V3MainText.YellowColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Page_ExternalModules"),
                    externalEnabled.ToString() + "/" + externalTotal.ToString(),
                    this.text.Tr("CT_Shuttle_ExternalRuntime_Tooltip_Count", externalTotal) +
                        "\n" +
                        this.text.Tr("CT_Shuttle_ExternalRuntime_Tooltip_Panels", externalWithPanels),
                externalEnabled > 0 ? V3MainText.GreenColor : V3MainText.AccentColor)
            };
        }

        internal ShuttleHeaderRibbonSpec BuildRibbonSpec(V3MainPageModel model)
        {
            ShuttleControlReadModel controlModel =
                model != null && model.ControlModel != null
                    ? model.ControlModel
                    : new ShuttleControlReadModel();
            string cooldownLabel = this.text.Tr("CT_Shuttle_Main_Cooldown");
            ShuttleHeaderProgressSpec progress = new ShuttleHeaderProgressSpec(
                cooldownLabel,
                this.GetLaunchProgressText(controlModel) + "\n" +
                    cooldownLabel + ": " + this.GetCooldownText(controlModel),
                Mathf.Clamp01(controlModel.LaunchReadyProgress),
                V3MainText.BlueColor);

            return new ShuttleHeaderRibbonSpec(
                progress,
                this.BuildFlightMetricSpecs(controlModel));
        }

        private IList<ShuttleHeaderMetricSpec> BuildFlightMetricSpecs(
            ShuttleControlReadModel model)
        {
            float internalDemandWatts = GetInternalDemandWatts(model);
            return new List<ShuttleHeaderMetricSpec>
            {
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_Range"),
                    this.text.Tr("CT_Shuttle_Main_Range"),
                    model.HardRangeCapTiles.ToString() + " " + this.text.Tr("CT_Shuttle_Main_Tiles"),
                    null,
                    V3MainText.BlueColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_ThrustEnergy"),
                    this.text.Tr("CT_Shuttle_Main_Thrust"),
                    ShuttleUIMetricFormatter.FormatEnergy(model.EnergyPerTileWd) + "/" +
                        this.text.Tr("CT_Shuttle_Main_Tile"),
                    null,
                    V3MainText.BlueColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_Reactor"),
                    this.text.Tr("CT_Shuttle_Main_Reactor"),
                    ShuttleUIMetricFormatter.FormatWatts(
                        ShuttleUIMetricFormatter.GetDisplayPowerWatts(model)),
                    null,
                    V3MainText.GreenColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_InternalDraw"),
                    this.text.Tr("CT_Shuttle_Main_Draw"),
                    ShuttleUIMetricFormatter.FormatWatts(internalDemandWatts),
                    null,
                    internalDemandWatts > 0f
                        ? V3MainText.YellowColor
                        : ShuttleUIStyle.MutedTextColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_InternalCrew"),
                    this.text.Tr("CT_Shuttle_Main_CrewShort"),
                    "-",
                    null,
                    ShuttleUIStyle.MutedTextColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_Main_Bus"),
                    this.text.Tr("CT_Shuttle_Main_Bus"),
                    model.InternalBusPowered
                        ? this.text.Tr("CT_Shuttle_Main_Online")
                        : this.text.Tr("CT_Shuttle_Main_Offline"),
                    null,
                    model.InternalBusPowered
                        ? V3MainText.GreenColor
                        : V3MainText.RedColor)
            };
        }

        internal IList<ShuttleHeaderCommandSpec> BuildCommandSpecs(
            ShuttlePageDrawContext context)
        {
            return this.commandSpecBuilder.Build(context);
        }

        private Texture2D GetIcon(ShuttlePageDrawContext context, string key)
        {
            return context != null &&
                context.Services != null &&
                context.Services.Icons != null
                    ? context.Services.Icons.GetIcon(key)
                    : null;
        }

        private static float Normalize01(float value, float max)
        {
            if (max <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(value / max);
        }

        private string GetLaunchProgressText(ShuttleControlReadModel model)
        {
            float progress = model != null ? Mathf.Clamp01(model.LaunchReadyProgress) : 1f;
            return this.text.Tr("CT_Shuttle_InfoPanel_Summary_Ready") + " " +
                ShuttleUIMetricFormatter.FormatPercent(progress);
        }

        private string GetCooldownText(ShuttleControlReadModel model)
        {
            if (model == null ||
                !model.HasLaunchCooldown ||
                model.LaunchCooldownRemainingTicks <= 0)
            {
                return "0";
            }

            float hours = model.LaunchCooldownRemainingTicks / 2500f;
            return hours.ToString("0.#") + " h";
        }

        private static float GetInternalDemandWatts(ShuttleControlReadModel model)
        {
            if (model == null)
            {
                return 0f;
            }

            if (model.ActiveInternalDemandWatts > 0f)
            {
                return model.ActiveInternalDemandWatts;
            }

            return Mathf.Max(0f, model.InternalIdleDemandWatts);
        }

        private void GetShieldHitPoints(
            ShuttleWeaponBayReadModel weaponBayModel,
            out int current,
            out int max)
        {
            current = 0;
            max = 0;
            if (weaponBayModel == null || weaponBayModel.Shields == null)
            {
                return;
            }

            for (int i = 0; i < weaponBayModel.Shields.Count; i++)
            {
                ShuttleShieldStatusReadModel shield = weaponBayModel.Shields[i];
                if (shield == null)
                {
                    continue;
                }

                current += Mathf.Max(0, shield.CurrentHitPoints);
                max += Mathf.Max(0, shield.MaxHitPoints);
            }
        }

        private void CountExternalModules(
            V3MainPageModel model,
            out int total,
            out int enabled,
            out int withPanels)
        {
            total = 0;
            enabled = 0;
            withPanels = 0;
            if (model == null || model.ExternalModuleModels == null)
            {
                return;
            }

            for (int i = 0; i < model.ExternalModuleModels.Count; i++)
            {
                ExternalModuleUIReadModel externalModel = model.ExternalModuleModels[i];
                if (externalModel == null)
                {
                    continue;
                }

                total++;
                if (externalModel.RuntimeEnabled)
                {
                    enabled++;
                }

                if (externalModel.HasExternalPanel)
                {
                    withPanels++;
                }
            }
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome
{
    internal enum ShuttlePageHeaderShieldMode
    {
        AggregateAnyMaxHitPoints,
        PreferredSurface
    }

    internal sealed class ShuttlePageHeaderStatusSpecBuilder
    {
        internal IList<ShuttleHeaderStatusSpec> Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleWeaponBayReadModel weaponBayModel,
            ShuttlePageHeaderShieldMode shieldMode)
        {
            controlModel = controlModel ?? new ShuttleControlReadModel();
            cargoSnapshot = cargoSnapshot ?? new ShuttleCargoSnapshot();
            weaponBayModel = weaponBayModel ?? ShuttleWeaponBayReadModel.Empty;

            float powerWatts =
                ShuttleUIMetricFormatter.GetDisplayPowerWatts(controlModel);
            float powerMax =
                ShuttleUIMetricFormatter.GetDisplayPowerMaxWatts(
                    controlModel,
                    powerWatts);
            float powerDemandWatts = controlModel.HasPowerRuntimeSnapshot
                ? Mathf.Max(0f, controlModel.ActiveInternalDemandWatts)
                : Mathf.Max(0f, controlModel.InternalIdleDemandWatts);
            float remainingPowerWatts = Mathf.Max(0f, powerWatts - powerDemandWatts);
            int shieldCurrent;
            int shieldMax;
            this.GetShieldHitPoints(
                weaponBayModel,
                shieldMode,
                out shieldCurrent,
                out shieldMax);
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
                    ShuttleUIText.Tr("CT_Shuttle_Header_Status_Power"),
                    ShuttleUIMetricFormatter.FormatWatts(remainingPowerWatts),
                    ShuttleUIMetricFormatter.FormatWattsPair(remainingPowerWatts, powerMax),
                    ShuttleUIStyle.HeaderPowerMeterColor,
                    Normalize01(remainingPowerWatts, powerMax)),
                new ShuttleHeaderStatusSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Header_Status_ShieldValue"),
                    shieldCurrent.ToString(),
                    shieldCurrent.ToString() + "/" + shieldMax.ToString(),
                    ShuttleUIStyle.HeaderShieldMeterColor,
                    Normalize01(shieldCurrent, shieldMax)),
                new ShuttleHeaderStatusSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Header_Status_Load"),
                    ShuttleUIMetricFormatter.FormatKgCompact(cargoCurrent),
                    ShuttleUIMetricFormatter.FormatKgPair(cargoCurrent, cargoCapacity),
                    ShuttleUIStyle.YellowStatusColor,
                    ShuttleUIMetricFormatter.GetRemainingCapacityRatio01(
                        cargoCurrent,
                        cargoCapacity)),
                new ShuttleHeaderStatusSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Header_Status_StoredPower"),
                    ShuttleUIMetricFormatter.FormatEnergy(controlModel.StoredEnergyWd),
                    ShuttleUIMetricFormatter.FormatEnergyPair(
                        controlModel.StoredEnergyWd,
                        controlModel.EnergyCapacityWd),
                    ShuttleUIStyle.HeaderPowerMeterColor,
                    Normalize01(controlModel.StoredEnergyWd, controlModel.EnergyCapacityWd)),
                new ShuttleHeaderStatusSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Hull_ArmorMetricLabel"),
                    ShuttleUIMetricFormatter.FormatPercent(armor01),
                    ShuttleUIMetricFormatter.FormatPercent(armor01),
                    ShuttleUIStyle.HeaderArmorMeterColor,
                    armor01)
            };
        }

        private void GetShieldHitPoints(
            ShuttleWeaponBayReadModel weaponBayModel,
            ShuttlePageHeaderShieldMode shieldMode,
            out int current,
            out int max)
        {
            current = 0;
            max = 0;
            if (weaponBayModel == null || weaponBayModel.Shields == null)
            {
                return;
            }

            if (shieldMode == ShuttlePageHeaderShieldMode.PreferredSurface)
            {
                ShuttleShieldStatusReadModel selected =
                    this.SelectPreferredShield(weaponBayModel);
                if (selected == null)
                {
                    return;
                }

                current = Mathf.Max(0, selected.CurrentHitPoints);
                max = Mathf.Max(0, selected.MaxHitPoints);
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

        private ShuttleShieldStatusReadModel SelectPreferredShield(
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            ShuttleShieldStatusReadModel selected = null;
            if (weaponBayModel == null || weaponBayModel.Shields == null)
            {
                return null;
            }

            for (int i = 0; i < weaponBayModel.Shields.Count; i++)
            {
                ShuttleShieldStatusReadModel candidate = weaponBayModel.Shields[i];
                if (candidate == null)
                {
                    continue;
                }

                if (selected == null ||
                    this.GetShieldPriority(candidate) > this.GetShieldPriority(selected))
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        private int GetShieldPriority(ShuttleShieldStatusReadModel shield)
        {
            if (shield == null)
            {
                return -1;
            }

            int score = 0;
            if (shield.HasModule)
            {
                score += 100;
            }

            if (shield.HasShield)
            {
                score += 100;
            }

            if (shield.IsSurfaceShield)
            {
                score += 50;
            }

            if (shield.IsEnabled)
            {
                score += 10;
            }

            if (shield.Online)
            {
                score += 10;
            }

            if (shield.MaxHitPoints > 0)
            {
                score += 1;
            }

            return score;
        }

        private static float Normalize01(float value, float max)
        {
            if (max <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(value / max);
        }
    }
}

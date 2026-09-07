using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime
{
    /// <summary>
    /// Internal shuttle power bus simulation. It does not query RimWorld PowerNet, wires,
    /// CompPowerTrader, CompPowerBattery, or any host thing comps.
    /// </summary>
    public sealed class PowerSystem : IShuttleStoredEnergySink, IShuttlePowerDemandSink
    {
        private const float WattDaysPerTick = 1f / 60000f;

        public void ApplyProfile(ShuttleProfile profile, ShuttleRuntimeState runtimeState, float initialStoredEnergyPercent)
        {
            if (runtimeState == null)
            {
                return;
            }

            runtimeState.EnsureInitialized();

            float capacityWd = this.GetEnergyStorageCapacityWd(profile);
            PowerRuntimeState power = runtimeState.Power;
            if (capacityWd <= 0f)
            {
                power.StoredEnergyWd = 0f;
                power.LastAppliedEnergyCapacityWd = 0f;
                power.HasInitializedCharge = true;
                return;
            }

            // Initial charge applies only to shuttles whose first synchronized profile already
            // has battery capacity. Installing the first battery later must not create free charge.
            if (!power.HasInitializedCharge)
            {
                power.StoredEnergyWd = capacityWd * this.Clamp01(initialStoredEnergyPercent);
                power.HasInitializedCharge = true;
            }

            power.StoredEnergyWd = this.Clamp(power.StoredEnergyWd, 0f, capacityWd);
            power.LastAppliedEnergyCapacityWd = capacityWd;
        }

        public void Tick(ShuttleProfile profile, ShuttleRuntimeState runtimeState, ShuttlePowerHostStatus hostStatus)
        {
            if (runtimeState == null)
            {
                return;
            }

            runtimeState.EnsureInitialized();

            PowerRuntimeState power = runtimeState.Power;
            float capacityWd = this.GetEnergyStorageCapacityWd(profile);
            float reactorWatts = hostStatus != null && hostStatus.ReactorOperational
                ? this.GetReactorGenerationWatts(profile)
                : 0f;
            float idleDemandWatts = this.GetInternalIdleDemandWatts(profile);
            float transientDemandWatts = power.ConsumeTransientInternalDemandWatts();
            float demandWatts = idleDemandWatts + transientDemandWatts;
            float maxChargeWatts = this.GetMaxBatteryChargeWatts(profile);
            float maxDischargeWatts = this.GetMaxBatteryDischargeWatts(profile);
            float gridExportCapacityWatts = this.GetGridExportCapacityWatts(profile);
            float reserveEnergyWd = this.GetReserveEnergyWd(profile);

            power.StoredEnergyWd = this.Clamp(power.StoredEnergyWd, 0f, capacityWd);
            power.LastReactorGenerationWatts = reactorWatts;
            power.LastInternalDemandWatts = demandWatts;
            power.LastBatteryChargeWatts = 0f;
            power.LastBatteryDischargeWatts = 0f;
            power.LastGridExportWatts = 0f;
            power.LastUnmetDemandWatts = 0f;

            float surplusWatts = reactorWatts - demandWatts;
            if (surplusWatts >= 0f)
            {
                power.InternalBusPowered = true;
                float chargeWatts = this.GetChargeWatts(surplusWatts, maxChargeWatts, capacityWd, power.StoredEnergyWd);
                if (chargeWatts > 0f)
                {
                    power.StoredEnergyWd += this.WattsToWattDaysPerTick(chargeWatts);
                    power.LastBatteryChargeWatts = chargeWatts;
                }

                float remainingWatts = surplusWatts - chargeWatts;
                if (remainingWatts > 0f && gridExportCapacityWatts > 0f)
                {
                    power.LastGridExportWatts = this.Min(remainingWatts, gridExportCapacityWatts);
                }

                power.StoredEnergyWd = this.Clamp(power.StoredEnergyWd, 0f, capacityWd);
                return;
            }

            float deficitWatts = -surplusWatts;
            // Internal idle demand may borrow from the flight battery, but it must not drain
            // the launch reserve that the flight calculator treats as unavailable.
            float dischargeWatts = this.GetDischargeWatts(deficitWatts, maxDischargeWatts, power.StoredEnergyWd, reserveEnergyWd);
            if (dischargeWatts > 0f)
            {
                power.StoredEnergyWd -= this.WattsToWattDaysPerTick(dischargeWatts);
                power.LastBatteryDischargeWatts = dischargeWatts;
            }

            float unmetWatts = deficitWatts - dischargeWatts;
            if (unmetWatts > 0.0001f)
            {
                power.InternalBusPowered = false;
                power.LastUnmetDemandWatts = unmetWatts;
            }
            else
            {
                power.InternalBusPowered = true;
                power.LastUnmetDemandWatts = 0f;
            }

            power.StoredEnergyWd = this.Clamp(power.StoredEnergyWd, 0f, capacityWd);
        }

        public bool TryConsumeStoredEnergyWd(ShuttleRuntimeState runtimeState, float amountWd)
        {
            if (runtimeState == null)
            {
                return false;
            }

            if (!this.IsFiniteFloat(amountWd))
            {
                return false;
            }

            if (amountWd <= 0f)
            {
                return true;
            }

            runtimeState.EnsureInitialized();
            PowerRuntimeState power = runtimeState.Power;
            if (power.StoredEnergyWd + 0.0001f < amountWd)
            {
                return false;
            }

            power.StoredEnergyWd -= amountWd;
            if (power.StoredEnergyWd < 0f)
            {
                power.StoredEnergyWd = 0f;
            }

            return true;
        }

        public void AddInternalDemandWatts(ShuttleRuntimeState runtimeState, float watts)
        {
            if (runtimeState == null || !this.IsFiniteFloat(watts) || watts <= 0f)
            {
                return;
            }

            runtimeState.EnsureInitialized();
            PowerRuntimeState power = runtimeState.Power;
            power.TransientInternalDemandWatts = power.TransientInternalDemandWatts + watts;
        }

        private float GetChargeWatts(float surplusWatts, float maxChargeWatts, float capacityWd, float storedEnergyWd)
        {
            if (surplusWatts <= 0f || maxChargeWatts <= 0f || capacityWd <= 0f)
            {
                return 0f;
            }

            float headroomWd = capacityWd - storedEnergyWd;
            if (headroomWd <= 0f)
            {
                return 0f;
            }

            return this.Min(surplusWatts, this.Min(maxChargeWatts, this.WattDaysToWattsPerTick(headroomWd)));
        }

        private float GetDischargeWatts(float deficitWatts, float maxDischargeWatts, float storedEnergyWd, float reserveEnergyWd)
        {
            float dischargeableEnergyWd = storedEnergyWd - this.Max(0f, reserveEnergyWd);
            if (deficitWatts <= 0f || maxDischargeWatts <= 0f || dischargeableEnergyWd <= 0f)
            {
                return 0f;
            }

            return this.Min(deficitWatts, this.Min(maxDischargeWatts, this.WattDaysToWattsPerTick(dischargeableEnergyWd)));
        }

        private float GetEnergyStorageCapacityWd(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.EnergyStorageCapacityWd) : 0f;
        }

        private float GetReactorGenerationWatts(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.ReactorGenerationWatts) : 0f;
        }

        private float GetGridExportCapacityWatts(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.GridExportCapacityWatts) : 0f;
        }

        private float GetInternalIdleDemandWatts(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.InternalIdleDemandWatts) : 0f;
        }

        private float GetMaxBatteryChargeWatts(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.MaxBatteryChargeWatts) : 0f;
        }

        private float GetMaxBatteryDischargeWatts(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.MaxBatteryDischargeWatts) : 0f;
        }

        private float GetReserveEnergyWd(ShuttleProfile profile)
        {
            return profile != null && profile.Flight != null ? this.Max(0f, profile.Flight.ReserveEnergyWd) : 0f;
        }

        private float WattsToWattDaysPerTick(float watts)
        {
            return watts * WattDaysPerTick;
        }

        private float WattDaysToWattsPerTick(float wattDays)
        {
            return wattDays / WattDaysPerTick;
        }

        private float Clamp01(float value)
        {
            return this.Clamp(value, 0f, 1f);
        }

        private float Clamp(float value, float min, float max)
        {
            min = this.SanitizeFinite(min, 0f);
            max = this.SanitizeFinite(max, min);
            if (max < min)
            {
                max = min;
            }

            value = this.SanitizeFinite(value, min);
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float Min(float a, float b)
        {
            a = this.SanitizeFinite(a, 0f);
            b = this.SanitizeFinite(b, 0f);
            return a < b ? a : b;
        }

        private float Max(float a, float b)
        {
            a = this.SanitizeFinite(a, 0f);
            b = this.SanitizeFinite(b, 0f);
            return a > b ? a : b;
        }

        private float SanitizeFinite(float value, float fallback)
        {
            return this.IsFiniteFloat(value) ? value : fallback;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

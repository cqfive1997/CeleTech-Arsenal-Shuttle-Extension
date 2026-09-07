using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal bool TryFillStoredEnergyForDev(out string message)
        {
            message = null;
            if (!Prefs.DevMode)
            {
                message = "[CeleTech Shuttle] Fill shuttle batteries is DevMode-only.";
                return false;
            }

            ShuttleProfile currentProfile = this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            float capacityWd = currentProfile != null && currentProfile.Power != null
                ? currentProfile.Power.EnergyStorageCapacityWd
                : 0f;
            if (capacityWd <= 0f)
            {
                message = "[CeleTech Shuttle] Shuttle has no internal battery capacity to fill.";
                return false;
            }

            this.runtimeState.Power.StoredEnergyWd = capacityWd;
            this.runtimeState.Power.HasInitializedCharge = true;
            this.runtimeState.Power.LastAppliedEnergyCapacityWd = capacityWd;
            message = "[CeleTech Shuttle] Shuttle batteries filled to " + capacityWd.ToString("0.#") + " Wd.";
            return true;
        }

        internal bool TryFillActiveSurfaceShieldForDev(out string message)
        {
            message = null;
            if (!Prefs.DevMode)
            {
                message = "[CeleTech Shuttle] Fill surface shield is DevMode-only.";
                return false;
            }

            this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            int maxHitPoints;
            if (!SharedSurfaceShieldRuntimeService.TryFillActiveSurfaceShieldForDev(
                this.shuttleHost,
                this.assemblyState,
                this.runtimeState,
                ticksGame,
                out maxHitPoints))
            {
                message = "[CeleTech Shuttle] No active Surface Shield runtime state was available to fill.";
                return false;
            }

            message = "[CeleTech Shuttle] Active Surface Shield filled to " + maxHitPoints + " HP.";
            return true;
        }

        internal bool TryFillHullForDev(out string message)
        {
            message = null;
            if (!Prefs.DevMode)
            {
                message = "[CeleTech Shuttle] Fill shuttle hull is DevMode-only.";
                return false;
            }

            ShuttleProfile currentProfile = this.ReconcileProfileToHost();
            this.EnsureRuntimeState();

            int maxHitPoints = currentProfile != null && currentProfile.Hull != null
                ? currentProfile.Hull.MaxHitPoints
                : 0;
            if (maxHitPoints <= 0)
            {
                message = "[CeleTech Shuttle] Shuttle has no hull profile to fill.";
                return false;
            }

            this.runtimeState.Hull.FillToMax(maxHitPoints);
            message = "[CeleTech Shuttle] Shuttle hull filled to " + maxHitPoints + " HP.";
            return true;
        }

        internal bool TryRepairHullForDev(float requestedHitPoints, out string message)
        {
            message = null;
            if (!Prefs.DevMode)
            {
                message = "[CeleTech Shuttle] Repair shuttle hull is DevMode-only.";
                return false;
            }

            ShuttleProfile currentProfile = this.ReconcileProfileToHost();
            this.EnsureRuntimeState();

            if (!SharedHullRepairService.NeedsRepair(currentProfile, this.runtimeState))
            {
                message = "[CeleTech Shuttle] Shuttle hull is already fully repaired.";
                return false;
            }

            float actualRepair = SharedHullRepairService.ApplyRepair(
                currentProfile,
                this.runtimeState,
                requestedHitPoints);
            if (actualRepair <= 0f)
            {
                message = "[CeleTech Shuttle] No hull repair was applied.";
                return false;
            }

            int maxHitPoints = currentProfile != null && currentProfile.Hull != null
                ? currentProfile.Hull.MaxHitPoints
                : 0;
            message = "[CeleTech Shuttle] Repaired shuttle hull by " +
                actualRepair.ToString("0.#") + " HP. Current: " +
                this.runtimeState.Hull.CurrentHitPoints.ToString("0.#") + "/" +
                maxHitPoints.ToString() + ".";
            return true;
        }

    }
}

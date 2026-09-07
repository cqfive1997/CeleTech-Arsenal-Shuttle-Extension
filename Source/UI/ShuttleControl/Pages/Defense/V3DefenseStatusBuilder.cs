using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Combines detached hull, shield and power facts into the page-level status text/severity.
    /// </summary>
    internal sealed class V3DefenseStatusBuilder
    {
        internal void Apply(
            V3DefensePageReadModel model,
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            if (model == null)
            {
                return;
            }

            bool hasDefenseHardware =
                (weaponBayModel != null && weaponBayModel.HasWeaponBay) ||
                model.Weapons.Count > 0 ||
                (model.Shield != null && model.Shield.HasShield);
            bool powerLow = hasDefenseHardware &&
                controlModel != null &&
                !controlModel.InternalBusPowered;
            this.SetStatus(
                model,
                powerLow,
                IsShieldCritical(model.Shield),
                IsHullCritical(model.Hull));
        }

        private void SetStatus(
            V3DefensePageReadModel model,
            bool powerLow,
            bool shieldBroken,
            bool hullCritical)
        {
            string status = string.Empty;
            string key = ShuttleUIText.StatusNormal;
            if (hullCritical)
            {
                status = Append(status, model.Hull.StatusLabel);
                key = ShuttleUIText.SeverityCritical;
            }

            if (shieldBroken)
            {
                status = Append(
                    status,
                    ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldDown"));
                key = ShuttleUIText.SeverityCritical;
            }

            if (powerLow)
            {
                status = Append(
                    status,
                    ShuttleUIText.Tr("CT_Shuttle_Defense_PowerLow"));
                key = key == ShuttleUIText.SeverityCritical
                    ? ShuttleUIText.SeverityCritical
                    : ShuttleUIText.SeverityWarning;
            }

            model.WeaponBayStatusText = string.IsNullOrEmpty(status)
                ? ShuttleUIText.Tr("CT_Shuttle_Common_Normal")
                : status;
            model.WeaponBayStatusKey = key;
        }

        private static bool IsHullCritical(ShuttleHullReadModel hull)
        {
            return hull != null &&
                hull.HasHull &&
                (hull.StatusKey == ShuttleUIText.StatusCritical ||
                 hull.StatusKey == ShuttleUIText.StatusBreached);
        }

        private static bool IsShieldCritical(V3DefenseShieldPanelModel shield)
        {
            return shield != null &&
                shield.HasShield &&
                (shield.StatusKey == ShuttleUIText.StatusBroken ||
                 shield.StatusKey == ShuttleUIText.StatusOffline ||
                 shield.StatusKey == ShuttleUIText.StatusMissing ||
                 shield.StatusKey == ShuttleUIText.StatusDown ||
                 shield.StatusKey == ShuttleUIText.StatusUnpowered);
        }

        private static string Append(string current, string addition)
        {
            return string.IsNullOrEmpty(current)
                ? addition
                : current + " / " + addition;
        }
    }
}

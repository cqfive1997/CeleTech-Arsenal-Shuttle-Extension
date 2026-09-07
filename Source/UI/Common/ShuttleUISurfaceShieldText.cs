using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Damage;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUISurfaceShieldText
    {
        private const string SurfaceShieldStatusPrefix = "CT_Shuttle_SurfaceShield_Status_";

        internal const string TicksSuffix = " ticks";

        internal static string GetSurfaceShieldStatusLabel(string statusKey)
        {
            string safeKey = string.IsNullOrEmpty(statusKey) ? ShuttleUIText.StatusMissing : statusKey;
            return (SurfaceShieldStatusPrefix + safeKey).Translate().ToString();
        }

        internal static bool IsSurfaceShieldStatusKey(string statusKey)
        {
            return statusKey == ShuttleUIText.StatusMissing ||
                statusKey == ShuttleUIText.StatusDisabled ||
                statusKey == ShuttleUIText.StatusOffline ||
                statusKey == ShuttleUIText.StatusBroken ||
                statusKey == ShuttleUIText.StatusRechargeBlocked ||
                statusKey == ShuttleUIText.StatusNoEnergy ||
                statusKey == ShuttleUIText.StatusFull ||
                statusKey == ShuttleUIText.StatusRecharging ||
                statusKey == ShuttleUIText.StatusOnline;
        }

        internal static string ToSurfaceDamageCategoryKey(ShuttleDamageCategory category)
        {
            if (category == ShuttleDamageCategory.EMP)
            {
                return "EMP";
            }

            if (category == ShuttleDamageCategory.Heat)
            {
                return "Heat";
            }

            if (category == ShuttleDamageCategory.Explosion)
            {
                return "Explosion";
            }

            if (category == ShuttleDamageCategory.Projectile)
            {
                return "Projectile";
            }

            if (category == ShuttleDamageCategory.Direct)
            {
                return "Direct";
            }

            return "Unknown";
        }
    }
}

using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Translates structured engagement failures at presentation/command boundaries.
    /// Evaluation code remains translation-free.
    /// </summary>
    internal static class ShuttleWeaponEngagementFailureText
    {
        internal static string TranslateReasonCode(string reasonCode)
        {
            ShuttleWeaponEngagementFailure failure;
            if (string.IsNullOrEmpty(reasonCode) ||
                !Enum.TryParse(reasonCode, out failure) ||
                failure == ShuttleWeaponEngagementFailure.None)
            {
                return null;
            }

            return Translate(failure);
        }

        internal static string Translate(ShuttleWeaponEngagementFailure failure)
        {
            switch (failure)
            {
                case ShuttleWeaponEngagementFailure.BackendUnavailable:
                case ShuttleWeaponEngagementFailure.WeaponUnavailable:
                    return "CT_Shuttle_WeaponTargetFailure_WeaponUnavailable".Translate().ToString();
                case ShuttleWeaponEngagementFailure.ForcedTargetUnsupported:
                    return "CT_Shuttle_Command_WeaponForcedTargetUnsupported".Translate().ToString();
                case ShuttleWeaponEngagementFailure.LocationTargetUnsupported:
                    return "CT_Shuttle_WeaponTargetFailure_LocationUnsupported".Translate().ToString();
                case ShuttleWeaponEngagementFailure.TargetNotHostile:
                    return "CT_Shuttle_WeaponTargetFailure_NotHostile".Translate().ToString();
                case ShuttleWeaponEngagementFailure.TargetOutOfRange:
                case ShuttleWeaponEngagementFailure.TargetInsideMinimumRange:
                case ShuttleWeaponEngagementFailure.TargetOutsidePointDefenseRadius:
                case ShuttleWeaponEngagementFailure.TargetOutsideFallbackRange:
                    return "CT_Shuttle_WeaponTargetFailure_OutOfRange".Translate().ToString();
                case ShuttleWeaponEngagementFailure.TargetBlockedByLineOfSight:
                case ShuttleWeaponEngagementFailure.TargetUnderThickRoof:
                case ShuttleWeaponEngagementFailure.VerbCannotHitTarget:
                    return "CT_Shuttle_WeaponTargetFailure_Obstructed".Translate().ToString();
                case ShuttleWeaponEngagementFailure.TargetFogged:
                    return "CT_Shuttle_WeaponTargetFailure_Fogged".Translate().ToString();
                default:
                    return "CT_Shuttle_WeaponTargetFailure_Unavailable".Translate().ToString();
            }
        }
    }
}

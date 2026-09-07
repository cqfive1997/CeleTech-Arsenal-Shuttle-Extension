namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponEngagementFailure
    {
        None,
        BackendUnavailable,
        WeaponUnavailable,
        ForcedTargetUnsupported,
        AutomaticFireUnavailable,
        AutomaticTargetMustBeThing,
        TargetInvalid,
        TargetOutOfBounds,
        TargetFogged,
        TargetUnavailable,
        TargetNotHostile,
        LocationTargetUnsupported,
        TargetOutOfRange,
        TargetInsideMinimumRange,
        TargetOutsidePointDefenseRadius,
        ProjectileInterceptionUnsupported,
        ProjectileThreatInvalid,
        ProjectileThreatTooFast,
        ProjectileThreatNotIncoming,
        ProjectileThreatTooLate,
        TargetOutsideFallbackRange,
        TargetBlockedByLineOfSight,
        TargetUnderThickRoof,
        VerbCannotHitTarget
    }

    /// <summary>
    /// Detached result shared by command validation and runtime targeting. It carries stable
    /// diagnostic codes only and never translates UI text or mutates weapon state.
    /// </summary>
    internal sealed class ShuttleWeaponEngagementResult
    {
        private static readonly ShuttleWeaponEngagementResult AllowedResult =
            new ShuttleWeaponEngagementResult(ShuttleWeaponEngagementFailure.None);

        private ShuttleWeaponEngagementResult(ShuttleWeaponEngagementFailure failure)
        {
            this.Failure = failure;
        }

        internal ShuttleWeaponEngagementFailure Failure { get; private set; }

        internal bool IsAllowed
        {
            get { return this.Failure == ShuttleWeaponEngagementFailure.None; }
        }

        internal string ReasonCode
        {
            get { return this.Failure.ToString(); }
        }

        internal static ShuttleWeaponEngagementResult Allowed()
        {
            return AllowedResult;
        }

        internal static ShuttleWeaponEngagementResult Rejected(
            ShuttleWeaponEngagementFailure failure)
        {
            return failure == ShuttleWeaponEngagementFailure.None
                ? AllowedResult
                : new ShuttleWeaponEngagementResult(failure);
        }
    }
}

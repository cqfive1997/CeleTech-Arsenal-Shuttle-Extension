using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal sealed class ShuttleDefenseTargeterService :
        IShuttleDefenseTargeterService
    {
        private readonly Action closeForTargeting;

        internal ShuttleDefenseTargeterService(Action closeForTargeting)
        {
            this.closeForTargeting = closeForTargeting;
        }

        public bool BeginForcedTargeting(
            ShuttleDefenseWeaponActionTarget weapon,
            Action<LocalTargetInfo> onTargetSelected)
        {
            if (weapon == null || onTargetSelected == null)
            {
                return false;
            }

            TargetingParameters targetParams = TargetingParameters.ForAttackAny();
            targetParams.canTargetLocations = weapon.CanTargetLocations;
            targetParams.canTargetPawns = true;
            targetParams.canTargetBuildings = true;
            targetParams.canTargetItems = false;
            targetParams.validator = delegate(TargetInfo target)
            {
                return CanSelectWeaponForcedTarget(weapon, target);
            };

            if (this.closeForTargeting != null)
            {
                this.closeForTargeting();
            }

            Find.Targeter.BeginTargeting(
                targetParams,
                delegate(LocalTargetInfo target)
                {
                    onTargetSelected(target);
                },
                null,
                null,
                null,
                true);
            return true;
        }

        private static bool CanSelectWeaponForcedTarget(
            ShuttleDefenseWeaponActionTarget weapon,
            TargetInfo target)
        {
            if (weapon == null || !target.IsValid)
            {
                return false;
            }

            if (target.HasThing)
            {
                return target.Thing != null && !target.Thing.Destroyed;
            }

            return weapon.CanTargetLocations && target.Cell.IsValid;
        }
    }
}

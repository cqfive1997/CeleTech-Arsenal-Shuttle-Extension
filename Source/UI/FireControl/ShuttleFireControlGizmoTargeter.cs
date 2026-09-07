using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    internal sealed class ShuttleFireControlGizmoTargeter
    {
        private readonly ShuttleFireControlGizmoModelBuilder modelBuilder;
        private readonly ShuttleFireControlGizmoActionTargetBuilder targetBuilder;
        private readonly IShuttleDefenseWeaponGroupUIActions groupActions;

        internal ShuttleFireControlGizmoTargeter(
            ShuttleFireControlGizmoModelBuilder modelBuilder,
            ShuttleFireControlGizmoActionTargetBuilder targetBuilder,
            IShuttleDefenseWeaponGroupUIActions groupActions)
        {
            this.modelBuilder = modelBuilder;
            this.targetBuilder = targetBuilder;
            this.groupActions = groupActions;
        }

        internal bool Begin()
        {
            ShuttleFireControlGizmoModel model = this.modelBuilder.RefreshForInteraction();
            if (model == null || !model.CanSetForcedTarget || this.groupActions == null)
            {
                return ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Defense_GroupNoApplicable"));
            }

            TargetingParameters parameters = TargetingParameters.ForAttackAny();
            parameters.canTargetLocations = model.CanTargetLocations;
            parameters.canTargetPawns = true;
            parameters.canTargetBuildings = true;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return CanSelectTarget(target, model.CanTargetLocations);
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    this.modelBuilder.RefreshForInteraction();
                    bool changed = this.groupActions.SetAllForcedTarget(
                        this.targetBuilder.Build(this.modelBuilder.WeaponBay),
                        target);
                    if (changed)
                    {
                        this.modelBuilder.Invalidate();
                    }
                },
                null,
                null,
                null,
                true);
            return true;
        }

        private static bool CanSelectTarget(TargetInfo target, bool canTargetLocations)
        {
            if (!target.IsValid)
            {
                return false;
            }

            if (target.HasThing)
            {
                return target.Thing != null && !target.Thing.Destroyed;
            }

            return canTargetLocations && target.Cell.IsValid;
        }
    }
}

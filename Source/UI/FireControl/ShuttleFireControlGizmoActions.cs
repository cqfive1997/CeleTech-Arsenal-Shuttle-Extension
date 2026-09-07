using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    /// <summary>
    /// Converts Gizmo clicks into existing group actions. No draw or runtime-state ownership.
    /// </summary>
    internal sealed class ShuttleFireControlGizmoActions
    {
        private readonly ShuttleFireControlGizmoModelBuilder modelBuilder;
        private readonly ShuttleFireControlGizmoActionTargetBuilder targetBuilder;
        private readonly ShuttleFireControlGizmoTargeter targeter;
        private readonly IShuttleDefenseWeaponGroupUIActions groupActions;
        private readonly Action openDefensePage;

        internal ShuttleFireControlGizmoActions(
            ShuttleFireControlGizmoModelBuilder modelBuilder,
            ShuttleFireControlGizmoActionTargetBuilder targetBuilder,
            ShuttleFireControlGizmoTargeter targeter,
            IShuttleDefenseWeaponGroupUIActions groupActions,
            Action openDefensePage)
        {
            this.modelBuilder = modelBuilder;
            this.targetBuilder = targetBuilder;
            this.targeter = targeter;
            this.groupActions = groupActions;
            this.openDefensePage = openDefensePage;
        }

        internal bool CanOpenDefense
        {
            get { return this.openDefensePage != null; }
        }

        internal void BeginTargeting()
        {
            if (this.targeter.Begin())
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
        }

        internal void SetHoldFire(bool holdFire)
        {
            this.RefreshTargets();
            this.CompleteIfChanged(this.groupActions.SetAllHoldFire(
                this.targetBuilder.Build(this.modelBuilder.WeaponBay),
                holdFire));
        }

        internal void ClearTargets()
        {
            this.RefreshTargets();
            this.CompleteIfChanged(this.groupActions.ClearAllForcedTargets(
                this.targetBuilder.Build(this.modelBuilder.WeaponBay)));
        }

        internal void SetFireControlLinked(bool linked)
        {
            this.RefreshTargets();
            this.CompleteIfChanged(this.groupActions.SetAllFireControlLinked(
                this.targetBuilder.Build(this.modelBuilder.WeaponBay),
                linked));
        }

        internal void ReloadAll()
        {
            this.RefreshTargets();
            this.CompleteIfChanged(this.groupActions.ReloadAll(
                this.targetBuilder.Build(this.modelBuilder.WeaponBay)));
        }

        internal void CancelReloadAll()
        {
            this.RefreshTargets();
            this.CompleteIfChanged(this.groupActions.CancelReloadAll(
                this.targetBuilder.Build(this.modelBuilder.WeaponBay)));
        }

        internal void OpenDefense()
        {
            if (this.openDefensePage == null)
            {
                return;
            }

            this.openDefensePage();
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        internal void OpenModeMenu(ShuttleFireControlGizmoModel model)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            this.AddModeOption(options, ShuttleWeaponFireControlMode.ManualOnly, true);
            this.AddModeOption(
                options,
                ShuttleWeaponFireControlMode.AutoDefense,
                model.AutoDefenseAvailable);
            this.AddModeOption(
                options,
                ShuttleWeaponFireControlMode.PointDefense,
                model.PointDefenseAvailable);
            this.AddModeOption(options, ShuttleWeaponFireControlMode.Offline, true);
            Find.WindowStack.Add(new FloatMenu(options));
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        internal string GetModeTooltip(ShuttleFireControlGizmoModel model)
        {
            string modeLabel = model.MixedFireControlModes
                ? ShuttleUIText.Tr("CT_Shuttle_FireControlGizmo_Mixed")
                : model.HasCommonFireControlMode
                    ? this.GetModeLabel(model.CommonFireControlMode)
                    : ShuttleUIText.Tr("CT_Shuttle_Defense_Unavailable");
            return ShuttleUIText.Tr("CT_Shuttle_FireControlGizmo_ModeTooltip", modeLabel);
        }

        private void AddModeOption(
            List<FloatMenuOption> options,
            ShuttleWeaponFireControlMode mode,
            bool available)
        {
            string label = ShuttleUIText.Tr(
                "CT_Shuttle_FireControl_ModeMenuItem",
                this.GetModeLabel(mode));
            if (!available)
            {
                options.Add(new FloatMenuOption(
                    ShuttleUIText.Tr(
                        "CT_Shuttle_UI_OptionUnavailableFormat",
                        label,
                        ShuttleUIText.Tr(
                            "CT_Shuttle_Defense_GroupNoApplicable")),
                    null));
                return;
            }

            options.Add(new FloatMenuOption(label, delegate { this.SetMode(mode); }));
        }

        private void SetMode(ShuttleWeaponFireControlMode mode)
        {
            this.RefreshTargets();
            this.CompleteIfChanged(this.groupActions.SetAllFireControlMode(
                this.targetBuilder.Build(this.modelBuilder.WeaponBay),
                mode));
        }

        private string GetModeLabel(ShuttleWeaponFireControlMode mode)
        {
            return ShuttleUIText.Tr("CT_Shuttle_FireControl_Mode_" + mode);
        }

        private void RefreshTargets()
        {
            this.modelBuilder.RefreshForInteraction();
        }

        private void CompleteIfChanged(bool changed)
        {
            if (!changed)
            {
                return;
            }

            this.modelBuilder.Invalidate();
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
    }
}

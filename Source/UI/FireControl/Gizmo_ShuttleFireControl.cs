using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    [StaticConstructorOnStartup]
    internal sealed class Gizmo_ShuttleFireControl : Gizmo
    {
        private const float GizmoWidth = 150f;
        private const float GizmoHeight = 75f;
        private const float Padding = 2f;
        private const float CellGap = 2f;
        private const float MiniCellGap = 2f;
        private const float MiniHeight = 24f;

        private readonly ShuttleFireControlGizmoModelBuilder modelBuilder;
        private readonly ShuttleFireControlGizmoActions actions;
        private readonly ShuttleFireControlGizmoButtonDrawer buttons;
        private readonly ShuttleFireControlGizmoTextures textures;

        internal Gizmo_ShuttleFireControl(
            ShuttleFireControlGizmoModelBuilder modelBuilder,
            ShuttleFireControlGizmoActions actions,
            ShuttleFireControlGizmoButtonDrawer buttons,
            ShuttleFireControlGizmoTextures textures)
        {
            this.modelBuilder = modelBuilder;
            this.actions = actions;
            this.buttons = buttons;
            this.textures = textures;
            this.Order = -90f;
        }

        internal bool ShouldDisplay
        {
            get
            {
                ShuttleFireControlGizmoModel model = this.modelBuilder.GetModel();
                return model != null && model.HasWeapons;
            }
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(GizmoWidth, maxWidth);
        }

        public override GizmoResult GizmoOnGUI(
            Vector2 topLeft,
            float maxWidth,
            GizmoRenderParms parms)
        {
            ShuttleFireControlGizmoModel model = this.modelBuilder.GetModel();
            Rect outer = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), GizmoHeight);
            Widgets.DrawWindowBackground(outer);

            Rect inner = outer.ContractedBy(Padding);
            Widgets.DrawBoxSolid(inner, new Color(0.018f, 0.035f, 0.048f, 0.96f));
            GUI.color = new Color(0.18f, 0.34f, 0.42f, 0.9f);
            Widgets.DrawBox(inner, 1);
            GUI.color = Color.white;

            float side = (inner.height - CellGap) / 2f;
            Rect leftTop = new Rect(
                inner.x,
                inner.y,
                side,
                side);
            Rect leftBottom = new Rect(
                inner.x,
                leftTop.yMax + CellGap,
                side,
                side);

            float centerX = leftTop.xMax + CellGap;
            float centerWidth = inner.width - side * 2f - CellGap * 2f;
            float targetHeight = inner.height - MiniHeight - CellGap;
            Rect centerTop = new Rect(
                centerX,
                inner.y,
                centerWidth,
                targetHeight);
            float miniWidth = (centerWidth - MiniCellGap * 2f) / 3f;
            Rect linkRect = new Rect(
                centerX,
                centerTop.yMax + CellGap,
                miniWidth,
                MiniHeight);
            Rect reloadRect = new Rect(
                linkRect.xMax + MiniCellGap,
                linkRect.y,
                miniWidth,
                MiniHeight);
            Rect defenseRect = new Rect(
                reloadRect.xMax + MiniCellGap,
                linkRect.y,
                centerWidth - miniWidth * 2f - MiniCellGap * 2f,
                MiniHeight);

            float rightX = centerTop.xMax + CellGap;
            Rect rightTop = new Rect(
                rightX,
                inner.y,
                side,
                side);
            Rect rightBottom = new Rect(
                rightX,
                rightTop.yMax + CellGap,
                side,
                side);

            if (this.buttons.Draw(
                leftTop,
                this.textures.OpenFire,
                "CT_Shuttle_Defense_GroupOpenFire",
                "CT_Shuttle_Defense_GroupOpenFireTooltip",
                model.CanOpenFire,
                !model.CanOpenFire && model.CanHoldFire &&
                    !model.AllControllableWeaponsHoldFire))
            {
                this.actions.SetHoldFire(false);
            }

            if (this.buttons.Draw(
                leftBottom,
                this.textures.HoldFire,
                "CT_Shuttle_Defense_GroupHoldFire",
                "CT_Shuttle_Defense_GroupHoldFireTooltip",
                model.CanHoldFire,
                model.AllControllableWeaponsHoldFire))
            {
                this.actions.SetHoldFire(true);
            }

            if (this.buttons.Draw(
                centerTop,
                this.textures.Target,
                "CT_Shuttle_FireControlGizmo_TargetAll",
                "CT_Shuttle_FireControlGizmo_TargetAllTooltip",
                model.CanSetForcedTarget,
                model.HasForcedTarget))
            {
                this.actions.BeginTargeting();
            }

            if (this.buttons.Draw(
                rightTop,
                this.textures.ClearTarget,
                "CT_Shuttle_Defense_GroupClearTargets",
                "CT_Shuttle_Defense_GroupClearTargetsTooltip",
                model.CanClearForcedTarget,
                false))
            {
                this.actions.ClearTargets();
            }

            if (this.buttons.DrawTextDescription(
                rightBottom,
                this.textures.Mode,
                "CT_Shuttle_FireControlGizmo_Mode",
                this.actions.GetModeTooltip(model),
                model.CanSetFireControlMode,
                model.HasCommonFireControlMode &&
                    model.CommonFireControlMode != ShuttleWeaponFireControlMode.Offline))
            {
                this.actions.OpenModeMenu(model);
            }

            bool linkNext = model.LinkOnNextClick;
            if (this.buttons.Draw(
                linkRect,
                this.textures.Link,
                linkNext
                    ? "CT_Shuttle_Defense_GroupLinkFireControl"
                    : "CT_Shuttle_Defense_GroupUnlinkFireControl",
                linkNext
                    ? "CT_Shuttle_Defense_GroupLinkFireControlTooltip"
                    : "CT_Shuttle_Defense_GroupUnlinkFireControlTooltip",
                model.CanToggleFireControlLink,
                model.AllLinkableWeaponsLinked))
            {
                this.actions.SetFireControlLinked(linkNext);
            }

            if (this.buttons.Draw(
                reloadRect,
                this.textures.Reload,
                model.ReloadActive
                    ? "CT_Shuttle_Defense_GroupCancelReload"
                    : "CT_Shuttle_Defense_GroupReload",
                model.ReloadActive
                    ? "CT_Shuttle_Defense_GroupCancelReloadTooltip"
                    : "CT_Shuttle_Defense_GroupReloadTooltip",
                model.ReloadActive
                    ? model.CanCancelReload
                    : model.CanReload,
                model.ReloadActive))
            {
                if (model.ReloadActive)
                {
                    this.actions.CancelReloadAll();
                }
                else
                {
                    this.actions.ReloadAll();
                }
            }

            if (this.buttons.Draw(
                defenseRect,
                this.textures.OpenDefense,
                "CT_Shuttle_Issue_Action_OpenDefense",
                "CT_Shuttle_FireControlGizmo_OpenDefenseTooltip",
                this.actions.CanOpenDefense,
                false))
            {
                this.actions.OpenDefense();
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }
}

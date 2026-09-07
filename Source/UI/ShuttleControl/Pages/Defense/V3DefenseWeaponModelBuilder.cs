using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseWeaponModelBuilder
    {
        private readonly V3DefenseTooltipBuilder tooltipBuilder;
        private readonly V3DefenseWeaponAmmoModelBuilder ammoBuilder =
            new V3DefenseWeaponAmmoModelBuilder();
        private readonly V3DefenseWeaponDescriptorBuilder descriptorBuilder =
            new V3DefenseWeaponDescriptorBuilder();
        private readonly V3DefenseWeaponModuleLookup moduleLookup =
            new V3DefenseWeaponModuleLookup();

        internal V3DefenseWeaponModelBuilder(V3DefenseTooltipBuilder tooltipBuilder)
        {
            this.tooltipBuilder = tooltipBuilder ?? new V3DefenseTooltipBuilder();
        }

        internal void BuildWeapons(
            V3DefensePageReadModel model,
            ShuttleWeaponBayReadModel weaponBayModel,
            ShuttleControlReadModel controlModel)
        {
            if (model == null ||
                weaponBayModel == null ||
                weaponBayModel.WeaponControls == null)
            {
                return;
            }

            for (int i = 0; i < weaponBayModel.WeaponControls.Count; i++)
            {
                ShuttleWeaponControlReadModel control = weaponBayModel.WeaponControls[i];
                if (control == null)
                {
                    continue;
                }

                V3DefenseWeaponEntryModel weapon = this.BuildWeapon(control, i);
                model.Weapons.Add(weapon);
                if (weapon.IsOnline && !weapon.IsDormant)
                {
                    model.OnlineWeaponCount++;
                }

                if (weapon.HasFireControl)
                {
                    model.OnlineFireControlCount++;
                }
            }

            if (model.Weapons.Count == 0)
            {
                this.BuildSlotOnlyWeapons(model, weaponBayModel);
            }

            this.moduleLookup.Bind(model.Weapons, controlModel);
        }

        internal void SelectWeapon(
            V3DefensePageReadModel model,
            string selectedWeaponId)
        {
            if (model == null || model.Weapons == null || model.Weapons.Count == 0)
            {
                return;
            }

            for (int i = 0; i < model.Weapons.Count; i++)
            {
                V3DefenseWeaponEntryModel weapon = model.Weapons[i];
                if (weapon != null && weapon.Id == selectedWeaponId)
                {
                    model.SelectedWeapon = weapon;
                    return;
                }
            }

            model.SelectedWeapon = model.Weapons[0];
        }

        internal float GetWeaponPowerWatts(ShuttleWeaponBayReadModel weaponBayModel)
        {
            if (weaponBayModel == null)
            {
                return 0f;
            }

            float standbyWatts = weaponBayModel.TotalStandbyPowerDrawWatts > 0f
                ? weaponBayModel.TotalStandbyPowerDrawWatts
                : 0f;
            float firingSurchargeWatts = weaponBayModel.MaxActiveFiringPowerDrawWatts > 0f
                ? weaponBayModel.MaxActiveFiringPowerDrawWatts
                : 0f;
            return standbyWatts + firingSurchargeWatts;
        }

        private V3DefenseWeaponEntryModel BuildWeapon(
            ShuttleWeaponControlReadModel control,
            int index)
        {
            ShuttleWeaponModuleDef weaponDef =
                this.descriptorBuilder.GetWeaponModuleDef(control.ModuleDefName);
            V3DefenseWeaponEntryModel weapon = new V3DefenseWeaponEntryModel();
            this.ApplyWeaponIdentity(weapon, control, weaponDef, index);
            this.ApplyFireControl(weapon, control);
            this.ammoBuilder.ApplyAmmo(control, weapon);
            weapon.ForcedTargetLabel = string.IsNullOrEmpty(control.ForcedTargetLabel)
                ? ShuttleUIText.Tr("CT_Shuttle_Common_None")
                : control.ForcedTargetLabel;
            weapon.IsOnline = control.CanToggleHoldFire;
            weapon.IsDormant = false;
            weapon.StatusKey = this.descriptorBuilder.GetWeaponStatusKey(weapon);
            weapon.StatusLabel =
                this.descriptorBuilder.GetWeaponStatusLabel(weapon.StatusKey);
            weapon.UIAnchor = this.descriptorBuilder.GetWeaponAnchorForSlotIndex(
                weapon.SlotIndex,
                index);
            weapon.Tooltip = this.tooltipBuilder.BuildWeaponTooltip(weapon);
            return weapon;
        }

        private void ApplyWeaponIdentity(
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponControlReadModel control,
            ShuttleWeaponModuleDef weaponDef,
            int index)
        {
            weapon.Id = !string.IsNullOrEmpty(control.ModuleInstanceID)
                ? control.ModuleInstanceID
                : "weapon-" + index.ToString();
            weapon.ModuleInstanceID = control.ModuleInstanceID;
            string fallbackLabel = !string.IsNullOrEmpty(control.WeaponLabel)
                ? control.WeaponLabel
                : this.descriptorBuilder.GetDefDisplayName(
                    weaponDef,
                    ShuttleUIText.Tr("CT_Shuttle_Defense_UnnamedWeapon"));
            string moduleLabel = this.descriptorBuilder.ResolveModuleDisplayName(
                control.ModuleDefName,
                control.ModuleLabel,
                fallbackLabel);
            weapon.Label = moduleLabel;
            weapon.ModuleLabel = moduleLabel;
            weapon.ModuleDefName = control.ModuleDefName;
            weapon.SlotID = control.ParentSlotID;
            weapon.SlotLabel = control.ParentSlotLabel;
            weapon.SlotShortLabel = control.ParentSlotShortLabel;
            weapon.SlotIndex = control.ParentSlotIndex;
            weapon.IsEnabled = control.IsEnabled;
            weapon.WeaponTypeLabel = !string.IsNullOrEmpty(control.WeaponRoleLabel)
                ? control.WeaponRoleLabel
                : this.descriptorBuilder.GetWeaponRoleFallbackLabel(weaponDef);
            weapon.DamageLabel = this.descriptorBuilder.GetDamageLabel(weaponDef);
            weapon.CooldownLabel =
                this.descriptorBuilder.GetCooldownLabel(weaponDef);
            weapon.FireRateLabel =
                this.descriptorBuilder.GetFireRateLabel(weaponDef);
            weapon.IconKey = this.descriptorBuilder.GetWeaponIconKey(
                weaponDef,
                control);
        }

        private void ApplyFireControl(
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponControlReadModel control)
        {
            weapon.HoldFire = control.HoldFire;
            weapon.HasForcedTarget = control.HasForcedTarget;
            weapon.LastForcedTargetFailureReason = control.LastForcedTargetFailureReason;
            weapon.FireControlLinked = control.FireControlLinked;
            weapon.FireControlMode = control.FireControlMode;
            weapon.FireControlModeLabel = control.FireControlModeLabel;
            weapon.TargetPriority = control.TargetPriority;
            weapon.TargetPriorityLabel = control.TargetPriorityLabel;
            weapon.AutoFireEnabled = control.AutoFireEnabled;
            weapon.HasFireControlRadar = control.HasFireControlRadar;
            weapon.FireControlAvailable = control.FireControlAvailable;
            weapon.EffectiveFireControlLinked = control.EffectiveFireControlLinked;
            weapon.AutoFireAvailable = control.AutoFireAvailable;
            weapon.PointDefenseAvailable = control.PointDefenseAvailable;
            weapon.FireControlUnavailableReason = control.FireControlUnavailableReason;
            weapon.FireControlStatusLabel = control.FireControlStatusLabel;
            weapon.FireControlStatusTooltip = control.FireControlStatusTooltip;
            weapon.CanToggleFireControlLink = control.CanToggleFireControlLink;
            weapon.CanSetFireControlMode = control.CanSetFireControlMode;
            weapon.CanSetTargetPriority = control.CanSetTargetPriority;
            weapon.CanSetAutoFire = control.CanSetAutoFire;
            weapon.HasFireControl = control.EffectiveFireControlLinked;
            weapon.CanToggleHoldFire = control.CanToggleHoldFire;
            weapon.CanSetForcedTarget = control.CanSetForcedTarget;
            weapon.CanClearForcedTarget = control.CanClearForcedTarget;
            weapon.CanTargetLocations = control.CanTargetLocations;
        }

        private void BuildSlotOnlyWeapons(
            V3DefensePageReadModel model,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            if (model == null ||
                weaponBayModel == null ||
                weaponBayModel.WeaponSlots == null)
            {
                return;
            }

            for (int i = 0; i < weaponBayModel.WeaponSlots.Count; i++)
            {
                ShuttleWeaponSlotReadModel slot = weaponBayModel.WeaponSlots[i];
                if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleDefName))
                {
                    continue;
                }

                model.Weapons.Add(this.BuildSlotOnlyWeapon(slot, i));
            }
        }

        private V3DefenseWeaponEntryModel BuildSlotOnlyWeapon(
            ShuttleWeaponSlotReadModel slot,
            int index)
        {
            ShuttleWeaponModuleDef weaponDef =
                this.descriptorBuilder.GetWeaponModuleDef(
                    slot.InstalledModuleDefName);
            V3DefenseWeaponEntryModel weapon = new V3DefenseWeaponEntryModel();
            weapon.Id = !string.IsNullOrEmpty(slot.SlotID)
                ? "slot-" + slot.SlotID
                : "slot-weapon-" + index.ToString();
            weapon.Label = this.descriptorBuilder.ResolveModuleDisplayName(
                slot.InstalledModuleDefName,
                slot.InstalledModuleLabel,
                this.descriptorBuilder.GetDefDisplayName(
                    weaponDef,
                    slot.InstalledModuleDefName));
            this.ApplySlotOnlyWeaponDefaults(weapon, slot, weaponDef, index);
            weapon.Tooltip = this.tooltipBuilder.BuildSlotOnlyWeaponTooltip(weapon);
            return weapon;
        }

        private void ApplySlotOnlyWeaponDefaults(
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponSlotReadModel slot,
            ShuttleWeaponModuleDef weaponDef,
            int index)
        {
            weapon.ModuleLabel = weapon.Label;
            weapon.ModuleDefName = slot.InstalledModuleDefName;
            weapon.SlotID = slot.SlotID;
            weapon.SlotLabel = slot.SlotLabel;
            weapon.SlotShortLabel = slot.SlotShortLabel;
            weapon.SlotIndex = slot.SlotIndex;
            weapon.IsEnabled = true;
            weapon.WeaponTypeLabel =
                this.descriptorBuilder.GetWeaponRoleFallbackLabel(weaponDef);
            weapon.DamageLabel = this.descriptorBuilder.GetDamageLabel(weaponDef);
            weapon.CooldownLabel =
                this.descriptorBuilder.GetCooldownLabel(weaponDef);
            weapon.FireRateLabel =
                this.descriptorBuilder.GetFireRateLabel(weaponDef);
            weapon.IconKey =
                this.descriptorBuilder.GetWeaponIconKey(weaponDef, null);
            weapon.FireControlLinked = true;
            weapon.FireControlMode = "AutoDefense";
            weapon.FireControlModeLabel =
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Mode_AutoDefense");
            weapon.TargetPriority = "ClosestHostile";
            weapon.TargetPriorityLabel =
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_ClosestHostile");
            weapon.AutoFireEnabled = true;
            weapon.FireControlUnavailableReason =
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Unavailable_Generic");
            weapon.FireControlStatusLabel =
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Status_WaitingForRadar");
            weapon.FireControlStatusTooltip =
                ShuttleUIText.Tr("CT_Shuttle_FireControl_EnableTooltip_NoRadar");
            weapon.ForcedTargetLabel =
                ShuttleUIText.Tr("CT_Shuttle_Defense_Unavailable");
            weapon.StatusKey = this.descriptorBuilder.GetWeaponStatusKey(weapon);
            weapon.StatusLabel =
                this.descriptorBuilder.GetWeaponStatusLabel(weapon.StatusKey);
            weapon.UIAnchor = this.descriptorBuilder.GetWeaponAnchorForSlotIndex(
                weapon.SlotIndex,
                index);
        }
    }
}

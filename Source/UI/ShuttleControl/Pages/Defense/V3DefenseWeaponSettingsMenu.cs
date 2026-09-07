using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseWeaponSettingsMenu
    {
        private readonly V3DefenseText text = new V3DefenseText();

        internal void Open(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            this.AddAmmoMenuOption(options, context, weapon);
            this.AddFireControlModeMenuOption(options, context, weapon);
            this.AddTargetPriorityMenuOption(options, context, weapon);
            this.AddAutoFireOption(options, context, weapon);
            this.AddFireControlLinkOption(options, context, weapon);
            this.AddAutoReloadOption(options, context, weapon);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        internal void OpenFireControl(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            this.AddFireControlModeMenuOption(options, context, weapon);
            this.AddTargetPriorityMenuOption(options, context, weapon);
            this.AddAutoFireOption(options, context, weapon);
            this.AddFireControlLinkOption(options, context, weapon);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void AddAmmoMenuOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            // Fixed-ammunition weapons do not need a disabled pseudo-command in Settings.
            if (weapon == null ||
                !weapon.HasAmmoSystem ||
                weapon.AmmoOptions == null ||
                weapon.AmmoOptions.Count <= 1)
            {
                return;
            }

            string label = ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_SelectAmmo");
            if (weapon.CanSelectAmmo &&
                GetAmmoActions(context) != null)
            {
                options.Add(new FloatMenuOption(
                    label,
                    delegate { this.OpenAmmoMenu(context, weapon); }));
                return;
            }

            options.Add(new FloatMenuOption(
                FormatUnavailableOption(
                    label,
                    ShuttleUIText.Tr(
                        "CT_Shuttle_WeaponAmmo_AmmoSelectionUnavailable")),
                null));
        }

        private void AddFireControlModeMenuOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            string label = ShuttleUIText.Tr("CT_Shuttle_FireControl_ModeButton");
            if (weapon.CanSetFireControlMode &&
                GetFireControlActions(context) != null)
            {
                options.Add(new FloatMenuOption(
                    label,
                    delegate { this.OpenFireControlModeMenu(context, weapon); }));
                return;
            }

            options.Add(new FloatMenuOption(
                FormatUnavailableOption(
                    label,
                    this.GetFireControlDisabledReason(weapon)),
                null));
        }

        private void AddTargetPriorityMenuOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            string label = ShuttleUIText.Tr("CT_Shuttle_FireControl_PriorityButton");
            if (weapon.CanSetTargetPriority &&
                GetFireControlActions(context) != null)
            {
                options.Add(new FloatMenuOption(
                    label,
                    delegate { this.OpenTargetPriorityMenu(context, weapon); }));
                return;
            }

            options.Add(new FloatMenuOption(
                FormatUnavailableOption(
                    label,
                    this.GetFireControlDisabledReason(weapon)),
                null));
        }

        private void AddAutoFireOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            string label = weapon.AutoFireEnabled
                ? ShuttleUIText.Tr("CT_Shuttle_FireControl_DisableAutoFire")
                : ShuttleUIText.Tr("CT_Shuttle_FireControl_EnableAutoFire");
            if (!weapon.CanSetAutoFire ||
                GetFireControlActions(context) == null)
            {
                options.Add(new FloatMenuOption(
                    FormatUnavailableOption(
                        label,
                        this.GetFireControlDisabledReason(weapon)),
                    null));
                return;
            }

            options.Add(new FloatMenuOption(
                label,
                delegate
                {
                    this.SetWeaponAutoFireEnabled(
                        context,
                        weapon,
                        !weapon.AutoFireEnabled);
                }));
        }

        private void AddFireControlLinkOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            string label = this.GetFireControlLinkButtonLabel(weapon);
            if (this.CanToggleFireControlLink(context, weapon))
            {
                options.Add(new FloatMenuOption(
                    label,
                    delegate { this.ToggleFireControlLink(context, weapon); }));
                return;
            }

            options.Add(new FloatMenuOption(
                FormatUnavailableOption(
                    label,
                    this.GetFireControlLinkTooltip(context, weapon)),
                null));
        }

        private void AddAutoReloadOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            string label = this.text.GetAutoReloadLabel(
                weapon.AutoReloadEnabled,
                weapon.AutoLoaderAvailable);
            if (weapon.HasAmmoSystem &&
                weapon.CanToggleAutoReload &&
                GetAmmoActions(context) != null)
            {
                options.Add(new FloatMenuOption(
                    label,
                    delegate
                    {
                        this.SetWeaponAutoReload(
                            context,
                            weapon,
                            !weapon.AutoReloadEnabled);
                    }));
                return;
            }

            options.Add(new FloatMenuOption(
                FormatUnavailableOption(
                    label,
                    this.GetAmmoCommandTooltip(weapon)),
                null));
        }

        private void OpenAmmoMenu(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || weapon.AmmoOptions == null)
            {
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < weapon.AmmoOptions.Count; i++)
            {
                V3DefenseAmmoOptionModel option = weapon.AmmoOptions[i];
                if (option == null || string.IsNullOrEmpty(option.AmmoDefName))
                {
                    continue;
                }

                string label = ShuttleUIText.Tr(
                    "CT_Shuttle_WeaponAmmo_OptionStockFormat",
                    option.Label,
                    option.StockCount);
                if (option.Selected)
                {
                    label = ShuttleUIText.Tr(
                        "CT_Shuttle_WeaponAmmo_OptionSelectedFormat",
                        label);
                }

                string ammoDefName = option.AmmoDefName;
                options.Add(new FloatMenuOption(
                    label,
                    delegate
                    {
                        this.SetWeaponAmmo(context, weapon, ammoDefName);
                    }));
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void OpenFireControlModeMenu(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (weapon != null && !weapon.HasFireControlRadar)
            {
                options.Add(new FloatMenuOption(
                    ShuttleUIText.Tr("CT_Shuttle_FireControl_PresetWithoutRadar"),
                    null));
            }

            this.AddFireControlModeOption(
                options,
                context,
                weapon,
                ShuttleWeaponFireControlMode.ManualOnly,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Mode_ManualOnly"),
                true);
            this.AddFireControlModeOption(
                options,
                context,
                weapon,
                ShuttleWeaponFireControlMode.AutoDefense,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Mode_AutoDefense"),
                weapon != null && (!weapon.HasFireControlRadar || weapon.AutoFireAvailable));
            this.AddFireControlModeOption(
                options,
                context,
                weapon,
                ShuttleWeaponFireControlMode.PointDefense,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Mode_PointDefense"),
                weapon != null && (!weapon.HasFireControlRadar || weapon.PointDefenseAvailable));
            this.AddFireControlModeOption(
                options,
                context,
                weapon,
                ShuttleWeaponFireControlMode.Offline,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_Mode_Offline"),
                true);

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenTargetPriorityMenu(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (weapon != null && !weapon.HasFireControlRadar)
            {
                options.Add(new FloatMenuOption(
                    ShuttleUIText.Tr("CT_Shuttle_FireControl_PresetWithoutRadar"),
                    null));
            }

            this.AddTargetPriorityOption(
                options,
                context,
                weapon,
                ShuttleWeaponTargetPriority.ClosestHostile,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_ClosestHostile"));
            this.AddTargetPriorityOption(
                options,
                context,
                weapon,
                ShuttleWeaponTargetPriority.RaidersFirst,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_RaidersFirst"));
            this.AddTargetPriorityOption(
                options,
                context,
                weapon,
                ShuttleWeaponTargetPriority.MechanoidsFirst,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_MechanoidsFirst"));
            this.AddTargetPriorityOption(
                options,
                context,
                weapon,
                ShuttleWeaponTargetPriority.ManhuntersFirst,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_ManhuntersFirst"));
            this.AddTargetPriorityOption(
                options,
                context,
                weapon,
                ShuttleWeaponTargetPriority.HighThreatFirst,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_HighThreatFirst"));
            this.AddTargetPriorityOption(
                options,
                context,
                weapon,
                ShuttleWeaponTargetPriority.ForcedTargetOnly,
                ShuttleUIText.Tr("CT_Shuttle_FireControl_TargetPriority_ForcedTargetOnly"));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void AddFireControlModeOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponFireControlMode mode,
            string label,
            bool available)
        {
            string displayLabel = ShuttleUIText.Tr(
                "CT_Shuttle_FireControl_ModeMenuItem",
                label);
            if (weapon == null || !weapon.CanSetFireControlMode || !available)
            {
                options.Add(new FloatMenuOption(
                    FormatUnavailableOption(
                        displayLabel,
                        this.GetFireControlDisabledReason(weapon)),
                    null));
                return;
            }

            options.Add(new FloatMenuOption(
                displayLabel,
                delegate { this.SetWeaponFireControlMode(context, weapon, mode); }));
        }

        private void AddTargetPriorityOption(
            List<FloatMenuOption> options,
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponTargetPriority priority,
            string label)
        {
            string displayLabel = ShuttleUIText.Tr(
                "CT_Shuttle_FireControl_TargetPriorityMenuItem",
                label);
            if (weapon == null || !weapon.CanSetTargetPriority)
            {
                options.Add(new FloatMenuOption(
                    FormatUnavailableOption(
                        displayLabel,
                        this.GetFireControlDisabledReason(weapon)),
                    null));
                return;
            }

            options.Add(new FloatMenuOption(
                displayLabel,
                delegate { this.SetWeaponTargetPriority(context, weapon, priority); }));
        }

        private bool CanToggleFireControlLink(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.CanToggleWeaponFireControlLink(target);
        }

        private bool ToggleFireControlLink(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.ToggleWeaponFireControlLink(target);
        }

        private string GetFireControlLinkTooltip(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null && target != null
                ? actions.GetFireControlLinkTooltip(target)
                : ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        private bool SetWeaponFireControlMode(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponFireControlMode mode)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.SetWeaponFireControlMode(target, mode);
        }

        private bool SetWeaponTargetPriority(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            ShuttleWeaponTargetPriority priority)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.SetWeaponTargetPriority(target, priority);
        }

        private bool SetWeaponAutoFireEnabled(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            bool enabled)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.SetWeaponAutoFireEnabled(target, enabled);
        }

        private bool SetWeaponAmmo(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            string ammoDefName)
        {
            IShuttleDefenseWeaponAmmoUIActions actions = GetAmmoActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.SetWeaponAmmo(target, ammoDefName);
        }

        private bool SetWeaponAutoReload(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon,
            bool enabled)
        {
            IShuttleDefenseWeaponAmmoUIActions actions = GetAmmoActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.SetWeaponAutoReload(target, enabled);
        }

        private static IShuttleDefenseWeaponFireControlUIActions GetFireControlActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.WeaponFireControlActions
                : null;
        }

        private static IShuttleDefenseWeaponAmmoUIActions GetAmmoActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.WeaponAmmoActions
                : null;
        }

        private string GetFireControlLinkButtonLabel(
            V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || !weapon.CanToggleFireControlLink)
            {
                return ShuttleUIText.Tr("CT_Shuttle_FireControl_Unavailable");
            }

            return weapon.FireControlLinked
                ? ShuttleUIText.Tr("CT_Shuttle_FireControl_Disable")
                : ShuttleUIText.Tr("CT_Shuttle_FireControl_Enable");
        }

        private string GetFireControlDisabledReason(
            V3DefenseWeaponEntryModel weapon)
        {
            return weapon != null && !string.IsNullOrEmpty(weapon.FireControlUnavailableReason)
                ? weapon.FireControlUnavailableReason
                : ShuttleUIText.Tr("CT_Shuttle_FireControl_Unavailable_Generic");
        }

        private string GetAmmoCommandTooltip(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                return ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_NoAmmoSystem");
            }

            if (!string.IsNullOrEmpty(weapon.LastAmmoFailureReason))
            {
                return weapon.LastAmmoFailureReason;
            }

            if (!string.IsNullOrEmpty(weapon.LastReloadBlockerReason))
            {
                return weapon.LastReloadBlockerReason;
            }

            if (weapon.MagazineCapacity > 0 &&
                weapon.LoadedAmmoCount >= weapon.MagazineCapacity)
            {
                return ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_Full");
            }

            return weapon.ReloadInProgress || weapon.ReloadRequested
                ? ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_CancelReload")
                : ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_Reload");
        }

        private static string FormatUnavailableOption(
            string label,
            string reason)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_UI_OptionUnavailableFormat",
                label,
                reason);
        }
    }
}

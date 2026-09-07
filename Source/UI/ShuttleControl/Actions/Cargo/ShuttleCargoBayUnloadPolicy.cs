using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal static class ShuttleCargoBayUnloadPolicy
    {
        internal static bool CanUnloadBay(ShuttleCargoBayActionTarget bay)
        {
            return bay != null &&
                (!bay.IsRefrigerated || bay.IsEnabled) &&
                CountUnloadableStacks(bay) > 0;
        }

        internal static string GetUnloadBayTooltip(ShuttleCargoBayActionTarget bay)
        {
            if (bay == null)
            {
                return Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (bay.IsRefrigerated && !bay.IsEnabled)
            {
                return !string.IsNullOrEmpty(bay.InactiveReason)
                    ? bay.InactiveReason
                    : Tr("CT_Shuttle_Cargo_ColdSettingsUnavailable");
            }

            int stackCount = CountUnloadableStacks(bay);
            if (stackCount <= 0)
            {
                return Tr("CT_Shuttle_Cargo_BayUnloadNoContents");
            }

            return Tr(
                "CT_Shuttle_Cargo_Action_UnloadBayTooltip",
                stackCount,
                CountUnloadableThings(bay));
        }

        internal static int CountUnloadableStacks(ShuttleCargoBayActionTarget bay)
        {
            int count = 0;
            for (int i = 0; bay != null && bay.Items != null && i < bay.Items.Count; i++)
            {
                if (IsUnloadableStack(bay, bay.Items[i]))
                {
                    count += ShuttleCargoStackMemberActionUtility.CountMembers(
                        bay.Items[i]);
                }
            }

            return count;
        }

        internal static int CountUnloadableThings(ShuttleCargoBayActionTarget bay)
        {
            int count = 0;
            for (int i = 0; bay != null && bay.Items != null && i < bay.Items.Count; i++)
            {
                ShuttleCargoStackActionTarget stack = bay.Items[i];
                if (IsUnloadableStack(bay, stack))
                {
                    count += stack.StackCount;
                }
            }

            return count;
        }

        internal static bool IsUnloadableStack(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoStackActionTarget stack)
        {
            if (bay == null || stack == null || stack.StackCount <= 0)
            {
                return false;
            }

            if (bay.IsRefrigerated)
            {
                return !string.IsNullOrEmpty(GetModuleInstanceID(bay, stack)) &&
                    ShuttleCargoStackMemberActionUtility.HasValidMembers(
                        stack,
                        ShuttleCargoStackActionSourceKind.RefrigeratedCargo);
            }

            return ShuttleCargoStackMemberActionUtility.HasValidMembers(
                stack,
                ShuttleCargoStackActionSourceKind.LoadedCargo);
        }

        internal static string GetModuleInstanceID(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoStackActionTarget stack)
        {
            if (stack != null && !string.IsNullOrEmpty(stack.ModuleInstanceID))
            {
                return stack.ModuleInstanceID;
            }

            return bay != null ? bay.ModuleInstanceID : null;
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        private static string Tr(string key, object arg0, object arg1)
        {
            return ShuttleUIText.Tr(key, arg0, arg1);
        }
    }
}

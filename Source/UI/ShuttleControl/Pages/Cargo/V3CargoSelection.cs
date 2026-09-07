using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoSelection
    {
        internal List<V3CargoVisibleStackModel> BuildVisibleStacks(
            V3CargoPageReadModel model,
            V3CargoCategory selectedCategory)
        {
            List<V3CargoVisibleStackModel> result = new List<V3CargoVisibleStackModel>();
            if (model == null || model.Bays == null)
            {
                return result;
            }

            for (int bayIndex = 0; bayIndex < model.Bays.Count; bayIndex++)
            {
                V3CargoBayCardModel bay = model.Bays[bayIndex];
                if (bay == null || bay.Items == null)
                {
                    continue;
                }

                for (int stackIndex = 0; stackIndex < bay.Items.Count; stackIndex++)
                {
                    V3CargoStackCardModel stack = bay.Items[stackIndex];
                    if (!this.MatchesCategory(stack, selectedCategory))
                    {
                        continue;
                    }

                    result.Add(new V3CargoVisibleStackModel(
                        bay,
                        stack,
                        GetBayKey(bay, bayIndex),
                        GetStackKey(bay, stack, bayIndex, stackIndex)));
                }
            }

            return result;
        }

        internal V3CargoVisibleStackModel EnsureSelectedStack(
            V3CargoPageState state,
            List<V3CargoVisibleStackModel> visibleStacks)
        {
            if (state == null || visibleStacks == null || visibleStacks.Count == 0)
            {
                if (state != null)
                {
                    state.SelectedBayKey = null;
                    state.SelectedStackKey = null;
                }

                return null;
            }

            V3CargoVisibleStackModel selected =
                this.FindSelectedStack(state.SelectedStackKey, visibleStacks);
            if (selected == null)
            {
                selected = visibleStacks[0];
            }

            this.SelectStack(state, selected);
            return selected;
        }

        internal void SelectStack(
            V3CargoPageState state,
            V3CargoVisibleStackModel selected)
        {
            if (state == null)
            {
                return;
            }

            state.SelectedBayKey = selected != null ? selected.BayKey : null;
            state.SelectedStackKey = selected != null ? selected.StackKey : null;
        }

        internal bool MatchesCategory(
            V3CargoStackCardModel stack,
            V3CargoCategory selectedCategory)
        {
            return selectedCategory == V3CargoCategory.None ||
                (stack != null && stack.Category == selectedCategory);
        }

        private V3CargoVisibleStackModel FindSelectedStack(
            string selectedStackKey,
            List<V3CargoVisibleStackModel> visibleStacks)
        {
            if (string.IsNullOrEmpty(selectedStackKey) || visibleStacks == null)
            {
                return null;
            }

            for (int i = 0; i < visibleStacks.Count; i++)
            {
                V3CargoVisibleStackModel candidate = visibleStacks[i];
                if (candidate != null && candidate.StackKey == selectedStackKey)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetBayKey(V3CargoBayCardModel bay, int bayIndex)
        {
            if (bay == null)
            {
                return "bay:" + bayIndex.ToString();
            }

            if (!string.IsNullOrEmpty(bay.BayKey))
            {
                return bay.BayKey;
            }

            if (!string.IsNullOrEmpty(bay.ModuleInstanceID))
            {
                return "module:" + bay.ModuleInstanceID;
            }

            return "bay:" + bayIndex.ToString();
        }

        private static string GetStackKey(
            V3CargoBayCardModel bay,
            V3CargoStackCardModel stack,
            int bayIndex,
            int stackIndex)
        {
            string bayKey = GetBayKey(bay, bayIndex);
            if (stack == null)
            {
                return bayKey + ":stack:" + stackIndex.ToString();
            }

            if (stack.SourceKind == V3CargoStackSourceKind.LoadedCargo)
            {
                return bayKey + ":loaded:" +
                    GetStableThingID(stack).ToString();
            }

            if (stack.SourceKind == V3CargoStackSourceKind.RefrigeratedCargo)
            {
                return bayKey + ":cold:" +
                    (stack.ModuleInstanceID ?? string.Empty) + ":" +
                    GetStableThingID(stack).ToString();
            }

            return bayKey + ":stack:" + stackIndex.ToString();
        }

        private static int GetStableThingID(V3CargoStackCardModel stack)
        {
            int result = stack != null ? stack.ThingIDNumber : 0;
            for (int i = 0; stack != null && stack.Members != null && i < stack.Members.Count; i++)
            {
                V3CargoStackMemberModel member = stack.Members[i];
                if (member != null &&
                    member.ThingIDNumber > 0 &&
                    (result <= 0 || member.ThingIDNumber < result))
                {
                    result = member.ThingIDNumber;
                }
            }

            return result;
        }
    }

    internal sealed class V3CargoVisibleStackModel
    {
        internal readonly V3CargoBayCardModel Bay;
        internal readonly V3CargoStackCardModel Stack;
        internal readonly string BayKey;
        internal readonly string StackKey;

        internal V3CargoVisibleStackModel(
            V3CargoBayCardModel bay,
            V3CargoStackCardModel stack,
            string bayKey,
            string stackKey)
        {
            this.Bay = bay;
            this.Stack = stack;
            this.BayKey = bayKey;
            this.StackKey = stackKey;
        }
    }
}

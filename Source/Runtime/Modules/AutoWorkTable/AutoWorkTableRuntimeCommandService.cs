using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableRuntimeCommandService
    {
        internal bool TrySetSelectedRecipe(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string sourceBenchDefName,
            string recipeDefName,
            out string failReason)
        {
            failReason = null;

            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out moduleDef,
                out state,
                out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TrySetSelectedRecipe(
                state,
                moduleDef,
                sourceBenchDefName,
                recipeDefName,
                out failReason);
        }

        internal bool TryClearSelectedRecipe(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out string failReason)
        {
            failReason = null;

            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out moduleDef,
                out state,
                out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TryClearSelectedRecipe(
                state,
                out failReason);
        }

        internal bool TrySetProductionPolicy(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failReason)
        {
            failReason = null;

            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out moduleDef,
                out state,
                out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TrySetProductionPolicy(
                state,
                moduleDef,
                mode,
                repeatCount,
                targetCount,
                out failReason);
        }

        internal bool TrySetPaused(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool paused,
            out string failReason)
        {
            failReason = null;

            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out moduleDef,
                out state,
                out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TrySetPaused(
                state,
                paused,
                out failReason);
        }

        internal bool TryPauseForLaunch(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out string failReason)
        {
            failReason = null;
            if (assemblyState == null)
            {
                failReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            List<AutoWorkTableRuntimeState> statesToPause = new List<AutoWorkTableRuntimeState>();
            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module == null ||
                    !(module.ModuleDef is ShuttleAutoWorkTableModuleDef) ||
                    !this.IsInstalledModuleReferenceCurrent(assemblyState, module))
                {
                    continue;
                }

                if (runtimeState == null)
                {
                    failReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString();
                    return false;
                }

                IShuttleModuleRuntimeState runtimePayload;
                if (!runtimeState.Modules.TryGetState(
                    module.ModuleInstanceID,
                    AutoWorkTableRuntimeSystem.AutoWorkTableRuntimeSystemKey,
                    out runtimePayload))
                {
                    failReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                    return false;
                }

                AutoWorkTableRuntimeState state = runtimePayload as AutoWorkTableRuntimeState;
                if (state == null)
                {
                    failReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                    return false;
                }

                state.EnsureInitialized();
                if (state.CompletionCommitBlocked)
                {
                    failReason = "AutoWorkTable completion commit is blocked.";
                    return false;
                }

                if (state.ProductionOrderCount > 0 ||
                    state.HasActiveProduction ||
                    state.HasPendingProducts ||
                    state.HasStagedIngredients)
                {
                    statesToPause.Add(state);
                }
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            for (int i = 0; i < statesToPause.Count; i++)
            {
                statesToPause[i].SetPaused(true, ticksGame);
            }

            return true;
        }

        internal bool TryAddProductionOrder(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string sourceBenchDefName,
            string recipeDefName,
            out string failReason)
        {
            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState, runtimeState, moduleInstanceID,
                out moduleDef, out state, out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TryAddProductionOrder(
                state, moduleDef, sourceBenchDefName, recipeDefName, out failReason);
        }

        internal bool TryRemoveProductionOrder(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            out string failReason)
        {
            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState, runtimeState, moduleInstanceID,
                out moduleDef, out state, out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TryRemoveProductionOrder(
                state, orderId, out failReason);
        }

        internal bool TryMoveProductionOrder(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            int direction,
            out string failReason)
        {
            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState, runtimeState, moduleInstanceID,
                out moduleDef, out state, out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TryMoveProductionOrder(
                state, orderId, direction, out failReason);
        }

        internal bool TrySetProductionOrderSuspended(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            bool suspended,
            out string failReason)
        {
            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState, runtimeState, moduleInstanceID,
                out moduleDef, out state, out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TrySetProductionOrderSuspended(
                state, orderId, suspended, out failReason);
        }

        internal bool TrySetProductionOrderPolicy(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failReason)
        {
            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState, runtimeState, moduleInstanceID,
                out moduleDef, out state, out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TrySetProductionOrderPolicy(
                state, moduleDef, orderId, mode, repeatCount, targetCount, out failReason);
        }

        internal bool TrySetProductionOrderIngredientFilter(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            ThingFilter ingredientFilter,
            bool useRecipeDefault,
            out string failReason)
        {
            ShuttleAutoWorkTableModuleDef moduleDef;
            AutoWorkTableRuntimeState state;
            if (!this.TryGetAutoWorkTableRuntimeState(
                assemblyState, runtimeState, moduleInstanceID,
                out moduleDef, out state, out failReason))
            {
                return false;
            }

            return AutoWorkTableRuntimeSystem.Instance.TrySetProductionOrderIngredientFilter(
                state, moduleDef, orderId, ingredientFilter, useRecipeDefault, out failReason);
        }

        private bool TryGetAutoWorkTableRuntimeState(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out ShuttleAutoWorkTableModuleDef moduleDef,
            out AutoWorkTableRuntimeState state,
            out string failReason)
        {
            moduleDef = null;
            state = null;
            failReason = null;

            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                failReason = "CT_Shuttle_AutoWorkTable_ModuleIdMissing".Translate().ToString();
                return false;
            }

            if (assemblyState == null)
            {
                failReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            ShuttleModule module = assemblyState.GetModule(moduleInstanceID);
            if (module == null || !this.IsInstalledModuleReferenceCurrent(assemblyState, module))
            {
                failReason = "CT_Shuttle_AutoWorkTable_ModuleNotInstalled".Translate().ToString();
                return false;
            }

            if (!module.IsEnabled)
            {
                failReason = "CT_Shuttle_AutoWorkTable_ModuleDisabled".Translate().ToString();
                return false;
            }

            moduleDef = module.ModuleDef as ShuttleAutoWorkTableModuleDef;
            if (moduleDef == null)
            {
                failReason = "CT_Shuttle_AutoWorkTable_TargetNotModule".Translate().ToString();
                return false;
            }

            if (runtimeState == null)
            {
                failReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            IShuttleModuleRuntimeState runtimePayload;
            if (!runtimeState.Modules.TryGetState(
                moduleInstanceID,
                AutoWorkTableRuntimeSystem.AutoWorkTableRuntimeSystemKey,
                out runtimePayload))
            {
                failReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            state = runtimePayload as AutoWorkTableRuntimeState;
            if (state == null)
            {
                failReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool IsInstalledModuleReferenceCurrent(
            ShuttleAssemblyState assemblyState,
            ShuttleModule module)
        {
            if (assemblyState == null || module == null)
            {
                return false;
            }

            ShuttleModuleSlot slot = assemblyState.GetModuleSlot(
                module.ParentSegmentInstanceID,
                module.ParentSlotID);
            return slot != null && slot.InstalledModuleInstanceID == module.ModuleInstanceID;
        }
    }
}

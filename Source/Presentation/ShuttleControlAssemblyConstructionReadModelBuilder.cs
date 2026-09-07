using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleControlAssemblyConstructionReadModelBuilder
    {
        internal void ApplyAssemblyConstruction(ShuttleControlReadModel model, ShuttleRuntimeState runtimeState)
        {
            if (model == null)
            {
                return;
            }

            model.AssemblyConstruction = new ShuttleAssemblyConstructionReadModel();
            if (runtimeState == null)
            {
                return;
            }

            runtimeState.EnsureInitialized();
            ShuttleAssemblyConstructionState state = runtimeState.AssemblyConstruction;
            if (state == null)
            {
                return;
            }

            model.AssemblyConstruction.LastFailureReason = state.LastFailureReason;
            this.ApplyQueuedOrders(model.AssemblyConstruction, state);
            if (!state.HasActiveOrder || state.ActiveOrder == null)
            {
                return;
            }

            ShuttleAssemblyConstructionOrder order = state.ActiveOrder;
            model.AssemblyConstruction.HasActiveOrder = true;
            model.AssemblyConstruction.OrderID = order.OrderID;
            model.AssemblyConstruction.Label = order.Label;
            model.AssemblyConstruction.TargetLabel = order.TargetLabel;
            model.AssemblyConstruction.StatusLabel = this.GetAssemblyConstructionStatusLabel(order.Status);
            model.AssemblyConstruction.Progress01 = order.Progress01;
            model.AssemblyConstruction.MaterialSummary =
                ShuttleConstructionMaterialUtility.BuildMaterialProgressSummary(order, state.StagedIngredients);
            model.AssemblyConstruction.MaterialProgress01 =
                ShuttleConstructionMaterialUtility.GetMaterialProgress01(order, state.StagedIngredients);
            model.AssemblyConstruction.AwaitingMaterials =
                order.Status == ShuttleAssemblyConstructionStatus.AwaitingMaterials;
            model.AssemblyConstruction.MaterialsReady =
                ShuttleConstructionMaterialUtility.HasAllRequiredMaterials(order, state.StagedIngredients);
            model.AssemblyConstruction.WorkDone = order.WorkDone;
            model.AssemblyConstruction.WorkTotal = order.WorkTotal;
            model.AssemblyConstruction.CostSummary = order.CostSummary;
            model.AssemblyConstruction.LastFailureReason = !string.IsNullOrEmpty(order.LastFailureReason)
                ? order.LastFailureReason
                : state.LastFailureReason;
        }

        private void ApplyQueuedOrders(
            ShuttleAssemblyConstructionReadModel model,
            ShuttleAssemblyConstructionState state)
        {
            if (model == null || state == null || state.QueuedOrders == null)
            {
                return;
            }

            for (int i = 0; i < state.QueuedOrders.Count; i++)
            {
                ShuttleAssemblyConstructionOrder order = state.QueuedOrders[i];
                if (order == null)
                {
                    continue;
                }

                model.QueuedOrders.Add(new ShuttleAssemblyConstructionQueueItemReadModel
                {
                    OrderID = order.OrderID,
                    TargetLabel = order.TargetLabel,
                    CostSummary = order.CostSummary,
                    Position = i + 1
                });
            }
        }

        private string GetAssemblyConstructionStatusLabel(ShuttleAssemblyConstructionStatus status)
        {
            if (status == ShuttleAssemblyConstructionStatus.AwaitingMaterials)
            {
                return "CT_Shuttle_AssemblyConstruction_Status_AwaitingMaterials".Translate().ToString();
            }

            if (status == ShuttleAssemblyConstructionStatus.Completing)
            {
                return "CT_Shuttle_AssemblyConstruction_Status_Completing".Translate().ToString();
            }

            if (status == ShuttleAssemblyConstructionStatus.Failed)
            {
                return "CT_Shuttle_AssemblyConstruction_Status_Failed".Translate().ToString();
            }

            return "CT_Shuttle_AssemblyConstruction_Status_WaitingForWork".Translate().ToString();
        }
    }
}

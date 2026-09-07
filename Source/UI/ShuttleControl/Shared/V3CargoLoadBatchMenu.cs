using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadBatchMenu
    {
        private readonly V3CargoLoadBatchSelectionService selectionService;
        private readonly V3CargoLoadBatchCategoryBuilder categoryBuilder;
        private readonly Action onSelectionChanged;

        internal V3CargoLoadBatchMenu(
            V3CargoLoadBatchSelectionService selectionService,
            V3CargoLoadCategoryResolver categoryResolver,
            V3CargoLoadTransferableMetricsResolver metricsResolver,
            Action onSelectionChanged)
        {
            this.selectionService = selectionService;
            this.categoryBuilder = new V3CargoLoadBatchCategoryBuilder(
                categoryResolver,
                metricsResolver);
            this.onSelectionChanged = onSelectionChanged;
        }

        internal bool CanOpenWithoutTargets
        {
            get
            {
                return this.selectionService != null &&
                    (this.selectionService.CanClearAll || this.selectionService.CanUndo);
            }
        }

        internal void Open(
            List<TransferableOneWay> targets,
            bool passengers)
        {
            if (Find.WindowStack == null)
            {
                return;
            }

            List<V3CargoLoadBatchCategory> categories =
                this.categoryBuilder.Build(targets, passengers);
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < categories.Count; i++)
            {
                V3CargoLoadBatchCategory category = categories[i];
                V3CargoLoadBatchCategory categorySnapshot = category;
                options.Add(new FloatMenuOption(
                    V3CargoLoadText.Tr(
                        passengers
                            ? "CT_Shuttle_LoadCargo_BatchBoardCategory"
                            : "CT_Shuttle_LoadCargo_BatchLoadCategory",
                        category.Label,
                        FormatAvailableCount(category.AvailableCount)),
                    delegate
                    {
                        this.TrySelectCategory(categorySnapshot, passengers);
                    }));
            }

            bool canClearAll = this.selectionService != null &&
                this.selectionService.CanClearAll;
            bool canUndo = this.selectionService != null &&
                this.selectionService.CanUndo;
            if (canClearAll || canUndo)
            {
                options.Add(new FloatMenuOption(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchManageSelection"),
                    null));
                options.Add(new FloatMenuOption(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchClearAll"),
                    canClearAll
                        ? (Action)delegate
                        {
                            this.ClearAllSelections();
                        }
                        : null));
                options.Add(new FloatMenuOption(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchUndo"),
                    canUndo
                        ? (Action)delegate
                        {
                            this.Complete(this.selectionService.Undo());
                        }
                        : null));
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        internal void ClearAllSelections()
        {
            if (this.selectionService == null)
            {
                return;
            }

            this.Complete(this.selectionService.ClearAll());
        }

        private void TrySelectCategory(
            V3CargoLoadBatchCategory category,
            bool passengers)
        {
            if (category == null ||
                category.Targets == null ||
                category.Targets.Count == 0 ||
                this.selectionService == null)
            {
                return;
            }

            V3CargoLoadBatchResult result =
                this.selectionService.SelectAll(category.Targets);
            if (result != null && result.Success)
            {
                this.CompleteCategory(result, category.Label, passengers, false);
                return;
            }

            Dialog_ShuttleCargoLoadConfirmV3.Open(
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchCapacityTitle"),
                V3CargoLoadText.Tr(
                    passengers
                        ? "CT_Shuttle_LoadCargo_BatchBoardCategoryDoesNotFit"
                        : "CT_Shuttle_LoadCargo_BatchLoadCategoryDoesNotFit",
                    category.Label),
                V3CargoLoadText.Tr(
                    passengers
                        ? "CT_Shuttle_LoadCargo_BatchBoardCategoryFit"
                        : "CT_Shuttle_LoadCargo_BatchLoadCategoryFit"),
                ShuttleV3DialogButtonKind.Primary,
                delegate
                {
                    this.CompleteCategory(
                        this.selectionService.FitToCapacity(category.Targets),
                        category.Label,
                        passengers,
                        true);
                });
        }

        private void CompleteCategory(
            V3CargoLoadBatchResult result,
            string categoryLabel,
            bool passengers,
            bool capacityLimited)
        {
            if (result == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            if (!result.Changed)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchNoChanges"));
                return;
            }

            if (this.onSelectionChanged != null)
            {
                this.onSelectionChanged();
            }

            if (capacityLimited || result.LimitedEntryCount > 0)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    V3CargoLoadText.Tr(
                        passengers
                            ? "CT_Shuttle_LoadCargo_BatchBoardCategoryLimited"
                            : "CT_Shuttle_LoadCargo_BatchLoadCategoryLimited",
                        categoryLabel,
                        result.ChangedEntryCount,
                        result.LimitedEntryCount));
                return;
            }

            ShuttleUICommandFeedback.ShowNeutral(
                V3CargoLoadText.Tr(
                    passengers
                        ? "CT_Shuttle_LoadCargo_BatchBoardCategoryApplied"
                        : "CT_Shuttle_LoadCargo_BatchLoadCategoryApplied",
                    categoryLabel,
                    result.ChangedEntryCount));
        }

        private static string FormatAvailableCount(long count)
        {
            return count >= long.MaxValue
                ? int.MaxValue + "+"
                : count.ToString("N0");
        }

        private void Complete(V3CargoLoadBatchResult result)
        {
            if (result == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            if (!result.Success)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchAllDoesNotFit"));
                return;
            }

            if (!result.Changed)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchNoChanges"));
                return;
            }

            if (this.onSelectionChanged != null)
            {
                this.onSelectionChanged();
            }

            if (result.LimitedEntryCount > 0)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    V3CargoLoadText.Tr(
                        "CT_Shuttle_LoadCargo_BatchAppliedLimited",
                        result.ChangedEntryCount,
                        result.LimitedEntryCount));
                return;
            }

            ShuttleUICommandFeedback.ShowNeutral(
                V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_BatchApplied",
                    result.ChangedEntryCount));
        }
    }
}

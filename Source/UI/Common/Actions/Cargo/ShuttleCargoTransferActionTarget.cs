namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal enum ShuttleCargoTransferDirection
    {
        Unknown,
        LoadedToRefrigerated,
        RefrigeratedToLoaded
    }

    internal sealed class ShuttleCargoTransferActionTarget
    {
        internal ShuttleCargoTransferDisplayTarget Display;
        internal ShuttleCargoTransferCommandTarget Command;
        internal ShuttleCargoTransferValidationContext Validation;
    }

    internal sealed class ShuttleCargoTransferDisplayTarget
    {
        internal ShuttleCargoTransferDirection Direction;
        internal string StackLabel;
        internal string SourceLabel;
        internal string DestinationLabel;
        internal int AvailableCount;
        internal int SelectedCount;
        internal float StackMassKg;
        internal float DestinationUsedMassKg;
        internal float DestinationCapacityKg;
    }

    internal sealed class ShuttleCargoTransferCommandTarget
    {
        internal ShuttleCargoTransferDirection Direction;
        internal int Count;
        internal int SourceAvailableCount;
        internal int SourceTransporterIndex = -1;
        internal int SourceLoadedIndex = -1;
        internal int SourceThingIDNumber;
        internal string SourceDefName;
        internal string SourceModuleInstanceID;
        internal int SourceColdIndex = -1;
        internal string DestinationModuleInstanceID;
    }

    internal sealed class ShuttleCargoTransferValidationContext
    {
        internal ShuttleCargoStackActionTarget Stack;
        internal ShuttleCargoPageActionContext PageContext;
        internal ShuttleCargoBayActionTarget DestinationBay;
    }

    internal static class ShuttleCargoTransferTargetBuilder
    {
        internal static ShuttleCargoTransferActionTarget Create(
            ShuttleCargoStackActionTarget stack,
            ShuttleCargoPageActionContext pageContext,
            ShuttleCargoBayActionTarget destinationBay,
            int count)
        {
            ShuttleCargoTransferActionTarget target =
                new ShuttleCargoTransferActionTarget();
            target.Display = BuildDisplay(stack, pageContext, destinationBay, count);
            target.Command = BuildCommand(stack, destinationBay, count);
            target.Validation = BuildValidation(stack, pageContext, destinationBay);
            return target;
        }

        private static ShuttleCargoTransferDisplayTarget BuildDisplay(
            ShuttleCargoStackActionTarget stack,
            ShuttleCargoPageActionContext pageContext,
            ShuttleCargoBayActionTarget destinationBay,
            int count)
        {
            ShuttleCargoTransferDisplayTarget display =
                new ShuttleCargoTransferDisplayTarget();
            display.Direction = GetDirection(stack);
            display.StackLabel = stack != null ? stack.Label : null;
            display.SourceLabel = null;
            display.DestinationLabel = GetDestinationLabel(display.Direction, destinationBay);
            display.AvailableCount = stack != null ? stack.StackCount : 0;
            display.SelectedCount = count;
            display.StackMassKg = stack != null ? stack.MassKg : 0f;
            ApplyDestinationMass(display, pageContext, destinationBay);
            return display;
        }

        private static ShuttleCargoTransferCommandTarget BuildCommand(
            ShuttleCargoStackActionTarget stack,
            ShuttleCargoBayActionTarget destinationBay,
            int count)
        {
            ShuttleCargoTransferCommandTarget command =
                new ShuttleCargoTransferCommandTarget();
            command.Direction = GetDirection(stack);
            command.Count = count;

            if (stack != null)
            {
                command.SourceAvailableCount = stack.StackCount;
                command.SourceTransporterIndex = stack.TransporterIndex;
                command.SourceLoadedIndex = stack.LoadedIndex;
                command.SourceThingIDNumber = stack.ThingIDNumber;
                command.SourceDefName = stack.DefName;
                command.SourceModuleInstanceID = stack.ModuleInstanceID;
                command.SourceColdIndex = stack.ColdIndex;
            }

            command.DestinationModuleInstanceID =
                destinationBay != null ? destinationBay.ModuleInstanceID : null;
            return command;
        }

        private static ShuttleCargoTransferValidationContext BuildValidation(
            ShuttleCargoStackActionTarget stack,
            ShuttleCargoPageActionContext pageContext,
            ShuttleCargoBayActionTarget destinationBay)
        {
            ShuttleCargoTransferValidationContext validation =
                new ShuttleCargoTransferValidationContext();
            validation.Stack = stack;
            validation.PageContext = pageContext;
            validation.DestinationBay = destinationBay;
            return validation;
        }

        private static ShuttleCargoTransferDirection GetDirection(
            ShuttleCargoStackActionTarget stack)
        {
            if (stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                return ShuttleCargoTransferDirection.LoadedToRefrigerated;
            }

            if (stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                return ShuttleCargoTransferDirection.RefrigeratedToLoaded;
            }

            return ShuttleCargoTransferDirection.Unknown;
        }

        private static string GetDestinationLabel(
            ShuttleCargoTransferDirection direction,
            ShuttleCargoBayActionTarget destinationBay)
        {
            if (direction == ShuttleCargoTransferDirection.LoadedToRefrigerated)
            {
                return destinationBay != null ? destinationBay.Label : null;
            }

            if (direction == ShuttleCargoTransferDirection.RefrigeratedToLoaded)
            {
                return null;
            }

            return null;
        }

        private static void ApplyDestinationMass(
            ShuttleCargoTransferDisplayTarget display,
            ShuttleCargoPageActionContext pageContext,
            ShuttleCargoBayActionTarget destinationBay)
        {
            if (display == null)
            {
                return;
            }

            if (display.Direction == ShuttleCargoTransferDirection.LoadedToRefrigerated)
            {
                display.DestinationUsedMassKg =
                    destinationBay != null ? MaxZero(destinationBay.UsedMassKg) : 0f;
                display.DestinationCapacityKg =
                    destinationBay != null ? MaxZero(destinationBay.CapacityKg) : 0f;
                return;
            }

            if (display.Direction == ShuttleCargoTransferDirection.RefrigeratedToLoaded)
            {
                display.DestinationUsedMassKg = SumNormalBayUsedMassKg(pageContext);
                display.DestinationCapacityKg = SumNormalBayCapacityKg(pageContext);
            }
        }

        private static float SumNormalBayUsedMassKg(
            ShuttleCargoPageActionContext pageContext)
        {
            float total = 0f;
            for (int i = 0; pageContext != null &&
                pageContext.Bays != null &&
                i < pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = pageContext.Bays[i];
                if (bay != null && !bay.IsRefrigerated)
                {
                    total += MaxZero(bay.UsedMassKg);
                }
            }

            return total;
        }

        private static float SumNormalBayCapacityKg(
            ShuttleCargoPageActionContext pageContext)
        {
            float total = 0f;
            for (int i = 0; pageContext != null &&
                pageContext.Bays != null &&
                i < pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = pageContext.Bays[i];
                if (bay != null && !bay.IsRefrigerated)
                {
                    total += MaxZero(bay.CapacityKg);
                }
            }

            return total;
        }

        private static float MaxZero(float value)
        {
            return value > 0f ? value : 0f;
        }
    }
}

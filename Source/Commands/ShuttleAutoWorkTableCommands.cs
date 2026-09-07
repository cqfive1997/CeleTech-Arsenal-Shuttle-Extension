using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class SetAutoWorkTableRecipeCommand : IShuttleCommand
    {
        public const string ID = "set-auto-worktable-recipe";

        public SetAutoWorkTableRecipeCommand(
            string moduleInstanceID,
            string sourceBenchDefName,
            string recipeDefName)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.SourceBenchDefName = sourceBenchDefName;
            this.RecipeDefName = recipeDefName;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public string SourceBenchDefName { get; private set; }
        public string RecipeDefName { get; private set; }
    }

    public sealed class ClearAutoWorkTableRecipeCommand : IShuttleCommand
    {
        public const string ID = "clear-auto-worktable-recipe";

        public ClearAutoWorkTableRecipeCommand(string moduleInstanceID)
        {
            this.ModuleInstanceID = moduleInstanceID;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
    }

    public sealed class SetAutoWorkTableProductionPolicyCommand : IShuttleCommand
    {
        public const string ID = "set-auto-worktable-production-policy";

        public SetAutoWorkTableProductionPolicyCommand(
            string moduleInstanceID,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Mode = mode;
            this.RepeatCount = repeatCount;
            this.TargetCount = targetCount;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public AutoWorkTableProductionMode Mode { get; private set; }
        public int RepeatCount { get; private set; }
        public int TargetCount { get; private set; }
    }

    public sealed class SetAutoWorkTablePausedCommand : IShuttleCommand
    {
        public const string ID = "set-auto-worktable-paused";

        public SetAutoWorkTablePausedCommand(string moduleInstanceID, bool paused)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Paused = paused;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public bool Paused { get; private set; }
    }

    public sealed class AddAutoWorkTableOrderCommand : IShuttleCommand
    {
        public const string ID = "add-auto-worktable-order";

        public AddAutoWorkTableOrderCommand(
            string moduleInstanceID,
            string sourceBenchDefName,
            string recipeDefName)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.SourceBenchDefName = sourceBenchDefName;
            this.RecipeDefName = recipeDefName;
        }

        public string CommandID { get { return ID; } }
        public string ModuleInstanceID { get; private set; }
        public string SourceBenchDefName { get; private set; }
        public string RecipeDefName { get; private set; }
    }

    public sealed class RemoveAutoWorkTableOrderCommand : IShuttleCommand
    {
        public const string ID = "remove-auto-worktable-order";

        public RemoveAutoWorkTableOrderCommand(string moduleInstanceID, int orderId)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.OrderId = orderId;
        }

        public string CommandID { get { return ID; } }
        public string ModuleInstanceID { get; private set; }
        public int OrderId { get; private set; }
    }

    public sealed class MoveAutoWorkTableOrderCommand : IShuttleCommand
    {
        public const string ID = "move-auto-worktable-order";

        public MoveAutoWorkTableOrderCommand(string moduleInstanceID, int orderId, int direction)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.OrderId = orderId;
            this.Direction = direction;
        }

        public string CommandID { get { return ID; } }
        public string ModuleInstanceID { get; private set; }
        public int OrderId { get; private set; }
        public int Direction { get; private set; }
    }

    public sealed class SetAutoWorkTableOrderSuspendedCommand : IShuttleCommand
    {
        public const string ID = "set-auto-worktable-order-suspended";

        public SetAutoWorkTableOrderSuspendedCommand(
            string moduleInstanceID,
            int orderId,
            bool suspended)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.OrderId = orderId;
            this.Suspended = suspended;
        }

        public string CommandID { get { return ID; } }
        public string ModuleInstanceID { get; private set; }
        public int OrderId { get; private set; }
        public bool Suspended { get; private set; }
    }

    public sealed class SetAutoWorkTableOrderPolicyCommand : IShuttleCommand
    {
        public const string ID = "set-auto-worktable-order-policy";

        public SetAutoWorkTableOrderPolicyCommand(
            string moduleInstanceID,
            int orderId,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.OrderId = orderId;
            this.Mode = mode;
            this.RepeatCount = repeatCount;
            this.TargetCount = targetCount;
        }

        public string CommandID { get { return ID; } }
        public string ModuleInstanceID { get; private set; }
        public int OrderId { get; private set; }
        public AutoWorkTableProductionMode Mode { get; private set; }
        public int RepeatCount { get; private set; }
        public int TargetCount { get; private set; }
    }

    public sealed class SetAutoWorkTableOrderIngredientFilterCommand : IShuttleCommand
    {
        public const string ID = "set-auto-worktable-order-ingredient-filter";

        public SetAutoWorkTableOrderIngredientFilterCommand(
            string moduleInstanceID,
            int orderId,
            ThingFilter ingredientFilter,
            bool useRecipeDefault)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.OrderId = orderId;
            this.IngredientFilter = AutoWorkTableIngredientFilterUtility.CopyOrNull(
                ingredientFilter);
            this.UseRecipeDefault = useRecipeDefault;
        }

        public string CommandID { get { return ID; } }
        public string ModuleInstanceID { get; private set; }
        public int OrderId { get; private set; }
        public ThingFilter IngredientFilter { get; private set; }
        public bool UseRecipeDefault { get; private set; }
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Requests installation of a segment def into one materialized shuttle-level slot.
    /// The UI supplies stable IDs only; assembly validation stays in the controller path.
    /// </summary>
    public sealed class InstallSegmentCommand : IShuttleCommand
    {
        public const string ID = "install-segment";

        public InstallSegmentCommand(string segmentSlotID, string segmentDefName)
        {
            this.SegmentSlotID = segmentSlotID;
            this.SegmentDefName = segmentDefName;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentSlotID { get; private set; }
        public string SegmentDefName { get; private set; }
    }

    /// <summary>
    /// Starts a work-order deconstruction flow for the segment mounted in one shuttle-level slot.
    /// The handler routes this through ShuttleModuleRemovalService; it must not directly mutate
    /// AssemblyState.
    /// </summary>
    public sealed class StartSegmentRemovalCommand : IShuttleCommand
    {
        public const string ID = "start-segment-removal";

        public StartSegmentRemovalCommand(string segmentSlotID)
        {
            this.SegmentSlotID = segmentSlotID;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentSlotID { get; private set; }
    }

    public sealed class ReplaceSegmentCommand : IShuttleCommand
    {
        public const string ID = "replace-segment";

        public ReplaceSegmentCommand(string segmentSlotID, string segmentDefName)
        {
            this.SegmentSlotID = segmentSlotID;
            this.SegmentDefName = segmentDefName;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentSlotID { get; private set; }
        public string SegmentDefName { get; private set; }
    }

    /// <summary>
    /// Requests installation of a module def into one concrete segment-owned module slot.
    /// </summary>
    public sealed class InstallModuleCommand : IShuttleCommand
    {
        public const string ID = "install-module";

        public InstallModuleCommand(
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName = null)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.ModuleSlotID = moduleSlotID;
            this.ModuleDefName = moduleDefName;
            this.SelectedStuffDefName = selectedStuffDefName;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentInstanceID { get; private set; }
        public string ModuleSlotID { get; private set; }
        public string ModuleDefName { get; private set; }
        public string SelectedStuffDefName { get; private set; }
    }

    /// <summary>
    /// Requests replacement of the module occupying one concrete segment-owned module slot.
    /// Required slots may use this path because they never become empty from the player's
    /// perspective; direct removal remains blocked for those slots.
    /// </summary>
    public sealed class ReplaceModuleCommand : IShuttleCommand
    {
        public const string ID = "replace-module";

        public ReplaceModuleCommand(
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName = null)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.ModuleSlotID = moduleSlotID;
            this.ModuleDefName = moduleDefName;
            this.SelectedStuffDefName = selectedStuffDefName;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentInstanceID { get; private set; }
        public string ModuleSlotID { get; private set; }
        public string ModuleDefName { get; private set; }
        public string SelectedStuffDefName { get; private set; }
    }

    public sealed class StartModuleRemovalCommand : IShuttleCommand
    {
        public const string ID = "start-module-removal";

        public StartModuleRemovalCommand(
            string segmentInstanceID,
            string moduleSlotID,
            string moduleInstanceID)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.ModuleSlotID = moduleSlotID;
            this.ModuleInstanceID = moduleInstanceID;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentInstanceID { get; private set; }
        public string ModuleSlotID { get; private set; }
        public string ModuleInstanceID { get; private set; }
    }

    public sealed class CancelModuleRemovalCommand : IShuttleCommand
    {
        public const string ID = "cancel-module-removal";

        public CancelModuleRemovalCommand(string moduleInstanceID)
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

    public sealed class AssignModuleRemovalWorkerCommand : IShuttleCommand
    {
        public const string ID = "assign-module-removal-worker";

        public AssignModuleRemovalWorkerCommand(string moduleInstanceID)
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

    public sealed class CancelSegmentRemovalCommand : IShuttleCommand
    {
        public const string ID = "cancel-segment-removal";

        public CancelSegmentRemovalCommand(string segmentSlotID)
        {
            this.SegmentSlotID = segmentSlotID;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentSlotID { get; private set; }
    }

    public sealed class AssignSegmentRemovalWorkerCommand : IShuttleCommand
    {
        public const string ID = "assign-segment-removal-worker";

        public AssignSegmentRemovalWorkerCommand(string segmentSlotID)
        {
            this.SegmentSlotID = segmentSlotID;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentSlotID { get; private set; }
    }

    /// <summary>
    /// Requests enabling or disabling one installed module instance through the assembly command
    /// boundary. The handler revalidates the current slot/module relationship before mutation.
    /// </summary>
    public sealed class SetModuleEnabledCommand : IShuttleCommand
    {
        public const string ID = "set-module-enabled";

        public SetModuleEnabledCommand(
            string segmentInstanceID,
            string moduleSlotID,
            string moduleInstanceID,
            bool enabled)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.ModuleSlotID = moduleSlotID;
            this.ModuleInstanceID = moduleInstanceID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentInstanceID { get; private set; }
        public string ModuleSlotID { get; private set; }
        public string ModuleInstanceID { get; private set; }
        public bool Enabled { get; private set; }
    }

    /// <summary>
    /// Requests a first-pass segment enable/disable operation. No segment runtime flag is
    /// persisted; the handler applies the target state to eligible installed modules.
    /// </summary>
    public sealed class SetSegmentModulesEnabledCommand : IShuttleCommand
    {
        public const string ID = "set-segment-modules-enabled";

        public SetSegmentModulesEnabledCommand(string segmentSlotID, bool enabled)
        {
            this.SegmentSlotID = segmentSlotID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string SegmentSlotID { get; private set; }
        public bool Enabled { get; private set; }
    }

    public sealed class BeginSegmentConstructionCommand : IShuttleCommand
    {
        public const string ID = "begin-segment-construction";

        public BeginSegmentConstructionCommand(string segmentSlotID, string segmentDefName)
        {
            this.SegmentSlotID = segmentSlotID;
            this.SegmentDefName = segmentDefName;
        }

        public string CommandID { get { return ID; } }
        public string SegmentSlotID { get; private set; }
        public string SegmentDefName { get; private set; }
    }

    public sealed class BeginSegmentReplacementConstructionCommand : IShuttleCommand
    {
        public const string ID = "begin-segment-replacement-construction";

        public BeginSegmentReplacementConstructionCommand(string segmentSlotID, string segmentDefName)
        {
            this.SegmentSlotID = segmentSlotID;
            this.SegmentDefName = segmentDefName;
        }

        public string CommandID { get { return ID; } }
        public string SegmentSlotID { get; private set; }
        public string SegmentDefName { get; private set; }
    }

    public sealed class BeginModuleConstructionCommand : IShuttleCommand
    {
        public const string ID = "begin-module-construction";

        public BeginModuleConstructionCommand(
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName = null)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.ModuleSlotID = moduleSlotID;
            this.ModuleDefName = moduleDefName;
            this.SelectedStuffDefName = selectedStuffDefName;
        }

        public string CommandID { get { return ID; } }
        public string SegmentInstanceID { get; private set; }
        public string ModuleSlotID { get; private set; }
        public string ModuleDefName { get; private set; }
        public string SelectedStuffDefName { get; private set; }
    }

    public sealed class BeginModuleReplacementConstructionCommand : IShuttleCommand
    {
        public const string ID = "begin-module-replacement-construction";

        public BeginModuleReplacementConstructionCommand(
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName = null)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.ModuleSlotID = moduleSlotID;
            this.ModuleDefName = moduleDefName;
            this.SelectedStuffDefName = selectedStuffDefName;
        }

        public string CommandID { get { return ID; } }
        public string SegmentInstanceID { get; private set; }
        public string ModuleSlotID { get; private set; }
        public string ModuleDefName { get; private set; }
        public string SelectedStuffDefName { get; private set; }
    }

    public sealed class CancelAssemblyConstructionCommand : IShuttleCommand
    {
        public const string ID = "cancel-assembly-construction";

        public CancelAssemblyConstructionCommand(string orderID)
        {
            this.OrderID = orderID;
        }

        public string CommandID { get { return ID; } }
        public string OrderID { get; private set; }
    }

    public sealed class DebugSetAssemblyConstructionProgressCommand : IShuttleCommand
    {
        public const string ID = "debug-set-assembly-construction-progress";

        public DebugSetAssemblyConstructionProgressCommand(string orderID, float progress01)
        {
            this.OrderID = orderID;
            this.Progress01 = progress01;
        }

        public string CommandID { get { return ID; } }
        public string OrderID { get; private set; }
        public float Progress01 { get; private set; }
    }

    public sealed class DebugCompleteAssemblyConstructionCommand : IShuttleCommand
    {
        public const string ID = "debug-complete-assembly-construction";

        public DebugCompleteAssemblyConstructionCommand(string orderID)
        {
            this.OrderID = orderID;
        }

        public string CommandID { get { return ID; } }
        public string OrderID { get; private set; }
    }

    public sealed class DebugFillAssemblyConstructionMaterialsCommand : IShuttleCommand
    {
        public const string ID = "debug-fill-assembly-construction-materials";

        public DebugFillAssemblyConstructionMaterialsCommand(string orderID)
        {
            this.OrderID = orderID;
        }

        public string CommandID { get { return ID; } }
        public string OrderID { get; private set; }
    }
}

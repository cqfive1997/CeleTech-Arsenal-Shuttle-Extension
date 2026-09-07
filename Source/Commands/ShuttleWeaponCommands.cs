using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Sets a runtime-only forced target for one installed weapon module.
    /// </summary>
    public sealed class SetWeaponForcedTargetCommand : IShuttleCommand
    {
        public const string ID = "set-weapon-forced-target";

        public SetWeaponForcedTargetCommand(string moduleInstanceID, LocalTargetInfo target)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Target = target;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public LocalTargetInfo Target { get; private set; }
    }

    /// <summary>
    /// Clears the runtime-only forced target for one installed weapon module.
    /// </summary>
    public sealed class ClearWeaponForcedTargetCommand : IShuttleCommand
    {
        public const string ID = "clear-weapon-forced-target";

        public ClearWeaponForcedTargetCommand(string moduleInstanceID)
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

    /// <summary>
    /// Updates the runtime-only hold-fire setting for one installed weapon module.
    /// </summary>
    public sealed class SetWeaponHoldFireCommand : IShuttleCommand
    {
        public const string ID = "set-weapon-hold-fire";

        public SetWeaponHoldFireCommand(string moduleInstanceID, bool holdFire)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.HoldFire = holdFire;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public bool HoldFire { get; private set; }
    }

    internal sealed class SetWeaponFireControlLinkedCommand : IShuttleCommand
    {
        public const string ID = "set-weapon-fire-control-linked";

        internal SetWeaponFireControlLinkedCommand(string moduleInstanceID, bool linked)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Linked = linked;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal bool Linked { get; private set; }
    }

    internal sealed class SetWeaponFireControlModeCommand : IShuttleCommand
    {
        public const string ID = "set-weapon-fire-control-mode";

        internal SetWeaponFireControlModeCommand(
            string moduleInstanceID,
            ShuttleWeaponFireControlMode mode)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Mode = mode;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal ShuttleWeaponFireControlMode Mode { get; private set; }
    }

    internal sealed class SetWeaponTargetPriorityCommand : IShuttleCommand
    {
        public const string ID = "set-weapon-target-priority";

        internal SetWeaponTargetPriorityCommand(
            string moduleInstanceID,
            ShuttleWeaponTargetPriority priority)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Priority = priority;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal ShuttleWeaponTargetPriority Priority { get; private set; }
    }

    internal sealed class SetWeaponAutoFireCommand : IShuttleCommand
    {
        public const string ID = "set-weapon-auto-fire";

        internal SetWeaponAutoFireCommand(string moduleInstanceID, bool enabled)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal bool Enabled { get; private set; }
    }

    internal sealed class SetShuttleWeaponAmmoCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-weapon-ammo";

        internal SetShuttleWeaponAmmoCommand(string moduleInstanceID, string ammoDefName)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.AmmoDefName = ammoDefName;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal string AmmoDefName { get; private set; }
    }

    internal sealed class ReloadShuttleWeaponCommand : IShuttleCommand
    {
        public const string ID = "reload-shuttle-weapon";

        internal ReloadShuttleWeaponCommand(string moduleInstanceID)
        {
            this.ModuleInstanceID = moduleInstanceID;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }
    }

    internal sealed class CancelShuttleWeaponReloadCommand : IShuttleCommand
    {
        public const string ID = "cancel-shuttle-weapon-reload";

        internal CancelShuttleWeaponReloadCommand(string moduleInstanceID)
        {
            this.ModuleInstanceID = moduleInstanceID;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }
    }

    internal sealed class SetShuttleWeaponAutoReloadCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-weapon-auto-reload";

        internal SetShuttleWeaponAutoReloadCommand(string moduleInstanceID, bool enabled)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal bool Enabled { get; private set; }
    }

    internal sealed class SetShuttleWeaponLogisticsAutoFeedCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-weapon-logistics-auto-feed";

        internal SetShuttleWeaponLogisticsAutoFeedCommand(string moduleInstanceID, bool enabled)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal bool Enabled { get; private set; }
    }

    internal sealed class SetShuttleWeaponManualReloadAllowedCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-weapon-manual-reload-allowed";

        internal SetShuttleWeaponManualReloadAllowedCommand(string moduleInstanceID, bool enabled)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get { return ID; }
        }

        internal string ModuleInstanceID { get; private set; }

        internal bool Enabled { get; private set; }
    }
}

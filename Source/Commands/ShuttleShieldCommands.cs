namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public interface IShuttleShieldCommandPort
    {
        ShuttleCommandResult SetShieldRadius(string moduleInstanceID, float selectedRadius);
        ShuttleCommandResult SetSurfaceShieldRechargeSpeed(float rechargeSpeedMultiplier);
    }

    /// <summary>
    /// Requests a runtime setting change for one installed shield module.
    /// This command does not affect profile-derived shuttle capability.
    /// </summary>
    public sealed class SetShuttleShieldRadiusCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-shield-radius";

        public SetShuttleShieldRadiusCommand(string moduleInstanceID, float selectedRadius)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.SelectedRadius = selectedRadius;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public float SelectedRadius { get; private set; }
    }

    /// <summary>
    /// Requests a runtime recharge-speed setting change for the active surface shield.
    /// This is player configuration; it does not alter profile capacity or damage rules.
    /// </summary>
    public sealed class SetShuttleSurfaceShieldRechargeSpeedCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-surface-shield-recharge-speed";

        public SetShuttleSurfaceShieldRechargeSpeedCommand(float rechargeSpeedMultiplier)
        {
            this.RechargeSpeedMultiplier = rechargeSpeedMultiplier;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public float RechargeSpeedMultiplier { get; private set; }
    }
}

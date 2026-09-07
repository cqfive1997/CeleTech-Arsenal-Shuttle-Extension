namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Result returned by the controller command boundary.
    /// </summary>
    public sealed class ShuttleCommandResult
    {
        private ShuttleCommandResult(bool success, string message, bool profileChanged)
        {
            this.Success = success;
            this.Message = message;
            this.ProfileChanged = profileChanged;
        }

        public bool Success
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        public bool ProfileChanged
        {
            get;
            private set;
        }

        public static ShuttleCommandResult Succeeded(string message)
        {
            return new ShuttleCommandResult(true, message, false);
        }

        public static ShuttleCommandResult Succeeded(string message, bool profileChanged)
        {
            return new ShuttleCommandResult(true, message, profileChanged);
        }

        public static ShuttleCommandResult Failed(string message)
        {
            return new ShuttleCommandResult(false, message, false);
        }
    }
}

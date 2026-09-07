namespace CeleTech.ShuttleExtension.ModularShuttle.API.Commands
{
    /// <summary>
    /// Result returned by a third-party external command handler.
    /// The host maps success and message into the main command result flow.
    /// </summary>
    public sealed class ShuttleExternalCommandResult
    {
        private ShuttleExternalCommandResult(bool success, string message)
        {
            this.Success = success;
            this.Message = message;
        }

        public bool Success { get; private set; }

        public string Message { get; private set; }

        public static ShuttleExternalCommandResult Succeeded(string message)
        {
            return new ShuttleExternalCommandResult(true, message);
        }

        public static ShuttleExternalCommandResult Failed(string message)
        {
            return new ShuttleExternalCommandResult(false, message);
        }
    }
}

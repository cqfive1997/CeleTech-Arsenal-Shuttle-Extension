using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Small command router owned by ShuttleController. It keeps controller.Execute thin while
    /// preserving one authoritative write boundary for UI commands.
    /// </summary>
    internal sealed class ShuttleCommandDispatcher
    {
        private readonly List<IShuttleCommandHandler> handlers = new List<IShuttleCommandHandler>();

        public ShuttleCommandDispatcher(IEnumerable<IShuttleCommandHandler> handlers)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (IShuttleCommandHandler handler in handlers)
            {
                if (handler != null)
                {
                    this.handlers.Add(handler);
                }
            }
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_NoneProvided".Translate().ToString());
            }

            for (int i = 0; i < this.handlers.Count; i++)
            {
                IShuttleCommandHandler handler = this.handlers[i];
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    if (handler.CanHandle(command))
                    {
                        return handler.Execute(command, context);
                    }
                }
                catch (Exception exception)
                {
                    string commandID = this.GetCommandDebugID(command);
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] Shuttle command handler " +
                        handler.GetType().FullName +
                        " failed while executing command '" + commandID +
                        "'. Exception: " + exception,
                        this.MakeCommandExceptionHash(commandID, handler, exception));
                    return ShuttleCommandResult.Failed(
                        "Shuttle command failed: " + commandID + ".");
                }
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }

        private string GetCommandDebugID(IShuttleCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.CommandID))
            {
                return "unknown";
            }

            return command.CommandID;
        }

        private int MakeCommandExceptionHash(
            string commandID,
            IShuttleCommandHandler handler,
            Exception exception)
        {
            unchecked
            {
                string handlerName = handler != null ? handler.GetType().FullName : null;
                string exceptionName = exception != null ? exception.GetType().FullName : null;
                int hash = 31;
                hash = (hash * 37) + (commandID != null ? commandID.GetHashCode() : 0);
                hash = (hash * 37) + (handlerName != null ? handlerName.GetHashCode() : 0);
                hash = (hash * 37) + (exceptionName != null ? exceptionName.GetHashCode() : 0);
                hash = (hash * 37) + (exception != null && exception.Message != null
                    ? exception.Message.GetHashCode()
                    : 0);
                return hash;
            }
        }
    }
}

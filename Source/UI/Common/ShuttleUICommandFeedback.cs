using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUICommandFeedback
    {
        internal static bool ShowResult(ShuttleCommandResult result)
        {
            return ShowResult(result, MessageTypeDefOf.PositiveEvent);
        }

        internal static bool ShowResult(
            ShuttleCommandResult result,
            MessageTypeDef successMessageType)
        {
            if (result == null)
            {
                ShowReject(ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return false;
            }

            if (!string.IsNullOrEmpty(result.Message))
            {
                Messages.Message(
                    result.Message,
                    result.Success ? successMessageType : MessageTypeDefOf.RejectInput,
                    false);
            }

            return result.Success;
        }

        internal static bool ShowReject(string message)
        {
            return ShowReject(message, true);
        }

        internal static bool ShowReject(string message, bool fallbackWhenEmpty)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (!fallbackWhenEmpty)
                {
                    return false;
                }

                message = ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        internal static void ShowSuccess(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Messages.Message(message, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        internal static void ShowFailure(string message)
        {
            ShowReject(message);
        }

        internal static void ShowNeutral(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Messages.Message(message, MessageTypeDefOf.NeutralEvent, false);
            }
        }
    }
}

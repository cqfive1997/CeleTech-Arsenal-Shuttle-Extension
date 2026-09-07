using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleReadModelInternalTokenSanitizer
    {
        internal string Sanitize(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            message = this.ReplaceIndexedInternalToken(
                message,
                "segment.slot.",
                "CT_Shuttle_InfoPanel_RequiredSegmentSlotIndexed");
            message = this.ReplaceIndexedInternalToken(
                message,
                "module.slot.",
                "CT_Shuttle_InfoPanel_RequiredModuleSlotIndexed");
            return message;
        }

        private string ReplaceIndexedInternalToken(
            string message,
            string prefix,
            string translationKey)
        {
            int start = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            while (start >= 0)
            {
                int digitStart = start + prefix.Length;
                int digitEnd = digitStart;
                while (digitEnd < message.Length && char.IsDigit(message[digitEnd]))
                {
                    digitEnd++;
                }

                if (digitEnd <= digitStart)
                {
                    start = message.IndexOf(
                        prefix,
                        start + prefix.Length,
                        StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                string raw = message.Substring(start, digitEnd - start);
                string numberText = message.Substring(digitStart, digitEnd - digitStart);
                int index;
                string replacement = int.TryParse(numberText, out index)
                    ? translationKey.Translate(index + 1).ToString()
                    : translationKey.Translate("?").ToString();
                message = message.Replace(raw, replacement);
                start = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return message;
        }
    }
}

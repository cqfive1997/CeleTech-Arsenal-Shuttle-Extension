using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Research
{
    internal sealed class ShuttleControlResearchInvalidationService
    {
        private readonly Func<int> getResearchFingerprint;
        private readonly Action markDirty;
        private int lastFingerprint = int.MinValue;

        internal ShuttleControlResearchInvalidationService(
            Func<int> getResearchFingerprint,
            Action markDirty)
        {
            this.getResearchFingerprint = getResearchFingerprint;
            this.markDirty = markDirty;
        }

        internal void CheckResearchInvalidation()
        {
            if (this.getResearchFingerprint == null)
            {
                return;
            }

            int currentFingerprint;
            try
            {
                currentFingerprint = this.getResearchFingerprint();
            }
            catch (Exception exception)
            {
                LogOnce(
                    "ShuttleControlResearchInvalidationService.CheckResearchInvalidation",
                    exception);
                return;
            }

            if (this.lastFingerprint == int.MinValue)
            {
                this.lastFingerprint = currentFingerprint;
                return;
            }

            if (currentFingerprint == this.lastFingerprint)
            {
                return;
            }

            this.lastFingerprint = currentFingerprint;
            if (this.markDirty != null)
            {
                this.markDirty();
            }
        }

        private static void LogOnce(string actionName, Exception exception)
        {
            if (!Prefs.DevMode || exception == null)
            {
                return;
            }

            Log.WarningOnce(
                "[CeleTech Shuttle] DevMode Shuttle Control V3 action '" +
                (actionName ?? "unknown") +
                "' failed. Exception: " + exception,
                MakeHash(actionName, exception));
        }

        private static int MakeHash(string actionName, Exception exception)
        {
            unchecked
            {
                int hash = 41;
                hash = (hash * 37) + (actionName != null ? actionName.GetHashCode() : 0);
                hash = (hash * 37) + (exception != null && exception.GetType() != null
                    ? exception.GetType().FullName.GetHashCode()
                    : 0);
                hash = (hash * 37) + (exception != null && exception.Message != null
                    ? exception.Message.GetHashCode()
                    : 0);
                return hash;
            }
        }
    }
}

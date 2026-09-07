using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Contains third-party callback exceptions at the synchronous cargo transaction boundary.
    /// </summary>
    internal static class ShuttleWeaponCargoCommitCallbackInvoker
    {
        internal static bool TryCommit(
            Func<int, bool> callback,
            int count,
            out string failure)
        {
            failure = null;
            try
            {
                if (callback(count))
                {
                    return true;
                }

                failure = "magazine-commit-rejected";
                return false;
            }
            catch (Exception exception)
            {
                failure = "magazine-commit-threw-" + exception.GetType().Name;
                return false;
            }
        }

        internal static bool TryRollback(
            Func<bool> callback,
            out string failure)
        {
            failure = null;
            try
            {
                if (callback())
                {
                    return true;
                }

                failure = "magazine-rollback-rejected";
                return false;
            }
            catch (Exception exception)
            {
                failure = "magazine-rollback-threw-" + exception.GetType().Name;
                return false;
            }
        }
    }
}

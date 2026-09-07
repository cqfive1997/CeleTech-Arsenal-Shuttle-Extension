namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Chooses whether a manual reload request must remain available to a Pawn instead of being
    /// presented as an unavailable automatic-loader operation.
    /// </summary>
    internal static class ShuttleWeaponReloadExecutorPolicy
    {
        internal static bool ShouldAwaitPawn(
            ShuttleWeaponReloadRequestKind requestKind,
            bool manualReloadAllowed,
            bool automaticLoaderAvailable)
        {
            return requestKind == ShuttleWeaponReloadRequestKind.Manual &&
                manualReloadAllowed &&
                !automaticLoaderAvailable;
        }
    }
}

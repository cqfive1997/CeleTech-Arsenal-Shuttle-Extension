using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    /// <summary>
    /// Classifies Pawns stored in the shared vanilla transporter holder.
    /// Ordinary living colonists are cockpit occupants; other Pawns remain cargo-loaded.
    /// </summary>
    internal static class ShuttleCockpitPresenceClassifier
    {
        internal static bool IsCockpitOccupant(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Dead &&
                pawn.IsColonist;
        }
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleReadModelDisplayNameReplacement
    {
        internal ShuttleReadModelDisplayNameReplacement(string rawID, string displayName)
        {
            this.RawID = rawID;
            this.DisplayName = displayName;
        }

        internal string RawID;
        internal string DisplayName;
    }
}

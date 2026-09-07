using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Profile
{
    /// <summary>
    /// Read-only segment facts exposed to third-party profile contributors.
    /// This view is detached from the live AssemblyState segment instance.
    /// </summary>
    public sealed class ShuttleSegmentContributionView
    {
        public ShuttleSegmentContributionView(
            string segmentInstanceId,
            string segmentDefName,
            string segmentTypeId,
            string label)
        {
            this.SegmentInstanceId = segmentInstanceId;
            this.SegmentDefName = segmentDefName;
            this.SegmentTypeId = segmentTypeId;
            this.Label = label;
        }

        public string SegmentInstanceId { get; private set; }

        public string SegmentDefName { get; private set; }

        public string SegmentTypeId { get; private set; }

        public string Label { get; private set; }

        internal static ShuttleSegmentContributionView From(
            ShuttleSegment segment,
            ShuttleSegmentBaseDef segmentDef)
        {
            return new ShuttleSegmentContributionView(
                segment != null ? segment.SegmentInstanceID : null,
                segment != null ? segment.segmentDefName : (segmentDef != null ? segmentDef.defName : null),
                segmentDef != null ? segmentDef.segmentTypeID : null,
                GetDefLabel(segmentDef));
        }

        private static string GetDefLabel(Verse.Def def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            string label = def.LabelCap.ToString();
            return !string.IsNullOrEmpty(label) ? label : def.defName;
        }
    }
}

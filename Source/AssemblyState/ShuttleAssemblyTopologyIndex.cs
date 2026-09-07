using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleAssemblyTopologyIndex
    {
        // Counts raw IDs and slot references from persisted lists. These maps deliberately
        // preserve duplicates instead of using ShuttleAssemblyState's lookup dictionaries,
        // because those dictionaries overwrite duplicate keys during rebuild.
        internal readonly Dictionary<string, List<string>> SegmentSlotIDReferences =
            new Dictionary<string, List<string>>();

        internal readonly Dictionary<string, List<string>> SegmentIDReferences =
            new Dictionary<string, List<string>>();

        internal readonly Dictionary<string, List<string>> ModuleIDReferences =
            new Dictionary<string, List<string>>();

        internal readonly Dictionary<string, List<string>> SegmentSlotReferences =
            new Dictionary<string, List<string>>();

        internal readonly Dictionary<string, List<string>> ModuleSlotReferences =
            new Dictionary<string, List<string>>();

        internal readonly Dictionary<string, ShuttleSegment> FirstSegmentByID =
            new Dictionary<string, ShuttleSegment>();

        internal readonly Dictionary<string, ShuttleModule> FirstModuleByID =
            new Dictionary<string, ShuttleModule>();
    }
}

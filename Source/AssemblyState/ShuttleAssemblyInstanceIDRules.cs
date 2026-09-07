namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal static class ShuttleAssemblyInstanceIDRules
    {
        internal const int ExhaustionThreshold = int.MaxValue - 1;
        private const string SegmentPrefix = "segment-";
        private const string ModulePrefix = "module-";

        internal static string MakeSegmentInstanceID(int value)
        {
            return SegmentPrefix + value;
        }

        internal static string MakeModuleInstanceID(int value)
        {
            return ModulePrefix + value;
        }

        internal static bool IsValidSegmentInstanceID(string value)
        {
            int parsedValue;
            return TryParseSegmentInstanceNumber(value, out parsedValue) &&
                parsedValue < ExhaustionThreshold;
        }

        internal static bool IsValidModuleInstanceID(string value)
        {
            int parsedValue;
            return TryParseModuleInstanceNumber(value, out parsedValue) &&
                parsedValue < ExhaustionThreshold;
        }

        internal static bool TryParseSegmentInstanceNumber(string value, out int parsedValue)
        {
            return TryParseInstanceNumber(value, SegmentPrefix, out parsedValue);
        }

        internal static bool TryParseModuleInstanceNumber(string value, out int parsedValue)
        {
            return TryParseInstanceNumber(value, ModulePrefix, out parsedValue);
        }

        private static bool TryParseInstanceNumber(string value, string prefix, out int parsedValue)
        {
            parsedValue = 0;
            if (string.IsNullOrEmpty(value) ||
                string.IsNullOrEmpty(prefix) ||
                !value.StartsWith(prefix))
            {
                return false;
            }

            string numericSuffix = value.Substring(prefix.Length);
            return int.TryParse(numericSuffix, out parsedValue) && parsedValue > 0;
        }
    }
}

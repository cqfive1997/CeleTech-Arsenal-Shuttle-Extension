using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    internal static class ShuttleExternalSDKCollections
    {
        internal static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        {
            List<T> copy = new List<T>();
            if (source != null)
            {
                foreach (T item in source)
                {
                    if (item != null)
                    {
                        copy.Add(item);
                    }
                }
            }

            return new ReadOnlyCollection<T>(copy);
        }

        internal static IReadOnlyList<string> CopyStrings(IEnumerable<string> source)
        {
            List<string> copy = new List<string>();
            if (source != null)
            {
                foreach (string item in source)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        copy.Add(item);
                    }
                }
            }

            return new ReadOnlyCollection<string>(copy);
        }
    }
}

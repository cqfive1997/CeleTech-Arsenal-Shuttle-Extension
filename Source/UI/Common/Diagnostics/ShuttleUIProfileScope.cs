using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics
{
    internal struct ShuttleUIProfileScope : IDisposable
    {
        private readonly ShuttleUIProfileSection section;
        private readonly long startTimestamp;

        internal ShuttleUIProfileScope(
            ShuttleUIProfileSection section,
            long startTimestamp)
        {
            this.section = section;
            this.startTimestamp = startTimestamp;
        }

        internal static ShuttleUIProfileScope Inactive
        {
            get
            {
                return new ShuttleUIProfileScope(
                    ShuttleUIProfileSection.Count,
                    0L);
            }
        }

        public void Dispose()
        {
            if (this.startTimestamp <= 0L)
            {
                return;
            }

            ShuttleUIProfiler.RecordElapsedSince(
                this.section,
                this.startTimestamp);
        }
    }
}

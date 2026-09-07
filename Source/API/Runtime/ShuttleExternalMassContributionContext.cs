using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Read-only mass contribution context for third-party external runtimes.
    /// Use this to report dynamic runtime-owned payload such as internal fluids,
    /// charge media, or other stateful contents. It does not expose shuttle internals.
    /// </summary>
    public sealed class ShuttleExternalMassContributionContext
    {
        private readonly List<ShuttleExternalMassContributionEntry> contributions =
            new List<ShuttleExternalMassContributionEntry>();

        internal ShuttleExternalMassContributionContext(
            ShuttleExternalModuleInfo module,
            IShuttleExternalRuntimeStateReader state,
            int ticksGame)
        {
            this.Module = module;
            this.State = state;
            this.TicksGame = ticksGame;
        }

        public ShuttleExternalModuleInfo Module { get; private set; }

        public IShuttleExternalRuntimeStateReader State { get; private set; }

        public int TicksGame { get; private set; }

        internal IReadOnlyList<ShuttleExternalMassContributionEntry> ContributionsForRead
        {
            get
            {
                return this.contributions;
            }
        }

        public void AddMassKg(float amountKg, string labelKey)
        {
            this.AddMassKg(amountKg, labelKey, null);
        }

        public void AddMassKg(float amountKg, string labelKey, string debugLabel)
        {
            if (amountKg <= 0f || float.IsNaN(amountKg) || float.IsInfinity(amountKg))
            {
                return;
            }

            ShuttleExternalMassContributionEntry entry =
                new ShuttleExternalMassContributionEntry();
            entry.MassKg = amountKg;
            entry.LabelKey = labelKey;
            entry.DebugLabel = debugLabel;
            this.contributions.Add(entry);
        }
    }

    internal sealed class ShuttleExternalMassContributionEntry
    {
        internal float MassKg;
        internal string LabelKey;
        internal string DebugLabel;
    }
}

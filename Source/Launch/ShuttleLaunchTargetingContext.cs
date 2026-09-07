using System;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    /// <summary>
    /// Narrow input for the shuttle-owned world targeter.
    /// It exposes only read/command seams needed for hover quotes and final confirm commands.
    /// </summary>
    public sealed class ShuttleLaunchTargetingContext
    {
        public ShuttleLaunchTargetingContext(
            ThingWithComps host,
            IShuttleCommandExecutor commandExecutor,
            Func<ShuttleProfile> getProfileForRead,
            Func<ShuttleRuntimeState> getRuntimeState,
            Func<ShuttleCargoSnapshot> buildCargoSnapshot)
        {
            this.Host = host;
            this.CommandExecutor = commandExecutor;
            this.GetProfileForRead = getProfileForRead;
            this.GetRuntimeState = getRuntimeState;
            this.BuildCargoSnapshot = buildCargoSnapshot;
        }

        public ThingWithComps Host { get; private set; }
        public IShuttleCommandExecutor CommandExecutor { get; private set; }
        public Func<ShuttleProfile> GetProfileForRead { get; private set; }
        public Func<ShuttleRuntimeState> GetRuntimeState { get; private set; }
        public Func<ShuttleCargoSnapshot> BuildCargoSnapshot { get; private set; }
    }
}

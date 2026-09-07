using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal interface IShuttleHolderLaunchTransferParticipant
    {
        string Key { get; }

        int ExportOrder { get; }

        IReadOnlyList<string> ManifestHolderKinds { get; }

        bool NeedsTransfer(ThingWithComps host);

        bool CanUse(
            ThingWithComps host,
            TransportersArrivalAction arrivalAction,
            out string failureReason);

        bool TryExport(
            ThingWithComps host,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason);

        ShuttleHolderLaunchTransferParticipantRollbackResult TryRollback(
            ThingWithComps host,
            List<ShuttleLaunchCargoHandoff> handoffs,
            Map map,
            string context);
    }

    internal sealed class ShuttleHolderLaunchTransferParticipantRollbackResult
    {
        internal ShuttleHolderLaunchTransferParticipantRollbackResult(
            bool rollbackSucceeded,
            bool handoffsClear,
            bool thingsQuarantined,
            string notice)
        {
            this.RollbackSucceeded = rollbackSucceeded;
            this.HandoffsClear = handoffsClear;
            this.ThingsQuarantined = thingsQuarantined;
            this.Notice = notice;
        }

        internal bool RollbackSucceeded { get; private set; }

        internal bool HandoffsClear { get; private set; }

        internal bool ThingsQuarantined { get; private set; }

        internal string Notice { get; private set; }

        internal static ShuttleHolderLaunchTransferParticipantRollbackResult Cleared(
            bool rollbackSucceeded,
            bool thingsQuarantined,
            string notice)
        {
            return new ShuttleHolderLaunchTransferParticipantRollbackResult(
                rollbackSucceeded,
                true,
                thingsQuarantined,
                notice);
        }

        internal static ShuttleHolderLaunchTransferParticipantRollbackResult Blocked(string notice)
        {
            return new ShuttleHolderLaunchTransferParticipantRollbackResult(
                false,
                false,
                false,
                notice);
        }
    }
}

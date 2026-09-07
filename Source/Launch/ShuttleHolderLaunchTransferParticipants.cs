using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleHolderLaunchTransferParticipants
    {
        internal const string DevTestKey = "DevTest";
        internal const string HabitatKey = "Habitat";
        internal const string MedicalBayKey = "MedicalBay";
        internal const string MechChargerKey = "MechCharger";
        internal const string PrisonCellKey = "PrisonCell";

        internal static ShuttleHolderLaunchTransferTransaction CreateTransaction(ThingWithComps host)
        {
            return new ShuttleHolderLaunchTransferTransaction(Collect(host));
        }

        internal static List<IShuttleHolderLaunchTransferParticipant> Collect(ThingWithComps host)
        {
            List<IShuttleHolderLaunchTransferParticipant> participants =
                new List<IShuttleHolderLaunchTransferParticipant>();

            DevTestParticipant devTest = new DevTestParticipant();
            if (devTest.NeedsTransfer(host))
            {
                participants.Add(devTest);
            }

            IShuttleHolderLaunchTransferParticipant habitat = TryCreateHabitatParticipant(host);
            if (habitat != null)
            {
                participants.Add(habitat);
            }

            MedicalBayParticipant medicalBay = new MedicalBayParticipant();
            if (medicalBay.NeedsTransfer(host))
            {
                participants.Add(medicalBay);
            }

            MechChargerParticipant mechCharger = new MechChargerParticipant();
            if (mechCharger.NeedsTransfer(host))
            {
                participants.Add(mechCharger);
            }

            PrisonCellParticipant prisonCell = new PrisonCellParticipant();
            if (prisonCell.NeedsTransfer(host))
            {
                participants.Add(prisonCell);
            }

            participants.Sort((left, right) => left.ExportOrder.CompareTo(right.ExportOrder));
            return participants;
        }

        private static IShuttleHolderLaunchTransferParticipant TryCreateHabitatParticipant(ThingWithComps host)
        {
            if (ShuttleHolderLaunchTransferService.NeedsHabitatMixedLaunchTransfer(host))
            {
                return new HabitatParticipant(HabitatTransferMode.Mixed);
            }

            if (ShuttleHolderLaunchTransferService.NeedsHabitatJoyLaunchTransfer(host))
            {
                return new HabitatParticipant(HabitatTransferMode.Joy);
            }

            if (ShuttleHolderLaunchTransferService.NeedsHabitatLivingLaunchTransfer(host))
            {
                return new HabitatParticipant(HabitatTransferMode.Living);
            }

            return null;
        }

        private enum HabitatTransferMode
        {
            Living,
            Joy,
            Mixed
        }

        private sealed class DevTestParticipant : IShuttleHolderLaunchTransferParticipant
        {
            private static readonly string[] HolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.DevTestHolderKind
            };

            public string Key
            {
                get { return DevTestKey; }
            }

            public int ExportOrder
            {
                get { return 10; }
            }

            public IReadOnlyList<string> ManifestHolderKinds
            {
                get { return HolderKinds; }
            }

            public bool NeedsTransfer(ThingWithComps host)
            {
                if (!Prefs.DevMode)
                {
                    return false;
                }

                CompShuttleHolderLaunchTransferState state = host != null
                    ? host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                    : null;
                return state != null && state.HasDevTestHeldThings;
            }

            public bool CanUse(
                ThingWithComps host,
                TransportersArrivalAction arrivalAction,
                out string failureReason)
            {
                failureReason = null;
                return true;
            }

            public bool TryExport(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                out string failureReason)
            {
                return ShuttleHolderLaunchTransferService.TryExportDevTestHolderForLaunch(
                    host,
                    handoffs,
                    out failureReason);
            }

            public ShuttleHolderLaunchTransferParticipantRollbackResult TryRollback(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                Map map,
                string context)
            {
                string notice;
                bool rollbackSucceeded = ShuttleHolderLaunchTransferService.TryRollbackDevTestExport(
                    host,
                    handoffs,
                    map,
                    out notice);
                return ShuttleHolderLaunchTransferParticipantRollbackResult.Cleared(
                    rollbackSucceeded,
                    false,
                    notice);
            }
        }

        private sealed class HabitatParticipant : IShuttleHolderLaunchTransferParticipant
        {
            private static readonly string[] LivingHolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind,
                ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind,
                ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind
            };

            private static readonly string[] JoyHolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind
            };

            private static readonly string[] MixedHolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind,
                ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind,
                ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind,
                ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind
            };

            private readonly HabitatTransferMode mode;

            internal HabitatParticipant(HabitatTransferMode mode)
            {
                this.mode = mode;
            }

            public string Key
            {
                get
                {
                    return HabitatKey + ":" + this.mode;
                }
            }

            public int ExportOrder
            {
                get { return 20; }
            }

            public IReadOnlyList<string> ManifestHolderKinds
            {
                get
                {
                    if (this.mode == HabitatTransferMode.Mixed)
                    {
                        return MixedHolderKinds;
                    }

                    if (this.mode == HabitatTransferMode.Joy)
                    {
                        return JoyHolderKinds;
                    }

                    return LivingHolderKinds;
                }
            }

            public bool NeedsTransfer(ThingWithComps host)
            {
                if (this.mode == HabitatTransferMode.Mixed)
                {
                    return ShuttleHolderLaunchTransferService.NeedsHabitatMixedLaunchTransfer(host);
                }

                if (this.mode == HabitatTransferMode.Joy)
                {
                    return ShuttleHolderLaunchTransferService.NeedsHabitatJoyLaunchTransfer(host);
                }

                return ShuttleHolderLaunchTransferService.NeedsHabitatLivingLaunchTransfer(host);
            }

            public bool CanUse(
                ThingWithComps host,
                TransportersArrivalAction arrivalAction,
                out string failureReason)
            {
                if (this.mode == HabitatTransferMode.Mixed)
                {
                    return ShuttleHolderLaunchTransferService.CanUseHabitatMixedLaunchTransfer(
                        host,
                        arrivalAction,
                        out failureReason);
                }

                if (this.mode == HabitatTransferMode.Joy)
                {
                    return ShuttleHolderLaunchTransferService.CanUseHabitatJoyLaunchTransfer(
                        host,
                        arrivalAction,
                        out failureReason);
                }

                return ShuttleHolderLaunchTransferService.CanUseHabitatLivingLaunchTransfer(
                    host,
                    arrivalAction,
                    out failureReason);
            }

            public bool TryExport(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                out string failureReason)
            {
                if (this.mode == HabitatTransferMode.Mixed)
                {
                    return ShuttleHolderLaunchTransferService.TryExportHabitatMixedToLaunchHandoff(
                        host,
                        handoffs,
                        out failureReason);
                }

                if (this.mode == HabitatTransferMode.Joy)
                {
                    return ShuttleHolderLaunchTransferService.TryExportHabitatJoyToLaunchHandoff(
                        host,
                        handoffs,
                        out failureReason);
                }

                return ShuttleHolderLaunchTransferService.TryExportHabitatLivingToLaunchHandoff(
                    host,
                    handoffs,
                    out failureReason);
            }

            public ShuttleHolderLaunchTransferParticipantRollbackResult TryRollback(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                Map map,
                string context)
            {
                bool rollbackSucceeded = true;
                string notice = null;

                if (this.mode == HabitatTransferMode.Mixed)
                {
                    rollbackSucceeded = ShuttleHolderLaunchTransferService.TryRollbackHabitatMixedLaunchExport(
                        host,
                        handoffs,
                        out notice);
                    return ShuttleHolderLaunchTransferParticipantRollbackResult.Cleared(
                        rollbackSucceeded,
                        false,
                        notice);
                }

                if (this.mode == HabitatTransferMode.Joy)
                {
                    rollbackSucceeded = ShuttleHolderLaunchTransferService.TryRollbackHabitatJoyLaunchExport(
                        host,
                        handoffs,
                        out notice);
                    string livingNotice;
                    bool livingRollbackSucceeded =
                        ShuttleHolderLaunchTransferService.TryRollbackHabitatLivingLaunchExport(
                            host,
                            handoffs,
                            out livingNotice);
                    rollbackSucceeded = rollbackSucceeded && livingRollbackSucceeded;
                    notice = AppendNotice(notice, livingNotice);
                    return ShuttleHolderLaunchTransferParticipantRollbackResult.Cleared(
                        rollbackSucceeded,
                        false,
                        notice);
                }

                rollbackSucceeded = ShuttleHolderLaunchTransferService.TryRollbackHabitatLivingLaunchExport(
                    host,
                    handoffs,
                    out notice);
                return ShuttleHolderLaunchTransferParticipantRollbackResult.Cleared(
                    rollbackSucceeded,
                    false,
                    notice);
            }
        }

        private sealed class MedicalBayParticipant : IShuttleHolderLaunchTransferParticipant
        {
            private static readonly string[] HolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind
            };

            public string Key
            {
                get { return MedicalBayKey; }
            }

            public int ExportOrder
            {
                get { return 30; }
            }

            public IReadOnlyList<string> ManifestHolderKinds
            {
                get { return HolderKinds; }
            }

            public bool NeedsTransfer(ThingWithComps host)
            {
                return ShuttleHolderLaunchTransferService.NeedsMedicalBayPatientLaunchTransfer(host);
            }

            public bool CanUse(
                ThingWithComps host,
                TransportersArrivalAction arrivalAction,
                out string failureReason)
            {
                bool hasHabitat = ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(host);
                bool hasMechCharger = ShuttleHolderLaunchTransferService.NeedsMechChargerLaunchTransfer(host);
                if (hasHabitat && hasMechCharger)
                {
                    return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                        host,
                        arrivalAction,
                        out failureReason);
                }

                if (hasHabitat)
                {
                    return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                        host,
                        arrivalAction,
                        out failureReason);
                }

                return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransfer(
                    host,
                    arrivalAction,
                    out failureReason);
            }

            public bool TryExport(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                out string failureReason)
            {
                string habitatManifestReason;
                if (ShuttleHolderLaunchTransferService.HasOnlyActiveHabitatLaunchTransferManifest(
                    host,
                    out habitatManifestReason))
                {
                    // Phase D: MedicalBay may follow Habitat only inside the current
                    // participant transaction, never after a broad or stale manifest.
                    return ShuttleHolderLaunchTransferService.TryExportMedicalBayPatientsToLaunchHandoffWithHabitatTransaction(
                        host,
                        handoffs,
                        out failureReason);
                }

                if (!ShuttleHolderLaunchTransferService.TryExportMedicalBayPatientsToLaunchHandoff(
                    host,
                    handoffs,
                    out failureReason))
                {
                    return false;
                }

                if (!ShuttleHolderLaunchTransferService.NeedsDevMedicalBayPatientRealLaunchRollbackSpike(host))
                {
                    return true;
                }

                failureReason = "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test forced failure before skyfaller spawn. MedicalBay rollback=" +
                    "pending transaction rollback";
                return false;
            }

            public ShuttleHolderLaunchTransferParticipantRollbackResult TryRollback(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                Map map,
                string context)
            {
                bool rollbackSucceeded;
                bool pawnsQuarantined;
                string notice;
                bool handoffsClear =
                    ShuttleHolderLaunchTransferService.TryRollbackMedicalBayPatientLaunchExportWithRecovery(
                        host,
                        handoffs,
                        out rollbackSucceeded,
                        out pawnsQuarantined,
                        out notice);

                LogMedicalBayRollbackForDev(
                    rollbackSucceeded,
                    pawnsQuarantined,
                    handoffsClear,
                    context,
                    notice);
                return new ShuttleHolderLaunchTransferParticipantRollbackResult(
                    rollbackSucceeded,
                    handoffsClear,
                    pawnsQuarantined,
                    notice);
            }
        }

        private sealed class MechChargerParticipant : IShuttleHolderLaunchTransferParticipant
        {
            private static readonly string[] HolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.MechChargerHolderKind
            };

            public string Key
            {
                get { return MechChargerKey; }
            }

            public int ExportOrder
            {
                get { return 40; }
            }

            public IReadOnlyList<string> ManifestHolderKinds
            {
                get { return HolderKinds; }
            }

            public bool NeedsTransfer(ThingWithComps host)
            {
                return ShuttleHolderLaunchTransferService.NeedsMechChargerLaunchTransfer(host);
            }

            public bool CanUse(
                ThingWithComps host,
                TransportersArrivalAction arrivalAction,
                out string failureReason)
            {
                bool hasHabitat = ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(host);
                bool hasMedicalBay = ShuttleHolderLaunchTransferService.NeedsMedicalBayPatientLaunchTransfer(host);
                if (hasHabitat && hasMedicalBay)
                {
                    return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
                        host,
                        arrivalAction,
                        out failureReason);
                }

                if (hasMedicalBay)
                {
                    return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithMedicalConcurrency(
                        host,
                        arrivalAction,
                        out failureReason);
                }

                return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithHabitatConcurrency(
                    host,
                    arrivalAction,
                    out failureReason);
            }

            public bool TryExport(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                out string failureReason)
            {
                string habitatAndMedicalBayManifestReason;
                string habitatManifestReason;
                string medicalBayManifestReason;
                if (ShuttleHolderLaunchTransferService.HasOnlyActiveHabitatAndMedicalBayLaunchTransferManifest(
                    host,
                    out habitatAndMedicalBayManifestReason))
                {
                    return ShuttleHolderLaunchTransferService.TryExportMechChargerToLaunchHandoffWithHabitatAndMedicalTransaction(
                        host,
                        handoffs,
                        out failureReason);
                }

                if (ShuttleHolderLaunchTransferService.HasOnlyActiveMedicalBayLaunchTransferManifest(
                    host,
                    out medicalBayManifestReason))
                {
                    return ShuttleHolderLaunchTransferService.TryExportMechChargerToLaunchHandoffWithMedicalTransaction(
                        host,
                        handoffs,
                        out failureReason);
                }

                if (ShuttleHolderLaunchTransferService.HasOnlyActiveHabitatLaunchTransferManifest(
                    host,
                    out habitatManifestReason))
                {
                    return ShuttleHolderLaunchTransferService.TryExportMechChargerToLaunchHandoffWithHabitatTransaction(
                        host,
                        handoffs,
                        out failureReason);
                }

                return ShuttleHolderLaunchTransferService.TryExportMechChargerToLaunchHandoff(
                    host,
                    handoffs,
                    out failureReason);
            }

            public ShuttleHolderLaunchTransferParticipantRollbackResult TryRollback(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                Map map,
                string context)
            {
                bool rollbackSucceeded;
                bool mechsQuarantined;
                string notice;
                bool handoffsClear =
                    ShuttleHolderLaunchTransferService.TryRollbackMechChargerLaunchExportWithRecovery(
                        host,
                        handoffs,
                        out rollbackSucceeded,
                        out mechsQuarantined,
                        out notice);
                return new ShuttleHolderLaunchTransferParticipantRollbackResult(
                    rollbackSucceeded,
                    handoffsClear,
                    mechsQuarantined,
                    notice);
            }
        }

        private sealed class PrisonCellParticipant : IShuttleHolderLaunchTransferParticipant
        {
            private static readonly string[] HolderKinds =
            {
                ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind
            };

            public string Key
            {
                get { return PrisonCellKey; }
            }

            public int ExportOrder
            {
                get { return 50; }
            }

            public IReadOnlyList<string> ManifestHolderKinds
            {
                get { return HolderKinds; }
            }

            public bool NeedsTransfer(ThingWithComps host)
            {
                return ShuttleHolderLaunchTransferService.NeedsPrisonCellPrisonerLaunchTransfer(host);
            }

            public bool CanUse(
                ThingWithComps host,
                TransportersArrivalAction arrivalAction,
                out string failureReason)
            {
                return ShuttleHolderLaunchTransferService.CanUsePrisonCellPrisonerLaunchTransfer(
                    host,
                    arrivalAction,
                    out failureReason);
            }

            public bool TryExport(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                out string failureReason)
            {
                return ShuttleHolderLaunchTransferService.TryExportPrisonCellPrisonersToLaunchHandoff(
                    host,
                    handoffs,
                    out failureReason);
            }

            public ShuttleHolderLaunchTransferParticipantRollbackResult TryRollback(
                ThingWithComps host,
                List<ShuttleLaunchCargoHandoff> handoffs,
                Map map,
                string context)
            {
                bool rollbackSucceeded;
                bool prisonersQuarantined;
                string notice;
                bool handoffsClear =
                    ShuttleHolderLaunchTransferService.TryRollbackPrisonCellPrisonerLaunchExportWithRecovery(
                        host,
                        handoffs,
                        out rollbackSucceeded,
                        out prisonersQuarantined,
                        out notice);
                return new ShuttleHolderLaunchTransferParticipantRollbackResult(
                    rollbackSucceeded,
                    handoffsClear,
                    prisonersQuarantined,
                    notice);
            }
        }

        private static void LogMedicalBayRollbackForDev(
            bool rollbackSucceeded,
            bool pawnsQuarantined,
            bool handoffsClear,
            string context,
            string notice)
        {
            if (!Prefs.DevMode && handoffsClear)
            {
                return;
            }

            string prefix = "[CeleTech Shuttle] MedicalBay rollback recovery. context=" +
                (context ?? "null") +
                " ";
            if (rollbackSucceeded)
            {
                if (Prefs.DevMode)
                {
                    Log.Message(prefix + "MedicalBay rollback succeeded. " + (notice ?? "null"));
                }

                return;
            }

            if (pawnsQuarantined)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning(prefix + "MedicalBay rollback failed but pawns were quarantined in MedicalBayPatientStagingThings. " +
                        (notice ?? "null"));
                }

                return;
            }

            if (!handoffsClear)
            {
                Log.Error(prefix +
                    "MedicalBay rollback failed and pawns remain in committed handoffs; normal cargo rollback was not allowed to silently consume them. " +
                    (notice ?? "null"));
                return;
            }

            if (Prefs.DevMode)
            {
                Log.Warning(prefix +
                    "MedicalBay rollback failed, but no MedicalBay manifest pawns remain in committed handoffs. Manifest remains active for diagnostics/recovery. " +
                    (notice ?? "null"));
            }
        }

        private static string AppendNotice(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left + " " + right;
        }
    }
}

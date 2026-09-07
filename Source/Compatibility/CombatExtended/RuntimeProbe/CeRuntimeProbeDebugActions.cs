using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "CE hidden weapon runtime probe...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginProbe()
        {
            BeginProbe(false, false, false);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE owner-token last-round probe...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginOwnerProbe()
        {
            BeginProbe(true, false, false);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE owner-token short-burst probe (legacy dev)...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginOwnerBurstProbe()
        {
            BeginProbe(true, true, false);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE owner-token held short-burst save probe (legacy dev)...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginOwnerHeldBurstProbe()
        {
            BeginProbe(true, true, true);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE runtime probe: resume held burst",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ResumeHeldBurst()
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            string failure = null;
            if (component == null || !component.ResumeHeldBurst(out failure))
            {
                Notify(
                    failure ?? "No held CE burst is available.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            Notify(
                "The held CE burst will continue on game ticks.",
                MessageTypeDefOf.NeutralEvent);
        }

        private static void BeginProbe(
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad)
        {
            TargetingParameters parameters = new TargetingParameters();
            parameters.canTargetBuildings = true;
            parameters.canTargetPawns = false;
            parameters.canTargetItems = false;
            parameters.canTargetLocations = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return target.Thing is ThingWithComps;
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    Thing host = target.Thing;
                    if (host != null)
                    {
                        OpenCandidateMenu(
                            host,
                            useAmmoOwnerAdapter,
                            useBurstFire,
                            holdBurstForSaveLoad);
                    }
                },
                null,
                null,
                null,
                true);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE runtime probe: report current",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportCurrent()
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            if (component == null || !component.Report())
            {
                Notify("No CE runtime probe session is present.", MessageTypeDefOf.RejectInput);
            }
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE runtime probe: clear current",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearCurrent()
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            if (component == null || !component.Clear())
            {
                Notify("No CE runtime probe session is present.", MessageTypeDefOf.RejectInput);
                return;
            }

            Notify("The CE runtime probe session was cleared.", MessageTypeDefOf.NeutralEvent);
        }

        private static void OpenCandidateMenu(
            Thing host,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad)
        {
            List<CeRuntimeProbeCandidate> candidates =
                CeRuntimeProbeCandidateResolver.Resolve();
            if (candidates.Count == 0)
            {
                Notify(
                    "No resolved CE magazine weapon with Verb_ShootCE was found.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                CeRuntimeProbeCandidate candidate = candidates[i];
                options.Add(new FloatMenuOption(
                    candidate.MenuLabel,
                    delegate
                    {
                        BeginTargetSelection(
                            host,
                            candidate,
                            useAmmoOwnerAdapter,
                            useBurstFire,
                            holdBurstForSaveLoad);
                    }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void BeginTargetSelection(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad)
        {
            TargetingParameters parameters = TargetingParameters.ForAttackAny();
            parameters.canTargetLocations = true;
            parameters.canTargetPawns = true;
            parameters.canTargetBuildings = true;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                if (!target.IsValid || host == null || !host.Spawned)
                {
                    return false;
                }

                return !target.HasThing || target.Thing != host;
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    StartSession(
                        host,
                        candidate,
                        target,
                        useAmmoOwnerAdapter,
                        useBurstFire,
                        holdBurstForSaveLoad);
                },
                null,
                null,
                null,
                true);
        }

        private static void StartSession(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            LocalTargetInfo target,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad)
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            if (component == null)
            {
                Notify("The CE runtime probe component is unavailable.", MessageTypeDefOf.RejectInput);
                return;
            }

            string failure;
            if (!component.TryStart(
                host,
                candidate,
                target,
                useAmmoOwnerAdapter,
                useBurstFire,
                holdBurstForSaveLoad,
                out failure))
            {
                Notify(failure ?? "The CE runtime probe did not start.", MessageTypeDefOf.RejectInput);
                return;
            }

            Notify(
                "CE runtime probe started. Details are in the RimWorld log.",
                MessageTypeDefOf.NeutralEvent);
        }

        private static void Notify(string text, MessageTypeDef messageType)
        {
            Messages.Message(text, messageType, false);
        }
    }
}

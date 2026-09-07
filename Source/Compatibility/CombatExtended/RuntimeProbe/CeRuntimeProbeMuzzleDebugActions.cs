using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeMuzzleDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "CE manual-source muzzle consumer probe...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginProbe()
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
                    if (target.Thing != null)
                    {
                        OpenCandidateMenu(target.Thing);
                    }
                },
                null,
                null,
                null,
                true);
        }

        private static void OpenCandidateMenu(Thing host)
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
                    delegate { BeginSourceSelection(host, candidate); }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void BeginSourceSelection(
            Thing host,
            CeRuntimeProbeCandidate candidate)
        {
            Notify(
                "Select the visible barrel tip inside the cyan shuttle area.",
                MessageTypeDefOf.NeutralEvent);

            List<IntVec3> allowedCells =
                CeRuntimeProbeMuzzleSourceSelector.BuildAllowedCells(host);

            TargetingParameters parameters = new TargetingParameters();
            parameters.canTargetLocations = true;
            parameters.canTargetBuildings = false;
            parameters.canTargetPawns = false;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return CeRuntimeProbeMuzzleSourceSelector.CanSelect(
                    host,
                    target);
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    CeRuntimeProbeMuzzleSource source;
                    if (!CeRuntimeProbeMuzzleSourceSelector.TryCapture(
                        host,
                        target,
                        out source))
                    {
                        Notify(
                            "That point is not a valid muzzle point near the selected shuttle.",
                            MessageTypeDefOf.RejectInput);
                        BeginSourceSelection(host, candidate);
                        return;
                    }

                    BeginTargetSelection(host, candidate, source);
                },
                delegate(LocalTargetInfo target)
                {
                    CeRuntimeProbeMuzzleSourceSelector.DrawAllowedArea(
                        allowedCells,
                        target);
                });
        }

        private static void BeginTargetSelection(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            CeRuntimeProbeMuzzleSource source)
        {
            Notify(
                "Select the CE shot target.",
                MessageTypeDefOf.NeutralEvent);

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
                    StartSession(host, candidate, source, target);
                },
                delegate(LocalTargetInfo target)
                {
                    CeRuntimeProbeMuzzleSourceSelector.DrawCommittedSource(
                        source,
                        target);
                });
        }

        internal static void StartSession(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            CeRuntimeProbeMuzzleSource source,
            LocalTargetInfo target)
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            if (component == null)
            {
                Notify(
                    "The CE runtime probe component is unavailable.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            string failure;
            if (!component.TryStartMuzzleProbe(
                host,
                candidate,
                source,
                target,
                out failure))
            {
                Notify(
                    failure ?? "The CE muzzle probe did not start.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            Notify(
                "CE muzzle probe completed or started. Details are in the RimWorld log.",
                MessageTypeDefOf.NeutralEvent);
        }

        internal static void Notify(string text, MessageTypeDef messageType)
        {
            Messages.Message(text, messageType, false);
        }
    }
}

using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeResolvedMuzzleDebugActions
    {
        [DebugAction(
            "CeleTech Shuttle",
            "CE existing-resolver muzzle probe...",
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
                        OpenInstalledWeaponMenu(target.Thing);
                    }
                },
                null,
                null,
                null,
                true);
        }

        private static void OpenInstalledWeaponMenu(Thing host)
        {
            List<CeRuntimeProbeResolvedMuzzleChoice> choices =
                new List<CeRuntimeProbeResolvedMuzzleChoice>();
            string failure;
            if (!CeRuntimeProbeResolvedMuzzleAdapter.TryBuildChoices(
                host,
                choices,
                out failure))
            {
                CeRuntimeProbeMuzzleDebugActions.Notify(
                    failure ?? "No installed shuttle weapon source was found.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>(choices.Count);
            for (int i = 0; i < choices.Count; i++)
            {
                CeRuntimeProbeResolvedMuzzleChoice choice = choices[i];
                options.Add(new FloatMenuOption(
                    choice.MenuLabel,
                    delegate { OpenCeCandidateMenu(host, choice); }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenCeCandidateMenu(
            Thing host,
            CeRuntimeProbeResolvedMuzzleChoice choice)
        {
            List<CeRuntimeProbeCandidate> candidates =
                CeRuntimeProbeCandidateResolver.Resolve();
            if (candidates.Count == 0)
            {
                CeRuntimeProbeMuzzleDebugActions.Notify(
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
                    delegate { BeginTargetSelection(host, choice, candidate); }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void BeginTargetSelection(
            Thing host,
            CeRuntimeProbeResolvedMuzzleChoice choice,
            CeRuntimeProbeCandidate candidate)
        {
            CeRuntimeProbeMuzzleDebugActions.Notify(
                "Select the CE shot target; the shuttle's existing resolver will supply the muzzle.",
                MessageTypeDefOf.NeutralEvent);

            TargetingParameters parameters = TargetingParameters.ForAttackAny();
            parameters.canTargetLocations = true;
            parameters.canTargetPawns = true;
            parameters.canTargetBuildings = true;
            parameters.canTargetItems = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return target.IsValid &&
                    host != null &&
                    host.Spawned &&
                    (!target.HasThing || target.Thing != host);
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    StartResolvedSession(host, choice, candidate, target);
                },
                null,
                null,
                null,
                true);
        }

        private static void StartResolvedSession(
            Thing host,
            CeRuntimeProbeResolvedMuzzleChoice choice,
            CeRuntimeProbeCandidate candidate,
            LocalTargetInfo target)
        {
            CeRuntimeProbeMuzzleSource source;
            string failure;
            if (!CeRuntimeProbeResolvedMuzzleAdapter.TryResolve(
                host,
                choice,
                target,
                out source,
                out failure))
            {
                CeRuntimeProbeMuzzleDebugActions.Notify(
                    failure ?? "The existing shuttle muzzle resolver did not return a source.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            CeRuntimeProbeMuzzleDebugActions.StartSession(
                host,
                candidate,
                source,
                target);
        }
    }
}

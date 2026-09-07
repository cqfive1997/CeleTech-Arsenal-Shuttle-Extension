using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    internal sealed class BHLiteModuleUIDrawer : ShuttleExternalModulePanelProviderBase
    {
        private readonly string runtimeSystemKey;

        public BHLiteModuleUIDrawer(string runtimeSystemKey)
        {
            this.runtimeSystemKey = runtimeSystemKey;
        }

        public override string RuntimeSystemKey
        {
            get { return runtimeSystemKey; }
        }

        public override bool CanShow(ShuttleExternalModulePanelContext context)
        {
            return context != null &&
                context.Module != null &&
                !string.IsNullOrWhiteSpace(RuntimeSystemKey) &&
                context.RuntimeSystemKey == RuntimeSystemKey;
        }

        public override float GetPreferredHeight(ShuttleExternalModulePanelContext context, float width)
        {
            return 240f;
        }

        public override void DrawPanel(Rect rect, ShuttleExternalModulePanelContext context)
        {
            if (context == null)
            {
                return;
            }

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                bool liteLoaded;
                bool fullDbhLoaded;
                bool hiddenByFullDbh;
                bool runtimeActive;
                bool bridgeResolved;
                bool hygieneResolved;
                bool bladderResolved;
                bool thirstResolved;
                bool thirstServiceEnabled;
                bool hostInfoAvailable;
                bool hostSpawned;
                int serviceableOccupants;
                int servicedPawns;
                int serviceActions;
                int hygieneServices;
                int bladderServices;
                int serviceFailures;
                int lastDiagnosticTick;
                int nextDiagnosticTick;
                int lastServiceTick;
                int nextServiceTick;
                string availabilityReason;
                string blockedReason;
                string lastResult;
                string missingNeedDefs;
                string optionalNeedDiagnostic;
                string runtimeFullKey;
                string lastServicedPawnLabels;
                string lastServiceFailure;

                context.State.TryGetBool("liteLoaded", out liteLoaded);
                context.State.TryGetBool("fullDbhLoaded", out fullDbhLoaded);
                context.State.TryGetBool("liteHiddenByFullDbh", out hiddenByFullDbh);
                context.State.TryGetBool("liteRuntimeActive", out runtimeActive);
                context.State.TryGetBool("liteBridgeResolved", out bridgeResolved);
                context.State.TryGetBool("liteHygieneResolved", out hygieneResolved);
                context.State.TryGetBool("liteBladderResolved", out bladderResolved);
                context.State.TryGetBool("liteThirstResolved", out thirstResolved);
                context.State.TryGetBool("liteThirstServiceEnabled", out thirstServiceEnabled);
                context.State.TryGetBool("liteHostInfoAvailable", out hostInfoAvailable);
                context.State.TryGetBool("liteHostSpawned", out hostSpawned);
                context.State.TryGetInt("liteServiceableOccupants", out serviceableOccupants);
                context.State.TryGetInt("liteServicedPawns", out servicedPawns);
                context.State.TryGetInt("liteServiceActions", out serviceActions);
                context.State.TryGetInt("liteHygieneServices", out hygieneServices);
                context.State.TryGetInt("liteBladderServices", out bladderServices);
                context.State.TryGetInt("liteServiceFailures", out serviceFailures);
                context.State.TryGetInt("liteLastDiagnosticTick", out lastDiagnosticTick);
                context.State.TryGetInt("liteNextDiagnosticTick", out nextDiagnosticTick);
                context.State.TryGetInt("liteLastServiceTick", out lastServiceTick);
                context.State.TryGetInt("liteNextServiceTick", out nextServiceTick);
                context.State.TryGetString("liteAvailabilityReason", out availabilityReason);
                context.State.TryGetString("liteBlockedReason", out blockedReason);
                context.State.TryGetString("liteLastResult", out lastResult);
                context.State.TryGetString("liteMissingNeedDefs", out missingNeedDefs);
                context.State.TryGetString("liteOptionalNeedDiagnostic", out optionalNeedDiagnostic);
                context.State.TryGetString("runtimeSystemKey", out runtimeFullKey);
                context.State.TryGetString("liteLastServicedPawnLabels", out lastServicedPawnLabels);
                context.State.TryGetString("liteLastServiceFailure", out lastServiceFailure);

                Widgets.DrawBoxSolid(rect, new Color(0.07f, 0.08f, 0.09f, 0.55f));
                Widgets.DrawBox(rect);
                rect = rect.ContractedBy(8f);
                float y = rect.y;

                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(rect.x, y, rect.width, 26f), "CT_Shuttle_Addon_BHLite_PlayerTitle".Translate().ToString());
                y += 28f;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x, y, rect.width, 36f), "CT_Shuttle_Addon_BHLite_PlayerSubtitle".Translate().ToString());
                y += 42f;

                DrawLine(rect, ref y, "CT_Shuttle_Addon_BHLite_Status".Translate().ToString(), FormatLiteStatus(liteLoaded, fullDbhLoaded, hiddenByFullDbh, runtimeActive, blockedReason));
                DrawLine(rect, ref y, "CT_Shuttle_Addon_ServiceableOccupants".Translate().ToString(), serviceableOccupants.ToString());
                DrawLine(rect, ref y, "CT_Shuttle_Addon_ServicedPawns".Translate().ToString(), servicedPawns.ToString());
                DrawLine(rect, ref y, "CT_Shuttle_Addon_ServiceActions".Translate().ToString(), serviceActions.ToString());
                DrawLine(rect, ref y, "CT_Shuttle_Addon_BHLite_HygieneServices".Translate().ToString(), hygieneServices.ToString());
                DrawLine(rect, ref y, "CT_Shuttle_Addon_BHLite_BladderServices".Translate().ToString(), bladderServices.ToString());
                DrawLine(rect, ref y, "CT_Shuttle_Addon_BHLite_LastServiceTick".Translate().ToString(), lastServiceTick.ToString());

            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
            }
        }

        private static void DrawLine(Rect rect, ref float y, string label, string value)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 20f), label + ": " + value);
            y += 20f;
        }

        private static string FormatLiteStatus(
            bool liteLoaded,
            bool fullDbhLoaded,
            bool hiddenByFullDbh,
            bool runtimeActive,
            string blockedReason)
        {
            if (hiddenByFullDbh || fullDbhLoaded && liteLoaded)
            {
                return "CT_Shuttle_Addon_BHLite_Status_HiddenByFull".Translate().ToString();
            }

            if (!liteLoaded)
            {
                return "CT_Shuttle_Addon_Status_Missing".Translate().ToString();
            }

            if (!runtimeActive || !string.IsNullOrEmpty(blockedReason))
            {
                return "CT_Shuttle_Addon_Status_Blocked".Translate().ToString();
            }

            return "CT_Shuttle_Addon_Status_Active".Translate().ToString();
        }

        private static string FormatNeeds(bool hygieneResolved, bool bladderResolved)
        {
            return "Hygiene=" + FormatBool(hygieneResolved) + ", Bladder=" + FormatBool(bladderResolved);
        }

        private static string FormatBool(bool value)
        {
            return value
                ? "CT_Shuttle_Addon_Status_Active".Translate().ToString()
                : "CT_Shuttle_Addon_Status_Missing".Translate().ToString();
        }

        private static string ResolveText(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}

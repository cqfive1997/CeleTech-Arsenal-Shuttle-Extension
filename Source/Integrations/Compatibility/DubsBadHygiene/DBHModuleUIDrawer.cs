using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.AdditionalModule;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal sealed class DBHModuleUIDrawer : ShuttleExternalModulePanelProviderBase
    {
        private static readonly Color PanelBackground = new Color(0.055f, 0.065f, 0.075f, 0.55f);
        private static readonly Color CardBackground = new Color(0.105f, 0.125f, 0.145f, 0.78f);
        private static readonly Color InnerCardBackground = new Color(0.075f, 0.088f, 0.102f, 0.82f);
        private static readonly Color SoftBorder = new Color(0.32f, 0.43f, 0.50f, 0.48f);
        private static readonly Color BarBackground = new Color(0.030f, 0.037f, 0.045f, 0.88f);
        private static readonly Color TitleText = new Color(0.86f, 0.94f, 0.98f, 1f);
        private static readonly Color MutedText = new Color(0.66f, 0.74f, 0.78f, 1f);
        private static readonly Color WaterBlue = new Color(0.16f, 0.54f, 0.92f, 1f);
        private static readonly Color OnlineGreen = new Color(0.22f, 0.58f, 0.32f, 0.95f);
        private static readonly Color WarningAmber = new Color(0.77f, 0.57f, 0.16f, 0.95f);
        private static readonly Color ErrorRed = new Color(0.70f, 0.18f, 0.14f, 0.95f);
        private static readonly Color OfflineGray = new Color(0.34f, 0.38f, 0.40f, 0.95f);
        private const string InputCleanWaterCapacity = "cleanWaterCapacity";
        private const string InputSewageCapacity = "sewageCapacity";
        private const string InputSepticTreatmentRate = "septicTreatmentRatePerDay";
        private static readonly Dictionary<string, string> NumericInputBuffers = new Dictionary<string, string>();

        private readonly string runtimeSystemKey;

        public DBHModuleUIDrawer(string runtimeSystemKey)
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
                context.RuntimeSystemKey == RuntimeSystemKey;
        }

        public override float GetPreferredHeight(ShuttleExternalModulePanelContext context, float width)
        {
            return Prefs.DevMode ? 548f : 470f;
        }

        public override void DrawPanel(Rect rect, ShuttleExternalModulePanelContext context)
        {
            if (context == null)
            {
                return;
            }

            CT_Shuttle_DBHIntegrationDef integration = DBHIntegrationResolver.ResolveDefaultIntegration();
            bool dbhLoaded;
            bool internalBusPowered;
            bool pipeConnected;
            bool waterPipeConnected;
            bool sewagePipeConnected;
            float cleanWater;
            float cleanWaterCapacity;
            float sewage;
            float sewageCapacity;
            float liquidMassKg;
            float lastPipeWaterAdded;
            float lastPipeSewageDrained;
            float septicTreatmentRatePerDay;
            string blockedReason;
            string pipeBlockedReason;

            context.State.TryGetBool("dbhLoaded", out dbhLoaded);
            context.State.TryGetBool("internalBusPowered", out internalBusPowered);
            context.State.TryGetBool("pipeConnected", out pipeConnected);
            context.State.TryGetBool("waterPipeConnected", out waterPipeConnected);
            context.State.TryGetBool("sewagePipeConnected", out sewagePipeConnected);
            cleanWater = DBHLiquidUtility.ReadCleanWaterLiters(context.State, 0f);
            sewage = DBHLiquidUtility.ReadSewageLiters(context.State, 0f);
            cleanWaterCapacity = DBHLiquidUtility.ReadCleanWaterCapacityLiters(context.State, integration);
            sewageCapacity = DBHLiquidUtility.ReadSewageCapacityLiters(context.State, integration);
            liquidMassKg = DBHLiquidUtility.CalculateLiquidMassKg(
                cleanWater,
                sewage,
                integration);
            context.State.TryGetFloat("lastPipeWaterAdded", out lastPipeWaterAdded);
            context.State.TryGetFloat("lastPipeSewageDrained", out lastPipeSewageDrained);
            septicTreatmentRatePerDay = ReadSepticTreatmentRatePerDay(context, integration);
            context.State.TryGetString("blockedReason", out blockedReason);
            context.State.TryGetString("pipeBlockedReason", out pipeBlockedReason);

            string playerStatus = FormatPlayerStatus(context.RuntimeEnabled, dbhLoaded, internalBusPowered, blockedReason);
            Color statusColor = ResolveStatusColor(context.RuntimeEnabled, dbhLoaded, internalBusPowered, blockedReason);
            string networkStatus = FormatExternalNetworkStatus(
                pipeConnected,
                waterPipeConnected,
                sewagePipeConnected,
                lastPipeWaterAdded,
                lastPipeSewageDrained,
                pipeBlockedReason);

            Widgets.DrawBoxSolid(rect, PanelBackground);
            DrawSoftBorder(rect, SoftBorder);
            rect = rect.ContractedBy(8f);

            Text.Font = GameFont.Tiny;
            float y = rect.y;
            DrawHeaderCard(
                rect,
                ref y,
                playerStatus,
                statusColor,
                context.Module != null ? context.Module.IdlePowerDrawWatts : 0f,
                liquidMassKg,
                networkStatus);
            DrawLiquidCards(rect, ref y, cleanWater, cleanWaterCapacity, sewage, sewageCapacity, septicTreatmentRatePerDay, integration);
            DrawCapacityControls(rect, ref y, context, cleanWater, cleanWaterCapacity, sewage, sewageCapacity, septicTreatmentRatePerDay, integration);
            if (Prefs.DevMode)
            {
                DrawPerformanceCard(rect, ref y);
            }
        }

        public override void CollectCommands(
            ShuttleExternalModulePanelContext context,
            IShuttleExternalModulePanelCommandSink sink)
        {
            if (sink == null)
            {
                return;
            }

            // Configuration buttons are drawn inline inside the settings card.
        }

        private static ShuttleExternalPanelCommandContribution BuildConfigCommand(
            ShuttleExternalModulePanelContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string localKey,
            string commandLocalKey,
            string label,
            string tooltip,
            int order)
        {
            float currentValue = ReadConfigValue(context, integration, localKey);
            string buffer = EnsureInputBuffer(context, localKey, currentValue);
            float parsedValue;
            bool validNumber = TryParseFloat(buffer, out parsedValue);
            bool validValue = validNumber && IsConfigValueValid(context, integration, localKey, parsedValue);
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            arguments["target"] = localKey;
            arguments["value"] = validNumber
                ? parsedValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                : buffer;
            bool enabled = context != null && context.RuntimeEnabled && validValue;
            string disabledReason = validValue
                ? "CT_Shuttle_Addon_TankCapacityCommand_Disabled".Translate().ToString()
                : ResolveConfigDisabledReason(localKey);
            return new ShuttleExternalPanelCommandContribution(
                commandLocalKey,
                label,
                tooltip,
                AdditionalModuleIdentity.ResolvePackageId() + "/" + DBHTankCapacityCommandHandler.LocalCommandKey,
                arguments,
                enabled,
                disabledReason,
                order);
        }

        private static void DrawLine(Rect rect, ref float y, string label, string value)
        {
            Rect line = new Rect(rect.x, y, rect.width, 20f);
            Widgets.Label(line, label + ": " + value);
            y += 20f;
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }

        private static void DrawHeaderCard(
            Rect rect,
            ref float y,
            string status,
            Color statusColor,
            float powerWatts,
            float liquidMassKg,
            string networkStatus)
        {
            Rect card = new Rect(rect.x, y, rect.width, 112f);
            DrawCard(card, CardBackground);

            Text.Font = GameFont.Small;
            DrawColoredLabel(
                new Rect(card.x + 12f, card.y + 10f, card.width - 148f, 24f),
                "CT_Shuttle_Addon_DBH_HygieneSuite_PlayerTitle".Translate().ToString());
            Text.Font = GameFont.Tiny;
            DrawColoredLabel(
                new Rect(card.x + 12f, card.y + 38f, card.width - 24f, 18f),
                "CT_Shuttle_Addon_DBH_HygieneSuite_PlayerSubtitle".Translate().ToString(),
                MutedText);

            DrawPill(new Rect(card.xMax - 120f, card.y + 12f, 106f, 24f), status, statusColor);

            float metricY = card.y + 66f;
            float gap = 8f;
            float metricWidth = (card.width - 24f - gap) / 2f;
            DrawMetric(
                new Rect(card.x + 12f, metricY, metricWidth, 18f),
                "CT_Shuttle_Addon_DBH_PowerDemand".Translate().ToString(),
                powerWatts.ToString("0.#") + " W");
            DrawMetric(
                new Rect(card.x + 12f + metricWidth + gap, metricY, metricWidth, 18f),
                "CT_Shuttle_Addon_DBH_LiquidMass".Translate().ToString(),
                liquidMassKg.ToString("0.#") + " kg");
            Rect networkRect = new Rect(card.x + 12f, metricY + 24f, card.width - 24f, 18f);
            DrawMetric(
                networkRect,
                "CT_Shuttle_Addon_DBH_ExternalNetwork".Translate().ToString(),
                networkStatus);
            TooltipHandler.TipRegion(
                networkRect,
                "CT_Shuttle_Addon_DBH_PipeConnectionHint".Translate().ToString());
            y += card.height + 8f;
        }

        private static void DrawLiquidCards(
            Rect rect,
            ref float y,
            float cleanWater,
            float cleanWaterCapacity,
            float sewage,
            float sewageCapacity,
            float septicTreatmentRatePerDay,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            float gap = 12f;
            float cardWidth = (rect.width - gap) / 2f;
            Rect waterCard = new Rect(rect.x, y, cardWidth, 140f);
            Rect sewageCard = new Rect(rect.x + cardWidth + gap, y, cardWidth, 140f);
            DrawLiquidCard(
                waterCard,
                "CT_Shuttle_Addon_DBH_CleanWaterTank".Translate().ToString(),
                cleanWater,
                cleanWaterCapacity,
                DBHLiquidUtility.CalculateLiquidMassKg(cleanWater, 0f, integration),
                new Color(0.18f, 0.55f, 0.95f),
                null);
            DrawLiquidCard(
                sewageCard,
                "CT_Shuttle_Addon_DBH_SepticTank".Translate().ToString(),
                sewage,
                sewageCapacity,
                DBHLiquidUtility.CalculateLiquidMassKg(0f, sewage, integration),
                ResolveSewageColor(sewage, sewageCapacity),
                FormatSepticStatus(sewage, sewageCapacity, septicTreatmentRatePerDay));
            y += 150f;
        }

        private static void DrawLiquidCard(
            Rect card,
            string title,
            float current,
            float capacity,
            float massKg,
            Color fillColor,
            string status)
        {
            DrawCard(card, InnerCardBackground);
            Rect inner = card.ContractedBy(10f);
            float pctValue = capacity > 0f ? Mathf.Clamp01(current / capacity) : 0f;
            string pct = (pctValue * 100f).ToString("0") + "%";

            Text.Font = GameFont.Tiny;
            DrawColoredLabel(new Rect(inner.x, inner.y, inner.width - 50f, 18f), title, TitleText);
            Text.Anchor = TextAnchor.UpperRight;
            DrawColoredLabel(new Rect(inner.xMax - 48f, inner.y, 48f, 18f), pct, MutedText);
            Text.Anchor = TextAnchor.UpperLeft;

            Text.Font = GameFont.Small;
            DrawColoredLabel(
                new Rect(inner.x, inner.y + 28f, inner.width, 24f),
                FormatTank(current, capacity) + " L");
            Text.Font = GameFont.Tiny;
            DrawColoredLabel(
                new Rect(inner.x, inner.y + 54f, inner.width, 18f),
                massKg.ToString("0.#") + " kg",
                MutedText);

            Rect bar = new Rect(inner.x, inner.y + 78f, inner.width, 22f);
            DrawFillBar(bar, pctValue, fillColor);
            if (!string.IsNullOrEmpty(status))
            {
                DrawColoredLabel(new Rect(inner.x, inner.y + 108f, inner.width, 18f), status, fillColor);
            }
        }

        private static void DrawCapacityControls(
            Rect rect,
            ref float y,
            ShuttleExternalModulePanelContext context,
            float cleanWater,
            float cleanWaterCapacity,
            float sewage,
            float sewageCapacity,
            float septicTreatmentRatePerDay,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            Rect card = new Rect(rect.x, y, rect.width, 156f);
            DrawCard(card, CardBackground);
            Rect inner = card.ContractedBy(10f);
            float min = DBHLiquidUtility.MinTankCapacityLiters(integration);
            float max = DBHLiquidUtility.MaxTankCapacityLiters(integration);

            Text.Font = GameFont.Tiny;
            DrawColoredLabel(new Rect(inner.x, inner.y, inner.width, 18f), "CT_Shuttle_Addon_DBH_ConfigTitle".Translate().ToString(), TitleText);
            DrawColoredLabel(
                new Rect(inner.x, inner.y + 22f, inner.width, 18f),
                "CT_Shuttle_Addon_DBH_CapacityRange".Translate(min.ToString("0.#"), max.ToString("0.#")).ToString(),
                MutedText);

            DrawConfigInputRow(
                context,
                new Rect(inner.x, inner.y + 48f, inner.width, 24f),
                integration,
                InputCleanWaterCapacity,
                "CT_Shuttle_Addon_DBH_CleanWaterCapacity".Translate().ToString(),
                cleanWaterCapacity,
                cleanWater,
                "L",
                "dbh-apply-clean-water-capacity",
                "CT_Shuttle_Addon_DBH_ApplyCleanWaterCapacity".Translate().ToString(),
                "CT_Shuttle_Addon_TankCapacityCommand_Tooltip".Translate().ToString(),
                10);
            DrawConfigInputRow(
                context,
                new Rect(inner.x, inner.y + 78f, inner.width, 24f),
                integration,
                InputSewageCapacity,
                "CT_Shuttle_Addon_DBH_SepticCapacity".Translate().ToString(),
                sewageCapacity,
                sewage,
                "L",
                "dbh-apply-septic-capacity",
                "CT_Shuttle_Addon_DBH_ApplySepticCapacity".Translate().ToString(),
                "CT_Shuttle_Addon_TankCapacityCommand_Tooltip".Translate().ToString(),
                20);
            DrawConfigInputRow(
                context,
                new Rect(inner.x, inner.y + 108f, inner.width, 24f),
                integration,
                InputSepticTreatmentRate,
                "CT_Shuttle_Addon_DBH_SepticTreatmentRate".Translate().ToString(),
                septicTreatmentRatePerDay,
                -1f,
                "L/day",
                "dbh-apply-septic-treatment-rate",
                "CT_Shuttle_Addon_DBH_ApplySepticTreatmentRate".Translate().ToString(),
                "CT_Shuttle_Addon_DBH_TreatmentRateCommand_Tooltip".Translate().ToString(),
                30);
            y += card.height + 8f;
        }

        private static void DrawPerformanceCard(Rect rect, ref float y)
        {
            Rect card = new Rect(rect.x, y, rect.width, 64f);
            DrawCard(card, CardBackground);
            Rect inner = card.ContractedBy(10f);

            Text.Font = GameFont.Tiny;
            DrawColoredLabel(
                new Rect(inner.x, inner.y, inner.width, 18f),
                "CT_Shuttle_Addon_DBH_Perf_Title".Translate().ToString(),
                TitleText);

            float columnGap = 10f;
            float columnWidth = (inner.width - columnGap) / 2f;
            DrawMetric(
                new Rect(inner.x, inner.y + 28f, columnWidth, 18f),
                "CT_Shuttle_Addon_DBH_Perf_PipeBridge".Translate().ToString(),
                FormatPerformanceMetric(DBHPerformanceSection.PipeBridge));
            DrawMetric(
                new Rect(inner.x + columnWidth + columnGap, inner.y + 28f, columnWidth, 18f),
                "CT_Shuttle_Addon_DBH_Perf_OccupantService".Translate().ToString(),
                FormatPerformanceMetric(DBHPerformanceSection.OccupantService));
            y += card.height + 8f;
        }

        private static string FormatPerformanceMetric(DBHPerformanceSection section)
        {
            double averageMs;
            double peakMs;
            long samples;
            if (!DBHRuntimePerformanceMetrics.TryGetSnapshot(
                section,
                out averageMs,
                out peakMs,
                out samples))
            {
                return "CT_Shuttle_Addon_DBH_Perf_NoSamples".Translate().ToString();
            }

            return "CT_Shuttle_Addon_DBH_Perf_Value".Translate(
                averageMs.ToString("0.###"),
                peakMs.ToString("0.###"),
                samples.ToString()).ToString();
        }

        private static void DrawConfigInputRow(
            ShuttleExternalModulePanelContext context,
            Rect rect,
            CT_Shuttle_DBHIntegrationDef integration,
            string fieldKey,
            string label,
            float currentValue,
            float storedValue,
            string unit,
            string commandLocalKey,
            string buttonLabel,
            string tooltip,
            int order)
        {
            float buttonWidth = Mathf.Min(74f, Mathf.Max(58f, rect.width * 0.12f));
            float labelWidth = Mathf.Min(168f, rect.width * 0.30f);
            float inputWidth = Mathf.Min(120f, Mathf.Max(86f, rect.width * 0.22f));
            Rect labelRect = new Rect(rect.x, rect.y + 3f, labelWidth, rect.height);
            Rect inputRect = new Rect(rect.x + labelWidth + 8f, rect.y, inputWidth, rect.height);
            Rect buttonRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            Rect suffixRect = new Rect(inputRect.xMax + 8f, rect.y + 3f, Mathf.Max(0f, buttonRect.x - inputRect.xMax - 16f), rect.height);

            DrawColoredLabel(labelRect, label, MutedText);
            string before = EnsureInputBuffer(context, fieldKey, currentValue);
            string edited = Widgets.TextField(inputRect, before ?? string.Empty);
            SetInputBuffer(context, fieldKey, NormalizeNumericBuffer(edited));

            string suffix = unit;
            if (storedValue >= 0f)
            {
                suffix = unit + "  " + "CT_Shuttle_Addon_DBH_CurrentStored".Translate(storedValue.ToString("0.#")).ToString();
            }

            DrawColoredLabel(suffixRect, suffix, MutedText);
            DrawInlineCommandButton(
                context,
                buttonRect,
                BuildConfigCommand(
                    context,
                    integration,
                    fieldKey,
                    commandLocalKey,
                    buttonLabel,
                    tooltip,
                    order));
        }

        private static void DrawInlineCommandButton(
            ShuttleExternalModulePanelContext context,
            Rect rect,
            ShuttleExternalPanelCommandContribution contribution)
        {
            string disabledReason = null;
            bool enabled = context != null &&
                context.CommandExecutor != null &&
                context.CommandExecutor.CanExecute(contribution, out disabledReason);
            if (context == null || context.CommandExecutor == null)
            {
                disabledReason = "CT_Shuttle_Addon_DBH_InlineCommandUnavailable".Translate().ToString();
            }

            string tooltip = enabled && contribution != null ? contribution.Tooltip : disabledReason;
            bool clicked = DrawInlineStyledButton(
                rect,
                contribution != null ? contribution.Label : string.Empty,
                enabled,
                tooltip);
            if (clicked && enabled && context != null && context.CommandExecutor != null)
            {
                context.CommandExecutor.Execute(contribution);
            }

            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private static bool DrawInlineStyledButton(
            Rect rect,
            string label,
            bool enabled,
            string tooltip)
        {
            Color accent = WaterBlue;
            Color background = enabled
                ? new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.88f)
                : new Color(0.10f, 0.11f, 0.12f, 0.72f);
            Color border = enabled
                ? new Color(0.22f, 0.66f, 0.95f, 0.95f)
                : SoftBorder;
            Color textColor = enabled ? new Color(0.36f, 0.78f, 1f, 1f) : MutedText;
            bool hovered = enabled && Mouse.IsOver(rect);
            if (hovered)
            {
                background = new Color(background.r, background.g, background.b, 1f);
            }

            Widgets.DrawBoxSolid(rect, background);
            DrawSoftBorder(rect, border);
            if (enabled)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), 1f), new Color(1f, 1f, 1f, 0.12f));
            }

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            DrawColoredLabel(rect.ContractedBy(2f), label, textColor);
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return Widgets.ButtonInvisible(rect);
        }

        private static void DrawTankBar(
            Rect rect,
            ref float y,
            string label,
            float current,
            float capacity,
            Color fillColor)
        {
            Rect labelRect = new Rect(rect.x, y, rect.width, 18f);
            Widgets.Label(labelRect, label + ": " + FormatTank(current, capacity) + " L");
            y += 18f;

            Rect barRect = new Rect(rect.x, y, rect.width, 18f);
            float pct = capacity > 0f ? Mathf.Clamp01(current / capacity) : 0f;
            Widgets.DrawBoxSolid(barRect, new Color(0.12f, 0.12f, 0.12f, 0.35f));
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * pct, barRect.height);
            Widgets.DrawBoxSolid(fillRect, fillColor);
            Widgets.DrawBox(barRect);
            y += 24f;
        }

        private static void DrawCard(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
            DrawSoftBorder(rect, SoftBorder);
        }

        private static void DrawPill(Rect rect, string label, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
            DrawSoftBorder(rect, new Color(0.76f, 0.90f, 0.94f, 0.32f));
            Text.Anchor = TextAnchor.MiddleCenter;
            DrawColoredLabel(rect, label, Color.white);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawMetric(Rect rect, string label, string value)
        {
            DrawColoredLabel(rect, label + ": " + value, MutedText);
        }

        private static void DrawFillBar(Rect rect, float pct, Color fillColor)
        {
            Widgets.DrawBoxSolid(rect, BarBackground);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(pct), rect.height), fillColor);
            DrawSoftBorder(rect, new Color(0.46f, 0.56f, 0.60f, 0.42f));
        }

        private static void DrawButtonPreviewRow(Rect rect, string label, string[] values)
        {
            float labelWidth = 92f;
            DrawColoredLabel(new Rect(rect.x, rect.y + 2f, labelWidth, rect.height), label, MutedText);
            float gap = 6f;
            float x = rect.x + labelWidth;
            float width = Mathf.Min(72f, (rect.width - labelWidth - gap * (values.Length - 1)) / values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                Rect chip = new Rect(x + (width + gap) * i, rect.y, width, rect.height);
                Widgets.DrawBoxSolid(chip, InnerCardBackground);
                DrawSoftBorder(chip, SoftBorder);
                Text.Anchor = TextAnchor.MiddleCenter;
                DrawColoredLabel(chip, values[i], TitleText);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private static void DrawMetricCard(Rect rect, string label, string value)
        {
            Widgets.DrawBoxSolid(rect, InnerCardBackground);
            DrawSoftBorder(rect, new Color(0.28f, 0.38f, 0.44f, 0.38f));
            Rect inner = rect.ContractedBy(5f);
            Text.Font = GameFont.Tiny;
            DrawColoredLabel(new Rect(inner.x, inner.y, inner.width, 18f), label, MutedText);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerLeft;
            DrawColoredLabel(new Rect(inner.x, inner.y + 16f, inner.width, 22f), value, TitleText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;
        }

        private static void DrawSoftBorder(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void DrawColoredLabel(Rect rect, string text)
        {
            DrawColoredLabel(rect, text, TitleText);
        }

        private static void DrawColoredLabel(Rect rect, string text, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.Label(rect, text);
            GUI.color = oldColor;
        }

        private static string FormatTank(float current, float capacity)
        {
            return current.ToString("0.#") + " / " + capacity.ToString("0.#");
        }

        private static string FormatCumulativeRecent(float cumulative, float recent)
        {
            return cumulative.ToString("0.#") + " / " + recent.ToString("0.#");
        }

        private static float ReadConfigValue(
            ShuttleExternalModulePanelContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string fieldKey)
        {
            if (fieldKey == InputCleanWaterCapacity)
            {
                return DBHLiquidUtility.ReadCleanWaterCapacityLiters(context != null ? context.State : null, integration);
            }

            if (fieldKey == InputSewageCapacity)
            {
                return DBHLiquidUtility.ReadSewageCapacityLiters(context != null ? context.State : null, integration);
            }

            if (fieldKey == InputSepticTreatmentRate)
            {
                return ReadSepticTreatmentRatePerDay(context, integration);
            }

            return DBHLiquidUtility.ReadTankCapacityLiters(context != null ? context.State : null, integration);
        }

        private static bool IsConfigValueValid(
            ShuttleExternalModulePanelContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string fieldKey,
            float value)
        {
            if (context == null || context.State == null)
            {
                return false;
            }

            if (fieldKey == InputSepticTreatmentRate)
            {
                return value >= 0f && value <= MaxSepticTreatmentRatePerDay(integration);
            }

            float min = DBHLiquidUtility.MinTankCapacityLiters(integration);
            float max = DBHLiquidUtility.MaxTankCapacityLiters(integration);
            if (value < min || value > max)
            {
                return false;
            }

            if (fieldKey == InputCleanWaterCapacity)
            {
                return value >= DBHLiquidUtility.ReadCleanWaterLiters(context.State, 0f);
            }

            if (fieldKey == InputSewageCapacity)
            {
                return value >= DBHLiquidUtility.ReadSewageLiters(context.State, 0f);
            }

            return false;
        }

        private static string ResolveConfigDisabledReason(string fieldKey)
        {
            return fieldKey == InputSepticTreatmentRate
                ? "CT_Shuttle_Addon_DBH_TreatmentRateInvalid".Translate().ToString()
                : "CT_Shuttle_Addon_DBH_CapacityCannotBelowStored".Translate().ToString();
        }

        private static string EnsureInputBuffer(
            ShuttleExternalModulePanelContext context,
            string fieldKey,
            float defaultValue)
        {
            string key = InputBufferKey(context, fieldKey);
            string value;
            if (!NumericInputBuffers.TryGetValue(key, out value))
            {
                value = FormatInputNumber(defaultValue);
                NumericInputBuffers[key] = value;
            }

            return value;
        }

        private static void SetInputBuffer(
            ShuttleExternalModulePanelContext context,
            string fieldKey,
            string value)
        {
            NumericInputBuffers[InputBufferKey(context, fieldKey)] = value ?? string.Empty;
        }

        private static string InputBufferKey(
            ShuttleExternalModulePanelContext context,
            string fieldKey)
        {
            string moduleId = context != null &&
                context.Module != null &&
                !string.IsNullOrEmpty(context.Module.ModuleInstanceId)
                    ? context.Module.ModuleInstanceId
                    : "default";
            return moduleId + "/" + fieldKey;
        }

        private static string NormalizeNumericBuffer(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
            bool hasDecimal = false;
            for (int i = 0; i < value.Length && builder.Length < 12; i++)
            {
                char c = value[i];
                if (c >= '0' && c <= '9')
                {
                    builder.Append(c);
                }
                else if ((c == '.' || c == ',') && !hasDecimal)
                {
                    builder.Append('.');
                    hasDecimal = true;
                }
            }

            return builder.ToString();
        }

        private static string FormatInputNumber(float value)
        {
            return value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool TryParseFloat(string value, out float result)
        {
            result = 0f;
            return !string.IsNullOrWhiteSpace(value) &&
                float.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out result);
        }

        private static float ReadSepticTreatmentRatePerDay(
            ShuttleExternalModulePanelContext context,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            float value;
            if (context != null &&
                context.State != null &&
                context.State.TryGetFloat("septicTreatmentRatePerDay", out value))
            {
                return Mathf.Clamp(value, 0f, MaxSepticTreatmentRatePerDay(integration));
            }

            return Mathf.Clamp(
                integration != null ? integration.septicTreatmentRatePerDay : 1500f,
                0f,
                MaxSepticTreatmentRatePerDay(integration));
        }

        private static float MaxSepticTreatmentRatePerDay(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.maxSepticTreatmentRatePerDay > 0f
                ? integration.maxSepticTreatmentRatePerDay
                : 10000f;
        }

        private static string ResolveText(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static string FormatPlayerStatus(
            bool runtimeEnabled,
            bool dbhLoaded,
            bool internalBusPowered,
            string blockedReason)
        {
            if (!runtimeEnabled || !internalBusPowered || !dbhLoaded)
            {
                return "CT_Shuttle_Addon_DBH_Status_Offline".Translate().ToString();
            }

            return string.IsNullOrEmpty(blockedReason)
                ? "CT_Shuttle_Addon_DBH_Status_Online".Translate().ToString()
                : "CT_Shuttle_Addon_DBH_Status_Blocked".Translate().ToString();
        }

        private static Color ResolveStatusColor(
            bool runtimeEnabled,
            bool dbhLoaded,
            bool internalBusPowered,
            string blockedReason)
        {
            if (!runtimeEnabled || !internalBusPowered || !dbhLoaded)
            {
                return OfflineGray;
            }

            return string.IsNullOrEmpty(blockedReason)
                ? OnlineGreen
                : WarningAmber;
        }

        private static string FormatExternalNetworkStatus(
            bool pipeConnected,
            bool waterPipeConnected,
            bool sewagePipeConnected,
            float lastPipeWaterAdded,
            float lastPipeSewageDrained,
            string pipeBlockedReason)
        {
            if (!pipeConnected)
            {
                return "CT_Shuttle_Addon_DBH_ExternalNetwork_NotConnected".Translate().ToString();
            }

            if (waterPipeConnected && !sewagePipeConnected)
            {
                return "CT_Shuttle_Addon_DBH_ExternalNetwork_WaterOnly".Translate().ToString();
            }

            if (sewagePipeConnected && !waterPipeConnected)
            {
                return "CT_Shuttle_Addon_DBH_ExternalNetwork_SewageOnly".Translate().ToString();
            }

            if (lastPipeWaterAdded > 0f)
            {
                return "CT_Shuttle_Addon_DBH_RecentPipeWater".Translate().ToString() + " " +
                    lastPipeWaterAdded.ToString("0.#") + " L";
            }

            if (lastPipeSewageDrained > 0f)
            {
                return "CT_Shuttle_Addon_DBH_RecentPipeSewage".Translate().ToString() + " " +
                    lastPipeSewageDrained.ToString("0.#") + " L";
            }

            string noWater = "CT_Shuttle_Addon_Pipe_NoDrawableWater".Translate().ToString();
            string noSewage = "CT_Shuttle_Addon_Pipe_NoSewageFacility".Translate().ToString();
            if (!string.IsNullOrEmpty(pipeBlockedReason) && pipeBlockedReason == noWater)
            {
                return "CT_Shuttle_Addon_DBH_ExternalNetwork_NoWater".Translate().ToString();
            }

            if (!string.IsNullOrEmpty(pipeBlockedReason) && pipeBlockedReason == noSewage)
            {
                return "CT_Shuttle_Addon_DBH_ExternalNetwork_NoSewage".Translate().ToString();
            }

            if (waterPipeConnected && sewagePipeConnected)
            {
                return "CT_Shuttle_Addon_DBH_ExternalNetwork_Connected".Translate().ToString();
            }

            return "CT_Shuttle_Addon_DBH_ExternalNetwork_NotConnected".Translate().ToString();
        }

        private static string FormatRecentServiceStatus(
            bool runtimeEnabled,
            bool dbhLoaded,
            bool internalBusPowered,
            int serviceActions,
            string blockedReason)
        {
            if (!runtimeEnabled || !internalBusPowered || !dbhLoaded)
            {
                return "CT_Shuttle_Addon_DBH_Status_Offline".Translate().ToString();
            }

            if (!string.IsNullOrEmpty(blockedReason))
            {
                return "CT_Shuttle_Addon_DBH_Status_Blocked".Translate().ToString();
            }

            return serviceActions > 0
                ? "CT_Shuttle_Addon_DBH_Status_Online".Translate().ToString()
                : "CT_Shuttle_Addon_DBH_ServiceReady".Translate().ToString();
        }

        private static string FormatSepticStatus(float sewage, float capacity, float treatmentRatePerDay)
        {
            float pct = capacity > 0f ? sewage / capacity : 0f;
            string treatment = treatmentRatePerDay > 0f
                ? "CT_Shuttle_Addon_DBH_TreatmentPerDay".Translate(treatmentRatePerDay.ToString("0.#")).ToString()
                : "CT_Shuttle_Addon_DBH_TreatmentDisabled".Translate().ToString();
            if (pct >= 0.999f)
            {
                return "CT_Shuttle_Addon_DBH_SepticFull".Translate().ToString() + " | " + treatment;
            }

            if (pct >= 0.80f)
            {
                return "CT_Shuttle_Addon_DBH_SepticNearlyFull".Translate().ToString() + " | " + treatment;
            }

            return "CT_Shuttle_Addon_DBH_SepticNormal".Translate().ToString() + " | " + treatment;
        }

        private static Color ResolveSewageColor(float sewage, float capacity)
        {
            float pct = capacity > 0f ? sewage / capacity : 0f;
            if (pct >= 0.999f)
            {
                return ErrorRed;
            }

            if (pct >= 0.80f)
            {
                return WarningAmber;
            }

            return new Color(0.62f, 0.48f, 0.18f);
        }

        private static string FormatBoolState(bool value)
        {
            return (value ? "CT_Shuttle_Addon_Status_Active" : "CT_Shuttle_Addon_Status_Blocked").Translate().ToString();
        }

        private static string FormatLoadedState(bool loaded)
        {
            return (loaded ? "CT_Shuttle_Addon_Status_Loaded" : "CT_Shuttle_Addon_Status_Missing").Translate().ToString();
        }

        private static string FormatOnlineState(bool online)
        {
            return (online ? "CT_Shuttle_Addon_Status_Online" : "CT_Shuttle_Addon_Status_Offline").Translate().ToString();
        }

        private static string FormatActiveBlockedState(bool active)
        {
            return (active ? "CT_Shuttle_Addon_Status_Active" : "CT_Shuttle_Addon_Status_Blocked").Translate().ToString();
        }

        private static string FormatConnectedState(bool connected)
        {
            return (connected ? "CT_Shuttle_Addon_Status_Connected" : "CT_Shuttle_Addon_Status_NotConnected").Translate().ToString();
        }

        private static string FormatAvailableState(bool available)
        {
            return (available ? "CT_Shuttle_Addon_Status_Available" : "CT_Shuttle_Addon_Status_Unavailable").Translate().ToString();
        }

        private static string FormatDumpMethod(string dumpMethod)
        {
            if (string.IsNullOrEmpty(dumpMethod) || dumpMethod == "none")
            {
                return "CT_Shuttle_Addon_DumpMethod_None".Translate().ToString();
            }

            if (dumpMethod == "pipe")
            {
                return "CT_Shuttle_Addon_DumpMethod_Pipe".Translate().ToString();
            }

            if (dumpMethod == "map")
            {
                return "CT_Shuttle_Addon_DumpMethod_Map".Translate().ToString();
            }

            if (dumpMethod == "world")
            {
                return "CT_Shuttle_Addon_DumpMethod_World".Translate().ToString();
            }

            if (dumpMethod == "failed")
            {
                return "CT_Shuttle_Addon_DumpMethod_Failed".Translate().ToString();
            }

            return dumpMethod;
        }
    }
}

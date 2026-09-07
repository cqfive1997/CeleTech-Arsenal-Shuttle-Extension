using System.Globalization;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal sealed class DBHTankCapacityCommandHandler : IShuttleExternalCommandHandler
    {
        public const string LocalCommandKey = "dbh-set-tank-capacity";
        public const string TargetCleanWaterCapacity = "cleanWaterCapacity";
        public const string TargetSewageCapacity = "sewageCapacity";
        public const string TargetSepticTreatmentRatePerDay = "septicTreatmentRatePerDay";
        private const string TargetTankCapacity = "tankCapacity";

        public ShuttleExternalCommandResult Execute(ShuttleExternalCommandContext context)
        {
            if (context == null || context.State == null || context.Command == null)
            {
                return ShuttleExternalCommandResult.Failed(Tr("CT_Shuttle_Addon_DBH_ConfigCommand_ContextMissing"));
            }

            CT_Shuttle_DBHIntegrationDef integration = DBHIntegrationResolver.ResolveDefaultIntegration();
            DBHLiquidUtility.EnsureTankState(context.State, integration);

            string targetKind = ResolveTargetKind(context);
            float target;
            if (!TryResolveTargetValue(context, integration, targetKind, out target))
            {
                return ShuttleExternalCommandResult.Failed(Tr("CT_Shuttle_Addon_DBH_ConfigCommand_MissingValue"));
            }

            string reason;
            if (!ApplyTarget(context, integration, targetKind, target, out reason))
            {
                return ShuttleExternalCommandResult.Failed(reason);
            }

            string message = BuildSuccessMessage(context, integration, targetKind);
            context.State.SetString("lastResult", message);
            context.State.SetString("blockedReason", null);
            return ShuttleExternalCommandResult.Succeeded(message);
        }

        private static string ResolveTargetKind(ShuttleExternalCommandContext context)
        {
            string raw;
            if (context.Command.Arguments != null &&
                context.Command.Arguments.TryGetValue("target", out raw) &&
                !string.IsNullOrWhiteSpace(raw))
            {
                return raw.Trim();
            }

            return TargetTankCapacity;
        }

        private static bool TryResolveTargetValue(
            ShuttleExternalCommandContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string targetKind,
            out float target)
        {
            target = 0f;
            string raw;
            if (context.Command.Arguments != null &&
                context.Command.Arguments.TryGetValue("value", out raw) &&
                TryParseFloat(raw, out target))
            {
                return true;
            }

            if (context.Command.Arguments != null &&
                context.Command.Arguments.TryGetValue("targetLiters", out raw) &&
                TryParseFloat(raw, out target))
            {
                return true;
            }

            float delta;
            if (context.Command.Arguments != null &&
                context.Command.Arguments.TryGetValue("deltaLiters", out raw) &&
                TryParseFloat(raw, out delta))
            {
                target = ReadCurrentValue(context, integration, targetKind) + delta;
                return true;
            }

            return false;
        }

        private static bool ApplyTarget(
            ShuttleExternalCommandContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string targetKind,
            float target,
            out string reason)
        {
            reason = null;
            if (targetKind == TargetCleanWaterCapacity)
            {
                return DBHLiquidUtility.TrySetCleanWaterCapacityLiters(context.State, integration, target, out reason);
            }

            if (targetKind == TargetSewageCapacity)
            {
                return DBHLiquidUtility.TrySetSewageCapacityLiters(context.State, integration, target, out reason);
            }

            if (targetKind == TargetSepticTreatmentRatePerDay)
            {
                float clamped = Clamp(target, 0f, MaxSepticTreatmentRatePerDay(integration));
                context.State.SetFloat("septicTreatmentRatePerDay", clamped);
                context.State.SetFloat("septicTreatmentEffectiveRatePerDay", clamped);
                return true;
            }

            return DBHLiquidUtility.TrySetTankCapacityLiters(context.State, integration, target, out reason);
        }

        private static string BuildSuccessMessage(
            ShuttleExternalCommandContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string targetKind)
        {
            if (targetKind == TargetCleanWaterCapacity)
            {
                return Tr(
                    "CT_Shuttle_Addon_DBH_ConfigCommand_CleanWaterCapacitySet",
                    DBHLiquidUtility.ReadCleanWaterCapacityLiters(context.State, integration).ToString("0.#"));
            }

            if (targetKind == TargetSewageCapacity)
            {
                return Tr(
                    "CT_Shuttle_Addon_DBH_ConfigCommand_SepticCapacitySet",
                    DBHLiquidUtility.ReadSewageCapacityLiters(context.State, integration).ToString("0.#"));
            }

            if (targetKind == TargetSepticTreatmentRatePerDay)
            {
                return Tr(
                    "CT_Shuttle_Addon_DBH_ConfigCommand_TreatmentRateSet",
                    ReadCurrentValue(context, integration, targetKind).ToString("0.#"));
            }

            return Tr(
                "CT_Shuttle_Addon_DBH_ConfigCommand_TankCapacitySet",
                DBHLiquidUtility.ReadTankCapacityLiters(context.State, integration).ToString("0.#"));
        }

        private static float ReadCurrentValue(
            ShuttleExternalCommandContext context,
            CT_Shuttle_DBHIntegrationDef integration,
            string targetKind)
        {
            if (targetKind == TargetCleanWaterCapacity)
            {
                return DBHLiquidUtility.ReadCleanWaterCapacityLiters(context.State, integration);
            }

            if (targetKind == TargetSewageCapacity)
            {
                return DBHLiquidUtility.ReadSewageCapacityLiters(context.State, integration);
            }

            if (targetKind == TargetSepticTreatmentRatePerDay)
            {
                float value;
                if (context.State.TryGetFloat("septicTreatmentRatePerDay", out value))
                {
                    return Clamp(value, 0f, MaxSepticTreatmentRatePerDay(integration));
                }

                return Clamp(
                    integration != null ? integration.septicTreatmentRatePerDay : 1500f,
                    0f,
                    MaxSepticTreatmentRatePerDay(integration));
            }

            return DBHLiquidUtility.ReadTankCapacityLiters(context.State, integration);
        }

        private static bool TryParseFloat(string value, out float result)
        {
            result = 0f;
            return !string.IsNullOrWhiteSpace(value) &&
                float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }

        private static float MaxSepticTreatmentRatePerDay(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.maxSepticTreatmentRatePerDay > 0f
                ? integration.maxSepticTreatmentRatePerDay
                : 10000f;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }

        private static string Tr(string key, params object[] args)
        {
            return string.Format(Tr(key), args);
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal static class DBHLiquidUtility
    {
        public const string TankCapacityLitersKey = "tankCapacityLiters";
        public const string CleanWaterLitersKey = "cleanWaterLiters";
        public const string SewageLitersKey = "sewageLiters";
        public const string CleanWaterCapacityLitersKey = "cleanWaterCapacityLiters";
        public const string SewageCapacityLitersKey = "sewageCapacityLiters";

        public static void InitializeTankState(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            if (state == null)
            {
                return;
            }

            float cleanCapacity = ClampConfiguredCapacity(DefaultCleanWaterCapacityLiters(integration), integration);
            float sewageCapacity = ClampConfiguredCapacity(DefaultSewageCapacityLiters(integration), integration);
            float cleanWater = cleanCapacity * 0.5f;
            float sewage = 0f;
            WriteTankState(state, integration, cleanCapacity, sewageCapacity, cleanWater, sewage);
        }

        public static void EnsureTankState(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            if (state == null)
            {
                return;
            }

            float cleanWater = ReadCleanWaterLiters(state, -1f);
            float sewage = ReadSewageLiters(state, -1f);
            float cleanCapacity = ReadCleanWaterCapacityLiters(state, integration);
            float sewageCapacity = ReadSewageCapacityLiters(state, integration);
            if (cleanWater < 0f)
            {
                cleanWater = cleanCapacity * 0.5f;
            }

            if (sewage < 0f)
            {
                sewage = 0f;
            }

            cleanCapacity = ClampConfiguredCapacity(cleanCapacity, integration);
            sewageCapacity = ClampConfiguredCapacity(sewageCapacity, integration);
            cleanCapacity = Max(cleanCapacity, cleanWater);
            sewageCapacity = Max(sewageCapacity, sewage);
            WriteTankState(state, integration, cleanCapacity, sewageCapacity, cleanWater, sewage);
        }

        public static bool TrySetTankCapacityLiters(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float requestedCapacityLiters,
            out string reason)
        {
            reason = null;
            if (state == null)
            {
                reason = Tr("CT_Shuttle_Addon_DBH_ConfigCommand_ContextMissing");
                return false;
            }

            EnsureTankState(state, integration);
            float cleanWater = ReadCleanWaterLiters(state, 0f);
            float sewage = ReadSewageLiters(state, 0f);
            float minCapacity = MinTankCapacityLiters(integration);
            float maxCapacity = MaxTankCapacityLiters(integration);
            float target = Clamp(requestedCapacityLiters, minCapacity, maxCapacity);
            if (target + 0.001f < Max(cleanWater, sewage))
            {
                reason = Tr("CT_Shuttle_Addon_DBH_ConfigCommand_TankCapacityBelowStored");
                return false;
            }

            WriteTankState(state, integration, target, target, cleanWater, sewage);
            return true;
        }

        public static bool TrySetCleanWaterCapacityLiters(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float requestedCapacityLiters,
            out string reason)
        {
            reason = null;
            if (state == null)
            {
                reason = Tr("CT_Shuttle_Addon_DBH_ConfigCommand_CleanWaterContextMissing");
                return false;
            }

            EnsureTankState(state, integration);
            float cleanWater = ReadCleanWaterLiters(state, 0f);
            float sewage = ReadSewageLiters(state, 0f);
            float target = ClampConfiguredCapacity(requestedCapacityLiters, integration);
            if (target + 0.001f < cleanWater)
            {
                reason = Tr("CT_Shuttle_Addon_DBH_ConfigCommand_CleanWaterBelowStored");
                return false;
            }

            WriteTankState(
                state,
                integration,
                target,
                ReadSewageCapacityLiters(state, integration),
                cleanWater,
                sewage);
            return true;
        }

        public static bool TrySetSewageCapacityLiters(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float requestedCapacityLiters,
            out string reason)
        {
            reason = null;
            if (state == null)
            {
                reason = Tr("CT_Shuttle_Addon_DBH_ConfigCommand_SepticContextMissing");
                return false;
            }

            EnsureTankState(state, integration);
            float cleanWater = ReadCleanWaterLiters(state, 0f);
            float sewage = ReadSewageLiters(state, 0f);
            float target = ClampConfiguredCapacity(requestedCapacityLiters, integration);
            if (target + 0.001f < sewage)
            {
                reason = Tr("CT_Shuttle_Addon_DBH_ConfigCommand_SepticBelowStored");
                return false;
            }

            WriteTankState(
                state,
                integration,
                ReadCleanWaterCapacityLiters(state, integration),
                target,
                cleanWater,
                sewage);
            return true;
        }

        public static float ReadTankCapacityLiters(
            IShuttleExternalRuntimeStateReader state,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            float cleanCapacity;
            float sewageCapacity;
            bool hasClean = TryGetFloat(state, CleanWaterCapacityLitersKey, out cleanCapacity) ||
                TryGetFloat(state, "cleanWaterCapacity", out cleanCapacity);
            bool hasSewage = TryGetFloat(state, SewageCapacityLitersKey, out sewageCapacity) ||
                TryGetFloat(state, "sewageCapacity", out sewageCapacity);
            if (hasClean || hasSewage)
            {
                return ClampConfiguredCapacity(Max(hasClean ? cleanCapacity : 0f, hasSewage ? sewageCapacity : 0f), integration);
            }

            float value;
            if (TryGetFloat(state, TankCapacityLitersKey, out value))
            {
                return ClampConfiguredCapacity(value, integration);
            }

            return ClampConfiguredCapacity(DefaultTankCapacityLiters(integration), integration);
        }

        public static float ReadCleanWaterCapacityLiters(
            IShuttleExternalRuntimeStateReader state,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            float value;
            if (TryGetFloat(state, CleanWaterCapacityLitersKey, out value) ||
                TryGetFloat(state, "cleanWaterCapacity", out value))
            {
                return ClampConfiguredCapacity(value, integration);
            }

            if (TryGetFloat(state, TankCapacityLitersKey, out value))
            {
                return ClampConfiguredCapacity(value, integration);
            }

            return ClampConfiguredCapacity(DefaultCleanWaterCapacityLiters(integration), integration);
        }

        public static float ReadSewageCapacityLiters(
            IShuttleExternalRuntimeStateReader state,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            float value;
            if (TryGetFloat(state, SewageCapacityLitersKey, out value) ||
                TryGetFloat(state, "sewageCapacity", out value))
            {
                return ClampConfiguredCapacity(value, integration);
            }

            if (TryGetFloat(state, TankCapacityLitersKey, out value))
            {
                return ClampConfiguredCapacity(value, integration);
            }

            return ClampConfiguredCapacity(DefaultSewageCapacityLiters(integration), integration);
        }

        public static float ReadCleanWaterLiters(IShuttleExternalRuntimeStateReader state, float fallback)
        {
            float value;
            if (TryGetFloat(state, CleanWaterLitersKey, out value) ||
                TryGetFloat(state, "cleanWater", out value))
            {
                return value;
            }

            return fallback;
        }

        public static float ReadSewageLiters(IShuttleExternalRuntimeStateReader state, float fallback)
        {
            float value;
            if (TryGetFloat(state, SewageLitersKey, out value) ||
                TryGetFloat(state, "sewage", out value))
            {
                return value;
            }

            return fallback;
        }

        public static void SetCleanWaterLiters(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float cleanWaterLiters)
        {
            if (state == null)
            {
                return;
            }

            float cleanCapacity = ReadCleanWaterCapacityLiters(state, integration);
            float sewageCapacity = ReadSewageCapacityLiters(state, integration);
            float sewage = ReadSewageLiters(state, 0f);
            WriteTankState(state, integration, cleanCapacity, sewageCapacity, cleanWaterLiters, sewage);
        }

        public static void SetSewageLiters(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float sewageLiters)
        {
            if (state == null)
            {
                return;
            }

            float cleanCapacity = ReadCleanWaterCapacityLiters(state, integration);
            float sewageCapacity = ReadSewageCapacityLiters(state, integration);
            float cleanWater = ReadCleanWaterLiters(state, cleanCapacity * 0.5f);
            WriteTankState(state, integration, cleanCapacity, sewageCapacity, cleanWater, sewageLiters);
        }

        public static void WriteTankState(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float capacityLiters,
            float cleanWaterLiters,
            float sewageLiters)
        {
            WriteTankState(state, integration, capacityLiters, capacityLiters, cleanWaterLiters, sewageLiters);
        }

        public static void WriteTankState(
            IShuttleExternalRuntimeStateStore state,
            CT_Shuttle_DBHIntegrationDef integration,
            float cleanWaterCapacityLiters,
            float sewageCapacityLiters,
            float cleanWaterLiters,
            float sewageLiters)
        {
            if (state == null)
            {
                return;
            }

            float cleanCapacity = ClampConfiguredCapacity(cleanWaterCapacityLiters, integration);
            float sewageCapacity = ClampConfiguredCapacity(sewageCapacityLiters, integration);
            float clean = Clamp(cleanWaterLiters, 0f, cleanCapacity);
            float sewage = Clamp(sewageLiters, 0f, sewageCapacity);
            float legacyCapacity = Max(cleanCapacity, sewageCapacity);

            state.SetFloat(TankCapacityLitersKey, legacyCapacity);
            state.SetFloat(CleanWaterCapacityLitersKey, cleanCapacity);
            state.SetFloat(SewageCapacityLitersKey, sewageCapacity);
            state.SetFloat(CleanWaterLitersKey, clean);
            state.SetFloat(SewageLitersKey, sewage);

            // Legacy aliases kept for older bridge/UI code and save migration readability.
            state.SetFloat("cleanWaterCapacity", cleanCapacity);
            state.SetFloat("sewageCapacity", sewageCapacity);
            state.SetFloat("cleanWater", clean);
            state.SetFloat("sewage", sewage);
        }

        public static float CalculateLiquidMassKg(
            float cleanWaterLiters,
            float sewageLiters,
            CT_Shuttle_DBHIntegrationDef integration)
        {
            float waterFactor = integration != null && integration.waterMassKgPerLiter > 0f
                ? integration.waterMassKgPerLiter
                : 1f;
            float sewageFactor = integration != null && integration.sewageMassKgPerLiter > 0f
                ? integration.sewageMassKgPerLiter
                : 1f;
            return Max(0f, cleanWaterLiters) * waterFactor +
                Max(0f, sewageLiters) * sewageFactor;
        }

        public static float DefaultTankCapacityLiters(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.defaultTankCapacityLiters > 0f
                ? integration.defaultTankCapacityLiters
                : 1000f;
        }

        public static float DefaultCleanWaterCapacityLiters(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.cleanWaterCapacity > 0f
                ? integration.cleanWaterCapacity
                : DefaultTankCapacityLiters(integration);
        }

        public static float DefaultSewageCapacityLiters(CT_Shuttle_DBHIntegrationDef integration)
        {
            if (integration != null && integration.sewageCapacity > 0f)
            {
                return integration.sewageCapacity;
            }

            return integration != null && integration.linkSewageCapacityToWaterCapacity
                ? DefaultCleanWaterCapacityLiters(integration)
                : DefaultTankCapacityLiters(integration);
        }

        public static float MinTankCapacityLiters(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.minTankCapacityLiters > 0f
                ? integration.minTankCapacityLiters
                : 250f;
        }

        public static float MaxTankCapacityLiters(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.maxTankCapacityLiters > 0f
                ? integration.maxTankCapacityLiters
                : 5000f;
        }

        public static float TankCapacityStepLiters(CT_Shuttle_DBHIntegrationDef integration)
        {
            return integration != null && integration.tankCapacityStepLiters > 0f
                ? integration.tankCapacityStepLiters
                : 250f;
        }

        private static float ClampConfiguredCapacity(float value, CT_Shuttle_DBHIntegrationDef integration)
        {
            return Clamp(value, MinTankCapacityLiters(integration), MaxTankCapacityLiters(integration));
        }

        private static bool TryGetFloat(
            IShuttleExternalRuntimeStateReader state,
            string key,
            out float value)
        {
            value = 0f;
            return state != null && state.TryGetFloat(key, out value);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static float Max(float left, float right)
        {
            return left > right ? left : right;
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }
    }
}

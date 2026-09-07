using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal sealed class DBHReflectionBridge
    {
        private readonly CT_Shuttle_DBHIntegrationDef integration;
        private NeedDef hygieneNeedDef;
        private NeedDef bladderNeedDef;
        private NeedDef thirstNeedDef;
        private MethodInfo hygieneCleanMethod;
        private MethodInfo bladderDumpMethod;
        private bool resolved;
        private string missingNeedDefs;
        private string optionalMissingNeedDefs;
        private const string BladderDirection = "FallsWhenNeedBuilds";

        public DBHReflectionBridge(CT_Shuttle_DBHIntegrationDef integration)
        {
            this.integration = integration;
        }

        public string MissingNeedDefs
        {
            get { return missingNeedDefs; }
        }

        public string OptionalMissingNeedDefs
        {
            get { return optionalMissingNeedDefs; }
        }

        public string BladderDirectionForDisplay
        {
            get { return BladderDirection + " (DBH XML: BladderRateMultiplier describes bladder level falling)."; }
        }

        public bool Resolve()
        {
            if (resolved)
            {
                return hygieneNeedDef != null && bladderNeedDef != null;
            }

            resolved = true;
            hygieneNeedDef = ResolveNeedDef(integration != null ? integration.hygieneNeedDefName : null, "hygieneNeedDefName");
            bladderNeedDef = ResolveNeedDef(integration != null ? integration.bladderNeedDefName : null, "bladderNeedDefName");
            thirstNeedDef = ResolveOptionalNeedDef(integration != null ? integration.thirstNeedDefName : null, "thirstNeedDefName");
            hygieneCleanMethod = FindSingleFloatVoidMethod(
                hygieneNeedDef != null ? hygieneNeedDef.needClass : null,
                "clean");
            bladderDumpMethod = FindParameterlessVoidMethod(
                bladderNeedDef != null ? bladderNeedDef.needClass : null,
                "dump");
            return hygieneNeedDef != null && bladderNeedDef != null;
        }

        public bool TryGetNeedAvailability(Pawn pawn, out bool hasHygiene, out bool hasBladder)
        {
            hasHygiene = false;
            hasBladder = false;
            if (pawn == null || pawn.needs == null || !Resolve())
            {
                return false;
            }

            hasHygiene = pawn.needs.TryGetNeed(hygieneNeedDef) != null;
            hasBladder = pawn.needs.TryGetNeed(bladderNeedDef) != null;
            return true;
        }

        public bool TryReadNeeds(Pawn pawn, out float hygiene, out float bladder, out float thirst)
        {
            hygiene = -1f;
            bladder = -1f;
            thirst = -1f;

            if (pawn == null || pawn.needs == null || !Resolve())
            {
                return false;
            }

            hygiene = ReadNeedPercent(pawn, hygieneNeedDef);
            bladder = ReadNeedPercent(pawn, bladderNeedDef);
            thirst = ReadNeedPercent(pawn, thirstNeedDef);
            return hygiene >= 0f || bladder >= 0f || thirst >= 0f;
        }

        public bool TryRaiseHygiene(Pawn pawn, float amount)
        {
            if (pawn == null || pawn.needs == null || amount <= 0f || !Resolve() || hygieneNeedDef == null)
            {
                return false;
            }

            Need need = pawn.needs.TryGetNeed(hygieneNeedDef);
            if (need == null)
            {
                return false;
            }

            float before = need.CurLevel;
            if (TryInvokeNeedMethod(need, hygieneCleanMethod, new object[] { amount }, "hygiene-clean"))
            {
                return need.CurLevel > before;
            }

            return TryRaiseNeedPercent(need, amount, 1f);
        }

        public bool TryReadBladder(Pawn pawn, out float value)
        {
            value = -1f;
            if (pawn == null || pawn.needs == null || !Resolve() || bladderNeedDef == null)
            {
                return false;
            }

            value = ReadNeedPercent(pawn, bladderNeedDef);
            return value >= 0f;
        }

        public bool TryServiceBladder(Pawn pawn, float targetOrDelta)
        {
            if (pawn == null || pawn.needs == null || targetOrDelta <= 0f || !Resolve() || bladderNeedDef == null)
            {
                return false;
            }

            Need need = pawn.needs.TryGetNeed(bladderNeedDef);
            if (need == null)
            {
                return false;
            }

            float current = need.CurLevelPercentage;
            float targetPercent = targetOrDelta <= 1f && targetOrDelta > current
                ? targetOrDelta
                : current + targetOrDelta;

            float before = need.CurLevel;
            if (TryInvokeNeedMethod(need, bladderDumpMethod, null, "bladder-dump"))
            {
                // DBH's dump() advances the need in small per-tick steps and marks the need
                // as actively using a toilet. Preserve that marker, then finish this abstract
                // holder service up to the configured target without simulating hundreds of
                // repeated fixture ticks.
                float boundedTarget = Clamp01(targetPercent);
                if (need.CurLevelPercentage < boundedTarget)
                {
                    need.CurLevel = need.MaxLevel * boundedTarget;
                }

                return need.CurLevel > before;
            }

            return TryRaiseNeedPercent(need, targetPercent - current, targetPercent);
        }

        private static bool TryInvokeNeedMethod(
            Need need,
            MethodInfo method,
            object[] arguments,
            string warningKey)
        {
            if (need == null || method == null || !method.DeclaringType.IsInstanceOfType(need))
            {
                return false;
            }

            try
            {
                method.Invoke(need, arguments);
                return true;
            }
            catch (Exception ex)
            {
                AdditionalModuleLog.WarningOnce(
                    "dbh-need-method-" + warningKey,
                    "DBH need method invocation failed for " + warningKey + ": " + RootMessage(ex));
                return false;
            }
        }

        private static float ReadNeedPercent(Pawn pawn, NeedDef def)
        {
            if (pawn == null || pawn.needs == null || def == null)
            {
                return -1f;
            }

            Need need = pawn.needs.TryGetNeed(def);
            return need != null ? need.CurLevelPercentage : -1f;
        }

        private NeedDef ResolveNeedDef(string defName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                AddMissing(fieldName + " is empty");
                return null;
            }

            NeedDef def = DefDatabase<NeedDef>.GetNamedSilentFail(defName.Trim());
            if (def == null)
            {
                AddMissing(fieldName + " '" + defName.Trim() + "'");
            }

            return def;
        }

        private NeedDef ResolveOptionalNeedDef(string defName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                AddOptionalMissing(fieldName + " is empty");
                return null;
            }

            NeedDef def = DefDatabase<NeedDef>.GetNamedSilentFail(defName.Trim());
            if (def == null)
            {
                AddOptionalMissing(fieldName + " '" + defName.Trim() + "'");
            }

            return def;
        }

        private void AddMissing(string message)
        {
            missingNeedDefs = string.IsNullOrEmpty(missingNeedDefs)
                ? message
                : missingNeedDefs + ", " + message;
        }

        private void AddOptionalMissing(string message)
        {
            optionalMissingNeedDefs = string.IsNullOrEmpty(optionalMissingNeedDefs)
                ? message
                : optionalMissingNeedDefs + ", " + message;
        }

        private static bool TryRaiseNeedPercent(Need need, float deltaPercent, float maxPercent)
        {
            if (need == null || deltaPercent <= 0f)
            {
                return false;
            }

            float before = need.CurLevel;
            float targetPercent = Clamp01(need.CurLevelPercentage + deltaPercent);
            if (maxPercent >= 0f)
            {
                targetPercent = targetPercent > maxPercent ? maxPercent : targetPercent;
            }

            need.CurLevel = need.MaxLevel * targetPercent;
            return need.CurLevel > before;
        }

        private static MethodInfo FindSingleFloatVoidMethod(Type type, string methodName)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(float) },
                null);
            return method != null && method.ReturnType == typeof(void) ? method : null;
        }

        private static MethodInfo FindParameterlessVoidMethod(Type type, string methodName)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);
            return method != null && method.ReturnType == typeof(void) ? method : null;
        }

        private static string RootMessage(Exception exception)
        {
            TargetInvocationException targetInvocation = exception as TargetInvocationException;
            if (targetInvocation != null && targetInvocation.InnerException != null)
            {
                return targetInvocation.InnerException.Message;
            }

            return exception != null ? exception.Message : "unknown error";
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}

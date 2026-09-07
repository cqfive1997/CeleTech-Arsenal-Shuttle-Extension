using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    internal sealed class BHLiteReflectionBridge
    {
        private readonly CT_Shuttle_BHLiteIntegrationDef integration;
        private bool resolved;
        private NeedDef hygieneNeedDef;
        private NeedDef bladderNeedDef;
        private NeedDef thirstNeedDef;
        private MethodInfo hygieneCleanMethod;
        private MethodInfo bladderDumpMethod;
        private Type hygieneNeedType;
        private Type bladderNeedType;
        private string lastFailureReason;

        public BHLiteReflectionBridge(CT_Shuttle_BHLiteIntegrationDef integration)
        {
            this.integration = integration;
        }

        public bool HygieneResolved
        {
            get { return hygieneNeedDef != null; }
        }

        public bool BladderResolved
        {
            get { return bladderNeedDef != null; }
        }

        public bool ThirstResolved
        {
            get { return thirstNeedDef != null; }
        }

        public bool RequiredNeedsResolved
        {
            get { return HygieneResolved && BladderResolved; }
        }

        public string LastFailureReason
        {
            get { return lastFailureReason; }
        }

        public string MissingRequiredNeedDefs
        {
            get
            {
                if (RequiredNeedsResolved)
                {
                    return null;
                }

                System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
                if (!HygieneResolved)
                {
                    missing.Add(NeedName(integration != null ? integration.hygieneNeedDefName : null, "Hygiene"));
                }

                if (!BladderResolved)
                {
                    missing.Add(NeedName(integration != null ? integration.bladderNeedDefName : null, "Bladder"));
                }

                return string.Join(", ", missing.ToArray());
            }
        }

        public string OptionalNeedDiagnostic
        {
            get
            {
                string thirstName = integration != null ? integration.thirstNeedDefName : null;
                if (string.IsNullOrWhiteSpace(thirstName))
                {
                    return "DBHThirst is not configured for Lite diagnostic.";
                }

                return ThirstResolved
                    ? thirstName.Trim() + " resolved; Lite v1 does not service thirst."
                    : thirstName.Trim() + " missing or disabled; Lite v1 does not service thirst.";
            }
        }

        public bool Resolve()
        {
            if (resolved)
            {
                return RequiredNeedsResolved;
            }

            resolved = true;
            hygieneNeedDef = ResolveNeedDef(integration != null ? integration.hygieneNeedDefName : null);
            bladderNeedDef = ResolveNeedDef(integration != null ? integration.bladderNeedDefName : null);
            thirstNeedDef = ResolveNeedDef(integration != null ? integration.thirstNeedDefName : null);
            return RequiredNeedsResolved;
        }

        public bool TryReadHygiene(Pawn pawn, out float value)
        {
            value = -1f;
            Need need = TryGetNeed(pawn, hygieneNeedDef);
            if (need == null)
            {
                return false;
            }

            value = need.CurLevelPercentage;
            return value >= 0f;
        }

        public bool TryCleanHygiene(Pawn pawn, float gainPercent, float targetPercent)
        {
            lastFailureReason = null;
            if (gainPercent <= 0f)
            {
                return false;
            }

            Need need = TryGetNeed(pawn, hygieneNeedDef);
            if (need == null)
            {
                return false;
            }

            float currentPercent = need.CurLevelPercentage;
            float target = Clamp01(targetPercent);
            if (currentPercent >= target)
            {
                return false;
            }

            float deltaPercent = Math.Min(gainPercent, target - currentPercent);
            float deltaLevel = deltaPercent * SafeMaxLevel(need);
            if (deltaLevel <= 0f)
            {
                return false;
            }

            try
            {
                MethodInfo method = ResolveHygieneCleanMethod(need);
                float before = need.CurLevel;
                if (method != null)
                {
                    method.Invoke(need, new object[] { deltaLevel });
                }
                else
                {
                    need.CurLevel = Math.Min(need.CurLevel + deltaLevel, SafeMaxLevel(need) * target);
                }

                return need.CurLevel > before;
            }
            catch (Exception exception)
            {
                lastFailureReason = DescribeException("Hygiene clean failed", exception);
                return false;
            }
        }

        public bool TryReadBladder(Pawn pawn, out float value)
        {
            value = -1f;
            Need need = TryGetNeed(pawn, bladderNeedDef);
            if (need == null)
            {
                return false;
            }

            value = need.CurLevelPercentage;
            return value >= 0f;
        }

        public bool TryServiceBladder(Pawn pawn, float targetPercent, int maxDumpCalls, out int dumpCallsUsed)
        {
            dumpCallsUsed = 0;
            lastFailureReason = null;
            if (maxDumpCalls <= 0)
            {
                return false;
            }

            Need need = TryGetNeed(pawn, bladderNeedDef);
            if (need == null)
            {
                return false;
            }

            float target = Clamp01(targetPercent);
            if (need.CurLevelPercentage >= target)
            {
                return false;
            }

            try
            {
                MethodInfo method = ResolveBladderDumpMethod(need);
                float before = need.CurLevel;
                if (method != null)
                {
                    for (int i = 0; i < maxDumpCalls && need.CurLevelPercentage < target; i++)
                    {
                        float beforeCall = need.CurLevel;
                        method.Invoke(need, null);
                        if (need.CurLevel <= beforeCall)
                        {
                            break;
                        }

                        dumpCallsUsed++;
                    }
                }
                else
                {
                    float maxLevel = SafeMaxLevel(need);
                    float deltaLevel = Math.Min(0.003f * maxDumpCalls, (target * maxLevel) - need.CurLevel);
                    if (deltaLevel > 0f)
                    {
                        need.CurLevel = Math.Min(need.CurLevel + deltaLevel, target * maxLevel);
                        dumpCallsUsed = maxDumpCalls;
                    }
                }

                return need.CurLevel > before;
            }
            catch (Exception exception)
            {
                lastFailureReason = DescribeException("Bladder dump failed", exception);
                return false;
            }
        }

        private static NeedDef ResolveNeedDef(string defName)
        {
            return !string.IsNullOrWhiteSpace(defName)
                ? DefDatabase<NeedDef>.GetNamedSilentFail(defName.Trim())
                : null;
        }

        private Need TryGetNeed(Pawn pawn, NeedDef def)
        {
            if (pawn == null || pawn.needs == null || def == null || !Resolve())
            {
                return null;
            }

            return pawn.needs.TryGetNeed(def);
        }

        private MethodInfo ResolveHygieneCleanMethod(Need need)
        {
            Type type = need != null ? need.GetType() : null;
            if (type == null)
            {
                return null;
            }

            if (hygieneCleanMethod != null && hygieneNeedType == type)
            {
                return hygieneCleanMethod;
            }

            hygieneNeedType = type;
            hygieneCleanMethod = FindMethod(type, "clean", typeof(float));
            return hygieneCleanMethod;
        }

        private MethodInfo ResolveBladderDumpMethod(Need need)
        {
            Type type = need != null ? need.GetType() : null;
            if (type == null)
            {
                return null;
            }

            if (bladderDumpMethod != null && bladderNeedType == type)
            {
                return bladderDumpMethod;
            }

            bladderNeedType = type;
            bladderDumpMethod = FindMethod(type, "dump");
            return bladderDumpMethod;
        }

        private static MethodInfo FindMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (type != null)
            {
                MethodInfo method = type.GetMethod(methodName, Flags, null, parameterTypes ?? Type.EmptyTypes, null);
                if (method != null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static float SafeMaxLevel(Need need)
        {
            return need != null && need.MaxLevel > 0f ? need.MaxLevel : 1f;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static string DescribeException(string prefix, Exception exception)
        {
            TargetInvocationException invocationException = exception as TargetInvocationException;
            Exception actual = invocationException != null && invocationException.InnerException != null
                ? invocationException.InnerException
                : exception;
            return prefix + ": " + actual.GetType().Name;
        }

        private static string NeedName(string configured, string fallback)
        {
            return string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
        }
    }
}

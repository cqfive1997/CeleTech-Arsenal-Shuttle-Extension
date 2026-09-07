using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadTransferableMetrics
    {
        internal static readonly V3CargoLoadTransferableMetrics Empty =
            new V3CargoLoadTransferableMetrics
            {
                MaxCount = 0,
                RawCountLong = 0L,
                UnitMass = 0f,
                TotalMass = 0f,
                Label = "-",
                LabelWithoutCount = "-",
                TypeLabel = "-",
                SearchText = string.Empty
            };

        internal Thing DisplayThing;
        internal int MaxCount;
        internal long RawCountLong;
        internal float UnitMass;
        internal float TotalMass;
        internal string Label;
        internal string LabelWithoutCount;
        internal string TypeLabel;
        internal string SearchText;
    }

    internal sealed class V3CargoLoadTransferableMetricsResolver
    {
        private readonly Dictionary<TransferableOneWay, V3CargoLoadTransferableMetrics> cache =
            new Dictionary<TransferableOneWay, V3CargoLoadTransferableMetrics>();
        private readonly Dictionary<TransferableOneWay, string> searchTextCache =
            new Dictionary<TransferableOneWay, string>();

        internal void Clear()
        {
            this.cache.Clear();
            this.searchTextCache.Clear();
        }

        internal V3CargoLoadTransferableMetrics GetMetrics(TransferableOneWay transferable)
        {
            if (transferable == null)
            {
                return V3CargoLoadTransferableMetrics.Empty;
            }

            V3CargoLoadTransferableMetrics metrics;
            if (this.cache.TryGetValue(transferable, out metrics))
            {
                return metrics;
            }

            metrics = this.BuildMetrics(transferable);
            this.cache[transferable] = metrics;
            return metrics;
        }

        internal Thing GetDisplayThing(TransferableOneWay transferable)
        {
            return GetDisplayThingRaw(transferable);
        }

        internal string GetSearchText(TransferableOneWay transferable)
        {
            if (transferable == null)
            {
                return string.Empty;
            }

            string cached;
            if (this.searchTextCache.TryGetValue(transferable, out cached))
            {
                return cached;
            }

            cached = this.BuildSearchTextWithoutMass(transferable);
            this.searchTextCache[transferable] = cached;
            return cached;
        }

        internal int GetMaxCount(TransferableOneWay transferable)
        {
            return this.GetMetrics(transferable).MaxCount;
        }

        internal float GetUnitMass(TransferableOneWay transferable)
        {
            return this.GetMetrics(transferable).UnitMass;
        }

        internal float GetSelectedMassForTransferable(TransferableOneWay transferable)
        {
            if (transferable == null)
            {
                return 0f;
            }

            return CalculateMass(this.GetUnitMass(transferable), transferable.CountToTransfer);
        }

        private V3CargoLoadTransferableMetrics BuildMetrics(TransferableOneWay transferable)
        {
            V3CargoLoadTransferableMetrics metrics = new V3CargoLoadTransferableMetrics();
            metrics.DisplayThing = GetDisplayThingRaw(transferable);
            metrics.TypeLabel = this.ResolveTypeLabel(metrics.DisplayThing);

            long rawCount = 0L;
            double totalMass = 0.0;
            if (transferable != null && transferable.things != null)
            {
                for (int i = 0; i < transferable.things.Count; i++)
                {
                    Thing thing = transferable.things[i];
                    if (thing == null)
                    {
                        continue;
                    }

                    int stackCount = thing.stackCount > 0 ? thing.stackCount : 1;
                    if (rawCount <= long.MaxValue - stackCount)
                    {
                        rawCount += stackCount;
                    }
                    else
                    {
                        rawCount = long.MaxValue;
                    }

                    float mass = thing.GetStatValue(StatDefOf.Mass, true, -1);
                    if (mass > 0f && !float.IsNaN(mass) && !float.IsInfinity(mass))
                    {
                        totalMass += (double)mass * stackCount;
                    }
                }
            }

            metrics.RawCountLong = rawCount;
            metrics.MaxCount = rawCount > int.MaxValue ? int.MaxValue : (int)rawCount;
            metrics.TotalMass = ClampMass(totalMass);
            metrics.UnitMass = rawCount > 0L ? ClampMass(totalMass / rawCount) : 0f;
            metrics.LabelWithoutCount = this.ResolveDisplayLabelWithoutCount(metrics.DisplayThing);
            metrics.Label = this.ResolveAggregateDisplayLabel(
                metrics.LabelWithoutCount,
                metrics.DisplayThing,
                metrics);
            metrics.SearchText = string.Empty;
            return metrics;
        }

        private string BuildSearchTextWithoutMass(TransferableOneWay transferable)
        {
            Thing displayThing = GetDisplayThingRaw(transferable);
            StringBuilder builder = new StringBuilder();
            this.AppendSearchPart(builder, this.ResolveDisplayLabel(displayThing));
            this.AppendSearchPart(builder, this.ResolveTypeLabel(displayThing));

            if (transferable == null || transferable.things == null)
            {
                return builder.ToString();
            }

            int limit = transferable.things.Count;
            if (limit > 16)
            {
                limit = 16;
            }

            for (int i = 0; i < limit; i++)
            {
                Thing thing = transferable.things[i];
                if (thing == null)
                {
                    continue;
                }

                this.AppendSearchPart(builder, thing.LabelCap.ToString());
                if (thing.def != null)
                {
                    this.AppendSearchPart(builder, thing.def.defName);
                    this.AppendSearchPart(builder, thing.def.label);
                }

                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    this.AppendSearchPart(builder, pawn.LabelShortCap);
                    if (pawn.Name != null)
                    {
                        this.AppendSearchPart(builder, pawn.Name.ToStringFull);
                        this.AppendSearchPart(builder, pawn.Name.ToStringShort);
                    }

                    if (pawn.kindDef != null)
                    {
                        this.AppendSearchPart(builder, pawn.kindDef.defName);
                        this.AppendSearchPart(builder, pawn.kindDef.label);
                    }
                }
            }

            return builder.ToString();
        }

        private void AppendSearchPart(StringBuilder builder, string value)
        {
            if (builder == null || string.IsNullOrEmpty(value))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(value.ToLowerInvariant());
        }

        private string ResolveDisplayLabel(Thing thing)
        {
            if (thing == null)
            {
                return "-";
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                if (!string.IsNullOrEmpty(pawn.LabelShortCap))
                {
                    return StripRichTextTags(pawn.LabelShortCap);
                }

                if (pawn.Name != null)
                {
                    return StripRichTextTags(pawn.Name.ToStringShort);
                }
            }

            return StripRichTextTags(thing.LabelCap.ToString());
        }

        private string ResolveDisplayLabelWithoutCount(Thing thing)
        {
            if (thing == null)
            {
                return "-";
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return this.ResolveDisplayLabel(pawn);
            }

            return StripRichTextTags(thing.LabelCapNoCount.ToString());
        }

        private string ResolveAggregateDisplayLabel(
            string labelWithoutCount,
            Thing thing,
            V3CargoLoadTransferableMetrics metrics)
        {
            if (thing == null || metrics == null)
            {
                return "-";
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return labelWithoutCount;
            }

            if (metrics.RawCountLong <= 1L)
            {
                return labelWithoutCount;
            }

            return labelWithoutCount + " x" + V3CargoLoadText.FormatCount(metrics);
        }

        private string ResolveTypeLabel(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                if (pawn.kindDef != null && !string.IsNullOrEmpty(pawn.kindDef.label))
                {
                    return pawn.kindDef.LabelCap;
                }

                if (pawn.def != null && !string.IsNullOrEmpty(pawn.def.label))
                {
                    return pawn.def.LabelCap;
                }
            }

            if (thing != null && thing.def != null)
            {
                if (!string.IsNullOrEmpty(thing.def.label))
                {
                    return thing.def.LabelCap;
                }

                if (!string.IsNullOrEmpty(thing.def.defName))
                {
                    return thing.def.defName;
                }
            }

            return "-";
        }

        private static Thing GetDisplayThingRaw(TransferableOneWay transferable)
        {
            if (transferable == null || transferable.things == null || transferable.things.Count == 0)
            {
                return null;
            }

            return transferable.things[0];
        }

        private static string StripRichTextTags(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('<') < 0)
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            bool insideTag = false;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (current == '>')
                {
                    insideTag = false;
                    continue;
                }

                if (!insideTag)
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }

        internal static float CalculateMass(float unitMass, int count)
        {
            if (unitMass <= 0f || count <= 0)
            {
                return 0f;
            }

            return ClampMass((double)unitMass * count);
        }

        private static float ClampMass(double mass)
        {
            if (double.IsNaN(mass) || mass <= 0.0)
            {
                return 0f;
            }

            if (double.IsInfinity(mass) || mass > float.MaxValue)
            {
                return float.MaxValue;
            }

            return (float)mass;
        }
    }
}

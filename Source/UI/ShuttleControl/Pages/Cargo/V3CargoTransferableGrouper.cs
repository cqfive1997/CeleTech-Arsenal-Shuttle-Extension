using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoTransferableGrouper
    {
        internal void GroupInPlace(List<V3CargoStackCardModel> stacks)
        {
            if (stacks == null || stacks.Count < 2)
            {
                return;
            }

            List<V3CargoStackCardModel> grouped =
                new List<V3CargoStackCardModel>(stacks.Count);
            Dictionary<ThingDef, List<V3CargoStackCardModel>> groupsByDef =
                new Dictionary<ThingDef, List<V3CargoStackCardModel>>();

            for (int i = 0; i < stacks.Count; i++)
            {
                V3CargoStackCardModel candidate = stacks[i];
                if (candidate == null)
                {
                    continue;
                }

                ThingDef bucketDef = candidate.DisplayThing != null
                    ? candidate.DisplayThing.def
                    : null;
                List<V3CargoStackCardModel> bucket;
                if (bucketDef == null || !groupsByDef.TryGetValue(bucketDef, out bucket))
                {
                    bucket = new List<V3CargoStackCardModel>();
                    if (bucketDef != null)
                    {
                        groupsByDef[bucketDef] = bucket;
                    }
                }

                V3CargoStackCardModel target = this.FindCompatibleGroup(
                    bucket,
                    candidate);
                if (target == null)
                {
                    grouped.Add(candidate);
                    bucket.Add(candidate);
                    continue;
                }

                this.MergeInto(target, candidate);
            }

            stacks.Clear();
            stacks.AddRange(grouped);
        }

        private V3CargoStackCardModel FindCompatibleGroup(
            List<V3CargoStackCardModel> groups,
            V3CargoStackCardModel candidate)
        {
            for (int i = 0; groups != null && i < groups.Count; i++)
            {
                V3CargoStackCardModel existing = groups[i];
                if (this.CanGroup(existing, candidate))
                {
                    return existing;
                }
            }

            return null;
        }

        private bool CanGroup(
            V3CargoStackCardModel existing,
            V3CargoStackCardModel candidate)
        {
            return existing != null &&
                candidate != null &&
                existing.SourceKind == candidate.SourceKind &&
                existing.DisplayThing != null &&
                candidate.DisplayThing != null &&
                TransferableUtility.TransferAsOne(
                    existing.DisplayThing,
                    candidate.DisplayThing,
                    TransferAsOneMode.PodsOrCaravanPacking);
        }

        private void MergeInto(
            V3CargoStackCardModel target,
            V3CargoStackCardModel source)
        {
            target.StackCount = AddClamped(target.StackCount, source.StackCount);
            target.MassKg += source.MassKg;
            for (int i = 0; source.Members != null && i < source.Members.Count; i++)
            {
                V3CargoStackMemberModel member = source.Members[i];
                if (member != null)
                {
                    target.Members.Add(member);
                }
            }
        }

        private static int AddClamped(int left, int right)
        {
            long total = (long)left + right;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }
    }
}

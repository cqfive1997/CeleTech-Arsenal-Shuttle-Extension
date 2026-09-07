using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal static class ShuttleCargoStackMemberActionUtility
    {
        internal static bool HasValidMembers(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoStackActionSourceKind sourceKind)
        {
            if (target == null || target.StackCount <= 0 || target.SourceKind != sourceKind)
            {
                return false;
            }

            if (target.Members == null || target.Members.Count == 0)
            {
                return IsValidMember(BuildRepresentativeMember(target), sourceKind);
            }

            int total = 0;
            for (int i = 0; i < target.Members.Count; i++)
            {
                ShuttleCargoStackMemberActionTarget member = target.Members[i];
                if (!IsValidMember(member, sourceKind))
                {
                    return false;
                }

                total = AddClamped(total, member.StackCount);
            }

            return total == target.StackCount;
        }

        internal static int CountMembers(ShuttleCargoStackActionTarget target)
        {
            return target != null && target.Members != null && target.Members.Count > 0
                ? target.Members.Count
                : target != null && target.StackCount > 0 ? 1 : 0;
        }

        internal static bool TryBuildSlices(
            ShuttleCargoStackActionTarget target,
            int requestedCount,
            out List<ShuttleCargoStackMemberActionTarget> slices)
        {
            slices = new List<ShuttleCargoStackMemberActionTarget>();
            if (target == null ||
                requestedCount <= 0 ||
                target.StackCount <= 0 ||
                requestedCount > target.StackCount)
            {
                return false;
            }

            int remaining = requestedCount;
            if (target.Members == null || target.Members.Count == 0)
            {
                ShuttleCargoStackMemberActionTarget representative =
                    BuildRepresentativeMember(target);
                if (!IsValidMember(representative, target.SourceKind))
                {
                    return false;
                }

                representative.StackCount = requestedCount;
                slices.Add(representative);
                return true;
            }

            for (int i = 0; i < target.Members.Count && remaining > 0; i++)
            {
                ShuttleCargoStackMemberActionTarget member = target.Members[i];
                if (!IsValidMember(member, target.SourceKind))
                {
                    slices.Clear();
                    return false;
                }

                int count = member.StackCount < remaining
                    ? member.StackCount
                    : remaining;
                slices.Add(CopyMember(member, count));
                remaining -= count;
            }

            if (remaining != 0)
            {
                slices.Clear();
                return false;
            }

            return true;
        }

        internal static string ResolveColdModuleInstanceID(
            ShuttleCargoStackActionTarget target,
            List<ShuttleCargoStackMemberActionTarget> members)
        {
            if (target != null && !string.IsNullOrEmpty(target.ModuleInstanceID))
            {
                return target.ModuleInstanceID;
            }

            for (int i = 0; members != null && i < members.Count; i++)
            {
                ShuttleCargoStackMemberActionTarget member = members[i];
                if (member != null && !string.IsNullOrEmpty(member.ModuleInstanceID))
                {
                    return member.ModuleInstanceID;
                }
            }

            return null;
        }

        private static bool IsValidMember(
            ShuttleCargoStackMemberActionTarget member,
            ShuttleCargoStackActionSourceKind sourceKind)
        {
            if (member == null ||
                member.SourceKind != sourceKind ||
                member.StackCount <= 0 ||
                member.ThingIDNumber <= 0 ||
                string.IsNullOrEmpty(member.DefName))
            {
                return false;
            }

            if (sourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                return member.TransporterIndex >= 0 && member.LoadedIndex >= 0;
            }

            if (sourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                return !string.IsNullOrEmpty(member.ModuleInstanceID) &&
                    member.ColdIndex >= 0;
            }

            return false;
        }

        private static ShuttleCargoStackMemberActionTarget BuildRepresentativeMember(
            ShuttleCargoStackActionTarget target)
        {
            return target == null
                ? null
                : new ShuttleCargoStackMemberActionTarget
                {
                    SourceKind = target.SourceKind,
                    DefName = target.DefName,
                    StackCount = target.StackCount,
                    TransporterIndex = target.TransporterIndex,
                    LoadedIndex = target.LoadedIndex,
                    ThingIDNumber = target.ThingIDNumber,
                    ModuleInstanceID = target.ModuleInstanceID,
                    ColdIndex = target.ColdIndex
                };
        }

        private static ShuttleCargoStackMemberActionTarget CopyMember(
            ShuttleCargoStackMemberActionTarget source,
            int count)
        {
            return new ShuttleCargoStackMemberActionTarget
            {
                SourceKind = source.SourceKind,
                DefName = source.DefName,
                StackCount = count,
                TransporterIndex = source.TransporterIndex,
                LoadedIndex = source.LoadedIndex,
                ThingIDNumber = source.ThingIDNumber,
                ModuleInstanceID = source.ModuleInstanceID,
                ColdIndex = source.ColdIndex
            };
        }

        private static int AddClamped(int left, int right)
        {
            long total = (long)left + right;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }
    }
}

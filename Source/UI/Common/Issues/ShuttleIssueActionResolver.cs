using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueActionResolver
    {
        internal static ShuttleIssueActionKind ResolveCurrentActionKind(
            ShuttleIssueCategory category,
            string code,
            string scope)
        {
            string text = ((scope ?? string.Empty) + " " + (code ?? string.Empty)).ToLowerInvariant();
            if (category == ShuttleIssueCategory.Cargo)
            {
                return ShuttleIssueActionKind.OpenCargo;
            }

            if (category == ShuttleIssueCategory.Loading ||
                category == ShuttleIssueCategory.Crew)
            {
                return ShuttleIssueActionKind.OpenLoading;
            }

            if (category == ShuttleIssueCategory.Assembly)
            {
                return ShuttleIssueClassifier.IsDefenseIssueText(text) &&
                    !ShuttleIssueClassifier.IsAssemblyInstallIssueText(text)
                    ? ShuttleIssueActionKind.OpenDefense
                    : ShuttleIssueActionKind.OpenAssembly;
            }

            if (category == ShuttleIssueCategory.Power)
            {
                return ShuttleIssueActionKind.OpenAssembly;
            }

            if (category == ShuttleIssueCategory.Defense)
            {
                return ShuttleIssueActionKind.OpenDefense;
            }

            if (category == ShuttleIssueCategory.Medical)
            {
                return ShuttleIssueActionKind.OpenMedical;
            }

            if (category == ShuttleIssueCategory.PrisonCell)
            {
                return ShuttleIssueActionKind.OpenPrisonCell;
            }

            if (category == ShuttleIssueCategory.Launch)
            {
                return ShuttleIssueActionKind.OpenLaunch;
            }

            if (ShuttleIssueClassifier.IsDefenseIssueText(text))
            {
                return ShuttleIssueActionKind.OpenDefense;
            }

            return ShuttleIssueActionKind.None;
        }

        internal static ShuttleIssueActionKind ResolveLaunchActionKind(
            string category,
            string targetPage,
            string code)
        {
            string text = ((category ?? string.Empty) + " " +
                (targetPage ?? string.Empty) + " " +
                (code ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("cargo"))
            {
                return ShuttleIssueActionKind.OpenCargo;
            }

            if (text.Contains("load") || text.Contains("crew"))
            {
                return ShuttleIssueActionKind.OpenLoading;
            }

            if (text.Contains("assembly") ||
                text.Contains("segment") ||
                text.Contains("module"))
            {
                return ShuttleIssueClassifier.IsDefenseIssueText(text) &&
                    !ShuttleIssueClassifier.IsAssemblyInstallIssueText(text)
                    ? ShuttleIssueActionKind.OpenDefense
                    : ShuttleIssueActionKind.OpenAssembly;
            }

            if (text.Contains("power") || text.Contains("energy") || text.Contains("battery"))
            {
                return ShuttleIssueActionKind.OpenAssembly;
            }

            if (text.Contains("medical"))
            {
                return ShuttleIssueActionKind.OpenMedical;
            }

            if (text.Contains("prison"))
            {
                return ShuttleIssueActionKind.OpenPrisonCell;
            }

            if (text.Contains("launch"))
            {
                return ShuttleIssueActionKind.OpenLaunch;
            }

            if (ShuttleIssueClassifier.IsDefenseIssueText(text))
            {
                return ShuttleIssueActionKind.OpenDefense;
            }

            return ShuttleIssueActionKind.OpenAssembly;
        }

        internal static string ResolveActionLabel(ShuttleIssueActionKind actionKind)
        {
            if (actionKind == ShuttleIssueActionKind.OpenCargo)
            {
                return "CT_Shuttle_Issue_Action_OpenCargo".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenLoading)
            {
                return "CT_Shuttle_Issue_Action_OpenLoading".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenAssembly)
            {
                return "CT_Shuttle_Issue_Action_OpenAssembly".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenLaunch)
            {
                return "CT_Shuttle_Issue_Action_OpenLaunch".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenDefense)
            {
                return "CT_Shuttle_Issue_Action_OpenDefense".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenMedical)
            {
                return "CT_Shuttle_Issue_Action_OpenMedical".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenPrisonCell)
            {
                return "CT_Shuttle_Issue_Action_OpenPrisonCell".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenProcessing)
            {
                return "CT_Shuttle_Issue_Action_OpenProcessing".Translate().ToString();
            }

            if (actionKind == ShuttleIssueActionKind.OpenExternalModule)
            {
                return "CT_Shuttle_Issue_Action_OpenExternalModule".Translate().ToString();
            }

            return null;
        }
    }
}

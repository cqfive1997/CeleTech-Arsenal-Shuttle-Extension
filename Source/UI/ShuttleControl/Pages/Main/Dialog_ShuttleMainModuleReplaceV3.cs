using System;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class Dialog_ShuttleMainModuleReplaceV3 :
        Dialog_ShuttleMainModuleSelectionV3
    {
        internal Dialog_ShuttleMainModuleReplaceV3(
            Func<List<V3MainInstallCandidateModel>> candidateProvider,
            Func<int> getResearchFingerprint,
            Action<V3MainInstallCandidateModel> onInstall)
            : base(candidateProvider, getResearchFingerprint, onInstall, true)
        {
        }
    }
}

using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    public sealed class ShuttleControlIssueModel
    {
        public string Code;
        public string Message;
        public string Severity;
        public string Scope;
        public string ReferenceID;
    }

    public sealed class ShuttleLaunchChecklistItemModel
    {
        public string Code;
        public string Message;
        public string Severity;
        public string Category;
        public string ReferenceID;
        public bool BlocksLaunch;
        public bool IsRiskOnly;
        public string MessageKey;
        public List<string> MessageArgs;
        public string DisplayName;
        public string DisplayNameKey;
        public string ActionText;
        public string TargetPage;
        public float Progress01 = -1f;
        public int SortPriority;
        public string Tooltip;
    }

    public sealed class ShuttleDeveloperDiagnosticModel
    {
        public string Code;
        public string Message;
        public string Kind;
        public string Severity;
        public string Scope;
        public string ReferenceID;
        public int Tick;
        public float? ElapsedMs;
        public string Tooltip;
    }
}

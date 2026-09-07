using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleIssuePanelSpec
    {
        internal readonly string Title;
        internal readonly string EmptyMessage;
        internal readonly IList<ShuttleIssueRowSpec> Rows;

        internal ShuttleIssuePanelSpec(
            string title,
            string emptyMessage,
            IList<ShuttleIssueRowSpec> rows)
        {
            this.Title = title;
            this.EmptyMessage = emptyMessage;
            this.Rows = rows;
        }
    }
}

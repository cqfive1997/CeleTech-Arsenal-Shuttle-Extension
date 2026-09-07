namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    public sealed class ShuttleAssemblyIntegrityIssue
    {
        public ShuttleAssemblyIntegrityIssue(
            string code,
            string referenceID,
            string developerDetail,
            bool blocking)
        {
            this.Code = code;
            this.ReferenceID = referenceID;
            this.DeveloperDetail = developerDetail;
            this.Blocking = blocking;
        }

        public string Code { get; private set; }

        public string ReferenceID { get; private set; }

        public string DeveloperDetail { get; private set; }

        public bool Blocking { get; private set; }
    }
}

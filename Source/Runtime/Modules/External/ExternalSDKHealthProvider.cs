using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKHealthProvider : IShuttleExternalIntegrationHealthProvider
    {
        private static readonly ExternalSDKHealthProvider instance = new ExternalSDKHealthProvider();

        private ExternalSDKHealthProvider()
        {
        }

        internal static ExternalSDKHealthProvider Instance
        {
            get
            {
                return instance;
            }
        }

        public ShuttleExternalIntegrationHealthReport GetIntegrationHealthReport()
        {
            return ExternalSDKHealthReportBuilder.Build(null, null);
        }
    }
}

using System;
using System.Configuration;

namespace NID.Areas.ElectionTransport.Infrastructure
{
    public static class TransportModuleOptions
    {
        public static string ConnectionStringName
        {
            get
            {
                return Read("ElectionTransport.ConnectionStringName", "NIDEntities");
            }
        }

        public static string IntegrationApiKey
        {
            get { return Read("ElectionTransport.IntegrationApiKey", string.Empty); }
        }

        public static int DashboardRefreshSeconds
        {
            get { return ReadInt("ElectionTransport.DashboardRefreshSeconds", 10, 3, 300); }
        }

        public static int DemoTickSeconds
        {
            get { return ReadInt("ElectionTransport.DemoTickSeconds", 5, 2, 60); }
        }

        public static int TrailMinutes
        {
            get { return ReadInt("ElectionTransport.TrailMinutes", 15, 5, 180); }
        }

        public static int DefaultMapZoom
        {
            get { return ReadInt("ElectionTransport.DefaultMapZoom", 12, 4, 19); }
        }

        public static bool AllowDemoAdministration
        {
            get { return ReadBool("ElectionTransport.AllowDemoAdministration", true); }
        }

        private static string Read(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int ReadInt(string key, int fallback, int minimum, int maximum)
        {
            int value;
            if (!int.TryParse(ConfigurationManager.AppSettings[key], out value))
            {
                value = fallback;
            }

            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool ReadBool(string key, bool fallback)
        {
            bool value;
            return bool.TryParse(ConfigurationManager.AppSettings[key], out value)
                ? value
                : fallback;
        }
    }
}

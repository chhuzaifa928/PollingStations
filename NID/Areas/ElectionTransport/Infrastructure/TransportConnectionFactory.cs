using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

namespace NID.Areas.ElectionTransport.Infrastructure
{
    public interface ITransportConnectionFactory
    {
        SqlConnection Create();
    }

    public sealed class TransportConnectionFactory : ITransportConnectionFactory
    {
        public SqlConnection Create()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[TransportModuleOptions.ConnectionStringName];

            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "Connection string '" + TransportModuleOptions.ConnectionStringName +
                    "' was not found. Add ElectionTransport.ConnectionStringName to Web.config or create an ElectionTransportConnection connection string.");
            }

            string connectionString = settings.ConnectionString;

            if (string.Equals(settings.ProviderName, "System.Data.EntityClient", StringComparison.OrdinalIgnoreCase)
                || connectionString.IndexOf("metadata=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                EntityConnectionStringBuilder entityBuilder =
                    new EntityConnectionStringBuilder(connectionString);
                connectionString = entityBuilder.ProviderConnectionString;
            }

            return new SqlConnection(connectionString);
        }
    }
}

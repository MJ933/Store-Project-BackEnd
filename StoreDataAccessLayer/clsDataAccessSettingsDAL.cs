using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace StoreDataAccessLayer
{
    public static class clsDataAccessSettingsDAL
    {
        private static string _connectionString;

        public static void Initialize(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
            }
        }

        public static NpgsqlDataSource CreateDataSource()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string has not been initialized. Call Initialize() first.");
            }

            return NpgsqlDataSource.Create(_connectionString);
        }
    }
}
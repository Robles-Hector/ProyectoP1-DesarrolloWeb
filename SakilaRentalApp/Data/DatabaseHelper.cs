using Microsoft.Data.SqlClient;
using System.Data;

namespace SakilaRentalApp.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SakilaConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'SakilaConnection'.");
        }

        /// <summary>
        /// Obtiene y abre una conexión a SQL Server de manera segura.
        /// </summary>
        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// Obtiene una conexión síncrona a SQL Server.
        /// </summary>
        public SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public string ConnectionString => _connectionString;
    }
}

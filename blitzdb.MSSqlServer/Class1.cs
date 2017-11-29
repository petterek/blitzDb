using System;
using System.Data;
using System.Data.SqlClient;

namespace blitzdb.MSSqlServer
{
    public class ConnectionManager
    {
        readonly string connectionString;
        SqlConnection sqlConnection;

        public ConnectionManager(string connectionString)
        {
            this.connectionString = connectionString;
            sqlConnection = new SqlConnection(connectionString);
        }


        public SqlConnection Connection => sqlConnection;
        
        public SqlCommand CreateCommand(string commandText)
        {
            return new SqlCommand(commandText, sqlConnection);
        }

    }
}

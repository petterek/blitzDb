using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace blitzdb.SqlServer
{

    public class SqlDBAbstraction : SqlDBReaderAbstraction, ISqlServerDBAbstraction
    {
        public SqlDBAbstraction(SqlConnection conn) : base(conn)
        {
        }

        public async Task ExecuteAsync(SqlCommand dbCommand)
        {
            dbCommand.Connection = (System.Data.SqlClient.SqlConnection)this.con;

            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    await dbCommand.ExecuteNonQueryAsync();
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                await dbCommand.ExecuteNonQueryAsync();
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(SqlCommand dbCommand)
        {
            object ret;
            dbCommand.Connection = (System.Data.SqlClient.SqlConnection)con;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    ret = await dbCommand.ExecuteScalarAsync();
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                ret = await dbCommand.ExecuteScalarAsync();
            }

            return (T)ret;
        }
    }
}
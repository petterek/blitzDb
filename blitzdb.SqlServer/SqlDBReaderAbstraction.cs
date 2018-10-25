using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace blitzdb.SqlServer
{
    public class SqlDBReaderAbstraction : DBReaderAbstraction
    {
        public SqlDBReaderAbstraction(SqlConnection conn) : base(conn)
        {
        }

        public async Task FillAsync(SqlCommand dbCommand, object toFill)
        {
            dbCommand.Connection = (System.Data.SqlClient.SqlConnection)con;
            var help = new Helpers(toFill.GetType(), dbCommand.CommandText);

            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    var res = await dbCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    help.Fill(toFill, res);
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(toFill, res);
            }
        }

        public async Task<T> FillAsync<T>(SqlCommand dbCommand) where T : new()
        {
            var toFill = Activator.CreateInstance<T>();
            dbCommand.Connection = (System.Data.SqlClient.SqlConnection)con;

            var help = new Helpers(toFill.GetType(), dbCommand.CommandText);

            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    var res = await dbCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                    if (!help.Fill(toFill, res))
                    {
                        toFill = default(T);
                    }
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(toFill, res);
            }
            return toFill;
        }

        public async Task<T> RehydrateAsync<T>(SqlCommand dbCommand)
        {
            dbCommand.Connection = (System.Data.SqlClient.SqlConnection)con;
            var help = new Helpers(typeof(T), dbCommand.CommandText);
            object ret;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    var res = await dbCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    ret = help.Rehydrate(res);
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                ret = help.Rehydrate(res);
            }

            return (T)ret;
        }
    }
}
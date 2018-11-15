using System.Data;

namespace blitzdb
{

    public class DBAbstraction : DBReaderAbstraction, IDBAbstraction
    {
        public DBAbstraction(IDbConnection con) : base(con)
        {
        }

        public void Execute(IDbCommand dbCommand)
        {
            dbCommand.Connection = con;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    dbCommand.ExecuteNonQuery();
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                dbCommand.ExecuteNonQuery();
            }
        }

        public T ExecuteScalar<T>(IDbCommand dbCommand)
        {
            object ret;
            dbCommand.Connection = con;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    ret = dbCommand.ExecuteScalar();
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                ret = dbCommand.ExecuteScalar();
            }

            return (T)ret;
        }
    }
}
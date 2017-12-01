using System;
using System.Data;

namespace blitzdb
{



    public class DBReaderAbstrction : IDbReaderAbstraction
    {
        protected IDbConnection con;

        public DBReaderAbstrction(IDbConnection con)
        {
            this.con = con;
        }

        public void Fill(IDbCommand dbCommand, object toFill)
        {

            dbCommand.Connection = con;
            var help = new Helpers(toFill.GetType(), dbCommand.CommandText);

            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(toFill, res);
                con.Close();
            }
            else
            {
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(toFill, res);
            }

        }
    }

    public class DBAbstraction : DBReaderAbstrction, IDBAbstraction
    {


        public DBAbstraction(IDbConnection con) : base(con) { }
        
        public void Execute(IDbCommand dbCommand)
        {
            dbCommand.Connection = con;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                dbCommand.ExecuteNonQuery();
                con.Close();
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
                ret = dbCommand.ExecuteScalar();
                con.Close();
            }
            else
            {
                ret = dbCommand.ExecuteScalar();
            }

            return (T)ret;
        }

    }
}

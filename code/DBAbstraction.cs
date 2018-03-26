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

        public T Fill<T>(IDbCommand dbCommand) where T : new()
        {
            var ret = Activator.CreateInstance<T>();
            Fill(dbCommand, ret);
            return ret;
        }

        public T Rehydrate<T>(IDbCommand dbCommand)
        {
            dbCommand.Connection = con;
            var help = new Helpers(typeof(T), dbCommand.CommandText);
            object ret;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                ret = help.Rehydrate(res);
                con.Close();
            }
            else
            {
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                ret = help.Rehydrate(res);
            }

            return (T)ret;
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

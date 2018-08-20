using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using static blitzdb.ExtensionMethods;

namespace blitzdb
{
    public class DBReaderAbstraction : IDbReaderAbstraction
    {
        public readonly IDbConnection con;

        public DBReaderAbstraction(IDbConnection con)
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
                try
                {
                    CheckForSplitting(dbCommand, toFill, (data, reader) => help.Fill(data, reader));
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                CheckForSplitting(dbCommand, toFill, (data, reader) => help.Fill(data, reader));
            }
        }

        public T Fill<T>(IDbCommand dbCommand) where T : new()
        {
            var toFill = Activator.CreateInstance<T>();
            dbCommand.Connection = con;

            var help = new Helpers(toFill.GetType(), dbCommand.CommandText);

            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    CheckForSplitting(dbCommand, toFill, (data, reader) => help.Fill(data, reader));
                    if (!help.DataRead)
                    {
                        if (typeof(IList).IsAssignableFrom(typeof(T))) //no result on lists gives empty list as result.
                        {
                            toFill = (T)Activator.CreateInstance(typeof(T));
                        }
                        else
                        {
                            toFill = default(T);
                        }
                    }
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                CheckForSplitting(dbCommand, toFill, (data, reader) => help.Fill(data, reader));
            }
            return toFill;
        }

        public T Rehydrate<T>(IDbCommand dbCommand)
        {
            dbCommand.Connection = con;
            var help = new Helpers(typeof(T), dbCommand.CommandText);
            object ret = null;
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                try
                {
                    CheckForSplitting(dbCommand, null, (data, reader) => ret = help.Rehydrate(reader));
                }
                finally
                {
                    con.Close();
                }
            }
            else
            {
                CheckForSplitting(dbCommand, null, (data, reader) => ret = help.Rehydrate(reader));
            }

            return (T)ret;
        }

        private void CheckForSplitting(IDbCommand dbCommand, object toFill, Action<object, IDataReader> callback)
        {
            IDbDataParameter toSplit = null;

            foreach (IDbDataParameter p in dbCommand.Parameters)
            {
                if (p.Value.GetType() == typeof(ExpandableValue))
                {
                    toSplit = p;
                    dbCommand.Parameters.Remove(p);
                    break;
                }
            }

            if (toSplit != null)
            {
                var orgCmdText = dbCommand.CommandText;
                var paramCount = dbCommand.Parameters.Count;
                int x = 0;
                int thisRun = 0;
                List<string> names = new List<string>();
                var theValue = ((ExpandableValue)toSplit.Value);

                foreach (var el in theValue.ValueList)
                {
                    thisRun++;
                    var p = dbCommand.CreateParameter();
                    p.Value = el;
                    p.DbType = toSplit.DbType;

                    string v = $"{toSplit.ParameterName}_{x}";
                    names.Add($"@{v}");
                    p.ParameterName = v;

                    dbCommand.Parameters.Add(p);
                    x++;
                    if (thisRun >= theValue.SplitSize)
                    {
                        dbCommand.CommandText = orgCmdText.Replace($"@{toSplit.ParameterName}", string.Join(",", names.ToArray()));
                        ExecAndFill(dbCommand, toFill, callback);

                        while (dbCommand.Parameters.Count > paramCount)
                        {
                            dbCommand.Parameters.RemoveAt(paramCount);
                        }
                        thisRun = 0;
                        names = new List<string>();
                    }
                }
                if (thisRun > 0)
                {
                    dbCommand.CommandText = orgCmdText.Replace($"@{toSplit.ParameterName}", string.Join(",", names.ToArray()));
                    ExecAndFill(dbCommand, toFill, callback);
                }
            }
            else
            {
                ExecAndFill(dbCommand, toFill, callback);
            }
        }

        private static void ExecAndFill(IDbCommand dbCommand, object toFill, Action<object, IDataReader> callback)
        {
            var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
            callback(toFill, res);
            //help.Fill(toFill, res);
        }
    }

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
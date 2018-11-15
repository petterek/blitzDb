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
            var toFill = Helpers.CreateInstance<T>();
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
                            toFill = Helpers.CreateInstance<T>();
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
                if(p.Value != null)
                {
                    if (p.Value.GetType() == typeof(ExpandableValue))
                    {
                        toSplit = p;
                        dbCommand.Parameters.Remove(p);
                        break;
                    }
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


        private List<object> FillManyGeneric(IDbCommand dbCommand, List<Type> toFill)
        {
            var cmds = dbCommand.CommandText.Trim().Split(';');
            dbCommand.Connection = con;
            bool manageConnection = (con.State == ConnectionState.Closed);
            var ret = new List<object>();

            if (cmds.Length - 1 != toFill.Count) throw new NotSupportedException("Number of commands and types must be equal");

            if (manageConnection) con.Open();

            var dbResult = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
            try
            {
                int counter = 0;
                foreach (var type in toFill)
                {
                    var help = new Helpers(type, cmds[counter]);
                    object resultHolder = Helpers.CreateInstance(type);

                    help.Fill(resultHolder, dbResult, false);

                    if (!help.DataRead)
                    {
                        if (typeof(IList).IsAssignableFrom(type)) //no result on lists gives empty list as result.
                        {
                            ret.Add(Helpers.CreateInstance(type));
                        }
                        else
                        {
                            ret.Add(null);
                        }
                    }
                    else
                    {
                        ret.Add(resultHolder);
                    }

                    counter++;
                    dbResult.NextResult();
                }
            }
            finally
            {
                if (manageConnection) con.Close();
                dbResult.Close();
            }

            return ret;
        }

        public (T1, T2) Fill<T1, T2>(IDbCommand dbCommand) where T1 : new() where T2 : new()
        {

            var res = FillManyGeneric(dbCommand, new List<Type> { typeof(T1), typeof(T2) });
            return ((T1)res[0], (T2)res[1]);
        }

        public (T1, T2, T3) Fill<T1, T2, T3>(IDbCommand dbCommand) where T1 : new() where T2 : new() where T3 : new()
        {
            var res = FillManyGeneric(dbCommand, new List<Type> { typeof(T1), typeof(T2), typeof(T3) });
            return ((T1)res[0], (T2)res[1], (T3)res[2]);
        }
        public (T1, T2, T3,T4) Fill<T1, T2, T3,T4>(IDbCommand dbCommand) where T1 : new() where T2 : new() where T3 : new() where T4 : new()
        {
            var res = FillManyGeneric(dbCommand, new List<Type> { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
            return ((T1)res[0], (T2)res[1], (T3)res[2], (T4)res[3]);
        }

        public void Dispose()
        {
            con.Dispose();
        }
    }
}
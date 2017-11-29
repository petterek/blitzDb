using System;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
        
    public class DBAbstraction : IDBAbstraction
    {
        readonly IDbConnection con;

        public DBAbstraction(IDbConnection con)
        {
            this.con = con;
        }

        

        public void ExpandParameter(IDbCommand cmd, IDataParameter param, object[] values)
        {
            var cmdText = cmd.CommandText;
            int x = 0;
            List<string> names = new List<string>();

            foreach(var el in values)
            {
                var p = cmd.CreateParameter();
                p.DbType = param.DbType;
                string v = $"{param.ParameterName}_{x}";
                names.Add($"@{v}");
                p.ParameterName = v;
                p.Value = el;
                cmd.Parameters.Add(p);
                x++;
            }
            cmdText = cmdText.Replace($"@{param.ParameterName}", string.Join(",", names.ToArray()));
            cmd.CommandText = cmdText;
        }

                        

        public void Fill(object toFill, IDbCommand dbCommand)
        {
            dbCommand.Connection = con;
            var help = new Helpers(toFill.GetType(), dbCommand.CommandText);

            if (con.State == ConnectionState.Closed)
            {
                using (con)
                {
                    con.Open();
                    var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                    help.Fill(toFill, res);
                }
            }
            else
            {
                var res = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(toFill, res);
            }

        }

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
            }else
            {
                ret = dbCommand.ExecuteScalar();
            }

            return (T)ret;
        }

    }
}

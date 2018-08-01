using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
    public static class ExtensionMethods
    {
        public class ExpandableValue
        {
            public IEnumerable ValueList { get; }

            public ExpandableValue(IEnumerable value)
            {
                ValueList = value;
            }
        }

        public static void ExpandParameter(this IDbCommand cmd, IDataParameter param, IEnumerable values)
        {
            var dbType = param.DbType;

            param.Value = new ExpandableValue(values);
            param.DbType = dbType;
            cmd.Parameters.Add(param);
        }

        public static void Fill(this IDbConnection con, IDbCommand cmd, object target)
        {
            cmd.Connection = con;
            var help = new Helpers(target.GetType(), cmd.CommandText);

            if (con.State == ConnectionState.Closed)
            {
                con.Open();
                var res = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(target, res);
                con.Close();
            }
            else
            {
                var res = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
                help.Fill(target, res);
            }
        }
    }
}
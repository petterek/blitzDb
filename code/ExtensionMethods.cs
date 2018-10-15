using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace blitzdb
{
    public static class ExtensionMethods
    {
        public class ExpandableValue
        {
            public IEnumerable ValueList { get; }
            public int SplitSize { get; }

            public ExpandableValue(IEnumerable value, int splitSize)
            {
                SplitSize = splitSize;
                ValueList = value;
            }
        }



        public static void ExpandParameter(this IDbCommand cmd, IDataParameter param, IList values, int maximumNumberBeforeSplitting = 200)
        {
            if (values.Count <= maximumNumberBeforeSplitting)
            {
                var cmdText = cmd.CommandText;
                int x = 0;
                List<string> names = new List<string>();

                foreach (var el in values)
                {
                    var p = cmd.CreateParameter();
                    p.Value = el;
                    p.DbType = param.DbType;
                    string v = $"{param.ParameterName}_{x}";
                    names.Add($"@{v}");
                    p.ParameterName = v;

                    cmd.Parameters.Add(p);
                    x++;
                }
                cmdText = cmdText.Replace($"@{param.ParameterName}", string.Join(",", names.ToArray()));
                cmd.CommandText = cmdText;
            }
            else
            {
                var dbType = param.DbType;

                param.Value = new ExpandableValue(values, maximumNumberBeforeSplitting);
                param.DbType = dbType;
                cmd.Parameters.Add(param);
            }
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
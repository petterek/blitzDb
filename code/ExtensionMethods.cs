using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
    public static class ExtensionMethods
    {
        public static void ExpandParameter(this IDbCommand cmd, IDataParameter param, IEnumerable values)
        {
            var cmdText = cmd.CommandText;
            int x = 0;
            List<string> names = new List<string>();

            foreach (var el in values)
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
    }
}

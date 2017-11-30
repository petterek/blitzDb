using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace blitzdb
{

    class Helpers
    {

        static MethodInfo getValueMi = typeof(IDataRecord).GetMethod("GetValue");
        static MethodInfo isDbNullMi = typeof(IDataRecord).GetMethod("IsDBNull");
        static Dictionary<string, Action<object, IDataReader>> fillerCache = new Dictionary<string, Action<object, IDataReader>>();

        private Type type;
        private string commandText;
        private string fillerKey;
        private System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(".*?(where|$)");


        public Helpers(Type type, string commandText)
        {
            if (typeof(IList).IsAssignableFrom(type))
            {
                if (!type.IsGenericType) throw new ArgumentException("toFill must be list with 1 generic argument");
                this.type = type.GetGenericArguments()[0];
            }
            else
            {
                this.type = type;
            }
            this.commandText = commandText;

            fillerKey = regEx.Match(commandText).Value;

            if (fillerKey.Contains("*"))
            {
                fillerKey = "";
            }
            else
            {
                fillerKey = $"{type.FullName}-{fillerKey}".GetHashCode().ToString();
            }

        }

        internal static Action<Object, IDataReader> CreateFillerFunction(Type toFill, IDataReader res)
        {

            List<Expression> allEx = new List<Expression>();
            ParameterExpression inpP = Expression.Parameter(typeof(object), "ObjectToFill");

            var inp = Expression.Variable(toFill);

            allEx.Add(Expression.Assign(inp, Expression.Convert(inpP, toFill)));

            var readerInp = Expression.Parameter(typeof(IDataReader), "ReaderToReadFrom");

            for (var x = 0; x < res.FieldCount; x++)
            {
                var name = res.GetName(x);

                FieldInfo fieldInfo = toFill.GetField(name);

                if (fieldInfo == null) throw new MissingFieldException(name);

                var type = fieldInfo.FieldType;

                var toSet = Expression.PropertyOrField(inp, name);
                var fromValue = Expression.Call(readerInp, getValueMi, new Expression[] { Expression.Constant(x) });

                Expression ex;

                if (Nullable.GetUnderlyingType(type) != null | type == typeof(string)) //reference types must be checked for DBNull.Value
                {
                    ex = Expression.Block(
                        //Expression.Assign(param, fromValue),
                        Expression.IfThenElse(
                        Expression.Call(readerInp, isDbNullMi, new[] { Expression.Constant(x) }),
                            Expression.Assign(toSet, Expression.Constant(null, type)),
                            Expression.Assign(toSet, Expression.Convert(fromValue, type)
                                )
                            )
                         );
                }
                else
                {
                    ex = Expression.Assign(toSet, Expression.Convert(fromValue, type));
                }

                allEx.Add(ex);
            }

            var be = Expression.Block(new[] { inp }, allEx.ToArray());


            return Expression.Lambda<Action<Object, IDataReader>>(be, inpP, readerInp).Compile();
        }

        internal void Fill(object toFill, IDataReader data)
        {
            if (typeof(IList).IsAssignableFrom(toFill.GetType()))
            {
                FillList((System.Collections.IList)toFill, data);
            }
            else
            {
                data.Read();
                FillSingle(toFill, data);
            }
        }
        private void FillSingle(object toFill, IDataReader res)
        {
            GetFillerMethod(toFill.GetType(), res)(toFill, res);
        }

        private void FillList(IList toFill, IDataReader res)
        {

            while (res.Read())
            {
                var oRef = Activator.CreateInstance(type);
                toFill.Add(oRef);
                FillSingle(oRef, res);
            }
        }

        private Action<object, IDataReader> GetFillerMethod(Type typeToFill, IDataReader res)
        {
            if (fillerKey == "")
            {
                return CreateFillerFunction(typeToFill, res);//not able to cache filler..  
            }

            if (!fillerCache.ContainsKey(fillerKey))
            {
                lock (fillerKey)
                {
                    if (!fillerCache.ContainsKey(fillerKey))
                    {
                        fillerCache[fillerKey] = CreateFillerFunction(typeToFill, res);
                    }
                }
            }
            return fillerCache[fillerKey];
        }
    }
}

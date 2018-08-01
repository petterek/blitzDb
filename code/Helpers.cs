using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace blitzdb
{
    public class Helpers
    {
        private static MethodInfo getValueMi = typeof(IDataRecord).GetMethod("GetValue");
        private static MethodInfo isDbNullMi = typeof(IDataRecord).GetMethod("IsDBNull");
        private static Dictionary<string, Action<object, IDataReader>> fillerCache = new Dictionary<string, Action<object, IDataReader>>();
        private static Dictionary<string, Func<IDataReader, object>> rehydrateCache = new Dictionary<string, Func<IDataReader, object>>();
        private Type ListType;
        private Type type;
        private bool IsList = false;
        private string commandText;
        private string fillerKey;
        private System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(".*?(where|$)");

        public Helpers(Type type, string commandText)
        {
            if (typeof(IList).IsAssignableFrom(type))
            {
                if (!type.IsGenericType) throw new ArgumentException("toFill must be list with 1 generic argument");
                this.ListType = type;
                this.type = type.GetGenericArguments()[0];
                this.IsList = true;
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

        public bool DataRead { get; private set; }

        internal static Action<Object, IDataReader> CreateFillerAction(Type toFill, IDataReader res)
        {
            List<Expression> allEx = new List<Expression>();
            ParameterExpression inpP = Expression.Parameter(typeof(object), "ObjectToFill");

            var inp = Expression.Variable(toFill);

            allEx.Add(Expression.Assign(inp, Expression.Convert(inpP, toFill)));

            var readerInp = Expression.Parameter(typeof(IDataReader), "ReaderToReadFrom");

            for (var x = 0; x < res.FieldCount; x++)
            {
                var name = res.GetName(x);

                // make case insensitive
                var fieldInfos = toFill.GetMember(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (fieldInfos.Length == 0) throw new MissingFieldException(name);
                if (fieldInfos.Length > 1) throw new AmbiguousMatchException(name);

                var fieldInfo = fieldInfos[0];
                Type type;
                if (fieldInfo.MemberType == MemberTypes.Field)
                {
                    type = toFill.GetField(fieldInfo.Name).FieldType;
                }
                else if (fieldInfo.MemberType == MemberTypes.Property)
                {
                    type = toFill.GetProperty(fieldInfo.Name).PropertyType;
                }
                else
                {
                    throw new NotSupportedException("Only fields and properties is supported.");
                }

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

        internal static Func<IDataReader, object> CreateRehydrateFunction(Type toFill, IDataReader res)
        {
            List<Expression> allEx = new List<Expression>();

            //Add check here for constructor with multiple params

            var theValueToFill = Expression.Variable(toFill);

            ConstructorInfo[] constructorInfos = toFill.GetConstructors();

            if (constructorInfos.Length > 1)
            {
                throw new MultipleConstructorsNotSupportedException();
            }

            var readerInp = Expression.Parameter(typeof(IDataReader));

            ConstructorInfo theConstructor = constructorInfos[0];
            ParameterInfo[] ConstructorParams = theConstructor.GetParameters();

            BlockExpression be;

            if (ConstructorParams.Length == 0)
            {
                allEx.Add(Expression.Assign(theValueToFill, Expression.New(toFill)));

                for (var x = 0; x < res.FieldCount; x++)
                {
                    var name = res.GetName(x);
                    var fieldInfos = toFill.GetMember(name);

                    if (fieldInfos == null) throw new MissingFieldException(name);
                    if (fieldInfos.Length > 1) throw new AmbiguousMatchException(name);

                    var fieldInfo = fieldInfos[0];
                    Type type;
                    if (fieldInfo.MemberType == MemberTypes.Field)
                    {
                        type = toFill.GetField(fieldInfo.Name).FieldType;
                    }
                    else if (fieldInfo.MemberType == MemberTypes.Property)
                    {
                        type = toFill.GetProperty(fieldInfo.Name).PropertyType;
                    }
                    else
                    {
                        throw new NotSupportedException("Only fields and properties is supported.");
                    }

                    var toSet = Expression.PropertyOrField(theValueToFill, name);
                    var fromValue = Expression.Call(readerInp, getValueMi, new Expression[] { Expression.Constant(x) });

                    allEx.Add(Expression.Assign(toSet, WrapForNullables(readerInp, x, type, fromValue)));
                }
                allEx.Add(theValueToFill);
                be = Expression.Block(new[] { theValueToFill }, allEx.ToArray());
            }
            else
            {
                var allParams = new Dictionary<string, ParameterExpression>();

                foreach (var param in ConstructorParams)
                {
                    allParams.Add(param.Name.ToLower(), null);
                }

                for (var x = 0; x < res.FieldCount; x++)
                {
                    var name = res.GetName(x);

                    var constructorParam = ConstructorParams.FirstOrDefault(pInfo => string.Compare(pInfo.Name, name, true) == 0);
                    if (constructorParam == null) { continue; }

                    var variabel = Expression.Variable(constructorParam.ParameterType, $"var_{x}");
                    var fromValue = Expression.Call(readerInp, getValueMi, new Expression[] { Expression.Constant(x) });

                    allParams[name.ToLower()] = variabel;
                    allEx.Add(Expression.Assign(variabel, WrapForNullables(readerInp, x, constructorParam.ParameterType, fromValue)));

                    //allEx.Add(Expression.Assign(variabel, Expression.Convert(fromValue, constructorParam.ParameterType)));
                }
                allEx.Add(Expression.Assign(theValueToFill, Expression.New(theConstructor, allParams.Values.ToArray())));

                allEx.Add(theValueToFill);
                var scopParams = new List<ParameterExpression>() { theValueToFill };
                scopParams.AddRange(allParams.Values);
                be = Expression.Block(scopParams, allEx.ToArray());
            }

            return Expression.Lambda<Func<IDataReader, object>>(be, readerInp).Compile();
        }

        private static Expression WrapForNullables(ParameterExpression readerInp, int x, Type type, MethodCallExpression fromValue)
        {
            Expression ex;
            if (Nullable.GetUnderlyingType(type) != null | type == typeof(string)) //reference types must be checked for DBNull.Value
            {
                //var ret = Expression.Parameter(type);
                return
                    Expression.Condition(
                        Expression.Call(readerInp, isDbNullMi, new[] { Expression.Constant(x) }),
                            Expression.Constant(null, type),
                            Expression.Convert(fromValue, type)
                        );
            }
            else
            {
                ex = Expression.Convert(fromValue, type);
            }

            return ex;
        }

        public bool Fill(object toFill, IDataReader data)
        {
            if (IsList)
            {
                FillList((System.Collections.IList)toFill, data);
                data.Close();
                return true;
            }
            else
            {
                if (data.Read())
                {
                    DataRead = true;
                    FillSingle(toFill, data);
                    data.Close();
                    return true;
                }
                else
                {
                    DataRead = false;
                    return false;
                }
            }
        }

        public object Rehydrate(IDataReader data)
        {
            if (!data.Read())
            {
                DataRead = false;
                return null;
            }

            DataRead = true;

            if (IsList)
            {
                return RehydrateList(data);
            }
            else
            {
                return RehydrateSingle(data);
            }
        }

        private void FillSingle(object toFill, IDataReader res)
        {
            GetFillerMethod(res)(toFill, res);
        }

        private Object CreateSingle(Type instType, IDataReader reader)
        {
            var ret = Activator.CreateInstance(instType);
            FillSingle(ret, reader);
            return ret;
        }

        private object RehydrateSingle(IDataReader res)
        {
            return GetRehydrateFunc(res)(res);
        }

        private void FillList(IList toFill, IDataReader res)
        {
            bool isValueType = type.IsPrimitive | type.BaseType == typeof(System.ValueType);
            while (res.Read())
            {
                DataRead = true;
                if (isValueType) //If the list is of type List<int>. Will always use col 0
                {
                    object value = res[0];
                    if (value != DBNull.Value) toFill.Add(value);
                }
                else
                {
                    toFill.Add(CreateSingle(type, res));
                }
            }
        }

        private IList RehydrateList(IDataReader res)
        {
            var ret = (IList)Activator.CreateInstance(ListType);

            Func<IDataReader, object> toCall = null;
            if (type.IsPrimitive)
            {
                toCall = (reader) => reader[0];
            }
            else
            {
                toCall = (reader) => RehydrateSingle(res);
            }

            do //Not to good but read has happend allready to check for result..
            {
                ret.Add(toCall(res));
            } while (res.Read());

            return ret;
        }

        private Action<object, IDataReader> GetFillerMethod(IDataReader res)
        {
            if (fillerKey == "")
            {
                return CreateFillerAction(type, res);//not able to cache filler..
            }

            if (!fillerCache.ContainsKey(fillerKey))
            {
                lock (fillerKey)
                {
                    if (!fillerCache.ContainsKey(fillerKey))
                    {
                        fillerCache[fillerKey] = CreateFillerAction(type, res);
                    }
                }
            }
            return fillerCache[fillerKey];
        }

        private Func<IDataReader, object> GetRehydrateFunc(IDataReader reader)
        {
            if (fillerKey == "")
            {
                return CreateRehydrateFunction(type, reader);//not able to cache filler..
            }

            if (!rehydrateCache.ContainsKey(fillerKey))
            {
                lock (fillerKey)
                {
                    if (!rehydrateCache.ContainsKey(fillerKey))
                    {
                        rehydrateCache[fillerKey] = CreateRehydrateFunction(type, reader);
                    }
                }
            }
            return rehydrateCache[fillerKey];
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
    public interface IDbReaderAbstraction
    {

        /// <summary>
        /// Fills an object by matching names on fields or properties
        /// </summary>
        /// <param name="dbCommand"></param>
        /// <param name="toFill"></param>
        void Fill(IDbCommand dbCommand, object toFill);
        T Fill<T>(IDbCommand dbCommand) where T : new();
        (T1, T2) Fill<T1, T2>(IDbCommand dbCommand) where T1 : new() where T2 : new();
        (T1, T2, T3) Fill<T1, T2, T3>(IDbCommand dbCommand) where T1 : new() where T2 : new() where T3 : new();
        (T1, T2, T3, T4) Fill<T1, T2, T3, T4>(IDbCommand dbCommand) where T1 : new() where T2 : new() where T3 : new() where T4:new();
        /// <summary>
        /// Creates an objct with values injected into constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbCommand"></param>
        /// <returns></returns>
        T Rehydrate<T>(IDbCommand dbCommand);
    }

    public interface IDBAbstraction : IDbReaderAbstraction
    {
        void Execute(IDbCommand dbCommand);
        T ExecuteScalar<T>(IDbCommand dbCommand);
    }
}
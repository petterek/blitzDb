using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
    public interface IDbReaderAbstraction
    {
        void Fill(IDbCommand dbCommand, object toFill);
        T Rehydrate<T>(IDbCommand dbCommand);
    }

    public interface IDBAbstraction : IDbReaderAbstraction
    {
        void Execute(IDbCommand dbCommand);
        T ExecuteScalar<T>(IDbCommand dbCommand);
    }
}
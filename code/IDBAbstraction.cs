using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
    public interface IDbReaderAbstraction
    {
        void Fill(IDbCommand dbCommand, object toFill);
    }

    public interface IDBAbstraction : IDbReaderAbstraction
    {
        void Execute(IDbCommand dbCommand);
        T ExecuteScalar<T>(IDbCommand dbCommand);
    }
}
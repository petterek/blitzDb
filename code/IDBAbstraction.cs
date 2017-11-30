using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace blitzdb
{
    public interface IDBAbstraction
    {
        void Execute(IDbCommand dbCommand);
        T ExecuteScalar<T>(IDbCommand dbCommand);
        void Fill(IDbCommand dbCommand, object toFill);

        void ExpandParameter(IDbCommand cmd, IDataParameter param, IEnumerable values);
    }
}
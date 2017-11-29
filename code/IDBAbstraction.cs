using System.Data;

namespace blitzdb
{
    public interface IDBAbstraction
    {
        void Execute(IDbCommand dbCommand);
        T ExecuteScalar<T>(IDbCommand dbCommand);
        void Fill(object toFill, IDbCommand dbCommand);
    }
}
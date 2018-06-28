using System.Data.SqlClient;
using System.Threading.Tasks;

namespace blitzdb.SqlServer
{
    public interface ISqlServerDBAbstraction : IDbReaderAbstraction
    {
        Task ExecuteAsync(SqlCommand dbCommand);

        Task<T> ExecuteScalarAsync<T>(SqlCommand dbCommand);
    }
}
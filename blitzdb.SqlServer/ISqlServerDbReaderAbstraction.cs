using System.Data.SqlClient;
using System.Threading.Tasks;

namespace blitzdb.SqlServer
{
    public interface ISqlServerDbReaderAbstraction : IDbReaderAbstraction
    {
        Task FillAsync(SqlCommand dbCommand, object toFill);

        Task<T> FillAsync<T>(SqlCommand dbCommand) where T : new();

        Task<T> RehydrateAsync<T>(SqlCommand dbCommand);
    }
}
using NUnit.Framework;
using System;
using System.Data.SqlClient;
using System.Threading;

namespace blitzdb.Test
{
    [TestFixture] public class BufferdDBTest : TestBase
    {

        [SetUp]
        public void SetUp()
        {
            bdb = new BufferdDbAbstraction(new SqlConnection(ConnectionString()));
            sqlDBAbstraction = new blitzdb.SqlServer.SqlDBAbstraction(new SqlConnection(ConnectionString()));
        }

        [Test] public void ValuesIsWrittenToDB()
        {

            SqlCommand dbCommand = new SqlCommand($"Insert into TableOne (Id,Guid,Name) values(@Id,@Guid,@Name)");
            dbCommand.Parameters.AddWithValue("Id", 10);
            dbCommand.Parameters.AddWithValue("Guid", Guid.NewGuid());
            dbCommand.Parameters.AddWithValue("Name", "Pyse");


            bdb.Execute(dbCommand);
            Thread.Sleep(10000);
        }

    }

}
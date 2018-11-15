using NUnit.Framework;
using System;
using System.Data.SqlClient;


namespace blitzdb.Test
{
    public abstract class TestBase
    {
        protected string ConnectionString() => $"Data Source=localhost;Initial Catalog={currentDb};Integrated Security=True; TimeOut=1;";

        private string MgmtConnectionString = $"Data Source=localhost;Integrated Security=True; TimeOut=1;";

        private string currentDb;
        protected IDBAbstraction bdb;

        protected SqlServer.SqlDBAbstraction sqlDBAbstraction;

        [OneTimeSetUp]
        public void SetupOnce()

        {
            currentDb = Guid.NewGuid().ToString();

            var db = new blitzdb.DBAbstraction(new SqlConnection(MgmtConnectionString));

            db.Execute(new SqlCommand($"Create database [{currentDb}]"));

            SetUp();

            bdb.Execute(new SqlCommand($@"
                                        CREATE TABLE[dbo].[TableOne](
                                            [Id][int] NOT NULL,
                                            [Guid][uniqueidentifier] NULL,
                                            [Name][nvarchar](50) NOT NULL,
                                            [StringWithoutValue] [nvarchar](50) NULL
                                        ) ON[PRIMARY]"));

            bdb.Execute(new SqlCommand($"Insert into TableOne (Id,Guid,Name) values(1,'{currentDb}','PEtter')"));
            bdb.Execute(new SqlCommand($"Insert into TableOne (Id,Guid,Name) values(2,'{currentDb}','PEtter')"));
            bdb.Execute(new SqlCommand($"Insert into TableOne (Id,Guid,Name) values(3,null,'PEtter')"));
        }

        [OneTimeTearDown]
        public void TearDownOnce()
        {
            //if (TestContext.CurrentContext.Result.FailCount == 0)
            {
                SqlConnection con = new SqlConnection(MgmtConnectionString);
                var db = new DBAbstraction(con);
                db.Execute(new SqlCommand(
                    $@"ALTER DATABASE [{currentDb}]
                   SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

                   DROP DATABASE [{currentDb}]
                   "));
            }
        }

        [SetUp]
        public void SetUp()
        {
            bdb = new blitzdb.DBAbstraction(new SqlConnection(ConnectionString()));
            sqlDBAbstraction = new blitzdb.SqlServer.SqlDBAbstraction(new SqlConnection(ConnectionString()));
        }

        [TearDown]
        public void TearDown()
        {
            //Conn.Close();
        }
    }

}
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using blitzdb;

namespace test
{
    [TestFixture]
    public class Class1
    {
        private string ConnectionString() => $"Data Source=localhost;Initial Catalog={currentDb};Integrated Security=True; TimeOut=1;";

        private string MgmtConnectionString = $"Data Source=localhost;Integrated Security=True; TimeOut=1;";

        private string currentDb;
        private blitzdb.DBAbstraction bdb;

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
        }

        [TearDown]
        public void TearDown()
        {
            //Conn.Close();
        }

        [Test, MaxTime(50)]
        public void ConnectToDbAndExecuteQuery()
        {
            var cmd = new SqlCommand("Select * from tableOne");

            bdb.Execute(cmd);
        }

        [Test, MaxTime(50), Repeat(50)]
        public void FillOneObjectFromDb()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where id =1");
            var o = new DataObject();

            bdb.Fill(cmd, o);

            Assert.AreEqual(1, o.Id);
        }

        [Test]
        public void FillListWithPrimitivType()
        {
            var cmd = new SqlCommand("Select Id from tableOne");

            var o = new List<int>();

            bdb.Fill(cmd, o);

            Assert.AreEqual(3, o.Count);
        }

        [Test]
        public void FillListWithValueType()
        {
            var cmd = new SqlCommand("Select Guid from tableOne");

            var o = new List<Guid>();

            bdb.Fill(cmd, o);

            Assert.AreEqual(2, o.Count);
        }

        [Test, MaxTime(100)]
        public void FillListOfObjectFromDb()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid from tableOne ");
            var o = new List<DataObject>();

            bdb.Fill(cmd, o);

            Assert.AreEqual(3, o.Count);
            Assert.AreEqual(1, o[0].Id);
        }

        [Test]
        public void StringWithoutValueIsSetToNull()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where id =1");
            var o = new DataObject();

            bdb.Fill(cmd, o);
            Assert.AreEqual(1, o.Id);
            Assert.IsNull(o.StringWithoutValue);
            Assert.AreEqual("PEtter", o.Name);
        }

        [Test]
        public void GenericFillWorks()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where id =1");

            var o = bdb.Fill<DataObject>(cmd);
            Assert.AreEqual(1, o.Id);
            Assert.IsNull(o.StringWithoutValue);
            Assert.AreEqual("PEtter", o.Name);
        }

        [Test]
        public void StandardParametersWorks()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            var o = new List<DataObject>();
            cmd.Parameters.AddWithValue("Id", 1);
            bdb.Fill(cmd, o);

            Assert.AreEqual(1, o.Count);
            Assert.AreEqual(1, o[0].Id);
        }

        [Test]
        public void ExpandingParametersWorks()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where Id in(@Id) ");
            var o = new List<DataObject>();
            cmd.Parameters.AddWithValue("notInUse", 1);
            cmd.ExpandParameter(new SqlParameter("Id", DbType.Int32), new object[] { 1, 2, 4, 5, 6 });
            cmd.Parameters.AddWithValue("alsoNotInUse", 1);
            bdb.splitSize = 2;
            bdb.Fill(cmd, o);

            Assert.AreEqual(2, o.Count);
            Assert.AreEqual(1, o[0].Id);
        }

        [Test]
        public void ExpandingParametersWorksWithGuids()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where Guid in(@Id) ");

            var o = new List<DataObject>();

            cmd.ExpandParameter(new SqlParameter("Id", SqlDbType.UniqueIdentifier), new object[] { Guid.NewGuid() });
            bdb.Fill(cmd, o);
        }

        [Test]
        public void WorkingWithPropertiesInsteadOfFields()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            var o = new List<DataObjectWProperties>();
            cmd.Parameters.AddWithValue("Id", 1);
            bdb.Fill(cmd, o);
        }

        [Test]
        public void ThrowsNotSupportedException()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            var o = new List<DataObjectWithFunction>();
            cmd.Parameters.AddWithValue("Id", 1);
            Assert.Throws<NotSupportedException>(() => bdb.Fill(cmd, o));
        }

        [Test]
        public void RehydrateWorks()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", 1);
            var o = bdb.Rehydrate<DataObjectWProperties>(cmd);

            Assert.AreEqual(1, o.Id);
        }

        [Test]
        public void RehydrateWorksWithConstructor()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", 1);

            var o = bdb.Rehydrate<ImmutableObject>(cmd);

            Assert.AreEqual(1, o.Id);
        }

        [Test]
        public void RehydrateWorksWithConstructorAndList()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", 1);

            var o = bdb.Rehydrate<List<ImmutableObject>>(cmd);

            Assert.AreEqual(1, o.Count);
        }

        [Test]
        public void RehydrateWorksWithListOfPrimitives()
        {
            var cmd = new SqlCommand("Select Id from tableOne ");

            var o = bdb.Rehydrate<List<int>>(cmd);

            Assert.AreEqual(3, o.Count);
        }

        [Test]
        public void RehydrateWorksWithConstructorAndNullValue()
        {
            var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", 1);
            var o = bdb.Rehydrate<ImmutableObjectWithNullString>(cmd);

            Assert.AreEqual(1, o.Id);
        }

        [Test]
        public void MakeFieldCheckCaseInsensitive_Test()
        {
            var cmd = new SqlCommand("Select Id,Name as NaMe ,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", 1);
            var ret = new List<DataObject>();

            bdb.Fill(cmd, ret);
        }

        [Test]
        public void FillingNonExistingReturnsNull_Test()
        {
            var cmd = new SqlCommand("Select Id,Name as NaMe ,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", -1);
            var ret = bdb.Fill<DataObject>(cmd);

            Assert.IsNull(ret);
        }

        [Test]
        public void ConnectionIsClosedWhenExceptionIsThrown()
        {
            var cmd = new SqlCommand("Select Id,Nameeeeee as NaMe ,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", -1);
            Assert.Throws<System.Data.SqlClient.SqlException>(() => bdb.Fill<DataObject>(cmd));

            Assert.AreEqual(ConnectionState.Closed, bdb.con.State);
        }

        [Test]
        public void ConnectionIsClosedWhenExceptionIsThrownInFillNonGeneric()
        {
            var cmd = new SqlCommand("Select Id,Nameeeeee as NaMe ,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", -1);
            Assert.Throws<System.Data.SqlClient.SqlException>(() => bdb.Fill(cmd, new object()));

            Assert.AreEqual(ConnectionState.Closed, bdb.con.State);
        }

        [Test]
        public void ConnectionIsClosedWhenExceptionIsThrownInExecute()
        {
            var cmd = new SqlCommand("Select Id,Nameeeeee as NaMe ,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", -1);
            Assert.Throws<System.Data.SqlClient.SqlException>(() => bdb.Execute(cmd));

            Assert.AreEqual(ConnectionState.Closed, bdb.con.State);
        }

        [Test]
        public void ConnectionIsClosedWhenExceptionIsThrownInExecuteScalar()
        {
            var cmd = new SqlCommand("Select Id,Nameeeeee as NaMe ,Guid,StringWithoutValue from tableOne where Id =@Id ");
            cmd.Parameters.AddWithValue("Id", -1);
            Assert.Throws<System.Data.SqlClient.SqlException>(() => bdb.ExecuteScalar<int>(cmd));

            Assert.AreEqual(ConnectionState.Closed, bdb.con.State);
        }
    }

    internal class ImmutableObjectWithNullString
    {
        public int Id { get; }
        public Guid? Guid { get; }
        public string Name { get; }
        public string StringWithoutValue { get; }

        public ImmutableObjectWithNullString(int Id, Guid? guid, string name, string stringWithoutValue)
        {
            this.StringWithoutValue = stringWithoutValue;
            Name = name;
            Guid = guid;
            this.Id = Id;
        }
    }

    internal class ImmutableObject
    {
        public int Id { get; }
        public Guid? Guid { get; }
        public string Name { get; }

        public ImmutableObject(int Id, Guid? guid, string name)
        {
            Name = name;
            Guid = guid;
            this.Id = Id;
        }
    }

    internal class DataObjectWithFunction
    {
        public void Id()
        {
        }
    }

    internal class DataObjectWProperties
    {
        public int Id { get; set; }
        public Guid? Guid { get; set; }
        public string Name { get; set; }
        public string StringWithoutValue { get; set; }
    }

    internal class DataObject
    {
        public int Id;
        public Guid? Guid;
        public string Name;
        public string StringWithoutValue;
    }
}
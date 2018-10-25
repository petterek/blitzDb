using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace test
{
    [TestFixture]
    public class AsyncTest : TestBase
    {
        [Test]
        public async Task QueryDBAsync()
        {
            var cmd = new SqlCommand("Select Guid from tableOne");

            var o = new List<Guid>();

            await sqlDBAbstraction.FillAsync(cmd, o);

            Assert.AreEqual(2, o.Count);
        }
    }
}
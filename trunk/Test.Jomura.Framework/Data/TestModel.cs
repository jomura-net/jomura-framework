using Jomura.Data;
using NUnit.Framework;
using Test.Jomura.Framework.Data;
using System.Data.Common;
using System.Collections.ObjectModel;

namespace Test.Jomura.Framework
{
    [TestFixture]
    public class TestModel : AbstractDAC
    {
        public TestModel()
        {
            CommandTimeout = 600;
        }

        [Test]
        public void InsertTest()
        {
            string sql = @"
INSERT INTO TestTable01
 VALUES (
  @id
 ,@value
 )";
            Collection<DbParameter> parms = new Collection<DbParameter>();
            parms.Add(CreateParameter("@id", 1));
            parms.Add(CreateParameter("@value", "‚Ä‚·‚Æ1"));

            Assert.AreEqual(ExecuteNonQuery(sql, parms), 1);
        }

        [TearDown]
        public void DeleteTest()
        {
            string sql = @"DELETE FROM TestTable01";
            Assert.AreEqual(ExecuteNonQuery(sql), 1);
        }

        [Test]
        public void FillTest()
        {
            InsertTest();

            string sql = @"
select * from TestTable01
";
            //Collection<SqlParameter> parms = new Collection<SqlParameter>();

            TestDS.TestTable01DataTable table = new TestDS.TestTable01DataTable();
            Fill(table, sql);

            Assert.AreEqual(table.Rows[0]["value"], "‚Ä‚·‚Æ1");
        }
    }

    [TestFixture]
    public class TestModel2 : AbstractDAC
    {
        public TestModel2()
            : base("Jomura.Framework.Test2")
        {
        }

        [Test]
        public void FillTest()
        {
            string sql = @"
select * from TestTable02
";
            //Collection<SqlParameter> parms = new Collection<SqlParameter>();

            TestDS.TestTable02DataTable table = new TestDS.TestTable02DataTable();
            Fill(table, sql);

            Assert.AreEqual(table.Rows.Count, 0);
        }
    }
}

using Jomura.Data;
using NUnit.Framework;
using Test.Jomura.Framework.Data;

namespace Test.Jomura.Framework
{
    [TestFixture]
    public class TestDAC : AbstractDAC
    {
        public TestDAC()
        {
            CommandTimeout = 600;
        }

        [Test]
        public void FillTest()
        {
            string sql = @"
select * from TestTable01
";
            //Collection<SqlParameter> parms = new Collection<SqlParameter>();

            TestDS.TestTable01DataTable table = new TestDS.TestTable01DataTable();
            Fill(table, sql);

            Assert.IsNotNull(table);
        }

        /*
        [Test]
        public void InsertTest()
        {
            string sql = @"
INSERT INTO TestTable01
 VALUES (
  1
 ,'‚Ä‚·‚Æ1'
 )
";

            TestDS.TestTable01DataTable table = new TestDS.TestTable01DataTable();
            TestDS.TestTable01Row row = table.NewTestTable01Row();
            row.id = 1;
            row.value = "‚Ä‚·‚Æ1";
            table.AddTestTable01Row(row);
            this.Update(table);

            Assert.IsNotNull(table);
        }
         */
    }

    [TestFixture]
    public class TestDAC2 : AbstractDAC
    {
        public TestDAC2()
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

            Assert.IsNotNull(table);
        }
    }
}

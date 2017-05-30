using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DotNetDBF.Test
{

    [TestFixture]
    public class Issue19Test
    {
        private Stream DbfsData(string filename) => typeof(Issue19Test).Assembly
            .GetManifestResourceStream($"{nameof(DotNetDBF)}.{nameof(Test)}.dbfs.{filename}");

        [Test]
        public void ReadTest()
        {
            using (var dbfstream = DbfsData("dbase_8b.dbf"))
            using (var memoStream = DbfsData("dbase_8b.dbt"))
            using (DBFReader dbfr = new DBFReader(dbfstream) { DataMemo = memoStream})
            {
                object[] record = dbfr.NextRecord();
                //This line would hang issue #19 https://github.com/ekonbenefits/dotnetdbf/issues/19
                Assert.Throws<DBTException>(() => record[5].ToString());
            }
        }
    }
}

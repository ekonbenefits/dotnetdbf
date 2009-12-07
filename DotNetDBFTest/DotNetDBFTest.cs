using System;
using System.IO;
using System.Linq;
using DotNetDBF;
using NUnit.Framework;

namespace DotNetDBFTest
{
    [TestFixture]
    public class DotNetDBFTest: AssertionHelper
    {
        private string TestPath = Path.Combine(Path.GetTempPath(), "121212.dbf");

        private string TestRAFPath =
            Path.Combine(Path.GetTempPath(), "raf-1212.dbf");


        private string TestClipLongPath =
    Path.Combine(Path.GetTempPath(), "cliplong.dbf");

        private string TestMemoPath =
Path.Combine(Path.GetTempPath(), "clipmemo.dbf");

        private string TestSelectPath =
Path.Combine(Path.GetTempPath(), "select.dbf");

        private string GetCharacters(int aLength)
        {
            var chars = new[]{"a","b","c","d","e","f","g"," "};
            var returnval = string.Join(string.Empty,
                                        Enumerable.Range(0, aLength).Select(it => chars[it % chars.Length]).ToArray());
            Assert.That(returnval.Length, EqualTo(aLength), "GetCharacters() did not return correct length  string");
            return returnval;
        }
        

        private static void println(String s)
        {
            Console.WriteLine(s);
        }


        [Test]
        public void checkDataType_N()
        {
            Decimal writtenValue;
            using (
                Stream fos =
                    File.Open(TestPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var writer = new DBFWriter();
                var field = new DBFField("F1", NativeDbType.Numeric, 15, 0);
                writer.Fields = new[] { field };

                writtenValue = 123456789012345L;
                writer.AddRecord(writtenValue);
                writer.Write(fos);
            }
            using (
                Stream fis =
                    File.Open(TestPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var reader = new DBFReader(fis);
           
                var readValues = reader.NextRecord();

                Assert.That(readValues[0], EqualTo(writtenValue),"Written Value Equals Read");
            }
        }


        [Test]
        public void checkLongCharLengthWithClipper()
        {
            var fieldLength = 750;
            string writtenValue;
            using (
                Stream fos =
                    File.Open(TestClipLongPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var writer = new DBFWriter();
                var field = new DBFField("F1", NativeDbType.Char, fieldLength);
                writer.Fields = new[] { field };

                writtenValue = GetCharacters(fieldLength);
                writer.AddRecord(writtenValue);
                writer.Write(fos);
            }
            using (
                Stream fis =
                    File.Open(TestClipLongPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var reader = new DBFReader(fis);
                Assert.That(reader.Fields.First().FieldLength, EqualTo(fieldLength));
                var readValues = reader.NextRecord();

                Assert.That(readValues[0], EqualTo(writtenValue), "Written Value not equaling Read");
            }
        }



        [Test]
        public void checkDataType_M()
        {
            var fieldLength = 2400;
            MemoValue writtenValue;
            using (
                Stream fos =
                    File.Open(TestMemoPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var writer = new DBFWriter
                                 {
                                     DataMemoLoc = Path.ChangeExtension(TestMemoPath, "DBT")
                                 };
                var field = new DBFField("F1", NativeDbType.Memo);
                writer.Fields = new[] { field };

                writtenValue = new MemoValue(GetCharacters(fieldLength));
                writer.AddRecord(writtenValue);
                writer.Write(fos);
            }
            using (
                Stream fis =
                    File.Open(TestMemoPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var reader = new DBFReader(fis)
                {
                    DataMemoLoc = Path.ChangeExtension(TestMemoPath, "DBT")
                };
                var readValues = reader.NextRecord();

                Assert.That(readValues[0], EqualTo(writtenValue), "Written Value not equaling Read");
            }
        }

        [Test]
        public void checkSelect()
        {
            var fieldLength = 2400;
            string writtenValue;
            using (
                Stream fos =
                    File.Open(TestSelectPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var writer = new DBFWriter
                {
                    DataMemoLoc = Path.ChangeExtension(TestSelectPath, "DBT")
                };
                var field = new DBFField("F1", NativeDbType.Memo);
                var field2 = new DBFField("F2", NativeDbType.Numeric,10);
                var field3 = new DBFField("F3", NativeDbType.Char,10);
                writer.Fields = new[] { field , field2, field3};

                writtenValue = "alpha";
                writer.AddRecord(new MemoValue(GetCharacters(fieldLength)),10,writtenValue);
                writer.Write(fos);
            }
            using (
                Stream fis =
                    File.Open(TestSelectPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                var reader = new DBFReader(fis)
                {
                    DataMemoLoc = Path.ChangeExtension(TestSelectPath, "DBT")
                };
                reader.SetSelectFields("F3");
                var readValues = reader.NextRecord();

                Assert.That(readValues[0], StartsWith(writtenValue), "Written Value not equaling Read");
            }
        }

        [Test]
        public void checkRAFwriting()
        {
            println("Writing in RAF mode ... ");

            if (File.Exists(TestRAFPath))
            {
                File.Delete(TestRAFPath);
            }
            using (var writer = new DBFWriter(TestRAFPath))
            {
                var fields = new DBFField[2];

                fields[0] = new DBFField("F1", NativeDbType.Char, 10);

                fields[1] = new DBFField("F2", NativeDbType.Numeric, 2);

                writer.Fields = fields;


                writer.WriteRecord("Red", 10);
                writer.WriteRecord("Blue", 20);
            }

            println("done.");

            println("Appending to this file");

            using (var writer = new DBFWriter(TestRAFPath))
            {
                writer.WriteRecord("Green", 33);

                writer.WriteRecord("Yellow", 44);
            }
            println("done.");


        }

        [Test]
        public void ShowPaths()
        {
            println(TestPath);

            println(TestRAFPath);

            println(TestClipLongPath);
        }

        [Test]
        public void Test()
        {
            using (
            Stream fis =
                File.Open(@"f:\st\dev\testdata\p.dbf",
                          FileMode.OpenOrCreate,
                          FileAccess.ReadWrite))
            {
                var reader = new DBFReader(fis)
                {
                    DataMemoLoc = Path.ChangeExtension(@"f:\st\dev\testdata\p.dbf", "DBT")
                };
                var readValues = reader.NextRecord();

               Console.WriteLine(readValues);
            }
        }


        [Test]
        public void test1()
        {

            Assert.DoesNotThrow(() => { new DBFWriter(); }, "Can't Create empty DBFWriter Object");

        }

        [Test]
        public void test2()
        {
            Assert.DoesNotThrow(() => { new DBFField(); }, "Can't Create empty DBFWriter Object");
        }


        [Test]
        public void test3()
        {
            WriteSample();
            ReadSample();
        }
       
        public void WriteSample()
        {
            var field = new DBFField {Name = "F1", DataType = NativeDbType.Numeric};
            var writer = new DBFWriter {Fields = new[] {field}};
            writer.AddRecord(3);
            using (
                Stream fos =
                    File.Open(TestPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                writer.Write(fos);
            }

        }

       
        public void ReadSample()
        {
            using (var reader = new DBFReader(TestPath))
            {
                Assert.That(reader.RecordCount, EqualTo(1));
            }
        }

       
    }
}
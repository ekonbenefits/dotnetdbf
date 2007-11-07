using System;
using System.IO;
using DotNetDBF;
using NUnit.Framework;

namespace DotNetDBFTest
{
    [TestFixture]
    public class DotNetDBFTest
    {
        private string TestPath = Path.Combine(Path.GetTempPath(), "121212.dbf");

        private string TestRAFPath =
            Path.Combine(Path.GetTempPath(), "raf-1212.dbf");

        private void print(String s)
        {
            Console.Write(s);
        }

        private void println(String s)
        {
            Console.WriteLine(s);
        }

        [Test]
        public void checkDataType_N()
        {
            Decimal value;
            using (
                Stream fos =
                    File.Open(TestPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                DBFWriter writer = new DBFWriter();
                DBFField field = new DBFField("F1", NativeDbType.Numeric, 15, 0);

                writer.Fields = new DBFField[] {field};
                value = 123456789012345L;
                writer.AddRecord(value);
                print(" written=" + value);
                writer.Write(fos);
            }
            using (
                Stream fis =
                    File.Open(TestPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                DBFReader reader = new DBFReader(fis);

                Object[] values = reader.NextRecord();
                print(" read=" + (Decimal) values[0]);
                println(" written == read ("
                        + (((Decimal) values[0]).Equals(value)) + ")");
            }
        }

        [Test]
        public void checkRAFwriting()
        {
            print("Writing in RAF mode ... ");

            if (File.Exists(TestRAFPath))
            {
                File.Delete(TestRAFPath);
            }
            using (DBFWriter writer = new DBFWriter(TestRAFPath))
            {
                DBFField[] fields = new DBFField[2];

                fields[0] = new DBFField("F1", NativeDbType.Char, 10);

                fields[1] = new DBFField("F2", NativeDbType.Numeric, 2);

                writer.Fields = fields;


                writer.WriteRecord("Red", 10);
                writer.WriteRecord("Blue", 20);
            }

            println("done.");

            print("Appending to this file");

            using (DBFWriter writer = new DBFWriter(TestRAFPath))
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
        }

        [Test]
        public void test1()
        {
            print("Creating an empty DBFWriter object... ");
            DBFWriter writer = new DBFWriter();
            println("OK.");
        }

        [Test]
        public void test2()
        {
            print("Creating an empty DBFField object... ");
            DBFField field = new DBFField();
            println("OK.");
        }

        [Test]
        public void test3()
        {
            print("Writing a sample DBF file ... ");
            DBFField field = new DBFField();
            field.Name = "F1";
            field.DataType = NativeDbType.Numeric;
            DBFWriter writer = new DBFWriter();
            writer.Fields = new DBFField[] {field};
            writer.AddRecord(3);
            using (
                Stream fos =
                    File.Open(TestPath,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite))
            {
                writer.Write(fos);
            }

            println("OK.");
        }

        [Test]
        public void test4()
        {
            print("Reading the written file ...");
            using (DBFReader reader = new DBFReader(TestPath))
            {
                print("\tRecord count=" + reader.RecordCount);
            }
            println(" OK.");
        }
    }
}
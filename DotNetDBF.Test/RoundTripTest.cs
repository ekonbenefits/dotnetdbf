using System;
using System.IO;
using System.Runtime.CompilerServices;
using DotNetDBF;
using NUnit.Framework;

namespace DotNetDBFTest
{
    /// <summary>
    /// Writing, and reading back what was written. The writer was the least covered part of the
    /// library - half its lines and half its branches - although writing files is half of what
    /// it is for. One type per test, so a failure names the type that broke.
    /// </summary>
    [TestFixture]
    public class RoundTripTest : AssertionHelper
    {
        /// <summary>
        /// A test that fails because the library is wrong, not because the test is. Each one
        /// names the issue it describes. CI excludes this category so master stays green, so
        /// run them deliberately to see where things stand:
        ///
        ///     dotnet test --filter TestCategory=KnownBugs
        /// </summary>
        private const string KnownBugs = "KnownBugs";


        private string TestPath([CallerMemberName] string name = null)
            => Path.Combine(Path.GetTempPath(), name + "_roundtrip.dbf");

        /// <summary>Writes one record of one field, reads it back, and hands back what came out.</summary>
        /// <remarks>
        /// A Memo field needs a memo location on both ends even when the value is null, or the
        /// reader throws "Memo Location Not Set", so the caller asks for one with
        /// <paramref name="withMemo" />.
        /// </remarks>
        private object RoundTrip(DBFField field, object value, [CallerMemberName] string name = null,
                                 bool withMemo = false)
        {
            var path = TestPath(name);
            var memo = Path.ChangeExtension(path, "dbt");
            if (withMemo && File.Exists(memo)) File.Delete(memo);   // OpenOrCreate, so it would grow forever

            using (var fos = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new DBFWriter())
            {
                if (withMemo) writer.DataMemoLoc = memo;
                writer.Fields = new[] { field };
                writer.AddRecord(value);
                writer.Write(fos);
            }

            using (var fis = File.Open(path, FileMode.Open, FileAccess.Read))
            using (var reader = new DBFReader(fis))
            {
                if (withMemo) reader.DataMemoLoc = memo;
                return reader.NextRecord()[0];
            }
        }

        // --- one test per field type ------------------------------------------------------

        [Test]
        public void Character_round_trips()
        {
            var read = RoundTrip(new DBFField("F1", NativeDbType.Char, 10), "hello");
            Assert.That(read, EqualTo("hello"));
        }

        [Test]
        public void Logical_round_trips_both_ways()
        {
            Assert.That(RoundTrip(new DBFField("T", NativeDbType.Logical), true), EqualTo(true));
            Assert.That(RoundTrip(new DBFField("F", NativeDbType.Logical), false), EqualTo(false));
        }

        [Test]
        public void Date_round_trips()
        {
            var when = new DateTime(2019, 7, 4);
            Assert.That(RoundTrip(new DBFField("D1", NativeDbType.Date), when), EqualTo(when));
        }

        [Test]
        public void Numeric_round_trips_with_decimals()
        {
            var value = 123.45m;
            Assert.That(RoundTrip(new DBFField("N1", NativeDbType.Numeric, 10, 2), value), EqualTo(value));
        }

        [Test]
        public void Float_round_trips()
        {
            var value = 3.5m;
            Assert.That(RoundTrip(new DBFField("F1", NativeDbType.Float, 10, 2), value), EqualTo(value));
        }

        [Test]
        public void Memo_round_trips_a_value_longer_than_a_field()
        {
            var text = new string('x', 300);
            var path = TestPath();
            var memo = Path.ChangeExtension(path, "dbt");
            if (File.Exists(memo)) File.Delete(memo);   // DataMemoLoc opens OpenOrCreate, so it would append forever
            using (var fos = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new DBFWriter { DataMemoLoc = memo })
            {
                writer.Fields = new[] { new DBFField("M1", NativeDbType.Memo) };
                writer.AddRecord(new MemoValue(text));
                writer.Write(fos);
            }

            using (var fis = File.Open(path, FileMode.Open, FileAccess.Read))
            using (var reader = new DBFReader(fis) { DataMemoLoc = memo })
            {
                var read = reader.NextRecord()[0];
                Assert.That(read.ToString(), EqualTo(text));
            }
        }

        // --- absent values ----------------------------------------------------------------

        /// <summary>
        /// FAILING - #48, not a pin. An absent value comes back as one of three
        /// different things depending on the field's type:
        ///
        ///     Char     ""              (empty string)
        ///     Numeric  null
        ///     Date     null
        ///     Float    null
        ///     Logical  DBNull.Value
        ///     Memo     DBNull.Value
        ///
        /// A caller cannot write one check for "was this set?". Which of the three is right is
        /// a decision for the library; that they disagree is the bug, so this asserts only that
        /// they agree, whichever way it is settled.
        /// </summary>
        [Test, Category(KnownBugs)]
        public void A_null_reads_back_the_same_way_whatever_the_field_type()
        {
            var forChar = RoundTrip(new DBFField("C", NativeDbType.Char, 10), null, "null_char");
            var forNumeric = RoundTrip(new DBFField("N", NativeDbType.Numeric, 10, 2), null, "null_numeric");
            var forDate = RoundTrip(new DBFField("D", NativeDbType.Date), null, "null_date");
            var forFloat = RoundTrip(new DBFField("F", NativeDbType.Float, 10, 2), null, "null_float");
            var forLogical = RoundTrip(new DBFField("L", NativeDbType.Logical), null, "null_logical");
            var forMemo = RoundTrip(new DBFField("M", NativeDbType.Memo), null, "null_memo", withMemo: true);

            string Describe(object v) =>
                v == null ? "null" : v == DBNull.Value ? "DBNull.Value" : $"{v.GetType().Name} \"{v}\"";

            var seen = new[] { forChar, forNumeric, forDate, forFloat, forLogical, forMemo };
            var kinds = string.Join(", ", new[]
            {
                "Char=" + Describe(forChar),
                "Numeric=" + Describe(forNumeric),
                "Date=" + Describe(forDate),
                "Float=" + Describe(forFloat),
                "Logical=" + Describe(forLogical),
                "Memo=" + Describe(forMemo),
            });

            foreach (var value in seen)
            {
                Assert.That(Describe(value), EqualTo(Describe(forNumeric)),
                    "every field type should report an absent value the same way: " + kinds);
            }
        }

        // --- several fields and several records -------------------------------------------

        [Test]
        public void Several_records_of_several_types_keep_their_order_and_values()
        {
            var path = TestPath();
            using (var fos = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[]
                {
                    new DBFField("NAME", NativeDbType.Char, 10),
                    new DBFField("AGE", NativeDbType.Numeric, 3, 0),
                    new DBFField("OK", NativeDbType.Logical),
                };
                writer.AddRecord("first", 1m, true);
                writer.AddRecord("second", 22m, false);
                writer.AddRecord("third", 333m, true);
                writer.Write(fos);
            }

            using (var fis = File.Open(path, FileMode.Open, FileAccess.Read))
            using (var reader = new DBFReader(fis))
            {
                Assert.That(reader.RecordCount, EqualTo(3));

                var first = reader.NextRecord();
                Assert.That(first[0], EqualTo("first"));
                Assert.That(first[1], EqualTo(1m));
                Assert.That(first[2], EqualTo(true));

                reader.NextRecord();                       // second

                var third = reader.NextRecord();
                Assert.That(third[0], EqualTo("third"));
                Assert.That(third[1], EqualTo(333m));
                Assert.That(third[2], EqualTo(true));

                Assert.That(reader.NextRecord(), Null, "past the end");
            }
        }

        [Test]
        public void The_file_ends_with_the_end_of_data_marker()
        {
            var path = TestPath();
            using (var fos = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("F1", NativeDbType.Char, 10) };
                writer.AddRecord("one");
                writer.Write(fos);
            }

            var bytes = File.ReadAllBytes(path);
            Assert.That(bytes[bytes.Length - 1], EqualTo(DBFFieldType.EndOfData));
        }

        // --- what the writer refuses ------------------------------------------------------

        [Test]
        public void A_record_of_the_wrong_length_is_refused()
        {
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("F1", NativeDbType.Char, 10) };
                Assert.Throws<DBFException>(() => writer.AddRecord("one", "two"));
            }
        }

        [Test]
        public void A_null_record_is_refused()
        {
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("F1", NativeDbType.Char, 10) };
                Assert.Throws<DBFException>(() => writer.AddRecord(null));
            }
        }

        [Test]
        public void A_record_before_any_field_is_refused()
        {
            using (var writer = new DBFWriter())
            {
                Assert.Throws<DBFException>(() => writer.AddRecord("nowhere to put this"));
            }
        }

        [Test]
        public void A_value_of_the_wrong_type_for_its_field_is_refused()
        {
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("N1", NativeDbType.Numeric, 10, 2) };
                // A numeric field takes anything IConvertible, so the object here is what it
                // refuses - a non-numeric string is not caught, see A_string_in_a_numeric_field.
                Assert.Throws<DBFRecordException>(() => writer.AddRecord(new object()));
            }

            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("D1", NativeDbType.Date) };
                Assert.Throws<DBFRecordException>(() => writer.AddRecord("not a date"));
            }

            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("L1", NativeDbType.Logical) };
                Assert.Throws<DBFRecordException>(() => writer.AddRecord("not a bool"));
            }

            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("M1", NativeDbType.Memo) };
                Assert.Throws<DBFRecordException>(() => writer.AddRecord("not a MemoValue"));
            }
        }

        // --- what the writer refuses before a record ---------------------------------------

        [Test]
        public void Fields_cannot_be_set_twice()
        {
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("F1", NativeDbType.Char, 10) };
                Assert.Throws<DBFException>(
                    () => writer.Fields = new[] { new DBFField("F2", NativeDbType.Char, 10) });
            }
        }

        [Test]
        public void Fields_cannot_be_empty_or_null()
        {
            using (var writer = new DBFWriter())
            {
                Assert.Throws<DBFException>(() => writer.Fields = new DBFField[0]);
            }

            using (var writer = new DBFWriter())
            {
                Assert.Throws<DBFException>(() => writer.Fields = null);
            }
        }

        [Test]
        public void A_null_field_is_refused()
        {
            using (var writer = new DBFWriter())
            {
                Assert.Throws<DBFException>(
                    () => writer.Fields = new[] { new DBFField("F1", NativeDbType.Char, 10), null });
            }
        }

        /// <summary>
        /// FAILING - #49, not a pin. AddRecord type-checks each value against its
        /// field precisely so bad data is refused where the caller can act on it. A numeric
        /// field checks for IConvertible, and string satisfies that, so "not a number" is
        /// accepted and the failure surfaces later as a FormatException out of Write - by which
        /// point the caller has no idea which record or field was at fault.
        /// </summary>
        [Test, Category(KnownBugs)]
        public void A_string_that_is_not_a_number_is_refused_by_a_numeric_field()
        {
            using (var writer = new DBFWriter())
            {
                writer.Fields = new[] { new DBFField("N1", NativeDbType.Numeric, 10, 2) };
                Assert.Throws<DBFRecordException>(() => writer.AddRecord("not a number"));
            }
        }

        [Test]
        public void A_numeric_string_that_is_a_number_is_accepted()
        {
            // The counterpart, and why the check cannot simply reject every string: a numeric
            // string converts fine and round-trips.
            Assert.That(RoundTrip(new DBFField("N1", NativeDbType.Numeric, 10, 2), "12.5"), EqualTo(12.5m));
        }
    }
}

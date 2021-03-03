/*
 DBFReader
 Class for reading the records assuming that the given
 InputStream comtains DBF data.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 
 License: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;

namespace DotNetDBF
{
    public class DBFReader : DBFBase, IDisposable
    {
        private BinaryReader _dataInputStream;
        private DBFHeader _header;
        private Stream _dataMemo;

        private string _dataMemoLoc;

        private int[] _selectFields = new int[] {};
        private int[] _orderedSelectFields = new int[] {};
        /* Class specific variables */
        private bool _isClosed = true;
        

        /**
		 Initializes a DBFReader object.
		 
		 When this constructor returns the object
		 will have completed reading the header (meta date) and
		 header information can be queried there on. And it will
		 be ready to return the first row.
		 
		 @param InputStream where the data is read from.
		 */


        public void SetSelectFields(params string[] aParams)
        {
            _selectFields =
                aParams.Select(
                    it =>
                        Array.FindIndex(_header.FieldArray,
                            jt => jt.Name.Equals(it, StringComparison.OrdinalIgnoreCase))).ToArray();
            _orderedSelectFields = _selectFields.OrderBy(it => it).ToArray();
        }

        public DBFField[] GetSelectFields()
        {
            return _selectFields.Any()
                ? _selectFields.Select(it => _header.FieldArray[it]).ToArray()
                : _header.FieldArray;
        }


        public DBFReader(string anIn)
        {
            try
            {
                _dataInputStream = new BinaryReader(
                    File.Open(anIn,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read)
                );

                var dbtPath = Path.ChangeExtension(anIn, "dbt");
                if (File.Exists(dbtPath))
                {
                    _dataMemoLoc = dbtPath;
                }

                _isClosed = false;
                _header = new DBFHeader();
                _header.Read(_dataInputStream);

                /* it might be required to leap to the start of records at times */
                var t_dataStartIndex = _header.HeaderLength
                                       - (32 + (32 * _header.FieldArray.Length))
                                       - 1;
                if (t_dataStartIndex > 0)
                {
                    _dataInputStream.ReadBytes((t_dataStartIndex));
                }
            }
            catch (IOException ex)
            {
                throw new DBFException("Failed To Read DBF", ex);
            }
        }

        public DBFReader(Stream anIn)
        {
            try
            {
                _dataInputStream = new BinaryReader(anIn);
                _isClosed = false;
                _header = new DBFHeader();
                _header.Read(_dataInputStream);

                /* it might be required to leap to the start of records at times */
                var t_dataStartIndex = _header.HeaderLength
                                       - (32 + (32 * _header.FieldArray.Length))
                                       - 1;
                if (t_dataStartIndex > 0)
                {
                    _dataInputStream.ReadBytes((t_dataStartIndex));
                }
            }
            catch (IOException e)
            {
                throw new DBFException("Failed To Read DBF", e);
            }
        }

        /**
		 Returns the number of records in the DBF.
		 */

        public int RecordCount => _header.NumberOfRecords;

        /**
		 Returns the asked Field. In case of an invalid index,
		 it returns a ArrayIndexOutOfBoundsException.
		 
		 @param index. Index of the field. Index of the first field is zero.
		 */

        public DBFField[] Fields => _header.FieldArray;

        #region IDisposable Members

        /// <summary>Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Close();
        }

        #endregion


        public string DataMemoLoc
        {
            get => _dataMemoLoc;
            set => _dataMemoLoc = value;
        }



        public delegate Stream LazyStream();

        private Stream _loadedStream;
     
        private LazyStream GetLazyStreamFromLocation()
        {

            if (_dataMemo == null && !string.IsNullOrEmpty(_dataMemoLoc))
            {
                return  () => _loadedStream ??
                                  (_loadedStream = File.Open(_dataMemoLoc, FileMode.Open, FileAccess.Read,
                                      FileShare.Read));
            }
            if (_dataMemo != null)
            {
                return () => _dataMemo;
            }
            return null;
        }

        public Stream DataMemo
        {
            get => _dataMemo;
            set => _dataMemo = value;
        }

        public override string ToString()
        {
            var sb =
                new StringBuilder(_header.Year + "/" + _header.Month + "/"
                                  + _header.Day + "\n"
                                  + "Total records: " + _header.NumberOfRecords +
                                  "\nHeader length: " + _header.HeaderLength +
                                  "");

            for (var i = 0; i < _header.FieldArray.Length; i++)
            {
                sb.Append(_header.FieldArray[i].Name);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public void Close()
        {


            _loadedStream?.Close();
            _dataMemo?.Close();
            _dataInputStream.Close();

            _dataMemo?.Dispose();



            _isClosed = true;
        }

        /**
		 Reads the returns the next row in the DBF stream.
		 @returns The next row as an Object array. Types of the elements
		 these arrays follow the convention mentioned in the class description.
		 */

        public object[] NextRecord()
        {
            return NextRecord(_selectFields, _orderedSelectFields);
        }


        internal object[] NextRecord(IEnumerable<int> selectIndexes, IList<int> sortedIndexes)
        {
            if (_isClosed)
            {
                throw new DBFException("Source is not open");
            }
            var tOrderdSelectIndexes = sortedIndexes;

            var recordObjects = new object[_header.FieldArray.Length];

            try
            {
                var isDeleted = false;
                do
                {
                    if (isDeleted)
                    {
                        _dataInputStream.ReadBytes(_header.RecordLength - 1);
                    }

                    int t_byte = _dataInputStream.ReadByte();
                    if (t_byte == DBFFieldType.EndOfData)
                    {
                        return null;
                    }

                    isDeleted = (t_byte == '*');
                } while (isDeleted);

                var j = 0;
                var k = -1;
                for (var i = 0; i < _header.FieldArray.Length; i++)
                {
                    if (tOrderdSelectIndexes.Count == j && j != 0
                        ||
                        (tOrderdSelectIndexes.Count > j && tOrderdSelectIndexes[j] > i && tOrderdSelectIndexes[j] != k))
                    {
                        _dataInputStream.BaseStream.Seek(_header.FieldArray[i].FieldLength, SeekOrigin.Current);
                        continue;
                    }
                    if (tOrderdSelectIndexes.Count > j)
                        k = tOrderdSelectIndexes[j];
                    j++;


                    switch (_header.FieldArray[i].DataType)
                    {
                        case NativeDbType.Char:

                            var b_array = new byte[
                                _header.FieldArray[i].FieldLength
                                ];
                            _dataInputStream.Read(b_array, 0, b_array.Length);

                            recordObjects[i] = CharEncoding.GetString(b_array).TrimEnd();
                            break;

                        case NativeDbType.Date:

                            var t_byte_year = new byte[4];
                            _dataInputStream.Read(t_byte_year,
                                0,
                                t_byte_year.Length);

                            var t_byte_month = new byte[2];
                            _dataInputStream.Read(t_byte_month,
                                0,
                                t_byte_month.Length);

                            var t_byte_day = new byte[2];
                            _dataInputStream.Read(t_byte_day,
                                0,
                                t_byte_day.Length);

                            try
                            {
                                var tYear = CharEncoding.GetString(t_byte_year);
                                var tMonth = CharEncoding.GetString(t_byte_month);
                                var tDay = CharEncoding.GetString(t_byte_day);

                                if (int.TryParse(tYear, out var tIntYear) &&
                                    int.TryParse(tMonth, out var tIntMonth) &&
                                    int.TryParse(tDay, out var tIntDay))
                                {
                                    recordObjects[i] = new DateTime(
                                        tIntYear,
                                        tIntMonth,
                                        tIntDay);
                                }
                                else
                                {
                                    recordObjects[i] = null;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                /* this field may be empty or may have improper value set */
                                recordObjects[i] = null;
                            }

                            break;

                        case NativeDbType.Float:

                            try
                            {
                                var t_float = new byte[
                                    _header.FieldArray[i].FieldLength
                                    ];
                                _dataInputStream.Read(t_float, 0, t_float.Length);
                                var tParsed = CharEncoding.GetString(t_float);
                                var tLast = tParsed.Substring(tParsed.Length - 1);
                                if (tParsed.Length > 0
                                    && tLast != " "
                                    && tLast != NullSymbol)
                                {
                                    //
                                    // A Float in FoxPro has 20 significant digits, since it is
                                    // stored as a string with possible E-postfix notation.
                                    // An IEEE 754 float or double can not handle this number of digits
                                    // correctly. Therefor the only correct implementation is to use a decimal.
                                    //
                                    recordObjects[i] = decimal.Parse(tParsed,
                                        NumberStyles.Float | NumberStyles.AllowLeadingWhite,
                                        NumberFormatInfo.InvariantInfo);
                                }
                                else
                                {
                                    recordObjects[i] = null;
                                }
                            }
                            catch (FormatException e)
                            {
                                throw new DBFException("Failed to parse Float",
                                    e);
                            }

                            break;

                        case NativeDbType.Numeric:

                            try
                            {
                                var t_numeric = new byte[
                                    _header.FieldArray[i].FieldLength
                                    ];
                                _dataInputStream.Read(t_numeric,
                                    0,
                                    t_numeric.Length);
                                var tParsed =
                                    CharEncoding.GetString(t_numeric);
                                var tLast = tParsed.Substring(tParsed.Length - 1);
                                if (tParsed.Length > 0
                                    && tLast != " "
                                    && tLast != NullSymbol)
                                {
                                    recordObjects[i] = decimal.Parse(tParsed,
                                        NumberStyles.Float | NumberStyles.AllowLeadingWhite,
                                        NumberFormatInfo.InvariantInfo);
                                }
                                else
                                {
                                    recordObjects[i] = null;
                                }
                            }
                            catch (FormatException e)
                            {
                                throw new DBFException(
                                    "Failed to parse Number", e);
                            }

                            break;

                        case NativeDbType.Logical:

                            var t_logical = _dataInputStream.ReadByte();
                            //todo find out whats really valid
                            if (t_logical == 'Y' || t_logical == 't'
                                || t_logical == 'T'
                                || t_logical == 't')
                            {
                                recordObjects[i] = true;
                            }
                            else if (t_logical == DBFFieldType.UnknownByte)
                            {
                                recordObjects[i] = DBNull.Value;
                            }
                            else
                            {
                                recordObjects[i] = false;
                            }
                            break;

                        case NativeDbType.Memo:
                            if (

                                string.IsNullOrEmpty(_dataMemoLoc) && 

                                _dataMemo is null)
                            {
                                throw new Exception("Memo Location Not Set");
                            }


                            var rawMemoPointer = _dataInputStream.ReadBytes(_header.FieldArray[i].FieldLength);
                            var memoPointer = CharEncoding.GetString(rawMemoPointer);
                            if (string.IsNullOrEmpty(memoPointer))
                            {
                                recordObjects[i] = DBNull.Value;
                                break;
                            }

                            if (!long.TryParse(memoPointer, out var tBlock))
                            {
                                //Because Memo files can vary and are often the least important data, 
                                //we will return null when it doesn't match our format.
                                recordObjects[i] = DBNull.Value;
                                break;
                            }


                            recordObjects[i] = new MemoValue(tBlock, this, 
                                _dataMemoLoc,
                                GetLazyStreamFromLocation());
                            break;
                        default:
                            {
                                byte[] data = _dataInputStream.ReadBytes(_header.FieldArray[i].FieldLength);

                                recordObjects[i] = data != null ? (object)data : DBNull.Value;

                                break;
                            }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            catch (IOException e)
            {
                throw new DBFException("Problem Reading File", e);
            }

            return selectIndexes.Any() ? selectIndexes.Select(it => recordObjects[it]).ToArray() : recordObjects;
        }
	
	/// <summary>
        /// Reads the data table from DBF from current position to end.
        /// </summary>
        /// <returns>The data table with all data, original types and names of columns.</returns>
        /// <param name="dataTableName">Name for data table.</param>
        public DataTable ReadDataTable(string dataTableName)
        {
            DataTable DT = new DataTable(dataTableName);
            for (var i = 0; i < _header.FieldArray.Length; i++)
            {
                DBFField DF = _header.FieldArray[i];
                string n = DF.Name;
                DT.Columns.Add(n, DF.Type);
                if (DF.Type == typeof(System.String))
                    DT.Columns[n].MaxLength = DF.FieldLength;
                DT.Columns[n].AllowDBNull = false;
            }
            object[] cDBF = NextRecord(_selectFields, _orderedSelectFields);
            while (cDBF != null)
            {
                DataRow nDR = DT.NewRow();
                for (var i = 0; i < _header.FieldArray.Length; i++)
                {
                    object o = cDBF[i];
                    nDR[_header.FieldArray[i].Name] = o;
                }
                DT.Rows.Add(nDR);
                cDBF = NextRecord(_selectFields, _orderedSelectFields);
            }
            return DT;
        }
    }
}

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
        private string _dataMemoLoc;

        private int[] _selectFields = new int[]{};
        /* Class specific variables */
        private bool isClosed = true;

        /**
		 Initializes a DBFReader object.
		 
		 When this constructor returns the object
		 will have completed reading the hader (meta date) and
		 header information can be quried there on. And it will
		 be ready to return the first row.
		 
		 @param InputStream where the data is read from.
		 */



        public void SetSelectFields(params string[] aParams)
        {
            _selectFields =
                aParams.Select(
                    it =>
                    Array.FindIndex(_header.FieldArray,
                                    jt => jt.Name.Equals(it, StringComparison.InvariantCultureIgnoreCase))).ToArray();
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

                isClosed = false;
                _header = new DBFHeader();
                _header.Read(_dataInputStream);

                /* it might be required to leap to the start of records at times */
                int t_dataStartIndex = _header.HeaderLength
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
                isClosed = false;
                _header = new DBFHeader();
                _header.Read(_dataInputStream);

                /* it might be required to leap to the start of records at times */
                int t_dataStartIndex = _header.HeaderLength
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

        public int RecordCount
        {
            get { return _header.NumberOfRecords; }
        }

        /**
		 Returns the asked Field. In case of an invalid index,
		 it returns a ArrayIndexOutofboundsException.
		 
		 @param index. Index of the field. Index of the first field is zero.
		 */

        public DBFField[] Fields
        {
            get { return _header.FieldArray; }
        }

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
            get
            {
                return _dataMemoLoc;
            }set
            {
                _dataMemoLoc = value;
            }
        }

        public override String ToString()
        {
            StringBuilder sb =
                new StringBuilder(_header.Year + "/" + _header.Month + "/"
                                  + _header.Day + "\n"
                                  + "Total records: " + _header.NumberOfRecords +
                                  "\nHeader length: " + _header.HeaderLength +
                                  "");

            for (int i = 0; i < _header.FieldArray.Length; i++)
            {
                sb.Append(_header.FieldArray[i].Name);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public void Close()
        {
            _dataInputStream.Close();
            isClosed = true;
        }

        /**
		 Reads the returns the next row in the DBF stream.
		 @returns The next row as an Object array. Types of the elements
		 these arrays follow the convention mentioned in the class description.
		 */
        public Object[] NextRecord()
        {
            return NextRecord(_selectFields);
        }

        internal Object[] NextRecord(IEnumerable<int> selectIndexes)
        {
            if (isClosed)
            {
                throw new DBFException("Source is not open");
            }


            var recordObjects = new Object[_header.FieldArray.Length];

            try
            {
                bool isDeleted = false;
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

                var tIndexes = selectIndexes.Any() ? selectIndexes.OrderBy(it=>it) : Enumerable.Range(0, _header.FieldArray.Length);

                var tLastIndex = 0;
                foreach (int i in tIndexes )
                {
                    for (int j = tLastIndex; j < i; j++)
                    {
                            _dataInputStream.BaseStream.Seek(_header.FieldArray[i].FieldLength, SeekOrigin.Current);
                    }
                    tLastIndex = i+1;
                  
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

                            byte[] t_byte_year = new byte[4];
                            _dataInputStream.Read(t_byte_year,
                                                 0,
                                                 t_byte_year.Length);

                            byte[] t_byte_month = new byte[2];
                            _dataInputStream.Read(t_byte_month,
                                                 0,
                                                 t_byte_month.Length);

                            byte[] t_byte_day = new byte[2];
                            _dataInputStream.Read(t_byte_day,
                                                 0,
                                                 t_byte_day.Length);

                            try
                            {
                                var tYear = CharEncoding.GetString(t_byte_year);
                                var tMonth = CharEncoding.GetString(t_byte_month);
                                var tDay = CharEncoding.GetString(t_byte_day);

                                recordObjects[i] = new DateTime(
                                    Int32.Parse(tYear),
                                    Int32.Parse(tMonth),
                                    Int32.Parse(tDay));
                            }
                            catch (FormatException)
                            {
                                recordObjects[i] = null;
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
                                byte[] t_float = new byte[
                                    _header.FieldArray[i].FieldLength
                                    ];
                                _dataInputStream.Read(t_float, 0, t_float.Length);
                                t_float = Utils.trimLeftSpaces(t_float);
                                String tParsed = CharEncoding.GetString(t_float);

                                if (t_float.Length > 0
                                    && !tParsed.Contains(DBFFieldType.Unknown))
                                {
                                    recordObjects[i] = Double.Parse(tParsed,NumberStyles.Float);
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
                                byte[] t_numeric = new byte[
                                    _header.FieldArray[i].FieldLength
                                    ];
                                _dataInputStream.Read(t_numeric,
                                                     0,
                                                     t_numeric.Length);
                                t_numeric = Utils.trimLeftSpaces(t_numeric);
                                string tParsed =
                                    CharEncoding.GetString(t_numeric);
                                if (t_numeric.Length > 0
                                    && !tParsed.Contains(DBFFieldType.Unknown))
                                {
                                    recordObjects[i] = Decimal.Parse(tParsed, NumberStyles.Float);
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

                            byte t_logical = _dataInputStream.ReadByte();
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
                            }else
                            {
                                recordObjects[i] = false;
                            }
                            break;

                        case NativeDbType.Memo:
                            if(string.IsNullOrEmpty(_dataMemoLoc))
                                throw new Exception("Memo Location Not Set");


                            var tRawMemoPointer = _dataInputStream.ReadBytes(10);
                            var tMemoPoiner = CharEncoding.GetString(tRawMemoPointer);
                            if(string.IsNullOrEmpty(tMemoPoiner))
                            {
                                recordObjects[i] = DBNull.Value;
                                break;
                            }

                            var tBlock =long.Parse(tMemoPoiner);


                            recordObjects[i] = new MemoValue(tBlock, this, _dataMemoLoc);
                            break;
                        default:
                            recordObjects[i] = DBNull.Value;
                            break;
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
    }
}
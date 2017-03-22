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
    /// <summary>
    /// Class for reading the records assuming that the given <see cref="Stream"/> contains DBF data.
    /// </summary>
    public class DBFReader : DBFBase, IDisposable
    {
        #region Fields
        private BinaryReader _dataInputStream;
        private DBFHeader _header;
        private string _dataMemoLoc;

        private int[] _selectFields;
        private List<int> _orderedSelectFields;
        private bool isClosed = true;
        #endregion

        #region Properties

        /// <summary>
        /// Returns the number of records in the DBF.
        /// </summary>
        public int RecordCount
        {
            get { return _header.NumberOfRecords; }
        }

        /// <summary>
        /// Returns the array of table fields.
        /// </summary>
        public DBFField[] Fields
        {
            get { return _header.FieldArray; }
        }

        public string DataMemoLoc
        {
            get
            {
                return _dataMemoLoc;
            }
            set
            {
                _dataMemoLoc = value;
            }
        }
        #endregion

        #region Constructors
        public DBFReader()
        {
            _selectFields = new int[0];
        }

        /// <summary>
        /// Initializes new <see cref="DBFReader"/> object.
        /// <para>
        /// Table metadata is cached in the header.
        /// </para>
        /// </summary>
        /// <param name="path">File to open.</param>
        public DBFReader(string path)
            :this()
        {
            try
            {
                _dataInputStream = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

                string dbtPath = Path.ChangeExtension(path, "dbt");
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
                throw new DBFException("Failed to read DBF", ex);
            }
        }

        /// <summary>
        /// Initializes new <see cref="DBFReader"/> object.
        /// <para>
        /// Table metadata is cached in the header.
        /// </para>
        /// </summary>
        /// <param name="inputStream"><see cref="Stream"/> where the data is read from.</param>
        public DBFReader(Stream input)
            :this()
        {
            try
            {
                _dataInputStream = new BinaryReader(input);
                isClosed = false;
                _header = new DBFHeader();
                _header.Read(_dataInputStream);

                // it might be required to leap to the start of records at times
                int dataStartIndex = _header.HeaderLength
                                       - (32 + (32 * _header.FieldArray.Length))
                                       - 1;
                if (dataStartIndex > 0)
                {
                    _dataInputStream.ReadBytes(dataStartIndex);
                }
            }
            catch (IOException e)
            {
                throw new DBFException("Failed to read DBF.", e);
            }
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// </summary>
        /// <remarks>filterpriority = 2</remarks>
        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            _dataInputStream.Close();
            isClosed = true;
        }

        #endregion

        public void SetSelectFields(params string[] aParams)
        {
            _selectFields = aParams
                .Select(p => Array.FindIndex(_header.FieldArray, field => field.Name.Equals(p, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();

            _orderedSelectFields = _selectFields.OrderBy(it => it).ToList();
        }

        public DBFField[] GetSelectFields()
        {
            return _selectFields.Any()
                ? _selectFields.Select(s => _header.FieldArray[s]).ToArray()
                : _header.FieldArray;
        }

        /// <summary>
        /// Reads then returns the next row in the DBF stream.
        /// </summary>
        /// <returns>
        /// The next row as an <see cref="Object"/> array. Types of the elements these arrays follow the convention mentioned in the class description.
        /// </returns>
        public object[] NextRecord()
        {
            return NextRecord(_selectFields, _orderedSelectFields);
        }

        /// <summary>
        /// Reads then returns the next row in the DBF stream. After the last row returns null.
        /// </summary>
        /// <param name="selectIndexes"></param>
        /// <param name="sortedIndexes"></param>
        /// <returns>The next row as an <see cref="Object"/> array. Types of the elements these arrays follow the convention mentioned in the class description.</returns>
        internal object[] NextRecord(int[] selectIndexes, IEnumerable<int> sortedIndexes)
        {
            if (isClosed)
            {
                throw new DBFException("Source is not open");
            }
            List<int> tOrderdSelectIndexes = new List<int>();
            if (sortedIndexes != null)
                tOrderdSelectIndexes.AddRange(sortedIndexes);

            object[] recordObjects = new object[_header.FieldArray.Length];

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

                int j = 0;
                int k = -1;
                for (int i = 0; i < _header.FieldArray.Length; i++)
                {

                    if (tOrderdSelectIndexes.Count == j && j != 0
                        || (tOrderdSelectIndexes.Count > j && tOrderdSelectIndexes[j] > i && tOrderdSelectIndexes[j] != k))
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
                            {
                                byte[] b_array = new byte[_header.FieldArray[i].FieldLength];
                                _dataInputStream.Read(b_array, 0, b_array.Length);

                                recordObjects[i] = CharEncoding.GetString(b_array).TrimEnd();
                                break;
                            }
                        case NativeDbType.Date:
                            {
                                byte[] yearByte = new byte[4];
                                byte[] monthByte = new byte[2];
                                byte[] dayByte = new byte[2];

                                _dataInputStream.Read(yearByte, 0, yearByte.Length);
                                _dataInputStream.Read(monthByte, 0, monthByte.Length);
                                _dataInputStream.Read(dayByte, 0, dayByte.Length);

                                try
                                {
                                    string yearString = CharEncoding.GetString(yearByte);
                                    string monthString = CharEncoding.GetString(monthByte);
                                    string dayString = CharEncoding.GetString(dayByte);

                                    int year, month, day;
                                    if (int.TryParse(yearString, out year) &&
                                        int.TryParse(monthString, out month) &&
                                        int.TryParse(dayString, out day))
                                    {
                                        recordObjects[i] = new DateTime(year, month, day);
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
                            }
                        case NativeDbType.Float:
                            {
                                try
                                {
                                    byte[] t_float = new byte[_header.FieldArray[i].FieldLength];
                                    _dataInputStream.Read(t_float, 0, t_float.Length);
                                    string tParsed = CharEncoding.GetString(t_float);
                                    string tLast = tParsed.Substring(tParsed.Length - 1);
                                    if (tParsed.Length > 0
                                        && tLast != " "
                                        && tLast != DBFFieldType.Unknown)
                                    {
                                        recordObjects[i] = Convert.ToSingle(tParsed, NumberFormatProvider);
                                    }
                                    else
                                    {
                                        recordObjects[i] = null;
                                    }
                                }
                                catch (FormatException e)
                                {
                                    throw new DBFException("Failed to parse Float", e);
                                }

                                break;
                            }
                        case NativeDbType.Numeric:
                            {
                                try
                                {
                                    byte[] t_numeric = new byte[_header.FieldArray[i].FieldLength];
                                    _dataInputStream.Read(t_numeric, 0, t_numeric.Length);
                                    string cellValue = CharEncoding.GetString(t_numeric);
                                    string lastChar = cellValue.Substring(cellValue.Length - 1);
                                    if (cellValue.Length > 0
                                        && lastChar != " "
                                        && lastChar != DBFFieldType.Unknown)
                                    {
                                        recordObjects[i] = Convert.ToDouble(cellValue, NumberFormatProvider);
                                    }
                                    else
                                    {
                                        recordObjects[i] = null;
                                    }
                                }
                                catch (FormatException e)
                                {
                                    throw new DBFException("Failed to parse number", e);
                                }

                                break;
                            }
                        case NativeDbType.Logical:
                            {
                                byte t_logical = _dataInputStream.ReadByte();
                                //todo find out whats really valid
                                if (t_logical == 'Y' || t_logical == 't' || t_logical == 'T' || t_logical == 't')
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
                            }
                        case NativeDbType.Memo:
                            {
                                if (string.IsNullOrEmpty(_dataMemoLoc))
                                    throw new Exception("Memo Location Not Set");

                                byte[] tRawMemoPointer = _dataInputStream.ReadBytes(_header.FieldArray[i].FieldLength);
                                string tMemoPoiner = CharEncoding.GetString(tRawMemoPointer);
                                if (string.IsNullOrEmpty(tMemoPoiner))
                                {
                                    recordObjects[i] = DBNull.Value;
                                    break;
                                }
                                long tBlock;
                                if (!long.TryParse(tMemoPoiner, out tBlock))
                                {
                                    //Because Memo files can vary and are often the least importat data, 
                                    //we will return null when it doesn't match our format.
                                    recordObjects[i] = DBNull.Value;
                                    break;
                                }

                                recordObjects[i] = new MemoValue(tBlock, this, _dataMemoLoc);
                                break;
                            }
                        default:
                            {
                                _dataInputStream.ReadBytes(_header.FieldArray[i].FieldLength);
                                recordObjects[i] = DBNull.Value;
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}/{1}/{2}", _header.Year, _header.Month, _header.Day);
            sb.AppendLine();
            sb.AppendFormat("Total records: {0}", _header.NumberOfRecords);
            sb.AppendLine();
            sb.AppendFormat("Header length: " + _header.HeaderLength);

            for (int i = 0; i < _header.FieldArray.Length; i++)
            {
                sb.AppendLine(_header.FieldArray[i].Name);
            }

            return sb.ToString();
        }
    }
}
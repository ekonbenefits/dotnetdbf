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
using System.IO;
using System.Text;

namespace DotNetDBF
{
    public class DBFReader : DBFBase, IDisposable
    {
        private BinaryReader dataInputStream;
        private DBFHeader header;

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

        public DBFReader(string anIn)
        {
            try
            {
                dataInputStream = new BinaryReader(
                    File.Open(anIn,
                              FileMode.Open,
                              FileAccess.Read,
                              FileShare.Read)
                    );
                isClosed = false;
                header = new DBFHeader();
                header.Read(dataInputStream);

                /* it might be required to leap to the start of records at times */
                int t_dataStartIndex = header.HeaderLength
                                       - (32 + (32 * header.FieldArray.Length))
                                       - 1;
                if (t_dataStartIndex > 0)
                {
                    dataInputStream.ReadBytes((t_dataStartIndex));
                }
            }
            catch (IOException e)
            {
                throw new DBFException("Failed To Read DBF", e);
            }
        }

        public DBFReader(Stream anIn)
        {
            try
            {
                dataInputStream = new BinaryReader(anIn);
                isClosed = false;
                header = new DBFHeader();
                header.Read(dataInputStream);

                /* it might be required to leap to the start of records at times */
                int t_dataStartIndex = header.HeaderLength
                                       - (32 + (32 * header.FieldArray.Length))
                                       - 1;
                if (t_dataStartIndex > 0)
                {
                    dataInputStream.ReadBytes((t_dataStartIndex));
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
            get { return header.NumberOfRecords; }
        }

        /**
		 Returns the asked Field. In case of an invalid index,
		 it returns a ArrayIndexOutofboundsException.
		 
		 @param index. Index of the field. Index of the first field is zero.
		 */

        public DBFField[] Fields
        {
            get { return header.FieldArray; }
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

        public override String ToString()
        {
            StringBuilder sb =
                new StringBuilder(header.Year + "/" + header.Month + "/"
                                  + header.Day + "\n"
                                  + "Total records: " + header.NumberOfRecords +
                                  "\nHEader length: " + header.HeaderLength +
                                  "");

            for (int i = 0; i < header.FieldArray.Length; i++)
            {
                sb.Append(header.FieldArray[i].Name);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public void Close()
        {
            dataInputStream.Close();
            isClosed = true;
        }

        /**
		 Reads the returns the next row in the DBF stream.
		 @returns The next row as an Object array. Types of the elements
		 these arrays follow the convention mentioned in the class description.
		 */

        public Object[] NextRecord()
        {
            if (isClosed)
            {
                throw new DBFException("Source is not open");
            }

            Object[] recordObjects = new Object[header.FieldArray.Length
                ];

            try
            {
                bool isDeleted = false;
                do
                {
                    if (isDeleted)
                    {
                        dataInputStream.ReadBytes(header.RecordLength - 1);
                    }

                    int t_byte = dataInputStream.ReadByte();
                    if (t_byte == DBFValue.EndOfData)
                    {
                        return null;
                    }

                    isDeleted = (t_byte == '*');
                } while (isDeleted);

                for (int i = 0; i < header.FieldArray.Length; i++)
                {
                    switch (header.FieldArray[i].DataType)
                    {
                        case NativeDbType.Char:

                            byte[] b_array = new byte[
                                header.FieldArray[i].FieldLength
                                ];
                            dataInputStream.Read(b_array, 0, b_array.Length);

                            recordObjects[i] = CharEncoding.GetString(b_array);
                            break;

                        case NativeDbType.Date:

                            byte[] t_byte_year = new byte[4];
                            dataInputStream.Read(t_byte_year,
                                                 0,
                                                 t_byte_year.Length);

                            byte[] t_byte_month = new byte[2];
                            dataInputStream.Read(t_byte_month,
                                                 0,
                                                 t_byte_month.Length);

                            byte[] t_byte_day = new byte[2];
                            dataInputStream.Read(t_byte_day,
                                                 0,
                                                 t_byte_day.Length);

                            try
                            {
                                recordObjects[i] = new DateTime(
                                    Int32.Parse(
                                        CharEncoding.GetString(t_byte_year)),
                                    Int32.Parse(
                                        CharEncoding.GetString(t_byte_month)),
                                    Int32.Parse(
                                        CharEncoding.GetString(t_byte_day))
                                    );
                            }
                            catch (FormatException)
                            {
                                /* this field may be empty or may have improper value set */
                                recordObjects[i] = null;
                            }

                            break;

                        case NativeDbType.Float:

                            try
                            {
                                byte[] t_float = new byte[
                                    header.FieldArray[i].FieldLength
                                    ];
                                dataInputStream.Read(t_float, 0, t_float.Length);
                                t_float = Utils.trimLeftSpaces(t_float);
                                String tParsed = CharEncoding.GetString(t_float);

                                if (t_float.Length > 0
                                    && !tParsed.Contains(DBFValue.Unknown))
                                {
                                    recordObjects[i] = Double.Parse(tParsed);
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
                                    header.FieldArray[i].FieldLength
                                    ];
                                dataInputStream.Read(t_numeric,
                                                     0,
                                                     t_numeric.Length);
                                t_numeric = Utils.trimLeftSpaces(t_numeric);
                                string tParsed =
                                    CharEncoding.GetString(t_numeric);
                                if (t_numeric.Length > 0
                                    && !tParsed.Contains(DBFValue.Unknown))
                                {
                                    recordObjects[i] = Decimal.Parse(tParsed);
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

                            byte t_logical = dataInputStream.ReadByte();
                            //todo find out whats really valid
                            if (t_logical == 'Y' || t_logical == 't'
                                || t_logical == 'T'
                                || t_logical == 't')
                            {
                                recordObjects[i] = true;
                            }
                            else
                            {
                                recordObjects[i] = false;
                            }
                            break;

                        case NativeDbType.Memo:
                            // TODO Later
                            recordObjects[i] = DBNull.Value;
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

            return recordObjects;
        }
    }
}
/*
 DBFWriter
 Class for defining a DBF structure and addin data to that structure and
 finally writing it to an OutputStream.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DotNetDBF
{
    public class DBFWriter : DBFBase, IDisposable
    {
        private DBFHeader header;
        private Stream raf;
        private int recordCount;
        private List<object> v_records = new List<object>();
        private Stream _dataMemo;

        private string _dataMemoLoc;

        /// Creates an empty Object.
        public DBFWriter()
        {
            header = new DBFHeader();
        }


        /// Creates a DBFWriter which can append to records to an existing DBF file.
        /// @param dbfFile. The file passed in should be a valid DBF file.
        /// @exception Throws DBFException if the passed in file does exist but not a valid DBF file, or if an IO error occurs.
        public DBFWriter(string dbfFile)
        {
            try
            {
                raf =
                    File.Open(dbfFile,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite);

                DataMemoLoc = Path.ChangeExtension(dbfFile, "dbt");

                /* before proceeding check whether the passed in File object
				 is an empty/non-existent file or not.
				 */
                if (raf.Length == 0)
                {
                    header = new DBFHeader();
                    return;
                }

                header = new DBFHeader();
                header.Read(new BinaryReader(raf));

                /* position file pointer at the end of the raf */
                raf.Seek(-1, SeekOrigin.End);
                /* check whether the last byte is 0x1A (end of file marker for dbf files) - in this case move 1 byte back to ignore it when writing new records */ 
                var lastByte = raf.ReadByte();  /* Advances to end of stream */
                if (lastByte == DBFFieldType.EndOfData)
                {
                    raf.Seek(-1, SeekOrigin.End);
                }
            }
            catch (FileNotFoundException e)
            {
                throw new DBFException("Specified file is not found. ", e);
            }
            catch (IOException e)
            {
                throw new DBFException(" while reading header", e);
            }
            recordCount = header.NumberOfRecords;
        }

        public DBFWriter(Stream dbfFile)
        {
            raf = dbfFile;

            /* before proceeding check whether the passed in File object
			 is an empty/non-existent file or not.
			 */
            if (raf.Length == 0)
            {
                header = new DBFHeader();
                return;
            }

            header = new DBFHeader();
            header.Read(new BinaryReader(raf));

            /* position file pointer at the end of the raf */
            raf.Seek(-1, SeekOrigin.End);
            /* check whether the last byte is 0x1A (end of file marker for dbf files) - in this case move 1 byte back to ignore it when writing new records */ 
            var lastByte = raf.ReadByte();  /* Advances to end of stream */
            if (lastByte == DBFFieldType.EndOfData)
            {
                raf.Seek(-1, SeekOrigin.End);
            }

            recordCount = header.NumberOfRecords;
        }

        public byte Signature
        {
            get => header.Signature;
            set => header.Signature = value;
        }


        
        public string DataMemoLoc
        {
            get => _dataMemoLoc;
            set
            {
                _dataMemoLoc = value;
                
                _dataMemo?.Close();
                _dataMemo = File.Open(_dataMemoLoc,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite);
            }
        }

        public Stream DataMemo
        {
            get => _dataMemo;
            set => _dataMemo = value;
        }

        public byte LanguageDriver
        {
            set
            {
                if (header.LanguageDriver != 0x00)
                {
                    throw new DBFException("LanguageDriver has already been set");
                }

                header.LanguageDriver = value;
            }
        }


        public DBFField[] Fields
        {
            get => header.FieldArray;


            set
            {
                if (header.FieldArray != null)
                {
                    throw new DBFException("Fields has already been set");
                }

                if (value == null
                    || value.Length == 0)
                {
                    throw new DBFException("Should have at least one field");
                }

                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] == null)
                    {
                        throw new DBFException("Field " + (i + 1) + " is null");
                    }
                }

                header.FieldArray = value;

                try
                {
                    if (raf != null
                        && raf.Length == 0)
                    {
                        /*
						 this is a new/non-existent file. So write header before proceeding
						 */
                        header.Write(new BinaryWriter(raf));
                    }
                }
                catch (IOException e)
                {
                    throw new DBFException("Error accessing file", e);
                }
            }
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

        /**
		 Add a record.
		 */

        public void WriteRecord(params object[] values)
        {
            if (raf == null)
            {
                throw new DBFException(
                    "Not initialized with file for WriteRecord use, use AddRecord instead");
            }
            AddRecord(values, true);
        }

        public void AddRecord(params object[] values)
        {
            if (raf != null)
            {
                throw new DBFException(
                    "Appending to a file, requires using WriteRecord instead");
            }
            AddRecord(values, false);
        }

        private void AddRecord(object[] values, bool writeImmediately)
        {
            if (header.FieldArray == null)
            {
                throw new DBFException(
                    "Fields should be set before adding records");
            }

            if (values == null)
            {
                throw new DBFException("Null cannot be added as row");
            }

            if (values.Length
                != header.FieldArray.Length)
            {
                throw new DBFException(
                    "Invalid record. Invalid number of fields in row");
            }

            for (var i = 0; i < header.FieldArray.Length; i++)
            {
                var fld = header.FieldArray[i];
                var val = values[i];

                if (val is null || val is DBNull)
                {
                    continue;
                }

                void ThrowErrorIfNot<T>()
                {
                    if (!(val is T))
                    {
                        throw new DBFRecordException($"Invalid value '{val}' for field {i}({fld.Name}{fld.DataType})", 0);
                    }
                }

                switch (fld.DataType)
                {
                    case NativeDbType.Char:
                        //ignore all objects have ToString()
                        break;

                    case NativeDbType.Logical:
                        ThrowErrorIfNot<bool>();
                        break;

                    case NativeDbType.Numeric:
                        ThrowErrorIfNot<IConvertible>();
                        break;

                    case NativeDbType.Date:
                        ThrowErrorIfNot<DateTime>();
                        break;

                    case NativeDbType.Float:
                         ThrowErrorIfNot<IConvertible>();
                        break;
                    case NativeDbType.Memo:
                        ThrowErrorIfNot<MemoValue>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!writeImmediately)
            {
                v_records.Add(values);
            }
            else
            {
                try
                {
                    WriteRecord(new BinaryWriter(raf), values);
                    recordCount++;
                }
                catch (IOException e)
                {
                    throw new DBFException(
                        "Error occured while writing record. ", e);
                }
            }
        }

        ///Writes the set data to the OutputStream.
        public void Write(Stream tOut)
        {
            try
            {
                var outStream = new BinaryWriter(tOut);

                header.NumberOfRecords = v_records.Count;
                header.Write(outStream);

                /* Now write all the records */
                var t_recCount = v_records.Count;
                for (var i = 0; i < t_recCount; i++)
                {
                    /* iterate through records */

                    var t_values = (object[]) v_records[i];

                    WriteRecord(outStream, t_values);
                }

                outStream.Write(DBFFieldType.EndOfData);
                outStream.Flush();
            }
            catch (IOException e)
            {
                throw new DBFException("Error Writing", e);
            }
        }

        public void Close()
        {
            /* everything is written already. just update the header for record count and the END_OF_DATA mark */
            header.NumberOfRecords = recordCount;
            if (raf != null)
            {
                raf.Seek(0, SeekOrigin.Begin);
                header.Write(new BinaryWriter(raf));
                raf.Seek(0, SeekOrigin.End);
                raf.WriteByte(DBFFieldType.EndOfData);

                raf.Close();
                _dataMemo?.Close();

            } else if (!string.IsNullOrEmpty(DataMemoLoc))
            {
                DataMemo.Close();
            }

        }

        private void WriteRecord(BinaryWriter dataOutput, object[] objectArray)
        {
            dataOutput.Write((byte) ' ');
            for (var j = 0; j < header.FieldArray.Length; j++)
            {
                /* iterate through fields */

                switch (header.FieldArray[j].DataType)
                {
                    case NativeDbType.Char:
                        if (objectArray[j] != null && objectArray[j] != DBNull.Value)
                        {
                            var str_value = objectArray[j].ToString();
                            dataOutput.Write(
                                Utils.textPadding(str_value,
                                    CharEncoding,
                                    header.FieldArray[j].
                                        FieldLength
                                )
                            );
                        }
                        else
                        {
                            dataOutput.Write(
                                Utils.textPadding("",
                                    CharEncoding,
                                    header.FieldArray[j].
                                        FieldLength
                                )
                            );
                        }

                        break;

                    case NativeDbType.Date:
                        if (objectArray[j] != null && objectArray[j] != DBNull.Value)
                        {
                            var tDate = (DateTime) objectArray[j];

                            dataOutput.Write(
                                CharEncoding.GetBytes(tDate.ToString("yyyyMMdd")));
                        }
                        else
                        {
                            dataOutput.Write(
                                Utils.FillArray(new byte[8], DBFFieldType.Space));
                        }

                        break;

                    case NativeDbType.Float:

                        if (objectArray[j] != null && objectArray[j] != DBNull.Value)
                        {
                            var tDouble = Convert.ToDecimal(objectArray[j]);
                            dataOutput.Write(
                                Utils.NumericFormating(
                                    tDouble,
                                    CharEncoding,
                                    header.FieldArray[j].FieldLength,
                                    header.FieldArray[j].DecimalCount
                                )
                            );
                        }
                        else
                        {
                            dataOutput.Write(
                                Utils.textPadding(
                                    NullSymbol,
                                    CharEncoding,
                                    header.FieldArray[j].FieldLength,
                                    Utils.ALIGN_RIGHT
                                )
                            );
                        }

                        break;

                    case NativeDbType.Numeric:

                        if (objectArray[j] != null && objectArray[j] != DBNull.Value)
                        {
                            var tDecimal = Convert.ToDecimal(objectArray[j]);
                            dataOutput.Write(
                                Utils.NumericFormating(
                                    tDecimal,
                                    CharEncoding,
                                    header.FieldArray[j].FieldLength,
                                    header.FieldArray[j].DecimalCount
                                )
                            );
                        }
                        else
                        {
                            dataOutput.Write(
                                Utils.textPadding(
                                    NullSymbol,
                                    CharEncoding,
                                    header.FieldArray[j].FieldLength,
                                    Utils.ALIGN_RIGHT
                                )
                            );
                        }

                        break;
                    case NativeDbType.Logical:

                        if (objectArray[j] != null && objectArray[j] != DBNull.Value)
                        {
                            if ((bool) objectArray[j])
                            {
                                dataOutput.Write(DBFFieldType.True);
                            }
                            else
                            {
                                dataOutput.Write(DBFFieldType.False);
                            }
                        }
                        else
                        {
                            dataOutput.Write(DBFFieldType.UnknownByte);
                        }

                        break;

                    case NativeDbType.Memo:
                        if (objectArray[j] != null && objectArray[j] != DBNull.Value)
                        {
                            var tMemoValue = ((MemoValue) objectArray[j]);

                            tMemoValue.Write(this);

                            dataOutput.Write(Utils.NumericFormating(tMemoValue.Block, CharEncoding, 10, 0));
                        }
                        else
                        {
                            dataOutput.Write(
                                Utils.textPadding("",
                                    CharEncoding,
                                    10
                                )
                            );
                        }


                        break;

                    default:
                        throw new DBFException("Unknown field type "
                                               + header.FieldArray[j].DataType);
                }
            } /* iterating through the fields */
        }
    }
}

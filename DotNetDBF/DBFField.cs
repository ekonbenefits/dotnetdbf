/*
 DBFField
 Class represents a "field" (or column) definition of a DBF data structure.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 

 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DotNetDBF
{
    /// <summary>
    /// Class represents a "field" (or a column) definition of a DBF data structure.
    /// </summary>
    public class DBFField
    {
        #region Fields
        public const int SIZE = 32;
        public byte dataType; /* 11 */
        public byte decimalCount; /* 17 */
        public int fieldLength; /* 16 */
        public byte[] fieldName = new byte[11]; /* 0-10*/
        public byte indexFieldFlag; /* 31 */

        /* other class variables */
        public int nameNullIndex = 0;
        public int reserv1; /* 12-15 */
        public short reserv2; /* 18-19 */
        public short reserv3; /* 21-22 */
        public byte[] reserv4 = new byte[7]; /* 24-30 */
        public byte setFieldsFlag; /* 23 */
        public byte workAreaId; /* 20 */
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        public String Name
        {
            get { return Encoding.ASCII.GetString(fieldName, 0, nameNullIndex); }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Field name cannot be null.");
                }

                if (value.Length == 0 || value.Length > 10)
                {
                    throw new ArgumentException("Field name should be of length 0-10.");
                }

                fieldName = Encoding.ASCII.GetBytes(value);
                nameNullIndex = fieldName.Length;
            }
        }

        /// <summary>
        /// Gets the data type of the field.
        /// </summary>
        public Type Type
        {
            get
            {
                return Utils.TypeForNativeDBType(DataType);
            }
        }

        public NativeDbType DataType
        {
            get
            {
                return (NativeDbType)dataType;
            }
            set
            {
                switch (value)
                {
                    case NativeDbType.Date:
                        fieldLength = 8; /* fall through */
                        goto default;
                    case NativeDbType.Memo:
                        fieldLength = 10;
                        goto default;
                    case NativeDbType.Logical:
                        fieldLength = 1;
                        goto default;
                    default:
                        dataType = (byte)value;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the field length.
        /// <para><see cref="DecimalCount"/> must be set before setting value to this property.</para>
        /// </summary>
        public int FieldLength
        {
            get
            {
                if (DataType == NativeDbType.Char)
                {
                    return fieldLength + (decimalCount * 256);
                }

                return fieldLength;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Field length should be a positive number.");
                }

                if (DataType == NativeDbType.Date || DataType == NativeDbType.Memo || DataType == NativeDbType.Logical)
                {
                    throw new NotSupportedException("Cannot set length on this type of field.");
                }

                if (DataType == NativeDbType.Char && value > 255)
                {
                    fieldLength = value % 256;
                    decimalCount = (byte)(value / 256);
                    return;
                }

                fieldLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the decimal part for fields, which type is of numeric nature.
        /// <para>If the field type is any integer, the returned value will be zero.</para>
        /// </summary>
        public int DecimalCount
        {
            get { return decimalCount; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(
                        "Decimal length should be a positive number");
                }

                if (value > fieldLength)
                {
                    throw new ArgumentException(
                        "Decimal length should be less than field length");
                }

                decimalCount = (byte)value;
            }
        }

        public int Size
        {
            get { return SIZE; }
        }
        #endregion

        #region Constructors
        public DBFField()
        {
        }

        public DBFField(string fieldName, NativeDbType fieldType)
        {
            Name = fieldName;
            DataType = fieldType;
        }

        public DBFField(string fieldName, NativeDbType fieldType, Int32 fieldLength)
            : this(fieldName, fieldType)
        {
            FieldLength = fieldLength;
        }

        public DBFField(string fieldName, NativeDbType fieldType, Int32 fieldLength, Int32 decimalCount)
            :this(fieldName, fieldType, fieldLength)
        {
            DecimalCount = decimalCount;
        }
        #endregion

        public bool Read(BinaryReader br)
        {
            byte t_byte = br.ReadByte(); /* 0 */
            if (t_byte == DBFFieldType.EndOfField)
            {
                //System.out.println( "End of header found");
                return false;
            }

            br.Read(fieldName, 1, 10); /* 1-10 */
            fieldName[0] = t_byte;

            for (int i = 0; i < fieldName.Length; i++)
            {
                if (fieldName[i]
                    == 0)
                {
                    nameNullIndex = i;
                    break;
                }
            }

            dataType = br.ReadByte(); /* 11 */
            reserv1 = br.ReadInt32(); /* 12-15 */
            fieldLength = br.ReadByte(); /* 16 */
            decimalCount = br.ReadByte(); /* 17 */
            reserv2 = br.ReadInt16(); /* 18-19 */
            workAreaId = br.ReadByte(); /* 20 */
            reserv3 = br.ReadInt16(); /* 21-22 */
            setFieldsFlag = br.ReadByte(); /* 23 */
            br.Read(reserv4, 0, 7); /* 24-30 */
            indexFieldFlag = br.ReadByte(); /* 31 */
            return true;
        }

        /// <summary>
        /// Writes the content of DBFField object into the stream as per DBF format specifications.
        /// </summary>
        /// <param name="bw">Output stream.</param>
        /// <exception cref="IOException">If any stream related issues occur.</exception>
        public void Write(BinaryWriter bw)
        {
            // Field Name
            bw.Write(fieldName); /* 0-10 */
            bw.Write(new byte[11 - fieldName.Length],
                          0,
                          11 - fieldName.Length);

            // data type
            bw.Write(dataType); /* 11 */
            bw.Write(reserv1); /* 12-15 */
            bw.Write((byte) fieldLength); /* 16 */
            bw.Write(decimalCount); /* 17 */
            bw.Write(reserv2); /* 18-19 */
            bw.Write(workAreaId); /* 20 */
            bw.Write(reserv3); /* 21-22 */
            bw.Write(setFieldsFlag); /* 23 */
            bw.Write(reserv4); /* 24-30*/
            bw.Write(indexFieldFlag); /* 31 */
        }

        /// <summary>
        /// Creates a DBFField object from the data read from the given DataInputStream.
        /// <para>
        /// The data in the DataInputStream object is supposed to be organised correctly
        /// and the stream "pointer" is supposed to be positioned properly.
        /// </para>
        /// </summary>
        /// <param name="br">Data input stream.</param>
        /// <returns>New DBFField object.</returns>
        /// <exception cref="IOException">If any stream reading problems occures.</exception>
        static internal DBFField CreateField(BinaryReader br)
        {
            DBFField field = new DBFField();
            if (field.Read(br))
            {
                return field;
            }
            else
            {
                return null;
            }
        }

        public override string ToString()
        {
            return string.Format("Field: {0}, Length: {1}", Name, FieldLength);
        }
    }
}
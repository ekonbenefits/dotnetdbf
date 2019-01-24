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
    [DebuggerDisplay("Field:{Name}, Length:{FieldLength}")]
    public class DBFField
    {
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

        public DBFField()
        {
        }

        public DBFField(string fieldName, NativeDbType type)
        {
            Name = fieldName;
            DataType = type;
        }

        public DBFField(string fieldName,
                        NativeDbType type,
                        int fieldLength)
        {
            Name = fieldName;
            DataType = type;
            FieldLength = fieldLength;
        }

        public DBFField(string fieldName,
                        NativeDbType type,
                        int fieldLength,
                        int decimalCount)
        {
            Name = fieldName;
            DataType = type;
            FieldLength = fieldLength;
            DecimalCount = decimalCount;
        }

        public int Size => SIZE;

        /**
         Returns the name of the field.
         
         @return Name of the field as String.
         */

        public string Name
        {
            get => Encoding.ASCII.GetString(fieldName, 0, nameNullIndex);
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Field name cannot be null");
                }

                if (value.Length == 0
                    || value.Length > 10)
                {
                    throw new ArgumentException(
                        "Field name should be of length 0-10");
                }

                fieldName = Encoding.ASCII.GetBytes(value);
                nameNullIndex = fieldName.Length;
            }
        }

        /**
         Returns the data type of the field.
         
         @return Data type as byte.
         */

        public Type Type => Utils.TypeForNativeDBType(DataType);


        public NativeDbType DataType
        {
            get => (NativeDbType)dataType;
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

        /**
         Returns field length.
         
         @return field length as int.
         */

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
            /**
             Length of the field.
             This method should be called before calling setDecimalCount().
             
             @param Length of the field as int.
             */
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(
                        "Field length should be a positive number");
                }

                switch (DataType)
                {
                    case NativeDbType.Date:
                    case NativeDbType.Memo:
                    case NativeDbType.Logical:
                        throw new NotSupportedException(
                            "Cannot set length on this type of field");
                    case NativeDbType.Char when value > 255:
                        fieldLength = value % 256;
                        decimalCount = (byte) (value / 256);
                        return;
                    default:
                        fieldLength = value;
                        break;
                }
            }
        }

        /**
         Returns the decimal part. This is applicable
         only if the field type if of numeric in nature.
         
         If the field is specified to hold integral values
         the value returned by this method will be zero.
         
         @return decimal field size as int.
         */

        public int DecimalCount
        {
            get => decimalCount;
            /**
             Sets the decimal place size of the field.
             Before calling this method the size of the field
             should be set by calling setFieldLength().
             
             @param Size of the decimal field.
             */
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

                decimalCount = (byte) value;
            }
        }

        public bool Read(BinaryReader reader)
        {
            var t_byte = reader.ReadByte(); /* 0 */
            if (t_byte == DBFFieldType.EndOfField)
            {
                //System.out.println( "End of header found");
                return false;
            }

            reader.Read(fieldName, 1, 10); /* 1-10 */
            fieldName[0] = t_byte;

            for (var i = 0; i < fieldName.Length; i++)
            {
                if (fieldName[i]
                    == 0)
                {
                    nameNullIndex = i;
                    break;
                }
            }

            dataType = reader.ReadByte(); /* 11 */
            reserv1 = reader.ReadInt32(); /* 12-15 */
            fieldLength = reader.ReadByte(); /* 16 */
            decimalCount = reader.ReadByte(); /* 17 */
            reserv2 = reader.ReadInt16(); /* 18-19 */
            workAreaId = reader.ReadByte(); /* 20 */
            reserv3 = reader.ReadInt16(); /* 21-22 */
            setFieldsFlag = reader.ReadByte(); /* 23 */
            reader.Read(reserv4, 0, 7); /* 24-30 */
            indexFieldFlag = reader.ReadByte(); /* 31 */
            return true;
        }

        /**
         Writes the content of DBFField object into the stream as per
         DBF format specifications.
         
         @param os OutputStream
         @throws IOException if any stream related issues occur.
         */

        public void Write(BinaryWriter writer)
        {
            // Field Name
            writer.Write(fieldName); /* 0-10 */
            writer.Write(new byte[11 - fieldName.Length],
                          0,
                          11 - fieldName.Length);

            // data type
            writer.Write(dataType); /* 11 */
            writer.Write(reserv1); /* 12-15 */
            writer.Write((byte) fieldLength); /* 16 */
            writer.Write(decimalCount); /* 17 */
            writer.Write(reserv2); /* 18-19 */
            writer.Write(workAreaId); /* 20 */
            writer.Write(reserv3); /* 21-22 */
            writer.Write(setFieldsFlag); /* 23 */
            writer.Write(reserv4); /* 24-30*/
            writer.Write(indexFieldFlag); /* 31 */
        }

        /**
         Creates a DBFField object from the data read from the given DataInputStream.
         
         The data in the DataInputStream object is supposed to be organised correctly
         and the stream "pointer" is supposed to be positioned properly.
         
         @param in DataInputStream
         @return Returns the created DBFField object.
         @throws IOException If any stream reading problems occurs.
         */

        internal static DBFField CreateField(BinaryReader reader)
        {
            var field = new DBFField();
            if (field.Read(reader))
            {
                return field;
            }
            else
            {
                return null;
            }
        }
    }
}
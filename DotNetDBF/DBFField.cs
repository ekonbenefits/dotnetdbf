/*
 DBFField
 Class represents a "field" (or column) definition of a DBF data structure.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 

 */

using System;
using System.IO;
using System.Text;

namespace DotNetDBF
{
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

        public DBFField(string aFieldName, NativeDbType aType)
        {
            Name = aFieldName;
            DataType = aType;
        }

        public DBFField(string aFieldName,
                        NativeDbType aType,
                        Int32 aFieldLength)
        {
            Name = aFieldName;
            DataType = aType;
            FieldLength = aFieldLength;
        }

        public DBFField(string aFieldName,
                        NativeDbType aType,
                        Int32 aFieldLength,
                        Int32 aDecimalCount)
        {
            Name = aFieldName;
            DataType = aType;
            FieldLength = aFieldLength;
            DecimalCount = aDecimalCount;
        }

        public int Size
        {
            get { return SIZE; }
        }

        /**
		 Returns the name of the field.
		 
		 @return Name of the field as String.
		 */

        public String Name
        {
            get { return Encoding.ASCII.GetString(fieldName, 0, nameNullIndex); }
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

        public NativeDbType DataType
        {
            get
            {
                return
                    (NativeDbType)
                    Enum.ToObject(typeof (NativeDbType), dataType);
            }
            set
            {
                switch (value)
                {
                    case NativeDbType.Date:
                        fieldLength = 8; /* fall through */
                        goto case NativeDbType.Memo;
                    case NativeDbType.Char:
                    case NativeDbType.Logical:
                    case NativeDbType.Numeric:
                    case NativeDbType.Float:
                    case NativeDbType.Memo:
                        dataType = (byte) value;
                        break;

                    default:
                        throw new ArgumentException("Unknown data type");
                }
            }
        }

        /**
		 Returns field length.
		 
		 @return field length as int.
		 */

        public int FieldLength
        {
            get { return fieldLength; }
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

                if (DataType == NativeDbType.Date)
                {
                    throw new NotSupportedException(
                        "Cannot do this on a Date field");
                }

                fieldLength = value;
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
            get { return decimalCount; }
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

        public bool Read(BinaryReader aReader)
        {
            byte t_byte = aReader.ReadByte(); /* 0 */
            if (t_byte == DBFValue.EndOfField)
            {
                //System.out.println( "End of header found");
                return false;
            }

            aReader.Read(fieldName, 1, 10); /* 1-10 */
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

            dataType = aReader.ReadByte(); /* 11 */
            reserv1 = aReader.ReadInt32(); /* 12-15 */
            fieldLength = aReader.ReadByte(); /* 16 */
            decimalCount = aReader.ReadByte(); /* 17 */
            reserv2 = aReader.ReadInt16(); /* 18-19 */
            workAreaId = aReader.ReadByte(); /* 20 */
            reserv3 = aReader.ReadInt16(); /* 21-22 */
            setFieldsFlag = aReader.ReadByte(); /* 23 */
            aReader.Read(reserv4, 0, 7); /* 24-30 */
            indexFieldFlag = aReader.ReadByte(); /* 31 */
            return true;
        }

        /**
		 Writes the content of DBFField object into the stream as per
		 DBF format specifications.
		 
		 @param os OutputStream
		 @throws IOException if any stream related issues occur.
		 */

        public void Write(BinaryWriter aWriter)
        {
            // Field Name
            aWriter.Write(fieldName); /* 0-10 */
            aWriter.Write(new byte[11 - fieldName.Length],
                          0,
                          11 - fieldName.Length);

            // data type
            aWriter.Write(dataType); /* 11 */
            aWriter.Write(reserv1); /* 12-15 */
            aWriter.Write((byte) fieldLength); /* 16 */
            aWriter.Write(decimalCount); /* 17 */
            aWriter.Write(reserv2); /* 18-19 */
            aWriter.Write(workAreaId); /* 20 */
            aWriter.Write(reserv3); /* 21-22 */
            aWriter.Write(setFieldsFlag); /* 23 */
            aWriter.Write(reserv4); /* 24-30*/
            aWriter.Write(indexFieldFlag); /* 31 */
        }

        /**
		 Creates a DBFField object from the data read from the given DataInputStream.
		 
		 The data in the DataInputStream object is supposed to be organised correctly
		 and the stream "pointer" is supposed to be positioned properly.
		 
		 @param in DataInputStream
		 @return Returns the created DBFField object.
		 @throws IOException If any stream reading problems occures.
		 */

        static internal DBFField CreateField(BinaryReader aReader)
        {
            DBFField field = new DBFField();
            if (field.Read(aReader))
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
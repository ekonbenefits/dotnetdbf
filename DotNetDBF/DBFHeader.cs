/*
 DBFHeader
 Class for reading the metadata assuming that the given
 InputStream carries DBF data.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 
 License: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 

 */

using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetDBF
{
    public class DBFHeader
    {
        public const byte SIG_DBASE_III = 0x03;
        /* DBF structure start here */

        private byte day; /* 3 */
        private byte encryptionFlag; /* 15 */
        private DBFField[] fieldArray; /* each 32 bytes */
        private int freeRecordThread; /* 16-19 */
        private short headerLength; /* 8-9 */
        private byte incompleteTransaction; /* 14 */
        private byte languageDriver; /* 29 */
        private byte mdxFlag; /* 28 */
        private byte month; /* 2 */
        private int numberOfRecords; /* 4-7 */
        private short recordLength; /* 10-11 */
        private short reserv1; /* 12-13 */
        private int reserv2; /* 20-23 */
        private int reserv3; /* 24-27 */
        private short reserv4; /* 30-31 */
        private byte signature; /* 0 */
        private byte terminator1; /* n+1 */
        private byte year; /* 1 */

        public DBFHeader()
        {
            signature = SIG_DBASE_III;
            terminator1 = 0x0D;
        }

        internal short Size
        {
            get
            {
                return (short) (sizeof (byte) +
                                sizeof (byte) + sizeof (byte) + sizeof (byte) +
                                sizeof (int) +
                                sizeof (short) +
                                sizeof (short) +
                                sizeof (short) +
                                sizeof (byte) +
                                sizeof (byte) +
                                sizeof (int) +
                                sizeof (int) +
                                sizeof (int) +
                                sizeof (byte) +
                                sizeof (byte) +
                                sizeof (short) +
                                (DBFField.SIZE * fieldArray.Length) +
                                sizeof (byte));
            }
        }

        internal short RecordSize
        {
            get
            {
                int tRecordLength = 0;
                for (int i = 0; i < fieldArray.Length; i++)
                {
                    tRecordLength += fieldArray[i].FieldLength;
                }

                return (short)(tRecordLength + 1);
            }
        }

        internal short HeaderLength
        {
            set { headerLength = value; }

            get { return headerLength; }
        }

        internal DBFField[] FieldArray
        {
            set { fieldArray = value; }

            get { return fieldArray; }
        }

        internal byte Year
        {
            set { year = value; }

            get { return year; }
        }

        internal byte Month
        {
            set { month = value; }

            get { return month; }
        }

        internal byte Day
        {
            set { day = value; }

            get { return day; }
        }

        internal int NumberOfRecords
        {
            set { numberOfRecords = value; }

            get { return numberOfRecords; }
        }

        internal short RecordLength
        {
            set { recordLength = value; }

            get { return recordLength; }
        }

        internal void Read(BinaryReader dataInput)
        {
            signature = dataInput.ReadByte(); /* 0 */
            year = dataInput.ReadByte(); /* 1 */
            month = dataInput.ReadByte(); /* 2 */
            day = dataInput.ReadByte(); /* 3 */
            numberOfRecords = dataInput.ReadInt32(); /* 4-7 */

            headerLength = dataInput.ReadInt16(); /* 8-9 */
            recordLength = dataInput.ReadInt16(); /* 10-11 */

            reserv1 = dataInput.ReadInt16(); /* 12-13 */
            incompleteTransaction = dataInput.ReadByte(); /* 14 */
            encryptionFlag = dataInput.ReadByte(); /* 15 */
            freeRecordThread = dataInput.ReadInt32(); /* 16-19 */
            reserv2 = dataInput.ReadInt32(); /* 20-23 */
            reserv3 = dataInput.ReadInt32(); /* 24-27 */
            mdxFlag = dataInput.ReadByte(); /* 28 */
            languageDriver = dataInput.ReadByte(); /* 29 */
            reserv4 = dataInput.ReadInt16(); /* 30-31 */


            List<DBFField> v_fields = new List<DBFField>();

            DBFField field = DBFField.CreateField(dataInput); /* 32 each */
            while (field != null)
            {
                v_fields.Add(field);
                field = DBFField.CreateField(dataInput);
            }

            fieldArray = v_fields.ToArray();
            //System.out.println( "Number of fields: " + fieldArray.length);
        }

        internal void Write(BinaryWriter dataOutput)
        {
            dataOutput.Write(signature); /* 0 */
            DateTime tNow = DateTime.Now;
            year = (byte) (tNow.Year - 1900);
            month = (byte) (tNow.Month);
            day = (byte) (tNow.Day);

            dataOutput.Write(year); /* 1 */
            dataOutput.Write(month); /* 2 */
            dataOutput.Write(day); /* 3 */

            //System.out.println( "Number of records in O/S: " + numberOfRecords);
            dataOutput.Write(numberOfRecords); /* 4-7 */

            headerLength = Size;
            dataOutput.Write(headerLength); /* 8-9 */

            recordLength = RecordSize;
            dataOutput.Write(recordLength); /* 10-11 */

            dataOutput.Write(reserv1); /* 12-13 */
            dataOutput.Write(incompleteTransaction); /* 14 */
            dataOutput.Write(encryptionFlag); /* 15 */
            dataOutput.Write(freeRecordThread); /* 16-19 */
            dataOutput.Write(reserv2); /* 20-23 */
            dataOutput.Write(reserv3); /* 24-27 */

            dataOutput.Write(mdxFlag); /* 28 */
            dataOutput.Write(languageDriver); /* 29 */
            dataOutput.Write(reserv4); /* 30-31 */

            for (int i = 0; i < fieldArray.Length; i++)
            {
                //System.out.println( "Length: " + fieldArray[i].getFieldLength());
                fieldArray[i].Write(dataOutput);
            }

            dataOutput.Write(terminator1); /* n+1 */
        }
    }
}
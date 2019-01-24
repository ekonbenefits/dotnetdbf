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
    [Obsolete("Use DBFSignature instead", error:true)]
    public static class DBFSigniture
    {
        public const byte NotSet = 0,
            WithMemo = 0x80,
            DBase3 = 0x03,
            DBase3WithMemo = DBase3 | WithMemo;
    }

    public static class DBFSignature
    {
        public const byte NotSet = 0,
            WithMemo = 0x80,
            DBase3 = 0x03,
            DBase3WithMemo = DBase3 | WithMemo;
    }

    [Flags]
    public enum MemoFlags : byte
    {
    }


    public class DBFHeader
    {
        public const byte HeaderRecordTerminator = 0x0D;
        
        internal byte Signature { get; set; } /* 0 */
        internal byte Year { set; get; } /* 1 */
        internal byte Month { set; get; }  /* 2 */
        internal byte Day { set; get; } /* 3 */
        internal int NumberOfRecords { set; get; } /* 4-7 */
        internal short HeaderLength { set; get; }  /* 8-9 */
        internal short RecordLength { set; get; } /* 10-11 */
        private short _reserv1; /* 12-13 */
        private byte _incompleteTransaction; /* 14 */
        private byte _encryptionFlag; /* 15 */
        private int _freeRecordThread; /* 16-19 */
        private int _reserv2; /* 20-23 */
        private int _reserv3; /* 24-27 */
        private byte _mdxFlag; /* 28 */
        internal byte LanguageDriver { get; set; }  /* 29 */
        private short _reserv4; /* 30-31 */
        internal DBFField[] FieldArray { set; get; } /* each 32 bytes */

        
        public DBFHeader()
        {
            Signature = DBFSignature.DBase3;
        }

      

        internal short Size => (short) (sizeof(byte) +
                                        sizeof(byte) + sizeof(byte) + sizeof(byte) +
                                        sizeof(int) +
                                        sizeof(short) +
                                        sizeof(short) +
                                        sizeof(short) +
                                        sizeof(byte) +
                                        sizeof(byte) +
                                        sizeof(int) +
                                        sizeof(int) +
                                        sizeof(int) +
                                        sizeof(byte) +
                                        sizeof(byte) +
                                        sizeof(short) +
                                        (DBFField.SIZE * FieldArray.Length) +
                                        sizeof(byte));

        internal short RecordSize
        {
            get
            {
                var tRecordLength = 0;
                for (var i = 0; i < FieldArray.Length; i++)
                {
                    tRecordLength += FieldArray[i].FieldLength;
                }

                return (short) (tRecordLength + 1);
            }
        }

        internal void Read(BinaryReader dataInput)
        {
            Signature = dataInput.ReadByte(); /* 0 */
            Year = dataInput.ReadByte(); /* 1 */
            Month = dataInput.ReadByte(); /* 2 */
            Day = dataInput.ReadByte(); /* 3 */
            NumberOfRecords = dataInput.ReadInt32(); /* 4-7 */

            HeaderLength = dataInput.ReadInt16(); /* 8-9 */
            RecordLength = dataInput.ReadInt16(); /* 10-11 */

            _reserv1 = dataInput.ReadInt16(); /* 12-13 */
            _incompleteTransaction = dataInput.ReadByte(); /* 14 */
            _encryptionFlag = dataInput.ReadByte(); /* 15 */
            _freeRecordThread = dataInput.ReadInt32(); /* 16-19 */
            _reserv2 = dataInput.ReadInt32(); /* 20-23 */
            _reserv3 = dataInput.ReadInt32(); /* 24-27 */
            _mdxFlag = dataInput.ReadByte(); /* 28 */
            LanguageDriver = dataInput.ReadByte(); /* 29 */
            _reserv4 = dataInput.ReadInt16(); /* 30-31 */


            var v_fields = new List<DBFField>();

            var field = DBFField.CreateField(dataInput); /* 32 each */
            while (field != null)
            {
                v_fields.Add(field);
                field = DBFField.CreateField(dataInput);
            }

            FieldArray = v_fields.ToArray();
            //System.out.println( "Number of fields: " + _fieldArray.length);
        }

        internal void Write(BinaryWriter dataOutput)
        {
            dataOutput.Write(Signature); /* 0 */
            var tNow = DateTime.Now;
            Year = (byte) (tNow.Year - 1900);
            Month = (byte) (tNow.Month);
            Day = (byte) (tNow.Day);

            dataOutput.Write(Year); /* 1 */
            dataOutput.Write(Month); /* 2 */
            dataOutput.Write(Day); /* 3 */

            //System.out.println( "Number of records in O/S: " + numberOfRecords);
            dataOutput.Write(NumberOfRecords); /* 4-7 */

            HeaderLength = Size;
            dataOutput.Write(HeaderLength); /* 8-9 */

            RecordLength = RecordSize;
            dataOutput.Write(RecordLength); /* 10-11 */

            dataOutput.Write(_reserv1); /* 12-13 */
            dataOutput.Write(_incompleteTransaction); /* 14 */
            dataOutput.Write(_encryptionFlag); /* 15 */
            dataOutput.Write(_freeRecordThread); /* 16-19 */
            dataOutput.Write(_reserv2); /* 20-23 */
            dataOutput.Write(_reserv3); /* 24-27 */

            dataOutput.Write(_mdxFlag); /* 28 */
            dataOutput.Write(LanguageDriver); /* 29 */
            dataOutput.Write(_reserv4); /* 30-31 */

            foreach (var field in FieldArray)
            {
                field.Write(dataOutput);
            }

            dataOutput.Write(HeaderRecordTerminator); /* n+1 */
        }
    }
}
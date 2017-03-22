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
    public enum DBFSignature : byte
    {
        NotSet = 0,
        WithMemo = 0x80,
        DBase3 = 0x03,
        DBase3WithMemo = DBase3 | WithMemo
    }

    /// <summary>
    /// Class for reading the metadata assuming that the given FileStream carries DBF data.
    /// </summary>
    public class DBFHeader
    {
        #region Fields
        public const byte HeaderRecordTerminator = 0x0D;

        private byte _day; /* 3 */
        private byte _encryptionFlag; /* 15 */
        private DBFField[] _fieldArray; /* each 32 bytes */
        private int _freeRecordThread; /* 16-19 */
        private short _headerLength; /* 8-9 */
        private byte _incompleteTransaction; /* 14 */
        private byte _languageDriver; /* 29 */
        private byte _mdxFlag; /* 28 */
        private byte _month; /* 2 */
        private int _numberOfRecords; /* 4-7 */
        private short _recordLength; /* 10-11 */
        private short _reserv1; /* 12-13 */
        private int _reserv2; /* 20-23 */
        private int _reserv3; /* 24-27 */
        private short reserv4; /* 30-31 */
        private DBFSignature _signature; /* 0 */
        private byte _year; /* 1 */
        #endregion

        #region Properties

        internal DBFSignature Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }

        internal short Size
        {
            get
            {
                return (short)(sizeof(byte) +
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
                                (DBFField.SIZE * _fieldArray.Length) +
                                sizeof(byte));
            }
        }

        internal short RecordSize
        {
            get
            {
                int recordLength = 0;
                for (int i = 0; i < _fieldArray.Length; i++)
                {
                    recordLength += _fieldArray[i].FieldLength;
                }

                return (short)(recordLength + 1);
            }
        }

        internal short HeaderLength
        {
            get { return _headerLength; }
            set { _headerLength = value; }
        }

        internal DBFField[] FieldArray
        {
            get { return _fieldArray; }
            set { _fieldArray = value; }
        }

        internal byte Year
        {
            get { return _year; }
            set { _year = value; }
        }

        internal byte Month
        {
            get { return _month; }
            set { _month = value; }
        }

        internal byte Day
        {
            get { return _day; }
            set { _day = value; }
        }

        internal int NumberOfRecords
        {
            get { return _numberOfRecords; }
            set { _numberOfRecords = value; }
        }

        internal short RecordLength
        {
            get { return _recordLength; }
            set { _recordLength = value; }
        }

        internal byte LanguageDriver
        {
            get { return _languageDriver; }
            set { _languageDriver = value; }
        }
        #endregion

        #region Constructors
        public DBFHeader()
        {
            _signature = DBFSignature.DBase3;
        }
        #endregion

        internal void Read(BinaryReader dataInput)
        {
            _signature = (DBFSignature)dataInput.ReadByte(); /* 0 */
            _year = dataInput.ReadByte(); /* 1 */
            _month = dataInput.ReadByte(); /* 2 */
            _day = dataInput.ReadByte(); /* 3 */
            _numberOfRecords = dataInput.ReadInt32(); /* 4-7 */

            _headerLength = dataInput.ReadInt16(); /* 8-9 */
            _recordLength = dataInput.ReadInt16(); /* 10-11 */

            _reserv1 = dataInput.ReadInt16(); /* 12-13 */
            _incompleteTransaction = dataInput.ReadByte(); /* 14 */
            _encryptionFlag = dataInput.ReadByte(); /* 15 */
            _freeRecordThread = dataInput.ReadInt32(); /* 16-19 */
            _reserv2 = dataInput.ReadInt32(); /* 20-23 */
            _reserv3 = dataInput.ReadInt32(); /* 24-27 */
            _mdxFlag = dataInput.ReadByte(); /* 28 */
            _languageDriver = dataInput.ReadByte(); /* 29 */
            reserv4 = dataInput.ReadInt16(); /* 30-31 */


            List<DBFField> fields = new List<DBFField>();

            DBFField field = DBFField.CreateField(dataInput); /* 32 each */
            while (field != null)
            {
                fields.Add(field);
                field = DBFField.CreateField(dataInput);
            }

            _fieldArray = fields.ToArray();
            //System.out.println( "Number of fields: " + _fieldArray.length);
        }

        internal void Write(BinaryWriter dataOutput)
        {
            dataOutput.Write((byte)_signature); /* 0 */
            DateTime tNow = DateTime.Now;
            _year = (byte)(tNow.Year - 1900);
            _month = (byte)(tNow.Month);
            _day = (byte)(tNow.Day);

            dataOutput.Write(_year); /* 1 */
            dataOutput.Write(_month); /* 2 */
            dataOutput.Write(_day); /* 3 */

            //System.out.println( "Number of records in O/S: " + numberOfRecords);
            dataOutput.Write(_numberOfRecords); /* 4-7 */

            _headerLength = Size;
            dataOutput.Write(_headerLength); /* 8-9 */

            _recordLength = RecordSize;
            dataOutput.Write(_recordLength); /* 10-11 */

            dataOutput.Write(_reserv1); /* 12-13 */
            dataOutput.Write(_incompleteTransaction); /* 14 */
            dataOutput.Write(_encryptionFlag); /* 15 */
            dataOutput.Write(_freeRecordThread); /* 16-19 */
            dataOutput.Write(_reserv2); /* 20-23 */
            dataOutput.Write(_reserv3); /* 24-27 */

            dataOutput.Write(_mdxFlag); /* 28 */
            dataOutput.Write(_languageDriver); /* 29 */
            dataOutput.Write(reserv4); /* 30-31 */

            for (int i = 0; i < _fieldArray.Length; i++)
            {
                //System.out.println( "Length: " + _fieldArray[i].getFieldLength());
                _fieldArray[i].Write(dataOutput);
            }

            dataOutput.Write(HeaderRecordTerminator); /* n+1 */
        }
    }
}
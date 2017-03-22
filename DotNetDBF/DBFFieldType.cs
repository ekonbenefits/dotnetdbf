/*
 DBFFieldType
 Class for reading the records assuming that the given
 InputStream comtains DBF data.
 
 This file is part of DotNetDBF packege.
 
 author (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 
 License: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 */

using System.Data;

namespace DotNetDBF
{
    public enum NativeDbType : byte
    {
        Autoincrement = 0x2B, //+ in ASCII
        Timestamp =     0x40, //@ in ASCII
        Binary =        0x42, //B in ASCII
        Char =          0x43, //C in ASCII
        Date =          0x44, //D in ASCII
        Float =         0x46, //F in ASCII
        Ole =           0x47, //G in ASCII
        Long =          0x49, //I in ASCII
        Logical =       0x4C, //L in ASCII
        Memo =          0x4D, //M in ASCII
        Numeric =       0x4E, //N in ASCII
        Double =        0x4F, //O in ASCII
    }

    /// <summary>
    /// Class for reading the records assuming that the given Stream contains DBF data.
    /// </summary>
    public static class DBFFieldType
    {
        public const byte EndOfData = 0x1A; //^Z End of File
        public const byte EndOfField = 0x0D; //End of Field
        public const byte False = 0x46; //F in Ascci
        public const byte Space = 0x20; //Space in ascii
        public const byte True = 0x54; //T in ascii
        public const byte UnknownByte = 0x3F; //Unknown Bool value
        public const string Unknown = "?"; //Unknown value

        public static DbType FromNative(NativeDbType byteValue)
        {
            switch (byteValue)
            {
                case NativeDbType.Char:
                    return DbType.AnsiStringFixedLength;
                case NativeDbType.Logical:
                    return DbType.Boolean;
                case NativeDbType.Numeric:
                    return DbType.Decimal;
                case NativeDbType.Date:
                    return DbType.Date;
                case NativeDbType.Float:
                    return DbType.Decimal;
                case NativeDbType.Memo:
                    return DbType.AnsiString;
                default:
                    throw new DBFException(
                        string.Format("Unsupported Native Type {0}", byteValue));
            }
        }

        public static NativeDbType FromDbType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.AnsiStringFixedLength:
                    return NativeDbType.Char;
                case DbType.Boolean:
                    return NativeDbType.Logical;
                case DbType.Decimal:
                    return NativeDbType.Numeric;
                case DbType.Date:
                    return NativeDbType.Date;
                case DbType.AnsiString:
                    return NativeDbType.Memo;
                default:
                    throw new DBFException(
                        string.Format("Unsupported Type {0}", dbType));
            }
        }
    }
}
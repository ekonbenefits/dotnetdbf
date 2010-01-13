/*
 Utils
 Class for contining utility functions.
 
 This file is part of JavaDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007

 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace DotNetDBF
{
    static public class Utils
    {

        public const int ALIGN_LEFT = 10;
        public const int ALIGN_RIGHT = 12;

        static public byte[] FillArray(byte[] anArray, byte value)
        {
            for (int i = 0; i < anArray.Length; i++)
            {
                anArray[i] = value;
            }
            return anArray;
        }

        static public byte[] trimLeftSpaces(byte[] arr)
        {
            List<byte> tList = new List<byte>(arr.Length);

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != ' ')
                {
                    tList.Add(arr[i]);
                }
            }
            return tList.ToArray();
        }

        static public byte[] textPadding(String text,
                                         Encoding charEncoding,
                                         int length)
        {
            return textPadding(text, charEncoding, length, ALIGN_LEFT);
        }

        static public byte[] textPadding(String text,
                                         Encoding charEncoding,
                                         int length,
                                         int alignment)
        {
            return
                textPadding(text,
                            charEncoding,
                            length,
                            alignment,
                            DBFFieldType.Space);
        }

        static public byte[] textPadding(String text,
                                         Encoding charEncoding,
                                         int length,
                                         int alignment,
                                         byte paddingByte)
        {
            Encoding tEncoding = charEncoding;
            if (text.Length >= length)
            {
                return tEncoding.GetBytes(text.Substring(0, length));
            }

            byte[] byte_array = FillArray(new byte[length], paddingByte);

            switch (alignment)
            {
                case ALIGN_LEFT:
                    Array.Copy(tEncoding.GetBytes(text),
                               0,
                               byte_array,
                               0,
                               text.Length);
                    break;

                case ALIGN_RIGHT:
                    int t_offset = length - text.Length;
                    Array.Copy(tEncoding.GetBytes(text),
                               0,
                               byte_array,
                               t_offset,
                               text.Length);
                    break;
            }

            return byte_array;
        }

        static public byte[] NumericFormating(IFormattable doubleNum,
                                              Encoding charEncoding,
                                              int fieldLength,
                                              int sizeDecimalPart)
        {
            int sizeWholePart = fieldLength
                                -
                                (sizeDecimalPart > 0 ? (sizeDecimalPart + 1) : 0);

            StringBuilder format = new StringBuilder(fieldLength);

            for (int i = 0; i < sizeWholePart; i++)
            {
                format.Append("#");
            }

            if (sizeDecimalPart > 0)
            {
                format.Append(".");

                for (int i = 0; i < sizeDecimalPart; i++)
                {
                    format.Append("0");
                }
            }


            return
                textPadding(
                    doubleNum.ToString(format.ToString(),
                                       NumberFormatInfo.CurrentInfo),
                    charEncoding,
                    fieldLength,
                    ALIGN_RIGHT);
        }

        static public bool contains(byte[] arr, byte value)
        {
            return
                Array.Exists(arr,
                             delegate(byte anItem) { return anItem == value; });
        }


        static public Type TypeForNativeDBType(NativeDbType aType)
        {
            switch(aType)
            {
                case NativeDbType.Char:
                    return typeof (string);
                case NativeDbType.Date:
                    return typeof (DateTime);
                case NativeDbType.Numeric:
                    return typeof (decimal);
                case NativeDbType.Logical:
                    return typeof (bool);
                case NativeDbType.Float:
                    return typeof (float);
                case NativeDbType.Memo:
                    return typeof (MemoValue);
                default:
                    throw new ArgumentException("Unsupported Type");
            }
        }
    }
}
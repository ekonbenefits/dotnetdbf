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
using System.Linq;
using System.Text;

namespace DotNetDBF
{
    /// <summary>
    /// Static class that contains utility functions.
    /// </summary>
    public static class Utils
    {
        public const int ALIGN_LEFT = 10;
        public const int ALIGN_RIGHT = 12;

        public static byte[] FillArray(byte[] anArray, byte value)
        {
            for (int i = 0; i < anArray.Length; i++)
            {
                anArray[i] = value;
            }
            return anArray;
        }

        public static byte[] trimLeftSpaces(byte[] array)
        {
            List<byte> list = new List<byte>(array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != ' ')
                {
                    list.Add(array[i]);
                }
            }
            return list.ToArray();
        }

        public static byte[] textPadding(String text, Encoding charEncoding, int length)
        {
            return textPadding(text, charEncoding, length, ALIGN_LEFT);
        }

        public static byte[] textPadding(String text, Encoding charEncoding, int length, int alignment)
        {
            return textPadding(text, charEncoding, length, alignment, DBFFieldType.Space);
        }

        public static byte[] textPadding(String text, Encoding charEncoding, int length, int alignment, byte paddingByte)
        {
            Encoding encoding = charEncoding;
            var inputBytes = encoding.GetBytes(text);
            if (inputBytes.Length >= length)
            {
                return inputBytes.Take(length).ToArray();
            }

            byte[] array = FillArray(new byte[length], paddingByte);

            switch (alignment)
            {
                case ALIGN_LEFT:
                    Array.Copy(inputBytes, 0, array, 0, inputBytes.Length);
                    break;
                case ALIGN_RIGHT:
                    int offset = length - text.Length;
                    Array.Copy(inputBytes, 0, array, offset, inputBytes.Length);
                    break;
            }

            return array;
        }

        public static byte[] NumericFormating(IFormattable doubleNum, Encoding charEncoding, int fieldLength, int sizeDecimalPart)
        {
            int sizeWholePart = fieldLength - (sizeDecimalPart > 0 ? (sizeDecimalPart + 1) : 0);

            StringBuilder format = new StringBuilder(fieldLength);

            for (int i = 0; i < sizeWholePart; i++)
            {
                format.Append(i + 1 == sizeWholePart ? "0" : "#");
            }

            if (sizeDecimalPart > 0)
            {
                format.Append(".");

                for (int i = 0; i < sizeDecimalPart; i++)
                {
                    format.Append("0");
                }
            }
            return textPadding(doubleNum.ToString(format.ToString(), NumberFormatInfo.InvariantInfo), charEncoding, fieldLength, ALIGN_RIGHT);
        }

        public static bool contains(byte[] array, byte value)
        {
            return Array.Exists(array, item => item == value);
        }

        public static Type TypeForNativeDBType(NativeDbType aType)
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
                    throw new ArgumentException("Unsupported type.");
            }
        }
    }
}
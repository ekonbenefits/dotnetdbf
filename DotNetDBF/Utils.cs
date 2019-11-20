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
using System.Threading;

namespace DotNetDBF
{
    public static class Utils
    {
        public const int ALIGN_LEFT = 10;
        public const int ALIGN_RIGHT = 12;

        public static byte[] FillArray(byte[] anArray, byte value)
        {
            for (var i = 0; i < anArray.Length; i++)
            {
                anArray[i] = value;
            }
            return anArray;
        }

        public static byte[] trimLeftSpaces(byte[] arr)
        {
            var tList = new List<byte>(arr.Length);

            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] != ' ')
                {
                    tList.Add(arr[i]);
                }
            }
            return tList.ToArray();
        }

        public static byte[] textPadding(string text,
            Encoding charEncoding,
            int length)
        {
            return textPadding(text, charEncoding, length, ALIGN_LEFT);
        }

        public static byte[] textPadding(string text,
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

        public static byte[] textPadding(string text,
            Encoding charEncoding,
            int length,
            int alignment,
            byte paddingByte)
        {
            var tEncoding = charEncoding;
            var inputBytes = tEncoding.GetBytes(text);
            if (inputBytes.Length >= length)
            {
                return inputBytes.Take(length).ToArray();
            }

            var byte_array = FillArray(new byte[length], paddingByte);

            switch (alignment)
            {
                case ALIGN_LEFT:
                    Array.Copy(inputBytes,
                        0,
                        byte_array,
                        0,
                        inputBytes.Length);
                    break;

                case ALIGN_RIGHT:
                    var t_offset = length - text.Length;
                    Array.Copy(inputBytes,
                        0,
                        byte_array,
                        t_offset,
                        inputBytes.Length);
                    break;
            }

            return byte_array;
        }

        public static byte[] NumericFormating(IFormattable doubleNum,
            Encoding charEncoding,
            int fieldLength,
            int sizeDecimalPart)
        {
            var sizeWholePart = fieldLength
                                -
                                (sizeDecimalPart > 0 ? (sizeDecimalPart + 1) : 0);

            var format = new StringBuilder(fieldLength);

            for (var i = 0; i < sizeWholePart; i++)
            {
                format.Append(i + 1 == sizeWholePart ? "0" : "#");
            }

            if (sizeDecimalPart > 0)
            {
                format.Append(".");

                for (var i = 0; i < sizeDecimalPart; i++)
                {
                    format.Append("0");
                }
            }


            return
                textPadding(
                    doubleNum.ToString(format.ToString(),
                        NumberFormatInfo.InvariantInfo),
                    charEncoding,
                    fieldLength,
                    ALIGN_RIGHT);
        }

        public static bool contains(byte[] arr, byte value)
        {
            return
                Array.Exists(arr,
                    delegate(byte anItem) { return anItem == value; });
        }


        public static Type TypeForNativeDBType(NativeDbType aType)
        {
            switch (aType)
            {
                case NativeDbType.Char:
                    return typeof(string);
                case NativeDbType.Date:
                    return typeof(DateTime);
                case NativeDbType.Numeric:
                    return typeof(decimal);
                case NativeDbType.Logical:
                    return typeof(bool);
                case NativeDbType.Float:
                    return typeof(decimal);
                case NativeDbType.Memo:
                    return typeof(MemoValue);
                default:
                    return typeof(Object);
            }
        }
    }
}